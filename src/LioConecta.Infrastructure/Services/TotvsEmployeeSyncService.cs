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

        foreach (var employee in employees)
        {
            var person = await db.People.FirstOrDefaultAsync(p => p.Slug == employee.ExternalId, cancellationToken);
            if (person is null)
            {
                person = new Person
                {
                    Id = Guid.NewGuid(),
                    Slug = employee.ExternalId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.People.Add(person);
            }

            person.Name = employee.Name;
            person.Title = employee.Title;
            person.Dept = employee.DepartmentCode;
            person.Email = employee.Email;
            person.BirthDate = employee.BirthDate;
            person.HireDate = employee.HireDate;
            person.IsActive = employee.IsActive;
            person.UpdatedAt = DateTimeOffset.UtcNow;

            slugToId[employee.ExternalId] = person.Id;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var employee in employees)
        {
            if (string.IsNullOrWhiteSpace(employee.ManagerExternalId))
            {
                continue;
            }

            var person = await db.People.FirstOrDefaultAsync(p => p.Slug == employee.ExternalId, cancellationToken);
            if (person is null)
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
            await context.LogInfoAsync($"TOTVS employee sync completed: {employees.Count} employees.", cancellationToken);
        }

        return employees.Count;
    }
}
