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
                "Peso financeiro por SKU (visão consolidada — sem cliente/UN).",
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
                var totals = await QueryTotalsAsync(connection, def, filters, RowFilters.Empty, cancellationToken);
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

            var rowFilters = BuildRowFilters(query);

            var totals = await QueryTotalsAsync(
                connection,
                def,
                filters,
                rowFilters,
                cancellationToken);

            var items = await QueryRowsAsync(
                connection,
                def,
                filters,
                rowFilters,
                sortBy: query.SortBy,
                sortDir: query.SortDir,
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
        RowFilters rowFilters,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT COUNT(*)::bigint,
                   COALESCE(SUM(h.amount), 0)
            {BuildFromClause(includeLookups: rowFilters.NeedsLookups)}
            WHERE {BuildWhereClause(def, rowFilters)}
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        BindParams(cmd, def, filters, rowFilters);

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
        RowFilters rowFilters,
        string? sortBy,
        string? sortDir,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT h.sku,
                   COALESCE(d.descricao, '') AS sku_description,
                   h.cliente,
                   COALESCE(NULLIF(cli.nome_abrev, ''), NULLIF(cli.razao_social, ''), '') AS cliente_nome,
                   h.ung,
                   COALESCE(NULLIF(cv.descricao, ''), uf.label, '') AS ung_nome,
                   h.entity,
                   h.amount
            {BuildFromClause(includeLookups: true)}
            WHERE {BuildWhereClause(def, rowFilters)}
            {BuildOrderBy(sortBy, sortDir)}
            OFFSET @offset LIMIT @limit
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        BindParams(cmd, def, filters, rowFilters);
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
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                reader.IsDBNull(7) ? 0m : reader.GetDecimal(7)));
        }

        return rows;
    }

    private static string BuildFromClause(bool includeLookups)
    {
        if (!includeLookups)
        {
            return "FROM public.etl_hyperion h";
        }

        return """
            FROM public.etl_hyperion h
            LEFT JOIN public.dim_item d ON d.it_codigo = REPLACE(h.sku, 'SKU_', '')
            LEFT JOIN public.dim_cliente cli
              ON cli.cod_emitente = CASE
                   WHEN h.cliente ~ '^CLI_[0-9]+$' THEN REPLACE(h.cliente, 'CLI_', '')::int
                   ELSE NULL
                 END
            LEFT JOIN public.dim_canal_venda cv
              ON cv.cod_canal_venda = CASE
                   WHEN h.ung ~ '^UN_[0-9]+$' THEN REPLACE(h.ung, 'UN_', '')::int
                   ELSE NULL
                 END
            LEFT JOIN (
                VALUES
                    ('UN_10', 'Soluções B2B'),
                    ('UN_20', 'Distribuidores Varejo'),
                    ('UN_50', 'Ingredientes Industriais'),
                    ('UN_60', 'Diretas Exportação')
            ) AS uf(code, label) ON uf.code = h.ung
            """;
    }

    private static RowFilters BuildRowFilters(CompassScenarioRowsQuery query) =>
        new(
            query.Ung,
            query.Search,
            query.Sku,
            query.SkuDescription,
            query.Cliente,
            query.UngLabel,
            query.Entity);

    private static string BuildOrderBy(string? sortBy, string? sortDir)
    {
        var dir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        var key = (sortBy ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(key))
        {
            return "ORDER BY h.amount DESC";
        }

        var column = key switch
        {
            "sku" => "h.sku",
            "skudescription" or "descricao" => "COALESCE(d.descricao, '')",
            "cliente" => "COALESCE(NULLIF(cli.nome_abrev, ''), NULLIF(cli.razao_social, ''), h.cliente)",
            "ung" => "COALESCE(NULLIF(cv.descricao, ''), uf.label, h.ung)",
            "entity" => "h.entity",
            "amount" or "valor" => "h.amount",
            _ => "h.amount",
        };

        return $"ORDER BY {column} {dir}, h.sku ASC";
    }

    private static string BuildWhereClause(ScenarioDefinition def, RowFilters rowFilters)
    {
        var clauses = new List<string>
        {
            "h.version = @version",
            "h.scenario = @scenario",
            "h.years = @years",
            "h.period = @period",
            "h.account = @account",
            "h.entity = @entity",
            "h.item = 'NA_Item'",
            "h.atributo1 = 'NA'",
            "h.sku LIKE 'SKU_%'",
            "h.amount > 0",
        };

        if (def.PesoMode)
        {
            clauses.Add("h.cliente = 'NA_Cliente'");
            clauses.Add("h.ung = 'NA_UNG'");
            clauses.Add("h.sku <> ALL(@blacklist)");
        }
        else
        {
            clauses.Add("h.cliente NOT IN ('Total_Clientes')");
            if (!string.IsNullOrWhiteSpace(rowFilters.UngFilter))
            {
                clauses.Add("h.ung = @ungFilter");
            }
            else if (def.UseVolumeUng)
            {
                clauses.Add("h.ung = ANY(@ung)");
            }
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.Search))
        {
            clauses.Add("""
                (
                    h.sku ILIKE @search
                    OR h.cliente ILIKE @search
                    OR COALESCE(d.descricao, '') ILIKE @search
                    OR COALESCE(cli.nome_abrev, '') ILIKE @search
                    OR COALESCE(cli.razao_social, '') ILIKE @search
                    OR COALESCE(cv.descricao, '') ILIKE @search
                    OR COALESCE(uf.label, '') ILIKE @search
                )
                """);
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.Sku))
        {
            clauses.Add("h.sku ILIKE @skuFilter");
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.SkuDescription))
        {
            clauses.Add("COALESCE(d.descricao, '') ILIKE @skuDescriptionFilter");
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.Cliente))
        {
            clauses.Add("""
                (
                    h.cliente ILIKE @clienteFilter
                    OR COALESCE(cli.nome_abrev, '') ILIKE @clienteFilter
                    OR COALESCE(cli.razao_social, '') ILIKE @clienteFilter
                )
                """);
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.UngLabel))
        {
            clauses.Add("""
                (
                    h.ung ILIKE @ungLabelFilter
                    OR COALESCE(cv.descricao, '') ILIKE @ungLabelFilter
                    OR COALESCE(uf.label, '') ILIKE @ungLabelFilter
                )
                """);
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.Entity))
        {
            clauses.Add("h.entity ILIKE @entityFilter");
        }

        return string.Join("\n              AND ", clauses);
    }

    private static void BindParams(
        NpgsqlCommand cmd,
        ScenarioDefinition def,
        CompassScenarioFiltersDto filters,
        RowFilters rowFilters)
    {
        AddCommonParams(cmd, filters);
        cmd.Parameters.AddWithValue("account", def.Account);
        cmd.Parameters.AddWithValue("entity", def.Entity);

        if (def.PesoMode)
        {
            cmd.Parameters.AddWithValue("blacklist", PesoFinanceiroSkuBlacklist);
        }
        else if (!string.IsNullOrWhiteSpace(rowFilters.UngFilter))
        {
            cmd.Parameters.AddWithValue("ungFilter", rowFilters.UngFilter.Trim());
        }
        else if (def.UseVolumeUng)
        {
            cmd.Parameters.AddWithValue("ung", VolumeUng);
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.Search))
        {
            cmd.Parameters.AddWithValue("search", $"%{rowFilters.Search.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.Sku))
        {
            cmd.Parameters.AddWithValue("skuFilter", $"%{rowFilters.Sku.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.SkuDescription))
        {
            cmd.Parameters.AddWithValue("skuDescriptionFilter", $"%{rowFilters.SkuDescription.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.Cliente))
        {
            cmd.Parameters.AddWithValue("clienteFilter", $"%{rowFilters.Cliente.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.UngLabel))
        {
            cmd.Parameters.AddWithValue("ungLabelFilter", $"%{rowFilters.UngLabel.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(rowFilters.Entity))
        {
            cmd.Parameters.AddWithValue("entityFilter", $"%{rowFilters.Entity.Trim()}%");
        }
    }

    private sealed record RowFilters(
        string? UngFilter,
        string? Search,
        string? Sku,
        string? SkuDescription,
        string? Cliente,
        string? UngLabel,
        string? Entity)
    {
        public static RowFilters Empty { get; } = new(null, null, null, null, null, null, null);

        public bool NeedsLookups =>
            !string.IsNullOrWhiteSpace(Search)
            || !string.IsNullOrWhiteSpace(SkuDescription)
            || !string.IsNullOrWhiteSpace(Cliente)
            || !string.IsNullOrWhiteSpace(UngLabel);
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
