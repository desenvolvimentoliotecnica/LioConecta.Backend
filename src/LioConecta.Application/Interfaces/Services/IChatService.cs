using LioConecta.Application.Common;
using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IChatService
{
    Task<IReadOnlyList<ChatConversationDto>> GetConversationsAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<ChatMessageDto>> GetMessagesAsync(
        Guid conversationId,
        CursorPageRequest request,
        CancellationToken cancellationToken = default);

    Task<ChatMessageDto> SendMessageAsync(
        Guid conversationId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default);
}
