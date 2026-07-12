using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IFeedbackRepository
{
    Task AddAsync(FeedbackSubmission feedback, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeedbackSubmission>> ListRhChannelAsync(
        FeedbackStatus? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeedbackSubmission>> ListVisibleToPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<FeedbackSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
