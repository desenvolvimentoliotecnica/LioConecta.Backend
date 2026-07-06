using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class GraphDirectorySyncService(
    IGraphAdapter graphAdapter,
    IPersonPhotoStorageService photoStorage,
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

            return new GraphDirectorySyncResult(0, 0, 0, 0, 0, 0, 0, DateTimeOffset.UtcNow);
        }

        var objectIdToPersonId = new Dictionary<Guid, Guid>();
        var seenObjectIds = new HashSet<Guid>();
        var reservedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

            var slug = await ResolveUniqueSlugAsync(person.Id, user, reservedSlugs, cancellationToken);
            reservedSlugs.Add(slug);

            person.AzureAdObjectId = user.ObjectId;
            person.Slug = slug;
            person.Name = user.DisplayName;
            person.Email = email;
            person.Title = user.JobTitle;
            person.Dept = string.IsNullOrWhiteSpace(user.Department) ? null : user.Department.Trim();
            person.DepartmentId = null;
            person.TeamsUpn = user.UserPrincipalName;
            person.Phone = FirstNonEmpty(user.MobilePhone, user.BusinessPhones.FirstOrDefault());
            person.Location = user.OfficeLocation;
            person.IsActive = user.AccountEnabled;
            person.Status = user.AccountEnabled ? "active" : "inactive";
            if (!string.IsNullOrWhiteSpace(user.EmployeeId) && string.IsNullOrWhiteSpace(person.EmployeeId))
            {
                person.EmployeeId = user.EmployeeId.Trim();
            }

            if (user.EmployeeHireDate is not null)
            {
                person.HireDate = user.EmployeeHireDate;
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
        var (photosDownloaded, photosMissing, photosFailed) =
            await SyncPhotosAsync(context, cancellationToken);
        var syncedAt = DateTimeOffset.UtcNow;
        await UpsertSyncTimestampAsync(syncedAt, cancellationToken);

        if (context is not null)
        {
            await context.LogInfoAsync(
                $"Graph directory sync completed: fetched={users.Count}, created={created}, updated={updated}, managers={managerLinks}, deactivated={deactivated}, photos={photosDownloaded}, photosMissing={photosMissing}, photosFailed={photosFailed}.",
                cancellationToken);
        }

        return new GraphDirectorySyncResult(
            created,
            updated,
            deactivated,
            users.Count,
            photosDownloaded,
            photosMissing,
            photosFailed,
            syncedAt);
    }

    private async Task<(int Downloaded, int Missing, int Failed)> SyncPhotosAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken)
    {
        var people = await db.People
            .Where(p => p.IsActive && p.AzureAdObjectId != null)
            .ToListAsync(cancellationToken);

        if (people.Count == 0)
        {
            return (0, 0, 0);
        }

        var photoBytes = new Dictionary<Guid, byte[]?>();
        var failed = 0;

        await Parallel.ForEachAsync(
            people,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 10,
                CancellationToken = cancellationToken,
            },
            async (person, ct) =>
            {
                try
                {
                    var bytes = await graphAdapter.GetUserPhotoBytesAsync(person.AzureAdObjectId!.Value, ct);
                    lock (photoBytes)
                    {
                        photoBytes[person.Id] = bytes;
                    }
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref failed);
                }
            });

        var downloaded = 0;
        var missing = 0;

        foreach (var person in people)
        {
            if (!photoBytes.TryGetValue(person.Id, out var bytes) || bytes is null || bytes.Length == 0)
            {
                missing++;
                continue;
            }

            try
            {
                person.PhotoUrl = await photoStorage.SaveAsync(person.Slug, bytes, cancellationToken);
                person.UpdatedAt = DateTimeOffset.UtcNow;
                downloaded++;
            }
            catch (Exception)
            {
                failed++;
            }
        }

        if (downloaded > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        if (context is not null && downloaded > 0)
        {
            await context.LogInfoAsync(
                $"Graph photo sync stored {downloaded} avatars under /media/people.",
                cancellationToken);
        }

        return (downloaded, missing, failed);
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

    private async Task<string> ResolveUniqueSlugAsync(
        Guid personId,
        GraphDirectoryUser user,
        ISet<string> reservedSlugs,
        CancellationToken cancellationToken)
    {
        var baseSlug = PersonSlugHelper.FromEmailOrUpn(user.Mail, user.UserPrincipalName);
        var candidates = new[]
        {
            baseSlug,
            $"{baseSlug}-{user.ObjectId.ToString("N")[..6]}",
            $"{baseSlug}-{user.ObjectId:N}",
        };

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!reservedSlugs.Contains(candidate)
                && !await SlugConflictsAsync(personId, candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return $"{baseSlug}-{user.ObjectId:N}";
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
