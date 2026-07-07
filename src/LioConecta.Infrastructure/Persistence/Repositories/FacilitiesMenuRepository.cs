using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class FacilitiesMenuRepository(AppDbContext db) : IFacilitiesMenuRepository
{
    public async Task<CafeteriaMenu?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
        => await db.CafeteriaMenus
            .AsNoTracking()
            .Include(m => m.UpdatedBy)
            .FirstOrDefaultAsync(m => m.Date == date, cancellationToken);

    public async Task<IReadOnlyList<CafeteriaMenu>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
        => await db.CafeteriaMenus
            .AsNoTracking()
            .Include(m => m.UpdatedBy)
            .Where(m => m.Date >= from && m.Date <= to)
            .OrderBy(m => m.Date)
            .ToListAsync(cancellationToken);

    public async Task<CafeteriaMenu> UpsertAsync(CafeteriaMenu menu, CancellationToken cancellationToken = default)
    {
        var existing = await db.CafeteriaMenus.FirstOrDefaultAsync(m => m.Date == menu.Date, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            menu.Id = menu.Id == Guid.Empty ? Guid.NewGuid() : menu.Id;
            menu.CreatedAt = now;
            menu.UpdatedAt = now;
            db.CafeteriaMenus.Add(menu);
        }
        else
        {
            existing.PayloadJson = menu.PayloadJson;
            existing.ItemsJson = menu.ItemsJson;
            existing.Published = menu.Published;
            existing.UpdatedById = menu.UpdatedById;
            existing.UpdatedAt = now;
            menu = existing;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await db.CafeteriaMenus
            .AsNoTracking()
            .Include(m => m.UpdatedBy)
            .FirstAsync(m => m.Id == menu.Id, cancellationToken);
    }

    public async Task DeleteAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var existing = await db.CafeteriaMenus.FirstOrDefaultAsync(m => m.Date == date, cancellationToken);
        if (existing is null)
        {
            return;
        }

        db.CafeteriaMenus.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
    }
}
