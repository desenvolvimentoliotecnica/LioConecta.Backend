using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class TestUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public BusinessArea BusinessArea { get; set; }

    public Guid? OptionalPersonId { get; set; }

    public Person? OptionalPerson { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? ExpiresAt { get; set; }

    public string? Notes { get; set; }

    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
}
