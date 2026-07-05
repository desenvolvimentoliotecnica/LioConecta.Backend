using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class ComunicadoRead : BaseEntity
{
    public Guid ComunicadoId { get; set; }

    public Comunicado? Comunicado { get; set; }

    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public DateTimeOffset ReadAt { get; set; }
}
