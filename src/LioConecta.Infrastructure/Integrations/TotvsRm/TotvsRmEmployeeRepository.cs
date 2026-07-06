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
                LTRIM(RTRIM(F.CODSECAO)) AS CodSecao,
                LTRIM(RTRIM(S.DESCRICAO)) AS SecaoDescricao,
                LTRIM(RTRIM(F.CODFUNCAO)) AS CodFuncao,
                LTRIM(RTRIM(FN.NOME)) AS FuncaoDescricao,
                F.DATAADMISSAO AS DataAdmissao,
                LTRIM(RTRIM(P.CPF)) AS Cpf,
                LTRIM(RTRIM(P.CARTIDENTIDADE)) AS Rg,
                LTRIM(RTRIM(P.TELEFONE1)) AS Telefone,
                LTRIM(RTRIM(P.EMAIL)) AS EmailPessoal,
                LTRIM(RTRIM(P.CIDADE)) AS Cidade,
                LTRIM(RTRIM(P.ESTADO)) AS Estado,
                LTRIM(RTRIM(P.RUA)) AS Endereco,
                LTRIM(RTRIM(G.NOME)) AS GestorNome,
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
            LEFT JOIN dbo.PFUNC G WITH (NOLOCK)
                ON G.CODCOLIGADA = S.CODCOLIGADA AND G.CHAPA = S.CHAPACHEFE
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

    public async Task<IReadOnlyList<RmEmployeeAdmissionRecord>> GetActiveAdmissionsAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                LTRIM(RTRIM(F.CHAPA)) AS Chapa,
                F.DATAADMISSAO AS DataAdmissao,
                P.DTNASCIMENTO AS DataNascimento,
                LTRIM(RTRIM(P.EMAIL)) AS EmailPessoal
            FROM dbo.PFUNC F WITH (NOLOCK)
            LEFT JOIN dbo.PPESSOA P WITH (NOLOCK)
                ON P.CODIGO = F.CODPESSOA
            WHERE F.CODCOLIGADA = @CodColigada
              AND (F.CODSITUACAO IS NULL OR F.CODSITUACAO = 'A')
              AND (F.DATAADMISSAO IS NOT NULL OR P.DTNASCIMENTO IS NOT NULL);
            """;

        try
        {
            var rows = await QueryListAsync(
                "PFUNC admissions",
                cancellationToken,
                connection => connection.QueryAsync<RmEmployeeAdmissionRecord>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                }));

            return rows?.ToList() ?? [];
        }
        catch (TotvsRmIntegrationException)
        {
            return [];
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

    private async Task<IEnumerable<T>?> QueryListAsync<T>(
        string operationLabel,
        CancellationToken cancellationToken,
        Func<SqlConnection, Task<IEnumerable<T>>> queryFactory)
    {
        var runtime = await configurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled || string.IsNullOrWhiteSpace(runtime.Password))
        {
            return null;
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
                "Falha ao consultar TOTVS RM ({OperationLabel}).",
                operationLabel);

            return null;
        }
    }
}
