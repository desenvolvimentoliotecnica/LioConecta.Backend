using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class DataScopeResolver(
    ICurrentUserService currentUserService,
    AppDbContext db) : IDataScopeResolver
{
    public async Task<IReadOnlyList<Guid>> ResolveVisiblePersonIdsAsync(DataScope scope, CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);

        return scope switch
        {
            DataScope.Self => [personId],
            DataScope.Team =>
            [
                .. await db.People.AsNoTracking()
                    .Where(p => p.IsActive && p.ManagerId == personId)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken),
                personId,
            ],
            DataScope.Department => await ResolveDepartmentPersonIdsAsync(personId, cancellationToken),
            DataScope.Global => [],
            _ => [personId],
        };
    }

    public async Task<bool> CanAccessPersonAsync(Guid targetPersonId, DataScope scope, CancellationToken cancellationToken = default)
    {
        if (scope == DataScope.Global)
        {
            return true;
        }

        var visible = await ResolveVisiblePersonIdsAsync(scope, cancellationToken);
        return scope == DataScope.Global || visible.Contains(targetPersonId);
    }

    private async Task<IReadOnlyList<Guid>> ResolveDepartmentPersonIdsAsync(Guid personId, CancellationToken cancellationToken)
    {
        var dept = await db.People.AsNoTracking()
            .Where(p => p.Id == personId)
            .Select(p => p.Dept ?? (p.Department != null ? p.Department.Name : null))
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(dept))
        {
            return [personId];
        }

        return await db.People.AsNoTracking()
            .Where(p => p.IsActive && (p.Dept == dept || (p.Department != null && p.Department.Name == dept)))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
    }
}
