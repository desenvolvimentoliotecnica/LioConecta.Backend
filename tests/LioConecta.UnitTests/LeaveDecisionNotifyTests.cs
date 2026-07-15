using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.UnitTests;

public sealed class LeaveEmailNotifierDecisionTests
{
    [Fact]
    public async Task NotifyDecisionAsync_RespectsEmailDisabledFlag()
    {
        var queue = new FakeEmailQueue();
        var settings = new FakeSettings(emailEnabled: false, overrideEnabled: false);
        var notifier = new LeaveEmailNotifier(queue, settings);

        await notifier.NotifyDecisionAsync(
            CreateRecord(),
            CreateRequester(),
            approved: true,
            decisionNote: null,
            serviceTitle: "Solicitar Férias");

        Assert.Empty(queue.Requests);
    }

    [Fact]
    public async Task NotifyDecisionAsync_EnqueuesApprovedEmailForRequester()
    {
        var queue = new FakeEmailQueue();
        var settings = new FakeSettings(emailEnabled: true, overrideEnabled: false);
        var notifier = new LeaveEmailNotifier(queue, settings);
        var record = CreateRecord();
        var requester = CreateRequester();

        await notifier.NotifyDecisionAsync(
            record,
            requester,
            approved: true,
            decisionNote: "OK",
            serviceTitle: "Solicitar Férias");

        var msg = Assert.Single(queue.Requests);
        Assert.Equal(requester.Email, Assert.Single(msg.To));
        Assert.Contains("aprovada", msg.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Comentário", msg.BodyText ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"/servicos/ferias-ausencias?requestId={record.Id}", msg.BodyText ?? "");
        Assert.Contains("leave.request.approved", msg.MetadataJson ?? "");
    }

    [Fact]
    public async Task NotifyDecisionAsync_EnqueuesRejectedEmailWithReason()
    {
        var queue = new FakeEmailQueue();
        var settings = new FakeSettings(emailEnabled: true, overrideEnabled: false);
        var notifier = new LeaveEmailNotifier(queue, settings);

        await notifier.NotifyDecisionAsync(
            CreateRecord(),
            CreateRequester(),
            approved: false,
            decisionNote: "Sem cobertura",
            serviceTitle: "Solicitar Férias");

        var msg = Assert.Single(queue.Requests);
        Assert.Contains("rejeitada", msg.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Motivo: Sem cobertura", msg.BodyText ?? "");
        Assert.Contains("leave.request.rejected", msg.MetadataJson ?? "");
    }

    private static LeaveRecord CreateRecord() =>
        new()
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            PersonId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ServiceKey = "solicitar-ferias",
            Title = "Solicitação de férias",
            Status = "approved",
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 8, 15),
            Days = 15,
        };

    private static Person CreateRequester() =>
        new()
        {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Name = "Maria Silva",
            Email = "maria.silva@liotecnica.com.br",
            EmployeeId = "12345",
            IsActive = true,
        };

    private sealed class FakeEmailQueue : IEmailQueueService
    {
        public List<EmailEnqueueRequest> Requests { get; } = [];

        public Task<EmailMessageDto> EnqueueAsync(EmailEnqueueRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new EmailMessageDto(
                Guid.NewGuid(),
                "queued",
                request.To,
                request.Cc ?? [],
                request.Bcc ?? [],
                request.Subject,
                request.BodyHtml,
                request.BodyText,
                request.TemplateKey,
                request.MetadataJson,
                request.Priority,
                request.IdempotencyKey,
                request.CorrelationId,
                AttemptCount: 0,
                MaxAttempts: 5,
                LastError: null,
                ProviderMessageId: null,
                ScheduledAt: now,
                NextRetryAt: null,
                ProcessingStartedAt: null,
                SentAt: null,
                CreatedAt: now,
                UpdatedAt: now));
        }
    }

    private sealed class FakeSettings(bool emailEnabled, bool overrideEnabled) : IAppSettingsProvider
    {
        public string GetString(string key, string defaultValue = "") =>
            key == AppSettingKeys.LeaveEmailDevOverrideTo
                ? "override@liotecnica.com.br"
                : defaultValue;

        public bool TryGetString(string key, out string value)
        {
            value = GetString(key);
            return !string.IsNullOrEmpty(value);
        }

        public bool GetBool(string key, bool defaultValue = false) =>
            key switch
            {
                AppSettingKeys.LeaveEmailEnabled => emailEnabled,
                AppSettingKeys.LeaveEmailDevOverrideEnabled => overrideEnabled,
                _ => defaultValue,
            };

        public int GetInt(string key, int defaultValue = 0) => defaultValue;

        public IReadOnlyList<string> GetStringArray(string key) => [];

        public string GetConnectionString() => string.Empty;

        public string GetRedisConnection() => string.Empty;

        public void Reload(IReadOnlyDictionary<string, string> values)
        {
        }
    }
}

