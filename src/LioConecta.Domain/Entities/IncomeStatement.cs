using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class IncomeStatement : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public int Year { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal TotalWithheld { get; set; }

    public string LinesJson { get; set; } = "[]";
}
