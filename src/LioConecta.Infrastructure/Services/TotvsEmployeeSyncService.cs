using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class TotvsEmployeeSyncService(
    ITotvsAdapter totvsAdapter,
    AppDbContext db) : ITotvsEmployeeSyncService
{
    public async Task<int> SyncEmployeesAsync(
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

            if (employee.HireDate is not null)
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
        Application.Interfaces.Integrations.Models.TotvsEmployee employee,
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
