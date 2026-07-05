using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface ICurrentUserService
{
    Task<Guid> GetPersonIdAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserRole>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<ViewerContext> GetViewerContextAsync(Guid targetPersonId, CancellationToken cancellationToken = default);
}
