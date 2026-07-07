using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class CafeteriaMenu : BaseEntity
{
    public DateOnly Date { get; set; }

    /// <summary>Legado — lista simples de itens. Preferir <see cref="PayloadJson"/>.</summary>
    public string ItemsJson { get; set; } = "[]";

    public string PayloadJson { get; set; } = "{}";

    public bool Published { get; set; }

    public Guid? UpdatedById { get; set; }

    public Person? UpdatedBy { get; set; }
}
