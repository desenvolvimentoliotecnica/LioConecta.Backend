using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class CompassIbpSnapshot : BaseEntity
{
    public string Label { get; set; } = string.Empty;

    public string VersionAtual { get; set; } = string.Empty;

    public string VersionAnterior { get; set; } = string.Empty;

    public string SourceSystem { get; set; } = "Hyperion";

    public DateTimeOffset ImportedAt { get; set; }

    public int RowCount { get; set; }

    public bool IsActive { get; set; }

    public ICollection<CompassIbpRow> Rows { get; set; } = [];
}
