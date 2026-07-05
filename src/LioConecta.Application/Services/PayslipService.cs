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
        new("historico", "Histórico de Holerites", "Consulte contracheques dos últimos 24 meses com busca por competência e tipo de pagamento.", "historico", "Imediato", true, false, "Consultar", "Lista todas as competências disponíveis. Selecione uma linha para visualizar o demonstrativo completo."),
        new("comparativo", "Comparativo Salarial", "Compare proventos, descontos e líquido entre dois meses para entender variações na remuneração.", "consulta", "Imediato", true, false, "Consultar", "Compara automaticamente os dois últimos holerites. Útil para identificar diferenças de horas extras ou descontos."),
        new("demonstrativo", "Demonstrativo Detalhado", "Visualize rubricas, bases de cálculo, horas extras, adicionais e descontos linha a linha.", "holerite", "Imediato", true, false, "Visualizar", "Exibe todas as rubricas de proventos e descontos do holerite mais recente, com códigos e valores."),
        new("informe-rendimentos", "Informe de Rendimentos", "Emita o informe anual para declaração de Imposto de Renda com valores pagos e retidos.", "informe", "Imediato", true, false, "Emitir", "Disponível após fechamento anual. Contém totais pagos e impostos retidos mês a mês."),
        new("comprovante", "Comprovante de Rendimentos", "Documento simplificado para comprovação de renda em processos internos ou externos.", "informe", "Até 1 dia útil", true, false, "Solicitar", "Gera solicitação ao RH para emissão de comprovante simplificado. Prazo de até 1 dia útil."),
        new("carta-consignacao", "Carta de Consignação", "Consulte margem consignável e emita carta para empréstimos e convênios autorizados.", "documento", "Até 2 dias úteis", true, false, "Emitir", "Calcula margem consignável com base no salário líquido e encaminha solicitação de emissão da carta."),
        new("fgts", "FGTS e Encargos", "Resumo de depósitos de FGTS, INSS e demais encargos vinculados ao seu contrato.", "consulta", "Imediato", true, false, "Consultar", "Mostra depósitos mensais de FGTS calculados sobre a remuneração base dos últimos holerites."),
        new("descontos", "Descontos em Folha", "Detalhamento de plano de saúde, vale-transporte, empréstimos consignados e outros descontos.", "consulta", "Imediato", true, false, "Consultar", "Consolida descontos recorrentes do holerite mais recente com código e competência."),
        new("segunda-via", "Solicitar 2ª Via", "Peça reemissão de holerite de competências anteriores quando o documento original não estiver acessível.", "documento", "Até 3 dias úteis", false, false, "Solicitar", "Serviço offline: abre chamado ao RH informando a competência desejada. Prazo de até 3 dias úteis."),
        new("duvidas-rubricas", "Dúvidas sobre Rubricas", "Orientações sobre códigos, siglas e regras de cálculo aplicadas na sua folha de pagamento.", "consulta", "Até 2 dias úteis", true, false, "Consultar", "Glossário dos códigos de rubricas mais frequentes na sua folha, com descrição e regra de cálculo.")
    ];

    public async Task<PayslipSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var syncContext = await EnsureSyncedAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
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
                syncContext.SyncedAt);
        }

        return new PayslipSummaryDto(
            FormatCompetence(latest.Year, latest.Month),
            latest.NetAmount,
            eligible.Count,
            syncContext.AvailabilityStatus,
            syncContext.UserMessage,
            syncContext.DataSource,
            syncContext.SyncedAt);
    }

    public IReadOnlyList<PayslipServiceDto> GetServices() => ServiceCatalog;

    public async Task<IReadOnlyList<PayslipListItemDto>> ListAsync(
        int? year,
        int? month,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await EnsureSyncedAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var payslips = await ListEligiblePayslipsAsync(personId, year, month, limit, cancellationToken);
        return payslips.Select(ToListItem).ToList();
    }

    public async Task<PayslipDetailDto?> GetDetailAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        await EnsureSyncedAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        if (!await IsEligibleCompetenceAsync(personId, year, month, cancellationToken))
        {
            return null;
        }

        var payslip = await payslipRepository.GetByCompetenceAsync(personId, year, month, cancellationToken);
        return payslip is null ? null : ToDetail(payslip);
    }

    public async Task<byte[]> GetPdfAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        await EnsureSyncedAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        if (!await IsEligibleCompetenceAsync(personId, year, month, cancellationToken))
        {
            throw new InvalidOperationException("Holerite não encontrado.");
        }

        var payslip = await payslipRepository.GetByCompetenceAsync(personId, year, month, cancellationToken)
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
        var from = await GetDetailAsync(fromYear, fromMonth, cancellationToken);
        var to = await GetDetailAsync(toYear, toMonth, cancellationToken);
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
        var statement = await payslipRepository.GetIncomeStatementAsync(personId, year, cancellationToken);
        if (statement is null)
        {
            return null;
        }

        var lines = DeserializeLines<IncomeStatementLineDto>(statement.LinesJson);
        return new IncomeStatementDto(statement.Year, statement.TotalPaid, statement.TotalWithheld, lines);
    }

    public async Task<FgtsConsultaDto> GetFgtsConsultaAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSyncedAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var payslips = await ListEligiblePayslipsAsync(personId, null, null, 6, cancellationToken);
        var deposits = payslips
            .Where(p => p.PaymentType == "FOLHA")
            .Select(p =>
            {
                var baseAmount = p.GrossAmount * 0.08m;
                return new FgtsDepositDto(
                    FormatCompetence(p.Year, p.Month),
                    Math.Round(baseAmount, 2),
                    Math.Round(baseAmount * 2, 2));
            })
            .ToList();

        return new FgtsConsultaDto(deposits.Sum(d => d.Amount), deposits);
    }

    public async Task<DescontosConsultaDto> GetDescontosConsultaAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSyncedAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var eligible = await ListEligiblePayslipsAsync(personId, null, null, 100, cancellationToken);
        var latest = eligible.FirstOrDefault()
            ?? throw new InvalidOperationException("Nenhum holerite disponível.");

        var deductions = DeserializeLines<PayslipLineDto>(latest.DeductionsJson);
        var items = deductions
            .Select(d => new DescontoItemDto(d.Code, d.Label, d.Amount, FormatCompetence(latest.Year, latest.Month)))
            .ToList();

        return new DescontosConsultaDto(items.Sum(i => i.Amount), items);
    }

    public Task<RubricasConsultaDto> GetRubricasConsultaAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<RubricaHelpDto>
        {
            new("001", "Salário base", "Remuneração fixa mensal conforme contrato de trabalho."),
            new("050", "Horas extras 50%", "Adicional de 50% sobre a hora normal. Quantidade informada no demonstrativo."),
            new("080", "13º salário", "Gratificação natalina paga conforme calendário da folha."),
            new("201", "INSS", "Contribuição previdenciária descontada conforme tabela vigente."),
            new("202", "IRRF", "Imposto de renda retido na fonte calculado sobre a base tributável."),
            new("210", "Plano de saúde", "Coparticipação e mensalidade do plano corporativo."),
            new("220", "Vale-transporte", "Desconto de até 6% do salário base, conforme legislação.")
        };

        return Task.FromResult(new RubricasConsultaDto(items));
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

    private async Task<PayslipSyncContext> EnsureSyncedAsync(CancellationToken cancellationToken)
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

        var ttlMinutes = settings.GetInt(AppSettingKeys.WorkersTotvsPayslipCacheTtlMinutes, 60);
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

    private static PayslipListItemDto ToListItem(Payslip payslip) =>
        new(
            payslip.Year,
            payslip.Month,
            FormatCompetence(payslip.Year, payslip.Month),
            payslip.GrossAmount,
            payslip.NetAmount,
            payslip.PublishedAt,
            payslip.PaymentType);

    private static PayslipDetailDto ToDetail(Payslip payslip) =>
        new(
            payslip.Year,
            payslip.Month,
            FormatCompetence(payslip.Year, payslip.Month),
            payslip.GrossAmount,
            payslip.NetAmount,
            payslip.DeductionsTotal,
            DeserializeLines<PayslipLineDto>(payslip.EarningsJson),
            DeserializeLines<PayslipLineDto>(payslip.DeductionsJson),
            payslip.PublishedAt);

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
