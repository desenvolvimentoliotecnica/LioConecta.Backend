using Dapper;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

public sealed class TotvsRmEmployeeRepository(
    ITotvsRmConfigurationService configurationService,
    ILogger<TotvsRmEmployeeRepository> logger) : ITotvsRmEmployeeRepository
{
    public async Task<RmEmployeeProfileRecord?> GetProfileByChapaAsync(
        string chapa,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP 1
                LTRIM(RTRIM(F.CHAPA)) AS Chapa,
                F.CODPESSOA AS CodPessoa,
                LTRIM(RTRIM(F.NOME)) AS Nome,
                LTRIM(RTRIM(S.DESCRICAO)) AS SecaoDescricao,
                LTRIM(RTRIM(FN.NOME)) AS FuncaoDescricao,
                F.DATAADMISSAO AS DataAdmissao,
                LTRIM(RTRIM(P.CPF)) AS Cpf,
                LTRIM(RTRIM(F.CODBANCOPAGTO)) AS Banco,
                LTRIM(RTRIM(F.CODAGENCIAPAGTO)) AS Agencia,
                LTRIM(RTRIM(F.CONTAPAGAMENTO)) AS Conta
            FROM dbo.PFUNC F WITH (NOLOCK)
            LEFT JOIN dbo.PPESSOA P WITH (NOLOCK)
                ON P.CODIGO = F.CODPESSOA
            LEFT JOIN dbo.PSECAO S WITH (NOLOCK)
                ON S.CODCOLIGADA = F.CODCOLIGADA AND S.CODIGO = F.CODSECAO
            LEFT JOIN dbo.PFUNCAO FN WITH (NOLOCK)
                ON FN.CODCOLIGADA = F.CODCOLIGADA AND FN.CODIGO = F.CODFUNCAO
            WHERE F.CODCOLIGADA = @CodColigada
              AND F.CHAPA = @Chapa
              AND (F.CODSITUACAO IS NULL OR F.CODSITUACAO = 'A');
            """;

        try
        {
            return await QueryAsync(
                "PFUNC profile",
                chapa,
                cancellationToken,
                connection => connection.QueryFirstOrDefaultAsync<RmEmployeeProfileRecord>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa
                }));
        }
        catch (TotvsRmIntegrationException)
        {
            return null;
        }
    }

    private async Task<T?> QueryAsync<T>(
        string operationLabel,
        string chapa,
        CancellationToken cancellationToken,
        Func<SqlConnection, Task<T?>> queryFactory)
    {
        var runtime = await configurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled || string.IsNullOrWhiteSpace(runtime.Password))
        {
            return default;
        }

        try
        {
            await using var connection = TotvsRmConnectionFactory.CreateConnection(runtime);
            await connection.OpenAsync(cancellationToken);
            return await queryFactory(connection);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Falha ao consultar TOTVS RM ({OperationLabel}) para CHAPA {Chapa}.",
                operationLabel,
                chapa);

            return default;
        }
    }
}
