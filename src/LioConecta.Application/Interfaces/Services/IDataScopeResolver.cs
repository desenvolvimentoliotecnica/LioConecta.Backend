using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IDataScopeResolver
{
    Task<IReadOnlyList<Guid>> ResolveVisiblePersonIdsAsync(DataScope scope, CancellationToken cancellationToken = default);

    Task<bool> CanAccessPersonAsync(Guid targetPersonId, DataScope scope, CancellationToken cancellationToken = default);
}
