using LioConecta.Application.Common;
using LioConecta.Application.Common.Audit;
using LioConecta.Application.Common.DbExplorer;
using LioConecta.Application.DTOs.DbExplorer;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class DbExplorerService(
    IEnumerable<IDbExplorerProvider> providers,
    AppDbContext dbContext,
    IAppSettingsProvider settings,
    IAuditService auditService) : IDbExplorerService
{
    private readonly IReadOnlyDictionary<string, IDbExplorerProvider> _providers =
        providers.ToDictionary(p => p.ConnectionId, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<DbConnectionDto>> GetConnectionsAsync(CancellationToken cancellationToken)
    {
        var results = new List<DbConnectionDto>();
        foreach (var connectionId in DbExplorerCatalog.KnownConnectionIds)
        {
            if (!_providers.TryGetValue(connectionId, out var provider))
            {
                continue;
            }

            var available = await provider.TestConnectionAsync(cancellationToken);
            results.Add(new DbConnectionDto(
                provider.ConnectionId,
                provider.ConnectionId == DbExplorerCatalog.PostgresConnectionId ? "PostgreSQL (LioConecta)" : "TOTVS RM",
                provider.Engine,
                available,
                available ? null : "Conexão indisponível"));
        }

        return results;
    }

    public Task<IReadOnlyList<DbSchemaDto>> GetSchemasAsync(string connectionId, CancellationToken cancellationToken) =>
        Resolve(connectionId).GetSchemasAsync(cancellationToken);

    public Task<IReadOnlyList<DbTableDto>> GetTablesAsync(string connectionId, string schema, CancellationToken cancellationToken) =>
        Resolve(connectionId).GetTablesAsync(schema, cancellationToken);

    public Task<IReadOnlyList<DbColumnDto>> GetColumnsAsync(
        string connectionId,
        string schema,
        string table,
        CancellationToken cancellationToken) =>
        Resolve(connectionId).GetColumnsAsync(schema, table, cancellationToken);

    public async Task<DbRowsPageDto> GetRowsAsync(
        string connectionId,
        string schema,
        string table,
        int page,
        int pageSize,
        string? orderBy,
        string? search,
        CancellationToken cancellationToken)
    {
        if (SqlReadOnlyValidator.IsTableBlocked(schema, table, DbExplorerCatalog.DefaultBlockedTables))
        {
            throw new InvalidOperationException("Tabela bloqueada para consulta.");
        }

        pageSize = Math.Clamp(pageSize, 1, GetMaxRowsPerPage());
        return await Resolve(connectionId).GetRowsAsync(schema, table, Math.Max(page, 1), pageSize, orderBy, search, cancellationToken);
    }

    public async Task<ExecuteQueryResponse> ExecuteQueryAsync(
        Guid actorId,
        string connectionId,
        ExecuteQueryRequest request,
        CancellationToken cancellationToken)
    {
        var provider = Resolve(connectionId);
        var timeout = GetQueryTimeoutSeconds();
        var maxRows = GetMaxRowsPerPage() * 50;
        var pageSize = Math.Clamp(request.PageSize, 1, GetMaxRowsPerPage());
        var page = Math.Max(request.Page, 1);

        try
        {
            var sql = SqlReadOnlyValidator.NormalizeForExecution(request.Sql);
            SqlReadOnlyValidator.Validate(sql, provider.Engine.Contains("Postgre", StringComparison.OrdinalIgnoreCase)
                ? DbExplorerDialect.PostgreSql
                : DbExplorerDialect.SqlServer);

            var result = await provider.ExecuteQueryAsync(sql, page, pageSize, timeout, maxRows, cancellationToken);
            await LogQueryAsync(actorId, connectionId, sql, result.Rows.Count, result.DurationMs, true, null, cancellationToken);
            QueueAudit(actorId, connectionId, sql, true);
            return result;
        }
        catch (SqlReadOnlyValidationException)
        {
            throw;
        }
        catch (DbExplorerQueryException)
        {
            throw;
        }
        catch (Npgsql.PostgresException ex)
        {
            await LogQueryAsync(actorId, connectionId, request.Sql, 0, 0, false, ex.MessageText, cancellationToken);
            QueueAudit(actorId, connectionId, request.Sql, false);
            throw new DbExplorerQueryException(ex.MessageText);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            await LogQueryAsync(actorId, connectionId, request.Sql, 0, 0, false, ex.Message, cancellationToken);
            QueueAudit(actorId, connectionId, request.Sql, false);
            throw new DbExplorerQueryException(ex.Message);
        }
        catch (Exception ex)
        {
            await LogQueryAsync(actorId, connectionId, request.Sql, 0, 0, false, ex.Message, cancellationToken);
            QueueAudit(actorId, connectionId, request.Sql, false);
            throw;
        }
    }

    public async Task<string> ExportQueryCsvAsync(
        Guid actorId,
        string connectionId,
        ExecuteQueryRequest request,
        CancellationToken cancellationToken)
    {
        var provider = Resolve(connectionId);
        var sql = SqlReadOnlyValidator.NormalizeForExecution(request.Sql);
        SqlReadOnlyValidator.Validate(sql, provider.Engine.Contains("Postgre", StringComparison.OrdinalIgnoreCase)
            ? DbExplorerDialect.PostgreSql
            : DbExplorerDialect.SqlServer);
        return await provider.ExportQueryCsvAsync(sql, GetQueryTimeoutSeconds(), GetMaxExportRows(), cancellationToken);
    }

    public async Task<PagedDbQueryHistoryDto> GetQueryHistoryAsync(
        Guid actorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.DbExplorerQueryLogs.AsNoTracking().Where(x => x.ActorId == actorId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DbQueryHistoryEntryDto(
                x.Id,
                x.ConnectionId,
                x.SqlText,
                x.RowCount,
                x.DurationMs,
                x.Success,
                x.ErrorMessage,
                x.ExecutedAt))
            .ToListAsync(cancellationToken);
        return new PagedDbQueryHistoryDto(items, total, page, pageSize);
    }

    public async Task<DbQueryHistoryEntryDto?> GetQueryHistoryEntryAsync(
        Guid actorId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.DbExplorerQueryLogs.AsNoTracking()
            .Where(x => x.ActorId == actorId && x.Id == id)
            .Select(x => new DbQueryHistoryEntryDto(
                x.Id,
                x.ConnectionId,
                x.SqlText,
                x.RowCount,
                x.DurationMs,
                x.Success,
                x.ErrorMessage,
                x.ExecutedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DbSavedQueryDto>> GetSavedQueriesAsync(Guid actorId, CancellationToken cancellationToken) =>
        await dbContext.DbExplorerSavedQueries.AsNoTracking()
            .Where(x => x.ActorId == actorId)
            .OrderBy(x => x.Name)
            .Select(x => new DbSavedQueryDto(
                x.Id,
                x.Name,
                x.ConnectionId,
                x.SqlText,
                x.Description,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<DbSavedQueryDto> CreateSavedQueryAsync(
        Guid actorId,
        UpsertSavedQueryRequest request,
        CancellationToken cancellationToken)
    {
        ValidateSavedRequest(request);
        await EnsureSavedQueryLimitAsync(actorId, cancellationToken);
        await EnsureUniqueNameAsync(actorId, request.Name, null, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entity = new DbExplorerSavedQuery
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Name = request.Name.Trim(),
            ConnectionId = request.ConnectionId,
            SqlText = request.Sql.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.DbExplorerSavedQueries.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapSaved(entity);
    }

    private static DbSavedQueryDto MapSaved(DbExplorerSavedQuery x) =>
        new(x.Id, x.Name, x.ConnectionId, x.SqlText, x.Description, x.CreatedAt, x.UpdatedAt);

    public async Task<DbSavedQueryDto> UpdateSavedQueryAsync(
        Guid actorId,
        Guid id,
        UpsertSavedQueryRequest request,
        CancellationToken cancellationToken)
    {
        ValidateSavedRequest(request);
        await EnsureUniqueNameAsync(actorId, request.Name, id, cancellationToken);
        var entity = await dbContext.DbExplorerSavedQueries
            .FirstOrDefaultAsync(x => x.Id == id && x.ActorId == actorId, cancellationToken)
            ?? throw new KeyNotFoundException("Favorita não encontrada.");

        entity.Name = request.Name.Trim();
        entity.ConnectionId = request.ConnectionId;
        entity.SqlText = request.Sql.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapSaved(entity);
    }

    public async Task DeleteSavedQueryAsync(Guid actorId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.DbExplorerSavedQueries
            .FirstOrDefaultAsync(x => x.Id == id && x.ActorId == actorId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.DbExplorerSavedQueries.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DbSavedQueryDto> PromoteHistoryToSavedAsync(
        Guid actorId,
        Guid historyId,
        PromoteHistoryToSavedRequest request,
        CancellationToken cancellationToken)
    {
        var history = await dbContext.DbExplorerQueryLogs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == historyId && x.ActorId == actorId, cancellationToken)
            ?? throw new KeyNotFoundException("Histórico não encontrado.");

        return await CreateSavedQueryAsync(
            actorId,
            new UpsertSavedQueryRequest(request.Name, history.ConnectionId, history.SqlText, request.Description),
            cancellationToken);
    }

    public Task<DbSchemaGraphDto> GetSchemaGraphAsync(
        string connectionId,
        IReadOnlyList<string>? schemas,
        CancellationToken cancellationToken) =>
        Resolve(connectionId).GetSchemaGraphAsync(schemas, DbExplorerCatalog.DefaultBlockedTables, GetMaxDerNodes(), cancellationToken);

    public async Task<DbDerLayoutDto?> GetDerLayoutAsync(Guid actorId, string connectionId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.DbExplorerDerLayouts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ActorId == actorId && x.ConnectionId == connectionId, cancellationToken);
        return entity is null
            ? null
            : new DbDerLayoutDto(entity.Id, entity.ConnectionId, entity.LayoutJson, entity.UpdatedAt);
    }

    public async Task<DbDerLayoutDto> UpsertDerLayoutAsync(
        Guid actorId,
        string connectionId,
        UpsertDerLayoutRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = await dbContext.DbExplorerDerLayouts
            .FirstOrDefaultAsync(x => x.ActorId == actorId && x.ConnectionId == connectionId, cancellationToken);

        if (entity is null)
        {
            entity = new DbExplorerDerLayout
            {
                Id = Guid.NewGuid(),
                ActorId = actorId,
                ConnectionId = connectionId,
                LayoutJson = request.LayoutJson,
                CreatedAt = now,
                UpdatedAt = now,
            };
            dbContext.DbExplorerDerLayouts.Add(entity);
        }
        else
        {
            entity.LayoutJson = request.LayoutJson;
            entity.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new DbDerLayoutDto(entity.Id, entity.ConnectionId, entity.LayoutJson, entity.UpdatedAt);
    }

    public async Task DeleteDerLayoutAsync(Guid actorId, string connectionId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.DbExplorerDerLayouts
            .FirstOrDefaultAsync(x => x.ActorId == actorId && x.ConnectionId == connectionId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.DbExplorerDerLayouts.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IDbExplorerProvider Resolve(string connectionId)
    {
        if (!_providers.TryGetValue(connectionId, out var provider))
        {
            throw new KeyNotFoundException($"Conexão '{connectionId}' não suportada.");
        }

        return provider;
    }

    private void ValidateSavedRequest(UpsertSavedQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Nome é obrigatório.");
        }

        if (!DbExplorerCatalog.KnownConnectionIds.Contains(request.ConnectionId))
        {
            throw new ArgumentException("Conexão inválida.");
        }

        var dialect = request.ConnectionId == DbExplorerCatalog.PostgresConnectionId
            ? DbExplorerDialect.PostgreSql
            : DbExplorerDialect.SqlServer;
        SqlReadOnlyValidator.Validate(request.Sql, dialect);
    }

    private async Task EnsureSavedQueryLimitAsync(Guid actorId, CancellationToken cancellationToken)
    {
        var count = await dbContext.DbExplorerSavedQueries.CountAsync(x => x.ActorId == actorId, cancellationToken);
        if (count >= GetMaxSavedQueries())
        {
            throw new InvalidOperationException($"Limite de {GetMaxSavedQueries()} queries favoritas atingido.");
        }
    }

    private async Task EnsureUniqueNameAsync(Guid actorId, string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.DbExplorerSavedQueries.AnyAsync(
            x => x.ActorId == actorId && x.Name == name.Trim() && (excludeId == null || x.Id != excludeId),
            cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Já existe uma favorita com este nome.");
        }
    }

    private async Task LogQueryAsync(
        Guid actorId,
        string connectionId,
        string sql,
        int rowCount,
        int durationMs,
        bool success,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var maxLen = 8192;
        var sqlText = sql.Length > maxLen ? sql[..maxLen] : sql;
        dbContext.DbExplorerQueryLogs.Add(new DbExplorerQueryLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            ConnectionId = connectionId,
            SqlText = sqlText,
            RowCount = rowCount,
            DurationMs = durationMs,
            Success = success,
            ErrorMessage = errorMessage,
            ExecutedAt = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void QueueAudit(Guid actorId, string connectionId, string sql, bool success)
    {
        auditService.Queue(new PendingAuditEvent
        {
            Action = "DbExplorer.QueryExecuted",
            ActorId = actorId,
            TargetType = "DbExplorer",
            TargetId = connectionId,
            Source = AuditSource.EntityChange,
            DetailsJson = $"{{\"success\":{success.ToString().ToLowerInvariant()},\"sqlLength\":{sql.Length}}}",
        });
    }

    private int GetQueryTimeoutSeconds() => settings.GetInt("db_explorer.query_timeout_seconds", 30);

    private int GetMaxRowsPerPage() => settings.GetInt("db_explorer.max_rows_per_page", 200);

    private int GetMaxExportRows() => settings.GetInt("db_explorer.max_export_rows", 10_000);

    private int GetMaxSavedQueries() => settings.GetInt("db_explorer.max_saved_queries_per_user", 50);

    private int GetMaxDerNodes() => settings.GetInt("db_explorer.der_max_nodes", 150);
}
