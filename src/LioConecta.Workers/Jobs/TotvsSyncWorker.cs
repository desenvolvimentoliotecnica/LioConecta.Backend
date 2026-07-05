using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Workers.Jobs;

public sealed class TotvsSyncWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TotvsSyncWorker> _logger;
    private readonly IAppSettingsProvider _settings;

    public TotvsSyncWorker(
        IServiceProvider services,
        ILogger<TotvsSyncWorker> logger,
        IAppSettingsProvider settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _settings.GetInt(AppSettingKeys.WorkersTotvsSyncIntervalMinutes, 30);
        _logger.LogInformation("TOTVS sync worker started (interval: {Interval} min)", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncEmployeesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TOTVS sync failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    private async Task SyncEmployeesAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var totvs = scope.ServiceProvider.GetRequiredService<ITotvsAdapter>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var employees = await totvs.SyncEmployeesAsync(cancellationToken);
        if (employees.Count == 0)
        {
            _logger.LogWarning("TOTVS sync returned no employees");
            return;
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
                    CreatedAt = DateTime.UtcNow
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
            person.UpdatedAt = DateTime.UtcNow;

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
        _logger.LogInformation("TOTVS sync completed: {Count} employees", employees.Count);
    }
}
