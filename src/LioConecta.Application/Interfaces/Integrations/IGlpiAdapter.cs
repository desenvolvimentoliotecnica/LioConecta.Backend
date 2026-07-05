using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface IGlpiAdapter
{
    Task<GlpiTicketResult> CreateTicketAsync(
        string title,
        string description,
        string category,
        Guid requesterPersonId,
        CancellationToken cancellationToken = default);

    Task<GlpiTicketResult> GetTicketStatusAsync(string ticketId, CancellationToken cancellationToken = default);
}
