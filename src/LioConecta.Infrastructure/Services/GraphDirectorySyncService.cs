using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class GraphDirectorySyncService(
    IGraphAdapter graphAdapter,
    AppDbContext db) : IGraphDirectorySyncService
{
    public async Task<GraphDirectorySyncResult> SyncDirectoryAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken)
    {
        var users = await graphAdapter.GetDirectoryUsersAsync(cancellationToken);
        if (users.Count == 0)
        {
            if (context is not null)
            {
                await context.LogWarningAsync("Graph directory sync returned no users.", cancellationToken);
            }

            return new GraphDirectorySyncResult(0, 0, 0, 0, DateTimeOffset.UtcNow);
        }

        var objectIdToPersonId = new Dictionary<Guid, Guid>();
        var seenObjectIds = new HashSet<Guid>();
        var created = 0;
        var updated = 0;

        foreach (var user in users)
        {
            seenObjectIds.Add(user.ObjectId);
            var email = FirstNonEmpty(user.Mail, user.UserPrincipalName) ?? string.Empty;
            var person = await db.People
                .FirstOrDefaultAsync(
                    p => p.AzureAdObjectId == user.ObjectId
                         || (!string.IsNullOrWhiteSpace(email)
                             && p.Email.ToLower() == email.ToLower()),
                    cancellationToken);

            var isNew = person is null;
            if (person is null)
            {
                person = new Person
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                db.People.Add(person);
                created++;
            }
            else
            {
                updated++;
            }

            var slug = PersonSlugHelper.FromEmailOrUpn(user.Mail, user.UserPrincipalName);
            if (await SlugConflictsAsync(person.Id, slug, cancellationToken))
            {
                slug = $"{slug}-{user.ObjectId.ToString("N")[..6]}";
            }

            person.AzureAdObjectId = user.ObjectId;
            person.Slug = slug;
            person.Name = user.DisplayName;
            person.Email = email;
            person.Title = user.JobTitle;
            person.Dept = user.Department;
            person.TeamsUpn = user.UserPrincipalName;
            person.Phone = FirstNonEmpty(user.MobilePhone, user.BusinessPhones.FirstOrDefault());
            person.Location = user.OfficeLocation;
            person.IsActive = user.AccountEnabled;
            person.Status = user.AccountEnabled ? "active" : "inactive";
            if (!string.IsNullOrWhiteSpace(user.EmployeeId) && string.IsNullOrWhiteSpace(person.EmployeeId))
            {
                person.EmployeeId = user.EmployeeId.Trim();
            }

            person.UpdatedAt = DateTimeOffset.UtcNow;
            objectIdToPersonId[user.ObjectId] = person.Id;
        }

        await db.SaveChangesAsync(cancellationToken);

        var managerLinks = 0;
        foreach (var user in users)
        {
            if (user.ManagerObjectId is null
                || !objectIdToPersonId.TryGetValue(user.ObjectId, out var personId)
                || !objectIdToPersonId.TryGetValue(user.ManagerObjectId.Value, out var managerId))
            {
                continue;
            }

            var person = await db.People.FirstAsync(p => p.Id == personId, cancellationToken);
            if (person.ManagerId != managerId)
            {
                person.ManagerId = managerId;
                person.UpdatedAt = DateTimeOffset.UtcNow;
                managerLinks++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var deactivated = await DeactivateMissingGraphUsersAsync(seenObjectIds, cancellationToken);
        var syncedAt = DateTimeOffset.UtcNow;
        await UpsertSyncTimestampAsync(syncedAt, cancellationToken);

        if (context is not null)
        {
            await context.LogInfoAsync(
                $"Graph directory sync completed: fetched={users.Count}, created={created}, updated={updated}, managers={managerLinks}, deactivated={deactivated}.",
                cancellationToken);
        }

        return new GraphDirectorySyncResult(created, updated, deactivated, users.Count, syncedAt);
    }

    private async Task UpsertSyncTimestampAsync(DateTimeOffset syncedAt, CancellationToken cancellationToken)
    {
        var key = AppSettingKeys.GraphDirectoryLastSyncUtc;
        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting is null)
        {
            setting = new AppSetting
            {
                Id = Guid.NewGuid(),
                Key = key,
                Category = "graph",
                Label = "Diretório — última sincronização (UTC)",
                ValueType = "string",
                SortOrder = 4,
                CreatedAt = syncedAt,
            };
            db.AppSettings.Add(setting);
        }

        setting.Value = syncedAt.ToString("O");
        setting.UpdatedAt = syncedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> SlugConflictsAsync(Guid personId, string slug, CancellationToken cancellationToken)
    {
        return await db.People.AnyAsync(
            p => p.Id != personId && p.Slug == slug,
            cancellationToken);
    }

    private async Task<int> DeactivateMissingGraphUsersAsync(
        IReadOnlySet<Guid> seenObjectIds,
        CancellationToken cancellationToken)
    {
        var graphPeople = await db.People
            .Where(p => p.AzureAdObjectId != null && p.IsActive)
            .ToListAsync(cancellationToken);

        var deactivated = 0;
        foreach (var person in graphPeople)
        {
            if (person.AzureAdObjectId is null || seenObjectIds.Contains(person.AzureAdObjectId.Value))
            {
                continue;
            }

            person.IsActive = false;
            person.Status = "inactive";
            person.UpdatedAt = DateTimeOffset.UtcNow;
            deactivated++;
        }

        if (deactivated > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return deactivated;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
