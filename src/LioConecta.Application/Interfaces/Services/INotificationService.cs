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

    Task NotifyNewsPublishedAsync(FeedPost post, CancellationToken cancellationToken = default);

    Task NotifyPeerFeedbackAsync(
        FeedbackSubmission feedback,
        IReadOnlyList<Guid> recipientPersonIds,
        CancellationToken cancellationToken = default);

    Task NotifyPollCreatedAsync(FeedPost post, Poll poll, CancellationToken cancellationToken = default);

    Task NotifyPollClosedAsync(FeedPost post, Poll poll, CancellationToken cancellationToken = default);

    Task NotifyLeaveRequestCreatedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid leaveRecordId,
        string summary,
        CancellationToken cancellationToken = default,
        string? title = null);

    Task NotifyPontoAdjustmentCreatedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid adjustmentRecordId,
        string summary,
        CancellationToken cancellationToken = default,
        string? title = null);

    Task NotifyBirthdayCongratsAsync(
        FeedPost post,
        Person celebrated,
        Person author,
        CancellationToken cancellationToken = default);

    Task NotifyNewHireAsync(
        FeedPost post,
        Person newHire,
        CancellationToken cancellationToken = default);

    Task NotifyFeedPostLikedAsync(
        FeedPost post,
        Person liker,
        CancellationToken cancellationToken = default);

    Task NotifyFeedPostCommentedAsync(
        FeedPost post,
        Person commenter,
        string commentText,
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

    Task NotifyGroupCreationRequestedAsync(
        Guid managerPersonId,
        Guid groupId,
        string groupName,
        string requesterName,
        CancellationToken cancellationToken = default);

    Task NotifyGroupCreationDecisionAsync(
        Guid ownerPersonId,
        Guid groupId,
        string groupName,
        bool approved,
        string? reason,
        CancellationToken cancellationToken = default);

    Task NotifyGroupCreationExpiredAsync(
        Guid ownerPersonId,
        Guid groupId,
        string groupName,
        CancellationToken cancellationToken = default);

    Task NotifyGroupWallPostAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid groupId,
        string groupName,
        string authorName,
        CancellationToken cancellationToken = default);

    Task NotifyGroupTopicCreatedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid groupId,
        Guid topicId,
        string groupName,
        string topicTitle,
        string authorName,
        CancellationToken cancellationToken = default);

    Task NotifyGroupTopicReplyAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid groupId,
        Guid topicId,
        string groupName,
        string topicTitle,
        string authorName,
        CancellationToken cancellationToken = default);

    Task NotifyGroupOwnershipTransferRequestedAsync(
        Guid toPersonId,
        Guid managerPersonId,
        Guid groupId,
        string groupName,
        string fromOwnerName,
        string toPersonName,
        CancellationToken cancellationToken = default);

    Task NotifyGroupOwnershipTransferDecisionAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid groupId,
        string groupName,
        bool approved,
        string? reason,
        CancellationToken cancellationToken = default);
}
