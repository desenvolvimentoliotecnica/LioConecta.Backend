using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Application.Services;

public sealed class TimesheetSyncService(
    IPersonRepository personRepository,
    ITotvsRmTimesheetRepository totvsRmTimesheetRepository,
    ITotvsRmConfigurationService totvsRmConfigurationService,
    TimesheetMergeService timesheetMergeService,
    ITimesheetPeriodCacheRepository cacheRepository) : ITimesheetSyncService
{
    private const string Provider = "TOTVS RM";
    private const string Source = "totvs-rm";

    public async Task<PontoResponseDto> SyncPersonAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException($"Person {personId} was not found.");

        if (string.IsNullOrWhiteSpace(person.EmployeeId))
        {
            return BuildUnavailableResponse(
                "missing_employee_id",
                "Sua matricula nao esta vinculada ao perfil. Solicite ao RH a regularizacao do cadastro.");
        }

        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled)
        {
            return BuildUnavailableResponse(
                "rm_disabled",
                "Consulta de ponto temporariamente indisponivel. Entre em contato com o RH.");
        }

        var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
        if (string.IsNullOrWhiteSpace(chapa))
        {
            return BuildUnavailableResponse(
                "missing_employee_id",
                "Sua matricula nao esta vinculada ao perfil. Solicite ao RH a regularizacao do cadastro.");
        }

        var (dataDe, dataAte, endMonth, endYear) = TimesheetPeriodResolver.Resolve(
            month,
            year,
            runtime.TimesheetPeriodStartDay,
            runtime.TimesheetPeriodEndDay);

        var punches = await totvsRmTimesheetRepository.GetPunchesAsync(chapa, dataDe, dataAte, cancellationToken);
        var processedDays = await totvsRmTimesheetRepository.GetProcessedDaysAsync(
            chapa,
            dataDe,
            dataAte,
            cancellationToken);

        var (summary, entries) = timesheetMergeService.Merge(dataDe, dataAte, punches, processedDays);
        var syncedAt = DateTimeOffset.UtcNow;

        await cacheRepository.UpsertAsync(
            personId,
            endYear,
            endMonth,
            summary,
            entries,
            syncedAt,
            Source,
            cancellationToken);

        return new PontoResponseDto(
            "Ponto",
            summary,
            entries,
            Provider,
            false,
            "ok",
            null,
            "live",
            syncedAt);
    }

    public async Task<int> SyncAllActivePeopleAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken)
    {
        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled)
        {
            await LogInfoAsync(context, "Integracao TOTVS RM desabilitada; sync de ponto ignorado.", cancellationToken);
            return 0;
        }

        var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrazilTimeZone.SaoPauloTimeZone);
        var (currentEndMonth, currentEndYear) = TimesheetPeriodResolver.ResolveCurrentPeriodEnd(
            todayLocal.Date,
            runtime.TimesheetPeriodEndDay);

        var periodsToSync = new List<(int Year, int Month)>
        {
            (currentEndYear, currentEndMonth)
        };

        var previousEndDate = new DateTime(currentEndYear, currentEndMonth, 1).AddMonths(-1);
        periodsToSync.Add((previousEndDate.Year, previousEndDate.Month));

        var activePeople = (await personRepository.GetOrgChartPeopleAsync(cancellationToken))
            .Where(p => !string.IsNullOrWhiteSpace(p.EmployeeId))
            .ToList();

        var synced = 0;
        foreach (var person in activePeople)
        {
            foreach (var (year, month) in periodsToSync)
            {
                try
                {
                    await SyncPersonAsync(person.Id, year, month, cancellationToken);
                    synced++;
                }
                catch (TotvsRmIntegrationException ex)
                {
                    await LogWarningAsync(
                        context,
                        $"Falha ao sincronizar ponto de {person.Slug} ({month:D2}/{year}): {ex.Message}",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    await LogWarningAsync(
                        context,
                        $"Erro inesperado ao sincronizar ponto de {person.Slug} ({month:D2}/{year}): {ex.Message}",
                        cancellationToken);
                }
            }
        }

        await LogInfoAsync(
            context,
            $"Sync de ponto concluido: {synced}/{activePeople.Count * periodsToSync.Count} periodos.",
            cancellationToken);
        return synced;
    }

    private static async Task LogInfoAsync(IWorkerRunContext? context, string message, CancellationToken cancellationToken)
    {
        if (context is not null)
        {
            await context.LogInfoAsync(message, cancellationToken);
        }
    }

    private static async Task LogWarningAsync(IWorkerRunContext? context, string message, CancellationToken cancellationToken)
    {
        if (context is not null)
        {
            await context.LogWarningAsync(message, cancellationToken);
        }
    }

    private static PontoResponseDto BuildUnavailableResponse(string availabilityStatus, string userMessage)
    {
        return new PontoResponseDto(
            "Ponto",
            null,
            [],
            Provider,
            false,
            availabilityStatus,
            userMessage,
            null,
            null);
    }
}
