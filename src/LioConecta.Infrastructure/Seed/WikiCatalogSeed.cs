using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Infrastructure.Seed;

internal static class WikiCatalogSeed
{
    internal sealed record CatalogEntry(
        Guid Id,
        string Slug,
        string Title,
        string Summary,
        string Category,
        DateTimeOffset UpdatedAt,
        params string[] Paragraphs);

    internal static readonly CatalogEntry[] Entries =
    [
        new(
            Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddd001"),
            "vpn-instavel",
            "VPN instável ou desconectando",
            "Passos para reconectar VPN corporativa no Windows e macOS.",
            "acesso",
            new DateTimeOffset(2026, 7, 10, 18, 0, 0, TimeSpan.Zero),
            "Confirme que a rede local está estável e que o horário do sistema está sincronizado.",
            "No Windows, abra o cliente VPN corporativo, desconecte a sessão atual e reconecte com o perfil padrão. Se falhar, reinicie o serviço de VPN e tente novamente.",
            "No macOS, remova o perfil VPN problemático em Preferências do Sistema, reinstale o pacote corporativo e autentique com as credenciais de rede.",
            "Se o problema persistir após duas tentativas, abra um chamado no Help Desk informando sistema operacional, horário do incidente e prints de erro."),
        new(
            Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddd002"),
            "reset-senha",
            "Redefinir senha de rede",
            "Fluxo de reset de senha AD via portal de autoatendimento.",
            "acesso",
            new DateTimeOffset(2026, 7, 7, 17, 0, 0, TimeSpan.Zero),
            "Acesse o portal de autoatendimento de senha com o usuário corporativo e o fator de autenticação cadastrado (SMS ou aplicativo).",
            "Escolha uma senha que atenda à política: mínimo de 12 caracteres, letras maiúsculas e minúsculas, número e símbolo.",
            "Após a troca, aguarde até 5 minutos para sincronização e faça logoff/logon no Windows para renovar o ticket Kerberos.",
            "Em caso de bloqueio de conta ou fator de autenticação indisponível, contate o Help Desk com documento e matrícula."),
        new(
            Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddd003"),
            "impressora-offline",
            "Impressora corporativa offline",
            "Verificar fila, driver e conexão de rede da impressora.",
            "hardware",
            new DateTimeOffset(2026, 7, 4, 14, 0, 0, TimeSpan.Zero),
            "Verifique no painel da impressora se há papel, toner e mensagens de erro. Remova eventuais atolamentos.",
            "No Windows, abra Dispositivos e Impressoras, limpe a fila de impressão e defina a impressora corporativa como padrão.",
            "Confirme que o cabo de rede ou Wi-Fi corporativo está ativo e que o IP da impressora responde ao ping da estação.",
            "Se a fila continuar offline, reinstale o driver oficial pelo catálogo de softwares e abra chamado com patrimônio e localização."),
        new(
            Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddd004"),
            "outlook-sync",
            "Outlook não sincroniza",
            "Soluções para caixa de entrada travada ou perfil corrompido.",
            "software",
            new DateTimeOffset(2026, 6, 30, 19, 0, 0, TimeSpan.Zero),
            "Verifique a conectividade com a internet e o status do Microsoft 365. Aguarde a sincronização automática por alguns minutos.",
            "No Outlook, use Enviar/Receber → Atualizar pastas. Se permanecer travado, feche o aplicativo e limpe o cache do OST após backup das pastas locais.",
            "Como alternativa, crie um novo perfil de e-mail no Painel de Controle → Mail e configure novamente a conta corporativa.",
            "Se o problema for apenas em um calendário ou pasta compartilhada, remova e readicione a pasta. Persistindo, abra chamado com prints e versão do Outlook."),
        new(
            Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddd005"),
            "troca-notebook",
            "Solicitar troca de notebook",
            "Critérios de elegibilidade e prazos para substituição de equipamento.",
            "hardware",
            new DateTimeOffset(2026, 6, 27, 13, 0, 0, TimeSpan.Zero),
            "A troca é elegível quando o equipamento completa o ciclo de vida, apresenta falha recorrente ou há mudança de perfil de trabalho aprovada pelo gestor.",
            "Abra a solicitação em Serviços → Solicitar equipamento, anexe o número de patrimônio e descreva o motivo com evidências (laudo, prints ou ticket anterior).",
            "O prazo padrão de análise é de até 5 dias úteis. Após aprovação, a entrega segue o calendário de logística de TI.",
            "Até a entrega, mantenha backup no OneDrive corporativo e não formate o equipamento sem orientação do suporte."),
        new(
            Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddd006"),
            "wifi-corporativo",
            "Conectar à rede Wi-Fi corporativa",
            "Configuração de certificado e autenticação 802.1X.",
            "acesso",
            new DateTimeOffset(2026, 6, 22, 12, 30, 0, TimeSpan.Zero),
            "Conecte-se à SSID corporativa indicada no onboarding. Em redes 802.1X, use o usuário de rede no formato dominio\\usuario.",
            "Aceite o certificado raiz corporativo quando solicitado pelo sistema operacional. Não ignore alertas de certificado inválido.",
            "No Windows, confirme o perfil EAP em Configurações de Wi-Fi → Propriedades. No macOS, use o perfil de configuração distribuído pela TI.",
            "Se a autenticação falhar, remova o perfil antigo, reinstale o pacote de certificados e tente novamente. Persistindo, abra chamado no Help Desk."),
    ];

    internal static WikiArticle ToEntity(CatalogEntry entry, Guid authorId, DateTimeOffset seedTime)
    {
        var body = string.Join(
            string.Empty,
            entry.Paragraphs.Select(p => $"<p>{System.Net.WebUtility.HtmlEncode(p)}</p>"));

        return new WikiArticle
        {
            Id = entry.Id,
            Slug = entry.Slug,
            Title = entry.Title,
            Summary = entry.Summary,
            Category = entry.Category,
            BodyHtml = body,
            Status = WikiArticleStatus.Published,
            AuthorId = authorId,
            PublishedAt = entry.UpdatedAt,
            CreatedAt = entry.UpdatedAt < seedTime ? entry.UpdatedAt : seedTime,
            UpdatedAt = entry.UpdatedAt,
        };
    }

    internal static IEnumerable<WikiArticle> CreateAll(DateTimeOffset seedTime, Guid authorId) =>
        Entries.Select(e => ToEntity(e, authorId, seedTime));
}
