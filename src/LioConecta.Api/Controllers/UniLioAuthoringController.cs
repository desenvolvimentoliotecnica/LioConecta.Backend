using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

// LMS migrando para a app standalone LioConecta.UniLio (Frontend + Backend próprios).
// Endpoints mantidos aqui até o cutover (VITE_UNILIO_APP_URL) — não remover ainda.
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

    [HttpPost("courses/{id:guid}/withdraw")]
    [ProducesResponseType(typeof(UniLioAuthoringCourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAuthoringCourseDto>> WithdrawCourse(Guid id, CancellationToken cancellationToken)
        => Ok(await authoringService.WithdrawCourseAsync(id, cancellationToken));

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

    [HttpPost("courses/{courseId:guid}/modules/{moduleId:guid}/attachments")]
    [RequestSizeLimit(26_214_400)]
    [ProducesResponseType(typeof(UniLioModuleAttachmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioModuleAttachmentDto>> UploadModuleAttachment(
        Guid courseId,
        Guid moduleId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Arquivo obrigatório." });
        }

        await using var stream = file.OpenReadStream();
        var result = await authoringService.UploadModuleAttachmentAsync(
            courseId,
            moduleId,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);
        return Ok(result);
    }

    [HttpDelete("courses/{courseId:guid}/modules/{moduleId:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteModuleAttachment(
        Guid courseId,
        Guid moduleId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        await authoringService.DeleteModuleAttachmentAsync(courseId, moduleId, attachmentId, cancellationToken);
        return NoContent();
    }

    [HttpPost("courses/{courseId:guid}/scorm-package")]
    [RequestSizeLimit(209_715_200)]
    [RequestFormLimits(MultipartBodyLengthLimit = 209_715_200)]
    [ProducesResponseType(typeof(UniLioScormPackageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioScormPackageDto>> UploadScormPackage(
        Guid courseId,
        IFormFile file,
        [FromForm] int? passingScore,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Arquivo ZIP SCORM obrigatório." });
        }

        await using var stream = file.OpenReadStream();
        var result = await authoringService.UploadScormPackageAsync(
            courseId,
            stream,
            file.FileName,
            file.Length,
            passingScore,
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("courses/{courseId:guid}/assessment")]
    [ProducesResponseType(typeof(UniLioAuthoringAssessmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAuthoringAssessmentDto>> UpsertCourseAssessment(
        Guid courseId,
        [FromBody] UniLioUpsertAssessmentRequest request,
        CancellationToken cancellationToken)
        => Ok(await authoringService.UpsertCourseAssessmentAsync(courseId, request, cancellationToken));

    [HttpDelete("courses/{courseId:guid}/assessment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCourseAssessment(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        await authoringService.DeleteCourseAssessmentAsync(courseId, cancellationToken);
        return NoContent();
    }
}
