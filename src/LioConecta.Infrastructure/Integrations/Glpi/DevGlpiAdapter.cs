using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Infrastructure.Integrations.Glpi;

public sealed class DevGlpiAdapter : IGlpiAdapter
{
    private int _ticketCounter = 1000;

    public Task<GlpiTicketResult> CreateTicketAsync(
        string title,
        string description,
        string category,
        Guid requesterPersonId,
        CancellationToken cancellationToken = default)
    {
        var ticketId = Interlocked.Increment(ref _ticketCounter).ToString();
        return Task.FromResult(new GlpiTicketResult
        {
            TicketId = ticketId,
            Status = "New",
            Url = $"https://glpi.dev.local/front/ticket.form.php?id={ticketId}"
        });
    }

    public Task<GlpiTicketResult> GetTicketStatusAsync(
        string ticketId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new GlpiTicketResult
        {
            TicketId = ticketId,
            Status = "Processing",
            Url = $"https://glpi.dev.local/front/ticket.form.php?id={ticketId}"
        });
}
