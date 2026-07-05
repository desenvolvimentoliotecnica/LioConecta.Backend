using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    string? Href,
    bool IsRead,
    DateTimeOffset CreatedAt);
