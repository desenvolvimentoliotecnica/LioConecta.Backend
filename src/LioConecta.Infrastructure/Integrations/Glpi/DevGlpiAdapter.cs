using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Infrastructure.Integrations.Glpi;

public sealed class DevGlpiAdapter : IGlpiAdapter
{
    private int _ticketCounter = 4820;

    private readonly Dictionary<string, List<GlpiTicketSummary>> _ticketsByEmail = new(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<GlpiEntity> MockEntities =
    [
        new() { Id = 1, Name = "Liotécnica", FullName = "Liotécnica", ParentId = null },
        new() { Id = 2, Name = "Matriz", FullName = "Liotécnica > Matriz", ParentId = 1 },
        new() { Id = 3, Name = "Filial SP", FullName = "Liotécnica > Filial SP", ParentId = 1 },
    ];

    private static readonly IReadOnlyList<GlpiItilCategory> MockCategories =
    [
        new() { Id = 1, Name = "TI", FullName = "TI", ParentId = null, EntityId = 2 },
        new() { Id = 2, Name = "Hardware", FullName = "Hardware", ParentId = null, EntityId = 2 },
        new() { Id = 3, Name = "Software", FullName = "Software", ParentId = null, EntityId = 2 },
        new() { Id = 10, Name = "Sistemas Corporativos", FullName = "TI > Sistemas Corporativos", ParentId = 1, EntityId = 2 },
        new() { Id = 11, Name = "Web e Aplicações", FullName = "TI > Web e Aplicações", ParentId = 1, EntityId = 2 },
        new() { Id = 20, Name = "Notebooks", FullName = "Hardware > Notebooks", ParentId = 2, EntityId = 2 },
        new() { Id = 30, Name = "Office", FullName = "Software > Office", ParentId = 3, EntityId = 2 },
        new() { Id = 40, Name = "Infraestrutura", FullName = "Infraestrutura", ParentId = null, EntityId = 3 },
        new() { Id = 41, Name = "Rede", FullName = "Infraestrutura > Rede", ParentId = 40, EntityId = 3 },
    ];

    public Task<GlpiTicketResult> CreateTicketAsync(
        string title,
        string description,
        string priority,
        int entityId,
        int categoryId,
        string requesterEmail,
        CancellationToken cancellationToken = default)
    {
        var ticketId = Interlocked.Increment(ref _ticketCounter).ToString();
        var summary = new GlpiTicketSummary
        {
            TicketId = ticketId,
            Subject = title,
            Status = "1",
            StatusLabel = "Novo",
            PriorityLabel = priority,
            CreatedAt = DateTimeOffset.UtcNow,
            Url = $"https://glpi.dev.local/front/ticket.form.php?id={ticketId}",
            RequesterLabel = requesterEmail,
        };

        if (!_ticketsByEmail.TryGetValue(requesterEmail, out var list))
        {
            list = SeedTickets(requesterEmail);
            _ticketsByEmail[requesterEmail] = list;
        }

        list.Insert(0, summary);
        EnsureGlobalPool();

        return Task.FromResult(new GlpiTicketResult
        {
            TicketId = ticketId,
            Status = "New",
            Url = summary.Url,
        });
    }

    public Task<IReadOnlyList<GlpiEntity>> GetEntitiesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HelpDeskGlpiEntityTreeBuilder.Build(MockEntities));

