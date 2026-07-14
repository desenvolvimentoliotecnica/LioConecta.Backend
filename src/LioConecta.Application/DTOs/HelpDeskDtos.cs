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
    string? Subject,
    string? Priority,
    int EntityId,
    int CategoryId,
    string? Description,
    int? FormId = null,
    IReadOnlyList<HelpDeskFormAnswerDto>? Answers = null);

public sealed record HelpDeskFormAnswerDto(
    int QuestionId,
    string Value);

public sealed record HelpDeskFormCategoryDto(
    int Id,
    string Name,
    string? CompleteName,
    int? ParentId,
    int Level,
    int FormCount);

public sealed record HelpDeskFormSummaryDto(
    int Id,
    string Name,
    string? Description,
    string? Illustration,
    int CategoryId);

public sealed record HelpDeskFormOptionDto(
    string Value,
    string Label);

public sealed record HelpDeskFormQuestionDto(
    int Id,
    string Name,
    string Type,
    string FieldKind,
    bool IsMandatory,
    string? Description,
    string? DefaultValue,
    int? HorizontalRank,
    IReadOnlyList<HelpDeskFormOptionDto> Options);

public sealed record HelpDeskFormSectionDto(
    int Id,
    string Name,
    IReadOnlyList<HelpDeskFormQuestionDto> Questions);

public sealed record HelpDeskFormSchemaDto(
    int Id,
    string Name,
    string? Description,
    int CategoryId,
    IReadOnlyList<HelpDeskFormSectionDto> Sections);

public sealed record HelpDeskAreaDto(
    string Id,
    string Name,
    string Icon,
    int ServiceCount,
    int EntityId);

public sealed record HelpDeskGlpiEntityDto(
    int Id,
    string Name,
    string? FullName,
    int? ParentId,
    bool HasChildren);

public sealed record HelpDeskItilCategoryDto(
    int Id,
    string Name,
    string? FullName,
    int? ParentId,
    bool HasChildren,
    int EntityId);

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
    string Kind,
    string Content,
    DateTimeOffset CreatedAt,
    string? Author);

public sealed record HelpDeskTicketResolutionDto(
    string Content,
    DateTimeOffset? ResolvedAt,
    string? Author);

public sealed record HelpDeskTicketAttachmentDto(
    string DocumentId,
    string FileName,
    string? ContentType,
    long? SizeBytes);

public sealed record HelpDeskTicketDetailDto(
    HelpDeskTicketListItemDto Summary,
    string Description,
    string? Assignee,
    HelpDeskTicketResolutionDto? Resolution,
    IReadOnlyList<HelpDeskTicketEventDto> Events,
    IReadOnlyList<HelpDeskTicketAttachmentDto> Attachments);
