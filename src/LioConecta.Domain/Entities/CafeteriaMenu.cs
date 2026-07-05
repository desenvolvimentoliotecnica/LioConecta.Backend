using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class CafeteriaMenu : BaseEntity
{
    public DateOnly Date { get; set; }

    public string ItemsJson { get; set; } = "[]";
}
