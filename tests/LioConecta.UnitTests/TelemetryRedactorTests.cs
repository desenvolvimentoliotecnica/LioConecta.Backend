using LioConecta.Application.Common.Observability;

namespace LioConecta.UnitTests;

public class TelemetryRedactorTests
{
    [Fact]
    public void SanitizeProperties_StripsSensitiveAndUnknownKeys()
    {
        var properties = new Dictionary<string, object?>
        {
            ["message"] = "safe",
            ["password"] = "secret",
            ["token"] = "jwt-value",
            ["unexpectedField"] = "drop-me",
            ["routeTemplate"] = "/admin/observabilidade",
        };

        var json = TelemetryRedactor.SanitizeProperties(properties);

        Assert.NotNull(json);
        Assert.Contains("safe", json, StringComparison.Ordinal);
        Assert.Contains("/admin/observabilidade", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("jwt-value", json, StringComparison.Ordinal);
        Assert.DoesNotContain("drop-me", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveIpFields_BothMode_ReturnsFullAndHash()
    {
        var (ipAddress, ipHash) = TelemetryRedactor.ResolveIpFields("203.0.113.10", "both");

        Assert.Equal("203.0.113.10", ipAddress);
        Assert.NotNull(ipHash);
        Assert.Equal(32, ipHash!.Length);
    }

    [Fact]
    public void TruncateUserAgent_LimitsLength()
    {
        var longAgent = new string('x', 600);
        var truncated = TelemetryRedactor.TruncateUserAgent(longAgent);

        Assert.NotNull(truncated);
        Assert.Equal(512, truncated!.Length);
    }
}
