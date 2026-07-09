using System.Globalization;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed partial class UniLioService
{
    public async Task<UniLioQuestionDetailDto> CreateModuleQuestionAsync(
        Guid courseId,
        Guid? moduleId,
        CreateUniLioQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureEnrolledAsync(courseId, viewer, cancellationToken);

        var course = await db.UniLioCourses.AsNoTracking()
            .Include(c => c.Modules)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Curso {courseId} não encontrado.");

        var resolvedModuleId = moduleId ?? request.ModuleId;
        UniLioCourseModule? module = null;
        if (resolvedModuleId.HasValue)
        {
            module = course.Modules.FirstOrDefault(m => m.Id == resolvedModuleId.Value)
                ?? throw new KeyNotFoundException($"Módulo {resolvedModuleId} não encontrado no curso {courseId}.");
        }

        var body = request.Body?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("A dúvida não pode estar vazia.");
        }

        if (body.Length > 2000)
        {
            throw new ArgumentException("A dúvida deve ter no máximo 2000 caracteres.");
        }

        var visibility = NormalizeVisibility(request.Visibility);
        var now = DateTimeOffset.UtcNow;

        var question = new UniLioModuleQuestion
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            ModuleId = resolvedModuleId,
            AuthorPersonId = viewer.PersonId,
            Body = body,
            Visibility = visibility,
            Status = "open",
            LearnerReadAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.UniLioModuleQuestions.Add(question);
        await db.SaveChangesAsync(cancellationToken);

        if (course.InstructorPersonId.HasValue)
        {
            await notificationService.NotifyUniLioQuestionToInstructorAsync(
                course.InstructorPersonId.Value,
                viewer.Name,
                course.Title,
                module?.Title,
                question.Id,
                cancellationToken);
        }

        return await MapQuestionDetailAsync(question.Id, viewer, forInstructor: false, cancellationToken);
    }

    public async Task<UniLioQuestionsPageDto> GetModuleQuestionsAsync(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        await EnsureEnrolledAsync(courseId, viewer, cancellationToken);

        var moduleExists = await db.UniLioCourseModules.AsNoTracking()
            .AnyAsync(m => m.Id == moduleId && m.CourseId == courseId, cancellationToken);
        if (!moduleExists)
        {
            throw new KeyNotFoundException($"Módulo {moduleId} não encontrado no curso {courseId}.");
        }

        var query = db.UniLioModuleQuestions.AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.Course)
            .Include(q => q.Module)
            .Include(q => q.Replies)
            .Where(q => q.CourseId == courseId)
            .Where(q =>
                q.ModuleId == moduleId
                || q.ModuleId == null)
            .Where(q =>
                q.Visibility == "public"
                || q.AuthorPersonId == viewer.PersonId);

        var questions = await query
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = questions
            .Select(q => MapQuestionSummary(q, viewer.PersonId, forInstructor: false))
            .ToList();

        return new UniLioQuestionsPageDto(items, 1, items.Count, items.Count, 1, 0);
    }

    public async Task<UniLioQuestionsPageDto> GetMyQuestionsAsync(
        UniLioQuestionQuery query,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var queryable = db.UniLioModuleQuestions.AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.Course)
            .Include(q => q.Module)
            .Include(q => q.Replies)
            .Where(q => q.AuthorPersonId == viewer.PersonId);

        if (query.CourseId.HasValue)
        {
            queryable = queryable.Where(q => q.CourseId == query.CourseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            queryable = queryable.Where(q => q.Status == status);
        }

        var allForUnread = await queryable.ToListAsync(cancellationToken);
        var unreadCount = allForUnread.Count(q => IsUnreadForLearner(q));

        if (query.UnreadOnly == true)
        {
            var unreadIds = allForUnread
                .Where(q => IsUnreadForLearner(q))
                .Select(q => q.Id)
                .ToHashSet();
            queryable = queryable.Where(q => unreadIds.Contains(q.Id));
        }

        var totalCount = await queryable.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var questions = await queryable
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = questions
            .Select(q => MapQuestionSummary(q, viewer.PersonId, forInstructor: false))
            .ToList();

        return new UniLioQuestionsPageDto(items, page, pageSize, totalCount, totalPages, unreadCount);
    }

    public async Task<UniLioQuestionsPageDto> GetInstructorQuestionsAsync(
        UniLioQuestionQuery query,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        var persona = await ResolvePersonaAsync(viewer, cancellationToken);
        if (persona is not ("admin" or "instructor"))
        {
            throw new UnauthorizedAccessException("Sem permissão para consultar dúvidas de instrutor.");
        }

        var courseIds = await GetInstructorCourseIdsAsync(viewer, cancellationToken);
        if (courseIds.Count == 0)
        {
            return new UniLioQuestionsPageDto([], 1, query.PageSize, 0, 0, 0);
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var queryable = db.UniLioModuleQuestions.AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.Course)
            .Include(q => q.Module)
            .Include(q => q.Replies)
            .Where(q => courseIds.Contains(q.CourseId));

        if (query.CourseId.HasValue)
        {
            queryable = queryable.Where(q => q.CourseId == query.CourseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            queryable = queryable.Where(q => q.Status == status);
        }

        var allForUnread = await queryable.ToListAsync(cancellationToken);
        var unreadCount = allForUnread.Count(q => IsUnreadForInstructor(q));

        if (query.UnreadOnly == true)
        {
            var unreadIds = allForUnread
                .Where(q => IsUnreadForInstructor(q))
                .Select(q => q.Id)
                .ToHashSet();
            queryable = queryable.Where(q => unreadIds.Contains(q.Id));
        }

        var totalCount = await queryable.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var questions = await queryable
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = questions
            .Select(q => MapQuestionSummary(q, viewer.PersonId, forInstructor: true))
            .ToList();

        return new UniLioQuestionsPageDto(items, page, pageSize, totalCount, totalPages, unreadCount);
    }

    public async Task<UniLioQuestionDetailDto> GetInstructorQuestionDetailAsync(
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        var persona = await ResolvePersonaAsync(viewer, cancellationToken);
        if (persona is not ("admin" or "instructor"))
        {
            throw new UnauthorizedAccessException("Sem permissão para consultar esta dúvida.");
        }

        return await MapQuestionDetailAsync(questionId, viewer, forInstructor: true, cancellationToken);
    }

    public async Task<UniLioQuestionDetailDto> ReplyToQuestionAsync(
        Guid questionId,
        ReplyUniLioQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        var persona = await ResolvePersonaAsync(viewer, cancellationToken);
        if (persona is not ("admin" or "instructor"))
        {
            throw new UnauthorizedAccessException("Sem permissão para responder dúvidas.");
        }

        var question = await db.UniLioModuleQuestions
            .Include(q => q.Course)
            .Include(q => q.Module)
            .Include(q => q.Author)
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Dúvida {questionId} não encontrada.");

        if (!await IsInstructorOfCourseAsync(question.Course, viewer, cancellationToken))
        {
            throw new UnauthorizedAccessException("Sem permissão para responder esta dúvida.");
        }

        var body = request.Body?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("A resposta não pode estar vazia.");
        }

        if (body.Length > 2000)
        {
            throw new ArgumentException("A resposta deve ter no máximo 2000 caracteres.");
        }

        var now = DateTimeOffset.UtcNow;
        var reply = new UniLioModuleQuestionReply
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            AuthorPersonId = viewer.PersonId,
            Body = body,
            IsInstructorReply = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        question.Status = "answered";
        question.InstructorReadAt = now;
        question.LearnerReadAt = null;
        question.UpdatedAt = now;

        db.UniLioModuleQuestionReplies.Add(reply);
        await db.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyUniLioQuestionAnsweredToLearnerAsync(
            question.AuthorPersonId,
            question.Course.Title,
            question.Module?.Title,
            question.Id,
            cancellationToken);

        return await MapQuestionDetailAsync(questionId, viewer, forInstructor: true, cancellationToken);
    }

    public async Task MarkInstructorQuestionReadAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);
        var persona = await ResolvePersonaAsync(viewer, cancellationToken);
        if (persona is not ("admin" or "instructor"))
        {
            throw new UnauthorizedAccessException("Sem permissão.");
        }

        var question = await db.UniLioModuleQuestions
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Dúvida {questionId} não encontrada.");

        if (!await IsInstructorOfCourseAsync(question.Course, viewer, cancellationToken))
        {
            throw new UnauthorizedAccessException("Sem permissão.");
        }

        var now = DateTimeOffset.UtcNow;
        question.InstructorReadAt = now;
        question.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkLearnerQuestionReadAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        var viewer = await GetViewerAsync(cancellationToken);

        var question = await db.UniLioModuleQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.AuthorPersonId == viewer.PersonId, cancellationToken)
            ?? throw new KeyNotFoundException($"Dúvida {questionId} não encontrada.");

        var now = DateTimeOffset.UtcNow;
        question.LearnerReadAt = now;
        question.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureEnrolledAsync(
        Guid courseId,
        ViewerContext viewer,
        CancellationToken cancellationToken)
    {
        var visibleIds = await GetVisibleCourseIdsAsync(viewer, cancellationToken);
        if (!visibleIds.Contains(courseId))
        {
            throw new KeyNotFoundException($"Curso {courseId} não encontrado.");
        }

        var enrolled = await db.UniLioEnrollments.AsNoTracking()
            .AnyAsync(e => e.PersonId == viewer.PersonId && e.CourseId == courseId, cancellationToken);
        if (!enrolled)
        {
            await StartCourseAsync(courseId, cancellationToken);
        }
    }

    private async Task<List<Guid>> GetInstructorCourseIdsAsync(
        ViewerContext viewer,
        CancellationToken cancellationToken)
    {
        var namePattern = $"%{viewer.Name}%";
        return await db.UniLioCourses.AsNoTracking()
            .Where(c =>
                (c.InstructorPersonId.HasValue && c.InstructorPersonId == viewer.PersonId)
                || EF.Functions.ILike(c.InstructorName, namePattern))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    private Task<bool> IsInstructorOfCourseAsync(
        UniLioCourse course,
        ViewerContext viewer,
        CancellationToken cancellationToken)
    {
        if (viewer.Roles.Contains(UserRole.Admin) || viewer.Roles.Contains(UserRole.HR))
        {
            return Task.FromResult(true);
        }

        if (course.InstructorPersonId.HasValue && course.InstructorPersonId == viewer.PersonId)
        {
            return Task.FromResult(true);
        }

        var instructorName = course.InstructorName ?? string.Empty;
        var matchesName = instructorName.Contains(viewer.Name, StringComparison.OrdinalIgnoreCase);
        var matchesEmail = !string.IsNullOrWhiteSpace(viewer.Email)
            && instructorName.Contains(viewer.Email, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(matchesName || matchesEmail);
    }

    private static string NormalizeVisibility(string? visibility)
    {
        return string.Equals(visibility, "public", StringComparison.OrdinalIgnoreCase)
            ? "public"
            : "private";
    }

    private static bool IsUnreadForInstructor(UniLioModuleQuestion question)
        => question.InstructorReadAt is null;

    private static bool IsUnreadForLearner(UniLioModuleQuestion question)
    {
        var lastInstructorReply = question.Replies
            .Where(r => r.IsInstructorReply)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (lastInstructorReply is null)
        {
            return false;
        }

        return question.LearnerReadAt is null || question.LearnerReadAt < lastInstructorReply.CreatedAt;
    }

    private async Task<UniLioQuestionDetailDto> MapQuestionDetailAsync(
        Guid questionId,
        ViewerContext viewer,
        bool forInstructor,
        CancellationToken cancellationToken)
    {
        var question = await db.UniLioModuleQuestions.AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.Course)
            .Include(q => q.Module)
            .Include(q => q.Replies).ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Dúvida {questionId} não encontrada.");

        if (forInstructor)
        {
            if (!await IsInstructorOfCourseAsync(question.Course, viewer, cancellationToken))
            {
                throw new UnauthorizedAccessException("Sem permissão para consultar esta dúvida.");
            }
        }
        else if (question.AuthorPersonId != viewer.PersonId)
        {
            var enrolled = await db.UniLioEnrollments.AsNoTracking()
                .AnyAsync(e => e.PersonId == viewer.PersonId && e.CourseId == question.CourseId, cancellationToken);
            if (!enrolled || (question.Visibility != "public" && question.AuthorPersonId != viewer.PersonId))
            {
                throw new UnauthorizedAccessException("Sem permissão para consultar esta dúvida.");
            }
        }

        var unread = forInstructor
            ? IsUnreadForInstructor(question)
            : IsUnreadForLearner(question);

        var replies = question.Replies
            .OrderBy(r => r.CreatedAt)
            .Select(r => new UniLioQuestionReplyDto(
                r.Id,
                r.Author.Name,
                r.IsInstructorReply,
                r.Body,
                r.CreatedAt))
            .ToList();

        return new UniLioQuestionDetailDto(
            question.Id,
            question.CourseId,
            question.Course.Title,
            question.ModuleId,
            question.Module?.Title,
            question.AuthorPersonId,
            question.Author.Name,
            question.Body,
            question.Visibility,
            question.Status,
            unread,
            question.CreatedAt,
            replies);
    }

    private static UniLioQuestionSummaryDto MapQuestionSummary(
        UniLioModuleQuestion question,
        Guid viewerPersonId,
        bool forInstructor)
    {
        var unread = forInstructor
            ? IsUnreadForInstructor(question)
            : IsUnreadForLearner(question);

        var lastInstructorReply = question.Replies
            .Where(r => r.IsInstructorReply)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        return new UniLioQuestionSummaryDto(
            question.Id,
            question.CourseId,
            question.Course.Title,
            question.ModuleId,
            question.Module?.Title,
            question.AuthorPersonId,
            question.Author.Name,
            question.Body,
            question.Visibility,
            question.Status,
            unread,
            question.CreatedAt,
            question.Replies.Count,
            lastInstructorReply?.Body,
            lastInstructorReply?.CreatedAt);
    }
}
