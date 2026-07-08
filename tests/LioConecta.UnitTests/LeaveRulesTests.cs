using LioConecta.Application.Services;

namespace LioConecta.UnitTests;

public sealed class LeaveDateRulesTests
{
    [Fact]
    public void CountInclusiveDays_ReturnsCorrectCount()
    {
        var start = new DateOnly(2026, 7, 1);
        var end = new DateOnly(2026, 7, 10);
        Assert.Equal(10, LeaveDateRules.CountInclusiveDays(start, end));
    }

    [Fact]
    public void ValidateVacationPeriod_ThrowsWhenEndBeforeStart()
    {
        Assert.Throws<ArgumentException>(() =>
            LeaveDateRules.ValidateVacationPeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 7, 1)));
    }
}

public sealed class LeaveStatusNormalizerTests
{
    [Fact]
    public void FromRm_MapsRejectedStatuses()
    {
        Assert.Equal("rejected", LeaveStatusNormalizer.FromRm("C", null, null));
    }

    [Fact]
    public void FromRm_MapsCompletedWhenEndDatePast()
    {
        var end = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));
        Assert.Equal("completed", LeaveStatusNormalizer.FromRm(null, end.AddDays(-10), end));
    }

    [Fact]
    public void FromRm_MapsApprovedForFuturePeriod()
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var end = start.AddDays(9);
        Assert.Equal("approved", LeaveStatusNormalizer.FromRm("A", start, end));
    }
}
