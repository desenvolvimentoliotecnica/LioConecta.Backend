using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IFeedbackService
{
    Task<FeedbackSubmissionDto> CreateAsync(CreateFeedbackRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeedbackSubmissionDto>> ListAsync(FeedbackStatus? status, CancellationToken cancellationToken = default);
    Task<FeedbackSubmissionDto> UpdateAsync(Guid id, UpdateFeedbackRequest request, CancellationToken cancellationToken = default);
}
