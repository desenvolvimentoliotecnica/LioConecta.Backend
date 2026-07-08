using System.Globalization;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class CompassService(AppDbContext db, IAppSettingsProvider settingsProvider) : ICompassService
{
    private const string FaturamentoTipo = "Faturamento";
    private const string ContribuicaoLiquidaTipo = "Contribuição Líquida";
    private const string VolumeKgTipo = "Volume (KG)";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public Task<CompassBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var enabled = settingsProvider.GetBool(AppSettingKeys.CompassEnabled, true);
        var rolesJson = settingsProvider.GetString(
            AppSettingKeys.CompassAllowedRoles,
            "[\"Manager\",\"Admin\",\"AnalyticsViewer\"]");
        var emailsJson = settingsProvider.GetString(AppSettingKeys.CompassAllowedEmails, "[]");

        return Task.FromResult(new CompassBootstrapDto(
            enabled,
            DeserializeRoles(rolesJson),
            DeserializeEmails(emailsJson)));
    }

    public async Task<CompassMetaDto> GetMetaAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await GetActiveSnapshotAsync(cancellationToken)
            ?? throw new InvalidOperationException("Nenhum snapshot Compass IBP ativo.");

        var rows = db.CompassIbpRows.AsNoTracking().Where(r => r.SnapshotId == snapshot.Id);

        return new CompassMetaDto(
            MapSnapshot(snapshot),
            await rows.Select(r => r.Diretoria).Distinct().OrderBy(v => v).ToListAsync(cancellationToken),
            await rows.Select(r => r.Unidade).Distinct().OrderBy(v => v).ToListAsync(cancellationToken),
            await rows.Select(r => r.FamiliaComercial).Distinct().OrderBy(v => v).ToListAsync(cancellationToken),
            await rows.Select(r => r.Tipo).Distinct().OrderBy(v => v).ToListAsync(cancellationToken));
    }

    public async Task<CompassDashboardDto> GetDashboardAsync(
        CompassYtdQuery query,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetActiveSnapshotAsync(cancellationToken)
            ?? throw new InvalidOperationException("Nenhum snapshot Compass IBP ativo.");

        var filtered = ApplyFilters(db.CompassIbpRows.AsNoTracking().Where(r => r.SnapshotId == snapshot.Id), query);

        var faturamentoRows = filtered.Where(r => r.Tipo.Contains(FaturamentoTipo));
        var contribRows = filtered.Where(r => r.Tipo.Contains("Contribui") && r.Tipo.Contains("Líquida"));
        var volumeRows = filtered.Where(r => r.Tipo.Contains("Volume (KG)"));

        var totalFaturamentoAtual = await faturamentoRows.SumAsync(r => r.IbpAtual, cancellationToken);
        var totalFaturamentoAnterior = await faturamentoRows.SumAsync(r => r.IbpAnterior, cancellationToken);
        var totalFaturamentoVar = await faturamentoRows.SumAsync(r => r.Variacao, cancellationToken);

        var totalContribAtual = await contribRows.SumAsync(r => r.IbpAtual, cancellationToken);
        var totalContribAnterior = await contribRows.SumAsync(r => r.IbpAnterior, cancellationToken);
        var totalContribVar = await contribRows.SumAsync(r => r.Variacao, cancellationToken);

        var totalVolumeAtual = await volumeRows.SumAsync(r => r.IbpAtual, cancellationToken);
        var totalVolumeVar = await volumeRows.SumAsync(r => r.Variacao, cancellationToken);

        var nonZeroCount = await filtered.CountAsync(r => r.Variacao != 0, cancellationToken);

        var bridge = (await faturamentoRows
            .GroupBy(r => r.Diretoria)
            .Select(g => new CompassBridgeItemDto(
                g.Key,
                g.Sum(r => r.IbpAtual),
                g.Sum(r => r.IbpAnterior),
                g.Sum(r => r.Variacao)))
            .ToListAsync(cancellationToken))
            .OrderByDescending(b => Math.Abs(b.Variacao))
            .ToList();

        var topVariances = (await filtered
            .Where(r => r.Tipo.Contains(FaturamentoTipo))
            .OrderByDescending(r => r.Variacao < 0 ? -r.Variacao : r.Variacao)
            .Take(10)
            .Select(r => new CompassVarianceItemDto(
                r.Tipo,
                r.FamiliaComercial,
                r.SkuCode,
                r.SkuDescription,
                r.Cliente,
                r.Matriz,
                r.Diretoria,
                r.Unidade,
                r.IbpAtual,
                r.IbpAnterior,
                r.Variacao))
            .ToListAsync(cancellationToken));

        var kpis = BuildKpis(
            totalFaturamentoAtual,
            totalFaturamentoAnterior,
            totalFaturamentoVar,
            totalContribAtual,
            totalContribVar,
            totalVolumeAtual,
            totalVolumeVar,
            nonZeroCount,
            snapshot.RowCount);

        var alerts = BuildAlerts(bridge, nonZeroCount);

        return new CompassDashboardDto(MapSnapshot(snapshot), kpis, bridge, topVariances, alerts);
    }

    public async Task<CompassYtdPageDto> GetYtdPageAsync(
        CompassYtdQuery query,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetActiveSnapshotAsync(cancellationToken)
            ?? throw new InvalidOperationException("Nenhum snapshot Compass IBP ativo.");

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var filtered = ApplyFilters(db.CompassIbpRows.AsNoTracking().Where(r => r.SnapshotId == snapshot.Id), query);
        var totalCount = await filtered.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = (await filtered
            .OrderByDescending(r => r.Variacao < 0 ? -r.Variacao : r.Variacao)
            .ThenBy(r => r.Diretoria)
            .ThenBy(r => r.FamiliaComercial)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CompassIbpRowDto(
                r.Id,
                r.Tipo,
                r.FamiliaComercial,
                r.SkuCode,
                r.SkuDescription,
                r.ClienteHyperion,
                r.Cliente,
                r.Matriz,
                r.Diretoria,
                r.Unidade,
                r.IbpAtual,
                r.IbpAnterior,
                r.Variacao))
            .ToListAsync(cancellationToken));

        return new CompassYtdPageDto(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<CompassAggregatesDto> GetAggregatesAsync(
        CompassAggregatesQuery query,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetActiveSnapshotAsync(cancellationToken)
            ?? throw new InvalidOperationException("Nenhum snapshot Compass IBP ativo.");

        var ytdQuery = new CompassYtdQuery(
            query.Diretoria,
            query.Unidade,
            query.Familia,
            query.Tipo,
            query.Search);

        var filtered = ApplyFilters(db.CompassIbpRows.AsNoTracking().Where(r => r.SnapshotId == snapshot.Id), ytdQuery);

        var groupBy = query.GroupBy.Trim().ToLowerInvariant();
        var items = groupBy switch
        {
            "familia" => await GroupAggregate(filtered, r => r.FamiliaComercial, cancellationToken),
            "tipo" => await GroupAggregate(filtered, r => r.Tipo, cancellationToken),
            "unidade" => await GroupAggregate(filtered, r => r.Unidade, cancellationToken),
            "matriz" => await GroupAggregate(filtered, r => r.Matriz, cancellationToken),
            _ => await GroupAggregate(filtered, r => r.Diretoria, cancellationToken),
        };

        return new CompassAggregatesDto(groupBy, items);
    }

    private static async Task<IReadOnlyList<CompassAggregateRowDto>> GroupAggregate(
        IQueryable<CompassIbpRow> filtered,
        System.Linq.Expressions.Expression<Func<CompassIbpRow, string>> keySelector,
        CancellationToken cancellationToken)
    {
        return (await filtered
            .GroupBy(keySelector)
            .Select(g => new CompassAggregateRowDto(
                g.Key,
                g.Sum(r => r.IbpAtual),
                g.Sum(r => r.IbpAnterior),
                g.Sum(r => r.Variacao),
                g.Count()))
            .ToListAsync(cancellationToken))
            .OrderByDescending(r => Math.Abs(r.Variacao))
            .ToList();
    }

    private async Task<CompassIbpSnapshot?> GetActiveSnapshotAsync(CancellationToken cancellationToken)
    {
        return await db.CompassIbpSnapshots
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.ImportedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<CompassIbpRow> ApplyFilters(IQueryable<CompassIbpRow> queryable, CompassYtdQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Diretoria))
        {
            queryable = queryable.Where(r => r.Diretoria == query.Diretoria);
        }

        if (!string.IsNullOrWhiteSpace(query.Unidade))
        {
            queryable = queryable.Where(r => r.Unidade == query.Unidade);
        }

        if (!string.IsNullOrWhiteSpace(query.Familia))
        {
            queryable = queryable.Where(r => r.FamiliaComercial == query.Familia);
        }

        if (!string.IsNullOrWhiteSpace(query.Tipo))
        {
            queryable = queryable.Where(r => r.Tipo == query.Tipo);
        }

        if (query.OnlyNonZero)
        {
            queryable = queryable.Where(r => r.Variacao != 0);
        }

        if (query.MinVariacao.HasValue)
        {
            var min = query.MinVariacao.Value;
            queryable = queryable.Where(r => r.Variacao >= min || r.Variacao <= -min);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            queryable = queryable.Where(r =>
                EF.Functions.ILike(r.SkuCode, pattern) ||
                EF.Functions.ILike(r.SkuDescription, pattern) ||
                EF.Functions.ILike(r.Cliente, pattern) ||
                EF.Functions.ILike(r.Matriz, pattern) ||
                EF.Functions.ILike(r.ClienteHyperion, pattern));
        }

        return queryable;
    }

    private static CompassSnapshotDto MapSnapshot(CompassIbpSnapshot snapshot) =>
        new(
            snapshot.Id,
            snapshot.Label,
            snapshot.VersionAtual,
            snapshot.VersionAnterior,
            snapshot.SourceSystem,
            snapshot.ImportedAt,
            snapshot.RowCount);

    private static CompassVarianceItemDto MapVarianceItem(CompassIbpRow r) =>
        new(
            r.Tipo,
            r.FamiliaComercial,
            r.SkuCode,
            r.SkuDescription,
            r.Cliente,
            r.Matriz,
            r.Diretoria,
            r.Unidade,
            r.IbpAtual,
            r.IbpAnterior,
            r.Variacao);

    private static IReadOnlyList<CompassKpiDto> BuildKpis(
        decimal fatAtual,
        decimal fatAnterior,
        decimal fatVar,
        decimal contribAtual,
        decimal contribVar,
        decimal volAtual,
        decimal volVar,
        int nonZeroCount,
        int rowCount)
    {
        var fatDeltaPct = fatAnterior == 0 ? 0 : (fatVar / fatAnterior) * 100;

        return
        [
            new("revenue", "Faturamento Líquido YTD", FormatCurrency(fatAtual), FormatDelta(fatVar, fatDeltaPct), Trend(fatVar), "fa-coins", "blue"),
            new("contrib", "Contribuição Líquida YTD", FormatCurrency(contribAtual), FormatDelta(contribVar), Trend(contribVar), "fa-chart-line", "green"),
            new("volume", "Volume (KG) YTD", FormatNumber(volAtual), FormatDelta(volVar), Trend(volVar), "fa-weight-hanging", "purple"),
            new("variance_rows", "Linhas c/ Variação", nonZeroCount.ToString(CultureInfo.InvariantCulture), $"{Pct(nonZeroCount, rowCount)}%", "neutral", "fa-arrows-left-right", "amber"),
            new("total_rows", "Linhas no Snapshot", rowCount.ToString(CultureInfo.InvariantCulture), "Hyperion", "neutral", "fa-database", "blue"),
            new("source", "Origem", "Hyperion", "EPBCS", "neutral", "fa-compass", "blue"),
        ];
    }

    private static IReadOnlyList<CompassAlertDto> BuildAlerts(
        IReadOnlyList<CompassBridgeItemDto> bridge,
        int nonZeroCount)
    {
        var alerts = new List<CompassAlertDto>();

        foreach (var item in bridge.Where(b => Math.Abs(b.Variacao) >= 1_000_000).Take(3))
        {
            alerts.Add(new CompassAlertDto(
                $"bridge-{item.Diretoria}",
                item.Variacao >= 0 ? "info" : "warning",
                $"Variação relevante — {item.Diretoria}",
                $"Faturamento Líquido variou {FormatCurrency(item.Variacao)} vs IBP anterior.",
                1,
                "/compass/reconciliacao"));
        }

        if (nonZeroCount > 0)
        {
            alerts.Add(new CompassAlertDto(
                "nonzero",
                "info",
                "Linhas com variação",
                $"{nonZeroCount} combinações SKU×cliente×métrica com delta ≠ 0.",
                nonZeroCount,
                "/compass/analise-ytd"));
        }

        return alerts;
    }

    private static string Trend(decimal value) => value switch
    {
        > 0 => "up",
        < 0 => "down",
        _ => "neutral",
    };

    private static string FormatCurrency(decimal value) =>
        value.ToString("C0", new CultureInfo("pt-BR"));

    private static string FormatNumber(decimal value) =>
        value.ToString("N0", new CultureInfo("pt-BR"));

    private static string FormatDelta(decimal value, decimal? pct = null)
    {
        var sign = value >= 0 ? "+" : "";
        var baseText = $"{sign}{FormatCurrency(value)}";
        return pct.HasValue ? $"{baseText} ({sign}{pct.Value:F1}%)" : baseText;
    }

    private static string Pct(int part, int total) =>
        total == 0 ? "0" : ((part / (double)total) * 100).ToString("F1", CultureInfo.InvariantCulture);

    private static IReadOnlyList<string> DeserializeRoles(string raw)
    {
        try
        {
            var values = JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
            var roles = new List<string>();
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (Enum.TryParse<UserRole>(value.Trim(), true, out _))
                {
                    roles.Add(value.Trim());
                }
            }

            return roles.Count > 0 ? roles : ["Manager", "Admin", "AnalyticsViewer"];
        }
        catch
        {
            return ["Manager", "Admin", "AnalyticsViewer"];
        }
    }

    private static IReadOnlyList<string> DeserializeEmails(string raw)
    {
        try
        {
            var values = JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
