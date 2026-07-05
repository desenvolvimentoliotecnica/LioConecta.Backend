using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class ChatConversation : BaseEntity
{
    public string? Title { get; set; }

    public Guid CreatedById { get; set; }

    public Person? CreatedBy { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = [];
}
