using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LioConecta.Api.Services;

public sealed class SignalRChatBroadcaster(IHubContext<ChatHub> hubContext) : IChatBroadcaster
{
    public Task BroadcastMessageReceivedAsync(
        string conversationId,
        ChatMessageDto message,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group($"conversation:{conversationId}")
            .SendAsync("ChatMessageReceived", message, cancellationToken);

    public Task BroadcastConversationUpdatedAsync(
        string conversationId,
        ChatConversationDto conversation,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group($"conversation:{conversationId}")
            .SendAsync("ChatConversationUpdated", conversation, cancellationToken);
}
