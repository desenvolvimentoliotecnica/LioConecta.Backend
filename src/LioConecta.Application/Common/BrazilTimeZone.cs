namespace LioConecta.Application.Common;

public static class BrazilTimeZone
{
    private static readonly Lazy<TimeZoneInfo> SaoPaulo = new(ResolveSaoPaulo);

    public static TimeZoneInfo SaoPauloTimeZone => SaoPaulo.Value;

    private static TimeZoneInfo ResolveSaoPaulo()
    {
        foreach (var timeZoneId in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
