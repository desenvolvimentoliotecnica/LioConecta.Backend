using LioConecta.Application.DTOs;
using Microsoft.Data.SqlClient;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

public static class TotvsRmConnectionFactory
{
    public static SqlConnection CreateConnection(TotvsRmRuntimeConfiguration configuration)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = BuildDataSource(configuration.Server, configuration.Port),
            InitialCatalog = configuration.Database.Trim(),
            UserID = configuration.UserName.Trim(),
            Password = configuration.Password,
            TrustServerCertificate = configuration.TrustServerCertificate,
            Encrypt = configuration.TrustServerCertificate,
            ConnectTimeout = 20
        };

        return new SqlConnection(builder.ConnectionString);
    }

    private static string BuildDataSource(string server, int port)
    {
        var normalizedServer = server.Trim();
        if (normalizedServer.Contains(',', StringComparison.Ordinal))
        {
            return normalizedServer;
        }

        return port is > 0 and not 1433
            ? $"{normalizedServer},{port}"
            : normalizedServer;
    }
}
