using System.Net;
using System.Text.Json;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Mapping;

public static class UniLioFeedMapper
{
    public static FeedPost CreateCoursePublishedPost(UniLioCourse course, Guid authorId, DateTimeOffset now)
    {
        var href = $"/unilio/curso/{course.Id}";
        var title = WebUtility.HtmlEncode(course.Title);
        var excerpt = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(course.Description)
                ? "Novo curso disponível no UniLio."
                : course.Description.Trim());

        var content = $"""
            <span class="tag">Novo curso UniLio</span>
            <p style="margin-top:10px;"><strong>{title}</strong></p>
            <p>{excerpt}</p>
            <a class="announcement__cta" href="{href}">Acessar curso <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>
            """;

        var metadata = new Dictionary<string, object?>
        {
            ["courseId"] = course.Id.ToString(),
            ["title"] = course.Title,
            ["excerpt"] = course.Description,
            ["heroImageUrl"] = course.ThumbnailUrl,
            ["href"] = href,
            ["area"] = course.Area,
            ["durationMinutes"] = course.DurationMinutes,
            ["isMandatory"] = course.IsMandatory,
        };

        return new FeedPost
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Type = PostType.Comunicado,
            Content = content,
            MetadataJson = JsonSerializer.Serialize(metadata),
            IsPinned = course.IsMandatory,
            CreatedAt = course.PublishedAt ?? now,
            UpdatedAt = now,
        };
    }

    public static FeedPost CreateCourseCompletionPost(
        UniLioCourse course,
        Person learner,
        DateTimeOffset now)
    {
        var href = $"/unilio/curso/{course.Id}";
        var content = $"Concluí o curso **{course.Title}**!";

        var metadata = new Dictionary<string, object?>
        {
            ["kind"] = "unilio_course_completed",
            ["courseId"] = course.Id.ToString(),
            ["courseTitle"] = course.Title,
            ["heroImageUrl"] = course.ThumbnailUrl,
            ["href"] = href,
        };

        return new FeedPost
        {
            Id = Guid.NewGuid(),
            AuthorId = learner.Id,
            Type = PostType.Social,
            Content = content,
            MetadataJson = JsonSerializer.Serialize(metadata),
            IsPinned = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
