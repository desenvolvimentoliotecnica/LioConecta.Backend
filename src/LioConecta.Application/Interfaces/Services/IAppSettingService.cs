using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IAppSettingsProvider
{
    string GetString(string key, string defaultValue = "");

    bool TryGetString(string key, out string value);

    bool GetBool(string key, bool defaultValue = false);

    int GetInt(string key, int defaultValue = 0);

    IReadOnlyList<string> GetStringArray(string key);

    string GetConnectionString();

    string GetRedisConnection();

    void Reload(IReadOnlyDictionary<string, string> values);
}

public interface IAppSettingService
{
    Task<IReadOnlyList<AppSettingCategoryDto>> GetGroupedAsync(CancellationToken cancellationToken = default);

    Task<AppSettingsUpdateResultDto> BulkUpdateAsync(
        BulkUpdateAppSettingsRequest request,
        CancellationToken cancellationToken = default);
}
