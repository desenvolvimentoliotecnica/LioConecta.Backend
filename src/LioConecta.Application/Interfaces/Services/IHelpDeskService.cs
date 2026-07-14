using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IHelpDeskService
{
    Task<HelpDeskSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<HelpDeskServiceDto> GetServices();

    Task<IReadOnlyList<HelpDeskKnowledgeArticleDto>> GetKnowledgeAsync(
        string? query = null,
        CancellationToken cancellationToken = default);

    Task<HelpDeskTicketResultDto> CreateTicketAsync(
        CreateHelpDeskTicketRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HelpDeskFormCategoryDto>> GetFormCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HelpDeskFormSummaryDto>> GetFormsAsync(
        int? categoryId = null,
        CancellationToken cancellationToken = default);

    Task<HelpDeskFormSchemaDto?> GetFormSchemaAsync(
        int formId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HelpDeskAreaDto>> GetAreasAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HelpDeskGlpiEntityDto>> GetEntitiesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HelpDeskItilCategoryDto>> GetCategoriesAsync(
        string areaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HelpDeskTicketListItemDto>> GetMyTicketsAsync(
        string scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HelpDeskTicketListItemDto>> GetAllTicketsAsync(
        string scope,
        CancellationToken cancellationToken = default);

    Task<HelpDeskTicketDetailDto?> GetTicketDetailAsync(
        string ticketId,
        CancellationToken cancellationToken = default);

    Task<(byte[] Content, string ContentType, string FileName)?> GetTicketAttachmentAsync(
        string ticketId,
        string documentId,
        CancellationToken cancellationToken = default);

    Task UploadTicketAttachmentAsync(
        string ticketId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);
}
