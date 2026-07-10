using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class PontoAdjustmentService(
    IPontoAdjustmentRepository pontoAdjustmentRepository,
    IServiceRequestService serviceRequestService,
    ICurrentUserService currentUserService,
    IPersonRepository personRepository,
    PontoNotifyRecipientResolver pontoNotifyRecipientResolver,
    INotificationService notificationService,
    IPontoAttachmentStore pontoAttachmentStore) : IPontoAdjustmentService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Regex TimeRegex = new(
        @"^([01]?\d|2[0-3]):[0-5]\d$",
        RegexOptions.Compiled);

    private const string InfoNote =
        "Esta solicitação notifica o gestor direto. A correção formal no espelho ocorre após tratamento no RM Labore.";

    public async Task<PontoAdjustmentResultDto> CreateAsync(
        CreatePontoAdjustmentDto request,
        IReadOnlyList<PontoAttachmentInput>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var reason = (request.Reason ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Informe o motivo do ajuste.");
        }

        var days = (request.Days ?? []).ToList();
        if (days.Count == 0)
        {
            throw new ArgumentException("Selecione ao menos um dia para solicitar o ajuste.");
        }

        if (days.Count > 31)
        {
            throw new ArgumentException("Máximo de 31 dias por solicitação.");
        }

        var normalizedDays = new List<object>(days.Count);
        foreach (var day in days)
        {
            ValidateTime(day.ClockIn, "entrada");
            ValidateTime(day.LunchOut, "saída para almoço");
            ValidateTime(day.LunchIn, "retorno do almoço");
            ValidateTime(day.ClockOut, "saída");

            normalizedDays.Add(new
            {
                date = day.Date.ToString("O", CultureInfo.InvariantCulture),
                originalClockIn = day.OriginalClockIn ?? string.Empty,
                originalLunchOut = day.OriginalLunchOut ?? string.Empty,
                originalLunchIn = day.OriginalLunchIn ?? string.Empty,
                originalClockOut = day.OriginalClockOut ?? string.Empty,
                clockIn = day.ClockIn.Trim(),
                lunchOut = day.LunchOut.Trim(),
                lunchIn = day.LunchIn.Trim(),
                clockOut = day.ClockOut.Trim(),
            });
        }

        var savedAttachments = await SaveAttachmentsAsync(attachments, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var dayCount = days.Count;
        var title = dayCount == 1
            ? $"Ajuste de ponto — {days[0].Date:dd/MM/yyyy}"
            : $"Ajuste de ponto — {dayCount} dias";

        var payload = new Dictionary<string, object?>
        {
            ["reason"] = reason,
            ["dayCount"] = dayCount,
            ["days"] = normalizedDays,
            ["attachments"] = savedAttachments.Select(a => new
            {
                a.FileName,
                a.StorageFileName,
                a.ContentType,
                a.SizeBytes,
                a.Url,
            }).ToList(),
        };

        var created = await serviceRequestService.CreateAsync(
            new CreateServiceRequestRequest("servicos-ponto", ServiceCategory.RH, payload),
            cancellationToken);

        var record = new PontoAdjustmentRecord
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Title = title,
            Status = "pending",
            Reason = reason,
            DayCount = dayCount,
            DetailsJson = JsonSerializer.Serialize(
                new
                {
                    reason,
                    days = normalizedDays,
                    attachments = savedAttachments,
                },
                JsonOptions),
            ServiceRequestId = created.Id,
            DataSource = "portal",
            CreatedAt = now,
            UpdatedAt = now,
        };

        payload["recordId"] = record.Id;
        await pontoAdjustmentRepository.AddAsync(record, cancellationToken);

        var protocol = BuildProtocol(record.Id);
        await NotifyCreatedInternalAsync(record, personId, cancellationToken);

        var message = dayCount == 1
            ? $"Solicitação de ajuste registrada. Protocolo: {protocol}. Seu gestor foi notificado."
            : $"Solicitação de ajuste para {dayCount} dias registrada. Protocolo: {protocol}. Seu gestor foi notificado.";

        return new PontoAdjustmentResultDto(
            created.Id,
            record.Id,
            created.Status.ToString(),
            message,
            protocol);
    }

    public async Task<IReadOnlyList<PontoAdjustmentItemDto>> GetMineAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var records = await pontoAdjustmentRepository.ListByPersonAsync(personId, limit, cancellationToken);
        return records.Select(ToItem).ToList();
    }

    public async Task<PontoAdjustmentDetailDto?> GetMineDetailAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var record = await pontoAdjustmentRepository.GetByIdAsync(recordId, cancellationToken);
        if (record is null || record.PersonId != personId)
        {
            return null;
        }

        return await ToDetailAsync(record, management: false, cancellationToken);
    }

    public async Task<IReadOnlyList<PontoAdjustmentManagementItemDto>> GetManagementListAsync(
        string? status,
        string? query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var (canAccess, isRhScope, personId) = await EnsureCanManageAsync(cancellationToken);
        if (!canAccess)
        {
            throw new UnauthorizedAccessException("Acesso à gestão de ponto negado.");
        }

        IReadOnlyList<Guid>? personIds = null;
        if (!isRhScope)
        {
            var reports = await personRepository.GetDirectReportsAsync(personId, cancellationToken);
            personIds = reports.Select(r => r.Id).ToList();
            if (personIds.Count == 0)
            {
                return [];
            }
        }

        var records = await pontoAdjustmentRepository.ListManagementAsync(
            personIds,
            status,
            query,
            limit,
            cancellationToken);

        return records.Select(ToManagementItem).ToList();
    }

    public async Task<PontoAdjustmentManagementDetailDto?> GetManagementDetailAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        var (canAccess, isRhScope, personId) = await EnsureCanManageAsync(cancellationToken);
        if (!canAccess)
        {
            throw new UnauthorizedAccessException("Acesso à gestão de ponto negado.");
        }

        var record = await pontoAdjustmentRepository.GetWithPersonAsync(recordId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (!isRhScope)
        {
            var reports = await personRepository.GetDirectReportsAsync(personId, cancellationToken);
            if (reports.All(r => r.Id != record.PersonId))
            {
                throw new UnauthorizedAccessException("Acesso à solicitação de ponto negado.");
            }
        }

        var detail = await ToDetailAsync(record, management: true, cancellationToken);
        var person = record.Person;
        return new PontoAdjustmentManagementDetailDto(
            detail.Id,
            detail.ServiceRequestId,
            person?.Name ?? "Colaborador",
            person?.EmployeeId,
            person?.Email ?? string.Empty,
            detail.Title,
            detail.Status,
            detail.Reason,
            detail.DayCount,
            record.DataSource,
            detail.CreatedAt,
            detail.Days,
            detail.Timeline,
            InfoNote,
            detail.Attachments);
    }

    public async Task<PontoAttachmentFileDto?> GetManagementAttachmentAsync(
        Guid recordId,
        string storageFileName,
        CancellationToken cancellationToken = default)
    {
        var detail = await GetManagementDetailAsync(recordId, cancellationToken);
        if (detail is null)
        {
            return null;
        }

        var safeName = Path.GetFileName(storageFileName);
        var meta = detail.Attachments.FirstOrDefault(a =>
            string.Equals(a.StorageFileName, safeName, StringComparison.OrdinalIgnoreCase));
        if (meta is null)
        {
            return null;
        }

        var absolutePath = pontoAttachmentStore.ResolveAbsolutePath(meta.StorageFileName);
        if (absolutePath is null)
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(absolutePath, cancellationToken);
        return new PontoAttachmentFileDto(bytes, meta.ContentType, meta.FileName);
    }

    public async Task<PontoAdjustmentManagementDetailDto?> ApproveAsync(
        Guid recordId,
        ApprovePontoAdjustmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var record = await EnsureManagedRecordAsync(recordId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        record.Status = "approved";
        record.RmSyncStatus = "pending_rm_sync";
        record.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            record.DetailsJson = MergeDetailField(record.DetailsJson, "approvalComment", request.Comment!);
        }

        await pontoAdjustmentRepository.UpdateAsync(record, cancellationToken);

        return await GetManagementDetailAsync(recordId, cancellationToken);
    }

    public async Task<PontoAdjustmentManagementDetailDto?> RejectAsync(
        Guid recordId,
        RejectPontoAdjustmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var record = await EnsureManagedRecordAsync(recordId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        record.Status = "rejected";
        record.RmSyncStatus = null;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            record.DetailsJson = MergeDetailField(record.DetailsJson, "rejectionReason", request.Reason!);
        }

        await pontoAdjustmentRepository.UpdateAsync(record, cancellationToken);

        return await GetManagementDetailAsync(recordId, cancellationToken);
    }

    private async Task<PontoAdjustmentRecord?> EnsureManagedRecordAsync(Guid recordId, CancellationToken cancellationToken)
    {
        var (canAccess, isRhScope, personId) = await EnsureCanManageAsync(cancellationToken);
        if (!canAccess)
        {
            throw new UnauthorizedAccessException("Acesso à gestão de ponto negado.");
        }

        var record = await pontoAdjustmentRepository.GetWithPersonAsync(recordId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (!isRhScope)
        {
            var reports = await personRepository.GetDirectReportsAsync(personId, cancellationToken);
            if (reports.All(r => r.Id != record.PersonId))
            {
                throw new UnauthorizedAccessException("Acesso à solicitação de ponto negado.");
            }
        }

        return record;
    }

    private static string MergeDetailField(string? detailsJson, string key, string value)
    {
        try
        {
            var dict = string.IsNullOrWhiteSpace(detailsJson) || detailsJson == "{}"
                ? new Dictionary<string, JsonElement>()
                : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(detailsJson, JsonOptions)
                    ?? new Dictionary<string, JsonElement>();

            var merged = dict.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
            merged[key] = value;
            return JsonSerializer.Serialize(merged, JsonOptions);
        }
        catch
        {
            return detailsJson ?? "{}";
        }
    }

    private async Task NotifyCreatedInternalAsync(
        PontoAdjustmentRecord record,
        Guid requesterPersonId,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipients = await pontoNotifyRecipientResolver.ResolveAsync(requesterPersonId, cancellationToken);
            if (recipients.Count == 0)
            {
                return;
            }

            var requester = await personRepository.GetByIdAsync(requesterPersonId, cancellationToken);
            var dayLabel = record.DayCount == 1 ? "1 dia" : $"{record.DayCount} dias";
            var title = "Nova solicitação de ajuste de ponto";
            var summary = requester is null
                ? $"Nova solicitação de ajuste de ponto ({dayLabel})."
                : $"{requester.Name} solicitou ajuste de ponto ({dayLabel}).";

            await notificationService.NotifyPontoAdjustmentCreatedAsync(
                recipients.Select(r => r.Id).ToList(),
                record.Id,
                summary,
                cancellationToken,
                title);
        }
        catch
        {
            // Notifications are best-effort; request creation must not fail.
        }
    }

    private async Task<(bool CanAccess, bool IsRhScope, Guid PersonId)> EnsureCanManageAsync(
        CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        var (canAccess, isRhScope) = await pontoNotifyRecipientResolver.CanManageAsync(
            personId,
            roles,
            cancellationToken);
        return (canAccess, isRhScope, personId);
    }

    private async Task<IReadOnlyList<PontoAttachmentMetaDto>> SaveAttachmentsAsync(
        IReadOnlyList<PontoAttachmentInput>? attachments,
        CancellationToken cancellationToken)
    {
        var list = attachments ?? [];
        if (list.Count == 0)
        {
            return [];
        }

        if (list.Count > PontoAttachmentLimits.MaxFilesPerRequest)
        {
            throw new ArgumentException(
                $"Máximo de {PontoAttachmentLimits.MaxFilesPerRequest} anexos por solicitação.");
        }

        var saved = new List<PontoAttachmentMetaDto>(list.Count);
        foreach (var attachment in list)
        {
            saved.Add(await pontoAttachmentStore.SaveAsync(
                attachment.Content,
                attachment.FileName,
                attachment.ContentType,
                attachment.SizeBytes,
                cancellationToken));
        }

        return saved;
    }

    private async Task<PontoAdjustmentDetailDto> ToDetailAsync(
        PontoAdjustmentRecord record,
        bool management,
        CancellationToken cancellationToken)
    {
        var timeline = BuildTimeline(record);
        if (record.ServiceRequestId is Guid serviceRequestId)
        {
            var serviceRequest = await serviceRequestService.GetByIdAsync(serviceRequestId, cancellationToken);
            if (serviceRequest is not null)
            {
                timeline = MergeServiceRequestTimeline(timeline, serviceRequest);
            }
        }

        return new PontoAdjustmentDetailDto(
            record.Id,
            record.ServiceRequestId,
            record.Title,
            record.Status,
            record.Reason,
            record.DayCount,
            record.CreatedAt,
            ExtractDays(record.DetailsJson),
            timeline,
            MapAttachmentsForRecord(record.Id, record.DetailsJson, management));
    }

    private static PontoAdjustmentItemDto ToItem(PontoAdjustmentRecord record) =>
        new(
            record.Id,
            record.ServiceRequestId,
            record.Title,
            record.Status,
            record.DayCount,
            record.Reason,
            record.CreatedAt);

    private static PontoAdjustmentManagementItemDto ToManagementItem(PontoAdjustmentRecord record) =>
        new(
            record.Id,
            record.ServiceRequestId,
            record.Person?.Name ?? "Colaborador",
            record.Person?.EmployeeId,
            record.Person?.Email ?? string.Empty,
            record.Title,
            record.Status,
            record.DayCount,
            record.Reason,
            record.CreatedAt);

    private static IReadOnlyList<PontoAdjustmentTimelineEventDto> BuildTimeline(PontoAdjustmentRecord record)
    {
        return
        [
            new(
                "Solicitação enviada",
                "completed",
                record.CreatedAt,
                "Registro criado no portal."),
            new(
                $"Status: {StatusLabel(record.Status)}",
                record.Status,
                record.UpdatedAt,
                null),
        ];
    }

    private static IReadOnlyList<PontoAdjustmentTimelineEventDto> MergeServiceRequestTimeline(
        IReadOnlyList<PontoAdjustmentTimelineEventDto> baseTimeline,
        ServiceRequestDto serviceRequest)
    {
        var merged = baseTimeline.ToList();
        foreach (var item in serviceRequest.Events.OrderBy(evt => evt.CreatedAt))
        {
            merged.Add(new PontoAdjustmentTimelineEventDto(
                item.EventType,
                serviceRequest.Status.ToString().ToLowerInvariant(),
                item.CreatedAt,
                null));
        }

        return merged.OrderBy(evt => evt.OccurredAt).ToList();
    }

    private static IReadOnlyList<PontoAdjustmentDayDetailDto> ExtractDays(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson) || detailsJson == "{}")
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(detailsJson);
            if (!doc.RootElement.TryGetProperty("days", out var daysEl)
                || daysEl.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<PontoAdjustmentDayDetailDto>();
            foreach (var item in daysEl.EnumerateArray())
            {
                var dateText = GetString(item, "date");
                if (!DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    continue;
                }

                result.Add(new PontoAdjustmentDayDetailDto(
                    date,
                    GetString(item, "originalClockIn"),
                    GetString(item, "originalLunchOut"),
                    GetString(item, "originalLunchIn"),
                    GetString(item, "originalClockOut"),
                    GetString(item, "clockIn"),
                    GetString(item, "lunchOut"),
                    GetString(item, "lunchIn"),
                    GetString(item, "clockOut")));
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<PontoAttachmentMetaDto> MapAttachmentsForRecord(
        Guid recordId,
        string? detailsJson,
        bool management)
    {
        var attachments = ExtractAttachments(detailsJson);
        if (attachments.Count == 0)
        {
            return attachments;
        }

        return attachments
            .Select(a =>
            {
                var url = management
                    ? $"/rh/ponto/adjustments/management/{recordId:D}/attachments/{a.StorageFileName}"
                    : a.Url;
                return new PontoAttachmentMetaDto(
                    a.FileName,
                    a.StorageFileName,
                    a.ContentType,
                    a.SizeBytes,
                    url);
            })
            .ToList();
    }

    private static IReadOnlyList<PontoAttachmentMetaDto> ExtractAttachments(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson) || detailsJson == "{}")
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(detailsJson);
            if (!doc.RootElement.TryGetProperty("attachments", out var attachmentsEl)
                || attachmentsEl.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<PontoAttachmentMetaDto>();
            foreach (var item in attachmentsEl.EnumerateArray())
            {
                var fileName = GetString(item, "fileName");
                var storageFileName = GetString(item, "storageFileName");
                if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(storageFileName))
                {
                    continue;
                }

                var size = item.TryGetProperty("sizeBytes", out var sizeEl) && sizeEl.TryGetInt64(out var sizeBytes)
                    ? sizeBytes
                    : 0L;

                result.Add(new PontoAttachmentMetaDto(
                    fileName,
                    storageFileName,
                    GetString(item, "contentType"),
                    size,
                    GetString(item, "url")));
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            // Support PascalCase from older serializers
            var pascal = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
            if (!element.TryGetProperty(pascal, out value))
            {
                return string.Empty;
            }
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
    }

    private static void ValidateTime(string? value, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value) || !TimeRegex.IsMatch(value.Trim()))
        {
            throw new ArgumentException($"Informe o horário de {fieldLabel} no formato HH:mm.");
        }
    }

    private static string StatusLabel(string status) =>
        status.ToLowerInvariant() switch
        {
            "pending" => "Pendente",
            "approved" => "Aprovado",
            "rejected" => "Rejeitado",
            "completed" => "Concluído",
            _ => status,
        };

    private static string BuildProtocol(Guid recordId) =>
        $"PT-{recordId.ToString("N")[..8].ToUpperInvariant()}";
}
