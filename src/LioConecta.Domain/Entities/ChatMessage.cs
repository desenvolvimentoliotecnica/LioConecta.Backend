using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid ConversationId { get; set; }

    public ChatConversation? Conversation { get; set; }

    public Guid AuthorId { get; set; }

    public Person? Author { get; set; }

    public string Text { get; set; } = string.Empty;
}
