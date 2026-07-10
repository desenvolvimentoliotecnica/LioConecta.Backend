using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class PontoService(
    ICurrentUserService currentUserService,
    IAppSettingsProvider settings,
    ITotvsRmConfigurationService totvsRmConfigurationService,
    ITimesheetSyncService timesheetSyncService,
    ITimesheetPeriodCacheRepository cacheRepository,
    IPersonRepository personRepository,
    ITotvsRmEmployeeRepository employeeRepository) : IPontoService
{
    private const string Provider = "TOTVS RM";

    public async Task<PontoPeriodSettingsDto> GetPeriodSettingsAsync(CancellationToken cancellationToken)
    {
        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        var admissionDate = await ResolveAdmissionDateAsync(cancellationToken);
        var options = TimesheetPeriodResolver.BuildRecentPeriodOptions(
            TimesheetPeriodResolver.DefaultRecentPeriodCount,
            runtime.TimesheetPeriodStartDay,
            runtime.TimesheetPeriodEndDay,
            admissionDate: admissionDate);

        return new PontoPeriodSettingsDto(
            runtime.TimesheetPeriodStartDay,
            runtime.TimesheetPeriodEndDay,
            options
                .Select(option => new PontoPeriodOptionDto(option.EndMonth, option.EndYear, option.Label))
                .ToList());
    }

    public async Task<PontoResponseDto> GetTimesheetAsync(
        int? month,
        int? year,
        CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        var (_, _, resolvedEndMonth, resolvedEndYear) = TimesheetPeriodResolver.Resolve(
            month,
            year,
            runtime.TimesheetPeriodStartDay,
            runtime.TimesheetPeriodEndDay);

        var person = await personRepository.GetByIdAsync(personId, cancellationToken);
        if (person is null || string.IsNullOrWhiteSpace(person.EmployeeId))
        {
            return BuildUnavailableResponse(
                "missing_employee_id",
                "Sua matricula nao esta vinculada ao perfil. Solicite ao RH a regularizacao do cadastro.");
        }

        var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
        if (string.IsNullOrWhiteSpace(chapa))
        {
            return BuildUnavailableResponse(
                "missing_employee_id",
                "Sua matricula nao esta vinculada ao perfil. Solicite ao RH a regularizacao do cadastro.");
        }

        var admissionDate = await ResolveAdmissionDateAsync(person, chapa, cancellationToken);
        if (!TimesheetPeriodResolver.IsPeriodEligible(
                resolvedEndMonth,
                resolvedEndYear,
                runtime.TimesheetPeriodStartDay,
                runtime.TimesheetPeriodEndDay,
                admissionDate))
        {
            return BuildUnavailableResponse(
                "before_admission",
                "O periodo selecionado e anterior a sua data de admissao.");
        }

        if (!runtime.IsEnabled)
        {
            return BuildUnavailableResponse(
                "rm_disabled",
                "Consulta de ponto temporariamente indisponivel. Entre em contato com o RH.");
        }

        var cache = await cacheRepository.GetAsync(
            personId,
            resolvedEndYear,
            resolvedEndMonth,
            cancellationToken);
        var ttlMinutes = settings.GetInt(AppSettingKeys.WorkersTotvsTimesheetCacheTtlMinutes, 60);
        if (cache is not null && !IsStale(cache.SyncedAtUtc, ttlMinutes))
        {
            return BuildResponseFromCache(cache, "cache");
        }

        try
        {
            var synced = await timesheetSyncService.SyncPersonAsync(
                personId,
                resolvedEndYear,
                resolvedEndMonth,
                cancellationToken);

            return synced with
            {
                DataSource = "live",
                AvailabilityStatus = synced.AvailabilityStatus ?? "ok"
            };
        }
        catch (TotvsRmIntegrationDisabledException)
        {
            return BuildUnavailableResponse(
                "rm_disabled",
                "Consulta de ponto temporariamente indisponivel. Entre em contato com o RH.");
        }
        catch (TotvsRmIntegrationMisconfiguredException)
        {
            return BuildUnavailableResponse(
                "rm_disabled",
                "Consulta de ponto temporariamente indisponivel. Entre em contato com o RH.");
        }
        catch (TotvsRmIntegrationUnavailableException)
        {
            if (cache is not null)
            {
                return BuildResponseFromCache(cache, "cache") with
                {
                    UserMessage = "Exibindo dados em cache. Nao foi possivel atualizar agora.",
                    AvailabilityStatus = "rm_unavailable"
                };
            }

            return BuildUnavailableResponse(
                "rm_unavailable",
                "Nao foi possivel consultar o ponto agora. Tente novamente em alguns minutos.");
        }
    }

    private async Task<DateTime?> ResolveAdmissionDateAsync(CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var person = await personRepository.GetByIdAsync(personId, cancellationToken);
        if (person is null)
        {
            return null;
        }

        var chapa = string.IsNullOrWhiteSpace(person.EmployeeId)
            ? null
            : TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
        return await ResolveAdmissionDateAsync(person, chapa, cancellationToken);
    }

    private async Task<DateTime?> ResolveAdmissionDateAsync(
        Person person,
        string? chapa,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(chapa))
        {
            try
            {
                var profile = await employeeRepository.GetProfileByChapaAsync(chapa, cancellationToken);
                if (profile?.DataAdmissao is not null)
                {
                    return profile.DataAdmissao.Value.Date;
                }
            }
            catch (TotvsRmIntegrationDisabledException)
            {
                // Fallback para HireDate local.
            }
            catch (TotvsRmIntegrationMisconfiguredException)
            {
                // Fallback para HireDate local.
            }
            catch (TotvsRmIntegrationUnavailableException)
            {
                // Fallback para HireDate local.
            }
        }

        return person.HireDate?.ToDateTime(TimeOnly.MinValue);
    }

    private static bool IsStale(DateTimeOffset syncedAtUtc, int ttlMinutes)
    {
        return syncedAtUtc.AddMinutes(ttlMinutes) < DateTimeOffset.UtcNow;
    }

    private static PontoResponseDto BuildResponseFromCache(TimesheetPeriodCacheDto cache, string dataSource)
    {
        return new PontoResponseDto(
            "Ponto",
            cache.Summary,
            cache.Entries,
            Provider,
            false,
            "ok",
            null,
            dataSource,
            cache.SyncedAtUtc);
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
