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
    IServiceRequestService serviceRequestService,
    ICurrentUserService currentUserService,
    IAppSettingsProvider settingsProvider) : ILeaveService
{
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
        ServiceCatalog.Select(s => s with { PortalUrl = ResolvePortalUrl(s.Id) }).ToList();

    public async Task<LeaveBalanceDto> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
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
        var records = await leaveRepository.ListRecordsAsync(personId, limit, cancellationToken);
        return records.Select(ToHistoryItem).ToList();
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
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var service = ServiceCatalog.FirstOrDefault(s =>
            string.Equals(s.Id, request.ServiceId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Serviço não encontrado.");

        if (!service.Online)
        {
            throw new InvalidOperationException("Este serviço requer encaminhamento offline ao RH.");
        }

        var now = DateTimeOffset.UtcNow;
        var record = new LeaveRecord
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ServiceKey = service.Id,
            RecordType = service.Category,
            Title = $"{service.Title} — solicitação",
            Status = "pending",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Days = request.Days,
            DetailsJson = JsonSerializer.Serialize(new { notes = request.Notes }, JsonOptions),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await leaveRepository.AddRecordAsync(record, cancellationToken);

        var payload = new Dictionary<string, object?>
        {
            ["serviceId"] = request.ServiceId,
            ["startDate"] = request.StartDate?.ToString("O"),
            ["endDate"] = request.EndDate?.ToString("O"),
            ["days"] = request.Days,
            ["notes"] = request.Notes,
            ["recordId"] = record.Id,
        };

        var created = await serviceRequestService.CreateAsync(
            new CreateServiceRequestRequest("servicos-ferias", ServiceCategory.RH, payload),
            cancellationToken);

        return new LeaveRequestResultDto(
            created.Id,
            record.Id,
            created.Status.ToString(),
            "Solicitação registrada com sucesso. Acompanhe o andamento na página de Férias e ausências.");
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

    private static LeaveHistoryItemDto ToHistoryItem(LeaveRecord record)
    {
        string? note = null;
        if (!string.IsNullOrWhiteSpace(record.DetailsJson) && record.DetailsJson != "{}")
        {
            try
            {
                var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(record.DetailsJson, JsonOptions);
                if (raw?.TryGetValue("note", out var noteEl) == true)
                {
                    note = noteEl.GetString();
                }
            }
            catch
            {
                // ignore malformed json
            }
        }

        return new LeaveHistoryItemDto(
            record.Id,
            record.Title,
            record.RecordType,
            record.Status,
            record.StartDate,
            record.EndDate,
            record.Days,
            note);
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
