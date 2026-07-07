using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface ITeamsChatAdapter
{
    Task<IReadOnlyList<TeamsChatSummary>> ListChatsAsync(
        string accessToken,
        bool includeGroupChats,
        CancellationToken cancellationToken = default);

    Task<TeamsChatPage<TeamsChatMessage>> ListMessagesAsync(
        string accessToken,
        string conversationId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<TeamsChatMessage> SendMessageAsync(
        string accessToken,
        string conversationId,
        string text,
        CancellationToken cancellationToken = default);

    Task<TeamsChatSummary> CreateOneOnOneChatAsync(
        string accessToken,
        string targetUserId,
        CancellationToken cancellationToken = default);

    Task<TeamsGraphUser?> FindUserByEmailAsync(
        string accessToken,
        string email,
        CancellationToken cancellationToken = default);
}
