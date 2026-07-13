using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;
using Npgsql;

namespace LioConecta.Infrastructure.Integrations.CompassDatalake;

public interface ICompassDatalakeConnectionFactory
{
    bool IsConfigured { get; }

    string? ConfigurationMessage { get; }

    NpgsqlConnection? CreateConnection();
}

public sealed class CompassDatalakeConnectionFactory(IAppSettingsProvider settings) : ICompassDatalakeConnectionFactory
{
    private const int DefaultPort = 5432;
    private const string DefaultDatabase = "datalake";
    private const int ConnectionTimeoutSeconds = 15;

    public bool IsConfigured
    {
        get
        {
            var host = settings.GetString(AppSettingKeys.CompassDatalakeHost);
            var user = settings.GetString(AppSettingKeys.CompassDatalakeUsername);
            var password = settings.GetString(AppSettingKeys.CompassDatalakePassword);
            return !string.IsNullOrWhiteSpace(host)
                && !string.IsNullOrWhiteSpace(user)
                && !string.IsNullOrWhiteSpace(password);
        }
    }

    public string? ConfigurationMessage =>
        IsConfigured
            ? null
            : "Configure Host, Usuário e Senha do Datalake em Admin → Configurações do Backend → Compass IBP.";

    public NpgsqlConnection? CreateConnection()
    {
        if (!IsConfigured)
        {
            return null;
        }

        var host = settings.GetString(AppSettingKeys.CompassDatalakeHost).Trim();
        var user = settings.GetString(AppSettingKeys.CompassDatalakeUsername).Trim();
        var password = settings.GetString(AppSettingKeys.CompassDatalakePassword);
        var database = settings.GetString(AppSettingKeys.CompassDatalakeDatabase, DefaultDatabase).Trim();
        if (string.IsNullOrWhiteSpace(database))
        {
            database = DefaultDatabase;
        }

        var port = settings.GetInt(AppSettingKeys.CompassDatalakePort, DefaultPort);
        if (port <= 0)
        {
            port = DefaultPort;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = user,
            Password = password,
            Timeout = ConnectionTimeoutSeconds,
            CommandTimeout = 60,
            Pooling = true,
            MaxPoolSize = 8,
        };

        return new NpgsqlConnection(builder.ConnectionString);
    }
}
