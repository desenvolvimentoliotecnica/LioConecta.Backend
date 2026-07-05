using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class DevGraphAdapter : IGraphAdapter
{
    public Task<IReadOnlyList<GraphDirectoryUser>> GetDirectoryUsersAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GraphDirectoryUser> users =
        [
            new()
            {
                ObjectId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                DisplayName = "Maria Silva",
                UserPrincipalName = "maria.silva@liotecnica.com.br",
                Mail = "maria.silva@liotecnica.com.br",
                JobTitle = "Gerente de Projetos",
                Department = "Produto",
                AccountEnabled = true,
            },
            new()
            {
                ObjectId = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                DisplayName = "Carlos Mendes",
                UserPrincipalName = "carlos.mendes@liotecnica.com.br",
                Mail = "carlos.mendes@liotecnica.com.br",
                JobTitle = "Analista de Produto",
                Department = "Produto",
                ManagerObjectId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                AccountEnabled = true,
            },
            new()
            {
                ObjectId = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                DisplayName = "Patricia Nunes",
                UserPrincipalName = "patricia.nunes@liotecnica.com.br",
                Mail = "patricia.nunes@liotecnica.com.br",
                JobTitle = "Coordenadora de RH",
                Department = "Recursos Humanos",
                AccountEnabled = true,
            },
        ];

        return Task.FromResult(users);
    }

    public Task SyncUserPhotosAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<GraphDocument>> GetDocumentsAsync(
        string? category,
        CancellationToken cancellationToken = default)
    {
        var documents = new List<GraphDocument>
        {
            new()
            {
                ItemId = "doc-manual-colaborador",
                Title = "Manual do Colaborador",
                Category = "manuais",
                WebUrl = "https://sharepoint.dev.local/docs/manual-colaborador",
                ModifiedAt = DateTimeOffset.UtcNow.AddDays(-30)
            },
            new()
            {
                ItemId = "doc-politica-seguranca",
                Title = "Política de Segurança da Informação",
                Category = "politicas",
                WebUrl = "https://sharepoint.dev.local/docs/politica-seguranca",
                ModifiedAt = DateTimeOffset.UtcNow.AddDays(-14)
            },
            new()
            {
                ItemId = "doc-onboarding-produto",
                Title = "Onboarding · Produto",
                Category = "onboarding",
                WebUrl = "https://sharepoint.dev.local/docs/onboarding-produto",
                ModifiedAt = DateTimeOffset.UtcNow.AddDays(-7)
            }
        };

        if (!string.IsNullOrWhiteSpace(category))
        {
            documents = documents
                .Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Task.FromResult<IReadOnlyList<GraphDocument>>(documents);
    }

    public Task<IReadOnlyList<GraphCalendarEvent>> GetCalendarEventsAsync(
        Guid personId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var events = new List<GraphCalendarEvent>
        {
            new()
            {
                ExternalId = "evt-daily-standup",
                Title = "Daily · Produto",
                StartAt = now.Date.AddHours(10),
                EndAt = now.Date.AddHours(10).AddMinutes(30),
                Location = "Microsoft Teams"
            },
            new()
            {
                ExternalId = "evt-all-hands",
                Title = "All Hands LioTécnica",
                StartAt = now.Date.AddDays(3).AddHours(15),
                EndAt = now.Date.AddDays(3).AddHours(16),
                Location = "Auditório Matriz"
            },
            new()
            {
                ExternalId = "evt-1on1",
                Title = "1:1 com gestor",
                StartAt = now.Date.AddDays(1).AddHours(14),
                EndAt = now.Date.AddDays(1).AddHours(14).AddMinutes(45),
                Location = "Sala 310"
            }
        };

        var filtered = events
            .Where(e => e.StartAt >= from && e.StartAt <= to)
            .ToList();

        return Task.FromResult<IReadOnlyList<GraphCalendarEvent>>(filtered);
    }

    public Task<IReadOnlyList<GraphPlannerTask>> GetPlannerTasksAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GraphPlannerTask>>(new List<GraphPlannerTask>
        {
            new()
            {
                TaskId = "planner-task-001",
                Title = "Revisar backlog do trimestre",
                BucketName = "Em andamento",
                PercentComplete = 45,
                DueDate = DateTimeOffset.UtcNow.AddDays(5)
            },
            new()
            {
                TaskId = "planner-task-002",
                Title = "Atualizar documentação de onboarding",
                BucketName = "A fazer",
                PercentComplete = 0,
                DueDate = DateTimeOffset.UtcNow.AddDays(12)
            },
            new()
            {
                TaskId = "planner-task-003",
                Title = "Validar integração Totvs",
                BucketName = "Concluído",
                PercentComplete = 100,
                DueDate = DateTimeOffset.UtcNow.AddDays(-2)
            }
        });

    public Task<string?> GetUserPresenceAsync(Guid personId, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>("Available");
}
