namespace LioConecta.Application.DTOs;

public sealed record HelpDeskSummaryDto(
    int OpenTickets,
    string AvgResponseLabel,
    bool CanViewAllTickets = false);

public sealed record HelpDeskServiceDto(
    string Id,
    string Title,
    string Desc,
    string Category,
    string Provider,
    string Status,
    bool Featured,
    string Action,
    string HelpText,
    string? PortalUrl);

public sealed record HelpDeskKnowledgeArticleDto(
    string Id,
    string Title,
    string Summary,
    string Category,
    DateTimeOffset UpdatedAt,
    string Url);

public sealed record CreateHelpDeskTicketRequestDto(
    string Subject,
    string Priority,
    int CategoryId,
    string Description);

public sealed record HelpDeskItilCategoryDto(
    int Id,
    string Name,
    string? FullName);

public sealed record HelpDeskTicketResultDto(
    Guid RequestId,
    string Status,
    string Message,
    string? ExternalRef,
    string? ExternalUrl);

public sealed record HelpDeskTicketListItemDto(
    string TicketId,
    string Subject,
    string Status,
    string StatusLabel,
    string PriorityLabel,
    DateTimeOffset CreatedAt,
    string? ExternalUrl,
    string? RequesterLabel = null);

public sealed record HelpDeskTicketEventDto(
    string EventType,
    DateTimeOffset CreatedAt,
    string? Author);

public sealed record HelpDeskTicketDetailDto(
    HelpDeskTicketListItemDto Summary,
    string Description,
    string? Assignee,
    IReadOnlyList<HelpDeskTicketEventDto> Events);
