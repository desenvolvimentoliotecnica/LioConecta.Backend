using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class NotificationService(
    INotificationRepository notificationRepository,
    INotificationBroadcaster notificationBroadcaster,
    ICurrentUserService currentUserService) : INotificationService
{
    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var page = await notificationRepository.GetPageAsync(personId, request, cancellationToken);
        var items = page.Items.Select(NotificationMapper.ToDto).ToList();
        return PagedResult<NotificationDto>.FromItems(items, page.NextCursor, page.HasMore);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return await notificationRepository.GetUnreadCountAsync(personId, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await notificationRepository.MarkAsReadAsync(id, personId, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await notificationRepository.MarkAllAsReadAsync(personId, cancellationToken);
    }

    public async Task NotifyComunicadoCreatedAsync(
        Comunicado comunicado,
        CancellationToken cancellationToken = default)
    {
        var persons = await notificationRepository.GetActivePersonsAsync(cancellationToken);
        if (persons.Count == 0)
        {
            return;
        }

        var readerId = comunicado.Slug ?? comunicado.Id.ToString();
        var href = $"/comunicados/leitura?id={Uri.EscapeDataString(readerId)}";
        var title = "Novo comunicado oficial";
        var body = comunicado.Title.Trim();
        var now = DateTimeOffset.UtcNow;

        var notifications = persons.Select(person => new Notification
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Type = NotificationType.Comunicado,
            Title = title,
            Body = body,
            Href = href,
            IsRead = false,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();

        await notificationRepository.AddRangeAsync(notifications, cancellationToken);

        var personById = persons.ToDictionary(p => p.Id);

        foreach (var notification in notifications)
        {
            if (!personById.TryGetValue(notification.PersonId, out var person))
            {
                continue;
            }

            var dto = NotificationMapper.ToDto(notification);

            try
            {
                await notificationBroadcaster.SendToPersonAsync(
                    PersonGroupKey.Resolve(person),
                    dto,
                    cancellationToken);
            }
            catch
            {
                // Real-time delivery is best-effort; notifications are persisted regardless.
            }
        }
    }
}
