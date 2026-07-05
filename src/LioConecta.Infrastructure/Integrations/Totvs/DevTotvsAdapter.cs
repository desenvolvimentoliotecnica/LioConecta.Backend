using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Infrastructure.Integrations.Totvs;

public sealed class DevTotvsAdapter : ITotvsAdapter
{
    private static readonly IReadOnlyList<TotvsEmployee> Employees =
    [
        new()
        {
            ExternalId = "julio-schwartzman",
            Name = "Júlio Schwartzman",
            Email = "julio.schwartzman@liotecnica.com.br",
            Title = "CEO · Diretoria Executiva",
            DepartmentCode = "executiva",
            ManagerExternalId = null,
            BirthDate = new DateOnly(1992, 3, 3),
            HireDate = new DateOnly(2020, 5, 1),
            IsActive = true
        },
        new()
        {
            ExternalId = "carlos-mendes",
            Name = "Carlos Mendes",
            Email = "carlos.mendes@liotecnica.com.br",
            Title = "Diretor de Produto",
            DepartmentCode = "produto",
            ManagerExternalId = "julio-schwartzman",
            BirthDate = new DateOnly(1988, 7, 12),
            HireDate = new DateOnly(2020, 1, 15),
            IsActive = true
        },
        new()
        {
            ExternalId = "maria-silva",
            Name = "Maria Silva",
            Email = "maria.silva@liotecnica.com.br",
            Title = "Gerente de Projetos",
            DepartmentCode = "produto",
            ManagerExternalId = "carlos-mendes",
            BirthDate = new DateOnly(1990, 11, 20),
            HireDate = new DateOnly(2022, 3, 10),
            IsActive = true
        },
        new()
        {
            ExternalId = "ricardo-souza",
            Name = "Ricardo Souza",
            Email = "ricardo.souza@liotecnica.com.br",
            Title = "Product Owner",
            DepartmentCode = "produto",
            ManagerExternalId = "carlos-mendes",
            BirthDate = new DateOnly(1991, 4, 8),
            HireDate = new DateOnly(2021, 9, 1),
            IsActive = true
        },
        new()
        {
            ExternalId = "julia-santos",
            Name = "Julia Santos",
            Email = "julia.santos@liotecnica.com.br",
            Title = "Designer de Produto",
            DepartmentCode = "produto",
            ManagerExternalId = "carlos-mendes",
            BirthDate = new DateOnly(1993, 6, 18),
            HireDate = new DateOnly(2021, 3, 22),
            IsActive = true
        }
    ];

    public Task<IReadOnlyList<TotvsEmployee>> SyncEmployeesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Employees);

    public Task<byte[]> GetPayslipAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Array.Empty<byte>());

    public Task<decimal> GetVacationBalanceAsync(Guid personId, CancellationToken cancellationToken = default) =>
        Task.FromResult(22.5m);

    public Task<string> SubmitVacationRequestAsync(
        Guid personId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult($"DEV-VR-{Guid.NewGuid():N}"[..12].ToUpperInvariant());

    public Task<IReadOnlyDictionary<string, object?>> GetBenefitsAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<string, object?>>(new Dictionary<string, object?>
        {
            ["healthPlan"] = "Unimed Premium",
            ["mealVoucher"] = 850.00m,
            ["gym"] = "TotalPass"
        });

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetTimeClockAsync(
        Guid personId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<IReadOnlyDictionary<string, object?>>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            entries.Add(new Dictionary<string, object?>
            {
                ["date"] = date.ToString("yyyy-MM-dd"),
                ["clockIn"] = "09:00",
                ["clockOut"] = "18:00",
                ["hours"] = 8
            });
        }

        return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(entries);
    }
}
