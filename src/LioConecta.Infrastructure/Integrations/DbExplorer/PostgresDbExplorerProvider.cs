using LioConecta.Application.Common;
using LioConecta.Application.Common.DbExplorer;
using LioConecta.Application.DTOs.DbExplorer;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LioConecta.Infrastructure.Integrations.DbExplorer;

public sealed class PostgresDbExplorerProvider(
    IAppSettingsProvider settings,
    ILogger<PostgresDbExplorerProvider> logger) : IDbExplorerProvider
{
    public string ConnectionId => DbExplorerCatalog.PostgresConnectionId;

    public string Engine => "PostgreSQL";

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PostgreSQL explorer connection test failed.");
            return false;
        }
    }

    public Task<IReadOnlyList<DbSchemaDto>> GetSchemasAsync(CancellationToken cancellationToken) =>
        QueryListAsync(
            """
            SELECT schema_name
            FROM information_schema.schemata
            WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
              AND schema_name NOT LIKE 'pg_temp_%'
              AND schema_name NOT LIKE 'pg_toast_temp_%'
            ORDER BY schema_name
            """,
            reader => new DbSchemaDto(reader.GetString(0)),
            cancellationToken);

    public Task<IReadOnlyList<DbTableDto>> GetTablesAsync(string schema, CancellationToken cancellationToken)
    {
        SqlReadOnlyValidator.ValidateIdentifier(schema, "schema");
        return QueryListAsync(
            """
            SELECT table_schema, table_name, table_type
            FROM information_schema.tables
            WHERE table_schema = @schema
              AND table_type IN ('BASE TABLE', 'VIEW')
            ORDER BY table_name
            """,
            reader => new DbTableDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2) == "VIEW" ? "view" : "table"),
            cancellationToken,
            new NpgsqlParameter("schema", schema));
    }

    public Task<IReadOnlyList<DbColumnDto>> GetColumnsAsync(
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        SqlReadOnlyValidator.ValidateIdentifier(schema, "schema");
        SqlReadOnlyValidator.ValidateIdentifier(table, "table");
        return QueryListAsync(
            """
            SELECT
                c.column_name,
                c.data_type,
                c.is_nullable = 'YES',
                EXISTS (
                    SELECT 1 FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                      ON tc.constraint_name = kcu.constraint_name
                     AND tc.table_schema = kcu.table_schema
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                      AND tc.table_schema = c.table_schema
                      AND tc.table_name = c.table_name
                      AND kcu.column_name = c.column_name
                ),
                EXISTS (
                    SELECT 1 FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                      ON tc.constraint_name = kcu.constraint_name
                     AND tc.table_schema = kcu.table_schema
                    WHERE tc.constraint_type = 'FOREIGN KEY'
                      AND tc.table_schema = c.table_schema
                      AND tc.table_name = c.table_name
                      AND kcu.column_name = c.column_name
                ),
                (
                    SELECT ccu.table_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                      ON tc.constraint_name = kcu.constraint_name
                     AND tc.table_schema = kcu.table_schema
                    JOIN information_schema.constraint_column_usage ccu
                      ON ccu.constraint_name = tc.constraint_name
                     AND ccu.table_schema = tc.table_schema
                    WHERE tc.constraint_type = 'FOREIGN KEY'
                      AND tc.table_schema = c.table_schema
                      AND tc.table_name = c.table_name
                      AND kcu.column_name = c.column_name
                    LIMIT 1
                ),
                (
                    SELECT ccu.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                      ON tc.constraint_name = kcu.constraint_name
                     AND tc.table_schema = kcu.table_schema
                    JOIN information_schema.constraint_column_usage ccu
                      ON ccu.constraint_name = tc.constraint_name
                     AND ccu.table_schema = tc.table_schema
                    WHERE tc.constraint_type = 'FOREIGN KEY'
                      AND tc.table_schema = c.table_schema
                      AND tc.table_name = c.table_name
                      AND kcu.column_name = c.column_name
                    LIMIT 1
                )
            FROM information_schema.columns c
            WHERE c.table_schema = @schema AND c.table_name = @table
            ORDER BY c.ordinal_position
            """,
            reader => new DbColumnDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetBoolean(2),
                reader.GetBoolean(3),
                reader.GetBoolean(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6)),
            cancellationToken,
            new NpgsqlParameter("schema", schema),
            new NpgsqlParameter("table", table));
    }

    public async Task<DbRowsPageDto> GetRowsAsync(
        string schema,
        string table,
        int page,
        int pageSize,
        string? orderBy,
        string? search,
        CancellationToken cancellationToken)
    {
        SqlReadOnlyValidator.ValidateIdentifier(schema, "schema");
        SqlReadOnlyValidator.ValidateIdentifier(table, "table");
        if (!string.IsNullOrWhiteSpace(orderBy))
        {
            SqlReadOnlyValidator.ValidateIdentifier(orderBy, "orderBy");
        }

        var qualified = $"\"{schema}\".\"{table}\"";
        var where = string.Empty;
        var countParams = new List<NpgsqlParameter>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where = " WHERE CAST(t.* AS text) ILIKE @search";
            countParams.Add(new NpgsqlParameter("search", $"%{search}%"));
        }

        var orderClause = string.IsNullOrWhiteSpace(orderBy) ? string.Empty : $" ORDER BY \"{orderBy}\"";
        var countSql = $"SELECT COUNT(*) FROM {qualified} t{where}";
        var dataSql = $"SELECT * FROM {qualified} t{where}{orderClause} LIMIT @limit OFFSET @offset";

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);
        await ExecAsync(connection, tx, "SET TRANSACTION READ ONLY", cancellationToken);

        var total = await ScalarAsync<int>(connection, tx, countSql, countParams.ToArray(), cancellationToken);
        var (columns, rows) = await QueryDataAsync(
            connection,
            tx,
            dataSql,
            [..countParams, new NpgsqlParameter("limit", pageSize), new NpgsqlParameter("offset", (page - 1) * pageSize)],
            cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return new DbRowsPageDto(columns, rows, total, page, pageSize);
    }

    public async Task<ExecuteQueryResponse> ExecuteQueryAsync(
        string sql,
        int page,
        int pageSize,
        int timeoutSeconds,
        int maxRows,
        CancellationToken cancellationToken)
    {
        sql = SqlReadOnlyValidator.NormalizeForExecution(sql);
        SqlReadOnlyValidator.Validate(sql, DbExplorerDialect.PostgreSql);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);
        await ExecAsync(connection, tx, "SET TRANSACTION READ ONLY", cancellationToken);

        var countSql = $"SELECT COUNT(*) FROM ({sql}) AS _explorer_q";
        var total = Math.Min(await ScalarAsync<int>(connection, tx, countSql, [], cancellationToken, timeoutSeconds), maxRows);
        var dataSql = $"SELECT * FROM ({sql}) AS _explorer_q LIMIT @limit OFFSET @offset";
        var (columns, rows) = await QueryDataAsync(
            connection,
            tx,
            dataSql,
            [new NpgsqlParameter("limit", pageSize), new NpgsqlParameter("offset", (page - 1) * pageSize)],
            cancellationToken,
            timeoutSeconds);
        await tx.CommitAsync(cancellationToken);
        sw.Stop();
        return new ExecuteQueryResponse(columns, rows, total, page, pageSize, (int)sw.ElapsedMilliseconds);
    }

    public async Task<string> ExportQueryCsvAsync(
        string sql,
        int timeoutSeconds,
        int maxExportRows,
        CancellationToken cancellationToken)
    {
        sql = SqlReadOnlyValidator.NormalizeForExecution(sql);
        SqlReadOnlyValidator.Validate(sql, DbExplorerDialect.PostgreSql);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);
        await ExecAsync(connection, tx, "SET TRANSACTION READ ONLY", cancellationToken);

        var exportSql = $"SELECT * FROM ({sql}) AS _explorer_q LIMIT {maxExportRows}";
        var (columns, rows) = await QueryDataAsync(connection, tx, exportSql, [], cancellationToken, timeoutSeconds);
        await tx.CommitAsync(cancellationToken);
        return DbExplorerResultMapper.ToCsv(columns, rows);
    }

    public async Task<DbSchemaGraphDto> GetSchemaGraphAsync(
        IReadOnlyList<string>? schemas,
        IReadOnlySet<string> blockedTables,
        int maxNodes,
        CancellationToken cancellationToken)
    {
        var schemaFilter = schemas is { Count: > 0 } ? " AND c.table_schema = ANY(@schemas)" : string.Empty;
        var tablesSql = $"""
            SELECT c.table_schema, c.table_name, c.table_type
            FROM information_schema.tables c
            WHERE c.table_schema NOT IN ('pg_catalog', 'information_schema')
              AND c.table_type IN ('BASE TABLE', 'VIEW')
              {schemaFilter}
            ORDER BY c.table_schema, c.table_name
            LIMIT @maxNodes
            """;

        var parameters = new List<NpgsqlParameter> { new("maxNodes", maxNodes) };
        if (schemas is { Count: > 0 })
        {
            parameters.Add(new NpgsqlParameter("schemas", schemas.ToArray()));
        }

        var tables = await QueryListAsync(
            tablesSql,
            reader => new DbTableDto(reader.GetString(0), reader.GetString(1), reader.GetString(2) == "VIEW" ? "view" : "table"),
            cancellationToken,
            parameters.ToArray());

        var nodes = new List<DbSchemaGraphNodeDto>();
        foreach (var table in tables)
        {
            if (SqlReadOnlyValidator.IsTableBlocked(table.Schema, table.Name, blockedTables))
            {
                continue;
            }

            var columns = await GetColumnsAsync(table.Schema, table.Name, cancellationToken);
            nodes.Add(new DbSchemaGraphNodeDto(
                $"{table.Schema}.{table.Name}",
                table.Schema,
                table.Name,
                table.Type,
                columns.Select(c => new DbSchemaGraphColumnDto(c.Name, c.DataType, c.IsPrimaryKey, c.IsForeignKey, c.IsNullable)).ToList()));
        }

        const string fkSql = """
            SELECT
                tc.table_schema || '.' || tc.table_name,
                kcu.column_name,
                ccu.table_schema || '.' || ccu.table_name,
                ccu.column_name
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
              ON tc.constraint_name = kcu.constraint_name
             AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage ccu
              ON ccu.constraint_name = tc.constraint_name
             AND ccu.table_schema = tc.table_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
            """;

        var nodeIds = nodes.Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var edges = new List<DbSchemaGraphEdgeDto>();
        var rawEdges = await QueryListAsync(
            fkSql,
            reader => (reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)),
            cancellationToken);

        var edgeIndex = 0;
        foreach (var (source, sourceCol, target, targetCol) in rawEdges)
        {
            if (!nodeIds.Contains(source) || !nodeIds.Contains(target))
            {
                continue;
            }

            edges.Add(new DbSchemaGraphEdgeDto($"fk-{edgeIndex++}", source, sourceCol, target, targetCol));
        }

        return new DbSchemaGraphDto(nodes, edges);
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString = settings.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string não configurada.");
        return new NpgsqlConnection(connectionString);
    }

    private async Task<IReadOnlyList<T>> QueryListAsync<T>(
        string sql,
        Func<NpgsqlDataReader, T> map,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 30 };
        command.Parameters.AddRange(parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(map(reader));
        }

        return results;
    }

    private static async Task ExecAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, tx);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<T> ScalarAsync<T>(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        string sql,
        NpgsqlParameter[] parameters,
        CancellationToken cancellationToken,
        int timeoutSeconds = 30)
    {
        await using var command = new NpgsqlCommand(sql, connection, tx) { CommandTimeout = timeoutSeconds };
        command.Parameters.AddRange(parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return (T)Convert.ChangeType(result ?? 0, typeof(T));
    }

    private static async Task<(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<object?>> Rows)> QueryDataAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        string sql,
        NpgsqlParameter[] parameters,
        CancellationToken cancellationToken,
        int timeoutSeconds = 30)
    {
        await using var command = new NpgsqlCommand(sql, connection, tx) { CommandTimeout = timeoutSeconds };
        command.Parameters.AddRange(parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        var rows = new List<IReadOnlyList<object?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(DbExplorerResultMapper.ReadRow(reader, reader.FieldCount));
        }

        return (columns, rows);
    }
}
