using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class AppSettingRepository(AppDbContext db) : IAppSettingRepository
{
    public async Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.AppSettings
            .AsNoTracking()
            .OrderBy(s => s.Category)
            .ThenBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<AppSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await db.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }

    public async Task UpsertAsync(AppSetting setting, CancellationToken cancellationToken = default)
    {
        var existing = await db.AppSettings
            .FirstOrDefaultAsync(s => s.Key == setting.Key, cancellationToken);

        if (existing is null)
        {
            db.AppSettings.Add(setting);
        }
        else
        {
            existing.Value = setting.Value;
            existing.Label = setting.Label;
            existing.Description = setting.Description;
            existing.ValueType = setting.ValueType;
            existing.IsSecret = setting.IsSecret;
            existing.SortOrder = setting.SortOrder;
            existing.UpdatedById = setting.UpdatedById;
            existing.UpdatedAt = setting.UpdatedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertManyAsync(IEnumerable<AppSetting> settings, CancellationToken cancellationToken = default)
    {
        foreach (var setting in settings)
        {
            var existing = await db.AppSettings
                .FirstOrDefaultAsync(s => s.Key == setting.Key, cancellationToken);

            if (existing is null)
            {
                db.AppSettings.Add(setting);
            }
            else
            {
                existing.Value = setting.Value;
                existing.UpdatedById = setting.UpdatedById;
                existing.UpdatedAt = setting.UpdatedAt;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
