namespace LioConecta.Application.Services;

public static class EmailRetryCalculator
{
    public static int CalculateDelaySeconds(int initialDelay, int maxDelay, int attemptCount)
    {
        var exponent = Math.Max(0, attemptCount - 1);
        var delay = initialDelay * Math.Pow(2, exponent);
        return (int)Math.Min(maxDelay, delay);
    }
}
