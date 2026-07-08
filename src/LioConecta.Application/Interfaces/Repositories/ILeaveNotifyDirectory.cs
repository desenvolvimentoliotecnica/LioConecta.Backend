using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface ILeaveNotifyDirectory
{
    Task<IReadOnlyList<Person>> FindActivePeopleByPortalRolesAsync(
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default);
}
