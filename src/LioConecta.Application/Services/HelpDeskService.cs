using LioConecta.Application.Common;
using LioConecta.Application.Common.Integrations;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LioConecta.Application.Services;

public sealed class HelpDeskService(
    IServiceRequestRepository serviceRequestRepository,
    IServiceRequestService serviceRequestService,
    ICurrentUserService currentUserService,
    IPersonRepository personRepository,
    IAppSettingsProvider appSettings,
    IGlpiAdapter glpiAdapter,
    IWikiService wikiService,
    ILogger<HelpDeskService> logger) : IHelpDeskService
{
    public const string HelpDeskType = "servicos-help-desk";

    public async Task<HelpDeskSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var email = await GetCurrentUserEmailAsync(cancellationToken);
        var tickets = await glpiAdapter.SearchTicketsByRequesterAsync(
            email,
            GlpiTicketScope.Open,
            cancellationToken);

        return new HelpDeskSummaryDto(tickets.Count, "2h críticos · 8h solicitações", await CanViewAllGlpiTicketsAsync(cancellationToken));
    }

    public IReadOnlyList<HelpDeskServiceDto> GetServices()
    {
        var portalBase = ResolvePortalBaseUrl();
        return
        [
            new(
                "abrir-chamado",
                "Abrir chamado",
                "Registre um novo incidente ou solicitação com prioridade, categoria e descrição detalhada.",
                "incidente",
                "Portal GLPI",
                "disponivel",
                true,
                "Solicitar",
                "Informe assunto, prioridade e descrição detalhada. Anexos podem ser enviados após abertura pelo portal GLPI.",
                $"{portalBase}/front/helpdesk.public.php"),
            new(
                "acompanhar-ticket",
                "Acompanhar ticket",
                "Consulte status, histórico e mensagens dos seus chamados abertos nos últimos 90 dias.",
                "solicitacao",
                "Service Desk",
                "disponivel",
                false,
                "Consultar",
                "Chamados abertos nos últimos 90 dias aparecem aqui. Para histórico completo, acesse o portal GLPI.",
                $"{portalBase}/front/ticket.php"),
            new(
                "base-conhecimento",
                "Base de conhecimento",
                "Artigos, tutoriais e soluções para problemas frequentes de hardware, software e acesso.",
                "duvida",
                "Wiki TI",
                "disponivel",
                false,
                "Consultar",
                "Busque por palavra-chave antes de abrir chamado. Muitos problemas comuns já possuem solução documentada.",
                "/documentos/wiki"),
        ];
    }

    public Task<IReadOnlyList<HelpDeskKnowledgeArticleDto>> GetKnowledgeAsync(
        string? query = null,
        CancellationToken cancellationToken = default) =>
        wikiService.GetPublishedKnowledgeAsync(query, cancellationToken);

    public async Task<HelpDeskTicketResultDto> CreateTicketAsync(
        CreateHelpDeskTicketRequestDto request,
        CancellationToken cancellationToken = default)
    {
        HelpDeskTicketCreateValidator.Validate(request);

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Person profile not found.");

        var subject = request.Subject.Trim();
        var description = request.Description.Trim();
        var priority = request.Priority.Trim();

        var glpiResult = await glpiAdapter.CreateTicketAsync(
            subject,
            description,
            priority,
            request.EntityId,
            request.CategoryId,
            person.Email,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(glpiResult.TicketId) ||
            string.Equals(glpiResult.Status, "Error", StringComparison.OrdinalIgnoreCase))
        {
            throw new GlpiIntegrationException("Não foi possível registrar o chamado no GLPI.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["subject"] = subject,
            ["priority"] = priority,
            ["entityId"] = request.EntityId,
            ["categoryId"] = request.CategoryId,
            ["description"] = description,
        };

        var created = await serviceRequestService.CreateAsync(
            new CreateServiceRequestRequest(HelpDeskType, ServiceCategory.TI, payload),
            cancellationToken);

        await serviceRequestRepository.SetExternalRefAsync(
            created.Id,
            glpiResult.TicketId,
            "TI — Service Desk",
            cancellationToken);

        return new HelpDeskTicketResultDto(
            created.Id,
            created.Status.ToString(),
            $"Chamado registrado com sucesso. Protocolo GLPI #{glpiResult.TicketId}.",
            glpiResult.TicketId,
            glpiResult.Url);
    }

    public async Task<IReadOnlyList<HelpDeskAreaDto>> GetAreasAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await glpiAdapter.GetEntitiesAsync(cancellationToken);
        var allCategories = await glpiAdapter.GetAllItilCategoriesAsync(cancellationToken);

        if (entities.Count == 0)
        {
            logger.LogWarning("GLPI não retornou entidades para o wizard de Help Desk.");
        }

        return entities
            .Select(entity =>
            {
                var entityCategories = allCategories
                    .Where(category => category.EntityId == entity.Id)
                    .ToList();

                return new HelpDeskAreaDto(
                    entity.Id.ToString(CultureInfo.InvariantCulture),
                    string.IsNullOrWhiteSpace(entity.Name) ? $"Entidade {entity.Id}" : entity.Name.Trim(),
                    HelpDeskGlpiAreaMapper.ResolveEntityIcon(entity.Name),
                    HelpDeskGlpiAreaMapper.CountSelectableLeaves(entityCategories),
                    entity.Id);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<HelpDeskGlpiEntityDto>> GetEntitiesAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await glpiAdapter.GetEntitiesAsync(cancellationToken);
        return entities
            .Select(e => new HelpDeskGlpiEntityDto(e.Id, e.Name, e.FullName, e.ParentId, e.HasChildren))
            .ToList();
    }

    public async Task<IReadOnlyList<HelpDeskItilCategoryDto>> GetCategoriesAsync(
        string areaId,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(areaId?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var entityId) ||
            entityId <= 0)
        {
            throw new ArgumentException("Área inválida. Informe o ID da entidade GLPI.");
        }

        var categories = await glpiAdapter.GetItilCategoriesAsync(entityId, cancellationToken);

        return categories
            .Select(c => new HelpDeskItilCategoryDto(c.Id, c.Name, c.FullName, c.ParentId, c.HasChildren, c.EntityId))
            .ToList();
    }

    public async Task<IReadOnlyList<HelpDeskTicketListItemDto>> GetMyTicketsAsync(
        string scope,
        CancellationToken cancellationToken = default)
    {
        var email = await GetCurrentUserEmailAsync(cancellationToken);
        var glpiScope = ParseScope(scope);
        var tickets = await glpiAdapter.SearchTicketsByRequesterAsync(email, glpiScope, cancellationToken);
        return tickets.Select(MapListItem).ToList();
    }

    public async Task<IReadOnlyList<HelpDeskTicketListItemDto>> GetAllTicketsAsync(
        string scope,
        CancellationToken cancellationToken = default)
    {
        if (!await CanViewAllGlpiTicketsAsync(cancellationToken))
        {
            throw new UnauthorizedAccessException("User is not allowed to view all GLPI tickets.");
        }

        var glpiScope = ParseScope(scope);
        var tickets = await glpiAdapter.SearchAllTicketsAsync(glpiScope, cancellationToken);
        return tickets.Select(MapListItem).ToList();
    }

    public async Task<HelpDeskTicketDetailDto?> GetTicketDetailAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        var email = await GetCurrentUserEmailAsync(cancellationToken);
        var canViewAll = await CanViewAllGlpiTicketsAsync(cancellationToken);
        var detail = await glpiAdapter.GetTicketDetailAsync(ticketId, email, canViewAll, cancellationToken);
        if (detail is null)
        {
            return null;
        }

        HelpDeskTicketResolutionDto? resolution = detail.Solution is null
            ? null
            : new HelpDeskTicketResolutionDto(
                detail.Solution.Content,
                detail.Solution.ResolvedAt,
                detail.Solution.Author);

        return new HelpDeskTicketDetailDto(
            MapListItem(detail.Summary),
            detail.Description,
            detail.Assignee,
            resolution,
            detail.Followups
                .Select(f => new HelpDeskTicketEventDto(f.Kind, f.Content, f.CreatedAt, f.Author))
                .ToList(),
            detail.Attachments
                .Select(a => new HelpDeskTicketAttachmentDto(a.DocumentId, a.FileName, a.ContentType, a.SizeBytes))
                .ToList());
    }

    public async Task<(byte[] Content, string ContentType, string FileName)?> GetTicketAttachmentAsync(
        string ticketId,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var email = await GetCurrentUserEmailAsync(cancellationToken);
        var canViewAll = await CanViewAllGlpiTicketsAsync(cancellationToken);
        var file = await glpiAdapter.GetTicketAttachmentAsync(
            ticketId,
            documentId,
            email,
            canViewAll,
            cancellationToken);
        if (file is null)
        {
            return null;
        }

        return (file.Content, file.ContentType, file.FileName);
    }

    private async Task<string> GetCurrentUserEmailAsync(CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Person profile not found.");
        return person.Email;
    }

    private string ResolvePortalBaseUrl()
    {
        var portal = appSettings.GetString(AppSettingKeys.GlpiPortalUrl);
        if (!string.IsNullOrWhiteSpace(portal))
        {
            return portal.TrimEnd('/');
        }

        var apiBase = appSettings.GetString(AppSettingKeys.GlpiBaseUrl);
        if (string.IsNullOrWhiteSpace(apiBase))
        {
            return "https://servicedesk.liotecnica.com.br";
        }

        var uri = new Uri(apiBase.TrimEnd('/'));
        return $"{uri.Scheme}://{uri.Host}";
    }

    private static GlpiTicketScope ParseScope(string scope) =>
        scope.Trim().ToLowerInvariant() switch
        {
            "all" => GlpiTicketScope.All,
            "90d" => GlpiTicketScope.Last90Days,
            _ => GlpiTicketScope.Open,
        };

    private static HelpDeskTicketListItemDto MapListItem(GlpiTicketSummary ticket) =>
        new(
            ticket.TicketId,
            ticket.Subject,
            ticket.Status,
            ticket.StatusLabel,
            ticket.PriorityLabel,
            ticket.CreatedAt,
            ticket.Url,
            ticket.RequesterLabel);

    private static readonly HashSet<string> AllTicketsViewerEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "leonardo.mendes@liotecnica.com.br",
    };

    private async Task<bool> CanViewAllGlpiTicketsAsync(CancellationToken cancellationToken)
    {
        var email = await GetCurrentUserEmailAsync(cancellationToken);
        if (AllTicketsViewerEmails.Contains(email))
        {
            return true;
        }

        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        return roles.Contains(UserRole.TI) || roles.Contains(UserRole.Admin);
    }
}
