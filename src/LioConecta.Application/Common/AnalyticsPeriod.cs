namespace LioConecta.Application.Common;

public static class AnalyticsPeriod
{
    public static (DateTimeOffset From, DateTimeOffset To, string Key) Resolve(string? period)
    {
        var to = DateTimeOffset.UtcNow;
        return period?.Trim().ToLowerInvariant() switch
        {
            "7d" => (to.AddDays(-7), to, "7d"),
            "90d" => (to.AddDays(-90), to, "90d"),
            "12m" => (to.AddDays(-365), to, "12m"),
            _ => (to.AddDays(-30), to, "30d"),
        };
    }
}
