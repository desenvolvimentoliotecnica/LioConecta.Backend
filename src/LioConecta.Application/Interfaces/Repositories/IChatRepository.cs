using LioConecta.Application.Common;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IChatRepository
{
    Task<IReadOnlyList<ChatConversation>> GetConversationsAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<ChatConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<ChatMessage>> GetMessagesPageAsync(
        Guid conversationId,
        CursorPageRequest request,
        CancellationToken cancellationToken = default);

    Task AddConversationAsync(ChatConversation conversation, CancellationToken cancellationToken = default);

    Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
}
