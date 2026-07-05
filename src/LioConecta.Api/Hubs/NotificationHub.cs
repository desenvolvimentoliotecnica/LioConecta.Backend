using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LioConecta.Api.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var personKey = ResolvePersonKey(Context.User);
        if (!string.IsNullOrWhiteSpace(personKey))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"person:{personKey}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var personKey = ResolvePersonKey(Context.User);
        if (!string.IsNullOrWhiteSpace(personKey))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"person:{personKey}");
        }

        await base.OnDisconnectedAsync(exception);
    }

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
