using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IUniLioAuthoringService
{
    Task<UniLioAuthoringCourseListDto> ListCoursesAsync(CancellationToken cancellationToken = default);

    Task<UniLioAuthoringCourseListDto> ListPendingCoursesAsync(CancellationToken cancellationToken = default);

    Task<UniLioAuthoringCourseDto> GetCourseAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UniLioApprovalReviewDto> GetApprovalReviewAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UniLioAuthoringCourseDto> CreateCourseAsync(
        UniLioUpsertCourseRequest request,
        CancellationToken cancellationToken = default);

    Task<UniLioAuthoringCourseDto> UpdateCourseAsync(
        Guid id,
        UniLioUpsertCourseRequest request,
        CancellationToken cancellationToken = default);

    Task<UniLioAuthoringCourseDto> SubmitCourseAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UniLioAuthoringCourseDto> ApproveCourseAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UniLioAuthoringCourseDto> RejectCourseAsync(
        Guid id,
        UniLioRejectCourseRequest request,
        CancellationToken cancellationToken = default);

    Task<UniLioAuthoringCourseDto> PublishCourseDirectAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UniLioModuleDto> AddModuleAsync(
        Guid courseId,
        UniLioUpsertModuleRequest request,
        CancellationToken cancellationToken = default);

    Task<UniLioModuleDto> UpdateModuleAsync(
        Guid courseId,
        Guid moduleId,
        UniLioUpsertModuleRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteModuleAsync(Guid courseId, Guid moduleId, CancellationToken cancellationToken = default);
}
