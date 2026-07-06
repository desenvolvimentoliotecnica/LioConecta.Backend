using LioConecta.Application.Common;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LioConecta.Infrastructure.Configuration;

public static class BootstrapConnection
{
    public const string EnvVarName = "LIOSNECTA_BOOTSTRAP_DB";
    public const string LegacyEnvVarName = "ConnectionStrings__DefaultConnection";

    public static string Resolve(string? fallback = null)
    {
        var fromEnv = Environment.GetEnvironmentVariable(EnvVarName)
            ?? Environment.GetEnvironmentVariable(LegacyEnvVarName);

        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback.Trim();
        }

        throw new InvalidOperationException(
            $"Configure {EnvVarName} or {LegacyEnvVarName} environment variable for the initial database connection.");
    }
}

public sealed class AppSettingsSeeder(AppDbContext db)
{
    public async Task EnsureDefaultsAsync(string bootstrapConnection, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var toAdd = new List<AppSetting>();
        var toUpdate = new List<AppSetting>();

        var existingRows = await db.AppSettings.ToListAsync(cancellationToken);
        var rowsByKey = existingRows.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var def in AppSettingCatalog.All)
        {
            if (!rowsByKey.TryGetValue(def.Key, out var row))
            {
                var defaultValue = def.Key == AppSettingKeys.DatabaseDefaultConnection
                    ? bootstrapConnection
                    : def.DefaultValue;

                toAdd.Add(new AppSetting
                {
                    Id = Guid.NewGuid(),
                    Key = def.Key,
                    Category = def.Category,
                    Label = def.Label,
                    Description = def.Description,
                    Value = defaultValue,
                    ValueType = def.ValueType,
                    IsSecret = def.IsSecret,
                    SortOrder = def.SortOrder,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                continue;
            }

            if (row.Label != def.Label
                || row.Description != def.Description
                || row.SortOrder != def.SortOrder
                || row.ValueType != def.ValueType
                || row.IsSecret != def.IsSecret
                || row.Category != def.Category)
            {
                row.Label = def.Label;
                row.Description = def.Description;
                row.SortOrder = def.SortOrder;
                row.ValueType = def.ValueType;
                row.IsSecret = def.IsSecret;
                row.Category = def.Category;
                row.UpdatedAt = now;
                toUpdate.Add(row);
            }
        }

        if (toAdd.Count == 0 && toUpdate.Count == 0)
        {
            return;
        }

        if (toAdd.Count > 0)
        {
            db.AppSettings.AddRange(toAdd);
        }

        if (toUpdate.Count > 0)
        {
            db.AppSettings.UpdateRange(toUpdate);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task<IReadOnlyDictionary<string, string>> LoadValuesAsync(
        string bootstrapConnection,
        CancellationToken cancellationToken = default)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(bootstrapConnection);

        await using var db = new AppDbContext(optionsBuilder.Options);
        await db.Database.MigrateAsync(cancellationToken);

        var seeder = new AppSettingsSeeder(db);

        try
        {
            await seeder.EnsureDefaultsAsync(bootstrapConnection, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return BuildDefaultValues(bootstrapConnection);
        }

        var rows = await db.AppSettings.AsNoTracking().ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> BuildDefaultValues(string bootstrapConnection)
    {
        return AppSettingCatalog.All.ToDictionary(
            d => d.Key,
            d => d.Key == AppSettingKeys.DatabaseDefaultConnection ? bootstrapConnection : d.DefaultValue,
            StringComparer.OrdinalIgnoreCase);
    }
}
