using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Application.Services;

public sealed class NoOpChatBroadcaster : IChatBroadcaster
{
    public Task BroadcastMessageReceivedAsync(
        string conversationId,
        ChatMessageDto message,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task BroadcastConversationUpdatedAsync(
        string conversationId,
        ChatConversationDto conversation,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
