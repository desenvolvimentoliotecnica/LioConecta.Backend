using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface INotificationBroadcaster
{
    Task SendToPersonAsync(string personGroupKey, NotificationDto notification, CancellationToken cancellationToken = default);
}
