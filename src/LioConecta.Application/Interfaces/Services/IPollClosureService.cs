namespace LioConecta.Application.Interfaces.Services;

public interface IPollClosureService
{
    Task ProcessClosedPollsAsync(CancellationToken cancellationToken = default);
}
