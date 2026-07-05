using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record MoodTodayDto(
    bool HasRegistered,
    MoodLevel? Mood,
    DateTimeOffset? RecordedAt);

public sealed record RegisterMoodRequest(MoodLevel Mood);

public sealed record RegisterMoodResultDto(
    MoodLevel Mood,
    DateTimeOffset RecordedAt);

public sealed record MoodMetricsDto(
    DateOnly From,
    DateOnly To,
    int Total,
    IReadOnlyDictionary<string, int> ByMood);
