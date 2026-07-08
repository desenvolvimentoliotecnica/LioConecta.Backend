namespace LioConecta.Application.Services;

public static class LeaveDateRules
{
    public static int CountInclusiveDays(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentException("A data fim deve ser igual ou posterior à data início.");
        }

        return end.DayNumber - start.DayNumber + 1;
    }

    public static void ValidateVacationPeriod(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentException("A data fim deve ser igual ou posterior à data início.");
        }
    }
}
