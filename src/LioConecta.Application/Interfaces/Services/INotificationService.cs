using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(
        CursorPageRequest request,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);

    Task NotifyComunicadoCreatedAsync(Comunicado comunicado, CancellationToken cancellationToken = default);

    Task NotifyPollCreatedAsync(FeedPost post, Poll poll, CancellationToken cancellationToken = default);

    Task NotifyPollClosedAsync(FeedPost post, Poll poll, CancellationToken cancellationToken = default);

    Task NotifyLeaveRequestCreatedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid leaveRecordId,
        string summary,
        CancellationToken cancellationToken = default);

    Task NotifyBirthdayCongratsAsync(
        FeedPost post,
        Person celebrated,
        Person author,
        CancellationToken cancellationToken = default);

    Task NotifyFeedPostLikedAsync(
        FeedPost post,
        Person liker,
        CancellationToken cancellationToken = default);

    Task NotifyUniLioCourseSubmittedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid courseId,
        string courseTitle,
        string submitterName,
        CancellationToken cancellationToken = default);

    Task NotifyUniLioCourseReviewedAsync(
        Guid instructorPersonId,
        Guid courseId,
        string courseTitle,
        bool approved,
        string? rejectionReason,
        CancellationToken cancellationToken = default);

    Task NotifyUniLioCoursePublishedAsync(
        UniLioCourse course,
        CancellationToken cancellationToken = default);

    Task NotifyUniLioCourseCompletedToInstructorAsync(
        Guid instructorPersonId,
        string learnerName,
        string courseTitle,
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task NotifyUniLioQuestionToInstructorAsync(
        Guid instructorPersonId,
        string learnerName,
        string courseTitle,
        string? moduleTitle,
        Guid questionId,
        CancellationToken cancellationToken = default);

    Task NotifyUniLioQuestionAnsweredToLearnerAsync(
        Guid learnerPersonId,
        string courseTitle,
        string? moduleTitle,
        Guid questionId,
        CancellationToken cancellationToken = default);
}
