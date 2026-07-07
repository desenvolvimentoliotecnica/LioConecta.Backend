using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class PayslipSyncService(
    IPersonRepository personRepository,
    ITotvsRmPayslipRepository totvsRmPayslipRepository,
    ITotvsRmEmployeeRepository employeeRepository,
    ITotvsRmConfigurationService totvsRmConfigurationService,
    IPayslipRepository payslipRepository) : IPayslipSyncService
{
    private const int MaxEnvelopes = 48;
    private const string Source = "totvs-rm";
    private static readonly JsonSerializerOptions JsonOptions = new();

    public async Task<PayslipSyncResultDto> SyncPersonAsync(
        Guid personId,
        CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException($"Person {personId} was not found.");

        if (string.IsNullOrWhiteSpace(person.EmployeeId))
        {
            return new PayslipSyncResultDto(0, "missing_employee_id", null, null);
        }

        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled)
        {
            return new PayslipSyncResultDto(0, "rm_disabled", null, null);
        }

        var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
        if (string.IsNullOrWhiteSpace(chapa))
        {
            return new PayslipSyncResultDto(0, "missing_employee_id", null, null);
        }

        var summaries = await totvsRmPayslipRepository.GetPayslipSummariesAsync(
            chapa,
            MaxEnvelopes,
            cancellationToken);

        var profile = await employeeRepository.GetProfileByChapaAsync(chapa, cancellationToken);
        var admissionDate = profile?.DataAdmissao?.Date;

        var eligibleSummaries = summaries
            .Where(summary => PayslipCompetenceRules.IsEligible(summary.AnoComp, summary.MesComp, admissionDate))
            .ToList();

        var syncedAt = DateTimeOffset.UtcNow;
        var synced = 0;

        foreach (var summary in eligibleSummaries)
        {
            var paymentType = PayslipRmMapper.MapPaymentTypeLabel(summary);
            if (await TrySyncEnvelopeAsync(
                    personId,
                    chapa,
                    summary,
                    paymentType,
                    syncedAt,
                    cancellationToken))
            {
                synced++;
            }
        }

        await payslipRepository.DeleteWithoutSourceAsync(personId, Source, cancellationToken);
        if (admissionDate is not null)
        {
            await payslipRepository.DeleteBeforeCompetenceAsync(
                personId,
                admissionDate.Value.Year,
                admissionDate.Value.Month,
                cancellationToken);
        }

        await SyncIncomeStatementsForPersonAsync(
            personId,
            chapa,
            eligibleSummaries,
            cancellationToken);

        await payslipRepository.DeleteIncomeStatementsWithoutSourceAsync(personId, Source, cancellationToken);

        return new PayslipSyncResultDto(synced, "ok", "live", syncedAt);
    }

    public async Task<bool> SyncIncomeStatementAsync(
        Guid personId,
        int year,
        CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken);
        if (person is null || string.IsNullOrWhiteSpace(person.EmployeeId))
        {
            return false;
        }

        var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
        if (string.IsNullOrWhiteSpace(chapa))
        {
            return false;
        }

        return await UpsertIncomeStatementFromRmAsync(personId, chapa, year, cancellationToken);
    }

    private async Task SyncIncomeStatementsForPersonAsync(
        Guid personId,
        string chapa,
        IReadOnlyList<RmPayslipSummaryRecord> eligibleSummaries,
        CancellationToken cancellationToken)
    {
        var years = eligibleSummaries
            .Select(summary => summary.AnoComp)
            .Distinct()
            .OrderByDescending(year => year);

        foreach (var year in years)
        {
            try
            {
                await UpsertIncomeStatementFromRmAsync(personId, chapa, year, cancellationToken);
            }
            catch (TotvsRmIntegrationException)
            {
                // Income statement sync is best-effort per year during bulk sync.
            }
        }
    }

    private async Task<bool> UpsertIncomeStatementFromRmAsync(
        Guid personId,
        string chapa,
        int year,
        CancellationToken cancellationToken)
    {
        var lines = await totvsRmPayslipRepository.GetIncomeStatementLinesAsync(
            chapa,
            year,
            cancellationToken);

        if (lines.Count == 0)
        {
            return false;
        }

        var dtoLines = lines
            .Where(line => line.MesComp is >= 1 and <= 12)
            .Select(line => new IncomeStatementLineDto(line.MesComp, line.TotalPaid, line.TotalWithheld))
            .OrderBy(line => line.Month)
            .ToList();

        var statement = new IncomeStatement
        {
            PersonId = personId,
            Year = year,
            TotalPaid = dtoLines.Sum(line => line.Paid),
            TotalWithheld = dtoLines.Sum(line => line.Withheld),
            LinesJson = JsonSerializer.Serialize(dtoLines, JsonOptions)
        };

        await payslipRepository.UpsertIncomeStatementAsync(statement, cancellationToken);
        return true;
    }

    public async Task<int> SyncAllActivePeopleAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken)
    {
        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled)
        {
            await LogInfoAsync(context, "Integracao TOTVS RM desabilitada; sync de holerites ignorado.", cancellationToken);
            return 0;
        }

        var activePeople = (await personRepository.GetOrgChartPeopleAsync(cancellationToken))
            .Where(p => !string.IsNullOrWhiteSpace(p.EmployeeId))
            .ToList();

        var syncedPeople = 0;
        foreach (var person in activePeople)
        {
            try
            {
                var result = await SyncPersonAsync(person.Id, cancellationToken);
                if (result.SyncedCount > 0)
                {
                    syncedPeople++;
                }

                await LogInfoAsync(
                    context,
                    $"Holerites sincronizados para {person.Name} (CHAPA {TotvsRmChapaNormalizer.Normalize(person.EmployeeId)}): {result.SyncedCount} envelope(s), syncedAt={result.SyncedAt:O}.",
                    cancellationToken);
            }
            catch (TotvsRmIntegrationException exception)
            {
                await LogInfoAsync(
                    context,
                    $"Falha ao sincronizar holerites de {person.Name}: {exception.Message}",
                    cancellationToken);
            }
        }

        return syncedPeople;
    }

    private async Task<bool> TrySyncEnvelopeAsync(
        Guid personId,
        string chapa,
        RmPayslipSummaryRecord summary,
        string paymentType,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken)
    {
        var envelopes = new List<RmPayslipSummaryRecord> { summary };
        var lines = await ResolveLinesAsync(
            chapa,
            summary.AnoComp,
            summary.MesComp,
            envelopes,
            summary.NroPeriodo,
            paymentType,
            cancellationToken);

        if (lines.Count == 0)
        {
            return false;
        }

        var earnings = lines
            .Where(line => !line.IsDeduction)
            .OrderBy(line => line.Code, StringComparer.Ordinal)
            .Select(line => new PayslipLineDto(line.Code, line.Description, line.Amount, null, line.Reference))
            .ToList();

        var deductions = lines
            .Where(line => line.IsDeduction)
            .OrderBy(line => line.Code, StringComparer.Ordinal)
            .Select(line => new PayslipLineDto(line.Code, line.Description, line.Amount, null, line.Reference))
            .ToList();

        var gross = earnings.Sum(item => item.Amount);
        var deductionsTotal = deductions.Sum(item => item.Amount);
        var net = gross - deductionsTotal;
        var publishedAt = summary.PaymentDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(summary.PaymentDate.Value, DateTimeKind.Unspecified), TimeSpan.Zero)
            : new DateTimeOffset(summary.AnoComp, summary.MesComp, DateTime.DaysInMonth(summary.AnoComp, summary.MesComp), 0, 0, 0, TimeSpan.Zero);

        var payslip = new Payslip
        {
            PersonId = personId,
            Year = summary.AnoComp,
            Month = summary.MesComp,
            NroPeriodo = summary.NroPeriodo,
            PaymentType = paymentType,
            GrossAmount = gross,
            NetAmount = net,
            DeductionsTotal = deductionsTotal,
            EarningsJson = JsonSerializer.Serialize(earnings, JsonOptions),
            DeductionsJson = JsonSerializer.Serialize(deductions, JsonOptions),
            PublishedAt = publishedAt,
            SyncedAtUtc = syncedAt,
            Source = Source
        };

        await payslipRepository.UpsertAsync(payslip, cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<RmPayslipLineRecord>> ResolveLinesAsync(
        string chapa,
        int anoComp,
        int mesComp,
        IReadOnlyList<RmPayslipSummaryRecord> envelopes,
        int nroPeriodo,
        string paymentType,
        CancellationToken cancellationToken)
    {
        foreach (var candidate in PayslipRmMapper.EnumerateEnvelopeCandidates(envelopes, nroPeriodo, paymentType))
        {
            var candidateLines = await TryGetPayslipLinesAsync(
                chapa,
                anoComp,
                mesComp,
                candidate.NroPeriodo,
                paymentType,
                cancellationToken);

            if (candidateLines.Count > 0)
            {
                return candidateLines;
            }
        }

        var monthLines = await TryGetPayslipLinesForMonthAsync(chapa, anoComp, mesComp, cancellationToken);
        if (monthLines.Count == 0)
        {
            return [];
        }

        var preferredPeriod = PayslipRmMapper.ResolveNroPeriodo(envelopes, nroPeriodo, paymentType);
        var lines = PayslipRmMapper.FilterLinesByPaymentType(
            monthLines.Where(line => line.NroPeriodo == preferredPeriod).ToList(),
            paymentType);

        if (lines.Count == 0)
        {
            lines = PayslipRmMapper.FilterLinesByPaymentType(
                monthLines.Where(line => line.NroPeriodo == preferredPeriod).ToList(),
                null);
        }

        if (lines.Count == 0)
        {
            var fallbackPeriod = monthLines
                .GroupBy(line => line.NroPeriodo)
                .OrderByDescending(group => group.Count())
                .First()
                .Key;

            lines = PayslipRmMapper.FilterLinesByPaymentType(
                monthLines.Where(line => line.NroPeriodo == fallbackPeriod).ToList(),
                paymentType);
        }

        return lines;
    }

    private async Task<IReadOnlyList<RmPayslipLineRecord>> TryGetPayslipLinesAsync(
        string chapa,
        int anoComp,
        int mesComp,
        int nroPeriodo,
        string paymentType,
        CancellationToken cancellationToken)
    {
        try
        {
            var lines = await totvsRmPayslipRepository.GetPayslipLinesAsync(
                chapa,
                anoComp,
                mesComp,
                nroPeriodo,
                cancellationToken);

            return PayslipRmMapper.FilterLinesByPaymentType(lines, paymentType);
        }
        catch (TotvsRmIntegrationException)
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<RmPayslipLineRecord>> TryGetPayslipLinesForMonthAsync(
        string chapa,
        int anoComp,
        int mesComp,
        CancellationToken cancellationToken)
    {
        try
        {
            return await totvsRmPayslipRepository.GetPayslipLinesForMonthAsync(
                chapa,
                anoComp,
                mesComp,
                cancellationToken);
        }
        catch (TotvsRmIntegrationException)
        {
            return [];
        }
    }

    private static Task LogInfoAsync(
        IWorkerRunContext? context,
        string message,
        CancellationToken cancellationToken) =>
        context is null
            ? Task.CompletedTask
            : context.LogInfoAsync(message, cancellationToken);
}