public sealed class LeaveServiceDecisionNotifyTests
{
    [Fact]
    public async Task ApproveAsync_NotifiesCollaboratorInAppAndEmail()
    {
        var harness = LeaveDecisionHarness.Create();
        var result = await harness.Service.ApproveAsync(
            harness.RecordId,
            new ApproveLeaveRequestDto("Aprovado", TriggerWriteBack: false));

        Assert.NotNull(result);
        Assert.Equal("approved", result!.Status);
        Assert.Equal(1, harness.Notifications.DecisionCalls);
        Assert.True(harness.Notifications.LastApproved);
        Assert.Equal(harness.RequesterId, harness.Notifications.LastRequesterId);
        Assert.Equal(1, harness.Emails.DecisionCalls);
        Assert.True(harness.Emails.LastApproved);
    }

    [Fact]
    public async Task RejectAsync_NotifiesCollaboratorWithReason()
    {
        var harness = LeaveDecisionHarness.Create();
        var result = await harness.Service.RejectAsync(
            harness.RecordId,
            new RejectLeaveRequestDto("Sem cobertura"));

        Assert.NotNull(result);
        Assert.Equal("rejected", result!.Status);
        Assert.Equal(1, harness.Notifications.DecisionCalls);
        Assert.False(harness.Notifications.LastApproved);
        Assert.Equal("Sem cobertura", harness.Notifications.LastReason);
        Assert.Equal(1, harness.Emails.DecisionCalls);
        Assert.False(harness.Emails.LastApproved);
        Assert.Equal("Sem cobertura", harness.Emails.LastNote);
    }

    [Fact]
    public async Task ApproveAsync_DoesNotFailWhenNotifiersThrow()
    {
        var harness = LeaveDecisionHarness.Create(throwOnNotify: true);
        var result = await harness.Service.ApproveAsync(
            harness.RecordId,
            new ApproveLeaveRequestDto(null, TriggerWriteBack: false));

        Assert.NotNull(result);
        Assert.Equal("approved", result!.Status);
    }

    private sealed class LeaveDecisionHarness
    {
        public required Guid RecordId { get; init; }
        public required Guid RequesterId { get; init; }
        public required LeaveService Service { get; init; }
        public required TrackingNotificationService Notifications { get; init; }
        public required TrackingLeaveEmailNotifier Emails { get; init; }

        public static LeaveDecisionHarness Create(bool throwOnNotify = false)
        {
            var managerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var requesterId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var recordId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

            var requester = new Person
            {
                Id = requesterId,
                Name = "Colaborador Teste",
                Email = "colab@liotecnica.com.br",
                EmployeeId = "999",
                IsActive = true,
            };

            var record = new LeaveRecord
            {
                Id = recordId,
                PersonId = requesterId,
                Person = requester,
                ServiceKey = "solicitar-ferias",
                Title = "Solicitação de férias",
                Status = "pending",
                StartDate = new DateOnly(2026, 9, 1),
                EndDate = new DateOnly(2026, 9, 10),
                Days = 10,
                DetailsJson = "{}",
            };

            var leaveRepo = new StubLeaveRepository(record);
            var personRepo = new StubPersonRepository(requester);
            var notifications = new TrackingNotificationService(throwOnNotify);
            var emails = new TrackingLeaveEmailNotifier(throwOnNotify);
            var resolver = new LeaveNotifyRecipientResolver(
                personRepo,
                new StubLeaveNotifyDirectory(),
                new StubPermissionService(),
                new StubSettings());

            var service = new LeaveService(
                leaveRepo,
                new StubLeaveSync(),
                new StubServiceRequestService(),
                new StubCurrentUser(managerId),
                new StubSettings(),
                new StubTotvsRmConfig(),
                personRepo,
                resolver,
                notifications,
                emails,
                new StubLeaveAttachmentStore(),
                new StubHourBankService(),
                new StubLeaveRmWriteBack());

            return new LeaveDecisionHarness
            {
                RecordId = recordId,
                RequesterId = requesterId,
                Service = service,
                Notifications = notifications,
                Emails = emails,
            };
        }
    }

