using LioConecta.Domain.Entities;

namespace LioConecta.Infrastructure.Services.UniLio;

internal static class UniLioCourseMetrics
{
    internal static decimal ResolveAvgRating(decimal catalogRating, IEnumerable<UniLioEnrollment> enrollments)
    {
        var rated = enrollments
            .Where(e => e.CourseContentRating is >= 1 and <= 5)
            .Select(e => e.CourseContentRating!.Value)
            .ToList();

        if (rated.Count > 0)
        {
            return Math.Round((decimal)rated.Average(), 1);
        }

        return catalogRating;
    }

    internal static DateTimeOffset? ResolvePublishedAt(UniLioCourse course)
    {
        if (course.PublishedAt is not null)
        {
            return course.PublishedAt;
        }

        if (IsPublishedStatus(course.Status))
        {
            return course.CreatedAt;
        }

        return null;
    }

    internal static bool IsPublishedStatus(string status) =>
        string.Equals(status, "published", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "active", StringComparison.OrdinalIgnoreCase);

    internal static (int EnrolledCount, int CompletedCount) CountEnrollments(IEnumerable<UniLioEnrollment> enrollments)
    {
        var list = enrollments as IReadOnlyList<UniLioEnrollment> ?? enrollments.ToList();
        return (
            list.Count,
            list.Count(e => string.Equals(e.Status, "completed", StringComparison.OrdinalIgnoreCase)));
    }
}
