namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed record TeamsChatSummary(
    string Id,
    string? Topic,
    string ChatType,
    TeamsChatMessagePreview? LastMessagePreview,
    IReadOnlyList<TeamsChatMember> Members);

public sealed record TeamsChatMessagePreview(
    string Id,
    DateTimeOffset CreatedAt,
    string? Text,
    TeamsChatIdentity? From);

public sealed record TeamsChatMember(
    string Id,
    string DisplayName,
    string? Email);

public sealed record TeamsChatMessage(
    string Id,
    DateTimeOffset CreatedAt,
    string Text,
    TeamsChatIdentity? From);

public sealed record TeamsChatIdentity(
    string Id,
    string DisplayName,
    string? Email);

public sealed record TeamsChatPage<T>(
    IReadOnlyList<T> Items,
    string? NextLink);

public sealed record TeamsGraphUser(
    string Id,
    string DisplayName,
    string? Email);
