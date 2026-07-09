using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services.UniLio;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed partial class UniLioService(
    AppDbContext db,
    IAppSettingsProvider settingsProvider,
    ICurrentUserService currentUserService,
    IPermissionService permissionService,
    UniLioCompletionService completionService,
    INotificationService notificationService) : IUniLioService
{
    private const int SkillGapTargetLevel = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<UniLioBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var enabled = settingsProvider.GetBool(AppSettingKeys.UniLioEnabled, true);
        var rolesJson = settingsProvider.GetString(
            AppSettingKeys.UniLioAllowedRoles,
            "[\"Employee\",\"Manager\",\"HR\",\"Admin\"]");
        var emailsJson = settingsProvider.GetString(AppSettingKeys.UniLioAllowedEmails, "[]");
        var canAccess = enabled
            && await UniLioAuthorization.CanAccessAsync(permissionService, cancellationToken);

        return new UniLioBootstrapDto(
            enabled,
            canAccess,
            DeserializeRoles(rolesJson),
            DeserializeEmails(emailsJson));
    }

    public async Task<UniLioMetaDto> GetMetaAsync(CancellationToken cancellationToken = default)
    {
        await UniLioAuthorization.EnsureCanAccessAsync(permissionService, cancellationToken);

        var areas = await db.UniLioCourses.AsNoTracking()
            .Select(c => c.Area)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(cancellationToken);

        var departments = await db.UniLioCourses.AsNoTracking()
            .Select(c => c.Department)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(cancellationToken);

        var contentTypes = await db.UniLioCourses.AsNoTracking()
            .Select(c => c.ContentType)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(cancellationToken);

        var skills = await db.UniLioSkills.AsNoTracking()
            .Select(s => s.Name)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(cancellationToken);

        var persona = await UniLioAuthorization.ResolvePersonaAsync(permissionService, cancellationToken);

        return new UniLioMetaDto(persona, areas, departments, contentTypes, skills);
    }

    public async Task<UniLioDashboardDto> GetDashboardAsync(
        UniLioQuery query,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        var enrollments = await db.UniLioEnrollments.AsNoTracking()
            .Where(e => e.PersonId == viewer.PersonId)
            .Include(e => e.Course)
            .ToListAsync(cancellationToken);

        var enrolledCount = enrollments.Count;
        var completedCount = enrollments.Count(e => e.Status == "completed");
        var mandatoryPending = enrollments.Count(e =>
            e.Course.IsMandatory && e.Status != "completed");
        var avgProgress = enrolledCount == 0
            ? 0
            : (int)Math.Round(enrollments.Average(e => e.ProgressPct));

        var kpis = new List<UniLioKpiDto>
        {
            new("enrolled", "Cursos matriculados", enrolledCount.ToString(CultureInfo.InvariantCulture), "", "neutral", "fa-book-open"),
            new("completed", "Concluídos", completedCount.ToString(CultureInfo.InvariantCulture), "", "up", "fa-circle-check"),
            new("mandatory_pending", "Obrigatórios pendentes", mandatoryPending.ToString(CultureInfo.InvariantCulture), "", mandatoryPending > 0 ? "down" : "neutral", "fa-shield-halved"),
            new("avg_progress", "Progresso médio", $"{avgProgress}%", "", avgProgress >= 50 ? "up" : "neutral", "fa-chart-line"),
        };

        var activePath = await GetActivePathSummaryAsync(viewer.PersonId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var alerts = enrollments
            .Where(e => e.Course.IsMandatory
                && e.Status != "completed"
                && e.DueDate.HasValue
                && e.DueDate.Value < now)
            .OrderBy(e => e.DueDate)
            .Take(5)
            .Select(e => new UniLioAlertDto(
                $"compliance-{e.CourseId}",
                "warning",
                $"Compliance vencido — {e.Course.Title}",
                $"Prazo era {e.DueDate!.Value:dd/MM/yyyy}. Progresso atual: {e.ProgressPct}%.",
                $"/unilio/curso/{e.CourseId}"))
            .ToList();

        var nextStepCourseIds = enrollments
            .Where(e => e.Status == "in_progress")
            .OrderByDescending(e => e.UpdatedAt)
            .Take(5)
            .Select(e => e.CourseId)
            .ToList();

        var visibleIds = await GetVisibleCourseIdsAsync(viewer, cancellationToken);
        var nextSteps = await MapCourseSummariesAsync(
            viewer,
            await GetCoursesQuery(visibleIds)
                .Where(c => nextStepCourseIds.Contains(c.Id))
                .ToListAsync(cancellationToken),
            cancellationToken);

        nextSteps = nextStepCourseIds
            .Select(id => nextSteps.FirstOrDefault(c => c.Id == id))
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        var recommendations = (await BuildRecommendationsAsync(viewer, cancellationToken))
            .Take(3)
            .ToList();

        return new UniLioDashboardDto(kpis, activePath, alerts, nextSteps, recommendations);
    }

    public async Task<UniLioCatalogPageDto> GetCatalogAsync(
        UniLioQuery query,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var visibleIds = await GetVisibleCourseIdsAsync(viewer, cancellationToken);
        var filtered = ApplyCourseFilters(
            GetCoursesQuery(visibleIds),
            query,
            viewer.PersonId);
        var totalCount = await filtered.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var courses = await filtered
            .OrderByDescending(c =>
                c.IsMandatory
                && !c.Enrollments.Any(e =>
                    e.PersonId == viewer.PersonId && e.Status == "completed"))
            .ThenByDescending(c => c.PublishedAt ?? c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = await MapCourseSummariesAsync(viewer, courses, cancellationToken);

        return new UniLioCatalogPageDto(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<UniLioCourseDetailDto> GetCourseDetailAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);

        var visibleIds = await GetVisibleCourseIdsAsync(viewer, cancellationToken);
        var course = await GetCoursesQuery(visibleIds)
            .Include(c => c.Modules).ThenInclude(m => m.Attachments)
            .Include(c => c.CourseSkills).ThenInclude(cs => cs.Skill)
            .Include(c => c.IntegrationLinks)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Curso {courseId} não encontrado.");

        var enrollment = await db.UniLioEnrollments.AsNoTracking()
            .Include(e => e.ModuleProgress)
            .FirstOrDefaultAsync(e => e.PersonId == viewer.PersonId && e.CourseId == courseId, cancellationToken);

        var completedModuleIds = enrollment?.ModuleProgress
            .Select(mp => mp.ModuleId)
            .ToHashSet() ?? [];

        var modules = course.Modules
            .OrderBy(m => m.SortOrder)
            .Select(m => new UniLioModuleDto(
                m.Id,
                m.SortOrder,
                m.Title,
                m.ContentType,
                m.ContentUrl,
                m.DurationMinutes,
                m.ArticleHtml,
                m.QuizJson,
                ParseQuizPassingScore(m.QuizJson),
                completedModuleIds.Contains(m.Id),
                UniLioModuleAttachmentMapper.Map(m.Attachments)))
            .ToList();

        var progressPct = enrollment?.ProgressPct ?? 0;
        var enrollmentStatus = enrollment?.Status ?? "not_enrolled";
        var stats = await GetCourseEnrollmentStatsAsync([courseId], cancellationToken);
        stats.TryGetValue(courseId, out var courseStats);

        return new UniLioCourseDetailDto(
            course.Id,
            course.SeedKey,
            course.Title,
            course.Description,
            course.ContentType,
            course.DurationMinutes,
            course.IsMandatory,
            course.Area,
            course.Department,
            course.Rating,
            course.InstructorName,
            course.ThumbnailUrl,
            course.ExternalUrl,
            course.Provider,
            course.Status,
            progressPct,
            enrollmentStatus,
            courseStats.Enrolled,
            courseStats.Completed,
            modules,
            course.CourseSkills.Select(cs => cs.Skill.Name).OrderBy(n => n).ToList(),
            MapIntegrationLinks(course.IntegrationLinks));
    }

    public async Task<UniLioCourseStartDto> StartCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);

        var visibleIds = await GetVisibleCourseIdsAsync(viewer, cancellationToken);
        if (!visibleIds.Contains(courseId))
        {
            throw new KeyNotFoundException($"Curso {courseId} não encontrado.");
        }

        var enrollment = await db.UniLioEnrollments
            .FirstOrDefaultAsync(e => e.PersonId == viewer.PersonId && e.CourseId == courseId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (enrollment is null)
        {
            enrollment = new UniLioEnrollment
            {
                Id = Guid.NewGuid(),
                PersonId = viewer.PersonId,
                CourseId = courseId,
                Status = "in_progress",
                ProgressPct = 0,
                StartedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.UniLioEnrollments.Add(enrollment);
        }
        else
        {
            enrollment.StartedAt ??= now;
            if (enrollment.Status is "not_enrolled" or "not_started")
            {
                enrollment.Status = "in_progress";
            }

            enrollment.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new UniLioCourseStartDto(
            courseId,
            enrollment.Status,
            enrollment.StartedAt ?? now);
    }

    public async Task<UniLioCourseEnrollmentsDto> GetCourseEnrollmentsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);

        var visibleIds = await GetVisibleCourseIdsAsync(viewer, cancellationToken);
        if (!visibleIds.Contains(courseId))
        {
            throw new KeyNotFoundException($"Curso {courseId} não encontrado.");
        }

        if (!await CanViewCourseEnrollmentsAsync(cancellationToken))
        {
            throw new UnauthorizedAccessException("Sem permissão para consultar matrículas deste curso.");
        }

        var stats = await GetCourseEnrollmentStatsAsync([courseId], cancellationToken);
        stats.TryGetValue(courseId, out var courseStats);

        var items = await db.UniLioEnrollments.AsNoTracking()
            .Include(e => e.Person)
            .Where(e => e.CourseId == courseId)
            .OrderByDescending(e => e.StartedAt ?? e.CreatedAt)
            .Select(e => new UniLioCourseEnrollmentRecordDto(
                e.PersonId,
                e.Person.Name,
                e.Status,
                e.StartedAt,
                e.CompletedAt))
            .ToListAsync(cancellationToken);

        return new UniLioCourseEnrollmentsDto(
            courseId,
            courseStats.Enrolled,
            courseStats.Completed,
            items);
    }

    public async Task<UniLioPathsDto> GetPathsAsync(CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);

        var paths = await db.UniLioLearningPaths.AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.PathCourses)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);

        var items = new List<UniLioPathSummaryDto>();
        foreach (var path in paths)
        {
            items.Add(await MapPathSummaryAsync(path, viewer.PersonId, cancellationToken));
        }

        return new UniLioPathsDto(items);
    }

    public async Task<UniLioPathDetailDto> GetPathDetailAsync(
        Guid pathId,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);

        var path = await db.UniLioLearningPaths.AsNoTracking()
            .Include(p => p.PathCourses).ThenInclude(pc => pc.Course)
            .FirstOrDefaultAsync(p => p.Id == pathId, cancellationToken)
            ?? throw new KeyNotFoundException($"Trilha {pathId} não encontrada.");

        var summary = await MapPathSummaryAsync(path, viewer.PersonId, cancellationToken);

        var courseIds = path.PathCourses
            .OrderBy(pc => pc.SortOrder)
            .Select(pc => pc.CourseId)
            .ToList();

        var courses = path.PathCourses
            .OrderBy(pc => pc.SortOrder)
            .Select(pc => pc.Course)
            .ToList();

        var mappedCourses = await MapCourseSummariesAsync(viewer, courses, cancellationToken);
        mappedCourses = courseIds
            .Select(id => mappedCourses.FirstOrDefault(c => c.Id == id))
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        return new UniLioPathDetailDto(
            path.Id,
            path.SeedKey,
            path.Title,
            path.Description,
            summary.ProgressPct,
            mappedCourses);
    }

    public async Task<UniLioProgressDto> CompleteModuleAsync(
        Guid courseId,
        Guid moduleId,
        UniLioCompleteModuleRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new UniLioCompleteModuleRequest();

        if (request.ContentRating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "A avaliação do curso deve ser entre 1 e 5 estrelas.");
        }

        var feedbackComment = string.IsNullOrWhiteSpace(request.FeedbackComment)
            ? null
            : request.FeedbackComment.Trim();

        var viewer = await GetViewerAsync(cancellationToken);

        var course = await db.UniLioCourses
            .Include(c => c.Modules)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Curso {courseId} não encontrado.");

        var module = course.Modules.FirstOrDefault(m => m.Id == moduleId)
            ?? throw new KeyNotFoundException($"Módulo {moduleId} não encontrado no curso {courseId}.");

        var enrollment = await db.UniLioEnrollments
            .Include(e => e.ModuleProgress)
            .FirstOrDefaultAsync(e => e.PersonId == viewer.PersonId && e.CourseId == courseId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (enrollment is null)
        {
            enrollment = new UniLioEnrollment
            {
                Id = Guid.NewGuid(),
                PersonId = viewer.PersonId,
                CourseId = courseId,
                Status = "in_progress",
                ProgressPct = 0,
                StartedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.UniLioEnrollments.Add(enrollment);
        }

        var completedModuleIds = enrollment.ModuleProgress
            .Select(mp => mp.ModuleId)
            .ToHashSet();

        if (!completedModuleIds.Contains(moduleId))
        {
            db.UniLioModuleProgress.Add(new UniLioModuleProgress
            {
                Id = Guid.NewGuid(),
                EnrollmentId = enrollment.Id,
                ModuleId = moduleId,
                CompletedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
            completedModuleIds.Add(moduleId);
        }

        var totalModules = course.Modules.Count;
        var completedModules = completedModuleIds.Count;
        var progressPct = totalModules == 0
            ? 100
            : (int)Math.Round(completedModules * 100.0 / totalModules);

        enrollment.ProgressPct = progressPct;
        enrollment.UpdatedAt = now;

        var courseCompleted = totalModules > 0 && completedModules >= totalModules;
        if (courseCompleted)
        {
            if (request.ContentRating is not (>= 1 and <= 5))
            {
                throw new ArgumentException(
                    "A avaliação do curso é obrigatória ao concluir todos os módulos.");
            }

            enrollment.CourseContentRating = request.ContentRating;
            enrollment.CourseFeedbackComment = feedbackComment;
            enrollment.Status = "completed";
            enrollment.CompletedAt = now;
        }
        else if (enrollment.Status != "completed")
        {
            enrollment.Status = "in_progress";
            enrollment.StartedAt ??= now;
        }

        await db.SaveChangesAsync(cancellationToken);

        var certificateIssued = false;
        if (courseCompleted)
        {
            certificateIssued = await TryIssueCertificateAsync(viewer.PersonId, courseId, cancellationToken);
            await completionService.TryNotifyCourseCompletedAsync(viewer.PersonId, courseId, cancellationToken);
        }

        return new UniLioProgressDto(
            courseId,
            progressPct,
            enrollment.Status,
            courseCompleted);
    }

    public async Task<UniLioAssessmentsDto> GetAssessmentsAsync(CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);

        var enrollments = await db.UniLioEnrollments.AsNoTracking()
            .Where(e => e.PersonId == viewer.PersonId)
            .Select(e => e.CourseId)
            .ToListAsync(cancellationToken);

        var assessments = await db.UniLioAssessments.AsNoTracking()
            .Include(a => a.Course)
            .Include(a => a.Attempts.Where(at => at.PersonId == viewer.PersonId))
            .Where(a => enrollments.Contains(a.CourseId))
            .OrderBy(a => a.Course.Title)
            .ToListAsync(cancellationToken);

        var pending = new List<UniLioAssessmentSummaryDto>();
        var history = new List<UniLioAssessmentSummaryDto>();

        foreach (var assessment in assessments)
        {
            var attempts = assessment.Attempts
                .OrderByDescending(a => a.AttemptedAt)
                .ToList();
            var lastAttempt = attempts.FirstOrDefault();
            var hasPassed = attempts.Any(a => a.Passed);

            var summary = new UniLioAssessmentSummaryDto(
                assessment.Id,
                assessment.CourseId,
                assessment.Course.Title,
                assessment.Title,
                assessment.PassingScore,
                lastAttempt?.Score,
                lastAttempt?.Passed,
                lastAttempt?.AttemptedAt,
                hasPassed ? "passed" : lastAttempt is null ? "pending" : "failed");

            if (hasPassed)
            {
                history.Add(summary);
            }
            else if (lastAttempt is null)
            {
                pending.Add(summary);
            }
            else
            {
                history.Add(summary);
                if (!lastAttempt.Passed)
                {
                    pending.Add(summary with { Status = "retry" });
                }
            }
        }

        return new UniLioAssessmentsDto(pending, history);
    }

    public async Task<UniLioAssessmentResultDto> SubmitAssessmentAsync(
        Guid assessmentId,
        UniLioAssessmentSubmitRequest request,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);

        var assessment = await db.UniLioAssessments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == assessmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Avaliação {assessmentId} não encontrada.");

        var score = CalculateAssessmentScore(assessment.QuestionsJson, request.Answers);
        var passed = score >= assessment.PassingScore;
        var now = DateTimeOffset.UtcNow;

        var attempt = new UniLioAssessmentAttempt
        {
            Id = Guid.NewGuid(),
            PersonId = viewer.PersonId,
            AssessmentId = assessmentId,
            Score = score,
            Passed = passed,
            AnswersJson = JsonSerializer.Serialize(request.Answers, JsonOptions),
            AttemptedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.UniLioAssessmentAttempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);

        string? certificateCode = null;
        var certificateIssued = false;
        if (passed)
        {
            certificateIssued = await TryIssueCertificateAsync(viewer.PersonId, assessment.CourseId, cancellationToken);
            if (certificateIssued)
            {
                certificateCode = await db.UniLioCertificates.AsNoTracking()
                    .Where(c => c.PersonId == viewer.PersonId && c.CourseId == assessment.CourseId)
                    .Select(c => c.CertificateCode)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            await completionService.TryNotifyCourseCompletedAsync(
                viewer.PersonId,
                assessment.CourseId,
                cancellationToken);
        }

        return new UniLioAssessmentResultDto(
            attempt.Id,
            score,
            passed,
            certificateIssued,
            certificateCode);
    }

    public async Task<UniLioCertificatesDto> GetCertificatesAsync(CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);

        var items = await db.UniLioCertificates.AsNoTracking()
            .Include(c => c.Course)
            .Where(c => c.PersonId == viewer.PersonId)
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new UniLioCertificateDto(
                c.Id,
                c.CourseId,
                c.Course.Title,
                c.CertificateCode,
                c.IssuedAt,
                c.Course.Area))
            .ToListAsync(cancellationToken);

        return new UniLioCertificatesDto(items);
    }

    public async Task<UniLioComplianceDto> GetComplianceAsync(CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var items = await db.UniLioEnrollments.AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.PersonId == viewer.PersonId && e.Course.IsMandatory)
            .OrderBy(e => e.DueDate)
            .Select(e => new UniLioComplianceItemDto(
                e.CourseId,
                e.Course.Title,
                e.Course.Area,
                e.ProgressPct,
                e.Status,
                e.DueDate,
                e.DueDate.HasValue && e.DueDate.Value < now && e.Status != "completed"))
            .ToListAsync(cancellationToken);

        return new UniLioComplianceDto(
            items,
            items.Count(i => i.Status == "completed"),
            items.Count(i => i.Status != "completed"),
            items.Count(i => i.IsOverdue));
    }

    public async Task<UniLioCommunityPageDto> GetCommunityAsync(
        UniLioQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var postsQuery = db.UniLioCommunityPosts.AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            postsQuery = postsQuery.Where(p =>
                EF.Functions.ILike(p.Body, pattern) ||
                EF.Functions.ILike(p.Author.Name, pattern) ||
                (p.Course != null && EF.Functions.ILike(p.Course.Title, pattern)));
        }

        var totalCount = await postsQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await postsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new UniLioCommunityPostDto(
                p.Id,
                p.Author.Name,
                p.Author.PhotoUrl,
                p.Course != null ? p.Course.Title : null,
                p.Body,
                p.LikesCount,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new UniLioCommunityPageDto(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<UniLioRecommendationsDto> GetRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        var items = await BuildRecommendationsAsync(viewer, cancellationToken);
        return new UniLioRecommendationsDto(items);
    }

    public async Task<UniLioRecommendationsDto> GetCourseRecommendationsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        var visibleIds = await GetVisibleCourseIdsAsync(viewer, cancellationToken);
        if (!visibleIds.Contains(courseId))
        {
            throw new KeyNotFoundException($"Curso {courseId} não encontrado.");
        }

        var items = await BuildCourseRecommendationsAsync(viewer, courseId, cancellationToken);
        return new UniLioRecommendationsDto(items);
    }

    public async Task<UniLioEventsDto> GetEventsAsync(CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);

        var registeredEventIds = await db.UniLioEventRegistrations.AsNoTracking()
            .Where(r => r.PersonId == viewer.PersonId)
            .Select(r => r.EventId)
            .ToListAsync(cancellationToken);

        var items = await db.UniLioEvents.AsNoTracking()
            .Include(e => e.Instructor)
            .Include(e => e.Registrations)
            .OrderBy(e => e.StartsAt)
            .Select(e => new UniLioEventDto(
                e.Id,
                e.Title,
                e.EventType,
                e.StartsAt,
                e.EndsAt,
                e.Instructor != null ? e.Instructor.Name : null,
                e.MaxAttendees,
                e.Registrations.Count,
                registeredEventIds.Contains(e.Id),
                e.MeetingUrl))
            .ToListAsync(cancellationToken);

        return new UniLioEventsDto(items);
    }

    public async Task<UniLioSkillsDto> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);

        var personSkills = await db.UniLioPersonSkills.AsNoTracking()
            .Include(ps => ps.Skill)
            .Where(ps => ps.PersonId == viewer.PersonId)
            .OrderBy(ps => ps.Skill.Category)
            .ThenBy(ps => ps.Skill.Name)
            .ToListAsync(cancellationToken);

        var skillIds = personSkills.Select(ps => ps.SkillId).ToList();

        var relatedCourses = await db.UniLioCourseSkills.AsNoTracking()
            .Include(cs => cs.Course)
            .Where(cs => skillIds.Contains(cs.SkillId))
            .GroupBy(cs => cs.SkillId)
            .Select(g => new
            {
                SkillId = g.Key,
                Titles = g.Select(cs => cs.Course.Title).Distinct().Take(5).ToList(),
            })
            .ToListAsync(cancellationToken);

        var relatedBySkill = relatedCourses.ToDictionary(x => x.SkillId, x => x.Titles);

        var items = personSkills.Select(ps => new UniLioSkillLevelDto(
            ps.SkillId,
            ps.Skill.Name,
            ps.Skill.Category,
            ps.Level,
            SkillGapTargetLevel,
            relatedBySkill.TryGetValue(ps.SkillId, out var titles)
                ? titles
                : []))
            .ToList();

        return new UniLioSkillsDto(items);
    }

    public async Task<UniLioManagerTeamDto> GetManagerTeamAsync(CancellationToken cancellationToken = default)
    {
        await UniLioAuthorization.EnsureTeamViewAsync(permissionService, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        var hasGlobalTeamScope = await UniLioAuthorization.HasGlobalDataScopeAsync(
            permissionService,
            "unilio.team.view",
            cancellationToken)
            || await UniLioAuthorization.CanApproveCoursesAsync(permissionService, cancellationToken);

        IQueryable<Person> teamQuery = db.People.AsNoTracking().Where(p => p.IsActive);
        if (!hasGlobalTeamScope)
        {
            teamQuery = teamQuery.Where(p => p.ManagerId == viewer.PersonId);
        }

        var members = await teamQuery
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                Department = p.Dept ?? (p.Department != null ? p.Department.Name : string.Empty),
            })
            .ToListAsync(cancellationToken);

        var memberIds = members.Select(m => m.Id).ToList();
        var enrollments = await db.UniLioEnrollments.AsNoTracking()
            .Include(e => e.Course)
            .Where(e => memberIds.Contains(e.PersonId))
            .ToListAsync(cancellationToken);

        var teamMembers = members.Select(m =>
        {
            var memberEnrollments = enrollments.Where(e => e.PersonId == m.Id).ToList();
            var enrolledCount = memberEnrollments.Count;
            var completedCount = memberEnrollments.Count(e => e.Status == "completed");
            var mandatoryPending = memberEnrollments.Count(e =>
                e.Course.IsMandatory && e.Status != "completed");
            var avgProgress = enrolledCount == 0
                ? 0
                : (int)Math.Round(memberEnrollments.Average(e => e.ProgressPct));

            return new UniLioTeamMemberDto(
                m.Id,
                m.Name,
                m.Department,
                enrolledCount,
                completedCount,
                mandatoryPending,
                avgProgress);
        }).ToList();

        var avgCompletionPct = teamMembers.Count == 0
            ? 0
            : (int)Math.Round(teamMembers.Average(m =>
                m.EnrolledCount == 0 ? 0 : m.CompletedCount * 100.0 / m.EnrolledCount));

        return new UniLioManagerTeamDto(teamMembers, teamMembers.Count, avgCompletionPct);
    }

    public async Task<UniLioInstructorCoursesDto> GetInstructorCoursesAsync(CancellationToken cancellationToken = default)
    {
        await UniLioAuthorization.EnsureInstructorPanelAsync(permissionService, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        var namePattern = $"%{viewer.Name}%";

        var courses = await db.UniLioCourses.AsNoTracking()
            .Where(c =>
                (c.InstructorPersonId.HasValue && c.InstructorPersonId == viewer.PersonId)
                || EF.Functions.ILike(c.InstructorName, namePattern))
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);

        var courseIds = courses.Select(c => c.Id).ToList();
        var enrollments = await db.UniLioEnrollments.AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId))
            .ToListAsync(cancellationToken);

        var items = courses.Select(c =>
        {
            var courseEnrollments = enrollments.Where(e => e.CourseId == c.Id).ToList();
            return new UniLioInstructorCourseDto(
                c.Id,
                c.Title,
                c.Area,
                courseEnrollments.Count,
                courseEnrollments.Count(e => e.Status == "completed"),
                c.Rating,
                c.Status);
        }).ToList();

        return new UniLioInstructorCoursesDto(items);
    }

    public async Task<UniLioReportsDto> GetReportsSummaryAsync(
        UniLioQuery query,
        CancellationToken cancellationToken = default)
    {
        await UniLioAuthorization.EnsureReportsViewAsync(permissionService, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        var hasGlobalReportsScope = await UniLioAuthorization.HasGlobalDataScopeAsync(
            permissionService,
            "unilio.reports.view",
            cancellationToken)
            || await UniLioAuthorization.CanApproveCoursesAsync(permissionService, cancellationToken);

        IQueryable<UniLioEnrollment> enrollmentsQuery = db.UniLioEnrollments.AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Person);

        if (!hasGlobalReportsScope)
        {
            var directReportIds = await db.People.AsNoTracking()
                .Where(p => p.ManagerId == viewer.PersonId && p.IsActive)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            enrollmentsQuery = enrollmentsQuery.Where(e => directReportIds.Contains(e.PersonId));
        }

        if (!string.IsNullOrWhiteSpace(query.Area))
        {
            enrollmentsQuery = enrollmentsQuery.Where(e => e.Course.Area == query.Area);
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            enrollmentsQuery = enrollmentsQuery.Where(e => e.Course.Department == query.Department);
        }

        var enrollments = await enrollmentsQuery.ToListAsync(cancellationToken);

        var totalEnrollments = enrollments.Count;
        var completed = enrollments.Count(e => e.Status == "completed");
        var inProgress = enrollments.Count(e => e.Status == "in_progress");
        var mandatoryPending = enrollments.Count(e =>
            e.Course.IsMandatory && e.Status != "completed");
        var avgProgress = totalEnrollments == 0
            ? 0
            : (int)Math.Round(enrollments.Average(e => e.ProgressPct));
        var completionRate = totalEnrollments == 0
            ? 0
            : (int)Math.Round(completed * 100.0 / totalEnrollments);

        var metrics = new List<UniLioReportMetricDto>
        {
            new("Matrículas", totalEnrollments.ToString(CultureInfo.InvariantCulture), ""),
            new("Concluídos", completed.ToString(CultureInfo.InvariantCulture), $"{completionRate}%"),
            new("Em andamento", inProgress.ToString(CultureInfo.InvariantCulture), ""),
            new("Obrigatórios pendentes", mandatoryPending.ToString(CultureInfo.InvariantCulture), ""),
            new("Progresso médio", $"{avgProgress}%", ""),
        };

        var visibleIds = await GetVisibleCourseIdsAsync(viewer, cancellationToken);
        var topCourses = await GetCoursesQuery(visibleIds)
            .OrderByDescending(c => c.Rating)
            .ThenByDescending(c => c.Enrollments.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var mappedTopCourses = await MapCourseSummariesAsync(viewer, topCourses, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var complianceGaps = enrollments
            .Where(e => e.Course.IsMandatory && e.Status != "completed")
            .OrderBy(e => e.DueDate)
            .Take(10)
            .Select(e => new UniLioComplianceItemDto(
                e.CourseId,
                e.Course.Title,
                e.Course.Area,
                e.ProgressPct,
                e.Status,
                e.DueDate,
                e.DueDate.HasValue && e.DueDate.Value < now && e.Status != "completed"))
            .ToList();

        return new UniLioReportsDto(metrics, mappedTopCourses, complianceGaps);
    }

    private async Task<ViewerContext> GetViewerAsync(CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var person = await db.People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken)
            ?? throw new InvalidOperationException($"Pessoa {personId} não encontrada.");

        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        return new ViewerContext(
            person.Id,
            person.Name,
            person.Email,
            person.Dept ?? string.Empty,
            roles);
    }

    private async Task<bool> CanViewCourseEnrollmentsAsync(CancellationToken cancellationToken) =>
        await UniLioAuthorization.CanUseInstructorPanelAsync(permissionService, cancellationToken)
        || await permissionService.HasPermissionAsync("unilio.team.view", cancellationToken: cancellationToken)
        || await UniLioAuthorization.CanApproveCoursesAsync(permissionService, cancellationToken);

    private async Task<List<Guid>> GetVisibleCourseIdsAsync(
        ViewerContext viewer,
        CancellationToken cancellationToken)
    {
        var courses = await db.UniLioCourses.AsNoTracking()
            .Where(c => c.Status == "published" || c.Status == "active")
            .Select(c => new { c.Id, c.VisibilityJson })
            .ToListAsync(cancellationToken);

        return courses
            .Where(c => IsCourseVisible(c.VisibilityJson, viewer))
            .Select(c => c.Id)
            .ToList();
    }

    private IQueryable<UniLioCourse> GetCoursesQuery(IReadOnlyCollection<Guid> visibleIds)
    {
        return db.UniLioCourses.AsNoTracking()
            .Include(c => c.CourseSkills).ThenInclude(cs => cs.Skill)
            .Include(c => c.IntegrationLinks)
            .Where(c => visibleIds.Contains(c.Id));
    }

    private static bool IsCourseVisible(string? visibilityJson, ViewerContext viewer)
    {
        if (string.IsNullOrWhiteSpace(visibilityJson))
        {
            return true;
        }

        try
        {
            var visibility = JsonSerializer.Deserialize<CourseVisibility>(visibilityJson, JsonOptions);
            if (visibility is null)
            {
                return true;
            }

            var roleAllowed = visibility.Roles is null or { Count: 0 }
                || visibility.Roles.Any(role =>
                    viewer.Roles.Any(userRole =>
                        string.Equals(userRole.ToString(), role, StringComparison.OrdinalIgnoreCase)));

            var departmentAllowed = visibility.Departments is null or { Count: 0 }
                || visibility.Departments.Any(dept =>
                    !string.IsNullOrWhiteSpace(viewer.Department)
                    && string.Equals(viewer.Department, dept, StringComparison.OrdinalIgnoreCase));

            return roleAllowed && departmentAllowed;
        }
        catch
        {
            return true;
        }
    }

    private static IQueryable<UniLioCourse> ApplyCourseFilters(
        IQueryable<UniLioCourse> queryable,
        UniLioQuery query,
        Guid personId)
    {
        if (!string.IsNullOrWhiteSpace(query.Area))
        {
            queryable = queryable.Where(c => c.Area == query.Area);
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            queryable = queryable.Where(c => c.Department == query.Department);
        }

        if (!string.IsNullOrWhiteSpace(query.ContentType))
        {
            queryable = queryable.Where(c => c.ContentType == query.ContentType);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            if (status is "in_progress" or "completed" or "not_enrolled" or "not_started")
            {
                queryable = queryable.Where(c =>
                    (status == "not_enrolled" || status == "not_started")
                        ? !c.Enrollments.Any(e => e.PersonId == personId)
                        : c.Enrollments.Any(e =>
                            e.PersonId == personId
                            && e.Status == (status == "not_started" ? "in_progress" : status)));
            }
            else
            {
                queryable = queryable.Where(c => c.Status == query.Status);
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            queryable = queryable.Where(c =>
                EF.Functions.ILike(c.Title, pattern)
                || EF.Functions.ILike(c.Description, pattern)
                || EF.Functions.ILike(c.InstructorName, pattern)
                || EF.Functions.ILike(c.Area, pattern));
        }

        return queryable;
    }

    private async Task<List<UniLioCourseSummaryDto>> MapCourseSummariesAsync(
        ViewerContext viewer,
        IReadOnlyList<UniLioCourse> courses,
        CancellationToken cancellationToken)
    {
        if (courses.Count == 0)
        {
            return [];
        }

        var courseIds = courses.Select(c => c.Id).ToList();
        var enrollments = await db.UniLioEnrollments.AsNoTracking()
            .Where(e => e.PersonId == viewer.PersonId && courseIds.Contains(e.CourseId))
            .ToDictionaryAsync(e => e.CourseId, cancellationToken);
        var stats = await GetCourseEnrollmentStatsAsync(courseIds, cancellationToken);

        return courses.Select(course =>
        {
            enrollments.TryGetValue(course.Id, out var enrollment);
            stats.TryGetValue(course.Id, out var courseStats);
            return new UniLioCourseSummaryDto(
                course.Id,
                course.SeedKey,
                course.Title,
                course.Description,
                course.ContentType,
                course.DurationMinutes,
                course.IsMandatory,
                course.Area,
                course.Department,
                course.Rating,
                course.InstructorName,
                course.ThumbnailUrl,
                course.ExternalUrl,
                course.Provider,
                course.Status,
                enrollment?.ProgressPct,
                enrollment?.Status ?? "not_enrolled",
                course.CourseSkills.Select(cs => cs.Skill.Name).OrderBy(n => n).ToList(),
                MapIntegrationLinks(course.IntegrationLinks),
                courseStats.Enrolled,
                courseStats.Completed);
        }).ToList();
    }

    private async Task<Dictionary<Guid, (int Enrolled, int Completed)>> GetCourseEnrollmentStatsAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken)
    {
        if (courseIds.Count == 0)
        {
            return [];
        }

        var rows = await db.UniLioEnrollments.AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId))
            .GroupBy(e => e.CourseId)
            .Select(g => new
            {
                CourseId = g.Key,
                Enrolled = g.Count(),
                Completed = g.Count(e => e.Status == "completed"),
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.CourseId, r => (r.Enrolled, r.Completed));
    }

    private async Task<UniLioPathSummaryDto?> GetActivePathSummaryAsync(
        Guid personId,
        CancellationToken cancellationToken)
    {
        var paths = await db.UniLioLearningPaths.AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.PathCourses)
            .ToListAsync(cancellationToken);

        UniLioPathSummaryDto? best = null;
        foreach (var path in paths)
        {
            var summary = await MapPathSummaryAsync(path, personId, cancellationToken);
            if (summary.ProgressPct is > 0 and < 100)
            {
                if (best is null || summary.ProgressPct > best.ProgressPct)
                {
                    best = summary;
                }
            }
        }

        return best ?? (paths.Count > 0
            ? await MapPathSummaryAsync(paths[0], personId, cancellationToken)
            : null);
    }

    private async Task<UniLioPathSummaryDto> MapPathSummaryAsync(
        UniLioLearningPath path,
        Guid personId,
        CancellationToken cancellationToken)
    {
        var courseIds = path.PathCourses.Select(pc => pc.CourseId).ToList();
        var courseCount = courseIds.Count;

        if (courseCount == 0)
        {
            return new UniLioPathSummaryDto(
                path.Id,
                path.SeedKey,
                path.Title,
                path.Description,
                0,
                0,
                0);
        }

        var enrollments = await db.UniLioEnrollments.AsNoTracking()
            .Where(e => e.PersonId == personId && courseIds.Contains(e.CourseId))
            .ToListAsync(cancellationToken);

        var completedCourses = enrollments.Count(e => e.Status == "completed");
        var progressPct = (int)Math.Round(completedCourses * 100.0 / courseCount);

        return new UniLioPathSummaryDto(
            path.Id,
            path.SeedKey,
            path.Title,
            path.Description,
            courseCount,
            progressPct,
            completedCourses);
    }

    private async Task<IReadOnlyList<UniLioRecommendationDto>> BuildRecommendationsAsync(
        ViewerContext viewer,
        CancellationToken cancellationToken)
    {
        var gapSkillIds = await db.UniLioPersonSkills.AsNoTracking()
            .Where(ps => ps.PersonId == viewer.PersonId && ps.Level < SkillGapTargetLevel)
            .Select(ps => ps.SkillId)
            .ToListAsync(cancellationToken);

        if (gapSkillIds.Count == 0)
        {
            return [];
        }

        var enrolledCourseIds = await db.UniLioEnrollments.AsNoTracking()
            .Where(e => e.PersonId == viewer.PersonId)
            .Select(e => e.CourseId)
            .ToListAsync(cancellationToken);

        var recommendations = await db.UniLioCourseSkills.AsNoTracking()
            .Include(cs => cs.Course)
            .Include(cs => cs.Skill)
            .Where(cs => gapSkillIds.Contains(cs.SkillId))
            .Where(cs => cs.Course.Status == "published" || cs.Course.Status == "active")
            .Where(cs => !enrolledCourseIds.Contains(cs.CourseId))
            .GroupBy(cs => cs.CourseId)
            .Select(g => g.First())
            .OrderByDescending(cs => cs.Course.Rating)
            .Take(10)
            .ToListAsync(cancellationToken);

        return recommendations
            .Where(cs => IsCourseVisible(cs.Course.VisibilityJson, viewer))
            .Select(cs => new UniLioRecommendationDto(
                cs.CourseId,
                cs.Course.Title,
                $"Recomendado para desenvolver {cs.Skill.Name}.",
                cs.Course.Area,
                cs.Course.DurationMinutes,
                cs.Course.ContentType,
                cs.Course.ThumbnailUrl))
            .ToList();
    }

    private async Task<IReadOnlyList<UniLioRecommendationDto>> BuildCourseRecommendationsAsync(
        ViewerContext viewer,
        Guid completedCourseId,
        CancellationToken cancellationToken)
    {
        var completedCourse = await db.UniLioCourses.AsNoTracking()
            .Include(c => c.CourseSkills).ThenInclude(cs => cs.Skill)
            .FirstOrDefaultAsync(c => c.Id == completedCourseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Curso {completedCourseId} não encontrado.");

        var skillIds = completedCourse.CourseSkills.Select(cs => cs.SkillId).ToHashSet();
        var area = completedCourse.Area;

        var completedCourseIds = await db.UniLioEnrollments.AsNoTracking()
            .Where(e => e.PersonId == viewer.PersonId && e.Status == "completed")
            .Select(e => e.CourseId)
            .ToListAsync(cancellationToken);

        var excludeIds = completedCourseIds.Append(completedCourseId).ToHashSet();

        var candidates = await db.UniLioCourses.AsNoTracking()
            .Include(c => c.CourseSkills).ThenInclude(cs => cs.Skill)
            .Where(c => !excludeIds.Contains(c.Id))
            .Where(c => c.Status == "published" || c.Status == "active")
            .Where(c => c.Area == area || c.CourseSkills.Any(cs => skillIds.Contains(cs.SkillId)))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(c => IsCourseVisible(c.VisibilityJson, viewer))
            .OrderByDescending(c => c.CourseSkills.Count(cs => skillIds.Contains(cs.SkillId)))
            .ThenByDescending(c => c.Area == area)
            .ThenByDescending(c => c.Rating)
            .Take(3)
            .Select(c =>
            {
                var sharedSkill = c.CourseSkills
                    .FirstOrDefault(cs => skillIds.Contains(cs.SkillId))
                    ?.Skill.Name;

                var reason = sharedSkill is not null
                    ? $"Complementa o tema de {sharedSkill}."
                    : $"Relacionado à área de {c.Area}.";

                return new UniLioRecommendationDto(
                    c.Id,
                    c.Title,
                    reason,
                    c.Area,
                    c.DurationMinutes,
                    c.ContentType,
                    c.ThumbnailUrl);
            })
            .ToList();
    }

    private async Task<bool> TryIssueCertificateAsync(
        Guid personId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var alreadyIssued = await db.UniLioCertificates.AsNoTracking()
            .AnyAsync(c => c.PersonId == personId && c.CourseId == courseId, cancellationToken);

        if (alreadyIssued)
        {
            return true;
        }

        var assessment = await db.UniLioAssessments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.CourseId == courseId, cancellationToken);

        if (assessment is not null)
        {
            var passed = await db.UniLioAssessmentAttempts.AsNoTracking()
                .AnyAsync(a => a.PersonId == personId && a.AssessmentId == assessment.Id && a.Passed, cancellationToken);

            if (!passed)
            {
                return false;
            }
        }

        var now = DateTimeOffset.UtcNow;
        var certificate = new UniLioCertificate
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            CourseId = courseId,
            CertificateCode = $"UNILIO-{now:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            IssuedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.UniLioCertificates.Add(certificate);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static int CalculateAssessmentScore(
        string questionsJson,
        IReadOnlyDictionary<string, string> answers)
    {
        var questions = ParseAssessmentQuestions(questionsJson);
        if (questions.Count == 0)
        {
            return 0;
        }

        var correct = 0;
        foreach (var question in questions)
        {
            if (answers.TryGetValue(question.Id, out var submitted)
                && string.Equals(submitted?.Trim(), question.CorrectAnswer?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                correct++;
            }
        }

        return (int)Math.Round(correct * 100.0 / questions.Count);
    }

    private static List<AssessmentQuestion> ParseAssessmentQuestions(string questionsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<AssessmentQuestion>>(questionsJson, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<UniLioIntegrationLinkDto> MapIntegrationLinks(
        IEnumerable<UniLioIntegrationLink> links) =>
        links
            .OrderBy(l => l.SourceType)
            .Select(l => new UniLioIntegrationLinkDto(
                l.SourceType,
                l.SourceKey,
                MapIntegrationLabel(l.SourceType)))
            .ToList();

    private static int? ParseQuizPassingScore(string? quizJson)
    {
        if (string.IsNullOrWhiteSpace(quizJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(quizJson);
            if (doc.RootElement.TryGetProperty("passingScore", out var score) && score.TryGetInt32(out var value))
            {
                return value;
            }
        }
        catch
        {
            // ignore malformed quiz json
        }

        return 70;
    }

    private static string MapIntegrationLabel(string sourceType) =>
        sourceType.Trim().ToLowerInvariant() switch
        {
            "biblioteca" => "Biblioteca",
            "beneficio" => "Benefícios",
            "loop" => "Loop Aprendizados",
            _ => sourceType,
        };

    private static IReadOnlyList<string> DeserializeRoles(string raw)
    {
        try
        {
            var values = JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
            var roles = new List<string>();
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (Enum.TryParse<UserRole>(value.Trim(), true, out _))
                {
                    roles.Add(value.Trim());
                }
            }

            return roles.Count > 0 ? roles : ["Employee", "Manager", "HR", "Admin"];
        }
        catch
        {
            return ["Employee", "Manager", "HR", "Admin"];
        }
    }

    private static IReadOnlyList<string> DeserializeEmails(string raw)
    {
        try
        {
            var values = JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private sealed record ViewerContext(
        Guid PersonId,
        string Name,
        string Email,
        string Department,
        IReadOnlyList<UserRole> Roles);

    private sealed class CourseVisibility
    {
        public List<string>? Roles { get; set; }

        public List<string>? Departments { get; set; }
    }

    private sealed class AssessmentQuestion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("correctAnswer")]
        public string CorrectAnswer { get; set; } = string.Empty;
    }
}