    public Task<IReadOnlyList<GlpiItilCategory>> GetAllItilCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var filtered = MockCategories.Where(c => c.EntityId == 2 || c.EntityId == 3).ToList();
        return Task.FromResult(HelpDeskItilCategoryTreeBuilder.Build(filtered, dedupeByLabel: false));
    }

    public Task<IReadOnlyList<GlpiItilCategory>> GetItilCategoriesAsync(
        int entityId,
        CancellationToken cancellationToken = default)
    {
        var filtered = MockCategories.Where(c => c.EntityId == entityId).ToList();
        return Task.FromResult(HelpDeskItilCategoryTreeBuilder.Build(filtered, dedupeByLabel: false));
    }

    public Task<GlpiTicketResult> GetTicketStatusAsync(
        string ticketId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new GlpiTicketResult
        {
            TicketId = ticketId,
            Status = "2",
            Url = $"https://glpi.dev.local/front/ticket.form.php?id={ticketId}",
        });

    public Task<IReadOnlyList<GlpiTicketSummary>> SearchTicketsByRequesterAsync(
        string requesterEmail,
        GlpiTicketScope scope,
        CancellationToken cancellationToken = default)
    {
        if (!_ticketsByEmail.TryGetValue(requesterEmail, out var list))
        {
            list = SeedTickets(requesterEmail);
            _ticketsByEmail[requesterEmail] = list;
        }

        return Task.FromResult<IReadOnlyList<GlpiTicketSummary>>(FilterByScope(list, scope));
    }

    public Task<IReadOnlyList<GlpiTicketSummary>> SearchAllTicketsAsync(
        GlpiTicketScope scope,
        CancellationToken cancellationToken = default)
    {
        EnsureGlobalPool();
        var all = _ticketsByEmail.Values.SelectMany(items => items).ToList();
        return Task.FromResult<IReadOnlyList<GlpiTicketSummary>>(FilterByScope(all, scope));
    }

    public Task<GlpiTicketDetail?> GetTicketDetailAsync(
        string ticketId,
        string requesterEmail,
        bool skipOwnershipCheck = false,
        CancellationToken cancellationToken = default)
    {
        EnsureGlobalPool();
        GlpiTicketSummary? summary = null;

        if (!skipOwnershipCheck)
        {
            if (!_ticketsByEmail.TryGetValue(requesterEmail, out var list))
            {
                list = SeedTickets(requesterEmail);
                _ticketsByEmail[requesterEmail] = list;
            }

            summary = list.FirstOrDefault(t => t.TicketId == ticketId);
        }
        else
        {
            summary = _ticketsByEmail.Values
                .SelectMany(items => items)
                .FirstOrDefault(t => t.TicketId == ticketId);
        }

        if (summary is null)
        {
            return Task.FromResult<GlpiTicketDetail?>(null);
        }

        return Task.FromResult<GlpiTicketDetail?>(new GlpiTicketDetail
        {
            Summary = summary,
            Description = "Chamado simulado para desenvolvimento local.",
            Assignee = "TI — Service Desk",
            Followups =
            [
                new GlpiTicketFollowup
                {
                    Content = "Chamado registrado no GLPI (mock).",
                    CreatedAt = summary.CreatedAt,
                    Author = "Service Desk",
                },
            ],
        });
    }

    private void EnsureGlobalPool()
    {
        SeedTickets("leonardo.mendes@liotecnica.com.br");
        SeedTickets("maria.silva@liotecnica.com.br");
        SeedTickets("carlos.mendes@liotecnica.com.br");
    }

    private static IReadOnlyList<GlpiTicketSummary> FilterByScope(IEnumerable<GlpiTicketSummary> items, GlpiTicketScope scope)
    {
        IEnumerable<GlpiTicketSummary> query = items;
        query = scope switch
        {
            GlpiTicketScope.Open => query.Where(t => GlpiStatusMapper.IsOpenStatus(t.Status)),
            GlpiTicketScope.Last90Days => query.Where(t => t.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-90)),
            _ => query,
        };

        return query.OrderByDescending(t => t.CreatedAt).ToList();
    }

    private List<GlpiTicketSummary> SeedTickets(string requesterEmail)
    {
        if (_ticketsByEmail.TryGetValue(requesterEmail, out var existing))
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var list = new List<GlpiTicketSummary>
        {
            new()
            {
                TicketId = "4821",
                Subject = $"VPN instável — {requesterEmail.Split('@')[0]}",
                Status = "2",
                StatusLabel = "Em atendimento (atribuído)",
                PriorityLabel = "Alta",
                CreatedAt = now.AddDays(-2),
                Url = "https://glpi.dev.local/front/ticket.form.php?id=4821",
                RequesterLabel = requesterEmail,
            },
            new()
            {
                TicketId = "4798",
                Subject = "Solicitação de mouse ergonômico",
                Status = "4",
                StatusLabel = "Pendente",
                PriorityLabel = "Média",
                CreatedAt = now.AddDays(-5),
                Url = "https://glpi.dev.local/front/ticket.form.php?id=4798",
                RequesterLabel = requesterEmail,
            },
            new()
            {
                TicketId = "4500",
                Subject = $"Chamado encerrado — {requesterEmail.Split('@')[0]}",
                Status = "6",
                StatusLabel = "Fechado",
                PriorityLabel = "Baixa",
                CreatedAt = now.AddDays(-120),
                Url = "https://glpi.dev.local/front/ticket.form.php?id=4500",
                RequesterLabel = requesterEmail,
            },
        };

        _ticketsByEmail[requesterEmail] = list;
        return list;
    }
}
