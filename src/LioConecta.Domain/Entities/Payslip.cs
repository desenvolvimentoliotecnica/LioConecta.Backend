using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class Payslip : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal NetAmount { get; set; }

    public decimal DeductionsTotal { get; set; }

    public string EarningsJson { get; set; } = "[]";

    public string DeductionsJson { get; set; } = "[]";

    public DateTimeOffset PublishedAt { get; set; }
}
