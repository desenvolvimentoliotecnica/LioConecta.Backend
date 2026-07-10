using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Person> People => Set<Person>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<FeedPost> FeedPosts => Set<FeedPost>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<PostMediaComment> PostMediaComments => Set<PostMediaComment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollOption> PollOptions => Set<PollOption>();
    public DbSet<PollVote> PollVotes => Set<PollVote>();
    public DbSet<Celebration> Celebrations => Set<Celebration>();
    public DbSet<Comunicado> Comunicados => Set<Comunicado>();
    public DbSet<ComunicadoRead> ComunicadoReads => Set<ComunicadoRead>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupPost> GroupPosts => Set<GroupPost>();
    public DbSet<DocumentMetadata> Documents => Set<DocumentMetadata>();
    public DbSet<BookmarkCatalogItem> BookmarkCatalogItems => Set<BookmarkCatalogItem>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceRequestEvent> ServiceRequestEvents => Set<ServiceRequestEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<CafeteriaMenu> CafeteriaMenus => Set<CafeteriaMenu>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<ObservabilityEvent> ObservabilityEvents => Set<ObservabilityEvent>();
    public DbSet<PageView> PageViews => Set<PageView>();
    public DbSet<AccessEvent> AccessEvents => Set<AccessEvent>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<MoodCheck> MoodChecks => Set<MoodCheck>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<ComunicadoHeroImage> ComunicadoHeroImages => Set<ComunicadoHeroImage>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<IncomeStatement> IncomeStatements => Set<IncomeStatement>();
    public DbSet<EmployeeBenefit> EmployeeBenefits => Set<EmployeeBenefit>();
    public DbSet<BenefitCatalog> BenefitCatalogs => Set<BenefitCatalog>();
    public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalances => Set<EmployeeLeaveBalance>();
    public DbSet<LeaveRecord> LeaveRecords => Set<LeaveRecord>();
    public DbSet<PontoAdjustmentRecord> PontoAdjustmentRecords => Set<PontoAdjustmentRecord>();
    public DbSet<TotvsRmConfiguration> TotvsRmConfigurations => Set<TotvsRmConfiguration>();
    public DbSet<WorkerRun> WorkerRuns => Set<WorkerRun>();
    public DbSet<WorkerRunLog> WorkerRunLogs => Set<WorkerRunLog>();
    public DbSet<TimesheetPeriodCache> TimesheetPeriodCaches => Set<TimesheetPeriodCache>();
    public DbSet<EmailConfiguration> EmailConfigurations => Set<EmailConfiguration>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();

    public DbSet<EmailAttachmentStaging> EmailAttachmentStagings => Set<EmailAttachmentStaging>();
    public DbSet<UserTeamsToken> UserTeamsTokens => Set<UserTeamsToken>();
    public DbSet<PortalUser> PortalUsers => Set<PortalUser>();
    public DbSet<OrgChartSettings> OrgChartSettings => Set<OrgChartSettings>();
    public DbSet<OrgDepartment> OrgDepartments => Set<OrgDepartment>();
    public DbSet<OrgDepartmentMapping> OrgDepartmentMappings => Set<OrgDepartmentMapping>();
    public DbSet<OrgPosition> OrgPositions => Set<OrgPosition>();
    public DbSet<CompassIbpSnapshot> CompassIbpSnapshots => Set<CompassIbpSnapshot>();
    public DbSet<CompassIbpRow> CompassIbpRows => Set<CompassIbpRow>();
    public DbSet<UniLioCourse> UniLioCourses => Set<UniLioCourse>();
    public DbSet<UniLioCourseModule> UniLioCourseModules => Set<UniLioCourseModule>();
    public DbSet<UniLioLearningPath> UniLioLearningPaths => Set<UniLioLearningPath>();
    public DbSet<UniLioPathCourse> UniLioPathCourses => Set<UniLioPathCourse>();
    public DbSet<UniLioSkill> UniLioSkills => Set<UniLioSkill>();
    public DbSet<UniLioCourseSkill> UniLioCourseSkills => Set<UniLioCourseSkill>();
    public DbSet<UniLioEnrollment> UniLioEnrollments => Set<UniLioEnrollment>();
    public DbSet<UniLioModuleProgress> UniLioModuleProgress => Set<UniLioModuleProgress>();
    public DbSet<UniLioAssessment> UniLioAssessments => Set<UniLioAssessment>();
    public DbSet<UniLioAssessmentAttempt> UniLioAssessmentAttempts => Set<UniLioAssessmentAttempt>();
    public DbSet<UniLioCertificate> UniLioCertificates => Set<UniLioCertificate>();
    public DbSet<UniLioEvent> UniLioEvents => Set<UniLioEvent>();
    public DbSet<UniLioEventRegistration> UniLioEventRegistrations => Set<UniLioEventRegistration>();
    public DbSet<UniLioCommunityPost> UniLioCommunityPosts => Set<UniLioCommunityPost>();
    public DbSet<UniLioPersonSkill> UniLioPersonSkills => Set<UniLioPersonSkill>();
    public DbSet<UniLioIntegrationLink> UniLioIntegrationLinks => Set<UniLioIntegrationLink>();
    public DbSet<UniLioModuleQuestion> UniLioModuleQuestions => Set<UniLioModuleQuestion>();
    public DbSet<UniLioModuleQuestionReply> UniLioModuleQuestionReplies => Set<UniLioModuleQuestionReply>();
    public DbSet<UniLioModuleAttachment> UniLioModuleAttachments => Set<UniLioModuleAttachment>();
    public DbSet<PhoneExtension> PhoneExtensions => Set<PhoneExtension>();
    public DbSet<PortalSystem> PortalSystems => Set<PortalSystem>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SubjectRoleAssignment> SubjectRoleAssignments => Set<SubjectRoleAssignment>();
    public DbSet<TestUser> TestUsers => Set<TestUser>();
    public DbSet<DbExplorerQueryLog> DbExplorerQueryLogs => Set<DbExplorerQueryLog>();
    public DbSet<DbExplorerSavedQuery> DbExplorerSavedQueries => Set<DbExplorerSavedQuery>();
    public DbSet<DbExplorerDerLayout> DbExplorerDerLayouts => Set<DbExplorerDerLayout>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ApplySnakeCaseTableNames(modelBuilder);
        ConfigurePerson(modelBuilder);
        ConfigureDepartment(modelBuilder);
        ConfigureFeed(modelBuilder);
        ConfigureComunicado(modelBuilder);
        ConfigureGroup(modelBuilder);
        ConfigureDocuments(modelBuilder);
        ConfigureBookmarkCatalog(modelBuilder);
        ConfigureServiceRequests(modelBuilder);
        ConfigureNotifications(modelBuilder);
        ConfigureChat(modelBuilder);
        ConfigureCalendar(modelBuilder);
        ConfigureAnalytics(modelBuilder);
        ConfigureObservability(modelBuilder);
        ConfigureUserPreferences(modelBuilder);
        ConfigureMoodChecks(modelBuilder);
        ConfigureAppSettings(modelBuilder);
        ConfigureComunicadoHeroImages(modelBuilder);
        ConfigurePayslips(modelBuilder);
        ConfigureEmployeeBenefits(modelBuilder);
        ConfigureBenefitCatalog(modelBuilder);
        ConfigureEmployeeLeave(modelBuilder);
        ConfigurePontoAdjustments(modelBuilder);
        ConfigureTotvsRmConfiguration(modelBuilder);
        ConfigureWorkerRuns(modelBuilder);
        ConfigureTimesheetPeriodCache(modelBuilder);
        ConfigureEmail(modelBuilder);
        ConfigurePortalUsers(modelBuilder);
        ConfigureUserTeamsTokens(modelBuilder);
        ConfigureOrgChartGovernance(modelBuilder);
        ConfigureCompass(modelBuilder);
        ConfigureUniLio(modelBuilder);
        ConfigurePhoneExtensions(modelBuilder);
        ConfigurePortalSystems(modelBuilder);
        ConfigureRbac(modelBuilder);
        ConfigureDbExplorer(modelBuilder);
    }

    private static void ApplySnakeCaseTableNames(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is not null)
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return string.Concat(name.Select((ch, i) =>
            i > 0 && char.IsUpper(ch) ? "_" + char.ToLowerInvariant(ch) : char.ToLowerInvariant(ch).ToString()));
    }

    private static void ConfigurePerson(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Person>();

        entity.HasIndex(p => p.Slug).IsUnique();
        entity.HasIndex(p => p.Email).IsUnique();
        entity.HasIndex(p => p.AzureAdObjectId);
        entity.HasIndex(p => p.DepartmentId);
        entity.HasIndex(p => p.ManagerId);
        entity.HasIndex(p => p.EmployeeId);

        entity.Property(p => p.EmployeeId).HasMaxLength(32);

        entity.HasOne(p => p.Department)
            .WithMany(d => d.Members)
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(p => p.Manager)
            .WithMany(p => p.DirectReports)
            .HasForeignKey(p => p.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureDepartment(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Department>();

        entity.HasIndex(d => d.Code);
        entity.HasIndex(d => d.ParentDepartmentId);

        entity.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureFeed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeedPost>(entity =>
        {
            entity.HasIndex(p => p.CreatedAt);
            entity.HasIndex(p => p.AuthorId);
            entity.HasIndex(p => p.Type);

            entity.HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasIndex(c => c.PostId);
            entity.HasIndex(c => c.AuthorId);

            entity.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PostMediaComment>(entity =>
        {
            entity.HasIndex(c => new { c.PostId, c.MediaUrl, c.CreatedAt });
            entity.HasIndex(c => c.AuthorId);
            entity.Property(c => c.MediaUrl).HasMaxLength(512).IsRequired();
            entity.Property(c => c.Text).IsRequired();

            entity.HasOne(c => c.Post)
                .WithMany()
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable("post_media_comments");
        });

        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasIndex(r => new { r.PostId, r.PersonId }).IsUnique();

            entity.HasOne(r => r.Post)
                .WithMany(p => p.Reactions)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Person)
                .WithMany()
                .HasForeignKey(r => r.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Poll>(entity =>
        {
            entity.HasIndex(p => p.PostId).IsUnique();

            entity.HasOne(p => p.Post)
                .WithMany()
                .HasForeignKey(p => p.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PollOption>(entity =>
        {
            entity.HasIndex(o => o.PollId);

            entity.HasOne(o => o.Poll)
                .WithMany(p => p.Options)
                .HasForeignKey(o => o.PollId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PollVote>(entity =>
        {
            entity.HasIndex(v => new { v.PollOptionId, v.PersonId }).IsUnique();
            entity.HasIndex(v => v.PersonId);

            entity.HasOne(v => v.PollOption)
                .WithMany(o => o.Votes)
                .HasForeignKey(v => v.PollOptionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.Person)
                .WithMany()
                .HasForeignKey(v => v.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Celebration>(entity =>
        {
            entity.HasIndex(c => c.PostId).IsUnique();

            entity.HasOne(c => c.Post)
                .WithMany()
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.CelebratedPerson)
                .WithMany()
                .HasForeignKey(c => c.CelebratedPersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureComunicado(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comunicado>(entity =>
        {
            entity.HasIndex(c => c.Kind);
            entity.HasIndex(c => c.PublishedAt);
            entity.HasIndex(c => c.ArchivedAt);
            entity.HasIndex(c => c.AuthorId);
            entity.HasIndex(c => c.Slug).IsUnique();
            entity.Property(c => c.Slug).HasMaxLength(120);

            entity.HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ComunicadoRead>(entity =>
        {
            entity.HasIndex(r => new { r.ComunicadoId, r.PersonId }).IsUnique();

            entity.HasOne(r => r.Comunicado)
                .WithMany(c => c.Reads)
                .HasForeignKey(r => r.ComunicadoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Person)
                .WithMany()
                .HasForeignKey(r => r.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureGroup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasIndex(g => g.OwnerId);
            entity.HasIndex(g => g.Status);
            entity.Property(g => g.Icon).HasMaxLength(64);

            entity.HasOne(g => g.Owner)
                .WithMany()
                .HasForeignKey(g => g.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.ReviewedBy)
                .WithMany()
                .HasForeignKey(g => g.ReviewedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasIndex(m => new { m.GroupId, m.PersonId }).IsUnique();

            entity.HasOne(m => m.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Person)
                .WithMany()
                .HasForeignKey(m => m.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupPost>(entity =>
        {
            entity.HasIndex(p => p.GroupId);

            entity.HasOne(p => p.Group)
                .WithMany(g => g.Posts)
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureDocuments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentMetadata>(entity =>
        {
            entity.HasIndex(d => d.Category);
            entity.HasIndex(d => d.SharePointItemId).IsUnique();
            entity.HasIndex(d => d.ModifiedAt);
            entity.HasIndex(d => d.SeedKey).IsUnique();
            entity.Property(d => d.Description).HasMaxLength(2048);
            entity.Property(d => d.MediaType).HasMaxLength(32);
            entity.Property(d => d.SeedKey).HasMaxLength(128);
        });
    }

    private static void ConfigureBookmarkCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookmarkCatalogItem>(entity =>
        {
            entity.Property(e => e.SeedKey).HasMaxLength(128);
            entity.Property(e => e.Kind).HasMaxLength(32);
            entity.Property(e => e.Title).HasMaxLength(512);
            entity.Property(e => e.Excerpt).HasMaxLength(1024);
            entity.Property(e => e.Href).HasMaxLength(2048);
            entity.Property(e => e.Icon).HasMaxLength(128);
            entity.Property(e => e.Source).HasMaxLength(256);
            entity.HasIndex(e => e.SeedKey).IsUnique();
            entity.HasIndex(e => e.Kind);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.SortOrder);
        });
    }

    private static void ConfigureServiceRequests(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.HasIndex(r => r.RequesterId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.Category);

            entity.HasOne(r => r.Requester)
                .WithMany()
                .HasForeignKey(r => r.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceRequestEvent>(entity =>
        {
            entity.HasIndex(e => e.ServiceRequestId);

            entity.HasOne(e => e.ServiceRequest)
                .WithMany(r => r.Events)
                .HasForeignKey(e => e.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Actor)
                .WithMany()
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => n.PersonId);
            entity.HasIndex(n => new { n.PersonId, n.IsRead });
            entity.HasIndex(n => n.CreatedAt);

            entity.HasOne(n => n.Person)
                .WithMany()
                .HasForeignKey(n => n.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureChat(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasIndex(c => c.CreatedById);

            entity.HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasIndex(m => m.ConversationId);
            entity.HasIndex(m => m.CreatedAt);
            entity.HasIndex(m => m.AuthorId);

            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Author)
                .WithMany()
                .HasForeignKey(m => m.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCalendar(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasIndex(e => e.StartAt);
            entity.HasIndex(e => e.ExternalId);
        });

        modelBuilder.Entity<CafeteriaMenu>(entity =>
        {
            entity.HasIndex(m => m.Date).IsUnique();
            entity.Property(m => m.PayloadJson).HasDefaultValue("{}");
            entity.Property(m => m.Published).HasDefaultValue(false);

            entity.HasOne(m => m.UpdatedBy)
                .WithMany()
                .HasForeignKey(m => m.UpdatedById)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureAnalytics(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalyticsEvent>(entity =>
        {
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.PersonId);

            entity.HasOne(e => e.Person)
                .WithMany()
                .HasForeignKey(e => e.PersonId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.Source);

            entity.HasOne(e => e.Actor)
                .WithMany()
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureObservability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ObservabilityEvent>(entity =>
        {
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => new { e.EventType, e.OccurredAt });
            entity.HasIndex(e => new { e.EventName, e.OccurredAt });
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.TraceId);
            entity.HasIndex(e => new { e.UserId, e.OccurredAt });
            entity.HasIndex(e => new { e.Severity, e.OccurredAt });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PageView>(entity =>
        {
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => new { e.RouteTemplate, e.OccurredAt });
            entity.HasIndex(e => new { e.UserId, e.OccurredAt });
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => new { e.Module, e.OccurredAt });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessEvent>(entity =>
        {
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => new { e.EventType, e.OccurredAt });
            entity.HasIndex(e => new { e.UserId, e.OccurredAt });
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => new { e.Result, e.OccurredAt });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureUserPreferences(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasIndex(p => p.PersonId).IsUnique();

            entity.HasOne(p => p.Person)
                .WithMany()
                .HasForeignKey(p => p.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureMoodChecks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MoodCheck>(entity =>
        {
            entity.HasIndex(m => new { m.PersonId, m.CheckDate }).IsUnique();
            entity.HasIndex(m => m.CheckDate);
            entity.HasIndex(m => m.Mood);

            entity.HasOne(m => m.Person)
                .WithMany()
                .HasForeignKey(m => m.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureAppSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasIndex(s => s.Key).IsUnique();
            entity.HasIndex(s => s.Category);

            entity.HasOne(s => s.UpdatedBy)
                .WithMany()
                .HasForeignKey(s => s.UpdatedById)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureComunicadoHeroImages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComunicadoHeroImage>(entity =>
        {
            entity.HasIndex(i => i.AssetId);
            entity.HasIndex(i => new { i.AssetId, i.Version }).IsUnique();
            entity.HasIndex(i => i.CreatedAt);
            entity.HasIndex(i => i.UploadedById);

            entity.HasOne(i => i.UploadedBy)
                .WithMany()
                .HasForeignKey(i => i.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePayslips(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payslip>(entity =>
        {
            entity.HasIndex(p => new { p.PersonId, p.Year, p.Month, p.PaymentType }).IsUnique();
            entity.HasIndex(p => p.PublishedAt);
            entity.Property(p => p.PaymentType).HasMaxLength(20);
            entity.Property(p => p.Source).HasMaxLength(50);

            entity.HasOne(p => p.Person)
                .WithMany()
                .HasForeignKey(p => p.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IncomeStatement>(entity =>
        {
            entity.HasIndex(i => new { i.PersonId, i.Year }).IsUnique();

            entity.HasOne(i => i.Person)
                .WithMany()
                .HasForeignKey(i => i.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureEmployeeBenefits(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeBenefit>(entity =>
        {
            entity.HasIndex(b => new { b.PersonId, b.BenefitKey }).IsUnique();
            entity.HasIndex(b => b.Category);

            entity.HasOne(b => b.Person)
                .WithMany()
                .HasForeignKey(b => b.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureBenefitCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BenefitCatalog>(entity =>
        {
            entity.ToTable("benefit_catalog");
            entity.HasIndex(b => b.CatalogKey).IsUnique();
            entity.HasIndex(b => b.Category);
            entity.HasIndex(b => b.IsActive);
        });
    }

    private static void ConfigureEmployeeLeave(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeLeaveBalance>(entity =>
        {
            entity.HasIndex(b => b.PersonId).IsUnique();

            entity.HasOne(b => b.Person)
                .WithMany()
                .HasForeignKey(b => b.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LeaveRecord>(entity =>
        {
            entity.HasIndex(r => new { r.PersonId, r.StartDate });
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => new { r.PersonId, r.RmExternalId });

            entity.HasOne(r => r.Person)
                .WithMany()
                .HasForeignKey(r => r.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePontoAdjustments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PontoAdjustmentRecord>(entity =>
        {
            entity.ToTable("ponto_adjustment_records");
            entity.Property(r => r.Title).HasMaxLength(256);
            entity.Property(r => r.Status).HasMaxLength(64);
            entity.Property(r => r.Reason).HasMaxLength(2000);
            entity.Property(r => r.DataSource).HasMaxLength(64);
            entity.HasIndex(r => new { r.PersonId, r.CreatedAt });
            entity.HasIndex(r => r.Status);

            entity.HasOne(r => r.Person)
                .WithMany()
                .HasForeignKey(r => r.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTotvsRmConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TotvsRmConfiguration>(entity =>
        {
            entity.Property(c => c.Server).HasMaxLength(256);
            entity.Property(c => c.Database).HasMaxLength(128);
            entity.Property(c => c.UserName).HasMaxLength(128);
        });
    }

    private static void ConfigureWorkerRuns(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkerRun>(entity =>
        {
            entity.HasIndex(r => r.WorkerKey);
            entity.HasIndex(r => r.StartedAtUtc);
            entity.Property(r => r.WorkerKey).HasMaxLength(64);
            entity.Property(r => r.Status).HasMaxLength(32);
            entity.Property(r => r.TriggerSource).HasMaxLength(32);
        });

        modelBuilder.Entity<WorkerRunLog>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.HasIndex(l => l.WorkerRunId);
            entity.Property(l => l.Level).HasMaxLength(16);

            entity.HasOne(l => l.WorkerRun)
                .WithMany(r => r.Logs)
                .HasForeignKey(l => l.WorkerRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureEmail(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailConfiguration>(entity =>
        {
            entity.Property(c => c.FromAddress).HasMaxLength(256);
            entity.Property(c => c.FromName).HasMaxLength(256);
            entity.Property(c => c.SmtpHost).HasMaxLength(256);
            entity.Property(c => c.SmtpUsername).HasMaxLength(256);

            entity.HasOne(c => c.UpdatedBy)
                .WithMany()
                .HasForeignKey(c => c.UpdatedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EmailMessage>(entity =>
        {
            entity.Property(m => m.Subject).HasMaxLength(500);
            entity.Property(m => m.TemplateKey).HasMaxLength(128);
            entity.Property(m => m.IdempotencyKey).HasMaxLength(128);
            entity.Property(m => m.ProviderMessageId).HasMaxLength(256);
            entity.Property(m => m.Status).HasConversion<string>().HasMaxLength(32);

            entity.HasIndex(m => new { m.Status, m.NextRetryAt, m.ScheduledAt });
            entity.HasIndex(m => m.CreatedAt);
            entity.HasIndex(m => m.CorrelationId);
            entity.HasIndex(m => m.IdempotencyKey)
                .IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL");

            entity.HasOne(m => m.CreatedBy)
                .WithMany()
                .HasForeignKey(m => m.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EmailAttachmentStaging>(entity =>
        {
            entity.ToTable("email_attachment_staging");
            entity.Property(a => a.FileName).HasMaxLength(256);
            entity.Property(a => a.ContentType).HasMaxLength(128);
            entity.Property(a => a.StoragePath).HasMaxLength(512);
            entity.HasIndex(a => new { a.CreatedById, a.IsConsumed, a.ExpiresAt });

            entity.HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePortalUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PortalUser>(entity =>
        {
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.PasswordHash).HasMaxLength(512);
            entity.Property(u => u.RolesJson).HasMaxLength(512);
            entity.Property(u => u.SecurityStamp).HasMaxLength(64);

            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Person)
                .WithMany()
                .HasForeignKey(u => u.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureUserTeamsTokens(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserTeamsToken>(entity =>
        {
            entity.HasIndex(t => t.PersonId).IsUnique();
            entity.HasIndex(t => t.ExpiresAt);

            entity.Property(t => t.EncryptedAccessToken).HasMaxLength(8192);
            entity.Property(t => t.EncryptedRefreshToken).HasMaxLength(8192);
            entity.Property(t => t.ScopesJson).HasMaxLength(2048);

            entity.HasOne(t => t.Person)
                .WithMany()
                .HasForeignKey(t => t.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureOrgChartGovernance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrgChartSettings>(entity =>
        {
            entity.Property(s => s.EditAllowedRolesJson).HasMaxLength(1024);
            entity.Property(s => s.EditAllowedEmailsJson).HasMaxLength(4096);
            entity.Property(s => s.ViewFullAllowedRolesJson).HasMaxLength(1024);

            entity.HasOne(s => s.UpdatedBy)
                .WithMany()
                .HasForeignKey(s => s.UpdatedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrgDepartment>(entity =>
        {
            entity.Property(d => d.Name).HasMaxLength(256);
            entity.HasIndex(d => d.Name);
            entity.HasIndex(d => d.ParentDepartmentId);

            entity.HasOne(d => d.ParentDepartment)
                .WithMany(d => d.ChildDepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrgDepartmentMapping>(entity =>
        {
            entity.Property(m => m.SourceName).HasMaxLength(256);
            entity.HasIndex(m => m.SourceName).IsUnique();
            entity.HasIndex(m => m.OrgDepartmentId);

            entity.HasOne(m => m.OrgDepartment)
                .WithMany()
                .HasForeignKey(m => m.OrgDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrgPosition>(entity =>
        {
            entity.Property(p => p.Title).HasMaxLength(256);
            entity.Property(p => p.DepartmentName).HasMaxLength(256);
            entity.Property(p => p.Source).HasConversion<int>();

            entity.HasIndex(p => p.PersonId).IsUnique();
            entity.HasIndex(p => p.ManagerPositionId);
            entity.HasIndex(p => p.OrgDepartmentId);
            entity.HasIndex(p => new { p.IsVisible, p.SortOrder });

            entity.HasOne(p => p.Person)
                .WithMany()
                .HasForeignKey(p => p.PersonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.OrgDepartment)
                .WithMany()
                .HasForeignKey(p => p.OrgDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.ManagerPosition)
                .WithMany(p => p.DirectReports)
                .HasForeignKey(p => p.ManagerPositionId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureTimesheetPeriodCache(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimesheetPeriodCache>(entity =>
        {
            entity.HasIndex(c => new { c.PersonId, c.Year, c.Month }).IsUnique();
            entity.Property(c => c.Source).HasMaxLength(32);

            entity.HasOne(c => c.Person)
                .WithMany()
                .HasForeignKey(c => c.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCompass(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompassIbpSnapshot>(entity =>
        {
            entity.HasIndex(s => s.IsActive);
            entity.HasIndex(s => s.ImportedAt);
            entity.Property(s => s.Label).HasMaxLength(256);
            entity.Property(s => s.VersionAtual).HasMaxLength(64);
            entity.Property(s => s.VersionAnterior).HasMaxLength(64);
            entity.Property(s => s.SourceSystem).HasMaxLength(64);
        });

        modelBuilder.Entity<CompassIbpRow>(entity =>
        {
            entity.HasIndex(r => r.SnapshotId);
            entity.HasIndex(r => new { r.SnapshotId, r.Diretoria });
            entity.HasIndex(r => new { r.SnapshotId, r.Unidade });
            entity.HasIndex(r => new { r.SnapshotId, r.FamiliaComercial });
            entity.HasIndex(r => new { r.SnapshotId, r.Tipo });
            entity.Property(r => r.Tipo).HasMaxLength(128);
            entity.Property(r => r.FamiliaComercial).HasMaxLength(256);
            entity.Property(r => r.SkuCode).HasMaxLength(64);
            entity.Property(r => r.SkuDescription).HasMaxLength(512);
            entity.Property(r => r.ClienteHyperion).HasMaxLength(256);
            entity.Property(r => r.Cliente).HasMaxLength(256);
            entity.Property(r => r.Matriz).HasMaxLength(128);
            entity.Property(r => r.Diretoria).HasMaxLength(128);
            entity.Property(r => r.Unidade).HasMaxLength(128);

            entity.HasOne(r => r.Snapshot)
                .WithMany(s => s.Rows)
                .HasForeignKey(r => r.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureUniLio(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UniLioCourse>(entity =>
        {
            entity.HasIndex(c => c.SeedKey).IsUnique();
            entity.HasIndex(c => c.Area);
            entity.HasIndex(c => c.Department);
            entity.HasIndex(c => c.ContentType);
            entity.HasIndex(c => c.IsMandatory);
            entity.Property(c => c.SeedKey).HasMaxLength(128);
            entity.Property(c => c.Title).HasMaxLength(512);
            entity.Property(c => c.ContentType).HasMaxLength(32);
            entity.Property(c => c.Area).HasMaxLength(128);
            entity.Property(c => c.Department).HasMaxLength(128);
            entity.Property(c => c.InstructorName).HasMaxLength(256);
            entity.Property(c => c.Status).HasMaxLength(32);
            entity.Property(c => c.Provider).HasMaxLength(256);
            entity.Property(c => c.RejectionReason).HasMaxLength(2000);
            entity.HasOne(c => c.InstructorPerson)
                .WithMany()
                .HasForeignKey(c => c.InstructorPersonId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UniLioCourseModule>(entity =>
        {
            entity.HasIndex(m => m.CourseId);
            entity.HasIndex(m => new { m.CourseId, m.SortOrder });
            entity.Property(m => m.Title).HasMaxLength(512);
            entity.Property(m => m.ContentType).HasMaxLength(32);
            entity.HasOne(m => m.Course)
                .WithMany(c => c.Modules)
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioModuleAttachment>(entity =>
        {
            entity.ToTable("uni_lio_module_attachments");
            entity.HasIndex(a => a.ModuleId);
            entity.HasIndex(a => new { a.ModuleId, a.SortOrder });
            entity.Property(a => a.FileName).HasMaxLength(512);
            entity.Property(a => a.StorageFileName).HasMaxLength(128);
            entity.Property(a => a.ContentType).HasMaxLength(128);
            entity.HasOne(a => a.Module)
                .WithMany(m => m.Attachments)
                .HasForeignKey(a => a.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioLearningPath>(entity =>
        {
            entity.HasIndex(p => p.SeedKey).IsUnique();
            entity.Property(p => p.SeedKey).HasMaxLength(128);
            entity.Property(p => p.Title).HasMaxLength(512);
        });

        modelBuilder.Entity<UniLioPathCourse>(entity =>
        {
            entity.HasIndex(pc => pc.PathId);
            entity.HasIndex(pc => new { pc.PathId, pc.CourseId }).IsUnique();
            entity.HasOne(pc => pc.Path)
                .WithMany(p => p.PathCourses)
                .HasForeignKey(pc => pc.PathId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(pc => pc.Course)
                .WithMany()
                .HasForeignKey(pc => pc.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioSkill>(entity =>
        {
            entity.HasIndex(s => s.SeedKey).IsUnique();
            entity.Property(s => s.SeedKey).HasMaxLength(128);
            entity.Property(s => s.Name).HasMaxLength(256);
            entity.Property(s => s.Category).HasMaxLength(128);
        });

        modelBuilder.Entity<UniLioCourseSkill>(entity =>
        {
            entity.HasIndex(cs => new { cs.CourseId, cs.SkillId }).IsUnique();
            entity.HasOne(cs => cs.Course)
                .WithMany(c => c.CourseSkills)
                .HasForeignKey(cs => cs.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(cs => cs.Skill)
                .WithMany(s => s.CourseSkills)
                .HasForeignKey(cs => cs.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioEnrollment>(entity =>
        {
            entity.HasIndex(e => new { e.PersonId, e.CourseId }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Status).HasMaxLength(32);
            entity.Property(e => e.CourseFeedbackComment).HasMaxLength(2000);
            entity.HasOne(e => e.Person)
                .WithMany()
                .HasForeignKey(e => e.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioModuleProgress>(entity =>
        {
            entity.HasIndex(mp => new { mp.EnrollmentId, mp.ModuleId }).IsUnique();
            entity.HasOne(mp => mp.Enrollment)
                .WithMany(e => e.ModuleProgress)
                .HasForeignKey(mp => mp.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(mp => mp.Module)
                .WithMany(m => m.ModuleProgress)
                .HasForeignKey(mp => mp.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioAssessment>(entity =>
        {
            entity.HasIndex(a => a.CourseId);
            entity.Property(a => a.Title).HasMaxLength(512);
            entity.HasOne(a => a.Course)
                .WithMany(c => c.Assessments)
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioAssessmentAttempt>(entity =>
        {
            entity.HasIndex(a => new { a.PersonId, a.AssessmentId });
            entity.HasOne(a => a.Person)
                .WithMany()
                .HasForeignKey(a => a.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.Assessment)
                .WithMany(a => a.Attempts)
                .HasForeignKey(a => a.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioCertificate>(entity =>
        {
            entity.HasIndex(c => c.CertificateCode).IsUnique();
            entity.HasIndex(c => new { c.PersonId, c.CourseId });
            entity.Property(c => c.CertificateCode).HasMaxLength(64);
            entity.HasOne(c => c.Person)
                .WithMany()
                .HasForeignKey(c => c.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.Course)
                .WithMany()
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioEvent>(entity =>
        {
            entity.HasIndex(e => e.StartsAt);
            entity.Property(e => e.Title).HasMaxLength(512);
            entity.Property(e => e.EventType).HasMaxLength(64);
            entity.HasOne(e => e.Instructor)
                .WithMany()
                .HasForeignKey(e => e.InstructorPersonId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UniLioEventRegistration>(entity =>
        {
            entity.HasIndex(r => new { r.EventId, r.PersonId }).IsUnique();
            entity.HasOne(r => r.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Person)
                .WithMany()
                .HasForeignKey(r => r.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioCommunityPost>(entity =>
        {
            entity.HasIndex(p => p.AuthorPersonId);
            entity.HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorPersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.Course)
                .WithMany()
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UniLioModuleQuestion>(entity =>
        {
            entity.HasIndex(q => new { q.CourseId, q.ModuleId });
            entity.HasIndex(q => q.AuthorPersonId);
            entity.HasIndex(q => new { q.CourseId, q.Visibility, q.Status });
            entity.Property(q => q.Body).HasMaxLength(2000);
            entity.Property(q => q.Visibility).HasMaxLength(16);
            entity.Property(q => q.Status).HasMaxLength(16);
            entity.HasOne(q => q.Course)
                .WithMany()
                .HasForeignKey(q => q.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(q => q.Module)
                .WithMany()
                .HasForeignKey(q => q.ModuleId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(q => q.Author)
                .WithMany()
                .HasForeignKey(q => q.AuthorPersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioModuleQuestionReply>(entity =>
        {
            entity.HasIndex(r => r.QuestionId);
            entity.Property(r => r.Body).HasMaxLength(2000);
            entity.HasOne(r => r.Question)
                .WithMany(q => q.Replies)
                .HasForeignKey(r => r.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Author)
                .WithMany()
                .HasForeignKey(r => r.AuthorPersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioPersonSkill>(entity =>
        {
            entity.HasIndex(ps => new { ps.PersonId, ps.SkillId }).IsUnique();
            entity.HasOne(ps => ps.Person)
                .WithMany()
                .HasForeignKey(ps => ps.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ps => ps.Skill)
                .WithMany(s => s.PersonSkills)
                .HasForeignKey(ps => ps.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UniLioIntegrationLink>(entity =>
        {
            entity.HasIndex(l => new { l.SourceType, l.SourceKey, l.CourseId });
            entity.Property(l => l.SourceType).HasMaxLength(64);
            entity.Property(l => l.SourceKey).HasMaxLength(128);
            entity.HasOne(l => l.Course)
                .WithMany(c => c.IntegrationLinks)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePhoneExtensions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PhoneExtension>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Extension).HasMaxLength(32);
            entity.Property(e => e.Mobile).HasMaxLength(64);
            entity.Property(e => e.Department).HasMaxLength(256);
            entity.Property(e => e.Title).HasMaxLength(256);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.ManagerName).HasMaxLength(256);
            entity.HasIndex(e => e.Extension);
            entity.HasIndex(e => e.Department);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.LegacySourceId);
            entity.HasIndex(e => e.PersonId);
            entity.HasOne(e => e.Person).WithMany().HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigurePortalSystems(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PortalSystem>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Slug).HasMaxLength(128);
            entity.Property(e => e.Description).HasMaxLength(1024);
            entity.Property(e => e.Category).HasMaxLength(128);
            entity.Property(e => e.UrlDev).HasMaxLength(2048);
            entity.Property(e => e.UrlHml).HasMaxLength(2048);
            entity.Property(e => e.UrlPrd).HasMaxLength(2048);
            entity.Property(e => e.IconFaClass).HasMaxLength(128);
            entity.Property(e => e.IconAssetUrl).HasMaxLength(512);
            entity.Property(e => e.AccessNotes).HasMaxLength(1024);
            entity.Property(e => e.SeedKey).HasMaxLength(64);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.SeedKey);
        });
    }

    private static void ConfigureRbac(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(p => p.Key);
            entity.Property(p => p.Key).HasMaxLength(128);
            entity.Property(p => p.Module).HasMaxLength(64);
            entity.Property(p => p.Resource).HasMaxLength(64);
            entity.Property(p => p.Action).HasMaxLength(64);
            entity.Property(p => p.Label).HasMaxLength(256);
            entity.Property(p => p.Description).HasMaxLength(1024);
            entity.Property(p => p.AllowedDataScopesJson).HasMaxLength(256);
            entity.Property(p => p.MenuPath).HasMaxLength(256);
            entity.HasIndex(p => p.Module);
            entity.HasIndex(p => p.BusinessArea);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(r => r.Name).HasMaxLength(128);
            entity.Property(r => r.Slug).HasMaxLength(128);
            entity.Property(r => r.Description).HasMaxLength(1024);
            entity.HasIndex(r => r.Slug).IsUnique();
            entity.HasIndex(r => r.IsSystem);
            entity.HasIndex(r => r.IsActive);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionKey });
            entity.Property(rp => rp.PermissionKey).HasMaxLength(128);
            entity.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionKey)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubjectRoleAssignment>(entity =>
        {
            entity.HasIndex(a => new { a.SubjectType, a.SubjectId });
            entity.HasIndex(a => a.RoleId);
            entity.HasIndex(a => new { a.SubjectType, a.SubjectId, a.RoleId }).IsUnique();
            entity.HasOne(a => a.Role)
                .WithMany(r => r.SubjectAssignments)
                .HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestUser>(entity =>
        {
            entity.Property(t => t.Email).HasMaxLength(256);
            entity.Property(t => t.PasswordHash).HasMaxLength(512);
            entity.Property(t => t.DisplayName).HasMaxLength(256);
            entity.Property(t => t.Notes).HasMaxLength(2048);
            entity.Property(t => t.SecurityStamp).HasMaxLength(64);
            entity.HasIndex(t => t.Email).IsUnique();
            entity.HasIndex(t => t.IsActive);
            entity.HasOne(t => t.OptionalPerson)
                .WithMany()
                .HasForeignKey(t => t.OptionalPersonId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureDbExplorer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbExplorerQueryLog>(entity =>
        {
            entity.HasIndex(x => x.ActorId);
            entity.HasIndex(x => x.ExecutedAt);
            entity.Property(x => x.ConnectionId).HasMaxLength(32);
            entity.HasOne(x => x.Actor)
                .WithMany()
                .HasForeignKey(x => x.ActorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DbExplorerSavedQuery>(entity =>
        {
            entity.HasIndex(x => new { x.ActorId, x.Name }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.ConnectionId).HasMaxLength(32);
            entity.HasOne(x => x.Actor)
                .WithMany()
                .HasForeignKey(x => x.ActorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DbExplorerDerLayout>(entity =>
        {
            entity.HasIndex(x => new { x.ActorId, x.ConnectionId }).IsUnique();
            entity.Property(x => x.ConnectionId).HasMaxLength(32);
            entity.Property(x => x.LayoutJson).HasColumnType("jsonb");
            entity.HasOne(x => x.Actor)
                .WithMany()
                .HasForeignKey(x => x.ActorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}