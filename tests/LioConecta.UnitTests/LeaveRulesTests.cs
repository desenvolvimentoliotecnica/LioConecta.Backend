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

public sealed class LeavePeriodClassifierTests
{
    private static readonly DateOnly Today = new(2026, 7, 10);

    [Fact]
    public void Classify_OpenAquisitive_IsEmAquisicao()
    {
        Assert.Equal(
            LeavePeriodClassifier.StatusEmAquisicao,
            LeavePeriodClassifier.Classify(new DateOnly(2027, 4, 12), new DateOnly(2028, 4, 12), Today));
    }

    [Fact]
    public void Classify_ClosedNotExpired_IsLiberado()
    {
        Assert.Equal(
            LeavePeriodClassifier.StatusLiberado,
            LeavePeriodClassifier.Classify(new DateOnly(2025, 7, 10), new DateOnly(2026, 7, 10), Today));
    }

    [Fact]
    public void Classify_Expired_IsVencido()
    {
        Assert.Equal(
            LeavePeriodClassifier.StatusVencido,
            LeavePeriodClassifier.Classify(new DateOnly(2019, 7, 10), new DateOnly(2020, 7, 10), Today));
    }

    [Fact]
    public void BuildContextNote_EmAquisicao_IncludesLiberationDate()
    {
        var note = LeavePeriodClassifier.BuildContextNote(
            LeavePeriodClassifier.StatusEmAquisicao,
            30,
            new DateOnly(2027, 4, 12),
            new DateOnly(2028, 4, 12));

        Assert.Contains("12/04/2027", note);
        Assert.Contains("30", note);
    }
}
