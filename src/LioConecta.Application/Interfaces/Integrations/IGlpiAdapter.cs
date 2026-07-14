using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface IGlpiAdapter
{
    Task<GlpiTicketResult> CreateTicketAsync(
        string title,
        string description,
        string priority,
        int entityId,
        int categoryId,
        string requesterEmail,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlpiEntity>> GetEntitiesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlpiItilCategory>> GetAllItilCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlpiItilCategory>> GetItilCategoriesAsync(
        int entityId,
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

    Task<GlpiOpenQueueCounts> CountOpenQueueAsync(
        CancellationToken cancellationToken = default);

    Task<GlpiTicketDetail?> GetTicketDetailAsync(
        string ticketId,
        string requesterEmail,
        bool skipOwnershipCheck = false,
        CancellationToken cancellationToken = default);

    Task<GlpiTicketAttachmentContent?> GetTicketAttachmentAsync(
        string ticketId,
        string documentId,
        string requesterEmail,
        bool skipOwnershipCheck = false,
        CancellationToken cancellationToken = default);

    Task UploadTicketDocumentAsync(
        string ticketId,
        string fileName,
        string contentType,
        Stream content,
        string requesterEmail,
        bool skipOwnershipCheck = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlpiFormCategory>> GetFormCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlpiFormSummary>> GetFormsAsync(
        int? categoryId = null,
        CancellationToken cancellationToken = default);

    Task<GlpiFormSchema?> GetFormSchemaAsync(
        int formId,
        CancellationToken cancellationToken = default);

    Task<GlpiTicketResult> CreateTicketFromFormAnswersAsync(
        int formId,
        int entityId,
        IReadOnlyList<GlpiFormAnswerInput> answers,
        string? subjectOverride,
        string requesterEmail,
        CancellationToken cancellationToken = default);
}
