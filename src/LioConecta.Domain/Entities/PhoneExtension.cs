using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class PhoneExtension : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? ManagerName { get; set; }
    public Guid? PersonId { get; set; }
    public Person? Person { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Id do registro no app legacy de ramais (idempotencia do seed).</summary>
    public int? LegacySourceId { get; set; }
}