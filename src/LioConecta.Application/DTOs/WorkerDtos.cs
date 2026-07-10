namespace LioConecta.Application.DTOs;

public sealed record WorkerDefinitionDto(
    string Key,
    string Label,
    string Description,
    string? IntervalSettingKey,
    int? DefaultIntervalMinutes,
    bool HostedInWorkersProcess,
    IReadOnlyList<string> DependsOn);

public sealed record WorkerRunDto(
    Guid Id,
    string WorkerKey,
    string Status,
    string TriggerSource,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string? ErrorMessage);

public sealed record WorkerRunLogDto(
    Guid Id,
    DateTimeOffset LoggedAtUtc,
    string Level,
    string Message);

public sealed record WorkerRunDetailDto(
    WorkerRunDto Run,
    IReadOnlyList<WorkerRunLogDto> Logs);

public sealed record WorkerTriggerResultDto(
    Guid RunId,
    string WorkerKey,
    string Status,
    string? ErrorMessage);

public sealed record WorkerConnectivityComponentDto(
    string Id,
    string Label,
    bool Healthy,
    long? LatencyMs,
    string? Message);

public sealed record WorkerConnectivityDto(
    DateTimeOffset CheckedAtUtc,
    IReadOnlyList<WorkerConnectivityComponentDto> Components);
