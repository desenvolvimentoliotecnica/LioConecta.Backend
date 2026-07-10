using LioConecta.Application.DTOs.DbExplorer;

namespace LioConecta.Application.Interfaces.Services;

public interface IDbExplorerService
{
    Task<IReadOnlyList<DbConnectionDto>> GetConnectionsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DbSchemaDto>> GetSchemasAsync(string connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DbTableDto>> GetTablesAsync(string connectionId, string schema, CancellationToken cancellationToken);

    Task<IReadOnlyList<DbColumnDto>> GetColumnsAsync(
        string connectionId,
        string schema,
        string table,
        CancellationToken cancellationToken);

    Task<DbRowsPageDto> GetRowsAsync(
        string connectionId,
        string schema,
        string table,
        int page,
        int pageSize,
        string? orderBy,
        string? search,
        CancellationToken cancellationToken);

    Task<ExecuteQueryResponse> ExecuteQueryAsync(
        Guid actorId,
        string connectionId,
        ExecuteQueryRequest request,
        CancellationToken cancellationToken);

    Task<string> ExportQueryCsvAsync(
        Guid actorId,
        string connectionId,
        ExecuteQueryRequest request,
        CancellationToken cancellationToken);

    Task<PagedDbQueryHistoryDto> GetQueryHistoryAsync(
        Guid actorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DbQueryHistoryEntryDto?> GetQueryHistoryEntryAsync(
        Guid actorId,
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DbSavedQueryDto>> GetSavedQueriesAsync(Guid actorId, CancellationToken cancellationToken);

    Task<DbSavedQueryDto> CreateSavedQueryAsync(
        Guid actorId,
        UpsertSavedQueryRequest request,
        CancellationToken cancellationToken);

    Task<DbSavedQueryDto> UpdateSavedQueryAsync(
        Guid actorId,
        Guid id,
        UpsertSavedQueryRequest request,
        CancellationToken cancellationToken);

    Task DeleteSavedQueryAsync(Guid actorId, Guid id, CancellationToken cancellationToken);

    Task<DbSavedQueryDto> PromoteHistoryToSavedAsync(
        Guid actorId,
        Guid historyId,
        PromoteHistoryToSavedRequest request,
        CancellationToken cancellationToken);

    Task<DbSchemaGraphDto> GetSchemaGraphAsync(
        string connectionId,
        IReadOnlyList<string>? schemas,
        CancellationToken cancellationToken);

    Task<DbDerLayoutDto?> GetDerLayoutAsync(Guid actorId, string connectionId, CancellationToken cancellationToken);

    Task<DbDerLayoutDto> UpsertDerLayoutAsync(
        Guid actorId,
        string connectionId,
        UpsertDerLayoutRequest request,
        CancellationToken cancellationToken);

    Task DeleteDerLayoutAsync(Guid actorId, string connectionId, CancellationToken cancellationToken);
}
