using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IUniLioService
{
    Task<UniLioBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default);

    Task<UniLioMetaDto> GetMetaAsync(CancellationToken cancellationToken = default);

    Task<UniLioDashboardDto> GetDashboardAsync(UniLioQuery query, CancellationToken cancellationToken = default);

    Task<UniLioCatalogPageDto> GetCatalogAsync(UniLioQuery query, CancellationToken cancellationToken = default);

    Task<UniLioCourseDetailDto> GetCourseDetailAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<UniLioPathsDto> GetPathsAsync(CancellationToken cancellationToken = default);

    Task<UniLioPathDetailDto> GetPathDetailAsync(Guid pathId, CancellationToken cancellationToken = default);

    Task<UniLioProgressDto> CompleteModuleAsync(Guid courseId, Guid moduleId, CancellationToken cancellationToken = default);

    Task<UniLioAssessmentsDto> GetAssessmentsAsync(CancellationToken cancellationToken = default);

    Task<UniLioAssessmentResultDto> SubmitAssessmentAsync(
        Guid assessmentId,
        UniLioAssessmentSubmitRequest request,
        CancellationToken cancellationToken = default);

    Task<UniLioCertificatesDto> GetCertificatesAsync(CancellationToken cancellationToken = default);

    Task<UniLioComplianceDto> GetComplianceAsync(CancellationToken cancellationToken = default);

    Task<UniLioCommunityPageDto> GetCommunityAsync(UniLioQuery query, CancellationToken cancellationToken = default);

    Task<UniLioRecommendationsDto> GetRecommendationsAsync(CancellationToken cancellationToken = default);

    Task<UniLioEventsDto> GetEventsAsync(CancellationToken cancellationToken = default);

    Task<UniLioSkillsDto> GetSkillsAsync(CancellationToken cancellationToken = default);

    Task<UniLioManagerTeamDto> GetManagerTeamAsync(CancellationToken cancellationToken = default);

    Task<UniLioInstructorCoursesDto> GetInstructorCoursesAsync(CancellationToken cancellationToken = default);

    Task<UniLioReportsDto> GetReportsSummaryAsync(UniLioQuery query, CancellationToken cancellationToken = default);
}
