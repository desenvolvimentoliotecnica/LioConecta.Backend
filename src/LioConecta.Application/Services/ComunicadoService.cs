using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using System.Text.Json;

namespace LioConecta.Application.Services;

public sealed class ComunicadoService(
    IComunicadoRepository comunicadoRepository,
    ICurrentUserService currentUserService,
    IPermissionService permissionService,
    INotificationService notificationService) : IComunicadoService
{
    public async Task<PagedResult<ComunicadoListItemDto>> ListAsync(
        ComunicadoKind? kind,
        bool archivedOnly,
        bool manage,
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var canManage = manage && await permissionService.HasPermissionAsync(
            "comunicados.manage", cancellationToken: cancellationToken);
        if (archivedOnly && !canManage)
        {
            return PagedResult<ComunicadoListItemDto>.FromItems([], null, false);
        }
        var viewerDepartmentId = await comunicadoRepository.GetDepartmentIdAsync(viewerId, cancellationToken);
        var page = await comunicadoRepository.GetPageAsync(
            kind, archivedOnly, viewerDepartmentId, canManage, request, cancellationToken);
        var items = new List<ComunicadoListItemDto>();

        foreach (var comunicado in page.Items)
        {
            var isRead = await comunicadoRepository.IsReadAsync(comunicado.Id, viewerId, cancellationToken);
            items.Add(ComunicadoMapper.ToListItem(comunicado, isRead));
        }

        return PagedResult<ComunicadoListItemDto>.FromItems(items, page.NextCursor, page.HasMore);
    }

    public async Task<ComunicadoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var comunicado = await comunicadoRepository.GetByIdAsync(id, cancellationToken);
        if (comunicado is null || !await CanViewAsync(comunicado, viewerId, cancellationToken))
        {
            return null;
        }

        var isRead = await comunicadoRepository.IsReadAsync(id, viewerId, cancellationToken);
        return ComunicadoMapper.ToDto(comunicado, isRead);
    }

    public async Task<ComunicadoDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var comunicado = await comunicadoRepository.GetBySlugAsync(normalized, cancellationToken);
        if (comunicado is null || !await CanViewAsync(comunicado, viewerId, cancellationToken))
        {
            return null;
        }

        var isRead = await comunicadoRepository.IsReadAsync(comunicado.Id, viewerId, cancellationToken);
        return ComunicadoMapper.ToDto(comunicado, isRead);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var comunicado = await comunicadoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Comunicado {id} was not found.");
        if (!await CanViewAsync(comunicado, viewerId, cancellationToken))
        {
            throw new KeyNotFoundException($"Comunicado {id} was not found.");
        }

        await comunicadoRepository.MarkAsReadAsync(id, viewerId, cancellationToken);
    }

    public async Task<ComunicadoHubDto> GetHubAsync(CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var counts = await comunicadoRepository.GetActiveCountsByKindAsync(cancellationToken);
        var archivedCount = await comunicadoRepository.GetArchivedCountAsync(cancellationToken);
        var urgentesUnread = await comunicadoRepository.GetUnreadUrgentCountAsync(viewerId, cancellationToken);
        var recent = await comunicadoRepository.GetRecentActiveAsync(5, cancellationToken);

        var recentItems = new List<ComunicadoListItemDto>();
        foreach (var comunicado in recent)
        {
            var isRead = await comunicadoRepository.IsReadAsync(comunicado.Id, viewerId, cancellationToken);
            recentItems.Add(ComunicadoMapper.ToListItem(comunicado, isRead));
        }

        return new ComunicadoHubDto(
            counts.GetValueOrDefault(ComunicadoKind.Oficial),
            counts.GetValueOrDefault(ComunicadoKind.Departamental),
            counts.GetValueOrDefault(ComunicadoKind.Urgente),
            urgentesUnread,
            archivedCount,
            recentItems);
    }

    public async Task<ComunicadoDto> CreateAsync(
        CreateComunicadoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required.", nameof(request));
        }

        await EnsureCreatePermissionAsync(request.Kind, cancellationToken);
        var authorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var comunicadoId = Guid.NewGuid();
        var status = request.Status;
        if (status == ComunicadoStatus.Archived)
        {
            throw new ArgumentException("A comunicado cannot be created archived.", nameof(request));
        }

        if (status == ComunicadoStatus.Scheduled && !request.ScheduledAt.HasValue)
        {
            throw new ArgumentException("ScheduledAt is required for scheduled comunicados.", nameof(request));
        }

        var comunicado = new Comunicado
        {
            Id = comunicadoId,
            Kind = request.Kind,
            Title = request.Title.Trim(),
            Slug = SlugHelper.FromTitle(request.Title, comunicadoId),
            Excerpt = request.Excerpt?.Trim(),
            ContentJson = JsonSerializer.Serialize(request.Content ?? new Dictionary<string, object?>()),
            AuthorId = authorId,
            HeroImageUrl = request.HeroImageUrl,
            IsMandatory = request.IsMandatory,
            Status = status,
            ScheduledAt = status == ComunicadoStatus.Scheduled ? request.ScheduledAt : null,
            AudienceType = request.AudienceType,
            AudienceDepartmentIdsJson = SerializeAudience(request.AudienceType, request.AudienceDepartmentIds),
            PublishedAt = status == ComunicadoStatus.Published ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };

        await comunicadoRepository.AddAsync(comunicado, cancellationToken);
        await comunicadoRepository.SaveChangesAsync(cancellationToken);
        await EnsurePublishedSideEffectsAsync(comunicado, now, cancellationToken);
        return ComunicadoMapper.ToDto(comunicado, false);
    }

    public async Task<ComunicadoDto> UpdateAsync(
        Guid id,
        UpdateComunicadoRequest request,
        CancellationToken cancellationToken = default)
    {
        var comunicado = await GetManageableAsync(id, cancellationToken);
        if (comunicado.Status is not (ComunicadoStatus.Draft or ComunicadoStatus.Scheduled))
        {
            throw new InvalidOperationException("Only draft or scheduled comunicados can be edited.");
        }

        if (request.Kind.HasValue) comunicado.Kind = request.Kind.Value;
        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title)) throw new ArgumentException("Title cannot be empty.");
            comunicado.Title = request.Title.Trim();
        }
        if (request.Excerpt is not null) comunicado.Excerpt = request.Excerpt.Trim();
        if (request.Content is not null) comunicado.ContentJson = JsonSerializer.Serialize(request.Content);
        if (request.HeroImageUrl is not null) comunicado.HeroImageUrl = request.HeroImageUrl;
        if (request.IsMandatory.HasValue) comunicado.IsMandatory = request.IsMandatory.Value;
        if (request.AudienceType.HasValue)
        {
            comunicado.AudienceType = request.AudienceType.Value;
            comunicado.AudienceDepartmentIdsJson = SerializeAudience(
                comunicado.AudienceType, request.AudienceDepartmentIds);
        }
        else if (request.AudienceDepartmentIds is not null)
        {
            comunicado.AudienceDepartmentIdsJson = SerializeAudience(
                comunicado.AudienceType, request.AudienceDepartmentIds);
        }

        comunicado.UpdatedAt = DateTimeOffset.UtcNow;
        await comunicadoRepository.SaveChangesAsync(cancellationToken);
        return ComunicadoMapper.ToDto(comunicado, false);
    }

    public async Task<ComunicadoDto> PublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var comunicado = await GetManageableAsync(id, cancellationToken);
        await PublishCoreAsync(comunicado, cancellationToken);
        return ComunicadoMapper.ToDto(comunicado, false);
    }

    public async Task<ComunicadoDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var comunicado = await GetManageableAsync(id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        comunicado.Status = ComunicadoStatus.Archived;
        comunicado.ArchivedAt = now;
        comunicado.ScheduledAt = null;
        comunicado.UpdatedAt = now;
        await comunicadoRepository.SaveChangesAsync(cancellationToken);
        return ComunicadoMapper.ToDto(comunicado, false);
    }

    public async Task<ComunicadoDto> ScheduleAsync(
        Guid id,
        DateTimeOffset scheduledAt,
        CancellationToken cancellationToken = default)
    {
        if (scheduledAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("ScheduledAt must be in the future.", nameof(scheduledAt));
        }

        var comunicado = await GetManageableAsync(id, cancellationToken);
        if (comunicado.Status == ComunicadoStatus.Archived)
        {
            throw new InvalidOperationException("Archived comunicados cannot be scheduled.");
        }

        comunicado.Status = ComunicadoStatus.Scheduled;
        comunicado.ScheduledAt = scheduledAt;
        comunicado.PublishedAt = null;
        comunicado.UpdatedAt = DateTimeOffset.UtcNow;
        await comunicadoRepository.SaveChangesAsync(cancellationToken);
        return ComunicadoMapper.ToDto(comunicado, false);
    }

    public async Task<ComunicadoMetricsDto> GetMetricsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var comunicado = await GetManageableAsync(id, cancellationToken);
        var metrics = await comunicadoRepository.GetMetricsAsync(comunicado, cancellationToken);
        var percent = metrics.EligibleReaders == 0
            ? 0
            : Math.Round(metrics.ReadCount * 100m / metrics.EligibleReaders, 2);
        return new ComunicadoMetricsDto(id, metrics.EligibleReaders, metrics.ReadCount, percent);
    }

    public async Task<int> PublishScheduledAsync(CancellationToken cancellationToken = default)
    {
        var due = await comunicadoRepository.GetScheduledDueAsync(DateTimeOffset.UtcNow, cancellationToken);
        foreach (var comunicado in due)
        {
            await PublishCoreAsync(comunicado, cancellationToken);
        }

        return due.Count;
    }

    private async Task<Comunicado> GetManageableAsync(Guid id, CancellationToken cancellationToken)
    {
        await permissionService.EnsurePermissionAsync("comunicados.manage", cancellationToken: cancellationToken);
        return await comunicadoRepository.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Comunicado {id} was not found.");
    }

    private async Task PublishCoreAsync(Comunicado comunicado, CancellationToken cancellationToken)
    {
        if (comunicado.Status == ComunicadoStatus.Archived)
        {
            throw new InvalidOperationException("Archived comunicados cannot be published.");
        }

        var now = DateTimeOffset.UtcNow;
        comunicado.Status = ComunicadoStatus.Published;
        comunicado.PublishedAt ??= now;
        comunicado.ScheduledAt = null;
        comunicado.UpdatedAt = now;
        await comunicadoRepository.SaveChangesAsync(cancellationToken);
        await EnsurePublishedSideEffectsAsync(comunicado, now, cancellationToken);
    }

    private async Task EnsurePublishedSideEffectsAsync(
        Comunicado comunicado,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (comunicado.Status != ComunicadoStatus.Published ||
            await comunicadoRepository.HasFeedPostAsync(comunicado.Id, cancellationToken))
        {
            return;
        }

        await comunicadoRepository.AddFeedPostAsync(comunicado, now, cancellationToken);
        await comunicadoRepository.SaveChangesAsync(cancellationToken);
        await notificationService.NotifyComunicadoCreatedAsync(comunicado, cancellationToken);
    }

    private async Task<bool> CanViewAsync(
        Comunicado comunicado,
        Guid viewerId,
        CancellationToken cancellationToken)
    {
        if (await permissionService.HasPermissionAsync("comunicados.manage", cancellationToken: cancellationToken))
        {
            return true;
        }
        if (comunicado.Status != ComunicadoStatus.Published) return false;
        if (comunicado.AudienceType == ComunicadoAudienceType.All) return true;

        var viewerDepartmentId = await comunicadoRepository.GetDepartmentIdAsync(viewerId, cancellationToken);
        return viewerDepartmentId.HasValue &&
               comunicado.AudienceDepartmentIdsJson.Contains(
                   viewerDepartmentId.Value.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureCreatePermissionAsync(ComunicadoKind kind, CancellationToken cancellationToken)
    {
        if (await permissionService.HasPermissionAsync("comunicados.manage", cancellationToken: cancellationToken))
        {
            return;
        }

        var key = kind switch
        {
            ComunicadoKind.Departamental => "comunicados.publish.departmental",
            ComunicadoKind.Urgente => "comunicados.publish.urgent",
            _ => "comunicados.publish.official"
        };
        await permissionService.EnsurePermissionAsync(key, cancellationToken: cancellationToken);
    }

    private static string SerializeAudience(
        ComunicadoAudienceType audienceType,
        IReadOnlyList<Guid>? departmentIds)
    {
        var ids = (departmentIds ?? [])
            .Distinct()
            .ToArray();
        if (audienceType == ComunicadoAudienceType.Departments && ids.Length == 0)
        {
            throw new ArgumentException("At least one department is required for department audiences.");
        }

        return JsonSerializer.Serialize(
            audienceType == ComunicadoAudienceType.All ? Array.Empty<Guid>() : ids);
    }
}
