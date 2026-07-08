using System.Text.Json;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Seed;

namespace LioConecta.Infrastructure.Seed;

internal sealed record CompassIbpRowSeed(
    string Tipo,
    string FamiliaComercial,
    string SkuCode,
    string SkuDescription,
    string ClienteHyperion,
    string Cliente,
    string Matriz,
    string Diretoria,
    string Unidade,
    decimal IbpAtual,
    decimal IbpAnterior,
    decimal Variacao);

internal sealed record CompassIbpSnapshotSeed(
    string Label,
    string VersionAtual,
    string VersionAnterior,
    string SourceSystem,
    int RowCount);

internal sealed record CompassIbpSeedFile(
    CompassIbpSnapshotSeed Snapshot,
    IReadOnlyList<CompassIbpRowSeed> Rows);

internal static class CompassCatalogSeed
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static CompassIbpSeedFile LoadFromJson()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Seed", "Data", "compass-ibp-sample.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Seed", "Data", "compass-ibp-sample.json")),
        };

        var path = candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("Compass IBP seed file not found.", candidates[0]);

        var json = File.ReadAllText(path);
        var payload = JsonSerializer.Deserialize<CompassIbpSeedFile>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize compass IBP seed file.");

        return payload;
    }

    public static CompassIbpSnapshot ToSnapshotEntity(CompassIbpSnapshotSeed seed, DateTimeOffset seedTime)
    {
        return new CompassIbpSnapshot
        {
            Id = SeedIds.CompassIbpSnapshotJul2026,
            Label = seed.Label,
            VersionAtual = seed.VersionAtual,
            VersionAnterior = seed.VersionAnterior,
            SourceSystem = seed.SourceSystem,
            ImportedAt = seedTime,
            RowCount = seed.RowCount,
            IsActive = true,
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };
    }

    public static IEnumerable<CompassIbpRow> ToRowEntities(
        Guid snapshotId,
        IReadOnlyList<CompassIbpRowSeed> rows,
        DateTimeOffset seedTime)
    {
        foreach (var row in rows)
        {
            yield return new CompassIbpRow
            {
                Id = Guid.NewGuid(),
                SnapshotId = snapshotId,
                Tipo = row.Tipo,
                FamiliaComercial = row.FamiliaComercial,
                SkuCode = row.SkuCode,
                SkuDescription = row.SkuDescription,
                ClienteHyperion = row.ClienteHyperion,
                Cliente = row.Cliente,
                Matriz = row.Matriz,
                Diretoria = row.Diretoria,
                Unidade = row.Unidade,
                IbpAtual = row.IbpAtual,
                IbpAnterior = row.IbpAnterior,
                Variacao = row.Variacao,
                CreatedAt = seedTime,
                UpdatedAt = seedTime,
            };
        }
    }
}
