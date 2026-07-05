using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LioConecta.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public override async Task OnConnectedAsync()
    {
        var personKey = ResolvePersonKey(Context.User);
        if (!string.IsNullOrWhiteSpace(personKey))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"person:{personKey}");
        }

        await base.OnConnectedAsync();
    }

    private static string ConversationGroup(string conversationId) => $"conversation:{conversationId}";

    private static string? ResolvePersonKey(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return null;
        }

        return user.FindFirstValue("oid")
            ?? user.FindFirstValue("person_slug")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
