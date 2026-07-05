using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/groups")]
[Authorize]
public sealed class GroupsController(
    IGroupService groupService,
    AppDbContext dbContext,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> List(CancellationToken cancellationToken)
    {
        var groups = await groupService.GetMyGroupsAsync(cancellationToken);
        return Ok(groups);
    }

    [HttpGet("explore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> Explore(CancellationToken cancellationToken)
    {
        var groups = await groupService.GetExploreGroupsAsync(cancellationToken);
        return Ok(groups);
    }

    [HttpGet("pending")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> ListPending(CancellationToken cancellationToken)
    {
        var groups = await groupService.GetPendingApprovalAsync(cancellationToken);
        return Ok(groups);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var group = await groupService.GetByIdAsync(id, cancellationToken);
        return group is null ? NotFound() : Ok(group);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<GroupDto>> Create(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "O nome do grupo é obrigatório." });
        }

        var group = await groupService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDto>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var group = await groupService.ApproveAsync(id, cancellationToken);
        return group is null ? NotFound() : Ok(group);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDto>> Reject(
        Guid id,
        [FromBody] RejectGroupRequest request,
        CancellationToken cancellationToken)
    {
        var group = await groupService.RejectAsync(id, request, cancellationToken);
        return group is null ? NotFound() : Ok(group);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GroupDto>> Update(
        Guid id,
        [FromBody] UpdateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await dbContext.Groups
            .Include(g => g.Owner)
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (group is null)
        {
            return NotFound();
        }

        if (group.OwnerId != personId)
        {
            return Forbid();
        }

        group.Name = request.Name.Trim();
        group.Description = request.Description?.Trim();
        group.IsPrivate = request.IsPrivate;
        group.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(GroupMapper.ToDto(group, isMember: true));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await dbContext.Groups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (group is null)
        {
            return NotFound();
        }

        if (group.OwnerId != personId)
        {
            return Forbid();
        }

        dbContext.Groups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public sealed record UpdateGroupRequest(
    string Name,
    string? Description,
    bool IsPrivate);
