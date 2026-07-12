using LioConecta.Application.DTOs;

using LioConecta.Application.Interfaces.Repositories;

using LioConecta.Application.Interfaces.Services;

using LioConecta.Application.Mapping;

using LioConecta.Domain.Entities;



namespace LioConecta.Application.Services;



public sealed class SearchService(

    ISearchRepository searchRepository,

    ICurrentUserService currentUserService,

    IPermissionService permissionService,

    IHelpDeskService helpDeskService) : ISearchService

{

    private static readonly HashSet<string> KnownTypes = new(StringComparer.OrdinalIgnoreCase)

    {

        "people",

        "documents",

        "comunicados",

        "groups",

        "systems",

        "feed",

        "unilio",

        "ramais",

        "knowledge",

        "calendar",

        "bookmarks",

    };



    private static readonly SearchResultDto EmptyResult = new(

        [], [], [], [], [], [], [], [], [], [], []);



    public async Task<SearchResultDto> SearchAsync(

        string query,

        int limit = 20,

        string? types = null,

        CancellationToken cancellationToken = default)

    {

        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)

        {

            return EmptyResult;

        }



        var selected = ParseTypes(types);

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);

        var departmentId = await searchRepository.GetPersonDepartmentIdAsync(personId, cancellationToken);



        // Sequential queries: shared scoped DbContext cannot run concurrent operations.

        IReadOnlyList<Person> peopleEntities = Wants(selected, "people")

            ? await searchRepository.SearchPeopleAsync(query, limit, cancellationToken)

            : [];

        IReadOnlyList<DocumentMetadata> documentEntities = Wants(selected, "documents")

            ? await searchRepository.SearchDocumentsAsync(query, limit, cancellationToken)

            : [];

        IReadOnlyList<Comunicado> comunicadoEntities = Wants(selected, "comunicados")

            ? await searchRepository.SearchComunicadosAsync(query, limit, departmentId, cancellationToken)

            : [];

        IReadOnlyList<Group> groupEntities = Wants(selected, "groups")

            ? await searchRepository.SearchGroupsAsync(query, limit, personId, cancellationToken)

            : [];

        IReadOnlyList<PortalSystem> systemEntities = Wants(selected, "systems")

            ? await searchRepository.SearchSystemsAsync(query, limit, cancellationToken)

            : [];

        IReadOnlyList<FeedPost> feedEntities = Wants(selected, "feed")

            ? await searchRepository.SearchFeedPostsAsync(query, limit, cancellationToken)

            : [];



        IReadOnlyList<UniLioCourse> unilioEntities = [];

        if (Wants(selected, "unilio")

            && await permissionService.HasPermissionAsync("unilio.access", cancellationToken: cancellationToken))

        {

            unilioEntities = await searchRepository.SearchUniLioCoursesAsync(query, limit, cancellationToken);

        }



        IReadOnlyList<PhoneExtension> ramalEntities = Wants(selected, "ramais")

            ? await searchRepository.SearchRamaisAsync(query, limit, cancellationToken)

            : [];

        IReadOnlyList<CalendarEvent> calendarEntities = Wants(selected, "calendar")

            ? await searchRepository.SearchCalendarEventsAsync(query, limit, cancellationToken)

            : [];

        IReadOnlyList<BookmarkCatalogItem> bookmarkEntities = Wants(selected, "bookmarks")

            ? await searchRepository.SearchBookmarksAsync(query, limit, cancellationToken)

            : [];



        IReadOnlyList<SearchKnowledgeHitDto> knowledge = [];

        if (Wants(selected, "knowledge"))

        {

            knowledge = (await helpDeskService.GetKnowledgeAsync(query, cancellationToken))

                .Take(Math.Clamp(limit, 1, 100))

                .Select(a => new SearchKnowledgeHitDto(a.Id, a.Title, a.Summary, a.Category, a.Url))

                .ToList();

        }



        return new SearchResultDto(

            peopleEntities.Select(p => PersonMapper.ToSummary(p)).ToList(),

            documentEntities.Select(DocumentMapper.ToDto).ToList(),

            comunicadoEntities.Select(c => ComunicadoMapper.ToListItem(c, isRead: false)).ToList(),

            groupEntities

                .Select(g => GroupMapper.ToDto(g, isMember: g.Members.Any(m => m.PersonId == personId)))

                .ToList(),

            systemEntities.Select(ToSystemHit).ToList(),

            feedEntities.Select(ToFeedHit).ToList(),

            unilioEntities.Select(ToUniLioHit).ToList(),

            ramalEntities.Select(ToRamalHit).ToList(),

            knowledge,

            calendarEntities.Select(ToCalendarHit).ToList(),

            bookmarkEntities.Select(ToBookmarkHit).ToList());

    }



    private static HashSet<string>? ParseTypes(string? types)

    {

        if (string.IsNullOrWhiteSpace(types))

        {

            return null;

        }



        var parsed = types

            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)

            .Where(KnownTypes.Contains)

            .ToHashSet(StringComparer.OrdinalIgnoreCase);



        return parsed.Count == 0 ? null : parsed;

    }



    private static bool Wants(HashSet<string>? selected, string type) =>

        selected is null || selected.Contains(type);



    private static SearchSystemHitDto ToSystemHit(PortalSystem system) =>

        new(system.Id, system.Name, system.Slug, system.Description, system.Category);



    private static SearchFeedHitDto ToFeedHit(FeedPost post)

    {

        var preview = post.Content.Length <= 160

            ? post.Content

            : post.Content[..160].TrimEnd() + "…";

        return new SearchFeedHitDto(post.Id, preview, post.Author?.Name, post.CreatedAt);

    }



    private static SearchUniLioHitDto ToUniLioHit(UniLioCourse course) =>

        new(course.Id, course.Title, Truncate(course.Description, 160), course.InstructorName, course.Area);



    private static SearchRamalHitDto ToRamalHit(PhoneExtension ramal) =>

        new(ramal.Id, ramal.Name, ramal.Extension, ramal.Department, ramal.Title, ramal.Email);



    private static SearchCalendarHitDto ToCalendarHit(CalendarEvent evt) =>

        new(evt.Id, evt.Title, evt.Location, evt.StartAt, evt.EndAt);



    private static SearchBookmarkHitDto ToBookmarkHit(BookmarkCatalogItem item) =>

        new(item.Id, item.Title, item.Excerpt, item.Href, item.Kind);



    private static string? Truncate(string? value, int maxLength)

    {

        if (string.IsNullOrEmpty(value))

        {

            return value;

        }



        return value.Length <= maxLength

            ? value

            : value[..maxLength].TrimEnd() + "…";

    }

}


