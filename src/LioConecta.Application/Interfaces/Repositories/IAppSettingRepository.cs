using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IAppSettingRepository
{
    Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AppSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task UpsertAsync(AppSetting setting, CancellationToken cancellationToken = default);

    Task UpsertManyAsync(IEnumerable<AppSetting> settings, CancellationToken cancellationToken = default);
}
