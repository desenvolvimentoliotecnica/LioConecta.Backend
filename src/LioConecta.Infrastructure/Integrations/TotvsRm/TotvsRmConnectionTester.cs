using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

public sealed class TotvsRmConnectionTester(ILogger<TotvsRmConnectionTester> logger)
{
    public async Task<TotvsRmConnectionTestResponse> TestAsync(
        TotvsRmRuntimeConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.Server))
        {
            return new TotvsRmConnectionTestResponse(false, "Servidor SQL nao informado.", null);
        }

        if (string.IsNullOrWhiteSpace(configuration.Database))
        {
            return new TotvsRmConnectionTestResponse(false, "Database nao informado.", null);
        }

        if (string.IsNullOrWhiteSpace(configuration.UserName))
        {
            return new TotvsRmConnectionTestResponse(false, "Usuario SQL nao informado.", null);
        }

        if (string.IsNullOrWhiteSpace(configuration.Password))
        {
            return new TotvsRmConnectionTestResponse(false, "Senha SQL nao informada.", null);
        }

        try
        {
            await using var connection = TotvsRmConnectionFactory.CreateConnection(configuration);
            await connection.OpenAsync(cancellationToken);

            await using var pingCommand = connection.CreateCommand();
            pingCommand.CommandText = "SELECT 1";
            pingCommand.CommandTimeout = 20;
            var ping = await pingCommand.ExecuteScalarAsync(cancellationToken);
            if (ping is null or not 1)
            {
                return new TotvsRmConnectionTestResponse(false, "Conexao estabelecida, mas a validacao basica falhou.", null);
            }

            var abatfunCount = await CountTableAsync(connection, TotvsRmConstants.PunchTableName, cancellationToken);
            var processedCount = await CountTableAsync(connection, TotvsRmConstants.ProcessedDayTableName, cancellationToken);
            var payrollCount = await CountTableAsync(connection, TotvsRmConstants.PayrollFinanceTableName, cancellationToken);

            var detail = $"Tabelas acessiveis: {TotvsRmConstants.PunchTableName} ({abatfunCount} registros), {TotvsRmConstants.ProcessedDayTableName} ({processedCount} registros), {TotvsRmConstants.PayrollFinanceTableName} ({payrollCount} registros). CodColigada fixa: {TotvsRmConstants.CodColigada}.";
            return new TotvsRmConnectionTestResponse(true, "Conexao com TOTVS RM realizada com sucesso.", detail);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Falha ao testar conexao TOTVS RM.");
            return new TotvsRmConnectionTestResponse(
                false,
                "Nao foi possivel conectar ao banco TOTVS RM.",
                exception.Message);
        }
    }

    private static async Task<long> CountTableAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT_BIG(1) FROM dbo.[{tableName}] WITH (NOLOCK)";
        command.CommandTimeout = 20;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result switch
        {
            long longValue => longValue,
            int intValue => intValue,
            _ => 0
        };
    }
}
