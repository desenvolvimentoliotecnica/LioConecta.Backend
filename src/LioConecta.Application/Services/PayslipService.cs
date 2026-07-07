using System.Globalization;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class PayslipService(
    IPayslipRepository payslipRepository,
    IServiceRequestService serviceRequestService,
    ICurrentUserService currentUserService,
    IPersonRepository personRepository,
    ITotvsRmEmployeeRepository employeeRepository,
    ITotvsRmConfigurationService totvsRmConfigurationService,
    IPayslipSyncService payslipSyncService,
    IAppSettingsProvider settings,
    PayslipPdfBuilder payslipPdfBuilder) : IPayslipService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly IReadOnlyList<PayslipServiceDto> ServiceCatalog =
    [
        new("visualizar", "Visualizar Contracheque", "Acesse o holerite da última competência com proventos, descontos e valor líquido.", "holerite", "Imediato", true, true, "Visualizar", "Abre o holerite mais recente disponível. Os valores ficam confidenciais até você optar por exibi-los na barra de ferramentas."),
        new("download-pdf", "Download em PDF", "Baixe o contracheque do mês selecionado em PDF para arquivamento pessoal ou comprovação.", "holerite", "Imediato", true, false, "Baixar", "Gera um arquivo PDF do holerite da competência mais recente. O download é registrado para auditoria interna."),
        new("historico", "Histórico de Holerites", "Consulte contracheques dos últimos 12 meses com busca por competência e tipo de pagamento.", "historico", "Imediato", true, false, "Consultar", "Lista as competências disponíveis sincronizadas do TOTVS RM. Selecione uma linha para visualizar o demonstrativo completo."),
        new("comparativo", "Comparativo Salarial", "Compare proventos, descontos e líquido entre dois meses para entender variações na remuneração.", "consulta", "Imediato", true, false, "Consultar", "Compara automaticamente os dois últimos holerites. Útil para identificar diferenças de horas extras ou descontos."),
        new("demonstrativo", "Demonstrativo Detalhado", "Visualize rubricas, bases de cálculo, horas extras, adicionais e descontos linha a linha.", "holerite", "Imediato", true, false, "Visualizar", "Exibe todas as rubricas de proventos e descontos do holerite mais recente, com códigos e valores."),
        new("informe-rendimentos", "Informe de Rendimentos", "Emita o informe anual para declaração de Imposto de Renda com valores pagos e retidos.", "informe", "Imediato", true, false, "Emitir", "Disponível após fechamento anual. Contém totais pagos e impostos retidos mês a mês."),
        new("comprovante", "Comprovante de Rendimentos", "Documento simplificado para comprovação de renda em processos internos ou externos.", "informe", "Imediato", true, false, "Emitir", "Gera PDF imediato com remuneração da última competência disponível."),
        new("carta-consignacao", "Carta de Consignação", "Consulte margem consignável e emita carta para empréstimos e convênios autorizados.", "documento", "Imediato", true, false, "Emitir", "Gera PDF com margem consignável estimada (35% do líquido) da última competência."),
        new("fgts", "FGTS e Encargos", "Resumo de depósitos de FGTS, INSS e demais encargos vinculados ao seu contrato.", "consulta", "Imediato", true, false, "Consultar", "Mostra depósitos mensais de FGTS integrados ao TOTVS RM (PFPERFF)."),
        new("descontos", "Descontos em Folha", "Detalhamento de plano de saúde, vale-transporte, empréstimos consignados e outros descontos.", "consulta", "Imediato", true, false, "Consultar", "Consolida descontos recorrentes do holerite mais recente com código e competência."),
        new("duvidas-rubricas", "Dúvidas sobre Rubricas", "Orientações sobre códigos, siglas e regras de cálculo aplicadas na sua folha de pagamento.", "consulta", "Imediato", true, false, "Consultar", "Glossário dos códigos de rubricas mais frequentes na sua folha, com descrição amigável.")
    ];

    public async Task<PayslipSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var syncContext = await TriggerSyncIfStaleAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var admissionDate = await ResolveAdmissionDateAsync(personId, cancellationToken);
        var hiredYear = admissionDate?.Year;
        var informeYear = ResolveInformeYear(hiredYear);
        var eligible = await ListEligiblePayslipsAsync(personId, null, null, 100, cancellationToken);
        var latest = eligible.FirstOrDefault();

        if (latest is null)
        {
            return new PayslipSummaryDto(
                "—",
                0m,
                0,
                syncContext.AvailabilityStatus,
                syncContext.UserMessage,
                syncContext.DataSource,
                syncContext.SyncedAt,
                hiredYear,
                informeYear);
        }

        return new PayslipSummaryDto(
            FormatCompetence(latest.Year, latest.Month),
            latest.NetAmount,
            eligible.Count,
            syncContext.AvailabilityStatus,
            syncContext.UserMessage,
            syncContext.DataSource,
            syncContext.SyncedAt,
            hiredYear,
            informeYear);
    }

    public IReadOnlyList<PayslipServiceDto> GetServices() => ServiceCatalog;

    public async Task<IReadOnlyList<PayslipListItemDto>> ListAsync(
        int? year,
        int? month,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await TriggerSyncIfStaleAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var payslips = await ListEligiblePayslipsAsync(personId, year, month, limit, cancellationToken);
        return payslips.Select(ToListItem).ToList();
    }

    public async Task<PayslipDetailDto?> GetDetailAsync(
        int year,
        int month,
        string? paymentType = null,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        if (!await IsEligibleCompetenceAsync(personId, year, month, cancellationToken))
        {
            return null;
        }

        var payslip = await payslipRepository.GetByCompetenceAsync(
            personId,
            year,
            month,
            paymentType,
            cancellationToken);
        return payslip is null ? null : ToDetail(payslip);
    }

    public async Task<byte[]> GetPdfAsync(
        int year,
        int month,
        string? paymentType = null,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        if (!await IsEligibleCompetenceAsync(personId, year, month, cancellationToken))
        {
            throw new InvalidOperationException("Holerite não encontrado.");
        }

        var payslip = await payslipRepository.GetByCompetenceAsync(
            personId,
            year,
            month,
            paymentType,
            cancellationToken)
            ?? throw new InvalidOperationException("Holerite não encontrado.");

        var document = await payslipPdfBuilder.BuildAsync(personId, payslip, cancellationToken)
            ?? throw new InvalidOperationException("Não foi possível montar o PDF do holerite.");

        return PayslipPdfGenerator.Generate(document);
    }

    public async Task<PayslipComparativoDto?> GetComparativoAsync(
        int fromYear,
        int fromMonth,
        int toYear,
        int toMonth,
        CancellationToken cancellationToken = default)
    {
        var from = await GetDetailAsync(fromYear, fromMonth, "FOLHA", cancellationToken);
        var to = await GetDetailAsync(toYear, toMonth, "FOLHA", cancellationToken);
        if (from is null || to is null)
        {
            return null;
        }

        return new PayslipComparativoDto(
            from,
            to,
            to.NetAmount - from.NetAmount,
            to.GrossAmount - from.GrossAmount);
    }

    public async Task<IncomeStatementDto?> GetIncomeStatementAsync(int year, CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var admissionDate = await ResolveAdmissionDateAsync(personId, cancellationToken);
        if (admissionDate is not null && year < admissionDate.Value.Year)
        {
            return null;
        }

        var statement = await payslipRepository.GetIncomeStatementAsync(personId, year, cancellationToken);
        if (statement is null)
        {
            return null;
        }

        var lines = DeserializeLines<IncomeStatementLineDto>(statement.LinesJson);
        lines = PayslipCompetenceRules
            .FilterIncomeLinesByAdmission(lines, year, admissionDate, line => line.Month)
            .Where(line => line.Paid > 0m || line.Withheld > 0m)
            .ToList();

        var payslips = await ListEligiblePayslipsAsync(personId, year, null, 24, cancellationToken);
        lines = PayslipIncomeStatementRules
            .EnrichWithheldFromPayslips(lines, payslips, year)
            .Where(line => line.Paid > 0m || line.Withheld > 0m)
            .ToList();

        if (lines.Count == 0)
        {
            return null;
        }

        return new IncomeStatementDto(
            statement.Year,
            lines.Sum(line => line.Paid),
            lines.Sum(line => line.Withheld),
            lines);
    }

    public async Task<FgtsConsultaDto> GetFgtsConsultaAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var eligible = await ListEligiblePayslipsAsync(personId, null, null, 12, cancellationToken);
        var deposits = eligible
            .Where(p => string.Equals(p.PaymentType, "FOLHA", StringComparison.OrdinalIgnoreCase))
            .Select(p =>
            {
                var amount = p.FgtsDepositAmount > 0m
                    ? p.FgtsDepositAmount
                    : Math.Round(p.GrossAmount * 0.08m, 2);

                return new FgtsDepositDto(
                    FormatCompetence(p.Year, p.Month),
                    amount,
                    Math.Round(amount * 2m, 2));
            })
            .Where(d => d.Amount > 0m)
            .ToList();

        return new FgtsConsultaDto(deposits.Sum(d => d.Amount), deposits);
    }

    public async Task<DescontosConsultaDto> GetDescontosConsultaAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var eligible = await ListEligiblePayslipsAsync(personId, null, null, 100, cancellationToken);
        var latest = eligible.FirstOrDefault(p =>
                         string.Equals(p.PaymentType, "FOLHA", StringComparison.OrdinalIgnoreCase))
                     ?? eligible.FirstOrDefault()
                     ?? throw new InvalidOperationException("Nenhum holerite disponível.");

        var deductions = DeserializeLines<PayslipLineDto>(latest.DeductionsJson)
            .Where(d => PayslipDeductionRules.IsReportableDeduction(d.Code, d.Label))
            .ToList();

        var items = deductions
            .Select(d => new DescontoItemDto(
                d.Code,
                PayslipRubricCatalog.ResolveLabel(d.Code, d.Label),
                d.Amount,
                FormatCompetence(latest.Year, latest.Month)))
            .ToList();

        return new DescontosConsultaDto(items.Sum(i => i.Amount), items);
    }

    public async Task<RubricasConsultaDto> GetRubricasConsultaAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var eligible = await ListEligiblePayslipsAsync(personId, null, null, 12, cancellationToken);
        var latest = eligible.FirstOrDefault(p =>
                         string.Equals(p.PaymentType, "FOLHA", StringComparison.OrdinalIgnoreCase))
                     ?? eligible.FirstOrDefault()
                     ?? throw new InvalidOperationException("Nenhum holerite disponível.");

        var earnings = DeserializeLines<PayslipLineDto>(latest.EarningsJson);
        var deductions = DeserializeLines<PayslipLineDto>(latest.DeductionsJson)
            .Where(d => PayslipDeductionRules.IsReportableDeduction(d.Code, d.Label));
        var items = earnings
            .Concat(deductions)
            .GroupBy(line => line.Code, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(line => line.Code, StringComparer.Ordinal)
            .Select(line => new RubricaHelpDto(
                line.Code,
                PayslipRubricCatalog.ResolveLabel(line.Code, line.Label),
                PayslipRubricCatalog.ResolveDescription(line.Code, line.Label)))
            .ToList();

        return new RubricasConsultaDto(items);
    }

    public async Task<byte[]> GetComprovantePdfAsync(CancellationToken cancellationToken = default) =>
        PayslipRhDocumentPdfGenerator.GenerateComprovante(await BuildRhDocumentAsync(cancellationToken));

    public async Task<byte[]> GetCartaConsignacaoPdfAsync(CancellationToken cancellationToken = default)
    {
        var document = await BuildRhDocumentAsync(cancellationToken);
        return PayslipRhDocumentPdfGenerator.GenerateCartaConsignacao(document);
    }

    public async Task<PayslipRequestResultDto> CreateRequestAsync(
        CreatePayslipRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["serviceId"] = request.ServiceId,
            ["competence"] = request.Competence,
            ["notes"] = request.Notes
        };

        var created = await serviceRequestService.CreateAsync(
            new CreateServiceRequestRequest("servicos-contracheque", ServiceCategory.RH, payload),
            cancellationToken);

        return new PayslipRequestResultDto(
            created.Id,
            created.Status.ToString(),
            "Solicitação registrada com sucesso. Acompanhe o andamento em Serviços.");
    }

    private async Task<bool> IsEligibleCompetenceAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var admissionDate = await ResolveAdmissionDateAsync(personId, cancellationToken);
        return PayslipCompetenceRules.IsEligible(year, month, admissionDate);
    }

    private async Task<IReadOnlyList<Payslip>> ListEligiblePayslipsAsync(
        Guid personId,
        int? year,
        int? month,
        int limit,
        CancellationToken cancellationToken)
    {
        var admissionDate = await ResolveAdmissionDateAsync(personId, cancellationToken);
        var payslips = await payslipRepository.ListAsync(personId, year, month, limit, cancellationToken);

        return payslips
            .Where(p => PayslipCompetenceRules.IsEligible(p.Year, p.Month, admissionDate))
            .ToList();
    }

    private async Task<string?> ResolveChapaAsync(
        Guid personId,
        CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken);
        if (person is null || string.IsNullOrWhiteSpace(person.EmployeeId))
        {
            return null;
        }

        return TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
    }

    private async Task<DateTime?> ResolveAdmissionDateAsync(
        Guid personId,
        CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken);
        if (person is null || string.IsNullOrWhiteSpace(person.EmployeeId))
        {
            return null;
        }

        var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
        if (string.IsNullOrWhiteSpace(chapa))
        {
            return null;
        }

        var profile = await employeeRepository.GetProfileByChapaAsync(chapa, cancellationToken);
        return profile?.DataAdmissao?.Date;
    }

    private async Task<PayslipRhDocumentDto> BuildRhDocumentAsync(CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Perfil não encontrado.");

        var latest = await payslipRepository.GetLatestAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Nenhum holerite disponível para emissão.");

        var chapa = await ResolveChapaAsync(personId, cancellationToken) ?? person.EmployeeId ?? "—";
        var profile = string.IsNullOrWhiteSpace(chapa)
            ? null
            : await employeeRepository.GetProfileByChapaAsync(chapa, cancellationToken);

        var net = latest.NetAmount;
        return new PayslipRhDocumentDto(
            "LIO Tecnica",
            profile?.Nome ?? person.Name,
            chapa,
            profile?.FuncaoDescricao ?? person.Title ?? "Colaborador",
            profile?.SecaoDescricao ?? person.Dept ?? "—",
            FormatCompetence(latest.Year, latest.Month),
            latest.GrossAmount,
            latest.DeductionsTotal,
            net,
            Math.Round(net * 0.35m, 2),
            DateTimeOffset.UtcNow.ToString("dd/MM/yyyy HH:mm", PtBr));
    }

    private async Task<PayslipSyncContext> TriggerSyncIfStaleAsync(CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var person = await personRepository.GetByIdAsync(personId, cancellationToken);

        if (person is null || string.IsNullOrWhiteSpace(person.EmployeeId))
        {
            return PayslipSyncContext.Unavailable(
                "missing_employee_id",
                "Sua matricula nao esta vinculada ao perfil. Solicite ao RH a regularizacao do cadastro.");
        }

        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled)
        {
            var cachedSyncedAt = await payslipRepository.GetMaxSyncedAtUtcAsync(personId, cancellationToken);
            if (cachedSyncedAt is not null)
            {
                return PayslipSyncContext.FromCache(cachedSyncedAt, "rm_disabled");
            }

            return PayslipSyncContext.Unavailable(
                "rm_disabled",
                "Consulta de holerite temporariamente indisponivel. Entre em contato com o RH.");
        }

        var ttlMinutes = settings.GetInt(AppSettingKeys.WorkersTotvsPayslipCacheTtlMinutes, 1440);
        var maxSyncedAt = await payslipRepository.GetMaxSyncedAtUtcAsync(personId, cancellationToken);
        if (maxSyncedAt is not null && !IsStale(maxSyncedAt.Value, ttlMinutes))
        {
            return PayslipSyncContext.FromCache(maxSyncedAt, "cache");
        }

        try
        {
            var result = await payslipSyncService.SyncPersonAsync(personId, cancellationToken);
            return new PayslipSyncContext(
                result.AvailabilityStatus,
                null,
                result.DataSource ?? "live",
                result.SyncedAt ?? DateTimeOffset.UtcNow);
        }
        catch (TotvsRmIntegrationDisabledException)
        {
            return PayslipSyncContext.Unavailable(
                "rm_disabled",
                "Consulta de holerite temporariamente indisponivel. Entre em contato com o RH.");
        }
        catch (TotvsRmIntegrationMisconfiguredException)
        {
            return PayslipSyncContext.Unavailable(
                "rm_disabled",
                "Consulta de holerite temporariamente indisponivel. Entre em contato com o RH.");
        }
        catch (TotvsRmIntegrationUnavailableException)
        {
            if (maxSyncedAt is not null)
            {
                return PayslipSyncContext.FromCache(maxSyncedAt, "rm_unavailable") with
                {
                    UserMessage = "Exibindo dados em cache. Nao foi possivel atualizar agora."
                };
            }

            return PayslipSyncContext.Unavailable(
                "rm_unavailable",
                "Nao foi possivel consultar holerites agora. Tente novamente em alguns minutos.");
        }
    }

    private static bool IsStale(DateTimeOffset syncedAtUtc, int ttlMinutes) =>
        syncedAtUtc.AddMinutes(ttlMinutes) < DateTimeOffset.UtcNow;

    private static int ResolveInformeYear(int? hiredYear)
    {
        var currentYear = DateTime.UtcNow.Year;
        if (hiredYear is not null && hiredYear.Value >= currentYear)
        {
            return currentYear;
        }

        return currentYear - 1;
    }

    private static PayslipListItemDto ToListItem(Payslip payslip) =>
        new(
            payslip.Year,
            payslip.Month,
            FormatCompetence(payslip.Year, payslip.Month),
            payslip.GrossAmount,
            payslip.NetAmount,
            payslip.PublishedAt,
            payslip.PaymentType);

    private static PayslipDetailDto ToDetail(Payslip payslip)
    {
        var earnings = DeserializeLines<PayslipLineDto>(payslip.EarningsJson)
            .Select(line => line with
            {
                Label = PayslipRubricCatalog.ResolveLabel(line.Code, line.Label)
            })
            .ToList();

        var deductions = DeserializeLines<PayslipLineDto>(payslip.DeductionsJson)
            .Where(line => PayslipDeductionRules.IsReportableDeduction(line.Code, line.Label))
            .Select(line => line with
            {
                Label = PayslipRubricCatalog.ResolveLabel(line.Code, line.Label)
            })
            .ToList();

        return new PayslipDetailDto(
            payslip.Year,
            payslip.Month,
            FormatCompetence(payslip.Year, payslip.Month),
            payslip.GrossAmount,
            payslip.NetAmount,
            deductions.Sum(line => line.Amount),
            earnings,
            deductions,
            payslip.PublishedAt);
    }

    private static IReadOnlyList<T> DeserializeLines<T>(string json) =>
        JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];

    private static string FormatCompetence(int year, int month)
    {
        var label = PtBr.DateTimeFormat.GetAbbreviatedMonthName(month);
        if (!string.IsNullOrEmpty(label))
        {
            label = char.ToUpper(label[0]) + label[1..].TrimEnd('.');
        }

        return $"{label}/{year}";
    }

    private sealed record PayslipSyncContext(
        string AvailabilityStatus,
        string? UserMessage,
        string? DataSource,
        DateTimeOffset? SyncedAt)
    {
        public static PayslipSyncContext Unavailable(string status, string message) =>
            new(status, message, null, null);

        public static PayslipSyncContext FromCache(DateTimeOffset? syncedAt, string status) =>
            new("ok", null, "cache", syncedAt);
    }
}
