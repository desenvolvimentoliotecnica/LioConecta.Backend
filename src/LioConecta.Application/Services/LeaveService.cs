using System.Globalization;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class LeaveService(
    ILeaveRepository leaveRepository,
    ILeaveSyncService leaveSyncService,
    IServiceRequestService serviceRequestService,
    ICurrentUserService currentUserService,
    IAppSettingsProvider settingsProvider,
    ITotvsRmConfigurationService totvsRmConfigurationService,
    IPersonRepository personRepository,
    LeaveNotifyRecipientResolver leaveNotifyRecipientResolver,
    INotificationService notificationService,
    ILeaveEmailNotifier leaveEmailNotifier,
    ILeaveAttachmentStore leaveAttachmentStore) : ILeaveService
{
    private const string VacationServiceKey = "solicitar-ferias";
    private const string MedicalCertificateServiceKey = "atestado";
    private const string ApprovalNote =
        "A aprovação formal da solicitação é feita no RM Labore. O portal apenas registra, notifica e espelha o status.";
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly IReadOnlyList<LeaveServiceDto> ServiceCatalog =
    [
        new("solicitar-ferias", "Solicitar Férias", "Informe período desejado, dias a usufruir e substituto. A solicitação segue para aprovação do gestor e registro no RH.", "ferias", "Até 3 dias úteis", true, true, "Solicitar", "Informe início, fim e substituto com pelo menos 30 dias de antecedência quando possível. O gestor recebe notificação automática.", null),
        new("saldo-ferias", "Consultar Saldo de Férias", "Visualize dias adquiridos, disponíveis, programados e vencidos conforme seu vínculo e período aquisitivo.", "ferias", "Imediato", true, false, "Consultar", "Saldo consolidado por período aquisitivo. Dias programados já descontam do disponível após aprovação.", null),
        new("abono", "Abono Pecuniário", "Converta até 10 dias de férias em pagamento conforme política vigente e acordo com o gestor.", "ferias", "Até 5 dias úteis", true, false, "Solicitar", "Limite legal de 10 dias. Solicitação vinculada ao mesmo período de férias.", null),
        new("lic-maternidade", "Licença Maternidade", "Registro de afastamento por nascimento ou adoção, com envio de documentos e acompanhamento pelo RH.", "licenca", "Até 2 dias úteis", false, false, "Solicitar", "Serviço offline: encaminhe documentos ao RH. Prazo de análise de até 2 dias úteis.", null),
        new("lic-paternidade", "Licença Paternidade", "Solicitação de licença paternidade conforme legislação e política interna de parentalidade.", "licenca", "Até 2 dias úteis", true, false, "Solicitar", "20 dias corridos conforme CLT. Anexe certidão de nascimento quando disponível.", null),
        new("lic-gala", "Licença Gala / Nojo", "Afastamento por casamento ou falecimento de familiar, com comprovação documental quando aplicável.", "licenca", "Até 1 dia útil", true, false, "Solicitar", "Gala: 3 dias consecutivos. Nojo: 2 dias por ocorrência.", null),
        new("atestado", "Registrar Atestado Médico", "Envie atestados para justificar ausências por motivo de saúde e alimentar o controle de ponto.", "afastamento", "Até 24 horas", true, false, "Registrar", "Envie em até 24h após o retorno. Atestados superiores a 15 dias podem exigir perícia.", null),
        new("afast-inss", "Afastamento INSS", "Acompanhamento de afastamentos previdenciários, documentação e retorno ao trabalho.", "afastamento", "Conforme INSS", false, false, "Consultar", "Consulte status no portal Meu INSS. RH acompanha retorno e exame de aptidão.", null),
        new("falta-justificada", "Falta Justificada", "Registre ausências pontuais com motivo e anexos, sujeitas à validação do gestor.", "afastamento", "Até 2 dias úteis", true, false, "Registrar", "Motivos aceitos: serviço militar, doação de sangue, alistamento, entre outros previstos em política.", null),
        new("banco-horas", "Banco de Horas", "Consulte saldo, créditos, débitos e solicite compensação de horas extras acumuladas.", "banco", "Imediato", true, false, "Consultar", "Saldo positivo pode ser compensado em saídas antecipadas ou folgas, conforme acordo coletivo.", null),
        new("historico", "Histórico de Ausências", "Linha do tempo com férias gozadas, licenças, atestados e demais registros dos últimos 24 meses.", "consulta", "Imediato", true, false, "Consultar", "Inclui solicitações pendentes, aprovadas e concluídas.", null),
        new("calendario-equipe", "Calendário da Equipe", "Visualize férias e ausências aprovadas dos colegas do seu time para facilitar o planejamento.", "consulta", "Imediato", true, false, "Abrir", "Exibe ausências aprovadas do time direto. Dados atualizados diariamente.", null),
    ];

    public async Task<LeaveSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await EnsureSyncedIfStaleAsync(personId, cancellationToken);

        var balance = await leaveRepository.GetBalanceAsync(personId, cancellationToken);
        var pending = await leaveRepository.CountPendingAsync(personId, cancellationToken);

        if (balance is null)
        {
            return new LeaveSummaryDto(0, pending, null);
        }

        return new LeaveSummaryDto(
            balance.AvailableDays,
            pending,
            FormatScheduledLabel(balance.NextScheduledStart));
    }

    public IReadOnlyList<LeaveServiceDto> GetServices() =>
        ServiceCatalog.Select(service => service with { PortalUrl = ResolvePortalUrl(service.Id) }).ToList();

    public async Task<LeaveBalanceDto> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await EnsureSyncedIfStaleAsync(personId, cancellationToken);

        var balance = await leaveRepository.GetBalanceAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Saldo de férias não encontrado.");

        var breakdown = DeserializeBreakdown(balance.BreakdownJson);
        return new LeaveBalanceDto(
            balance.AvailableDays,
            balance.AcquiredDays,
            balance.ScheduledDays,
            balance.ExpiredDays,
            breakdown.Periods,
            breakdown.Notes);
    }

    public async Task<IReadOnlyList<LeaveHistoryItemDto>> GetHistoryAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await EnsureSyncedIfStaleAsync(personId, cancellationToken);

        var records = await leaveRepository.ListRecordsAsync(personId, limit, cancellationToken);
        return records.Select(ToHistoryItem).ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestItemDto>> GetRequestsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await EnsureSyncedIfStaleAsync(personId, cancellationToken);

        var records = await leaveRepository.ListRequestsAsync(personId, limit, cancellationToken);
        return records.Select(ToRequestItem).ToList();
    }

    public async Task<LeaveRequestDetailDto?> GetRequestDetailAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var record = await leaveRepository.GetRecordByIdAsync(recordId, cancellationToken);

        if (record is null
            || record.PersonId != personId
            || (record.ServiceKey != VacationServiceKey
                && record.ServiceKey != MedicalCertificateServiceKey))
        {
            return null;
        }

        var notes = ExtractNotes(record.DetailsJson);
        var timeline = BuildTimeline(record);

        if (record.ServiceRequestId is Guid serviceRequestId)
        {
            var serviceRequest = await serviceRequestService.GetByIdAsync(serviceRequestId, cancellationToken);
            if (serviceRequest is not null)
            {
                timeline = MergeServiceRequestTimeline(timeline, serviceRequest);
            }
        }

        return new LeaveRequestDetailDto(
            record.Id,
            record.ServiceRequestId,
            record.Title,
            record.Status,
            record.RmSyncStatus,
            record.StartDate,
            record.EndDate,
            record.Days,
            notes,
            record.DataSource,
            record.CreatedAt,
            timeline,
            MapAttachmentsForRecord(record.Id, record.DetailsJson, management: false));
    }

    public async Task<LeaveBancoHorasDto> GetBancoHorasAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var balance = await leaveRepository.GetBalanceAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Saldo não encontrado.");

        var entries = new List<LeaveBancoHorasEntryDto>
        {
            new("Jun/2026", "Horas extras — projeto Q3", 4.5m, "credito"),
            new("Mai/2026", "Compensação saída antecipada", -8m, "debito"),
            new("Abr/2026", "Plantão suporte", 6m, "credito"),
            new("Mar/2026", "Treinamento externo", -2m, "debito"),
            new("Fev/2026", "Horas extras — inventário", 12m, "credito"),
        };

        return new LeaveBancoHorasDto(balance.BancoHorasBalanceHours, entries);
    }

    public Task<LeaveTeamCalendarDto> GetTeamCalendarAsync(CancellationToken cancellationToken = default)
    {
        var members = new List<LeaveTeamMemberDto>
        {
            new("Maria Silva", "Analista RH", "Férias", new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 24)),
            new("Ricardo Souza", "Gestor", "Férias", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 15)),
            new("Julia Santos", "Analista", "Licença", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5)),
            new("Carlos Mendes", "Desenvolvedor", "Férias", new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 20)),
        };

        return Task.FromResult(new LeaveTeamCalendarDto(members));
    }

    public async Task<LeaveRequestResultDto> CreateRequestAsync(
        CreateLeaveRequestDto request,
        IReadOnlyList<LeaveAttachmentInput>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await EnsureSyncedIfStaleAsync(personId, cancellationToken);

        var service = ServiceCatalog.FirstOrDefault(item =>
            string.Equals(item.Id, request.ServiceId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Serviço não encontrado.");

        if (!service.Online)
        {
            throw new InvalidOperationException("Este serviço requer encaminhamento offline ao RH.");
        }

        var isMedicalCertificate = string.Equals(
            service.Id,
            MedicalCertificateServiceKey,
            StringComparison.OrdinalIgnoreCase);
        var isVacation = string.Equals(service.Id, VacationServiceKey, StringComparison.OrdinalIgnoreCase);

        DateOnly? startDate = request.StartDate;
        DateOnly? endDate = request.EndDate;
        int? days = request.Days;

        if (isVacation)
        {
            if (startDate is null || endDate is null)
            {
                throw new ArgumentException("Informe data início e fim para solicitar férias.");
            }

            LeaveDateRules.ValidateVacationPeriod(startDate.Value, endDate.Value);
            days ??= LeaveDateRules.CountInclusiveDays(startDate.Value, endDate.Value);

            var balance = await leaveRepository.GetBalanceAsync(personId, cancellationToken);
            var available = balance?.AvailableDays ?? 0;

            if (available <= 0 || days.Value > available)
            {
                throw new LeaveInsufficientBalanceException(days.Value, available);
            }
        }

        var savedAttachments = await SaveAttachmentsAsync(
            attachments,
            requireAtLeastOne: isMedicalCertificate,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var payload = new Dictionary<string, object?>
        {
            ["serviceId"] = request.ServiceId,
            ["startDate"] = startDate?.ToString("O"),
            ["endDate"] = endDate?.ToString("O"),
            ["days"] = days,
            ["notes"] = request.Notes,
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
            new CreateServiceRequestRequest("servicos-ferias", ServiceCategory.RH, payload),
            cancellationToken);

        var record = new LeaveRecord
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ServiceKey = service.Id,
            RecordType = service.Category,
            Title = $"{service.Title} — solicitação",
            Status = "pending",
            StartDate = startDate,
            EndDate = endDate,
            Days = days,
            DetailsJson = JsonSerializer.Serialize(
                new
                {
                    notes = request.Notes,
                    attachments = savedAttachments,
                },
                JsonOptions),
            ServiceRequestId = created.Id,
            RmSyncStatus = isVacation ? "pending_rm_sync" : null,
            DataSource = "portal",
            CreatedAt = now,
            UpdatedAt = now,
        };

        payload["recordId"] = record.Id;
        await leaveRepository.AddRecordAsync(record, cancellationToken);

        var protocol = BuildProtocol(record.Id);

        if (isVacation || isMedicalCertificate)
        {
            await NotifyLeaveRequestCreatedInternalAsync(record, personId, service.Title, cancellationToken);
        }

        var message = isMedicalCertificate
            ? $"Atestado médico enviado com sucesso. Protocolo: {protocol}. O RH foi notificado."
            : $"Solicitação registrada com sucesso. Protocolo: {protocol}. Acompanhe o andamento na página de Férias e ausências.";

        return new LeaveRequestResultDto(
            created.Id,
            record.Id,
            created.Status.ToString(),
            message,
            protocol);
    }

    private async Task<IReadOnlyList<LeaveAttachmentMetaDto>> SaveAttachmentsAsync(
        IReadOnlyList<LeaveAttachmentInput>? attachments,
        bool requireAtLeastOne,
        CancellationToken cancellationToken)
    {
        var list = attachments ?? [];
        if (list.Count == 0)
        {
            if (requireAtLeastOne)
            {
                throw new ArgumentException("Anexe o atestado em PDF ou PNG para enviar a solicitação.");
            }

            return [];
        }

        if (list.Count > LeaveAttachmentLimits.MaxFilesPerRequest)
        {
            throw new ArgumentException(
                $"Máximo de {LeaveAttachmentLimits.MaxFilesPerRequest} anexos por solicitação.");
        }

        var saved = new List<LeaveAttachmentMetaDto>(list.Count);
        foreach (var attachment in list)
        {
            saved.Add(await leaveAttachmentStore.SaveAsync(
                attachment.Content,
                attachment.FileName,
                attachment.ContentType,
                attachment.SizeBytes,
                cancellationToken));
        }

        return saved;
    }

    private static string BuildProtocol(Guid recordId) =>
        $"LC-{recordId.ToString("N")[..8].ToUpperInvariant()}";

    public async Task<IReadOnlyList<LeaveManagementItemDto>> GetManagementListAsync(
        string? status,
        string? query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var (canAccess, isRhScope, personId) = await EnsureCanManageAsync(cancellationToken);
        if (!canAccess)
        {
            throw new UnauthorizedAccessException("Acesso à gestão de férias negado.");
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

        var records = await leaveRepository.ListManagementAsync(
            personIds,
            status,
            query,
            limit,
            cancellationToken);

        return records.Select(ToManagementItem).ToList();
    }

    public async Task<LeaveManagementDetailDto?> GetManagementDetailAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        var (canAccess, isRhScope, personId) = await EnsureCanManageAsync(cancellationToken);
        if (!canAccess)
        {
            throw new UnauthorizedAccessException("Acesso à gestão de férias negado.");
        }

        var record = await leaveRepository.GetRecordWithPersonAsync(recordId, cancellationToken);
        if (record is null
            || (record.ServiceKey != VacationServiceKey
                && record.ServiceKey != MedicalCertificateServiceKey))
        {
            return null;
        }

        if (!isRhScope)
        {
            var reports = await personRepository.GetDirectReportsAsync(personId, cancellationToken);
            if (reports.All(r => r.Id != record.PersonId))
            {
                throw new UnauthorizedAccessException("Acesso à solicitação de férias negado.");
            }
        }

        return await ToManagementDetailAsync(record, cancellationToken);
    }

    public async Task<LeaveAttachmentFileDto?> GetManagementAttachmentAsync(
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

        var absolutePath = leaveAttachmentStore.ResolveAbsolutePath(meta.StorageFileName);
        if (absolutePath is null)
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(absolutePath, cancellationToken);
        return new LeaveAttachmentFileDto(bytes, meta.ContentType, meta.FileName);
    }

    public async Task<byte[]?> GetRequestPdfAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var record = await leaveRepository.GetRecordWithPersonAsync(recordId, cancellationToken);
        if (record is null || record.PersonId != personId || record.ServiceKey != VacationServiceKey)
        {
            return null;
        }

        return BuildPdf(record);
    }

    public async Task<byte[]?> GetManagementPdfAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        var detail = await GetManagementDetailAsync(recordId, cancellationToken);
        if (detail is null)
        {
            return null;
        }

        var record = await leaveRepository.GetRecordWithPersonAsync(recordId, cancellationToken);
        return record is null ? null : BuildPdf(record);
    }

    private async Task NotifyLeaveRequestCreatedInternalAsync(
        LeaveRecord record,
        Guid requesterPersonId,
        string serviceTitle,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipients = await leaveNotifyRecipientResolver.ResolveAsync(requesterPersonId, cancellationToken);
            if (recipients.Count == 0)
            {
                return;
            }

            var requester = await personRepository.GetByIdAsync(requesterPersonId, cancellationToken);
            var isMedical = string.Equals(
                record.ServiceKey,
                MedicalCertificateServiceKey,
                StringComparison.OrdinalIgnoreCase);
            var period = FormatPeriodLabel(record.StartDate, record.EndDate);
            var title = isMedical
                ? "Novo atestado médico"
                : "Nova solicitação de férias";
            var summary = requester is null
                ? (isMedical
                    ? $"Novo atestado médico registrado ({period})."
                    : $"Nova solicitação de férias ({period}).")
                : (isMedical
                    ? $"{requester.Name} enviou atestado médico ({period})."
                    : $"{requester.Name} solicitou férias ({period}).");

            await notificationService.NotifyLeaveRequestCreatedAsync(
                recipients.Select(r => r.Id).ToList(),
                record.Id,
                summary,
                cancellationToken,
                title);

            if (requester is not null)
            {
                await leaveEmailNotifier.NotifyRequestCreatedAsync(
                    record,
                    requester,
                    recipients,
                    serviceTitle,
                    cancellationToken);
            }
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
        var (canAccess, isRhScope) = await leaveNotifyRecipientResolver.CanManageAsync(
            personId,
            roles,
            cancellationToken);
        return (canAccess, isRhScope, personId);
    }

    private async Task<LeaveManagementDetailDto> ToManagementDetailAsync(
        LeaveRecord record,
        CancellationToken cancellationToken)
    {
        var notes = ExtractNotes(record.DetailsJson);
        var timeline = BuildTimeline(record);

        if (record.ServiceRequestId is Guid serviceRequestId)
        {
            var serviceRequest = await serviceRequestService.GetByIdAsync(serviceRequestId, cancellationToken);
            if (serviceRequest is not null)
            {
                timeline = MergeServiceRequestTimeline(timeline, serviceRequest);
            }
        }

        var person = record.Person;
        return new LeaveManagementDetailDto(
            record.Id,
            record.ServiceRequestId,
            person?.Name ?? "Colaborador",
            person?.EmployeeId,
            person?.Email ?? string.Empty,
            record.Title,
            record.Status,
            record.RmSyncStatus,
            record.RmExternalId,
            record.StartDate,
            record.EndDate,
            record.Days,
            notes,
            record.DataSource,
            record.CreatedAt,
            timeline,
            ApprovalNote,
            MapAttachmentsForRecord(record.Id, record.DetailsJson, management: true));
    }

    private static LeaveManagementItemDto ToManagementItem(LeaveRecord record) =>
        new(
            record.Id,
            record.ServiceRequestId,
            record.Person?.Name ?? "Colaborador",
            record.Person?.EmployeeId,
            record.Person?.Email ?? string.Empty,
            record.Title,
            record.Status,
            record.RmSyncStatus,
            record.StartDate,
            record.EndDate,
            record.Days,
            record.DataSource,
            record.CreatedAt);

    private static byte[] BuildPdf(LeaveRecord record)
    {
        var person = record.Person;
        var period = FormatPeriodLabel(record.StartDate, record.EndDate);
        var model = new LeaveRequestPdfModel(
            person?.Name ?? "Colaborador",
            person?.EmployeeId ?? "—",
            person?.Email ?? "—",
            period,
            record.Days?.ToString(PtBr) ?? "—",
            record.Status,
            record.RmSyncStatus ?? "—",
            record.Id.ToString(),
            record.RmExternalId ?? "—",
            LeaveRequestPdfGenerator.FormatDateTime(record.CreatedAt),
            ExtractNotes(record.DetailsJson));

        return LeaveRequestPdfGenerator.Generate(model);
    }

    private static string FormatPeriodLabel(DateOnly? start, DateOnly? end)
    {
        if (start is null)
        {
            return "sem período";
        }

        var startLabel = start.Value.ToString("dd/MM/yyyy", PtBr);
        if (end is null)
        {
            return startLabel;
        }

        return $"{startLabel} – {end.Value.ToString("dd/MM/yyyy", PtBr)}";
    }

    private async Task EnsureSyncedIfStaleAsync(Guid personId, CancellationToken cancellationToken)
    {
        var ttlMinutes = settingsProvider.GetInt(AppSettingKeys.WorkersTotvsLeaveCacheTtlMinutes, 1440);
        var syncedAt = await leaveRepository.GetBalanceSyncedAtAsync(personId, cancellationToken);

        if (syncedAt is not null && syncedAt.Value.AddMinutes(ttlMinutes) >= DateTimeOffset.UtcNow)
        {
            return;
        }

        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled)
        {
            return;
        }

        try
        {
            await leaveSyncService.SyncPersonAsync(personId, cancellationToken);
        }
        catch
        {
            // Best-effort on-demand sync; cached seed/postgres data remains available.
        }
    }

    private static LeaveRequestItemDto ToRequestItem(LeaveRecord record) =>
        new(
            record.Id,
            record.ServiceRequestId,
            record.Title,
            record.Status,
            record.RmSyncStatus,
            record.StartDate,
            record.EndDate,
            record.Days,
            record.DataSource,
            record.CreatedAt);

    private static IReadOnlyList<LeaveTimelineEventDto> BuildTimeline(LeaveRecord record)
    {
        var events = new List<LeaveTimelineEventDto>
        {
            new(
                "Solicitação enviada",
                "completed",
                record.CreatedAt,
                "Registro criado no portal."),
        };

        if (record.RmSyncStatus is "pending_rm_sync")
        {
            events.Add(new LeaveTimelineEventDto(
                "Envio ao RM Labore",
                "pending",
                record.UpdatedAt,
                "Aguardando integração com o RM."));
        }
        else if (record.RmSyncStatus is "synced")
        {
            events.Add(new LeaveTimelineEventDto(
                "Registrado no RM",
                "completed",
                record.SyncedAt ?? record.UpdatedAt,
                "Período sincronizado com o RM."));
        }
        else if (record.RmSyncStatus is "failed")
        {
            events.Add(new LeaveTimelineEventDto(
                "Falha no envio ao RM",
                "rejected",
                record.UpdatedAt,
                "Nova tentativa será feita automaticamente."));
        }

        var statusLabel = LeaveStatusNormalizer.Label(record.Status);
        events.Add(new LeaveTimelineEventDto(
            $"Status: {statusLabel}",
            record.Status,
            record.UpdatedAt,
            null));

        return events;
    }

    private static IReadOnlyList<LeaveTimelineEventDto> MergeServiceRequestTimeline(
        IReadOnlyList<LeaveTimelineEventDto> baseTimeline,
        ServiceRequestDto serviceRequest)
    {
        var merged = baseTimeline.ToList();
        foreach (var item in serviceRequest.Events.OrderBy(evt => evt.CreatedAt))
        {
            merged.Add(new LeaveTimelineEventDto(
                item.EventType,
                serviceRequest.Status.ToString().ToLowerInvariant(),
                item.CreatedAt,
                null));
        }

        return merged.OrderBy(evt => evt.OccurredAt).ToList();
    }

    private string? ResolvePortalUrl(string serviceKey)
    {
        var settingKey = LeavePortalSettingCatalog.SettingKey(serviceKey);
        if (settingsProvider.TryGetString(settingKey, out var configured))
        {
            return string.IsNullOrWhiteSpace(configured) ? null : configured.Trim();
        }

        return null;
    }

    private static LeaveHistoryItemDto ToHistoryItem(LeaveRecord record) =>
        new(
            record.Id,
            record.Title,
            record.RecordType,
            record.Status,
            record.StartDate,
            record.EndDate,
            record.Days,
            ExtractNotes(record.DetailsJson));

    private static string? ExtractNotes(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson) || detailsJson == "{}")
        {
            return null;
        }

        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(detailsJson, JsonOptions);
            if (raw?.TryGetValue("notes", out var notesEl) == true)
            {
                return notesEl.GetString();
            }

            if (raw?.TryGetValue("note", out var noteEl) == true)
            {
                return noteEl.GetString();
            }
        }
        catch
        {
            // ignore malformed json
        }

        return null;
    }

    private static IReadOnlyList<LeaveAttachmentMetaDto> MapAttachmentsForRecord(
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
                    ? $"/rh/leave/management/{recordId:D}/attachments/{a.StorageFileName}"
                    : a.Url;
                return new LeaveAttachmentMetaDto(
                    a.FileName,
                    a.StorageFileName,
                    a.ContentType,
                    a.SizeBytes,
                    url);
            })
            .ToList();
    }

    private static IReadOnlyList<LeaveAttachmentMetaDto> ExtractAttachments(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson) || detailsJson == "{}")
        {
            return [];
        }

        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(detailsJson, JsonOptions);
            if (raw?.TryGetValue("attachments", out var attachmentsEl) != true
                || attachmentsEl.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var list = new List<LeaveAttachmentMetaDto>();
            foreach (var item in attachmentsEl.EnumerateArray())
            {
                var fileName = item.TryGetProperty("fileName", out var fnEl)
                    ? fnEl.GetString()
                    : item.TryGetProperty("FileName", out var fnEl2) ? fnEl2.GetString() : null;
                var storageFileName = item.TryGetProperty("storageFileName", out var snEl)
                    ? snEl.GetString()
                    : item.TryGetProperty("StorageFileName", out var snEl2) ? snEl2.GetString() : null;
                var contentType = item.TryGetProperty("contentType", out var ctEl)
                    ? ctEl.GetString()
                    : item.TryGetProperty("ContentType", out var ctEl2) ? ctEl2.GetString() : null;
                var url = item.TryGetProperty("url", out var urlEl)
                    ? urlEl.GetString()
                    : item.TryGetProperty("Url", out var urlEl2) ? urlEl2.GetString() : null;
                long sizeBytes = 0;
                if (item.TryGetProperty("sizeBytes", out var szEl) && szEl.TryGetInt64(out var sz))
                {
                    sizeBytes = sz;
                }
                else if (item.TryGetProperty("SizeBytes", out var szEl2) && szEl2.TryGetInt64(out var sz2))
                {
                    sizeBytes = sz2;
                }

                if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(storageFileName))
                {
                    continue;
                }

                list.Add(new LeaveAttachmentMetaDto(
                    fileName.Trim(),
                    Path.GetFileName(storageFileName.Trim()),
                    string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
                    sizeBytes,
                    string.IsNullOrWhiteSpace(url)
                        ? $"/leave/attachments/{Path.GetFileName(storageFileName.Trim())}"
                        : url.Trim()));
            }

            return list;
        }
        catch
        {
            return [];
        }
    }

    private static (IReadOnlyList<LeavePeriodDto> Periods, IReadOnlyList<string> Notes) DeserializeBreakdown(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ([], []);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var periods = new List<LeavePeriodDto>();

            if (root.TryGetProperty("periods", out var periodsEl) && periodsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in periodsEl.EnumerateArray())
                {
                    DateOnly? expires = null;
                    if (item.TryGetProperty("expiresAt", out var expEl) &&
                        expEl.ValueKind == JsonValueKind.String &&
                        DateOnly.TryParse(expEl.GetString(), out var parsed))
                    {
                        expires = parsed;
                    }

                    periods.Add(new LeavePeriodDto(
                        item.GetProperty("label").GetString() ?? "—",
                        item.GetProperty("acquiredDays").GetInt32(),
                        item.GetProperty("usedDays").GetInt32(),
                        item.GetProperty("availableDays").GetInt32(),
                        expires));
                }
            }

            var notes = new List<string>();
            if (root.TryGetProperty("notes", out var notesEl) && notesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var note in notesEl.EnumerateArray())
                {
                    var text = note.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        notes.Add(text);
                    }
                }
            }

            return (periods, notes);
        }
        catch
        {
            return ([], []);
        }
    }

    private static string? FormatScheduledLabel(DateOnly? start)
    {
        if (start is null)
        {
            return null;
        }

        var month = PtBr.DateTimeFormat.GetAbbreviatedMonthName(start.Value.Month);
        month = char.ToUpper(month[0]) + month[1..];
        return $"{month}/{start.Value.Year}";
    }
}
