using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class ServiceRequestService(
    IServiceRequestRepository serviceRequestRepository,
    ICurrentUserService currentUserService,
    IPermissionService permissionService,
    INotificationService notificationService,
    IServiceRequestAttachmentStore attachmentStore,
    LeaveNotifyRecipientResolver leaveNotifyRecipientResolver) : IServiceRequestService
{
    public static readonly string[] ManagedRhTypes =
    [
        "servicos-beneficios",
        "servicos-contracheque",
    ];

    private static readonly HashSet<ServiceRequestStatus> DecidableStatuses =
    [
        ServiceRequestStatus.Submitted,
        ServiceRequestStatus.InReview,
    ];

    private static readonly HashSet<ServiceRequestStatus> OpenForMessagingStatuses =
    [
        ServiceRequestStatus.Submitted,
        ServiceRequestStatus.InReview,
        ServiceRequestStatus.AwaitingConfirmation,
    ];

    public async Task<IReadOnlyList<ServiceRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var requesterId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var requests = await serviceRequestRepository.GetByRequesterAsync(requesterId, cancellationToken);
        return requests.Select(ServiceRequestMapper.ToDto).ToList();
    }

    public async Task<ServiceRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requesterId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var request = await serviceRequestRepository.GetByIdAsync(id, cancellationToken);
        if (request is null || request.RequesterId != requesterId)
        {
            return null;
        }

        return ServiceRequestMapper.ToDto(request);
    }

    public async Task<ServiceRequestDto> CreateAsync(
        CreateServiceRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var requesterId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var serviceRequest = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            Type = request.Type.Trim(),
            Category = request.Category,
            Status = ServiceRequestStatus.Submitted,
            RequesterId = requesterId,
            PayloadJson = JsonMapper.SerializeObjectDictionary(request.Payload),
            CreatedAt = now,
            UpdatedAt = now
        };

        await serviceRequestRepository.AddAsync(serviceRequest, cancellationToken);

        var submittedEvent = new ServiceRequestEvent
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            EventType = "Submitted",
            ActorId = requesterId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await serviceRequestRepository.AddEventAsync(submittedEvent, cancellationToken);
        serviceRequest.Events = [submittedEvent];

        var loaded = await serviceRequestRepository.GetByIdAsync(serviceRequest.Id, cancellationToken)
            ?? serviceRequest;

        if (ManagedRhTypes.Contains(loaded.Type, StringComparer.OrdinalIgnoreCase))
        {
            await NotifyCreatedAsync(loaded, cancellationToken);
        }

        return ServiceRequestMapper.ToDto(loaded);
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> GetManagementListAsync(
        ServiceRequestStatus? status,
        string? query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rh_requests.manage", DataScope.Global, cancellationToken);
        var items = await serviceRequestRepository.ListManagementAsync(
            ManagedRhTypes,
            status,
            query,
            limit <= 0 ? 50 : limit,
            cancellationToken);
        return items.Select(ServiceRequestMapper.ToDto).ToList();
    }

    public async Task<ServiceRequestDto?> GetManagementDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rh_requests.manage", DataScope.Global, cancellationToken);
        var request = await serviceRequestRepository.GetByIdAsync(id, cancellationToken);
        if (request is null || !ManagedRhTypes.Contains(request.Type, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        return ServiceRequestMapper.ToDto(request);
    }

    public async Task<ServiceRequestDto?> ApproveAsync(
        Guid id,
        ApproveServiceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rh_requests.manage", DataScope.Global, cancellationToken);
        return await DecideAsync(id, approved: true, commentOrReason: request.Comment, cancellationToken);
    }

    public async Task<ServiceRequestDto?> RejectAsync(
        Guid id,
        RejectServiceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rh_requests.manage", DataScope.Global, cancellationToken);
        return await DecideAsync(id, approved: false, commentOrReason: request.Reason, cancellationToken);
    }

    public async Task<ServiceRequestDto?> ReplyAsManagerAsync(
        Guid id,
        string? message,
        IReadOnlyList<ServiceRequestAttachmentInput>? attachments,
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rh_requests.manage", DataScope.Global, cancellationToken);
        return await AddMessageAsync(id, fromRh: true, message, attachments, cancellationToken);
    }

    public async Task<ServiceRequestDto?> ReplyAsRequesterAsync(
        Guid id,
        string? message,
        IReadOnlyList<ServiceRequestAttachmentInput>? attachments,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var entity = await serviceRequestRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (entity is null || entity.RequesterId != personId)
        {
            return null;
        }

        return await AddMessageAsync(id, fromRh: false, message, attachments, cancellationToken);
    }

    public async Task<ServiceRequestDto?> FinalizeAsync(
        Guid id,
        FinalizeServiceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rh_requests.manage", DataScope.Global, cancellationToken);
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var entity = await serviceRequestRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (entity is null || !ManagedRhTypes.Contains(entity.Type, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!DecidableStatuses.Contains(entity.Status))
        {
            throw new InvalidOperationException(
                $"Não é possível finalizar um pedido com status {entity.Status}.");
        }

        var now = DateTimeOffset.UtcNow;
        entity.Status = ServiceRequestStatus.AwaitingConfirmation;
        entity.UpdatedAt = now;

        var details = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            details["comment"] = request.Comment.Trim();
            details["message"] = request.Comment.Trim();
        }

        var finalizeEvent = new ServiceRequestEvent
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = entity.Id,
            EventType = "Finalized",
            ActorId = actorId,
            DetailsJson = details.Count == 0 ? null : JsonSerializer.Serialize(details),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await serviceRequestRepository.UpdateStatusAndAddEventAsync(
            entity.Id,
            ServiceRequestStatus.AwaitingConfirmation,
            finalizeEvent,
            cancellationToken);

        await notificationService.NotifyServiceRequestFinalizedAsync(
            entity.RequesterId,
            entity.Id,
            entity.Type,
            request.Comment,
            cancellationToken);

        var refreshed = await serviceRequestRepository.GetByIdAsync(entity.Id, cancellationToken) ?? entity;
        return ServiceRequestMapper.ToDto(refreshed);
    }

    public async Task<ServiceRequestDto?> ConfirmClosureAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var entity = await serviceRequestRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (entity is null || entity.RequesterId != personId)
        {
            return null;
        }

        if (entity.Status != ServiceRequestStatus.AwaitingConfirmation)
        {
            throw new InvalidOperationException(
                "Só é possível confirmar o encerramento quando o RH já finalizou o atendimento.");
        }

        var now = DateTimeOffset.UtcNow;
        var confirmEvent = new ServiceRequestEvent
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = entity.Id,
            EventType = "ClosureConfirmed",
            ActorId = personId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await serviceRequestRepository.UpdateStatusAndAddEventAsync(
            entity.Id,
            ServiceRequestStatus.Completed,
            confirmEvent,
            cancellationToken);

        var recipients = await leaveNotifyRecipientResolver.ResolveAsync(entity.RequesterId, cancellationToken);
        if (recipients.Count > 0)
        {
            var requesterName = entity.Requester?.Name?.Trim() ?? "Colaborador";
            await notificationService.NotifyServiceRequestClosureConfirmedAsync(
                recipients.Select(r => r.Id).ToList(),
                entity.Id,
                entity.Type,
                requesterName,
                cancellationToken);
        }

        var refreshed = await serviceRequestRepository.GetByIdAsync(entity.Id, cancellationToken) ?? entity;
        return ServiceRequestMapper.ToDto(refreshed);
    }

    public async Task<ServiceRequestAttachmentFileDto?> GetAttachmentAsync(
        Guid id,
        string storageFileName,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var request = await serviceRequestRepository.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var isRequester = request.RequesterId == personId;
        var isManager = false;
        if (!isRequester)
        {
            try
            {
                await permissionService.EnsurePermissionAsync(
                    "rh_requests.manage",
                    DataScope.Global,
                    cancellationToken);
                isManager = ManagedRhTypes.Contains(request.Type, StringComparer.OrdinalIgnoreCase);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        if (!isRequester && !isManager)
        {
            return null;
        }

        var safeName = Path.GetFileName(storageFileName);
        ServiceRequestAttachmentMetaDto? meta = null;
        foreach (var ev in request.Events)
        {
            var attachments = ExtractAttachments(ev.DetailsJson);
            meta = attachments.FirstOrDefault(a =>
                string.Equals(a.StorageFileName, safeName, StringComparison.OrdinalIgnoreCase));
            if (meta is not null)
            {
                break;
            }
        }

        if (meta is null)
        {
            return null;
        }

        var absolutePath = attachmentStore.ResolveAbsolutePath(meta.StorageFileName);
        if (absolutePath is null)
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(absolutePath, cancellationToken);
        return new ServiceRequestAttachmentFileDto(bytes, meta.ContentType, meta.FileName);
    }

    private async Task<ServiceRequestDto?> AddMessageAsync(
        Guid id,
        bool fromRh,
        string? message,
        IReadOnlyList<ServiceRequestAttachmentInput>? attachments,
        CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var entity = await serviceRequestRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (fromRh && !ManagedRhTypes.Contains(entity.Type, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!fromRh && entity.RequesterId != actorId)
        {
            return null;
        }

        if (!OpenForMessagingStatuses.Contains(entity.Status))
        {
            throw new InvalidOperationException(
                $"Não é possível enviar mensagem em um pedido com status {entity.Status}.");
        }

        var trimmed = message?.Trim() ?? string.Empty;
        var savedAttachments = await SaveAttachmentsAsync(attachments, cancellationToken);
        if (string.IsNullOrWhiteSpace(trimmed) && savedAttachments.Count == 0)
        {
            throw new InvalidOperationException("Informe uma mensagem ou anexe ao menos um arquivo.");
        }

        var now = DateTimeOffset.UtcNow;
        var nextStatus = entity.Status;
        if (fromRh && entity.Status == ServiceRequestStatus.Submitted)
        {
            nextStatus = ServiceRequestStatus.InReview;
        }

        var details = new Dictionary<string, object?>
        {
            ["role"] = fromRh ? "rh" : "requester",
            ["message"] = trimmed,
        };
        if (savedAttachments.Count > 0)
        {
            details["attachments"] = savedAttachments.Select(a => new
            {
                fileName = a.FileName,
                storageFileName = a.StorageFileName,
                contentType = a.ContentType,
                sizeBytes = a.SizeBytes,
                url = a.Url,
            }).ToList();
        }

        var messageEvent = new ServiceRequestEvent
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = entity.Id,
            EventType = "Message",
            ActorId = actorId,
            DetailsJson = JsonSerializer.Serialize(details),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await serviceRequestRepository.UpdateStatusAndAddEventAsync(
            entity.Id,
            nextStatus,
            messageEvent,
            cancellationToken);

        var refreshed = await serviceRequestRepository.GetByIdAsync(entity.Id, cancellationToken) ?? entity;
        var actorName = refreshed.Events
            .FirstOrDefault(e => e.Id == messageEvent.Id)?.Actor?.Name
            ?? (fromRh ? "RH" : "Colaborador");
        var preview = trimmed.Length > 0
            ? trimmed
            : $"{savedAttachments.Count} anexo(s)";

        if (fromRh)
        {
            await notificationService.NotifyServiceRequestMessageAsync(
                entity.RequesterId,
                entity.Id,
                entity.Type,
                actorName,
                fromRh: true,
                preview,
                cancellationToken);
        }
        else
        {
            var recipients = await leaveNotifyRecipientResolver.ResolveAsync(entity.RequesterId, cancellationToken);
            foreach (var recipient in recipients)
            {
                await notificationService.NotifyServiceRequestMessageAsync(
                    recipient.Id,
                    entity.Id,
                    entity.Type,
                    actorName,
                    fromRh: false,
                    preview,
                    cancellationToken);
            }
        }

        return ServiceRequestMapper.ToDto(refreshed);
    }

    private async Task<List<ServiceRequestAttachmentMetaDto>> SaveAttachmentsAsync(
        IReadOnlyList<ServiceRequestAttachmentInput>? attachments,
        CancellationToken cancellationToken)
    {
        var result = new List<ServiceRequestAttachmentMetaDto>();
        if (attachments is null || attachments.Count == 0)
        {
            return result;
        }

        if (attachments.Count > ServiceRequestAttachmentLimits.MaxFilesPerMessage)
        {
            throw new InvalidOperationException(
                $"Máximo de {ServiceRequestAttachmentLimits.MaxFilesPerMessage} anexos por mensagem.");
        }

        foreach (var attachment in attachments)
        {
            result.Add(await attachmentStore.SaveAsync(
                attachment.Content,
                attachment.FileName,
                attachment.ContentType,
                attachment.SizeBytes,
                cancellationToken));
        }

        return result;
    }

    private static IReadOnlyList<ServiceRequestAttachmentMetaDto> ExtractAttachments(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(detailsJson);
            if (!doc.RootElement.TryGetProperty("attachments", out var arr) ||
                arr.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var list = new List<ServiceRequestAttachmentMetaDto>();
            foreach (var item in arr.EnumerateArray())
            {
                var fileName = item.TryGetProperty("fileName", out var fn) ? fn.GetString()
                    : item.TryGetProperty("FileName", out var fn2) ? fn2.GetString() : null;
                var storage = item.TryGetProperty("storageFileName", out var sn) ? sn.GetString()
                    : item.TryGetProperty("StorageFileName", out var sn2) ? sn2.GetString() : null;
                var contentType = item.TryGetProperty("contentType", out var ct) ? ct.GetString()
                    : item.TryGetProperty("ContentType", out var ct2) ? ct2.GetString() : "application/octet-stream";
                var size = item.TryGetProperty("sizeBytes", out var sb) && sb.TryGetInt64(out var sizeVal)
                    ? sizeVal
                    : item.TryGetProperty("SizeBytes", out var sb2) && sb2.TryGetInt64(out var sizeVal2)
                        ? sizeVal2
                        : 0L;
                var url = item.TryGetProperty("url", out var u) ? u.GetString()
                    : item.TryGetProperty("Url", out var u2) ? u2.GetString() : "";

                if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(storage))
                {
                    continue;
                }

                list.Add(new ServiceRequestAttachmentMetaDto(
                    fileName,
                    storage,
                    contentType ?? "application/octet-stream",
                    size,
                    url ?? ""));
            }

            return list;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<ServiceRequestDto?> DecideAsync(
        Guid id,
        bool approved,
        string? commentOrReason,
        CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var entity = await serviceRequestRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (entity is null || !ManagedRhTypes.Contains(entity.Type, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!DecidableStatuses.Contains(entity.Status))
        {
            throw new InvalidOperationException(
                $"Não é possível {(approved ? "aprovar" : "rejeitar")} um pedido com status {entity.Status}.");
        }

        var now = DateTimeOffset.UtcNow;
        var nextStatus = approved ? ServiceRequestStatus.Approved : ServiceRequestStatus.Rejected;

        var details = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(commentOrReason))
        {
            details[approved ? "comment" : "reason"] = commentOrReason.Trim();
        }

        var decisionEvent = new ServiceRequestEvent
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = entity.Id,
            EventType = approved ? "Approved" : "Rejected",
            ActorId = actorId,
            DetailsJson = details.Count == 0 ? null : JsonSerializer.Serialize(details),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await serviceRequestRepository.UpdateStatusAndAddEventAsync(
            entity.Id,
            nextStatus,
            decisionEvent,
            cancellationToken);

        await notificationService.NotifyServiceRequestDecisionAsync(
            entity.RequesterId,
            entity.Id,
            entity.Type,
            approved,
            approved ? null : commentOrReason,
            cancellationToken);

        var refreshed = await serviceRequestRepository.GetByIdAsync(entity.Id, cancellationToken) ?? entity;
        return ServiceRequestMapper.ToDto(refreshed);
    }

    private async Task NotifyCreatedAsync(ServiceRequest request, CancellationToken cancellationToken)
    {
        var recipients = await leaveNotifyRecipientResolver.ResolveAsync(request.RequesterId, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var requesterName = request.Requester?.Name?.Trim();
        if (string.IsNullOrWhiteSpace(requesterName))
        {
            requesterName = "Colaborador";
        }

        var typeLabel = TypeLabel(request.Type);
        var summary = $"{requesterName} enviou um pedido de {typeLabel}.";
        await notificationService.NotifyServiceRequestCreatedAsync(
            recipients.Select(r => r.Id).ToList(),
            request.Id,
            summary,
            cancellationToken,
            title: $"Novo pedido — {typeLabel}");
    }

    internal static string TypeLabel(string type) => type.ToLowerInvariant() switch
    {
        "servicos-beneficios" => "benefícios",
        "servicos-contracheque" => "contracheque",
        _ => "RH",
    };
}
