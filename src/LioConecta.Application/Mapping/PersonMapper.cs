using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Mapping;

public static class JsonMapper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(json, Options) ?? [];
    }

    public static IReadOnlyDictionary<string, object?> DeserializeObjectDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object?>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, Options)
            ?? new Dictionary<string, object?>();
    }

    public static IReadOnlyDictionary<string, string>? DeserializeStringDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, Options);
    }

    public static string SerializeObjectDictionary(IReadOnlyDictionary<string, object?>? value)
        => JsonSerializer.Serialize(value ?? new Dictionary<string, object?>(), Options);

    public static string SerializeStringList(IReadOnlyList<string>? value)
        => JsonSerializer.Serialize(value ?? [], Options);
}

public static class PersonMapper
{
    public static PersonSummaryDto ToSummary(Person person)
        => new(
            person.Id,
            person.Slug,
            person.Name,
            person.Title,
            person.PhotoUrl,
            person.Department?.Name,
            person.Location,
            person.IsActive);

    public static MeDto ToMe(Person person, IReadOnlyList<UserRole> roles)
        => new(
            person.Id,
            person.Slug,
            person.Name,
            person.Email,
            person.Title,
            person.PhotoUrl,
            person.Department?.Name,
            roles);

    public static PersonProfileDto ToProfile(Person person, ViewerContext viewerContext)
    {
        var showSensitive = viewerContext is ViewerContext.Self or ViewerContext.HR or ViewerContext.Admin;

        return new PersonProfileDto(
            person.Id,
            person.Slug,
            person.Name,
            person.Title,
            showSensitive ? person.Email : MaskEmail(person.Email),
            showSensitive ? person.Phone : null,
            person.Location,
            person.PhotoUrl,
            person.Department?.Name,
            person.Manager?.Name,
            showSensitive ? person.BirthDate : null,
            person.HireDate,
            person.Status,
            JsonMapper.DeserializeStringList(person.TagsJson),
            JsonMapper.DeserializeStringList(person.SkillsJson),
            showSensitive ? JsonMapper.DeserializeStringDictionary(person.PersonalDataJson) : null,
            viewerContext);
    }

    public static OrgChartNodeDto ToOrgChartNode(Person person)
        => new(
            person.Id,
            person.OrgChartId,
            person.Slug,
            person.Name,
            person.Title,
            person.PhotoUrl,
            person.Department?.Name,
            person.ManagerId,
            JsonMapper.DeserializeStringList(person.TagsJson));

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        return $"{email[0]}***{email[atIndex..]}";
    }
}

public static class FeedMapper
{
    public static FeedPostDto ToDto(
        FeedPost post,
        Guid? viewerPersonId,
        bool includeComments = false)
    {
        var viewerReaction = viewerPersonId is null
            ? null
            : post.Reactions.FirstOrDefault(r => r.PersonId == viewerPersonId)?.ReactionType;

        var comments = includeComments
            ? post.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => ToCommentDto(c))
                .ToList()
            : [];

        return new FeedPostDto(
            post.Id,
            post.Type,
            post.Content,
            PersonMapper.ToSummary(post.Author ?? new Person { Name = "Desconhecido" }),
            post.CreatedAt,
            post.IsPinned,
            JsonMapper.DeserializeObjectDictionary(post.MetadataJson),
            post.Comments.Count,
            post.Reactions.Count,
            viewerReaction,
            comments);
    }

    public static CommentDto ToCommentDto(Comment comment)
        => new(
            comment.Id,
            comment.Text,
            PersonMapper.ToSummary(comment.Author ?? new Person { Name = "Desconhecido" }),
            comment.CreatedAt);
}

public static class ComunicadoMapper
{
    public static ComunicadoListItemDto ToListItem(Comunicado comunicado, bool isRead)
        => new(
            comunicado.Id,
            comunicado.Kind,
            comunicado.Title,
            comunicado.Excerpt,
            PersonMapper.ToSummary(comunicado.Author ?? new Person { Name = "Desconhecido" }),
            comunicado.HeroImageUrl,
            comunicado.IsMandatory,
            comunicado.PublishedAt,
            isRead);

    public static ComunicadoDto ToDto(Comunicado comunicado, bool isRead)
        => new(
            comunicado.Id,
            comunicado.Kind,
            comunicado.Title,
            comunicado.Excerpt,
            JsonMapper.DeserializeObjectDictionary(comunicado.ContentJson),
            PersonMapper.ToSummary(comunicado.Author ?? new Person { Name = "Desconhecido" }),
            comunicado.HeroImageUrl,
            comunicado.IsMandatory,
            comunicado.PublishedAt,
            isRead);
}

public static class DocumentMapper
{
    public static DocumentDto ToDto(DocumentMetadata document)
        => new(
            document.Id,
            document.Title,
            document.Category,
            document.SharePointUrl,
            document.ModifiedAt);
}

public static class ServiceRequestMapper
{
    public static ServiceRequestDto ToDto(ServiceRequest request)
        => new(
            request.Id,
            request.Type,
            request.Category,
            request.Status,
            PersonMapper.ToSummary(request.Requester ?? new Person { Name = "Desconhecido" }),
            JsonMapper.DeserializeObjectDictionary(request.PayloadJson),
            request.AssigneeTeam,
            request.ExternalRef,
            request.CreatedAt,
            request.UpdatedAt,
            request.Events
                .OrderBy(e => e.CreatedAt)
                .Select(ToEventDto)
                .ToList());

    public static ServiceRequestEventDto ToEventDto(ServiceRequestEvent serviceRequestEvent)
        => new(
            serviceRequestEvent.Id,
            serviceRequestEvent.EventType,
            serviceRequestEvent.Actor is null ? null : PersonMapper.ToSummary(serviceRequestEvent.Actor),
            serviceRequestEvent.CreatedAt,
            JsonMapper.DeserializeObjectDictionary(serviceRequestEvent.DetailsJson));
}

public static class NotificationMapper
{
    public static NotificationDto ToDto(Notification notification)
        => new(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Body,
            notification.Href,
            notification.IsRead,
            notification.CreatedAt);
}

public static class GroupMapper
{
    public static GroupDto ToDto(Group group, bool isMember)
        => new(
            group.Id,
            group.Name,
            group.Description,
            group.IsPrivate,
            PersonMapper.ToSummary(group.Owner ?? new Person { Name = "Desconhecido" }),
            group.Members.Count,
            isMember,
            group.CreatedAt);
}

public static class CalendarMapper
{
    public static CalendarEventDto ToDto(CalendarEvent calendarEvent)
        => new(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.StartAt,
            calendarEvent.EndAt,
            calendarEvent.Location,
            calendarEvent.Source);

    public static CafeteriaMenuDto ToDto(CafeteriaMenu menu)
        => new(menu.Date, JsonMapper.DeserializeStringList(menu.ItemsJson));
}
