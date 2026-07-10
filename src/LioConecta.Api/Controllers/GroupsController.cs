using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/groups")]
[Authorize]
public sealed class GroupsController(IGroupService groupService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await groupService.GetMyGroupsAsync(cancellationToken));
    }

    [HttpGet("explore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> Explore(CancellationToken cancellationToken)
    {
        return Ok(await groupService.GetExploreGroupsAsync(cancellationToken));
    }

    [HttpGet("pending-for-me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> PendingForMe(CancellationToken cancellationToken)
    {
        return Ok(await groupService.GetPendingForMeAsync(cancellationToken));
    }

    [HttpGet("expired")]
    [RequirePermission("groups.approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> Expired(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await groupService.GetExpiredAsync(cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("ownership-transfers/pending-for-me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupOwnershipTransferDto>>> PendingOwnershipTransfers(
        CancellationToken cancellationToken)
    {
        return Ok(await groupService.GetPendingOwnershipTransfersForMeAsync(cancellationToken));
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupDto>> Create(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "O nome do grupo é obrigatório." });
        }

        try
        {
            var group = await groupService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GroupDto>> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var group = await groupService.ApproveAsync(id, cancellationToken);
            return group is null ? NotFound() : Ok(group);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GroupDto>> Reject(
        Guid id,
        [FromBody] RejectGroupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var group = await groupService.RejectAsync(id, request, cancellationToken);
            return group is null ? NotFound() : Ok(group);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/resubmit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupDto>> Resubmit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var group = await groupService.ResubmitAsync(id, cancellationToken);
            return group is null ? NotFound() : Ok(group);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDto>> Join(Guid id, CancellationToken cancellationToken)
    {
        var group = await groupService.JoinAsync(id, cancellationToken);
        return group is null ? NotFound() : Ok(group);
    }

    [HttpPost("{id:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupDto>> Leave(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var group = await groupService.LeaveAsync(id, cancellationToken);
            return group is null ? NotFound() : Ok(group);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
        try
        {
            var group = await groupService.UpdateAsync(id, request, cancellationToken);
            return group is null ? NotFound() : Ok(group);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await groupService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<GroupMemberDto>>> Members(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await groupService.GetMembersAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPatch("{id:guid}/members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupMemberDto>> UpdateMemberRole(
        Guid id,
        Guid memberId,
        [FromBody] UpdateGroupMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var member = await groupService.UpdateMemberRoleAsync(id, memberId, request, cancellationToken);
            return member is null ? NotFound() : Ok(member);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/wall")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<GroupWallPostDto>>> Wall(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await groupService.GetWallAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/wall")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GroupWallPostDto>> CreateWallPost(
        Guid id,
        [FromBody] CreateGroupWallPostRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await groupService.CreateWallPostAsync(id, request, cancellationToken);
            return Created($"/api/v1/groups/{id}/wall/{post.Id}", post);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/wall/{postId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteWallPost(Guid id, Guid postId, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await groupService.DeleteWallPostAsync(id, postId, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/wall/{postId:guid}/reactions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupWallPostDto>> ToggleReaction(
        Guid id,
        Guid postId,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await groupService.ToggleWallReactionAsync(id, postId, cancellationToken);
            return post is null ? NotFound() : Ok(post);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id:guid}/topics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupTopicSummaryDto>>> Topics(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await groupService.GetTopicsAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id:guid}/topics/{topicId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupTopicDetailDto>> Topic(
        Guid id,
        Guid topicId,
        CancellationToken cancellationToken)
    {
        try
        {
            var topic = await groupService.GetTopicAsync(id, topicId, cancellationToken);
            return topic is null ? NotFound() : Ok(topic);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/topics")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupTopicDetailDto>> CreateTopic(
        Guid id,
        [FromBody] CreateGroupTopicRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var topic = await groupService.CreateTopicAsync(id, request, cancellationToken);
            return Created($"/api/v1/groups/{id}/topics/{topic.Id}", topic);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/topics/{topicId:guid}/replies")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupTopicReplyDto>> CreateReply(
        Guid id,
        Guid topicId,
        [FromBody] CreateGroupTopicReplyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var reply = await groupService.CreateTopicReplyAsync(id, topicId, request, cancellationToken);
            return Created($"/api/v1/groups/{id}/topics/{topicId}/replies/{reply.Id}", reply);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/topics/{topicId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTopic(Guid id, Guid topicId, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await groupService.DeleteTopicAsync(id, topicId, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:guid}/topics/{topicId:guid}/replies/{replyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteReply(
        Guid id,
        Guid topicId,
        Guid replyId,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await groupService.DeleteTopicReplyAsync(id, topicId, replyId, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/ownership-transfers")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupOwnershipTransferDto>> RequestOwnershipTransfer(
        Guid id,
        [FromBody] CreateOwnershipTransferRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var transfer = await groupService.RequestOwnershipTransferAsync(id, request, cancellationToken);
            return Created($"/api/v1/groups/ownership-transfers/{transfer.Id}", transfer);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("ownership-transfers/{transferId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupOwnershipTransferDto>> ApproveOwnershipTransfer(
        Guid transferId,
        CancellationToken cancellationToken)
    {
        try
        {
            var transfer = await groupService.ApproveOwnershipTransferAsync(transferId, cancellationToken);
            return transfer is null ? NotFound() : Ok(transfer);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("ownership-transfers/{transferId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupOwnershipTransferDto>> RejectOwnershipTransfer(
        Guid transferId,
        [FromBody] RejectGroupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var transfer = await groupService.RejectOwnershipTransferAsync(transferId, request, cancellationToken);
            return transfer is null ? NotFound() : Ok(transfer);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
