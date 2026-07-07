namespace LioConecta.Application.Services;

/// <summary>
/// Glossário amigável de rubricas RM (código → explicação para o colaborador).
/// Códigos desconhecidos recebem descrição derivada do rótulo RM.
/// </summary>
public static class PayslipRubricCatalog
{
    private sealed record RubricHelp(string Label, string Description);

    private static readonly Dictionary<string, RubricHelp> ByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["001"] = new("Salário base", "Remuneração fixa mensal do cargo, conforme contrato de trabalho."),
        ["101"] = new("Horas extras 50%", "Horas trabalhadas além da jornada, com adicional de 50% sobre a hora normal."),
        ["102"] = new("Horas extras 100%", "Horas em domingos/feriados ou situações com adicional de 100%."),
        ["201"] = new("INSS", "Contribuição previdenciária descontada conforme tabela do INSS."),
        ["202"] = new("IRRF", "Imposto de Renda Retido na Fonte calculado sobre a remuneração tributável."),
        ["401"] = new("Adiantamento salarial", "Antecipação de parte do salário, paga antes do fechamento da folha mensal."),
        ["404"] = new("Adiantamento salarial", "Valor referente ao adiantamento — não é desconto de benefício ou empréstimo."),
        ["501"] = new("Vale-transporte", "Desconto de até 6% do salário base referente ao benefício de transporte."),
        ["502"] = new("Plano de saúde", "Participação do colaborador no plano médico/odontológico corporativo."),
        ["503"] = new("Empréstimo consignado", "Parcela de empréstimo com desconto direto em folha, conforme contrato bancário."),
        ["504"] = new("Pensão alimentícia", "Desconto judicial ou acordado destinado a pensão alimentícia."),
        ["505"] = new("Faltas / atrasos", "Desconto proporcional por ausências não justificadas ou atrasos."),
        ["506"] = new("Contribuição sindical", "Desconto opcional ou obrigatório referente à contribuição sindical."),
    };

    public static string ResolveLabel(string code, string rmLabel) =>
        ByCode.TryGetValue(NormalizeCode(code), out var help) ? help.Label : rmLabel;

    public static string ResolveDescription(string code, string rmLabel)
    {
        var normalized = NormalizeCode(code);
        if (ByCode.TryGetValue(normalized, out var help))
        {
            return help.Description;
        }

        if (rmLabel.Contains("ADIANTAMENTO", StringComparison.OrdinalIgnoreCase))
        {
            return "Antecipação salarial paga antes do fechamento da folha do mês.";
        }

        if (rmLabel.Contains("INSS", StringComparison.OrdinalIgnoreCase))
        {
            return "Contribuição previdenciária conforme legislação vigente.";
        }

        if (rmLabel.Contains("IRRF", StringComparison.OrdinalIgnoreCase)
            || rmLabel.Contains("IMPOSTO DE RENDA", StringComparison.OrdinalIgnoreCase))
        {
            return "Retenção de Imposto de Renda na fonte sobre rendimentos do trabalho.";
        }

        return $"Verba registrada na folha de pagamento com o código {normalized}. Consulte o RH se precisar de detalhes sobre o cálculo.";
    }

    public static bool IsAdvancePayment(string code, string label) =>
        NormalizeCode(code) is "401" or "404"
        || label.Contains("ADIANTAMENTO", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeCode(string code) => code.Trim().TrimStart('0') switch
    {
        "" => "0",
        var trimmed => trimmed.PadLeft(3, '0')
    };
}
