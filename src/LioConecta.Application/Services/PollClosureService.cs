using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Application.Services;

public sealed class PollClosureService(
    IFeedRepository feedRepository,
    INotificationService notificationService) : IPollClosureService
{
    public async Task ProcessClosedPollsAsync(CancellationToken cancellationToken = default)
    {
        var polls = await feedRepository.GetPollsPendingClosureNotificationAsync(cancellationToken);
        if (polls.Count == 0)
        {
            return;
        }

        foreach (var poll in polls)
        {
            var post = poll.Post ?? await feedRepository.GetByIdAsync(poll.PostId, cancellationToken);
            if (post is null)
            {
                await feedRepository.TryMarkPollClosureNotifiedAsync(poll.Id, cancellationToken);
                continue;
            }

            var claimed = await feedRepository.TryMarkPollClosureNotifiedAsync(poll.Id, cancellationToken);
            if (!claimed)
            {
                continue;
            }

            await notificationService.NotifyPollClosedAsync(post, poll, cancellationToken);
        }
    }
}
