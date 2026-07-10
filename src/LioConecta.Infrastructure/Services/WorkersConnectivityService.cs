using System.Diagnostics;
using LioConecta.Application.Common;
using LioConecta.Application.Common.DbExplorer;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Integrations.TotvsRm;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace LioConecta.Infrastructure.Services;

public sealed class WorkersConnectivityService(
    IEnumerable<IDbExplorerProvider> providers,
    IAppSettingsProvider settings,
    ITotvsRmConfigurationService totvsRmConfigurationService,
    IServiceProvider serviceProvider,
    ILogger<WorkersConnectivityService> logger) : IWorkersConnectivityService
{
    private readonly IReadOnlyDictionary<string, IDbExplorerProvider> _providers =
        providers.ToDictionary(p => p.ConnectionId, StringComparer.OrdinalIgnoreCase);

    public async Task<WorkerConnectivityDto> CheckAsync(CancellationToken cancellationToken)
    {
        var api = CheckApi();
        var postgres = await CheckPostgresAsync(cancellationToken);
        var redis = await CheckRedisAsync(cancellationToken);
        var totvsRm = await CheckTotvsRmAsync(cancellationToken);

        return new WorkerConnectivityDto(
            DateTimeOffset.UtcNow,
            [api, postgres, redis, totvsRm]);
    }

    private static WorkerConnectivityComponentDto CheckApi()
    {
        var sw = Stopwatch.StartNew();
        sw.Stop();
        return new WorkerConnectivityComponentDto(
            WorkerConnectivityIds.Api,
            "API",
            true,
            sw.ElapsedMilliseconds,
            null);
    }

    private async Task<WorkerConnectivityComponentDto> CheckPostgresAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (!_providers.TryGetValue(DbExplorerCatalog.PostgresConnectionId, out var provider))
            {
                return Unhealthy(WorkerConnectivityIds.Postgres, "Portal DB", "Provider PostgreSQL não registrado.");
            }

            var ok = await provider.TestConnectionAsync(cancellationToken);
            sw.Stop();
            return ok
                ? Healthy(WorkerConnectivityIds.Postgres, "Portal DB", sw.ElapsedMilliseconds)
                : Unhealthy(WorkerConnectivityIds.Postgres, "Portal DB", "Conexão indisponível", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Portal DB connectivity check failed.");
            return Unhealthy(WorkerConnectivityIds.Postgres, "Portal DB", ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private async Task<WorkerConnectivityComponentDto> CheckRedisAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var redisConnection = settings.GetRedisConnection();
            if (string.IsNullOrWhiteSpace(redisConnection))
            {
                return Unhealthy(WorkerConnectivityIds.Redis, "Redis", "Connection string Redis não configurada.");
            }

            var multiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
            if (multiplexer is null || !multiplexer.IsConnected)
            {
                await using var temp = await ConnectionMultiplexer.ConnectAsync(redisConnection);
                var pong = await temp.GetDatabase().PingAsync();
                sw.Stop();
                _ = cancellationToken;
                return Healthy(WorkerConnectivityIds.Redis, "Redis", (long)pong.TotalMilliseconds);
            }

            var latency = await multiplexer.GetDatabase().PingAsync();
            sw.Stop();
            return Healthy(WorkerConnectivityIds.Redis, "Redis", (long)latency.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Redis connectivity check failed.");
            return Unhealthy(WorkerConnectivityIds.Redis, "Redis", ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private async Task<WorkerConnectivityComponentDto> CheckTotvsRmAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
            if (!runtime.IsEnabled)
            {
                return Unhealthy(WorkerConnectivityIds.TotvsRm, "TOTVS RM", "Integração TOTVS RM desabilitada.");
            }

            if (string.IsNullOrWhiteSpace(runtime.Server)
                || string.IsNullOrWhiteSpace(runtime.Database)
                || string.IsNullOrWhiteSpace(runtime.UserName))
            {
                return Unhealthy(WorkerConnectivityIds.TotvsRm, "TOTVS RM", "Configuração TOTVS RM incompleta.");
            }

            await using var connection = TotvsRmConnectionFactory.CreateConnection(runtime);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand("SELECT 1", connection) { CommandTimeout = 10 };
            _ = await command.ExecuteScalarAsync(cancellationToken);
            sw.Stop();
            return Healthy(WorkerConnectivityIds.TotvsRm, "TOTVS RM", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "TOTVS RM connectivity check failed.");
            return Unhealthy(WorkerConnectivityIds.TotvsRm, "TOTVS RM", "Conexão indisponível", sw.ElapsedMilliseconds);
        }
    }

    private static WorkerConnectivityComponentDto Healthy(string id, string label, long latencyMs) =>
        new(id, label, true, latencyMs, null);

    private static WorkerConnectivityComponentDto Unhealthy(
        string id,
        string label,
        string message,
        long? latencyMs = null) =>
        new(id, label, false, latencyMs, message);
}
