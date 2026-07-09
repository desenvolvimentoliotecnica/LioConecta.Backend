using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Application.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services.UniLio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace LioConecta.Infrastructure.Services;

public sealed class UniLioAuthoringService(
    AppDbContext db,
    ICurrentUserService currentUserService,
    IPermissionService permissionService,
    INotificationService notificationService,
    UniLioApprovalRecipientResolver approvalRecipientResolver,
    IUniLioEmailNotifier uniLioEmailNotifier,
    IHostEnvironment hostEnvironment) : IUniLioAuthoringService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<UniLioAuthoringCourseListDto> ListCoursesAsync(CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        var query = db.UniLioCourses.AsNoTracking().Include(c => c.Modules).AsQueryable();

        if (!await UniLioAuthorization.CanApproveCoursesAsync(permissionService, cancellationToken))
        {
            query = query.Where(c =>
                c.InstructorPersonId == viewer.PersonId
                || EF.Functions.ILike(c.InstructorName, $"%{viewer.Name}%"));
        }

        var courses = await query.OrderByDescending(c => c.UpdatedAt).ToListAsync(cancellationToken);
        return new UniLioAuthoringCourseListDto(courses.Select(MapListItem).ToList());
    }

    public async Task<UniLioAuthoringCourseListDto> ListPendingCoursesAsync(CancellationToken cancellationToken = default)
    {
        await UniLioAuthorization.EnsureCanApproveCoursesAsync(permissionService, cancellationToken);

        var courses = await db.UniLioCourses.AsNoTracking()
            .Include(c => c.Modules)
            .Where(c => c.Status == UniLioCourseStatuses.PendingApproval)
            .OrderBy(c => c.SubmittedAt)
            .ToListAsync(cancellationToken);

        return new UniLioAuthoringCourseListDto(courses.Select(MapListItem).ToList());
    }

    public async Task<UniLioAuthoringCourseDto> GetCourseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(id, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureCanViewAsync(course, viewer, cancellationToken);
        return MapAuthoringCourse(course);
    }

    public async Task<UniLioApprovalReviewDto> GetApprovalReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await UniLioAuthorization.EnsureCanApproveCoursesAsync(permissionService, cancellationToken);

        var course = await db.UniLioCourses.AsNoTracking()
            .Include(c => c.Modules)
            .Include(c => c.Assessments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Curso {id} não encontrado.");

        if (!string.Equals(course.Status, UniLioCourseStatuses.PendingApproval, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Curso não está aguardando aprovação.");
        }

        string? submittedByName = null;
        if (course.SubmittedByPersonId is Guid submitterId)
        {
            submittedByName = await db.People.AsNoTracking()
                .Where(p => p.Id == submitterId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var assessment = course.Assessments.FirstOrDefault();
        UniLioApprovalAssessmentDto? assessmentDto = null;
        if (assessment is not null)
        {
            var questionCount = ParseAssessmentQuestionCount(assessment.QuestionsJson);
            assessmentDto = new UniLioApprovalAssessmentDto(assessment.Title, assessment.PassingScore, questionCount);
        }

        return new UniLioApprovalReviewDto(
            course.Id,
            course.Title,
            course.Description,
            course.Area,
            course.DurationMinutes,
            course.IsMandatory,
            course.ThumbnailUrl,
            course.InstructorName,
            submittedByName,
            course.SubmittedAt,
            ParseTags(course.TagsJson),
            course.VisibilityJson,
            course.Modules
                .OrderBy(m => m.SortOrder)
                .Select(m => new UniLioApprovalModuleDto(m.SortOrder, m.Title, m.ContentType, m.DurationMinutes))
                .ToList(),
            assessmentDto);
    }

    public async Task<UniLioAuthoringCourseDto> CreateCourseAsync(
        UniLioUpsertCourseRequest request,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        await UniLioAuthorization.EnsureCanAuthorAsync(permissionService, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var seedKey = await GenerateUniqueSeedKeyAsync(request.Title, cancellationToken);

        var course = new UniLioCourse
        {
            Id = Guid.NewGuid(),
            SeedKey = seedKey,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ContentType = request.ContentType.Trim(),
            DurationMinutes = request.DurationMinutes,
            IsMandatory = request.IsMandatory,
            Area = request.Area.Trim(),
            Department = request.Department.Trim(),
            InstructorName = string.IsNullOrWhiteSpace(request.InstructorName)
                ? viewer.Name
                : request.InstructorName.Trim(),
            InstructorPersonId = viewer.PersonId,
            ThumbnailUrl = request.ThumbnailUrl,
            ExternalUrl = request.ExternalUrl,
            Provider = request.Provider,
            VisibilityJson = request.VisibilityJson,
            TagsJson = SerializeTags(request.Tags),
            Status = UniLioCourseStatuses.Draft,
            Rating = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.UniLioCourses.Add(course);
        await db.SaveChangesAsync(cancellationToken);
        return MapAuthoringCourse(course);
    }

    public async Task<UniLioAuthoringCourseDto> UpdateCourseAsync(
        Guid id,
        UniLioUpsertCourseRequest request,
        CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(id, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureCanEditAsync(course, viewer, cancellationToken);

        course.Title = request.Title.Trim();
        course.Description = request.Description.Trim();
        course.ContentType = request.ContentType.Trim();
        course.DurationMinutes = request.DurationMinutes;
        course.IsMandatory = request.IsMandatory;
        course.Area = request.Area.Trim();
        course.Department = request.Department.Trim();
        course.InstructorName = request.InstructorName.Trim();
        course.ThumbnailUrl = request.ThumbnailUrl;
        course.ExternalUrl = request.ExternalUrl;
        course.Provider = request.Provider;
        course.VisibilityJson = request.VisibilityJson;
        course.TagsJson = SerializeTags(request.Tags);
        course.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return MapAuthoringCourse(course);
    }

    public async Task<UniLioAuthoringCourseDto> SubmitCourseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(id, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureCanEditAsync(course, viewer, cancellationToken);

        if (!UniLioCourseStatuses.EditableByInstructor.Contains(course.Status))
        {
            throw new InvalidOperationException("Curso não pode ser enviado para aprovação neste status.");
        }

        var now = DateTimeOffset.UtcNow;
        course.Status = UniLioCourseStatuses.PendingApproval;
        course.SubmittedAt = now;
        course.SubmittedByPersonId = viewer.PersonId;
        course.RejectionReason = null;
        course.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var submitter = await db.People.AsNoTracking()
                .FirstAsync(p => p.Id == viewer.PersonId, cancellationToken);
            var approvers = await approvalRecipientResolver.ResolveApproversAsync(viewer.PersonId, cancellationToken);
            var approverIds = approvers.Select(a => a.Id).ToList();
            await notificationService.NotifyUniLioCourseSubmittedAsync(
                approverIds,
                course.Id,
                course.Title,
                submitter.Name,
                cancellationToken);
            await uniLioEmailNotifier.NotifyCourseSubmittedAsync(course, submitter, approvers, cancellationToken);
        }
        catch
        {
            // Notificações são best-effort.
        }

        return MapAuthoringCourse(course);
    }

    public async Task<UniLioAuthoringCourseDto> ApproveCourseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(id, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await UniLioAuthorization.EnsureCanApproveCoursesAsync(permissionService, cancellationToken);

        if (!string.Equals(course.Status, UniLioCourseStatuses.PendingApproval, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Apenas cursos pendentes podem ser aprovados.");
        }

        var now = DateTimeOffset.UtcNow;
        var isFirstPublish = course.PublishedAt is null;
        course.Status = UniLioCourseStatuses.Published;
        course.PublishedAt ??= now;
        course.ReviewedAt = now;
        course.ReviewedById = viewer.PersonId;
        course.RejectionReason = null;
        course.UpdatedAt = now;

        if (isFirstPublish && course.FeedPostId is null)
        {
            var feedPost = UniLioFeedMapper.CreateCoursePublishedPost(course, viewer.PersonId, now);
            db.FeedPosts.Add(feedPost);
            course.FeedPostId = feedPost.Id;
        }

        await db.SaveChangesAsync(cancellationToken);

        try
        {
            if (course.InstructorPersonId is Guid instructorId)
            {
                await notificationService.NotifyUniLioCourseReviewedAsync(
                    instructorId,
                    course.Id,
                    course.Title,
                    approved: true,
                    rejectionReason: null,
                    cancellationToken);
            }

            if (isFirstPublish)
            {
                await notificationService.NotifyUniLioCoursePublishedAsync(course, cancellationToken);
            }
        }
        catch
        {
            // best-effort
        }

        return MapAuthoringCourse(course);
    }

    public async Task<UniLioAuthoringCourseDto> RejectCourseAsync(
        Guid id,
        UniLioRejectCourseRequest request,
        CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(id, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await UniLioAuthorization.EnsureCanApproveCoursesAsync(permissionService, cancellationToken);

        if (!string.Equals(course.Status, UniLioCourseStatuses.PendingApproval, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Apenas cursos pendentes podem ser rejeitados.");
        }

        var now = DateTimeOffset.UtcNow;
        course.Status = UniLioCourseStatuses.Rejected;
        course.ReviewedAt = now;
        course.ReviewedById = viewer.PersonId;
        course.RejectionReason = request.Reason?.Trim();
        course.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        try
        {
            if (course.InstructorPersonId is Guid instructorId)
            {
                await notificationService.NotifyUniLioCourseReviewedAsync(
                    instructorId,
                    course.Id,
                    course.Title,
                    approved: false,
                    rejectionReason: course.RejectionReason,
                    cancellationToken);
            }
        }
        catch
        {
            // best-effort
        }

        return MapAuthoringCourse(course);
    }

    public async Task<UniLioAuthoringCourseDto> PublishCourseDirectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(id, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await UniLioAuthorization.EnsureCanPublishCoursesAsync(permissionService, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var isFirstPublish = course.PublishedAt is null;
        course.Status = UniLioCourseStatuses.Published;
        course.PublishedAt ??= now;
        course.UpdatedAt = now;

        if (isFirstPublish && course.FeedPostId is null)
        {
            var feedPost = UniLioFeedMapper.CreateCoursePublishedPost(course, viewer.PersonId, now);
            db.FeedPosts.Add(feedPost);
            course.FeedPostId = feedPost.Id;
        }

        await db.SaveChangesAsync(cancellationToken);

        if (isFirstPublish)
        {
            try
            {
                await notificationService.NotifyUniLioCoursePublishedAsync(course, cancellationToken);
            }
            catch
            {
                // best-effort
            }
        }

        return MapAuthoringCourse(course);
    }

    public async Task<UniLioModuleDto> AddModuleAsync(
        Guid courseId,
        UniLioUpsertModuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(courseId, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureCanEditAsync(course, viewer, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var module = new UniLioCourseModule
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            SortOrder = request.SortOrder,
            Title = request.Title.Trim(),
            ContentType = request.ContentType.Trim(),
            ContentUrl = request.ContentUrl,
            DurationMinutes = request.DurationMinutes,
            ArticleHtml = request.ArticleHtml,
            QuizJson = request.QuizJson,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.UniLioCourseModules.Add(module);
        course.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return MapModule(module, false);
    }

    public async Task<UniLioModuleDto> UpdateModuleAsync(
        Guid courseId,
        Guid moduleId,
        UniLioUpsertModuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(courseId, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureCanEditAsync(course, viewer, cancellationToken);

        var module = course.Modules.FirstOrDefault(m => m.Id == moduleId)
            ?? throw new KeyNotFoundException($"Módulo {moduleId} não encontrado.");

        module.SortOrder = request.SortOrder;
        module.Title = request.Title.Trim();
        module.ContentType = request.ContentType.Trim();
        module.ContentUrl = request.ContentUrl;
        module.DurationMinutes = request.DurationMinutes;
        module.ArticleHtml = request.ArticleHtml;
        module.QuizJson = request.QuizJson;
        module.UpdatedAt = DateTimeOffset.UtcNow;
        course.UpdatedAt = module.UpdatedAt;

        await db.SaveChangesAsync(cancellationToken);
        return MapModule(module, false);
    }

    public async Task DeleteModuleAsync(Guid courseId, Guid moduleId, CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(courseId, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureCanEditAsync(course, viewer, cancellationToken);

        var module = course.Modules.FirstOrDefault(m => m.Id == moduleId)
            ?? throw new KeyNotFoundException($"Módulo {moduleId} não encontrado.");

        foreach (var attachment in module.Attachments.ToList())
        {
            UniLioModuleAttachmentStorage.DeleteIfExists(attachment.StorageFileName, hostEnvironment);
        }

        db.UniLioCourseModules.Remove(module);
        course.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UniLioModuleAttachmentDto> UploadModuleAttachmentAsync(
        Guid courseId,
        Guid moduleId,
        Stream content,
        string fileName,
        string? contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(courseId, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureCanEditAsync(course, viewer, cancellationToken);

        var module = course.Modules.FirstOrDefault(m => m.Id == moduleId)
            ?? throw new KeyNotFoundException($"Módulo {moduleId} não encontrado.");

        UniLioModuleAttachmentStorage.Validate(fileName, sizeBytes);
        var extension = Path.GetExtension(fileName);
        var storageFileName = await UniLioModuleAttachmentStorage.SaveAsync(
            content,
            extension,
            hostEnvironment,
            cancellationToken);

        var sortOrder = module.Attachments.Count == 0
            ? 1
            : module.Attachments.Max(a => a.SortOrder) + 1;
        var now = DateTimeOffset.UtcNow;
        var attachment = new UniLioModuleAttachment
        {
            ModuleId = moduleId,
            FileName = fileName.Trim(),
            StorageFileName = storageFileName,
            ContentType = UniLioModuleAttachmentStorage.ResolveContentType(fileName, contentType),
            SizeBytes = sizeBytes,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.UniLioModuleAttachments.Add(attachment);
        module.UpdatedAt = now;
        course.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return UniLioModuleAttachmentMapper.Map(attachment);
    }

    public async Task DeleteModuleAttachmentAsync(
        Guid courseId,
        Guid moduleId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(courseId, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureCanEditAsync(course, viewer, cancellationToken);

        var module = course.Modules.FirstOrDefault(m => m.Id == moduleId)
            ?? throw new KeyNotFoundException($"Módulo {moduleId} não encontrado.");

        var attachment = module.Attachments.FirstOrDefault(a => a.Id == attachmentId)
            ?? throw new KeyNotFoundException($"Anexo {attachmentId} não encontrado.");

        UniLioModuleAttachmentStorage.DeleteIfExists(attachment.StorageFileName, hostEnvironment);
        db.UniLioModuleAttachments.Remove(attachment);
        module.UpdatedAt = DateTimeOffset.UtcNow;
        course.UpdatedAt = module.UpdatedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UniLioAuthoringAssessmentDto> UpsertCourseAssessmentAsync(
        Guid courseId,
        UniLioUpsertAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(courseId, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureCanEditAsync(course, viewer, cancellationToken);

        var assessment = course.Assessments.FirstOrDefault();
        var now = DateTimeOffset.UtcNow;
        if (assessment is null)
        {
            assessment = new UniLioAssessment
            {
                CourseId = courseId,
                Title = request.Title.Trim(),
                PassingScore = request.PassingScore,
                QuestionsJson = request.QuestionsJson,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.UniLioAssessments.Add(assessment);
        }
        else
        {
            assessment.Title = request.Title.Trim();
            assessment.PassingScore = request.PassingScore;
            assessment.QuestionsJson = request.QuestionsJson;
            assessment.UpdatedAt = now;
        }

        course.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return new UniLioAuthoringAssessmentDto(
            assessment.Id,
            assessment.Title,
            assessment.PassingScore,
            assessment.QuestionsJson);
    }

    public async Task DeleteCourseAssessmentAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await LoadCourseAsync(courseId, cancellationToken);
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureCanEditAsync(course, viewer, cancellationToken);

        var assessment = course.Assessments.FirstOrDefault()
            ?? throw new KeyNotFoundException("Avaliação final não configurada para este curso.");

        db.UniLioAssessments.Remove(assessment);
        course.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<UniLioCourse> LoadCourseAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.UniLioCourses
            .Include(c => c.Modules).ThenInclude(m => m.Attachments)
            .Include(c => c.Assessments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Curso {id} não encontrado.");
    }

    private async Task<ViewerContext> GetViewerAsync(CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var person = await db.People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken)
            ?? throw new InvalidOperationException($"Pessoa {personId} não encontrada.");

        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        return new ViewerContext(person.Id, person.Name, person.Email, roles);
    }

    private static bool IsCourseInstructor(UniLioCourse course, ViewerContext viewer) =>
        course.InstructorPersonId == viewer.PersonId
        || course.InstructorName.Contains(viewer.Name, StringComparison.OrdinalIgnoreCase);

    private async Task EnsureCanViewAsync(
        UniLioCourse course,
        ViewerContext viewer,
        CancellationToken cancellationToken)
    {
        if (await UniLioAuthorization.CanApproveCoursesAsync(permissionService, cancellationToken))
        {
            return;
        }

        if (IsCourseInstructor(course, viewer))
        {
            return;
        }

        throw new UnauthorizedAccessException("Sem permissão para visualizar este curso.");
    }

    private async Task EnsureCanEditAsync(
        UniLioCourse course,
        ViewerContext viewer,
        CancellationToken cancellationToken)
    {
        if (await UniLioAuthorization.CanApproveCoursesAsync(permissionService, cancellationToken))
        {
            return;
        }

        if (!IsCourseInstructor(course, viewer))
        {
            throw new UnauthorizedAccessException("Sem permissão para editar este curso.");
        }

        if (!await permissionService.HasPermissionAsync("unilio.courses.edit.own", cancellationToken: cancellationToken))
        {
            throw new UnauthorizedAccessException("Sem permissão para editar cursos próprios.");
        }

        if (!UniLioCourseStatuses.EditableByInstructor.Contains(course.Status))
        {
            throw new UnauthorizedAccessException("Curso não editável neste status.");
        }
    }

    private async Task<string> GenerateUniqueSeedKeyAsync(string title, CancellationToken cancellationToken)
    {
        var slug = Slugify(title);
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "curso";
        }

        var candidate = slug;
        var suffix = 1;
        while (await db.UniLioCourses.AnyAsync(c => c.SeedKey == candidate, cancellationToken))
        {
            suffix++;
            candidate = $"{slug}-{suffix}";
        }

        return candidate;
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", "");
        normalized = Regex.Replace(normalized, @"\s+", "-");
        normalized = Regex.Replace(normalized, @"-+", "-");
        return normalized.Trim('-');
    }

    private static string? SerializeTags(IReadOnlyList<string>? tags)
    {
        if (tags is null || tags.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList());
    }

    private static IReadOnlyList<string> ParseTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static int ParseAssessmentQuestionCount(string questionsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(questionsJson);
            return doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.GetArrayLength() : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static UniLioAuthoringCourseSummaryDto MapListItem(UniLioCourse course) =>
        new(
            course.Id,
            course.Title,
            course.Area,
            course.Status,
            course.Modules.Count,
            course.SubmittedAt,
            course.PublishedAt);

    private static UniLioAuthoringCourseDto MapAuthoringCourse(UniLioCourse course)
    {
        var assessment = course.Assessments.FirstOrDefault();
        UniLioAuthoringAssessmentDto? assessmentDto = assessment is null
            ? null
            : new UniLioAuthoringAssessmentDto(
                assessment.Id,
                assessment.Title,
                assessment.PassingScore,
                assessment.QuestionsJson);

        return new UniLioAuthoringCourseDto(
            course.Id,
            course.SeedKey,
            course.Title,
            course.Description,
            course.ContentType,
            course.DurationMinutes,
            course.IsMandatory,
            course.Area,
            course.Department,
            course.InstructorName,
            course.InstructorPersonId,
            course.ThumbnailUrl,
            course.ExternalUrl,
            course.Provider,
            course.Status,
            course.VisibilityJson,
            ParseTags(course.TagsJson),
            course.PublishedAt,
            course.SubmittedAt,
            course.RejectionReason,
            course.Modules
                .OrderBy(m => m.SortOrder)
                .Select(m => MapModule(m, false))
                .ToList(),
            assessmentDto);
    }

    private static UniLioModuleDto MapModule(UniLioCourseModule module, bool isCompleted) =>
        new(
            module.Id,
            module.SortOrder,
            module.Title,
            module.ContentType,
            module.ContentUrl,
            module.DurationMinutes,
            module.ArticleHtml,
            module.QuizJson,
            ParseQuizPassingScore(module.QuizJson),
            isCompleted,
            UniLioModuleAttachmentMapper.Map(module.Attachments));

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
            // ignore
        }

        return 70;
    }

    private sealed record ViewerContext(
        Guid PersonId,
        string Name,
        string? Email,
        IReadOnlyList<UserRole> Roles);
}
