using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LioConecta.Api.Services;

public sealed class SignalRNotificationBroadcaster(IHubContext<NotificationHub> hubContext) : INotificationBroadcaster
{
    public Task SendToPersonAsync(
        string personGroupKey,
        NotificationDto notification,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group($"person:{personGroupKey}")
            .SendAsync("NotificationReceived", notification, cancellationToken);
}
