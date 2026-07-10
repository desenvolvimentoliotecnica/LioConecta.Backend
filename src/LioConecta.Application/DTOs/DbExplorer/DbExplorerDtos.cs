namespace LioConecta.Application.DTOs.DbExplorer;

public sealed record DbConnectionDto(string Id, string Label, string Engine, bool Available, string? StatusMessage);

public sealed record DbSchemaDto(string Name);

public sealed record DbTableDto(string Schema, string Name, string Type);

public sealed record DbColumnDto(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    bool IsForeignKey,
    string? ForeignKeyTable,
    string? ForeignKeyColumn);

public sealed record DbRowsPageDto(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ExecuteQueryRequest(string Sql, int Page = 1, int PageSize = 200);

public sealed record ExecuteQueryResponse(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int TotalCount,
    int Page,
    int PageSize,
    int DurationMs);

public sealed record DbQueryHistoryEntryDto(
    Guid Id,
    string ConnectionId,
    string SqlText,
    int RowCount,
    int DurationMs,
    bool Success,
    string? ErrorMessage,
    DateTimeOffset ExecutedAt);

public sealed record PagedDbQueryHistoryDto(
    IReadOnlyList<DbQueryHistoryEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record DbSavedQueryDto(
    Guid Id,
    string Name,
    string ConnectionId,
    string SqlText,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpsertSavedQueryRequest(
    string Name,
    string ConnectionId,
    string Sql,
    string? Description);

public sealed record DbSchemaGraphColumnDto(
    string Name,
    string DataType,
    bool IsPrimaryKey,
    bool IsForeignKey,
    bool IsNullable);

public sealed record DbSchemaGraphNodeDto(
    string Id,
    string Schema,
    string Name,
    string Type,
    IReadOnlyList<DbSchemaGraphColumnDto> Columns);

public sealed record DbSchemaGraphEdgeDto(
    string Id,
    string SourceNodeId,
    string SourceColumn,
    string TargetNodeId,
    string TargetColumn);

public sealed record DbSchemaGraphDto(
    IReadOnlyList<DbSchemaGraphNodeDto> Nodes,
    IReadOnlyList<DbSchemaGraphEdgeDto> Edges);

public sealed record DbDerLayoutDto(
    Guid? Id,
    string ConnectionId,
    string LayoutJson,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertDerLayoutRequest(string LayoutJson);

public sealed record PromoteHistoryToSavedRequest(string Name, string? Description);
