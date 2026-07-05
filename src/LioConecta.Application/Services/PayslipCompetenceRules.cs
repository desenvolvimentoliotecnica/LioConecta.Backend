namespace LioConecta.Application.Services;

internal static class PayslipCompetenceRules
{
    public static bool IsEligible(int year, int month, DateTime? admissionDate)
    {
        if (month is < 1 or > 12)
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
}
