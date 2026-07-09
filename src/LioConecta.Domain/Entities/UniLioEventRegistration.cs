using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioEventRegistration : BaseEntity
{
    public Guid EventId { get; set; }

    public Guid PersonId { get; set; }

    public UniLioEvent Event { get; set; } = null!;

    public Person Person { get; set; } = null!;
}
