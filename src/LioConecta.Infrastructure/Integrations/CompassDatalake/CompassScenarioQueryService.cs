using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LioConecta.Infrastructure.Integrations.CompassDatalake;

public sealed class CompassScenarioQueryService(
    ICompassDatalakeConnectionFactory connectionFactory,
    ILogger<CompassScenarioQueryService> logger) : ICompassScenarioQueryService
{
    private static readonly string[] VolumeUng =
    [
        "UN_5", "UN_6", "UN_7", "UN_10", "UN_11", "UN_20", "UN_50", "UN_51", "UN_60",
    ];

    private static readonly string[] PesoFinanceiroSkuBlacklist =
    [
        "SKU_120070781", "SKU_120520114", "SKU_120070777", "SKU_120070779", "SKU_196980008",
        "SKU_120109293", "SKU_120021009", "SKU_120044005", "SKU_120044051", "SKU_120044106",
        "SKU_120044135", "SKU_120044155", "SKU_120047019", "SKU_120044183", "SKU_120047027",
        "SKU_120109434", "SKU_120014049", "SKU_120109055", "SKU_120070730", "SKU_120108916",
        "SKU_120109231", "SKU_120044170", "SKU_120109143", "SKU_120048007",
    ];

    private static readonly IReadOnlyDictionary<string, ScenarioDefinition> Definitions =
        new Dictionary<string, ScenarioDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["volume-toneladas"] = new(
                "volume-toneladas",
                "Volume_Toneladas",
                "Volume (t)",
                "Volume em toneladas por SKU e cliente nas unidades comerciais.",
                "Total_CC",
                UseVolumeUng: true,
                PesoMode: false),
            ["volume-qtde-vendas"] = new(
                "volume-qtde-vendas",
                "Volume_Qtde_Vendas",
                "Volume (unidades)",
                "Volume em unidades de venda por SKU e cliente.",
                "Global_CC",
                UseVolumeUng: true,
                PesoMode: false),
            ["peso-financeiro"] = new(
                "peso-financeiro",
                "Peso_Financeiro",
                "Peso financeiro",
                "Peso financeiro por SKU (visão consolidada, sem cliente).",
                "Global_CC",
                UseVolumeUng: false,
                PesoMode: true),
        };

    public async Task<CompassScenariosDto> GetScenariosAsync(
        CompassScenariosQuery query,
        CancellationToken cancellationToken = default)
    {
        var filters = BuildFilters(query.Version, query.Scenario, query.Years, query.Period);

        if (!connectionFactory.IsConfigured)
        {
            return new CompassScenariosDto(
                false,
                connectionFactory.ConfigurationMessage,
                filters,
                []);
        }

        await using var connection = connectionFactory.CreateConnection()
            ?? throw new InvalidOperationException("Falha ao criar conexão com o Datalake.");

        try
        {
            await connection.OpenAsync(cancellationToken);

            var items = new List<CompassScenarioItemDto>(Definitions.Count);
            foreach (var def in Definitions.Values)
            {
                var totals = await QueryTotalsAsync(connection, def, filters, ungFilter: null, search: null, cancellationToken);
                items.Add(new CompassScenarioItemDto(
                    def.Id,
                    def.Account,
                    def.Name,
                    def.Description,
                    totals.RowCount,
                    totals.TotalAmount,
                    "ativo"));
            }

            return new CompassScenariosDto(true, null, filters, items);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao consultar cenários Compass no Datalake");
            return new CompassScenariosDto(
                false,
                $"Não foi possível consultar o Datalake: {ex.Message}",
                filters,
                []);
        }
    }

    public async Task<CompassScenarioRowsPageDto> GetScenarioRowsAsync(
        string scenarioId,
        CompassScenarioRowsQuery query,
        CancellationToken cancellationToken = default)
    {
        var filters = BuildFilters(query.Version, query.Scenario, query.Years, query.Period);
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 200 ? 25 : query.PageSize;

        if (!Definitions.TryGetValue(scenarioId.Trim(), out var def))
        {
            return EmptyRowsPage(scenarioId, filters, page, pageSize, configured: true, "Cenário não encontrado.");
        }

        if (!connectionFactory.IsConfigured)
        {
            return EmptyRowsPage(def.Id, filters, page, pageSize, configured: false, connectionFactory.ConfigurationMessage);
        }

        await using var connection = connectionFactory.CreateConnection()
            ?? throw new InvalidOperationException("Falha ao criar conexão com o Datalake.");

        try
        {
            await connection.OpenAsync(cancellationToken);

            var totals = await QueryTotalsAsync(
                connection,
                def,
                filters,
                ungFilter: query.Ung,
                search: query.Search,
                cancellationToken);

            var items = await QueryRowsAsync(
                connection,
                def,
                filters,
                ungFilter: query.Ung,
                search: query.Search,
                offset: (page - 1) * pageSize,
                limit: pageSize,
                cancellationToken);

            var totalPages = totals.RowCount == 0
                ? 0
                : (int)Math.Ceiling(totals.RowCount / (double)pageSize);

            return new CompassScenarioRowsPageDto(
                def.Id,
                def.Account,
                def.Name,
                true,
                null,
                filters,
                items,
                page,
                pageSize,
                totals.RowCount,
                totals.TotalAmount,
                totalPages);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao consultar linhas do cenário {ScenarioId}", scenarioId);
            return EmptyRowsPage(
                def.Id,
                filters,
                page,
                pageSize,
                configured: false,
                $"Não foi possível consultar o Datalake: {ex.Message}");
        }
    }

    private static CompassScenarioRowsPageDto EmptyRowsPage(
        string scenarioId,
        CompassScenarioFiltersDto filters,
        int page,
        int pageSize,
        bool configured,
        string? message)
    {
        var name = Definitions.TryGetValue(scenarioId, out var def) ? def.Name : scenarioId;
        var account = Definitions.TryGetValue(scenarioId, out var d2) ? d2.Account : string.Empty;
        return new CompassScenarioRowsPageDto(
            scenarioId,
            account,
            name,
            configured,
            message,
            filters,
            [],
            page,
            pageSize,
            0,
            0m,
            0);
    }

    private static async Task<(long RowCount, decimal TotalAmount)> QueryTotalsAsync(
        NpgsqlConnection connection,
        ScenarioDefinition def,
        CompassScenarioFiltersDto filters,
        string? ungFilter,
        string? search,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT COUNT(*)::bigint,
                   COALESCE(SUM(amount), 0)
            FROM public.etl_hyperion
            WHERE {BuildWhereClause(def, ungFilter, search)}
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        BindParams(cmd, def, filters, ungFilter, search);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return (0, 0m);
        }

        return (reader.GetInt64(0), reader.GetDecimal(1));
    }

    private static async Task<IReadOnlyList<CompassScenarioRowDto>> QueryRowsAsync(
        NpgsqlConnection connection,
        ScenarioDefinition def,
        CompassScenarioFiltersDto filters,
        string? ungFilter,
        string? search,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT sku, cliente, ung, entity, amount
            FROM public.etl_hyperion
            WHERE {BuildWhereClause(def, ungFilter, search)}
            ORDER BY amount DESC
            OFFSET @offset LIMIT @limit
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        BindParams(cmd, def, filters, ungFilter, search);
        cmd.Parameters.AddWithValue("offset", offset);
        cmd.Parameters.AddWithValue("limit", limit);

        var rows = new List<CompassScenarioRowDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new CompassScenarioRowDto(
                reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                reader.IsDBNull(4) ? 0m : reader.GetDecimal(4)));
        }

        return rows;
    }

    private static string BuildWhereClause(ScenarioDefinition def, string? ungFilter, string? search)
    {
        var clauses = new List<string>
        {
            "version = @version",
            "scenario = @scenario",
            "years = @years",
            "period = @period",
            "account = @account",
            "entity = @entity",
            "item = 'NA_Item'",
            "atributo1 = 'NA'",
            "sku LIKE 'SKU_%'",
            "amount > 0",
        };

        if (def.PesoMode)
        {
            clauses.Add("cliente = 'NA_Cliente'");
            clauses.Add("ung = 'NA_UNG'");
            clauses.Add("sku <> ALL(@blacklist)");
        }
        else
        {
            clauses.Add("cliente NOT IN ('Total_Clientes')");
            if (!string.IsNullOrWhiteSpace(ungFilter))
            {
                clauses.Add("ung = @ungFilter");
            }
            else if (def.UseVolumeUng)
            {
                clauses.Add("ung = ANY(@ung)");
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            clauses.Add("(sku ILIKE @search OR cliente ILIKE @search)");
        }

        return string.Join("\n              AND ", clauses);
    }

    private static void BindParams(
        NpgsqlCommand cmd,
        ScenarioDefinition def,
        CompassScenarioFiltersDto filters,
        string? ungFilter,
        string? search)
    {
        AddCommonParams(cmd, filters);
        cmd.Parameters.AddWithValue("account", def.Account);
        cmd.Parameters.AddWithValue("entity", def.Entity);

        if (def.PesoMode)
        {
            cmd.Parameters.AddWithValue("blacklist", PesoFinanceiroSkuBlacklist);
        }
        else if (!string.IsNullOrWhiteSpace(ungFilter))
        {
            cmd.Parameters.AddWithValue("ungFilter", ungFilter.Trim());
        }
        else if (def.UseVolumeUng)
        {
            cmd.Parameters.AddWithValue("ung", VolumeUng);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            cmd.Parameters.AddWithValue("search", $"%{search.Trim()}%");
        }
    }

    private static void AddCommonParams(NpgsqlCommand cmd, CompassScenarioFiltersDto filters)
    {
        cmd.Parameters.AddWithValue("version", filters.Version);
        cmd.Parameters.AddWithValue("scenario", filters.Scenario);
        cmd.Parameters.AddWithValue("years", filters.Years);
        cmd.Parameters.AddWithValue("period", filters.Period);
    }

    private static CompassScenarioFiltersDto BuildFilters(
        string? version,
        string? scenario,
        string? years,
        string? period)
        => new(
            Normalize(version, "Oficial"),
            Normalize(scenario, "Orcado"),
            Normalize(years, "FY26"),
            Normalize(period, "Jan"));

    private static string Normalize(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private sealed record ScenarioDefinition(
        string Id,
        string Account,
        string Name,
        string Description,
        string Entity,
        bool UseVolumeUng,
        bool PesoMode);
}
