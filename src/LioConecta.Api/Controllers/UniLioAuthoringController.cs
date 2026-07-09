using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/unilio/authoring")]
[Authorize]
public sealed class UniLioAuthoringController(IUniLioAuthoringService authoringService) : ControllerBase
{
    [HttpGet("courses")]
    [ProducesResponseType(typeof(UniLioAuthoringCourseListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAuthoringCourseListDto>> ListCourses(CancellationToken cancellationToken)
        => Ok(await authoringService.ListCoursesAsync(cancellationToken));

    [HttpGet("courses/pending")]
    [ProducesResponseType(typeof(UniLioAuthoringCourseListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAuthoringCourseListDto>> ListPending(CancellationToken cancellationToken)
        => Ok(await authoringService.ListPendingCoursesAsync(cancellationToken));

    [HttpGet("courses/{id:guid}")]
    [ProducesResponseType(typeof(UniLioAuthoringCourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAuthoringCourseDto>> GetCourse(Guid id, CancellationToken cancellationToken)
        => Ok(await authoringService.GetCourseAsync(id, cancellationToken));

    [HttpGet("courses/{id:guid}/approval-review")]
    [ProducesResponseType(typeof(UniLioApprovalReviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioApprovalReviewDto>> GetApprovalReview(Guid id, CancellationToken cancellationToken)
        => Ok(await authoringService.GetApprovalReviewAsync(id, cancellationToken));

    [HttpPost("courses")]
    [ProducesResponseType(typeof(UniLioAuthoringCourseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UniLioAuthoringCourseDto>> CreateCourse(
        [FromBody] UniLioUpsertCourseRequest request,
        CancellationToken cancellationToken)
    {
        var course = await authoringService.CreateCourseAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
    }

    [HttpPut("courses/{id:guid}")]
    [ProducesResponseType(typeof(UniLioAuthoringCourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAuthoringCourseDto>> UpdateCourse(
        Guid id,
        [FromBody] UniLioUpsertCourseRequest request,
        CancellationToken cancellationToken)
        => Ok(await authoringService.UpdateCourseAsync(id, request, cancellationToken));

    [HttpPost("courses/{id:guid}/submit")]
    [ProducesResponseType(typeof(UniLioAuthoringCourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAuthoringCourseDto>> SubmitCourse(Guid id, CancellationToken cancellationToken)
        => Ok(await authoringService.SubmitCourseAsync(id, cancellationToken));

    [HttpPost("courses/{id:guid}/approve")]
    [ProducesResponseType(typeof(UniLioAuthoringCourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAuthoringCourseDto>> ApproveCourse(Guid id, CancellationToken cancellationToken)
        => Ok(await authoringService.ApproveCourseAsync(id, cancellationToken));

    [HttpPost("courses/{id:guid}/reject")]
    [ProducesResponseType(typeof(UniLioAuthoringCourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAuthoringCourseDto>> RejectCourse(
        Guid id,
        [FromBody] UniLioRejectCourseRequest request,
        CancellationToken cancellationToken)
        => Ok(await authoringService.RejectCourseAsync(id, request, cancellationToken));

    [HttpPost("courses/{id:guid}/publish")]
    [ProducesResponseType(typeof(UniLioAuthoringCourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAuthoringCourseDto>> PublishCourse(Guid id, CancellationToken cancellationToken)
        => Ok(await authoringService.PublishCourseDirectAsync(id, cancellationToken));

    [HttpPost("courses/{courseId:guid}/modules")]
    [ProducesResponseType(typeof(UniLioModuleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UniLioModuleDto>> AddModule(
        Guid courseId,
        [FromBody] UniLioUpsertModuleRequest request,
        CancellationToken cancellationToken)
        => Ok(await authoringService.AddModuleAsync(courseId, request, cancellationToken));

    [HttpPut("courses/{courseId:guid}/modules/{moduleId:guid}")]
    [ProducesResponseType(typeof(UniLioModuleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioModuleDto>> UpdateModule(
        Guid courseId,
        Guid moduleId,
        [FromBody] UniLioUpsertModuleRequest request,
        CancellationToken cancellationToken)
        => Ok(await authoringService.UpdateModuleAsync(courseId, moduleId, request, cancellationToken));

    [HttpDelete("courses/{courseId:guid}/modules/{moduleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteModule(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        await authoringService.DeleteModuleAsync(courseId, moduleId, cancellationToken);
        return NoContent();
    }
}