    private sealed class TrackingNotificationService(bool throwOnCall) : INotificationService
    {
        public int DecisionCalls { get; private set; }
        public Guid? LastRequesterId { get; private set; }
        public bool LastApproved { get; private set; }
        public string? LastReason { get; private set; }

        public Task NotifyLeaveRequestDecisionAsync(
            Guid requesterPersonId,
            Guid leaveRecordId,
            string serviceKey,
            string periodLabel,
            bool approved,
            string? reason,
            CancellationToken cancellationToken = default)
        {
            if (throwOnCall)
            {
                throw new InvalidOperationException("notify failed");
            }

            DecisionCalls++;
            LastRequesterId = requesterPersonId;
            LastApproved = approved;
            LastReason = reason;
            return Task.CompletedTask;
        }

        public Task<PagedResult<NotificationDto>> GetNotificationsAsync(CursorPageRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(PagedResult<NotificationDto>.Empty);

        public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkAllAsReadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyComunicadoCreatedAsync(Comunicado comunicado, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyNewsPublishedAsync(FeedPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyPeerFeedbackAsync(FeedbackSubmission feedback, IReadOnlyList<Guid> recipientPersonIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyPollCreatedAsync(FeedPost post, Poll poll, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyPollClosedAsync(FeedPost post, Poll poll, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyLeaveRequestCreatedAsync(IReadOnlyList<Guid> recipientPersonIds, Guid leaveRecordId, string summary, CancellationToken cancellationToken = default, string? title = null) => Task.CompletedTask;
        public Task NotifyPontoAdjustmentCreatedAsync(IReadOnlyList<Guid> recipientPersonIds, Guid adjustmentRecordId, string summary, CancellationToken cancellationToken = default, string? title = null) => Task.CompletedTask;
        public Task NotifyServiceRequestCreatedAsync(IReadOnlyList<Guid> recipientPersonIds, Guid serviceRequestId, string summary, CancellationToken cancellationToken = default, string? title = null) => Task.CompletedTask;
        public Task NotifyServiceRequestDecisionAsync(Guid requesterPersonId, Guid serviceRequestId, string requestType, bool approved, string? reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyServiceRequestMessageAsync(Guid recipientPersonId, Guid serviceRequestId, string requestType, string actorName, bool fromRh, string preview, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyServiceRequestFinalizedAsync(Guid requesterPersonId, Guid serviceRequestId, string requestType, string? comment, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyServiceRequestClosureConfirmedAsync(IReadOnlyList<Guid> recipientPersonIds, Guid serviceRequestId, string requestType, string requesterName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyBirthdayCongratsAsync(FeedPost post, Person celebrated, Person author, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyNewHireAsync(FeedPost post, Person newHire, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyFeedPostLikedAsync(FeedPost post, Person liker, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyFeedPostCommentedAsync(FeedPost post, Person commenter, string commentText, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyUniLioCourseSubmittedAsync(IReadOnlyList<Guid> recipientPersonIds, Guid courseId, string courseTitle, string submitterName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyUniLioCourseReviewedAsync(Guid instructorPersonId, Guid courseId, string courseTitle, bool approved, string? rejectionReason, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyUniLioCoursePublishedAsync(UniLioCourse course, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyUniLioCourseCompletedToInstructorAsync(Guid instructorPersonId, string learnerName, string courseTitle, Guid courseId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyUniLioQuestionToInstructorAsync(Guid instructorPersonId, string learnerName, string courseTitle, string? moduleTitle, Guid questionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyUniLioQuestionAnsweredToLearnerAsync(Guid learnerPersonId, string courseTitle, string? moduleTitle, Guid questionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyGroupCreationRequestedAsync(Guid managerPersonId, Guid groupId, string groupName, string requesterName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyGroupCreationDecisionAsync(Guid ownerPersonId, Guid groupId, string groupName, bool approved, string? reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyGroupCreationExpiredAsync(Guid ownerPersonId, Guid groupId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyGroupWallPostAsync(IReadOnlyList<Guid> recipientPersonIds, Guid groupId, string groupName, string authorName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyGroupTopicCreatedAsync(IReadOnlyList<Guid> recipientPersonIds, Guid groupId, Guid topicId, string groupName, string topicTitle, string authorName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyGroupTopicReplyAsync(IReadOnlyList<Guid> recipientPersonIds, Guid groupId, Guid topicId, string groupName, string topicTitle, string authorName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyGroupOwnershipTransferRequestedAsync(Guid toPersonId, Guid managerPersonId, Guid groupId, string groupName, string fromOwnerName, string toPersonName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyGroupOwnershipTransferDecisionAsync(IReadOnlyList<Guid> recipientPersonIds, Guid groupId, string groupName, bool approved, string? reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TrackingLeaveEmailNotifier(bool throwOnCall) : ILeaveEmailNotifier
    {
        public int DecisionCalls { get; private set; }
        public bool LastApproved { get; private set; }
        public string? LastNote { get; private set; }

        public Task NotifyRequestCreatedAsync(
            LeaveRecord record,
            Person requester,
            IReadOnlyList<Person> recipients,
            string serviceTitle,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyDecisionAsync(
            LeaveRecord record,
            Person requester,
            bool approved,
            string? decisionNote,
            string serviceTitle,
            CancellationToken cancellationToken = default)
        {
            if (throwOnCall)
            {
                throw new InvalidOperationException("email failed");
            }

            DecisionCalls++;
            LastApproved = approved;
            LastNote = decisionNote;
            return Task.CompletedTask;
        }
    }

    private sealed class StubLeaveRepository(LeaveRecord record) : ILeaveRepository
    {
        public Task<LeaveRecord?> GetRecordWithPersonAsync(Guid recordId, CancellationToken cancellationToken = default) =>
            Task.FromResult<LeaveRecord?>(recordId == record.Id ? record : null);

        public Task UpdateRecordAsync(LeaveRecord entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<EmployeeLeaveBalance?> GetBalanceAsync(Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmployeeLeaveBalance?>(null);

        public Task<DateTimeOffset?> GetBalanceSyncedAtAsync(Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult<DateTimeOffset?>(null);

        public Task UpsertBalanceAsync(EmployeeLeaveBalance balance, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<LeaveRecord>> ListRecordsAsync(Guid personId, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LeaveRecord>>([]);

        public Task<IReadOnlyList<LeaveRecord>> ListRequestsAsync(Guid personId, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LeaveRecord>>([]);

        public Task<LeaveRecord?> GetRecordByIdAsync(Guid recordId, CancellationToken cancellationToken = default) =>
            Task.FromResult<LeaveRecord?>(null);

        public Task<IReadOnlyList<LeaveRecord>> ListManagementAsync(IReadOnlyList<Guid>? personIds, string? status, string? query, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LeaveRecord>>([]);

        public Task<int> CountPendingAsync(Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task AddRecordAsync(LeaveRecord entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<LeaveRecord>> ListPendingWriteBackAsync(int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LeaveRecord>>([]);

        public Task UpsertRmRecordAsync(LeaveRecord entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubPersonRepository(Person person) : IPersonRepository
    {
        public Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Person?>(id == person.Id ? person : null);

        public Task<Person?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult<Person?>(null);

        public Task<IReadOnlyList<Person>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Person>>([]);

        public Task<IReadOnlyList<Person>> GetOrgChartPeopleAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Person>>([]);

        public Task<IReadOnlyList<Person>> GetDirectoryPeopleAsync(string? query, string? departmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Person>>([]);

        public Task<IReadOnlyList<Person>> GetPeersAsync(Guid personId, Guid managerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Person>>([]);

        public Task<IReadOnlyList<Person>> GetDirectReportsAsync(Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Person>>([]);

        public Task AddAsync(Person entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(Person entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<Person>> GetByAzureObjectIdsAsync(IEnumerable<Guid> objectIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Person>>([]);

        public Task<Person?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<Person?>(null);

        public Task<IReadOnlyList<Person>> GetByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Person>>([]);

        public Task<IReadOnlyList<Person>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Person>>(ids.Contains(person.Id) ? [person] : []);
    }

    private sealed class StubCurrentUser(Guid personId) : ICurrentUserService
    {
        public Task<Guid> GetPersonIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(personId);

        public Task<IReadOnlyList<UserRole>> GetRolesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserRole>>([UserRole.Manager]);

        public Task<ViewerContext> GetViewerContextAsync(Guid targetPersonId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class StubPermissionService : IPermissionService
    {
        public Task<RbacAuthContext?> GetAuthContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<RbacAuthContext?>(null);

        public Task<IReadOnlyList<EffectivePermissionDto>> GetEffectivePermissionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EffectivePermissionDto>>([]);

        public Task<bool> HasPermissionAsync(
            string permissionKey,
            DataScope? requiredScope = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                permissionKey is "leave.approve" or "leave.manage"
                && requiredScope is null or DataScope.Global);

        public Task EnsurePermissionAsync(
            string permissionKey,
            DataScope? requiredScope = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<RbacBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class StubLeaveNotifyDirectory : ILeaveNotifyDirectory
    {
        public Task<IReadOnlyList<Person>> FindActivePeopleByPortalRolesAsync(
            IReadOnlyList<string> roles,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Person>>([]);
    }

    private sealed class StubSettings : IAppSettingsProvider
    {
        public string GetString(string key, string defaultValue = "") => defaultValue;
        public bool TryGetString(string key, out string value) { value = string.Empty; return false; }
        public bool GetBool(string key, bool defaultValue = false) => defaultValue;
        public int GetInt(string key, int defaultValue = 0) => defaultValue;
        public IReadOnlyList<string> GetStringArray(string key) => [];
        public string GetConnectionString() => string.Empty;
        public string GetRedisConnection() => string.Empty;
        public void Reload(IReadOnlyDictionary<string, string> values) { }
    }

    private sealed class StubLeaveSync : ILeaveSyncService
    {
        public Task<LeaveSyncResultDto> SyncPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LeaveSyncResultDto(0, "ok", null, null));

        public Task<int> SyncAllActivePeopleAsync(IWorkerRunContext? context, CancellationToken cancellationToken) =>
            Task.FromResult(0);
    }

    private sealed class StubServiceRequestService : IServiceRequestService
    {
        public Task<IReadOnlyList<ServiceRequestDto>> GetMineAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ServiceRequestDto>>([]);

        public Task<ServiceRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceRequestDto?>(null);

        public Task<ServiceRequestDto> CreateAsync(CreateServiceRequestRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<ServiceRequestDto>> GetManagementListAsync(ServiceRequestStatus? status, string? query, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ServiceRequestDto>>([]);

        public Task<ServiceRequestDto?> GetManagementDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceRequestDto?>(null);

        public Task<ServiceRequestDto?> ApproveAsync(Guid id, ApproveServiceRequestDto request, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceRequestDto?>(null);

        public Task<ServiceRequestDto?> RejectAsync(Guid id, RejectServiceRequestDto request, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceRequestDto?>(null);

        public Task<ServiceRequestDto?> ReplyAsManagerAsync(Guid id, string? message, IReadOnlyList<ServiceRequestAttachmentInput>? attachments, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceRequestDto?>(null);

        public Task<ServiceRequestDto?> ReplyAsRequesterAsync(Guid id, string? message, IReadOnlyList<ServiceRequestAttachmentInput>? attachments, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceRequestDto?>(null);

        public Task<ServiceRequestDto?> FinalizeAsync(Guid id, FinalizeServiceRequestDto request, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceRequestDto?>(null);

        public Task<ServiceRequestDto?> ConfirmClosureAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceRequestDto?>(null);

        public Task<ServiceRequestAttachmentFileDto?> GetAttachmentAsync(Guid id, string storageFileName, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceRequestAttachmentFileDto?>(null);
    }

    private sealed class StubTotvsRmConfig : ITotvsRmConfigurationService
    {
        public Task<TotvsRmConfigurationDto> GetAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<TotvsRmConfigurationDto> SaveAsync(UpsertTotvsRmConfigurationRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<TotvsRmRuntimeConfiguration> GetRuntimeConfigurationAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<TotvsRmConnectionTestResponse> TestConnectionAsync(UpsertTotvsRmConfigurationRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task EnsureDefaultConfigurationAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class StubLeaveAttachmentStore : ILeaveAttachmentStore
    {
        public Task<LeaveAttachmentMetaDto> SaveAsync(Stream content, string fileName, string? contentType, long sizeBytes, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public string? ResolveAbsolutePath(string storageFileName) => null;
    }

    private sealed class StubHourBankService : IHourBankService
    {
        public Task<LeaveBancoHorasDto> GetMineAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<HourBankTeamMemberDto>> GetTeamAsync(string? query = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HourBankTeamMemberDto>>([]);

        public Task<LeaveBancoHorasDto> GetForPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class StubLeaveRmWriteBack : ILeaveRmWriteBack
    {
        public Task<LeaveRmWriteBackResult> SubmitVacationRequestAsync(
            LeaveRmWriteBackCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new LeaveRmWriteBackResult(true, "synced", null, "ok"));
    }
}
