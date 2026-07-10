using System.Globalization;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class HourBankService(
    ICurrentUserService currentUserService,
    IPersonRepository personRepository,
    ILeaveRepository leaveRepository,
    ITotvsRmHourBankRepository hourBankRepository,
    ITotvsRmConfigurationService totvsRmConfigurationService,
    PontoNotifyRecipientResolver pontoNotifyRecipientResolver) : IHourBankService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private const int HistoryPeriods = 12;
    private const int DayMovementDays = 90;

    public async Task<LeaveBancoHorasDto> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return await GetForPersonInternalAsync(personId, enforceTeamScope: false, cancellationToken);
    }

    public async Task<LeaveBancoHorasDto> GetForPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        return await GetForPersonInternalAsync(personId, enforceTeamScope: true, cancellationToken);
    }

    public async Task<IReadOnlyList<HourBankTeamMemberDto>> GetTeamAsync(
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        var currentPersonId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        var (canAccess, isRhScope) = await pontoNotifyRecipientResolver.CanManageAsync(
            currentPersonId,
            roles,
            cancellationToken);

        if (!canAccess)
        {
            throw new UnauthorizedAccessException("Sem permissão para consultar banco de horas da equipe.");
        }

        IReadOnlyList<Person> people;
        if (isRhScope)
        {
            people = (await personRepository.GetOrgChartPeopleAsync(cancellationToken))
                .Where(p => p.IsActive && !string.IsNullOrWhiteSpace(p.EmployeeId))
                .OrderBy(p => p.Name)
                .Take(200)
                .ToList();
        }
        else
        {
            people = await personRepository.GetDirectReportsAsync(currentPersonId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            people = people
                .Where(p =>
                    p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (p.EmployeeId?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (p.Title?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        var chapas = people
            .Select(p => TotvsRmChapaNormalizer.Normalize(p.EmployeeId))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyDictionary<string, RmHourBankBalanceRecord> balances;
        try
        {
            var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
            if (!runtime.IsEnabled)
            {
                return people.Select(p => new HourBankTeamMemberDto(
                    p.Id, p.Name, p.Title, p.EmployeeId, 0m, null)).ToList();
            }

            balances = await hourBankRepository.GetLatestBalancesByChapasAsync(chapas, cancellationToken);
        }
        catch (TotvsRmIntegrationException)
        {
            return people.Select(p => new HourBankTeamMemberDto(
                p.Id, p.Name, p.Title, p.EmployeeId, 0m, null)).ToList();
        }

        return people.Select(person =>
        {
            var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
            balances.TryGetValue(chapa ?? string.Empty, out var balance);
            return new HourBankTeamMemberDto(
                person.Id,
                person.Name,
                person.Title,
                person.EmployeeId,
                MinutesToHours(balance?.BalanceMinutes ?? 0),
                balance is null ? null : FormatPeriodLabel(balance.PeriodStart, balance.PeriodEnd));
        }).ToList();
    }

    private async Task<LeaveBancoHorasDto> GetForPersonInternalAsync(
        Guid personId,
        bool enforceTeamScope,
        CancellationToken cancellationToken)
    {
        if (enforceTeamScope)
        {
            await EnsureCanViewPersonAsync(personId, cancellationToken);
        }

        var person = await personRepository.GetByIdAsync(personId, cancellationToken);
        if (person is null || string.IsNullOrWhiteSpace(person.EmployeeId))
        {
            return Unavailable("missing_employee_id", "Matrícula não vinculada ao perfil.");
        }

        var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
        if (string.IsNullOrWhiteSpace(chapa))
        {
            return Unavailable("missing_employee_id", "Matrícula inválida.");
        }

        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled)
        {
            return Unavailable("rm_disabled", "Consulta de banco de horas temporariamente indisponível.");
        }

        try
        {
            var latest = await hourBankRepository.GetLatestBalanceAsync(chapa, cancellationToken);
            var history = await hourBankRepository.GetBalanceHistoryAsync(chapa, HistoryPeriods, cancellationToken);

            var to = DateTime.Today;
            var from = to.AddDays(-DayMovementDays);
            var days = await hourBankRepository.GetDayMovementsAsync(chapa, from, to, cancellationToken);

            var balanceHours = MinutesToHours(latest?.BalanceMinutes ?? 0);
            await CacheBalanceAsync(personId, balanceHours, cancellationToken);

            var entries = BuildEntries(history, days);
            return new LeaveBancoHorasDto(
                balanceHours,
                entries,
                latest is null ? null : FormatPeriodLabel(latest.PeriodStart, latest.PeriodEnd),
                "totvs-rm",
                "available",
                null);
        }
        catch (TotvsRmIntegrationDisabledException)
        {
            return Unavailable("rm_disabled", "Consulta de banco de horas temporariamente indisponível.");
        }
        catch (TotvsRmIntegrationMisconfiguredException)
        {
            return Unavailable("rm_misconfigured", "Integração TOTVS RM incompleta. Contate o administrador.");
        }
        catch (TotvsRmIntegrationUnavailableException)
        {
            return Unavailable("rm_unavailable", "Não foi possível consultar o banco de horas no TOTVS RM.");
        }
    }

    private async Task EnsureCanViewPersonAsync(Guid targetPersonId, CancellationToken cancellationToken)
    {
        var currentPersonId = await currentUserService.GetPersonIdAsync(cancellationToken);
        if (currentPersonId == targetPersonId)
        {
            return;
        }

        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        var (canAccess, isRhScope) = await pontoNotifyRecipientResolver.CanManageAsync(
            currentPersonId,
            roles,
            cancellationToken);

        if (!canAccess)
        {
            throw new UnauthorizedAccessException("Sem permissão para consultar banco de horas deste colaborador.");
        }

        if (isRhScope)
        {
            return;
        }

        var reports = await personRepository.GetDirectReportsAsync(currentPersonId, cancellationToken);
        if (reports.All(r => r.Id != targetPersonId))
        {
            throw new UnauthorizedAccessException("Colaborador fora da sua equipe.");
        }
    }

    private async Task CacheBalanceAsync(Guid personId, decimal balanceHours, CancellationToken cancellationToken)
    {
        var existing = await leaveRepository.GetBalanceAsync(personId, cancellationToken);
        if (existing is null)
        {
            return;
        }

        existing.BancoHorasBalanceHours = balanceHours;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await leaveRepository.UpsertBalanceAsync(existing, cancellationToken);
    }

    private static IReadOnlyList<LeaveBancoHorasEntryDto> BuildEntries(
        IReadOnlyList<RmHourBankBalanceRecord> history,
        IReadOnlyList<RmHourBankDayRecord> days)
    {
        var entries = new List<LeaveBancoHorasEntryDto>();

        // Extrato diário recente (créditos/débitos)
        foreach (var day in days.Take(60))
        {
            var label = day.Date.ToString("dd/MM/yyyy", PtBr);
            if (day.ExtraMinutes > 0)
            {
                entries.Add(new LeaveBancoHorasEntryDto(
                    label,
                    "Horas extras",
                    MinutesToHours(day.ExtraMinutes),
                    "credito"));
            }

            var debit = day.DelayMinutes + day.AbsenceMinutes;
            if (debit > 0)
            {
                var parts = new List<string>();
                if (day.DelayMinutes > 0) parts.Add("atraso");
                if (day.AbsenceMinutes > 0) parts.Add("falta");
                entries.Add(new LeaveBancoHorasEntryDto(
                    label,
                    $"Débito ({string.Join(" + ", parts)})",
                    -MinutesToHours(debit),
                    "debito"));
            }
        }

        // Se não houver movimentos diários, monta extrato por período (ASALDOBANCOHOR)
        if (entries.Count == 0)
        {
            foreach (var period in history)
            {
                var label = FormatPeriodLabel(period.PeriodStart, period.PeriodEnd);
                if (period.ExtraCurrentMinutes > 0)
                {
                    entries.Add(new LeaveBancoHorasEntryDto(
                        label,
                        "Horas extras do período",
                        MinutesToHours(period.ExtraCurrentMinutes),
                        "credito"));
                }

                var debit = period.DelayCurrentMinutes + period.AbsenceCurrentMinutes;
                if (debit > 0)
                {
                    entries.Add(new LeaveBancoHorasEntryDto(
                        label,
                        "Atrasos e faltas do período",
                        -MinutesToHours(debit),
                        "debito"));
                }
            }
        }

        return entries;
    }

    private static LeaveBancoHorasDto Unavailable(string status, string message) =>
        new(0m, [], null, "totvs-rm", status, message);

    private static decimal MinutesToHours(int minutes) =>
        Math.Round(minutes / 60m, 2, MidpointRounding.AwayFromZero);

    private static string FormatPeriodLabel(DateTime start, DateTime end) =>
        $"{start.ToString("dd/MM/yyyy", PtBr)} – {end.ToString("dd/MM/yyyy", PtBr)}";
}
