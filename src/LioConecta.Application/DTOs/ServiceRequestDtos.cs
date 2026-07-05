using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record ServiceRequestDto(
    Guid Id,
    string Type,
    ServiceCategory Category,
    ServiceRequestStatus Status,
    PersonSummaryDto Requester,
    IReadOnlyDictionary<string, object?> Payload,
    string? AssigneeTeam,
    string? ExternalRef,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ServiceRequestEventDto> Events);

public sealed record CreateServiceRequestRequest(
    string Type,
    ServiceCategory Category,
    IReadOnlyDictionary<string, object?> Payload);

public sealed record ServiceRequestEventDto(
    Guid Id,
    string EventType,
    PersonSummaryDto? Actor,
    DateTimeOffset CreatedAt,
    IReadOnlyDictionary<string, object?>? Details);
