using LioConecta.Api.Attributes;
using LioConecta.Api.Authorization;
using LioConecta.Application.Common.DbExplorer;
using LioConecta.Application.DTOs.DbExplorer;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/db-explorer")]
[Authorize(Policy = AuthPolicies.RequireStrictAdmin)]
[AccessAudited(Resource = "DbExplorer")]
public sealed class AdminDbExplorerController(
    IDbExplorerService dbExplorerService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("connections")]
    public async Task<ActionResult<IReadOnlyList<DbConnectionDto>>> GetConnections(CancellationToken cancellationToken) =>
        Ok(await dbExplorerService.GetConnectionsAsync(cancellationToken));

    [HttpGet("{connectionId}/schemas")]
    public async Task<ActionResult<IReadOnlyList<DbSchemaDto>>> GetSchemas(
        string connectionId,
        CancellationToken cancellationToken) =>
        Ok(await dbExplorerService.GetSchemasAsync(connectionId, cancellationToken));

    [HttpGet("{connectionId}/tables")]
    public async Task<ActionResult<IReadOnlyList<DbTableDto>>> GetTables(
        string connectionId,
        [FromQuery] string schema,
        CancellationToken cancellationToken) =>
        Ok(await dbExplorerService.GetTablesAsync(connectionId, schema, cancellationToken));

    [HttpGet("{connectionId}/tables/{schema}/{table}/columns")]
    public async Task<ActionResult<IReadOnlyList<DbColumnDto>>> GetColumns(
        string connectionId,
        string schema,
        string table,
        CancellationToken cancellationToken) =>
        Ok(await dbExplorerService.GetColumnsAsync(connectionId, schema, table, cancellationToken));

    [HttpGet("{connectionId}/tables/{schema}/{table}/rows")]
    public async Task<ActionResult<DbRowsPageDto>> GetRows(
        string connectionId,
        string schema,
        string table,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200,
        [FromQuery] string? orderBy = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default) =>
        Ok(await dbExplorerService.GetRowsAsync(connectionId, schema, table, page, pageSize, orderBy, search, cancellationToken));

    [HttpPost("{connectionId}/query")]
    public async Task<ActionResult<ExecuteQueryResponse>> ExecuteQuery(
        string connectionId,
        [FromBody] ExecuteQueryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
            return Ok(await dbExplorerService.ExecuteQueryAsync(actorId, connectionId, request, cancellationToken));
        }
        catch (SqlReadOnlyValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbExplorerQueryException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{connectionId}/query/export")]
    public async Task<IActionResult> ExportQuery(
        string connectionId,
        [FromBody] ExecuteQueryRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var csv = await dbExplorerService.ExportQueryCsvAsync(actorId, connectionId, request, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "db-explorer-export.csv");
    }

    [HttpGet("query-history")]
    public async Task<ActionResult<PagedDbQueryHistoryDto>> GetQueryHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return Ok(await dbExplorerService.GetQueryHistoryAsync(actorId, page, pageSize, cancellationToken));
    }

    [HttpGet("query-history/{id:guid}")]
    public async Task<ActionResult<DbQueryHistoryEntryDto>> GetQueryHistoryEntry(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var entry = await dbExplorerService.GetQueryHistoryEntryAsync(actorId, id, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpGet("saved-queries")]
    public async Task<ActionResult<IReadOnlyList<DbSavedQueryDto>>> GetSavedQueries(CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return Ok(await dbExplorerService.GetSavedQueriesAsync(actorId, cancellationToken));
    }

    [HttpPost("saved-queries")]
    public async Task<ActionResult<DbSavedQueryDto>> CreateSavedQuery(
        [FromBody] UpsertSavedQueryRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return Ok(await dbExplorerService.CreateSavedQueryAsync(actorId, request, cancellationToken));
    }

    [HttpPut("saved-queries/{id:guid}")]
    public async Task<ActionResult<DbSavedQueryDto>> UpdateSavedQuery(
        Guid id,
        [FromBody] UpsertSavedQueryRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return Ok(await dbExplorerService.UpdateSavedQueryAsync(actorId, id, request, cancellationToken));
    }

    [HttpDelete("saved-queries/{id:guid}")]
    public async Task<IActionResult> DeleteSavedQuery(Guid id, CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await dbExplorerService.DeleteSavedQueryAsync(actorId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("saved-queries/from-history/{historyId:guid}")]
    public async Task<ActionResult<DbSavedQueryDto>> PromoteHistoryToSaved(
        Guid historyId,
        [FromBody] PromoteHistoryToSavedRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return Ok(await dbExplorerService.PromoteHistoryToSavedAsync(actorId, historyId, request, cancellationToken));
    }

    [HttpGet("{connectionId}/schema-graph")]
    public async Task<ActionResult<DbSchemaGraphDto>> GetSchemaGraph(
        string connectionId,
        [FromQuery] string? schemas,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string>? schemaList = string.IsNullOrWhiteSpace(schemas)
            ? null
            : schemas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Ok(await dbExplorerService.GetSchemaGraphAsync(connectionId, schemaList, cancellationToken));
    }

    [HttpGet("der-layouts/{connectionId}")]
    public async Task<ActionResult<DbDerLayoutDto>> GetDerLayout(
        string connectionId,
        CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var layout = await dbExplorerService.GetDerLayoutAsync(actorId, connectionId, cancellationToken);
        return layout is null ? NotFound() : Ok(layout);
    }

    [HttpPut("der-layouts/{connectionId}")]
    public async Task<ActionResult<DbDerLayoutDto>> UpsertDerLayout(
        string connectionId,
        [FromBody] UpsertDerLayoutRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return Ok(await dbExplorerService.UpsertDerLayoutAsync(actorId, connectionId, request, cancellationToken));
    }

    [HttpDelete("der-layouts/{connectionId}")]
    public async Task<IActionResult> DeleteDerLayout(string connectionId, CancellationToken cancellationToken)
    {
        var actorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await dbExplorerService.DeleteDerLayoutAsync(actorId, connectionId, cancellationToken);
        return NoContent();
    }
}
