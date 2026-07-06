using LioConecta.Application.Interfaces.Integrations;

using LioConecta.Application.Interfaces.Integrations.Models;



namespace LioConecta.Infrastructure.Integrations.Glpi;



public sealed class DevGlpiAdapter : IGlpiAdapter

{

    private int _ticketCounter = 4820;



    private readonly Dictionary<string, List<GlpiTicketSummary>> _ticketsByEmail = new(StringComparer.OrdinalIgnoreCase);



    public Task<GlpiTicketResult> CreateTicketAsync(

        string title,

        string description,

        string priority,

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



    public Task<IReadOnlyList<GlpiItilCategory>> GetItilCategoriesAsync(

        CancellationToken cancellationToken = default) =>

        Task.FromResult<IReadOnlyList<GlpiItilCategory>>(

        [

            new() { Id = 1, Name = "Hardware", FullName = "Hardware" },

            new() { Id = 2, Name = "Software", FullName = "Software" },

            new() { Id = 3, Name = "Acesso / Senha", FullName = "Acesso / Senha" },

            new() { Id = 4, Name = "Rede / VPN", FullName = "Rede / VPN" },

            new() { Id = 5, Name = "Outros", FullName = "Outros" },

        ]);



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


