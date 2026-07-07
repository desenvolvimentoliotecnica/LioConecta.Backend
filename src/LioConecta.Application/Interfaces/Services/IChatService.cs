using LioConecta.Application.Common;
using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IChatService
{
    Task<ChatBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default);

    Task<ChatStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task LinkAccountAsync(LinkTeamsAccountRequest request, CancellationToken cancellationToken = default);

    Task UnlinkAccountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatConversationDto>> GetConversationsAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<ChatMessageDto>> GetMessagesAsync(
        string conversationId,
        CursorPageRequest request,
        CancellationToken cancellationToken = default);

    Task<ChatMessageDto> SendMessageAsync(
        string conversationId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<ChatConversationDto> CreateConversationAsync(
        CreateChatConversationRequest request,
        CancellationToken cancellationToken = default);
}
