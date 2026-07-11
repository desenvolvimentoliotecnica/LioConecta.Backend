namespace LioConecta.Application.Interfaces.Services;

public interface INewHireAnnouncementService
{
    Task<int> AnnounceRecentHiresAsync(CancellationToken cancellationToken = default);
}
