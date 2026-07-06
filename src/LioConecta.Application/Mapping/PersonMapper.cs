using System.Text.Json;
using LioConecta.Application.Common;
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

    public static string SerializeSkills(IReadOnlyList<PersonSkillDto> skills)
        => JsonSerializer.Serialize(
            skills.Select(skill => new
            {
                name = skill.Name,
                level = skill.Level,
                endorsements = skill.Endorsements,
            }),
            Options);

    public static IReadOnlyList<PersonSkillDto> DeserializeSkills(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var skills = new List<PersonSkillDto>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var name = item.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        skills.Add(new PersonSkillDto(name, 3, 0));
                    }

                    continue;
                }

                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var skillName = item.TryGetProperty("name", out var nameProp)
                    ? nameProp.GetString()
                    : item.TryGetProperty("Name", out var altNameProp)
                        ? altNameProp.GetString()
                        : null;

                if (string.IsNullOrWhiteSpace(skillName))
                {
                    continue;
                }

                var level = item.TryGetProperty("level", out var levelProp)
                    ? levelProp.GetInt32()
                    : item.TryGetProperty("Level", out var altLevelProp)
                        ? altLevelProp.GetInt32()
                        : 3;

                var endorsements = item.TryGetProperty("endorsements", out var endorsementsProp)
                    ? endorsementsProp.GetInt32()
                    : item.TryGetProperty("Endorsements", out var altEndorsementsProp)
                        ? altEndorsementsProp.GetInt32()
                        : 0;

                skills.Add(new PersonSkillDto(skillName, level, endorsements));
            }

            return skills;
        }
        catch (JsonException)
        {
            return DeserializeStringList(json)
                .Select(name => new PersonSkillDto(name, 3, 0))
                .ToList();
        }
    }
}

public static class PersonMapper
{
    public static PersonSummaryDto ToSummary(Person person, DateOnly? birthDate = null, DateOnly? hireDate = null)
        => new(
            person.Id,
            person.Slug,
            person.Name,
            person.Title,
            person.PhotoUrl,
            PersonDepartmentHelper.GetName(person),
            person.Location,
            person.Manager?.Slug,
            person.IsActive,
            birthDate ?? person.BirthDate,
            hireDate ?? person.HireDate);

    public static PersonDirectoryEntryDto ToDirectoryEntry(Person person)
        => new(
            person.Id,
            person.Slug,
            person.Name,
            person.Title,
            person.PhotoUrl,
            person.Email,
            person.TeamsUpn,
            PersonDepartmentHelper.GetName(person),
            person.Location,
            person.Manager?.Slug,
            person.IsActive,
            person.HireDate,
            person.Phone);

    public static MeDto ToMe(Person person, IReadOnlyList<UserRole> roles)
        => new(
            person.Id,
            person.Slug,
            person.Name,
            person.Email,
            person.Title,
            person.PhotoUrl,
            PersonDepartmentHelper.GetName(person),
            roles);

    public static PersonProfileDto ToProfile(Person person, ViewerContext viewerContext)
    {
        var showSensitive = viewerContext is ViewerContext.Self or ViewerContext.HR or ViewerContext.Admin;
        var personalData = showSensitive
            ? JsonMapper.DeserializeObjectDictionary(person.PersonalDataJson)
            : null;

        string? ReadPersonalString(string key)
        {
            if (personalData is null || !personalData.TryGetValue(key, out var value) || value is null)
            {
                return null;
            }

            return value switch
            {
                string text => text,
                JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
                _ => value.ToString()
            };
        }

        return new PersonProfileDto(
            person.Id,
            person.Slug,
            person.OrgChartId,
            person.Name,
            person.Title,
            showSensitive ? person.Email : MaskEmail(person.Email),
            showSensitive ? person.Phone : null,
            person.Location,
            person.PhotoUrl,
            PersonDepartmentHelper.GetName(person),
            person.Manager?.Name,
            person.Manager?.Slug,
            person.TeamsUpn,
            ReadPersonalString("aboutMe") ?? ReadPersonalString("bio"),
            ReadPersonalString("pronouns"),
            showSensitive ? person.BirthDate : null,
            person.HireDate,
            person.Status,
            JsonMapper.DeserializeStringList(person.TagsJson),
            JsonMapper.DeserializeSkills(person.SkillsJson),
            personalData,
            viewerContext);
    }

    public static OrgChartNodeDto ToOrgChartNode(Person person, bool isOrphan = false)
        => new(
            person.Id,
            person.OrgChartId,
            person.Slug,
            person.Name,
            person.Title,
            ResolvePhotoUrl(person),
            PersonDepartmentHelper.GetName(person),
            person.ManagerId,
            JsonMapper.DeserializeStringList(person.TagsJson),
            isOrphan,
            person.Email,
            person.TeamsUpn,
            person.Phone,
            person.Location,
            person.HireDate);

    public static PersonHierarchyMemberDto ToHierarchyMember(Person person)
        => new(
            person.Slug,
            person.Name,
            person.Title,
            ResolvePhotoUrl(person),
            PersonDepartmentHelper.GetName(person));

    private static string? ResolvePhotoUrl(Person person) =>
        string.IsNullOrWhiteSpace(person.PhotoUrl) ? null : person.PhotoUrl.Trim();

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
        bool includeComments = false,
        PollDto? poll = null)
    {
        var viewerReaction = viewerPersonId is null
            ? null
            : post.Reactions.FirstOrDefault(r => r.PersonId == viewerPersonId)?.ReactionType;

        var comments = includeComments
            ? post.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(ToCommentDto)
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
            comments,
            poll);
    }

    public static PollDto ToPollDto(Poll poll, Guid viewerPersonId)
    {
        var hasVoted = poll.Options.Any(o => o.Votes.Any(v => v.PersonId == viewerPersonId));
        var options = poll.Options
            .OrderBy(o => o.SortOrder)
            .Select(o => new PollOptionDto(
                o.Id,
                o.Text,
                o.Votes.Count,
                o.SortOrder,
                hasVoted && o.Votes.Any(v => v.PersonId == viewerPersonId)))
            .ToList();

        return new PollDto(poll.Id, poll.PostId, poll.Question, poll.EndsAt, hasVoted, options);
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
            comunicado.Slug,
            comunicado.Kind,
            comunicado.Title,
            comunicado.Excerpt,
            PersonMapper.ToSummary(comunicado.Author ?? new Person { Name = "Desconhecido" }),
            comunicado.HeroImageUrl,
            comunicado.IsMandatory,
            comunicado.PublishedAt,
            comunicado.ArchivedAt,
            isRead);

    public static ComunicadoDto ToDto(Comunicado comunicado, bool isRead)
        => new(
            comunicado.Id,
            comunicado.Slug,
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
            group.Type,
            group.AccessMode,
            group.Icon,
            group.Status,
            group.IsPrivate,
            PersonMapper.ToSummary(group.Owner ?? new Person { Name = "Desconhecido" }),
            group.Members.Count,
            group.Posts.Count,
            isMember,
            group.CreatedAt,
            group.ReviewedAt,
            group.RejectionReason);
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
