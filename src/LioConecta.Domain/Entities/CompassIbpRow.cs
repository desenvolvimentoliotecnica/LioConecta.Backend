using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class CompassIbpRow : BaseEntity
{
    public Guid SnapshotId { get; set; }

    public CompassIbpSnapshot Snapshot { get; set; } = null!;

    public string Tipo { get; set; } = string.Empty;

    public string FamiliaComercial { get; set; } = string.Empty;

    public string SkuCode { get; set; } = string.Empty;

    public string SkuDescription { get; set; } = string.Empty;

    public string ClienteHyperion { get; set; } = string.Empty;

    public string Cliente { get; set; } = string.Empty;

    public string Matriz { get; set; } = string.Empty;

    public string Diretoria { get; set; } = string.Empty;

    public string Unidade { get; set; } = string.Empty;

    public decimal IbpAtual { get; set; }

    public decimal IbpAnterior { get; set; }

    public decimal Variacao { get; set; }
}
