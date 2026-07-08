using System.Text.Json;
using System.Text.Json.Serialization;
using LioConecta.Domain.Entities;

namespace LioConecta.Infrastructure.Seed;

internal sealed class LegacyPhoneExtensionSeedRow
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("nome")] public string Nome { get; set; } = string.Empty;
    [JsonPropertyName("ramal")] public string Ramal { get; set; } = string.Empty;
    [JsonPropertyName("celular")] public string? Celular { get; set; }
    [JsonPropertyName("departamento")] public string Departamento { get; set; } = string.Empty;
    [JsonPropertyName("cargo")] public string? Cargo { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("gestor")] public string? Gestor { get; set; }
}

internal static class PhoneExtensionsCatalogSeed
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<LegacyPhoneExtensionSeedRow> LoadRows()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Seed", "Data", "phone-extensions-seed.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Seed", "Data", "phone-extensions-seed.json")),
        };
        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<LegacyPhoneExtensionSeedRow>>(json, JsonOptions) ?? [];
        }
        return [];
    }

    public static PhoneExtension ToEntity(LegacyPhoneExtensionSeedRow row, Guid? personId, DateTimeOffset seedTime) => new()
    {
        Id = Guid.NewGuid(),
        Name = row.Nome?.Trim() ?? string.Empty,
        Extension = row.Ramal?.Trim() ?? string.Empty,
        Mobile = string.IsNullOrWhiteSpace(row.Celular) ? null : row.Celular.Trim(),
        Department = row.Departamento?.Trim() ?? string.Empty,
        Title = string.IsNullOrWhiteSpace(row.Cargo) ? null : row.Cargo.Trim(),
        Email = string.IsNullOrWhiteSpace(row.Email) ? null : row.Email.Trim().ToLowerInvariant(),
        ManagerName = string.IsNullOrWhiteSpace(row.Gestor) ? null : row.Gestor.Trim(),
        PersonId = personId,
        IsActive = true,
        LegacySourceId = row.Id,
        CreatedAt = seedTime,
        UpdatedAt = seedTime,
    };
}