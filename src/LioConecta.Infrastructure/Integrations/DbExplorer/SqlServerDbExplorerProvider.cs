using LioConecta.Application.Common.DbExplorer;
using LioConecta.Application.DTOs.DbExplorer;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Integrations.TotvsRm;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.DbExplorer;

public sealed class SqlServerDbExplorerProvider(
    ITotvsRmConfigurationService totvsRmConfigurationService,
    ILogger<SqlServerDbExplorerProvider> logger) : IDbExplorerProvider
{
    public string ConnectionId => DbExplorerCatalog.TotvsRmConnectionId;

    public string Engine => "SQL Server";

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            return connection.State == System.Data.ConnectionState.Open;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SQL Server explorer connection test failed.");
            return false;
        }
    }

    public Task<IReadOnlyList<DbSchemaDto>> GetSchemasAsync(CancellationToken cancellationToken) =>
        QueryListAsync(
            """
            SELECT SCHEMA_NAME
            FROM INFORMATION_SCHEMA.SCHEMATA
            WHERE SCHEMA_NAME NOT IN ('sys', 'INFORMATION_SCHEMA', 'guest')
            ORDER BY SCHEMA_NAME
            """,
            reader => new DbSchemaDto(reader.GetString(0)),
            cancellationToken);

    public Task<IReadOnlyList<DbTableDto>> GetTablesAsync(string schema, CancellationToken cancellationToken)
    {
        SqlReadOnlyValidator.ValidateIdentifier(schema, "schema");
        return QueryListAsync(
            """
            SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @schema
              AND TABLE_TYPE IN ('BASE TABLE', 'VIEW')
            ORDER BY TABLE_NAME
            """,
            reader => new DbTableDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2) == "VIEW" ? "view" : "table"),
            cancellationToken,
            new SqlParameter("@schema", schema));
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
                c.COLUMN_NAME,
                c.DATA_TYPE,
                CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END,
                CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END,
                fk.REFERENCED_TABLE,
                fk.REFERENCED_COLUMN
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                  ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                 AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON pk.TABLE_SCHEMA = c.TABLE_SCHEMA AND pk.TABLE_NAME = c.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
            LEFT JOIN (
                SELECT
                    ku.TABLE_SCHEMA,
                    ku.TABLE_NAME,
                    ku.COLUMN_NAME,
                    ccu.TABLE_NAME AS REFERENCED_TABLE,
                    ccu.COLUMN_NAME AS REFERENCED_COLUMN
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                  ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                 AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
                JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu
                  ON ccu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                 AND ccu.TABLE_SCHEMA = tc.TABLE_SCHEMA
                WHERE tc.CONSTRAINT_TYPE = 'FOREIGN KEY'
            ) fk ON fk.TABLE_SCHEMA = c.TABLE_SCHEMA AND fk.TABLE_NAME = c.TABLE_NAME AND fk.COLUMN_NAME = c.COLUMN_NAME
            WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
            ORDER BY c.ORDINAL_POSITION
            """,
            reader => new DbColumnDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2) == 1,
                reader.GetInt32(3) == 1,
                reader.GetInt32(4) == 1,
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6)),
            cancellationToken,
            new SqlParameter("@schema", schema),
            new SqlParameter("@table", table));
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

        var qualified = $"[{schema}].[{table}]";
        var where = string.Empty;
        var parameters = new List<SqlParameter>();
        // Search on SQL Server browse is limited to exact table scan without dynamic pivot.
        if (!string.IsNullOrWhiteSpace(search))
        {
            logger.LogDebug("Search filter ignored for SQL Server table browse in DB Explorer.");
        }

        var orderClause = string.IsNullOrWhiteSpace(orderBy) ? string.Empty : $" ORDER BY [{orderBy}]";
        var countSql = $"SELECT COUNT(*) FROM {qualified} t{where}";
        var offset = (page - 1) * pageSize;
        var dataSql = $"SELECT * FROM {qualified} t{where}{orderClause} OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

        await using var connection = await OpenConnectionAsync(cancellationToken);
        var total = await ScalarAsync<int>(connection, countSql, parameters.ToArray(), cancellationToken);
        parameters.Add(new SqlParameter("@offset", offset));
        parameters.Add(new SqlParameter("@pageSize", pageSize));
        var (columns, rows) = await QueryDataAsync(connection, dataSql, parameters.ToArray(), cancellationToken);
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
        SqlReadOnlyValidator.Validate(sql, DbExplorerDialect.SqlServer);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var countSql = $"SELECT COUNT(*) FROM ({sql}) AS _explorer_q";
        var total = Math.Min(await ScalarAsync<int>(connection, countSql, [], cancellationToken, timeoutSeconds), maxRows);
        var offset = (page - 1) * pageSize;
        var dataSql = $"SELECT * FROM ({sql}) AS _explorer_q ORDER BY (SELECT NULL) OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
        var (columns, rows) = await QueryDataAsync(
            connection,
            dataSql,
            [new SqlParameter("@offset", offset), new SqlParameter("@pageSize", pageSize)],
            cancellationToken,
            timeoutSeconds);
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
        SqlReadOnlyValidator.Validate(sql, DbExplorerDialect.SqlServer);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var exportSql = $"SELECT TOP ({maxExportRows}) * FROM ({sql}) AS _explorer_q";
        var (columns, rows) = await QueryDataAsync(connection, exportSql, [], cancellationToken, timeoutSeconds);
        return DbExplorerResultMapper.ToCsv(columns, rows);
    }

    public async Task<DbSchemaGraphDto> GetSchemaGraphAsync(
        IReadOnlyList<string>? schemas,
        IReadOnlySet<string> blockedTables,
        int maxNodes,
        CancellationToken cancellationToken)
    {
        var tablesSql = """
            SELECT TOP (@maxNodes) TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA NOT IN ('sys', 'INFORMATION_SCHEMA')
              AND TABLE_TYPE IN ('BASE TABLE', 'VIEW')
            ORDER BY TABLE_SCHEMA, TABLE_NAME
            """;

        var parameters = new List<SqlParameter> { new("@maxNodes", maxNodes) };

        var tables = await QueryListAsync(
            tablesSql,
            reader => new DbTableDto(reader.GetString(0), reader.GetString(1), reader.GetString(2) == "VIEW" ? "view" : "table"),
            cancellationToken,
            parameters.ToArray());

        if (schemas is { Count: > 0 })
        {
            var schemaSet = schemas.ToHashSet(StringComparer.OrdinalIgnoreCase);
            tables = tables.Where(t => schemaSet.Contains(t.Schema)).ToList();
        }

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
                OBJECT_SCHEMA_NAME(f.parent_object_id) + '.' + OBJECT_NAME(f.parent_object_id),
                pc.name,
                OBJECT_SCHEMA_NAME(f.referenced_object_id) + '.' + OBJECT_NAME(f.referenced_object_id),
                rc.name
            FROM sys.foreign_keys f
            JOIN sys.foreign_key_columns fkc ON f.object_id = fkc.constraint_object_id
            JOIN sys.columns pc ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id
            JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
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

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        var connection = TotvsRmConnectionFactory.CreateConnection(runtime);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private async Task<IReadOnlyList<T>> QueryListAsync<T>(
        string sql,
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken,
        params SqlParameter[] parameters)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        command.Parameters.AddRange(parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(map(reader));
        }

        return results;
    }

    private static async Task<T> ScalarAsync<T>(
        SqlConnection connection,
        string sql,
        SqlParameter[] parameters,
        CancellationToken cancellationToken,
        int timeoutSeconds = 30)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = timeoutSeconds };
        command.Parameters.AddRange(parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return (T)Convert.ChangeType(result ?? 0, typeof(T));
    }

    private static async Task<(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<object?>> Rows)> QueryDataAsync(
        SqlConnection connection,
        string sql,
        SqlParameter[] parameters,
        CancellationToken cancellationToken,
        int timeoutSeconds = 30)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = timeoutSeconds };
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
