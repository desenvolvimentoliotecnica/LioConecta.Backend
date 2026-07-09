using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class UniLioApprovalRecipientResolver(
    IPersonRepository personRepository,
    ILeaveNotifyDirectory leaveNotifyDirectory,
    IAppSettingsProvider settingsProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Person>> ResolveApproversAsync(
        Guid submitterPersonId,
        CancellationToken cancellationToken = default)
    {
        var recipients = new Dictionary<Guid, Person>();

        var roles = ParseStringList(
            settingsProvider.GetString(AppSettingKeys.UniLioApprovalNotifyRoles, "[\"HR\",\"Admin\"]"));
        var roleSet = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase) { "Admin", "HR" };
        foreach (var person in await leaveNotifyDirectory.FindActivePeopleByPortalRolesAsync(
                     roleSet.ToList(),
                     cancellationToken))
        {
            recipients[person.Id] = person;
        }

        var notifyEmails = ParseStringList(
            settingsProvider.GetString(AppSettingKeys.UniLioApprovalNotifyEmails, "[]"));
        foreach (var person in await personRepository.GetByEmailsAsync(notifyEmails, cancellationToken))
        {
            recipients[person.Id] = person;
        }

        recipients.Remove(submitterPersonId);
        return recipients.Values.ToList();
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
