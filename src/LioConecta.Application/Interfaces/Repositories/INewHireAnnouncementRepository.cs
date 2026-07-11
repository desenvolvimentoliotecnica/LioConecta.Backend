using LioConecta.Domain.Entities;
namespace LioConecta.Application.Interfaces.Repositories;
public interface INewHireAnnouncementRepository
{
 Task<IReadOnlyList<Person>> GetUnannouncedRecentHiresAsync(DateOnly from, CancellationToken cancellationToken = default);
 Task AddAsync(FeedPost post, NewHireAnnouncement announcement, CancellationToken cancellationToken = default);
}
