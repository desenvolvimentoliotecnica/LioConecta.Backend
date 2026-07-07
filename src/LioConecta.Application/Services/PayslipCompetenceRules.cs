namespace LioConecta.Application.Services;

internal static class PayslipCompetenceRules
{
    public static bool IsEligible(int year, int month, DateTime? admissionDate)
    {
        if (month is < 1 or > 12)
        {
            return false;
        }

        if (IsFutureCompetence(year, month))
        {
            return false;
        }

        if (admissionDate is null)
        {
            return true;
        }

        var competence = year * 100 + month;
        var admissionCompetence = admissionDate.Value.Year * 100 + admissionDate.Value.Month;
        return competence >= admissionCompetence;
    }

    public static bool IsFutureCompetence(int year, int month, DateTime? referenceUtc = null)
    {
        var reference = referenceUtc ?? DateTime.UtcNow;
        var competence = year * 100 + month;
        var currentCompetence = reference.Year * 100 + reference.Month;
        return competence > currentCompetence;
    }
}
