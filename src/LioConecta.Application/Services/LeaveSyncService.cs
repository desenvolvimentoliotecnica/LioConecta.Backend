using System.Globalization;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class LeaveSyncService(
    IPersonRepository personRepository,
    ITotvsRmLeaveRepository totvsRmLeaveRepository,
    ITotvsRmHourBankRepository totvsRmHourBankRepository,
    ITotvsRmConfigurationService totvsRmConfigurationService,
    ILeaveRepository leaveRepository) : ILeaveSyncService
{
    private const string Source = "totvs-rm";
    private static readonly JsonSerializerOptions JsonOptions = new();
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<LeaveSyncResultDto> SyncPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException($"Person {personId} was not found.");

        if (string.IsNullOrWhiteSpace(person.EmployeeId))
        {
            return new LeaveSyncResultDto(0, "missing_employee_id", null, null);
        }

        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled)
        {
            return new LeaveSyncResultDto(0, "rm_disabled", null, null);
        }

        var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
        if (string.IsNullOrWhiteSpace(chapa))
        {
            return new LeaveSyncResultDto(0, "missing_employee_id", null, null);
        }

        var rmData = await totvsRmLeaveRepository.GetLeaveDataByChapaAsync(chapa, cancellationToken);
        if (rmData is null)
        {
            return new LeaveSyncResultDto(0, "rm_unavailable", null, null);
        }

        var syncedAt = DateTimeOffset.UtcNow;
        decimal bancoHorasHours = 0m;
        try
        {
            var hourBank = await totvsRmHourBankRepository.GetLatestBalanceAsync(chapa, cancellationToken);
            if (hourBank is not null)
            {
                bancoHorasHours = Math.Round(hourBank.BalanceMinutes / 60m, 2, MidpointRounding.AwayFromZero);
            }
        }
        catch (TotvsRmIntegrationException)
        {
            // Mantém 0 se banco de horas falhar; férias ainda sincronizam.
        }

        await UpsertBalanceAsync(personId, rmData, bancoHorasHours, syncedAt, cancellationToken);

        var syncedRecords = 0;
        foreach (var request in rmData.Requests)
        {
            if (await UpsertRmRequestAsync(personId, request, syncedAt, cancellationToken))
            {
                syncedRecords++;
            }
        }

        return new LeaveSyncResultDto(syncedRecords, "ok", "live", syncedAt);
    }

    public async Task<int> SyncAllActivePeopleAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken)
    {
        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled)
        {
            await LogInfoAsync(context, "Integração TOTVS RM desabilitada; sync de férias ignorado.", cancellationToken);
            return 0;
        }

        var activePeople = (await personRepository.GetOrgChartPeopleAsync(cancellationToken))
            .Where(person => !string.IsNullOrWhiteSpace(person.EmployeeId))
            .ToList();

        var syncedPeople = 0;
        foreach (var person in activePeople)
        {
            try
            {
                var result = await SyncPersonAsync(person.Id, cancellationToken);
                if (result.SyncedRecords > 0 || result.DataSource == "live")
                {
                    syncedPeople++;
                }

                await LogInfoAsync(
                    context,
                    $"Férias sincronizadas para {person.Name} (CHAPA {TotvsRmChapaNormalizer.Normalize(person.EmployeeId)}): {result.SyncedRecords} registro(s), syncedAt={result.SyncedAt:O}.",
                    cancellationToken);
            }
            catch (Exception exception)
            {
                await LogInfoAsync(
                    context,
                    $"Falha ao sincronizar férias de {person.Name}: {exception.Message}",
                    cancellationToken);
            }
        }

        return syncedPeople;
    }

    private async Task UpsertBalanceAsync(
        Guid personId,
        RmLeaveBalanceData rmData,
        decimal bancoHorasBalanceHours,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var periods = rmData.Periods
            .Where(period => period.SaldoDias > 0)
            .Select(period =>
            {
                var status = LeavePeriodClassifier.Classify(period.FimPeriodo, period.DataVencimento, today);
                return new
                {
                    label = FormatPeriodLabel(period.InicioPeriodo, period.FimPeriodo),
                    acquiredDays = period.DiasAdquiridos,
                    usedDays = period.DiasUsados,
                    availableDays = period.SaldoDias,
                    expiresAt = period.DataVencimento?.ToString("O"),
                    status,
                    liberatesAt = period.FimPeriodo?.ToString("O"),
                    contextNote = LeavePeriodClassifier.BuildContextNote(
                        status,
                        period.SaldoDias,
                        period.FimPeriodo,
                        period.DataVencimento),
                };
            })
            .ToList();

        var notes = new List<string>();
        if (rmData.AvailableDays > 0)
        {
            notes.Add($"{rmData.AvailableDays} dia(s) liberados para solicitação agora.");
        }

        if (rmData.AcquiringDays > 0)
        {
            notes.Add(
                rmData.NextLiberationAt is not null
                    ? $"{rmData.AcquiringDays} dia(s) em aquisição — liberação a partir de {rmData.NextLiberationAt.Value:dd/MM/yyyy}."
                    : $"{rmData.AcquiringDays} dia(s) em aquisição (ainda não podem ser solicitados).");
        }

        if (rmData.ExpiredDays > 0)
        {
            notes.Add($"{rmData.ExpiredDays} dia(s) vencidos — consulte o RH.");
        }

        if (rmData.NextScheduledStart is not null)
        {
            notes.Add($"Próximo período programado: {FormatScheduledLabel(rmData.NextScheduledStart, rmData.NextScheduledEnd)}.");
        }

        var balance = new EmployeeLeaveBalance
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            AvailableDays = rmData.AvailableDays,
            AcquiredDays = rmData.AcquiredDays,
            ScheduledDays = rmData.ScheduledDays,
            ExpiredDays = rmData.ExpiredDays,
            BancoHorasBalanceHours = bancoHorasBalanceHours,
            NextScheduledStart = rmData.NextScheduledStart,
            NextScheduledEnd = rmData.NextScheduledEnd,
            BreakdownJson = JsonSerializer.Serialize(
                new
                {
                    acquiringDays = rmData.AcquiringDays,
                    nextLiberationAt = rmData.NextLiberationAt?.ToString("O"),
                    periods,
                    notes,
                },
                JsonOptions),
            DataSource = Source,
            SyncedAt = syncedAt,
            CreatedAt = syncedAt,
            UpdatedAt = syncedAt,
        };

        await leaveRepository.UpsertBalanceAsync(balance, cancellationToken);
    }

    private async Task<bool> UpsertRmRequestAsync(
        Guid personId,
        RmVacationRequestRecord request,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalId))
        {
            return false;
        }

        var status = LeaveStatusNormalizer.FromRm(request.RmStatus, request.StartDate, request.EndDate);
        var record = new LeaveRecord
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ServiceKey = "solicitar-ferias",
            RecordType = "ferias",
            Title = request.Title ?? "Férias — RM",
            Status = status,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Days = request.Days,
            DetailsJson = JsonSerializer.Serialize(new
            {
                rmStatus = request.RmStatus,
                source = Source,
            }, JsonOptions),
            RmExternalId = request.ExternalId,
            RmSyncStatus = "synced",
            DataSource = Source,
            SyncedAt = syncedAt,
            CreatedAt = syncedAt,
            UpdatedAt = syncedAt,
        };

        await leaveRepository.UpsertRmRecordAsync(record, cancellationToken);
        return true;
    }

    private static string FormatPeriodLabel(DateOnly? start, DateOnly? end)
    {
        if (start is null || end is null)
        {
            return "—";
        }

        return $"{start.Value.Year}/{end.Value.Year}";
    }

    private static string? FormatScheduledLabel(DateOnly? start, DateOnly? end)
    {
        if (start is null)
        {
            return null;
        }

        var month = PtBr.DateTimeFormat.GetAbbreviatedMonthName(start.Value.Month);
        month = char.ToUpper(month[0]) + month[1..];
        return end is null
            ? $"{month}/{start.Value.Year}"
            : $"{month}/{start.Value.Year}";
    }

    private static Task LogInfoAsync(
        IWorkerRunContext? context,
        string message,
        CancellationToken cancellationToken) =>
        context?.LogInfoAsync(message, cancellationToken) ?? Task.CompletedTask;
}
