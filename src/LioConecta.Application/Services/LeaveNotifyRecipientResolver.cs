using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class LeaveNotifyRecipientResolver(
    IPersonRepository personRepository,
    ILeaveNotifyDirectory leaveNotifyDirectory,
    IPermissionService permissionService,
    IAppSettingsProvider settingsProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Person>> ResolveAsync(
        Guid requesterPersonId,
        CancellationToken cancellationToken = default)
    {
        var recipients = new Dictionary<Guid, Person>();

        var requester = await personRepository.GetByIdAsync(requesterPersonId, cancellationToken);
        if (requester?.ManagerId is Guid managerId)
        {
            var manager = await personRepository.GetByIdAsync(managerId, cancellationToken);
            if (manager is { IsActive: true })
            {
                recipients[manager.Id] = manager;
            }
        }

        var roles = ParseStringList(settingsProvider.GetString(AppSettingKeys.LeaveNotifyRoles, "[\"HR\"]"));
        var roleSet = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase) { "Admin" };
        foreach (var person in await leaveNotifyDirectory.FindActivePeopleByPortalRolesAsync(
                     roleSet.ToList(),
                     cancellationToken))
        {
            recipients[person.Id] = person;
        }

        var notifyEmails = ParseStringList(settingsProvider.GetString(AppSettingKeys.LeaveNotifyEmails, "[]"));
        foreach (var person in await personRepository.GetByEmailsAsync(notifyEmails, cancellationToken))
        {
            recipients[person.Id] = person;
        }

        recipients.Remove(requesterPersonId);
        return recipients.Values.ToList();
    }

    public async Task<(bool CanAccess, bool IsRhScope)> CanManageAsync(
        Guid currentPersonId,
        IReadOnlyList<UserRole> currentRoles,
        CancellationToken cancellationToken = default)
    {
        if (await permissionService.HasPermissionAsync("leave.manage", DataScope.Global, cancellationToken))
        {
            return (true, true);
        }

        if (await permissionService.HasPermissionAsync("leave.approve", DataScope.Global, cancellationToken))
        {
            return (true, true);
        }

        if (await permissionService.HasPermissionAsync("leave.approve", DataScope.Team, cancellationToken))
        {
            var reports = await personRepository.GetDirectReportsAsync(currentPersonId, cancellationToken);
            if (reports.Count > 0 || currentRoles.Contains(UserRole.Manager))
            {
                return (true, false);
            }
        }

        return (false, false);
    }

    private static IReadOnlyList<string> ParseStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions)?
                       .Where(item => !string.IsNullOrWhiteSpace(item))
                       .Select(item => item.Trim())
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList()
                   ?? [];
        }
        catch
        {
            return [];
        }
    }
}
