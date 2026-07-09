using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class UniLioCompletionService(
    AppDbContext db,
    INotificationService notificationService)
{
    public async Task TryNotifyCourseCompletedAsync(
        Guid personId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await db.UniLioEnrollments
            .Include(e => e.ModuleProgress)
            .FirstOrDefaultAsync(e => e.PersonId == personId && e.CourseId == courseId, cancellationToken);

        if (enrollment is null || enrollment.CompletionNotifiedAt is not null)
        {
            return;
        }

        var course = await db.UniLioCourses
            .Include(c => c.Modules)
            .Include(c => c.Assessments)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
        {
            return;
        }

        if (!await IsFullyCompletedAsync(enrollment, course, personId, cancellationToken))
        {
            return;
        }

        var learner = await db.People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);

        if (learner is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var feedPost = UniLioFeedMapper.CreateCourseCompletionPost(course, learner, now);
        db.FeedPosts.Add(feedPost);

        enrollment.CompletionNotifiedAt = now;
        enrollment.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        try
        {
            if (course.InstructorPersonId is Guid instructorId)
            {
                await notificationService.NotifyUniLioCourseCompletedToInstructorAsync(
                    instructorId,
                    learner.Name,
                    course.Title,
                    course.Id,
                    cancellationToken);
            }
        }
        catch
        {
            // best-effort
        }
    }

    private async Task<bool> IsFullyCompletedAsync(
        UniLioEnrollment enrollment,
        UniLioCourse course,
        Guid personId,
        CancellationToken cancellationToken)
    {
        var totalModules = course.Modules.Count;
        if (totalModules == 0)
        {
            return false;
        }

        var completedModuleIds = enrollment.ModuleProgress.Select(mp => mp.ModuleId).ToHashSet();
        var allModulesDone = course.Modules.All(m => completedModuleIds.Contains(m.Id));
        if (!allModulesDone)
        {
            return false;
        }

        var assessment = course.Assessments.FirstOrDefault();
        if (assessment is null)
        {
            return true;
        }

        return await db.UniLioAssessmentAttempts.AsNoTracking()
            .AnyAsync(
                a => a.PersonId == personId && a.AssessmentId == assessment.Id && a.Passed,
                cancellationToken);
    }
}
