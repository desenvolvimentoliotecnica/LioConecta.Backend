using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IHelpDeskService
{
    Task<HelpDeskSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<HelpDeskServiceDto> GetServices();

    IReadOnlyList<HelpDeskKnowledgeArticleDto> GetKnowledge(string? query = null);

    Task<HelpDeskTicketResultDto> CreateTicketAsync(
        CreateHelpDeskTicketRequestDto request,
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
}
