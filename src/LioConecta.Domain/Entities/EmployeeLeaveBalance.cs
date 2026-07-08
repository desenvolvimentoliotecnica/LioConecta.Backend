using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class EmployeeLeaveBalance : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public int AvailableDays { get; set; }

    public int AcquiredDays { get; set; }

    public int ScheduledDays { get; set; }

    public int ExpiredDays { get; set; }

    public decimal BancoHorasBalanceHours { get; set; }

    public DateOnly? NextScheduledStart { get; set; }

    public DateOnly? NextScheduledEnd { get; set; }

    public string BreakdownJson { get; set; } = "{}";

    public string? DataSource { get; set; }

    public DateTimeOffset? SyncedAt { get; set; }
}
