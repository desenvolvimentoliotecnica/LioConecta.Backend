using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Application.Services;

public sealed class ChatService(
    IAppSettingsProvider settingsProvider,
    ICurrentUserService currentUserService,
    IUserGraphTokenService graphTokenService,
    ITeamsChatAdapter teamsChatAdapter,
    IChatBroadcaster chatBroadcaster) : IChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<ChatBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var instance = settingsProvider.GetString(AppSettingKeys.AzureAdInstance, "https://login.microsoftonline.com/");
        var tenantId = settingsProvider.GetString(AppSettingKeys.AzureAdTenantId);
        var clientId = settingsProvider.GetString(AppSettingKeys.AzureAdClientId);
        var authority = BuildAuthority(instance, tenantId);

        var bootstrap = new ChatBootstrapDto(
            Enabled: settingsProvider.GetBool(AppSettingKeys.ChatTeamsEnabled, false),
            AuthMode: settingsProvider.GetString(AppSettingKeys.ChatTeamsAuthMode, "delegated"),
            DelegatedScopes: ResolveDelegatedScopes(),
            IncludeGroupChats: settingsProvider.GetBool(AppSettingKeys.ChatTeamsIncludeGroupChats, false),
            PollingIntervalSeconds: settingsProvider.GetInt(AppSettingKeys.ChatTeamsPollingIntervalSeconds, 30),
            SignalREnabled: settingsProvider.GetBool(AppSettingKeys.ChatTeamsSignalREnabled, true),
            MsalClientId: clientId,
            MsalTenantId: tenantId,
            MsalAuthority: authority);

        return Task.FromResult(bootstrap);
    }

    public async Task<ChatStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var enabled = settingsProvider.GetBool(AppSettingKeys.ChatTeamsEnabled, false);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var linked = await graphTokenService.HasLinkedAccountAsync(personId, cancellationToken);

        return new ChatStatusDto(
            Enabled: enabled,
            Linked: linked,
            NeedsConsent: enabled && !linked);
    }

    public async Task LinkAccountAsync(
        LinkTeamsAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureChatEnabled();

        if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ArgumentException("Access token e refresh token são obrigatórios.");
        }

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await graphTokenService.StoreTokensAsync(
            personId,
            request.AccessToken.Trim(),
            request.RefreshToken.Trim(),
            request.ExpiresAt,
            request.Scopes,
            cancellationToken);
    }

    public async Task UnlinkAccountAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await graphTokenService.UnlinkAsync(personId, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatConversationDto>> GetConversationsAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureChatEnabled();
        var accessToken = await GetUserAccessTokenAsync(cancellationToken);
        var includeGroupChats = settingsProvider.GetBool(AppSettingKeys.ChatTeamsIncludeGroupChats, false);

        var chats = await teamsChatAdapter.ListChatsAsync(accessToken, includeGroupChats, cancellationToken);
        return chats.Select(MapConversation).ToList();
    }

    public async Task<PagedResult<ChatMessageDto>> GetMessagesAsync(
        string conversationId,
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureChatEnabled();

        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException("Conversation id is required.", nameof(conversationId));
        }

        var accessToken = await GetUserAccessTokenAsync(cancellationToken);
        var page = await teamsChatAdapter.ListMessagesAsync(
            accessToken,
            conversationId,
            request.Cursor,
            request.Limit,
            cancellationToken);

        var items = page.Items.Select(MapMessage).ToList();
        return PagedResult<ChatMessageDto>.FromItems(items, page.NextLink, !string.IsNullOrWhiteSpace(page.NextLink));
    }

    public async Task<ChatMessageDto> SendMessageAsync(
        string conversationId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureChatEnabled();

        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException("Conversation id is required.", nameof(conversationId));
        }

        var text = request.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Message text is required.", nameof(request));
        }

        var accessToken = await GetUserAccessTokenAsync(cancellationToken);
        var sent = await teamsChatAdapter.SendMessageAsync(accessToken, conversationId, text, cancellationToken);
        var dto = MapMessage(sent);

        if (settingsProvider.GetBool(AppSettingKeys.ChatTeamsSignalREnabled, true))
        {
            await chatBroadcaster.BroadcastMessageReceivedAsync(conversationId, dto, cancellationToken);

            var conversation = new ChatConversationDto(
                conversationId,
                null,
                null,
                dto.Author,
                dto.Text,
                dto.CreatedAt,
                0,
                null);

            await chatBroadcaster.BroadcastConversationUpdatedAsync(conversationId, conversation, cancellationToken);
        }

        return dto;
    }

    public async Task<ChatConversationDto> CreateConversationAsync(
        CreateChatConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureChatEnabled();

        var targetEmail = request.TargetEmail?.Trim();
        if (string.IsNullOrWhiteSpace(targetEmail))
        {
            throw new ArgumentException("Target email is required.", nameof(request));
        }

        var accessToken = await GetUserAccessTokenAsync(cancellationToken);
        var targetUser = await teamsChatAdapter.FindUserByEmailAsync(accessToken, targetEmail, cancellationToken)
            ?? throw new KeyNotFoundException($"Usuário {targetEmail} não encontrado no Microsoft Graph.");

        var chat = await teamsChatAdapter.CreateOneOnOneChatAsync(accessToken, targetUser.Id, cancellationToken);
        return MapConversation(chat);
    }

    private async Task<string> GetUserAccessTokenAsync(CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return await graphTokenService.GetValidAccessTokenAsync(personId, cancellationToken);
    }

    private void EnsureChatEnabled()
    {
        if (!settingsProvider.GetBool(AppSettingKeys.ChatTeamsEnabled, false))
        {
            throw new InvalidOperationException("Integração Microsoft Teams Chat está desabilitada.");
        }
    }

    private IReadOnlyList<string> ResolveDelegatedScopes()
    {
        var raw = settingsProvider.GetString(AppSettingKeys.ChatTeamsDelegatedScopes);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ["Chat.ReadWrite", "User.Read", "offline_access"];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    private static string BuildAuthority(string instance, string tenantId)
    {
        var baseUrl = instance.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return baseUrl;
        }

        return $"{baseUrl}/{tenantId.Trim()}";
    }

    private static ChatConversationDto MapConversation(TeamsChatSummary chat)
    {
        ChatParticipantDto? lastAuthor = null;
        string? preview = null;
        DateTimeOffset? lastAt = null;

        if (chat.LastMessagePreview is not null)
        {
            preview = chat.LastMessagePreview.Text;
            lastAt = chat.LastMessagePreview.CreatedAt;
            if (chat.LastMessagePreview.From is not null)
            {
                lastAuthor = MapParticipant(chat.LastMessagePreview.From);
            }
        }

        var participants = chat.Members.Count > 0
            ? chat.Members.Select(m => new ChatParticipantDto(m.Id, m.DisplayName, m.Email, null)).ToList()
            : null;

        var title = chat.Topic;
        if (string.IsNullOrWhiteSpace(title) && participants is { Count: > 0 })
        {
            title = string.Join(", ", participants.Select(p => p.DisplayName));
        }

        return new ChatConversationDto(
            chat.Id,
            title,
            chat.ChatType,
            lastAuthor,
            preview,
            lastAt,
            UnreadCount: 0,
            participants);
    }

    private static ChatMessageDto MapMessage(TeamsChatMessage message)
    {
        var author = message.From is not null
            ? MapParticipant(message.From)
            : new ChatParticipantDto("unknown", "Desconhecido", null, null);

        return new ChatMessageDto(message.Id, author, message.Text, message.CreatedAt);
    }

    private static ChatParticipantDto MapParticipant(TeamsChatIdentity identity) =>
        new(identity.Id, identity.DisplayName, identity.Email, null);
}
