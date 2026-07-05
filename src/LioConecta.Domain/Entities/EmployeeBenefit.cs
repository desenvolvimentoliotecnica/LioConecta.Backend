using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class EmployeeBenefit : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public string BenefitKey { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Desc { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool Featured { get; set; }

    public bool IsActive { get; set; } = true;

    public string? PortalUrl { get; set; }

    public string HelpText { get; set; } = string.Empty;

    public decimal? MonthlyValue { get; set; }

    public string DetailsJson { get; set; } = "{}";
}
