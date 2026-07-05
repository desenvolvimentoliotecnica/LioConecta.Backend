using LioConecta.Application.Common;

namespace LioConecta.UnitTests;

public class TotvsRmChapaNormalizerTests
{
    [Theory]
    [InlineData("12345", "00012345")]
    [InlineData("00012345", "00012345")]
    [InlineData(" 12345 ", "00012345")]
    public void Normalize_PadsEmployeeIdToEightDigits(string input, string expected)
    {
        var result = TotvsRmChapaNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_ReturnsNullForMissingEmployeeId(string? input)
    {
        var result = TotvsRmChapaNormalizer.Normalize(input);
        Assert.Null(result);
    }
}
