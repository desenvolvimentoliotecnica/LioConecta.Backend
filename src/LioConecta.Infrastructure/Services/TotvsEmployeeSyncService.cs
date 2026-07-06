using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class TotvsEmployeeSyncService(
    ITotvsAdapter totvsAdapter,
    ITotvsRmEmployeeRepository employeeRepository,
    ITotvsRmConfigurationService totvsRmConfigurationService,
    AppDbContext db) : ITotvsEmployeeSyncService
{
    public async Task<int> SyncEmployeesAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken)
    {
        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (runtime.IsEnabled && !string.IsNullOrWhiteSpace(runtime.Password))
        {
            return await SyncFromRmAsync(context, cancellationToken);
        }

        return await SyncFromRestAdapterAsync(context, cancellationToken);
    }

    private async Task<int> SyncFromRmAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken)
    {
        var admissions = await employeeRepository.GetActiveAdmissionsAsync(cancellationToken);
        if (admissions.Count == 0)
        {
            if (context is not null)
            {
                await context.LogWarningAsync("TOTVS RM returned no active employee HR records.", cancellationToken);
            }

            return 0;
        }

        var byChapa = admissions
            .Select(record => new
            {
                Chapa = TotvsRmChapaNormalizer.Normalize(record.Chapa),
                Record = record,
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Chapa))
            .GroupBy(entry => entry.Chapa!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Record, StringComparer.OrdinalIgnoreCase);

        var byEmail = admissions
            .Where(record => !string.IsNullOrWhiteSpace(record.EmailPessoal))
            .GroupBy(record => record.EmailPessoal!.Trim().ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var people = await db.People.Where(p => p.IsActive).ToListAsync(cancellationToken);
        var matched = 0;
        var hireDatesUpdated = 0;
        var birthDatesUpdated = 0;
        var employeeIdsLinked = 0;

        foreach (var person in people)
        {
            var rmRecord = ResolveRmRecord(person, byChapa, byEmail);
            if (rmRecord is null)
            {
                continue;
            }

            var updated = false;

            if (string.IsNullOrWhiteSpace(person.EmployeeId) && !string.IsNullOrWhiteSpace(rmRecord.Chapa))
            {
                person.EmployeeId = rmRecord.Chapa.Trim();
                employeeIdsLinked++;
                updated = true;
            }

            if (rmRecord.DataAdmissao is not null)
            {
                var hireDate = DateOnly.FromDateTime(rmRecord.DataAdmissao.Value.Date);
                if (person.HireDate != hireDate)
                {
                    person.HireDate = hireDate;
                    hireDatesUpdated++;
                    updated = true;
                }
            }

            if (rmRecord.DataNascimento is not null)
            {
                var birthDate = DateOnly.FromDateTime(rmRecord.DataNascimento.Value.Date);
                if (person.BirthDate != birthDate)
                {
                    person.BirthDate = birthDate;
                    birthDatesUpdated++;
                    updated = true;
                }
            }

            if (updated)
            {
                matched++;
                person.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (context is not null)
        {
            await context.LogInfoAsync(
                $"TOTVS RM employee sync completed: rmRecords={admissions.Count}, matched={matched}, hireDatesUpdated={hireDatesUpdated}, birthDatesUpdated={birthDatesUpdated}, employeeIdsLinked={employeeIdsLinked}.",
                cancellationToken);
        }

        return matched;
    }

    private static RmEmployeeAdmissionRecord? ResolveRmRecord(
        Person person,
        IReadOnlyDictionary<string, RmEmployeeAdmissionRecord> byChapa,
        IReadOnlyDictionary<string, RmEmployeeAdmissionRecord> byEmail)
    {
        var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
        if (!string.IsNullOrWhiteSpace(chapa) && byChapa.TryGetValue(chapa, out var byEmployeeId))
        {
            return byEmployeeId;
        }

        if (!string.IsNullOrWhiteSpace(person.Email)
            && byEmail.TryGetValue(person.Email.Trim().ToLowerInvariant(), out var byPersonEmail))
        {
            return byPersonEmail;
        }

        return null;
    }

    private async Task<int> SyncFromRestAdapterAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken)
    {
        var employees = await totvsAdapter.SyncEmployeesAsync(cancellationToken);
        if (employees.Count == 0)
        {
            if (context is not null)
            {
                await context.LogWarningAsync("TOTVS sync returned no employees.", cancellationToken);
            }

            return 0;
        }

        var slugToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var enriched = 0;

        foreach (var employee in employees)
        {
            var person = await FindPersonAsync(employee, cancellationToken);
            var fromGraph = person?.AzureAdObjectId is not null;

            if (person is null)
            {
                person = new Person
                {
                    Id = Guid.NewGuid(),
                    Slug = employee.ExternalId,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                db.People.Add(person);
            }

            if (!fromGraph)
            {
                person.Name = employee.Name;
                person.Title = employee.Title;
                person.Dept = employee.DepartmentCode;
                person.Email = employee.Email;
                person.IsActive = employee.IsActive;
            }

            if (!string.IsNullOrWhiteSpace(employee.ExternalId))
            {
                person.EmployeeId = employee.ExternalId.Trim();
            }

            if (employee.BirthDate is not null)
            {
                person.BirthDate = employee.BirthDate;
            }

            if (employee.HireDate is not null && (!fromGraph || person.HireDate is null))
            {
                person.HireDate = employee.HireDate;
            }

            person.UpdatedAt = DateTimeOffset.UtcNow;
            slugToId[employee.ExternalId] = person.Id;
            enriched++;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var employee in employees)
        {
            if (string.IsNullOrWhiteSpace(employee.ManagerExternalId))
            {
                continue;
            }

            var person = await db.People.FirstOrDefaultAsync(p => p.Slug == employee.ExternalId, cancellationToken);
            if (person is null || person.ManagerId is not null)
            {
                continue;
            }

            person.ManagerId = slugToId.TryGetValue(employee.ManagerExternalId, out var managerId)
                ? managerId
                : null;
        }

        await db.SaveChangesAsync(cancellationToken);

        if (context is not null)
        {
            await context.LogInfoAsync(
                $"TOTVS employee sync completed: {employees.Count} employees, enriched={enriched}.",
                cancellationToken);
        }

        return employees.Count;
    }

    private async Task<Person?> FindPersonAsync(
        TotvsEmployee employee,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(employee.Email))
        {
            var email = employee.Email.Trim();
            var byEmail = await db.People.FirstOrDefaultAsync(
                p => p.Email.ToLower() == email.ToLower(),
                cancellationToken);
            if (byEmail is not null)
            {
                return byEmail;
            }
        }

        return await db.People.FirstOrDefaultAsync(p => p.Slug == employee.ExternalId, cancellationToken);
    }
}
