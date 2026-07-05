using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class ChatService(
    IChatRepository chatRepository,
    ICurrentUserService currentUserService) : IChatService
{
    public async Task<IReadOnlyList<ChatConversationDto>> GetConversationsAsync(
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var conversations = await chatRepository.GetConversationsAsync(personId, cancellationToken);

        return conversations.Select(conversation =>
        {
            var lastMessage = conversation.Messages
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            return new ChatConversationDto(
                conversation.Id,
                conversation.Title,
                lastMessage?.Author is null ? null : PersonMapper.ToSummary(lastMessage.Author),
                lastMessage?.Text,
                lastMessage?.CreatedAt,
                UnreadCount: 0);
        }).ToList();
    }

    public async Task<PagedResult<ChatMessageDto>> GetMessagesAsync(
        Guid conversationId,
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await chatRepository.GetByIdAsync(conversationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} was not found.");

        var page = await chatRepository.GetMessagesPageAsync(conversationId, request, cancellationToken);
        var items = page.Items
            .Select(message => new ChatMessageDto(
                message.Id,
                PersonMapper.ToSummary(message.Author ?? new Person { Name = "Desconhecido" }),
                message.Text,
                message.CreatedAt))
            .ToList();

        return PagedResult<ChatMessageDto>.FromItems(items, page.NextCursor, page.HasMore);
    }

    public async Task<ChatMessageDto> SendMessageAsync(
        Guid conversationId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        _ = await chatRepository.GetByIdAsync(conversationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} was not found.");

        var now = DateTimeOffset.UtcNow;
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            AuthorId = authorId,
            Text = request.Text.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await chatRepository.AddMessageAsync(message, cancellationToken);
        return new ChatMessageDto(
            message.Id,
            PersonMapper.ToSummary(new Person { Id = authorId, Name = string.Empty }),
            message.Text,
            message.CreatedAt);
    }
}
