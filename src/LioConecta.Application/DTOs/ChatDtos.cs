namespace LioConecta.Application.DTOs;

public sealed record ChatBootstrapDto(
    bool Enabled,
    string AuthMode,
    IReadOnlyList<string> DelegatedScopes,
    bool IncludeGroupChats,
    int PollingIntervalSeconds,
    bool SignalREnabled,
    string MsalClientId,
    string MsalTenantId,
    string MsalAuthority);

public sealed record ChatStatusDto(
    bool Enabled,
    bool Linked,
    bool NeedsConsent);

public sealed record ChatConversationDto(
    string Id,
    string? Title,
    string? ChatType,
    ChatParticipantDto? LastMessageAuthor,
    string? LastMessagePreview,
    DateTimeOffset? LastMessageAt,
    int UnreadCount,
    IReadOnlyList<ChatParticipantDto>? Participants);

public sealed record ChatMessageDto(
    string Id,
    ChatParticipantDto Author,
    string Text,
    DateTimeOffset CreatedAt);

public sealed record ChatParticipantDto(
    string Id,
    string DisplayName,
    string? Email,
    string? PhotoUrl);

public sealed record LinkTeamsAccountRequest(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<string>? Scopes);

public sealed record CreateChatConversationRequest(
    string TargetEmail);

public sealed record SendMessageRequest(string Text);

public sealed record TestChatConnectionRequest(
    string? TenantId,
    string? ClientId,
    string? ClientSecret);

public sealed record ChatConnectionTestResponse(
    bool Success,
    string Message,
    string? Detail,
    bool ChatEnabled,
    string? AuthMode);
