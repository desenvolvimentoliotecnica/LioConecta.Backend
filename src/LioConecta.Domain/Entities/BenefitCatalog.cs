using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class BenefitCatalog : BaseEntity
{
    public string CatalogKey { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Desc { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool Featured { get; set; }

    public bool IsActive { get; set; } = true;

    public string? PortalUrl { get; set; }

    public string HelpText { get; set; } = string.Empty;

    public decimal? DefaultMonthlyValue { get; set; }

    public int SortOrder { get; set; }

    /// <summary>
    /// Template JSON ({ lines, dependents, notes }) herdado por novos vínculos de colaborador.
    /// </summary>
    public string DefaultDetailsJson { get; set; } = "{}";
}
