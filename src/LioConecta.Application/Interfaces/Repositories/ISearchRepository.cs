using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface ISearchRepository
{
    Task<IReadOnlyList<Person>> SearchPeopleAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentMetadata>> SearchDocumentsAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Comunicado>> SearchComunicadosAsync(
        string query,
        int limit,
        Guid? viewerDepartmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> SearchGroupsAsync(
        string query,
        int limit,
        Guid viewerPersonId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PortalSystem>> SearchSystemsAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeedPost>> SearchFeedPostsAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UniLioCourse>> SearchUniLioCoursesAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PhoneExtension>> SearchRamaisAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarEvent>> SearchCalendarEventsAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookmarkCatalogItem>> SearchBookmarksAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<Guid?> GetPersonDepartmentIdAsync(Guid personId, CancellationToken cancellationToken = default);
}
