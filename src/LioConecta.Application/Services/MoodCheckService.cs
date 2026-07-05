using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class MoodCheckService(
    IMoodCheckRepository moodCheckRepository,
    IAnalyticsRepository analyticsRepository,
    ICurrentUserService currentUserService) : IMoodCheckService
{
    private static readonly TimeZoneInfo CompanyTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");

    public async Task<MoodTodayDto> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var today = GetCompanyToday();
        var existing = await moodCheckRepository.GetByPersonAndDateAsync(personId, today, cancellationToken);

        return existing is null
            ? new MoodTodayDto(false, null, null)
            : new MoodTodayDto(true, existing.Mood, existing.RecordedAt);
    }

    public async Task<RegisterMoodResultDto> RegisterAsync(
        RegisterMoodRequest request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var today = GetCompanyToday();
        var existing = await moodCheckRepository.GetByPersonAndDateAsync(personId, today, cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException("Mood already registered for today.");
        }

        var now = DateTimeOffset.UtcNow;
        var moodCheck = new MoodCheck
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Mood = request.Mood,
            CheckDate = today,
            RecordedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await moodCheckRepository.AddAsync(moodCheck, cancellationToken);

        await analyticsRepository.AddEventAsync(new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            EventType = "MoodCheckRecorded",
            PersonId = personId,
            MetadataJson = $"{{\"mood\":\"{request.Mood}\",\"checkDate\":\"{today:yyyy-MM-dd}\"}}",
            OccurredAt = now,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        return new RegisterMoodResultDto(request.Mood, now);
    }

    public async Task<MoodMetricsDto> GetMetricsAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var today = GetCompanyToday();
        var rangeFrom = from ?? today.AddDays(-30);
        var rangeTo = to ?? today;

        if (rangeFrom > rangeTo)
        {
            (rangeFrom, rangeTo) = (rangeTo, rangeFrom);
        }

        var entries = await moodCheckRepository.GetByDateRangeAsync(rangeFrom, rangeTo, cancellationToken);
        var byMood = entries
            .GroupBy(e => e.Mood.ToString())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        return new MoodMetricsDto(rangeFrom, rangeTo, entries.Count, byMood);
    }

    internal static DateOnly GetCompanyToday()
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CompanyTimeZone);
        return DateOnly.FromDateTime(local);
    }
}
