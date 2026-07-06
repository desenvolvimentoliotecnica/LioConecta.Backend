using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface IGlpiAdapter
{
    Task<GlpiTicketResult> CreateTicketAsync(
        string title,
        string description,
        string priority,
        int categoryId,
        string requesterEmail,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlpiItilCategory>> GetItilCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<GlpiTicketResult> GetTicketStatusAsync(
        string ticketId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlpiTicketSummary>> SearchTicketsByRequesterAsync(
        string requesterEmail,
        GlpiTicketScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlpiTicketSummary>> SearchAllTicketsAsync(
        GlpiTicketScope scope,
        CancellationToken cancellationToken = default);

    Task<GlpiTicketDetail?> GetTicketDetailAsync(
        string ticketId,
        string requesterEmail,
        bool skipOwnershipCheck = false,
        CancellationToken cancellationToken = default);
}
