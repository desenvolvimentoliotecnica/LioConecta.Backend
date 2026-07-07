using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IChatBroadcaster
{
    Task BroadcastMessageReceivedAsync(
        string conversationId,
        ChatMessageDto message,
        CancellationToken cancellationToken = default);

    Task BroadcastConversationUpdatedAsync(
        string conversationId,
        ChatConversationDto conversation,
        CancellationToken cancellationToken = default);
}
