using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Services;

public sealed class LeaveWriteBackService(
    ILeaveRepository leaveRepository,
    IPersonRepository personRepository,
    ILeaveRmWriteBack leaveRmWriteBack,
    IAppSettingsProvider settings,
    ILogger<LeaveWriteBackService> logger)
{
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        if (RmWriteBackModes.ResolveLeaveMode(settings) == RmWriteBackModes.Off)
        {
            return 0;
        }

        var pending = await leaveRepository.ListPendingWriteBackAsync(25, cancellationToken);
        var processed = 0;

        foreach (var record in pending)
        {
            if (record.StartDate is null || record.EndDate is null || record.Days is null)
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

            var result = await leaveRmWriteBack.SubmitVacationRequestAsync(
                new LeaveRmWriteBackCommand(
                    record.Id,
                    record.PersonId,
                    chapa,
                    record.StartDate.Value,
                    record.EndDate.Value,
                    record.Days.Value,
                    null),
                cancellationToken);

            record.RmSyncStatus = result.Status;
            record.RmExternalId = result.ExternalId ?? record.RmExternalId;
            record.UpdatedAt = DateTimeOffset.UtcNow;

            if (result.Success)
            {
                record.Status = "pending";
            }

            await leaveRepository.UpdateRecordAsync(record, cancellationToken);
            processed++;

            logger.LogInformation(
                "Write-back férias {RecordId}: success={Success}, status={Status}.",
                record.Id,
                result.Success,
                result.Status);
        }

        return processed;
    }
}
