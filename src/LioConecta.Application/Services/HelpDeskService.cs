using LioConecta.Application.Common;
using LioConecta.Application.Common.Integrations;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class HelpDeskService(
    IServiceRequestRepository serviceRequestRepository,
    IServiceRequestService serviceRequestService,
    ICurrentUserService currentUserService,
    IPersonRepository personRepository,
    IAppSettingsProvider appSettings,
    IGlpiAdapter glpiAdapter) : IHelpDeskService
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
                "https://wiki.dev.local/ti"),
            new(
                "chat-ao-vivo",
                "Chat ao vivo",
                "Atendimento síncrono com analista de plantão em horário comercial estendido (7h–22h).",
                "urgente",
                "Teams TI",
                "disponivel",
                false,
                "Abrir",
                "Canal recomendado para incidentes urgentes em horário comercial. Fora do horário, use telefone plantão.",
                "https://teams.microsoft.com/l/channel/ti-suporte"),
            new(
                "email-suporte",
                "E-mail suporte",
                "Canal assíncrono para demandas não urgentes. Resposta em até 1 dia útil.",
                "solicitacao",
                "ti.suporte@liotecnica.com.br",
                "disponivel",
                false,
                "Abrir",
                "Inclua prints, número de patrimônio e horário do incidente. Resposta em até 1 dia útil.",
                "mailto:ti.suporte@liotecnica.com.br"),
            new(
                "telefone-plantao",
                "Telefone plantão",
                "Linha exclusiva para incidentes críticos que impactam produção ou segurança.",
                "urgente",
                "Ramal 5500",
                "disponivel",
                false,
                "Consultar",
                "Use apenas para incidentes críticos (produção parada, vazamento de dados, indisponibilidade total).",
                null),
        ];
    }

    public IReadOnlyList<HelpDeskKnowledgeArticleDto> GetKnowledge(string? query = null)
    {
        var normalized = query?.Trim().ToLowerInvariant();
        var catalog = KnowledgeCatalog;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return catalog;
        }

        return catalog
            .Where(article =>
                article.Title.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                article.Summary.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                article.Category.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

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
        var definitions = HelpDeskAreaCatalog.Parse(
            appSettings.GetString(AppSettingKeys.HelpDeskGlpiAreas));
        var allCategories = await glpiAdapter.GetAllItilCategoriesAsync(cancellationToken);

        return definitions
            .Select(area =>
            {
                var areaCategories = HelpDeskAreaCatalog.ResolveAreaCategories(area, allCategories);
                var serviceCount = HelpDeskAreaCatalog.CountSelectableServices(area, areaCategories);
                return new HelpDeskAreaDto(area.Id, area.Name, area.Icon, serviceCount, area.EntityId);
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
        if (string.IsNullOrWhiteSpace(areaId))
        {
            throw new ArgumentException("Área inválida.");
        }

        var definitions = HelpDeskAreaCatalog.Parse(
            appSettings.GetString(AppSettingKeys.HelpDeskGlpiAreas));
        var area = definitions.FirstOrDefault(item =>
                        item.Id.Equals(areaId.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Área não encontrada.");

        var allCategories = await glpiAdapter.GetAllItilCategoriesAsync(cancellationToken);
        var areaCategories = HelpDeskAreaCatalog.ResolveAreaCategories(area, allCategories);
        var built = HelpDeskItilCategoryTreeBuilder.Build(areaCategories, dedupeByLabel: false);

        return built
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

        return new HelpDeskTicketDetailDto(
            MapListItem(detail.Summary),
            detail.Description,
            detail.Assignee,
            detail.Followups
                .Select(f => new HelpDeskTicketEventDto(f.Content, f.CreatedAt, f.Author))
                .ToList());
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

    private static readonly IReadOnlyList<HelpDeskKnowledgeArticleDto> KnowledgeCatalog =
    [
        new("kb-vpn", "VPN instável ou desconectando", "Passos para reconectar VPN corporativa no Windows e macOS.", "acesso", DateTimeOffset.UtcNow.AddDays(-2), "https://wiki.dev.local/ti/vpn-instavel"),
        new("kb-senha", "Redefinir senha de rede", "Fluxo de reset de senha AD via portal de autoatendimento.", "acesso", DateTimeOffset.UtcNow.AddDays(-5), "https://wiki.dev.local/ti/reset-senha"),
        new("kb-impressora", "Impressora corporativa offline", "Verificar fila, driver e conexão de rede da impressora.", "hardware", DateTimeOffset.UtcNow.AddDays(-8), "https://wiki.dev.local/ti/impressora-offline"),
        new("kb-email", "Outlook não sincroniza", "Soluções para caixa de entrada travada ou perfil corrompido.", "software", DateTimeOffset.UtcNow.AddDays(-12), "https://wiki.dev.local/ti/outlook-sync"),
        new("kb-notebook", "Solicitar troca de notebook", "Critérios de elegibilidade e prazos para substituição de equipamento.", "hardware", DateTimeOffset.UtcNow.AddDays(-15), "https://wiki.dev.local/ti/troca-notebook"),
        new("kb-wifi", "Conectar à rede Wi-Fi corporativa", "Configuração de certificado e autenticação 802.1X.", "acesso", DateTimeOffset.UtcNow.AddDays(-20), "https://wiki.dev.local/ti/wifi-corporativo"),
    ];
}
