using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record IntegrationFeedPublishRequest(
    string Type,
    Guid AuthorPersonId,
    string Content,
    string? MetadataJson = null,
    bool IsPinned = false,
    string? IdempotencyKey = null);

public sealed record IntegrationFeedPublishResponse(Guid PostId);

public sealed record IntegrationNotifyRequest(
    IReadOnlyList<Guid>? RecipientPersonIds,
    bool AllActive = false,
    string Title = "",
    string Body = "",
    string? Href = null,
    string? Type = null,
    string? Source = null,
    string? CorrelationId = null);

public sealed record IntegrationNotifyResponse(int RecipientCount);

public sealed record IntegrationEmailEnqueueRequest(
    IReadOnlyList<string> To,
    string Subject,
    string? BodyHtml = null,
    string? BodyText = null,
    string? MetadataJson = null,
    string? CorrelationId = null,
    Guid? CreatedById = null,
    short Priority = 0,
    string? IdempotencyKey = null);

public sealed record IntegrationEmailEnqueueResponse(Guid MessageId);

public sealed record IntegrationPeopleResolveRequest(
    IReadOnlyList<string>? Emails = null,
    IReadOnlyList<string>? Roles = null,
    IReadOnlyList<string>? PermissionKeys = null,
    bool ActiveOnly = true,
    Guid? ExcludePersonId = null);

public sealed record IntegrationPersonDto(
    Guid Id,
    string Name,
    string Email,
    IReadOnlyList<string> Roles);

public sealed record IntegrationPeopleResolveResponse(
    IReadOnlyList<IntegrationPersonDto> People);
