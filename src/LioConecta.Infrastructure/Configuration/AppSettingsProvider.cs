using System.Text.Json;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Infrastructure.Configuration;

public sealed class AppSettingsProvider : IAppSettingsProvider
{
    private Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public void Reload(IReadOnlyDictionary<string, string> values)
    {
        _values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
    }

    public string GetString(string key, string defaultValue = "")
    {
        return _values.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : defaultValue;
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        if (!_values.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        return bool.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (!_values.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        return int.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }

    public IReadOnlyList<string> GetStringArray(string key)
    {
        if (!_values.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(raw) ?? [];
        }
        catch
        {
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    public string GetConnectionString() =>
        GetString(Application.Common.AppSettingKeys.DatabaseDefaultConnection);

    public string GetRedisConnection() =>
        GetString(Application.Common.AppSettingKeys.RedisConnection);
}
