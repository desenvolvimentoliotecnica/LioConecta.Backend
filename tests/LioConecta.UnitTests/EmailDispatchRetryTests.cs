using LioConecta.Application.Services;

namespace LioConecta.UnitTests;

public class EmailDispatchRetryTests
{
    [Theory]
    [InlineData(60, 21600, 1, 60)]
    [InlineData(60, 21600, 2, 120)]
    [InlineData(60, 21600, 3, 240)]
    [InlineData(60, 21600, 10, 21600)]
    public void CalculateDelaySeconds_UsesExponentialBackoffWithCap(
        int initial,
        int max,
        int attempt,
        int expected)
    {
        var result = EmailRetryCalculator.CalculateDelaySeconds(initial, max, attempt);
        Assert.Equal(expected, result);
    }
}
