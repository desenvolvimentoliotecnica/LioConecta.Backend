using System.Globalization;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Services;

/// <summary>
/// Processa ajustes de ponto aprovados com RmSyncStatus=pending_rm_sync e envia ao TOTVS RM
/// (write-back SQL direto, ver docs/spike-writeback-sql-rm.md).
/// </summary>
public sealed class PontoWriteBackService(
    IPontoAdjustmentRepository pontoAdjustmentRepository,
    IPersonRepository personRepository,
    IPontoRmWriteBack pontoRmWriteBack,
    IAppSettingsProvider settings,
    ILogger<PontoWriteBackService> logger)
{
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        if (RmWriteBackModes.ResolvePontoMode(settings) == RmWriteBackModes.Off)
        {
            return 0;
        }

        var pending = await pontoAdjustmentRepository.ListPendingWriteBackAsync(25, cancellationToken);
        var processed = 0;

        foreach (var record in pending)
        {
            var days = ExtractDays(record.DetailsJson);
            if (days.Count == 0)
            {
                continue;
            }

            var person = await personRepository.GetByIdAsync(record.PersonId, cancellationToken);
            if (person is null || string.IsNullOrWhiteSpace(person.EmployeeId))
            {
                continue;
            }

            var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
            if (string.IsNullOrWhiteSpace(chapa))
            {
                continue;
            }

            var result = await pontoRmWriteBack.SubmitAdjustmentAsync(
                new PontoRmWriteBackCommand(record.Id, record.PersonId, chapa, days),
                cancellationToken);

            record.RmSyncStatus = result.Status;
            record.RmExternalId = result.ExternalId ?? record.RmExternalId;
            record.UpdatedAt = DateTimeOffset.UtcNow;

            await pontoAdjustmentRepository.UpdateAsync(record, cancellationToken);
            processed++;

            logger.LogInformation(
                "Write-back ponto {RecordId}: success={Success}, status={Status}.",
                record.Id,
                result.Success,
                result.Status);
        }

        return processed;
    }

    private static List<PontoRmWriteBackPunch> ExtractDays(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson) || detailsJson == "{}")
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(detailsJson);
            if (!doc.RootElement.TryGetProperty("days", out var daysEl) || daysEl.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<PontoRmWriteBackPunch>();
            foreach (var item in daysEl.EnumerateArray())
            {
                var dateText = GetString(item, "date");
                if (!DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    continue;
                }

                result.Add(new PontoRmWriteBackPunch(
                    date,
                    NullIfEmpty(GetString(item, "clockIn")),
                    NullIfEmpty(GetString(item, "lunchOut")),
                    NullIfEmpty(GetString(item, "lunchIn")),
                    NullIfEmpty(GetString(item, "clockOut"))));
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            var pascal = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
            if (!element.TryGetProperty(pascal, out value))
            {
                return string.Empty;
            }
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
    }
}
