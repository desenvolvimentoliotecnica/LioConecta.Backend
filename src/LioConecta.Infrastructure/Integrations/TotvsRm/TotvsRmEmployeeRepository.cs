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

    public async Task<RmEmployeeCareerHistoryData> GetCareerHistoryByChapaAsync(
        string chapa,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await QueryAsync(
                "career history",
                chapa,
                cancellationToken,
                async connection =>
                {
                    var functionHistory = await QueryFunctionHistoryAsync(connection, chapa, cancellationToken);
                    var sectionHistory = await QuerySectionHistoryAsync(connection, chapa, cancellationToken);
                    var salaryHistory = await QuerySalaryHistoryAsync(connection, chapa, cancellationToken);

                    return new RmEmployeeCareerHistoryData
                    {
                        FunctionHistory = functionHistory,
                        SectionHistory = sectionHistory,
                        SalaryHistory = salaryHistory,
                    };
                });

            return result ?? new RmEmployeeCareerHistoryData();
        }
        catch (TotvsRmIntegrationException)
        {
            return new RmEmployeeCareerHistoryData();
        }
    }

    private static async Task<IReadOnlyList<RmEmployeeFunctionHistoryRecord>> QueryFunctionHistoryAsync(
        SqlConnection connection,
        string chapa,
        CancellationToken cancellationToken)
    {
        var sqlTemplate = """
            SELECT
                H.DTMUDANCA AS EventDate,
                LTRIM(RTRIM(H.CODFUNCAO)) AS CodFuncao,
                LTRIM(RTRIM(FN.NOME)) AS FuncaoDescricao,
                LTRIM(RTRIM(C.NOME)) AS CargoDescricao
            FROM dbo.{0} H WITH (NOLOCK)
            LEFT JOIN dbo.PFUNCAO FN WITH (NOLOCK)
                ON FN.CODCOLIGADA = H.CODCOLIGADA AND FN.CODIGO = H.CODFUNCAO
            LEFT JOIN dbo.PCARGO C WITH (NOLOCK)
                ON C.CODCOLIGADA = FN.CODCOLIGADA AND C.CODIGO = FN.CARGO
            WHERE H.CODCOLIGADA = @CodColigada
              AND H.CHAPA = @Chapa
              AND H.DTMUDANCA IS NOT NULL
            ORDER BY H.DTMUDANCA ASC;
            """;

        foreach (var tableName in new[]
                 {
                     TotvsRmConstants.FunctionHistoryTableName,
                     TotvsRmConstants.FunctionHistoryFallbackTableName,
                 })
        {
            try
            {
                var sql = string.Format(sqlTemplate, tableName);
                var rows = await connection.QueryAsync<RmEmployeeFunctionHistoryRecord>(
                    sql,
                    new
                    {
                        CodColigada = TotvsRmConstants.CodColigada,
                        Chapa = chapa,
                    });

                return rows.ToList();
            }
            catch (SqlException exception) when (exception.Number is 208 or 3701)
            {
                // Invalid object name — try fallback table.
            }
        }

        return [];
    }

    private static async Task<IReadOnlyList<RmEmployeeSectionHistoryRecord>> QuerySectionHistoryAsync(
        SqlConnection connection,
        string chapa,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                H.DTMUDANCA AS EventDate,
                LTRIM(RTRIM(H.CODSECAO)) AS CodSecao,
                LTRIM(RTRIM(S.DESCRICAO)) AS SecaoDescricao
            FROM dbo.PFHSTSEC H WITH (NOLOCK)
            LEFT JOIN dbo.PSECAO S WITH (NOLOCK)
                ON S.CODCOLIGADA = H.CODCOLIGADA AND S.CODIGO = H.CODSECAO
            WHERE H.CODCOLIGADA = @CodColigada
              AND H.CHAPA = @Chapa
              AND H.DTMUDANCA IS NOT NULL
            ORDER BY H.DTMUDANCA ASC;
            """;

        try
        {
            var rows = await connection.QueryAsync<RmEmployeeSectionHistoryRecord>(
                sql,
                new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa,
                });

            return rows.ToList();
        }
        catch (SqlException exception) when (exception.Number is 208 or 3701)
        {
            return [];
        }
    }

    private static async Task<IReadOnlyList<RmEmployeeSalaryHistoryRecord>> QuerySalaryHistoryAsync(
        SqlConnection connection,
        string chapa,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                H.DTMUDANCA AS EventDate,
                H.SALARIO AS Salario,
                LTRIM(RTRIM(H.MOTIVO)) AS Motivo
            FROM dbo.PFHSTSAL H WITH (NOLOCK)
            WHERE H.CODCOLIGADA = @CodColigada
              AND H.CHAPA = @Chapa
              AND H.DTMUDANCA IS NOT NULL
            ORDER BY H.DTMUDANCA ASC;
            """;

        try
        {
            var rows = await connection.QueryAsync<RmEmployeeSalaryHistoryRecord>(
                sql,
                new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa,
                });

            return rows.ToList();
        }
        catch (SqlException exception) when (exception.Number is 208 or 3701)
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
