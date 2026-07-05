using System.Globalization;
using System.Text;
using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class PayslipService(
    IPayslipRepository payslipRepository,
    IServiceRequestService serviceRequestService,
    ICurrentUserService currentUserService) : IPayslipService
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
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var latest = await payslipRepository.GetLatestAsync(personId, cancellationToken);
        var count = await payslipRepository.CountAsync(personId, cancellationToken);

        if (latest is null)
        {
            return new PayslipSummaryDto("—", 0m, 0);
        }

        return new PayslipSummaryDto(
            FormatCompetence(latest.Year, latest.Month),
            latest.NetAmount,
            count);
    }

    public IReadOnlyList<PayslipServiceDto> GetServices() => ServiceCatalog;

    public async Task<IReadOnlyList<PayslipListItemDto>> ListAsync(
        int? year,
        int? month,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var payslips = await payslipRepository.ListAsync(personId, year, month, limit, cancellationToken);
        return payslips.Select(ToListItem).ToList();
    }

    public async Task<PayslipDetailDto?> GetDetailAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var payslip = await payslipRepository.GetByCompetenceAsync(personId, year, month, cancellationToken);
        return payslip is null ? null : ToDetail(payslip);
    }

    public async Task<byte[]> GetPdfAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var detail = await GetDetailAsync(year, month, cancellationToken)
            ?? throw new InvalidOperationException("Holerite não encontrado.");

        var sb = new StringBuilder();
        sb.AppendLine($"Contracheque — {detail.Competence}");
        sb.AppendLine($"Bruto: {detail.GrossAmount.ToString("C", PtBr)}");
        sb.AppendLine($"Descontos: {detail.DeductionsTotal.ToString("C", PtBr)}");
        sb.AppendLine($"Líquido: {detail.NetAmount.ToString("C", PtBr)}");
        sb.AppendLine();
        sb.AppendLine("Proventos:");
        foreach (var line in detail.Earnings)
        {
            sb.AppendLine($"  {line.Code} {line.Label}: {line.Amount.ToString("C", PtBr)}");
        }

        sb.AppendLine("Descontos:");
        foreach (var line in detail.Deductions)
        {
            sb.AppendLine($"  {line.Code} {line.Label}: {line.Amount.ToString("C", PtBr)}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
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
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var payslips = await payslipRepository.ListAsync(personId, null, null, 6, cancellationToken);
        var deposits = payslips
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
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var latest = await payslipRepository.GetLatestAsync(personId, cancellationToken)
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

    private static PayslipListItemDto ToListItem(Payslip payslip) =>
        new(
            payslip.Year,
            payslip.Month,
            FormatCompetence(payslip.Year, payslip.Month),
            payslip.GrossAmount,
            payslip.NetAmount,
            payslip.PublishedAt);

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
}
