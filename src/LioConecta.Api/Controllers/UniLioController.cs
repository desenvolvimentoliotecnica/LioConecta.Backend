using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

// LMS migrando para a app standalone LioConecta.UniLio (Frontend + Backend próprios).
// Endpoints mantidos aqui até o cutover (VITE_UNILIO_APP_URL) — não remover ainda.
[ApiController]
[Route("api/v1/unilio")]
[Authorize]
public sealed class UniLioController(IUniLioService uniLioService) : ControllerBase
{
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(UniLioBootstrapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioBootstrapDto>> GetBootstrap(CancellationToken cancellationToken)
        => Ok(await uniLioService.GetBootstrapAsync(cancellationToken));

    [HttpGet("meta")]
    [ProducesResponseType(typeof(UniLioMetaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioMetaDto>> GetMeta(CancellationToken cancellationToken)
        => Ok(await uniLioService.GetMetaAsync(cancellationToken));

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(UniLioDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioDashboardDto>> GetDashboard(
        [FromQuery] UniLioQuery query,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetDashboardAsync(query, cancellationToken));

    [HttpGet("catalog")]
    [ProducesResponseType(typeof(UniLioCatalogPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioCatalogPageDto>> GetCatalog(
        [FromQuery] UniLioQuery query,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetCatalogAsync(query, cancellationToken));

    [HttpGet("courses/{id:guid}")]
    [ProducesResponseType(typeof(UniLioCourseDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioCourseDetailDto>> GetCourse(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetCourseDetailAsync(id, cancellationToken));

    [HttpGet("courses/{id:guid}/recommendations")]
    [ProducesResponseType(typeof(UniLioRecommendationsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioRecommendationsDto>> GetCourseRecommendations(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetCourseRecommendationsAsync(id, cancellationToken));

    [HttpPost("courses/{id:guid}/start")]
    [ProducesResponseType(typeof(UniLioCourseStartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioCourseStartDto>> StartCourse(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.StartCourseAsync(id, cancellationToken));

    [HttpGet("courses/{id:guid}/enrollments")]
    [ProducesResponseType(typeof(UniLioCourseEnrollmentsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioCourseEnrollmentsDto>> GetCourseEnrollments(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetCourseEnrollmentsAsync(id, cancellationToken));

    [HttpGet("paths")]
    [ProducesResponseType(typeof(UniLioPathsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioPathsDto>> GetPaths(CancellationToken cancellationToken)
        => Ok(await uniLioService.GetPathsAsync(cancellationToken));

    [HttpGet("paths/{id:guid}")]
    [ProducesResponseType(typeof(UniLioPathDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioPathDetailDto>> GetPath(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetPathDetailAsync(id, cancellationToken));

    [HttpPost("courses/{courseId:guid}/modules/{moduleId:guid}/complete")]
    [ProducesResponseType(typeof(UniLioProgressDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioProgressDto>> CompleteModule(
        Guid courseId,
        Guid moduleId,
        [FromBody] UniLioCompleteModuleRequest? request,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.CompleteModuleAsync(courseId, moduleId, request, cancellationToken));

    [HttpGet("assessments")]
    [ProducesResponseType(typeof(UniLioAssessmentsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAssessmentsDto>> GetAssessments(CancellationToken cancellationToken)
        => Ok(await uniLioService.GetAssessmentsAsync(cancellationToken));

    [HttpPost("assessments/{id:guid}/submit")]
    [ProducesResponseType(typeof(UniLioAssessmentResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioAssessmentResultDto>> SubmitAssessment(
        Guid id,
        [FromBody] UniLioAssessmentSubmitRequest request,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.SubmitAssessmentAsync(id, request, cancellationToken));

    [HttpGet("certificates")]
    [ProducesResponseType(typeof(UniLioCertificatesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioCertificatesDto>> GetCertificates(CancellationToken cancellationToken)
        => Ok(await uniLioService.GetCertificatesAsync(cancellationToken));

    [HttpGet("compliance")]
    [ProducesResponseType(typeof(UniLioComplianceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioComplianceDto>> GetCompliance(CancellationToken cancellationToken)
        => Ok(await uniLioService.GetComplianceAsync(cancellationToken));

    [HttpGet("community")]
    [ProducesResponseType(typeof(UniLioCommunityPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioCommunityPageDto>> GetCommunity(
        [FromQuery] UniLioQuery query,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetCommunityAsync(query, cancellationToken));

    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(UniLioRecommendationsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioRecommendationsDto>> GetRecommendations(CancellationToken cancellationToken)
        => Ok(await uniLioService.GetRecommendationsAsync(cancellationToken));

    [HttpGet("events")]
    [ProducesResponseType(typeof(UniLioEventsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioEventsDto>> GetEvents(CancellationToken cancellationToken)
        => Ok(await uniLioService.GetEventsAsync(cancellationToken));

    [HttpGet("skills")]
    [ProducesResponseType(typeof(UniLioSkillsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioSkillsDto>> GetSkills(CancellationToken cancellationToken)
        => Ok(await uniLioService.GetSkillsAsync(cancellationToken));

    [HttpGet("manager/team")]
    [ProducesResponseType(typeof(UniLioManagerTeamDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioManagerTeamDto>> GetManagerTeam(CancellationToken cancellationToken)
        => Ok(await uniLioService.GetManagerTeamAsync(cancellationToken));

    [HttpGet("instructor/courses")]
    [ProducesResponseType(typeof(UniLioInstructorCoursesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioInstructorCoursesDto>> GetInstructorCourses(CancellationToken cancellationToken)
        => Ok(await uniLioService.GetInstructorCoursesAsync(cancellationToken));

    [HttpGet("reports/summary")]
    [ProducesResponseType(typeof(UniLioReportsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioReportsDto>> GetReportsSummary(
        [FromQuery] UniLioQuery query,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetReportsSummaryAsync(query, cancellationToken));

    [HttpPost("courses/{courseId:guid}/modules/{moduleId:guid}/questions")]
    [ProducesResponseType(typeof(UniLioQuestionDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioQuestionDetailDto>> CreateModuleQuestion(
        Guid courseId,
        Guid moduleId,
        [FromBody] CreateUniLioQuestionRequest request,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.CreateModuleQuestionAsync(courseId, moduleId, request, cancellationToken));

    [HttpPost("courses/{courseId:guid}/questions")]
    [ProducesResponseType(typeof(UniLioQuestionDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioQuestionDetailDto>> CreateCourseQuestion(
        Guid courseId,
        [FromBody] CreateUniLioQuestionRequest request,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.CreateModuleQuestionAsync(courseId, null, request, cancellationToken));

    [HttpGet("courses/{courseId:guid}/modules/{moduleId:guid}/questions")]
    [ProducesResponseType(typeof(UniLioQuestionsPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioQuestionsPageDto>> GetModuleQuestions(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetModuleQuestionsAsync(courseId, moduleId, cancellationToken));

    [HttpGet("me/questions")]
    [ProducesResponseType(typeof(UniLioQuestionsPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioQuestionsPageDto>> GetMyQuestions(
        [FromQuery] UniLioQuestionQuery query,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetMyQuestionsAsync(query, cancellationToken));

    [HttpGet("instructor/questions")]
    [ProducesResponseType(typeof(UniLioQuestionsPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioQuestionsPageDto>> GetInstructorQuestions(
        [FromQuery] UniLioQuestionQuery query,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetInstructorQuestionsAsync(query, cancellationToken));

    [HttpGet("instructor/questions/{id:guid}")]
    [ProducesResponseType(typeof(UniLioQuestionDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioQuestionDetailDto>> GetInstructorQuestionDetail(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetInstructorQuestionDetailAsync(id, cancellationToken));

    [HttpPost("instructor/questions/{id:guid}/reply")]
    [ProducesResponseType(typeof(UniLioQuestionDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioQuestionDetailDto>> ReplyToQuestion(
        Guid id,
        [FromBody] ReplyUniLioQuestionRequest request,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.ReplyToQuestionAsync(id, request, cancellationToken));

    [HttpPatch("instructor/questions/{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkInstructorQuestionRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        await uniLioService.MarkInstructorQuestionReadAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPatch("questions/{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkLearnerQuestionRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        await uniLioService.MarkLearnerQuestionReadAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("courses/{courseId:guid}/scorm/runtime")]
    [ProducesResponseType(typeof(UniLioScormRuntimeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioScormRuntimeDto>> GetScormRuntime(
        Guid courseId,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.GetScormRuntimeAsync(courseId, cancellationToken));

    [HttpPut("courses/{courseId:guid}/scorm/runtime")]
    [ProducesResponseType(typeof(UniLioScormRuntimeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniLioScormRuntimeDto>> UpdateScormRuntime(
        Guid courseId,
        [FromBody] UniLioScormRuntimeUpdateRequest request,
        CancellationToken cancellationToken)
        => Ok(await uniLioService.UpdateScormRuntimeAsync(courseId, request, cancellationToken));
}
