using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class LeaveRecord : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public string ServiceKey { get; set; } = string.Empty;

    public string RecordType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? Days { get; set; }

    public string DetailsJson { get; set; } = "{}";
}
