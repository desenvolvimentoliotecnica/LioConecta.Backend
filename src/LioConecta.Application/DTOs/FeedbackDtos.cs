using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record CreateFeedbackRequest(FeedbackCategory Category, string Subject, string Message, bool IsAnonymous);
public sealed record UpdateFeedbackRequest(FeedbackStatus Status, string? ResponseText, Guid? AssigneeId);
public sealed record FeedbackSubmissionDto(Guid Id, FeedbackCategory Category, FeedbackStatus Status, string Subject, string Message, bool IsAnonymous, string? ResponseText, Guid? AssigneeId, PersonSummaryDto? Author, DateTimeOffset CreatedAt, DateTimeOffset? RespondedAt);
