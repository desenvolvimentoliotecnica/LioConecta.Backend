using LioConecta.Application.DTOs.DbExplorer;

namespace LioConecta.Application.Interfaces.Integrations;

public interface IDbExplorerProvider
{
    string ConnectionId { get; }

    string Engine { get; }

    Task<bool> TestConnectionAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DbSchemaDto>> GetSchemasAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DbTableDto>> GetTablesAsync(string schema, CancellationToken cancellationToken);

    Task<IReadOnlyList<DbColumnDto>> GetColumnsAsync(string schema, string table, CancellationToken cancellationToken);

    Task<DbRowsPageDto> GetRowsAsync(
        string schema,
        string table,
        int page,
        int pageSize,
        string? orderBy,
        string? search,
        CancellationToken cancellationToken);

    Task<ExecuteQueryResponse> ExecuteQueryAsync(
        string sql,
        int page,
        int pageSize,
        int timeoutSeconds,
        int maxRows,
        CancellationToken cancellationToken);

    Task<string> ExportQueryCsvAsync(
        string sql,
        int timeoutSeconds,
        int maxExportRows,
        CancellationToken cancellationToken);

    Task<DbSchemaGraphDto> GetSchemaGraphAsync(
        IReadOnlyList<string>? schemas,
        IReadOnlySet<string> blockedTables,
        int maxNodes,
        CancellationToken cancellationToken);
}
