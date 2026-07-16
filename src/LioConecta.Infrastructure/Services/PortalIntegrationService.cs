using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class PortalIntegrationService(
    AppDbContext db,
    IPersonRepository personRepository,
    ILeaveNotifyDirectory leaveNotifyDirectory,
    IEmailQueueService emailQueue,
    INotificationService notificationService) : IPortalIntegrationService
{
    public async Task<IntegrationFeedPublishResponse> PublishFeedAsync(
        IntegrationFeedPublishRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Content is required.", nameof(request));
        }

        if (!Enum.TryParse<PostType>(request.Type, ignoreCase: true, out var postType))
        {
            throw new ArgumentException($"Invalid post type: {request.Type}", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await db.FeedPosts.AsNoTracking()
                .Where(p => !p.IsDeleted && p.MetadataJson != null && p.MetadataJson.Contains(request.IdempotencyKey))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing != Guid.Empty)
            {
                return new IntegrationFeedPublishResponse(existing);
            }
        }

        var now = DateTimeOffset.UtcNow;
        var metadata = request.MetadataJson;
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            metadata = MergeIdempotency(metadata, request.IdempotencyKey);
        }

        var post = new FeedPost
        {
            Id = Guid.NewGuid(),
            AuthorId = request.AuthorPersonId,
            Type = postType,
            Content = request.Content.Trim(),
            MetadataJson = metadata,
            IsPinned = request.IsPinned,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.FeedPosts.Add(post);
        await db.SaveChangesAsync(cancellationToken);
        return new IntegrationFeedPublishResponse(post.Id);
    }

    public Task<IntegrationNotifyResponse> NotifyAsync(
        IntegrationNotifyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            throw new ArgumentException("Title and Body are required.");
        }

        return NotifyCoreAsync(request, cancellationToken);
    }

    private async Task<IntegrationNotifyResponse> NotifyCoreAsync(
        IntegrationNotifyRequest request,
        CancellationToken cancellationToken)
    {
        var count = await notificationService.NotifySystemRecipientsAsync(
            request.RecipientPersonIds,
            request.AllActive,
            request.Title.Trim(),
            request.Body.Trim(),
            request.Href ?? "/",
            cancellationToken);
        return new IntegrationNotifyResponse(count);
    }

    public async Task<IntegrationEmailEnqueueResponse> EnqueueEmailAsync(
        IntegrationEmailEnqueueRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.To is null || request.To.Count == 0)
        {
            throw new ArgumentException("At least one recipient is required.", nameof(request));
        }

        Guid? correlationId = null;
        if (!string.IsNullOrWhiteSpace(request.CorrelationId)
            && Guid.TryParse(request.CorrelationId, out var parsed))
        {
            correlationId = parsed;
        }

        var result = await emailQueue.EnqueueAsync(
            new EmailEnqueueRequest(
                To: request.To,
                Subject: request.Subject,
                BodyHtml: request.BodyHtml,
                BodyText: request.BodyText,
                MetadataJson: request.MetadataJson,
                Priority: request.Priority,
                IdempotencyKey: request.IdempotencyKey,
                CorrelationId: correlationId,
                CreatedById: request.CreatedById),
            cancellationToken);

        return new IntegrationEmailEnqueueResponse(result.Id);
    }

    public async Task<IntegrationPeopleResolveResponse> ResolvePeopleAsync(
        IntegrationPeopleResolveRequest request,
        CancellationToken cancellationToken = default)
    {
        var byId = new Dictionary<Guid, Person>();

        if (request.Roles is { Count: > 0 })
        {
            foreach (var person in await leaveNotifyDirectory.FindActivePeopleByPortalRolesAsync(
                         request.Roles, cancellationToken))
            {
                byId[person.Id] = person;
            }
        }

        if (request.Emails is { Count: > 0 })
        {
            foreach (var person in await personRepository.GetByEmailsAsync(request.Emails, cancellationToken))
            {
                if (!request.ActiveOnly || person.IsActive)
                {
                    byId[person.Id] = person;
                }
            }
        }

        if (request.PermissionKeys is { Count: > 0 })
        {
            foreach (var person in await FindPeopleByPermissionKeysAsync(
                         request.PermissionKeys, request.ActiveOnly, cancellationToken))
            {
                byId[person.Id] = person;
            }
        }

        if (request.ExcludePersonId is Guid exclude)
        {
            byId.Remove(exclude);
        }

        var roleMap = await LoadRoleNamesAsync(byId.Keys.ToList(), cancellationToken);

        var people = byId.Values
            .OrderBy(p => p.Name)
            .Select(p => new IntegrationPersonDto(
                p.Id,
                p.Name,
                p.Email,
                roleMap.GetValueOrDefault(p.Id, [])))
            .ToList();

        return new IntegrationPeopleResolveResponse(people);
    }

    private async Task<IReadOnlyList<Person>> FindPeopleByPermissionKeysAsync(
        IReadOnlyList<string> permissionKeys,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var keySet = permissionKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (keySet.Count == 0)
        {
            return [];
        }

        var roleIds = await db.RolePermissions.AsNoTracking()
            .Where(rp => keySet.Contains(rp.PermissionKey))
            .Select(rp => rp.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (roleIds.Count == 0)
        {
            return [];
        }

        var personIds = await db.SubjectRoleAssignments.AsNoTracking()
            .Where(a => a.SubjectType == RbacSubjectType.Person && roleIds.Contains(a.RoleId))
            .Select(a => a.SubjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var portalUserIds = await db.SubjectRoleAssignments.AsNoTracking()
            .Where(a => a.SubjectType == RbacSubjectType.PortalUser && roleIds.Contains(a.RoleId))
            .Select(a => a.SubjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (portalUserIds.Count > 0)
        {
            var emails = await db.PortalUsers.AsNoTracking()
                .Where(u => portalUserIds.Contains(u.Id) && u.IsActive)
                .Select(u => u.Email)
                .ToListAsync(cancellationToken);
            foreach (var person in await personRepository.GetByEmailsAsync(emails, cancellationToken))
            {
                personIds.Add(person.Id);
            }
        }

        personIds = personIds.Distinct().ToList();
        if (personIds.Count == 0)
        {
            return [];
        }

        var people = await personRepository.GetByIdsAsync(personIds, cancellationToken);
        return activeOnly ? people.Where(p => p.IsActive).ToList() : people;
    }

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> LoadRoleNamesAsync(
        IReadOnlyList<Guid> personIds,
        CancellationToken cancellationToken)
    {
        var result = personIds.ToDictionary(id => id, _ => (IReadOnlyList<string>)[]);
        if (personIds.Count == 0)
        {
            return result;
        }

        var rows = await (
            from a in db.SubjectRoleAssignments.AsNoTracking()
            join r in db.Roles.AsNoTracking() on a.RoleId equals r.Id
            where a.SubjectType == RbacSubjectType.Person && personIds.Contains(a.SubjectId)
            select new { a.SubjectId, r.Name }).ToListAsync(cancellationToken);

        foreach (var group in rows.GroupBy(x => x.SubjectId))
        {
            result[group.Key] = group.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        return result;
    }

    private static string MergeIdempotency(string? metadataJson, string idempotencyKey)
    {
        try
        {
            Dictionary<string, object?> map;
            if (string.IsNullOrWhiteSpace(metadataJson))
            {
                map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                map = JsonSerializer.Deserialize<Dictionary<string, object?>>(metadataJson)
                      ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }

            map["idempotencyKey"] = idempotencyKey;
            return JsonSerializer.Serialize(map);
        }
        catch
        {
            return JsonSerializer.Serialize(new { idempotencyKey });
        }
    }
}
