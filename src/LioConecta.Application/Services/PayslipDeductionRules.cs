namespace LioConecta.Application.Services;

public static class PayslipDeductionRules
{
    /// <summary>
    /// Rubricas que aparecem como desconto no RM mas não representam desconto real
    /// (ex.: adiantamento salarial cod. 404).
    /// </summary>
    public static bool IsReportableDeduction(string code, string label) =>
        !PayslipRubricCatalog.IsAdvancePayment(code, label);
}
