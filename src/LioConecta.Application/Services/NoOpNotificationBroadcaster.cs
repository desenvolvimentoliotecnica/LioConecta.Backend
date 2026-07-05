using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Application.Services;

public sealed class NoOpNotificationBroadcaster : INotificationBroadcaster
{
    public Task SendToPersonAsync(
        string personGroupKey,
        NotificationDto notification,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
