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
    IPersonRepository personRepository,
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
        var readerId = comunicado.Slug ?? comunicado.Id.ToString();
        var href = $"/comunicados/leitura?id={Uri.EscapeDataString(readerId)}";
        var title = "Novo comunicado oficial";
        var body = comunicado.Title.Trim();

        await BroadcastToAllActivePersonsAsync(
            NotificationType.Comunicado,
            title,
            body,
            href,
            cancellationToken);
    }

    public Task NotifyPollCreatedAsync(
        FeedPost post,
        Poll poll,
        CancellationToken cancellationToken = default)
    {
        var href = "/feed";
        var title = "Nova enquete no feed";
        var body = poll.Question.Trim();

        return BroadcastToAllPersonsAsync(
            NotificationType.Feed,
            title,
            body,
            href,
            cancellationToken);
    }

    public Task NotifyPollClosedAsync(
        FeedPost post,
        Poll poll,
        CancellationToken cancellationToken = default)
    {
        var href = "/feed";
        var title = "Enquete encerrada";
        var body = poll.Question.Trim();

        return BroadcastToAllPersonsAsync(
            NotificationType.Feed,
            title,
            body,
            href,
            cancellationToken);
    }

    public async Task NotifyLeaveRequestCreatedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid leaveRecordId,
        string summary,
        CancellationToken cancellationToken = default,
        string? title = null)
    {
        var recipients = await personRepository.GetByIdsAsync(recipientPersonIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var href = $"/servicos/ferias-ausencias/gestao?requestId={leaveRecordId}";
        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.ServiceRequest,
            string.IsNullOrWhiteSpace(title) ? "Nova solicitação de férias" : title.Trim(),
            summary.Trim(),
            href,
            cancellationToken);
    }

    public async Task NotifyBirthdayCongratsAsync(
        FeedPost post,
        Person celebrated,
        Person author,
        CancellationToken cancellationToken = default)
    {
        // Sempre notifica o parabenizado — inclusive se autor == celebrado (auto-parabéns).
        var now = DateTimeOffset.UtcNow;
        var title = celebrated.Id == author.Id
            ? "Você publicou parabéns no feed"
            : $"{author.Name} te parabenizou";
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            PersonId = celebrated.Id,
            Type = NotificationType.Birthday,
            Title = title,
            Body = post.Content.Trim(),
            Href = $"/?post={post.Id}",
            IsRead = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await notificationRepository.AddRangeAsync([notification], cancellationToken);

        var dto = NotificationMapper.ToDto(notification);
        try
        {
            await notificationBroadcaster.SendToPersonAsync(
                PersonGroupKey.Resolve(celebrated),
                dto,
                cancellationToken);
        }
        catch
        {
            // Real-time delivery is best-effort; notifications are persisted regardless.
        }
    }

    public async Task NotifyFeedPostLikedAsync(
        FeedPost post,
        Person liker,
        CancellationToken cancellationToken = default)
    {
        if (post.AuthorId == liker.Id)
        {
            return;
        }

        var author = await personRepository.GetByIdAsync(post.AuthorId, cancellationToken);
        if (author is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var body = post.Content.Trim();
        if (body.Length > 200)
        {
            body = $"{body[..197]}...";
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            PersonId = author.Id,
            Type = NotificationType.Feed,
            Title = $"{liker.Name.Trim()} curtiu sua publicação",
            Body = string.IsNullOrWhiteSpace(body) ? "Veja no feed." : body,
            Href = $"/?post={post.Id}",
            IsRead = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await notificationRepository.AddRangeAsync([notification], cancellationToken);

        var dto = NotificationMapper.ToDto(notification);
        try
        {
            await notificationBroadcaster.SendToPersonAsync(
                PersonGroupKey.Resolve(author),
                dto,
                cancellationToken);
        }
        catch
        {
            // Real-time delivery is best-effort; notifications are persisted regardless.
        }
    }

    public async Task NotifyFeedPostCommentedAsync(
        FeedPost post,
        Person commenter,
        string commentText,
        CancellationToken cancellationToken = default)
    {
        if (post.AuthorId == commenter.Id)
        {
            return;
        }

        var author = await personRepository.GetByIdAsync(post.AuthorId, cancellationToken);
        if (author is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var body = commentText.Trim();
        if (body.Length > 200)
        {
            body = $"{body[..197]}...";
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            PersonId = author.Id,
            Type = NotificationType.Feed,
            Title = $"{commenter.Name.Trim()} comentou sua publicação",
            Body = string.IsNullOrWhiteSpace(body) ? "Veja o comentário no feed." : body,
            Href = $"/?post={post.Id}",
            IsRead = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await notificationRepository.AddRangeAsync([notification], cancellationToken);

        var dto = NotificationMapper.ToDto(notification);
        try
        {
            await notificationBroadcaster.SendToPersonAsync(
                PersonGroupKey.Resolve(author),
                dto,
                cancellationToken);
        }
        catch
        {
            // Real-time delivery is best-effort; notifications are persisted regardless.
        }
    }

    public async Task NotifyUniLioCourseSubmittedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid courseId,
        string courseTitle,
        string submitterName,
        CancellationToken cancellationToken = default)
    {
        var recipients = await personRepository.GetByIdsAsync(recipientPersonIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var href = $"/unilio/admin/aprovacoes/{courseId}";
        var body = $"{submitterName.Trim()} enviou o curso \"{courseTitle.Trim()}\" para aprovação.";
        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.System,
            "Curso aguardando aprovação",
            body,
            href,
            cancellationToken);
    }

    public async Task NotifyUniLioCourseReviewedAsync(
        Guid instructorPersonId,
        Guid courseId,
        string courseTitle,
        bool approved,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var instructor = await personRepository.GetByIdAsync(instructorPersonId, cancellationToken);
        if (instructor is null)
        {
            return;
        }

        var href = $"/unilio/instrutor/curso/{courseId}/editar";
        var title = approved ? "Curso aprovado" : "Curso rejeitado";
        var body = approved
            ? $"Seu curso \"{courseTitle.Trim()}\" foi aprovado e publicado."
            : $"Seu curso \"{courseTitle.Trim()}\" foi rejeitado."
              + (string.IsNullOrWhiteSpace(rejectionReason) ? "" : $" Motivo: {rejectionReason.Trim()}");

        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([instructor]),
            NotificationType.System,
            title,
            body,
            href,
            cancellationToken);
    }

    public Task NotifyUniLioCoursePublishedAsync(
        UniLioCourse course,
        CancellationToken cancellationToken = default)
    {
        var href = $"/unilio/curso/{course.Id}";
        return BroadcastToAllActivePersonsAsync(
            NotificationType.System,
            "Novo curso disponível",
            course.Title.Trim(),
            href,
            cancellationToken);
    }

    public async Task NotifyUniLioCourseCompletedToInstructorAsync(
        Guid instructorPersonId,
        string learnerName,
        string courseTitle,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var instructor = await personRepository.GetByIdAsync(instructorPersonId, cancellationToken);
        if (instructor is null)
        {
            return;
        }

        var href = $"/unilio/instrutor/curso/{courseId}/editar";
        var body = $"{learnerName.Trim()} concluiu o curso \"{courseTitle.Trim()}\".";
        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([instructor]),
            NotificationType.System,
            "Aluno concluiu seu curso",
            body,
            href,
            cancellationToken);
    }

    public async Task NotifyUniLioQuestionToInstructorAsync(
        Guid instructorPersonId,
        string learnerName,
        string courseTitle,
        string? moduleTitle,
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        var instructor = await personRepository.GetByIdAsync(instructorPersonId, cancellationToken);
        if (instructor is null)
        {
            return;
        }

        var context = string.IsNullOrWhiteSpace(moduleTitle)
            ? $"curso \"{courseTitle.Trim()}\""
            : $"módulo \"{moduleTitle.Trim()}\" do curso \"{courseTitle.Trim()}\"";
        var href = $"/unilio/instrutor/duvidas?question={questionId}";
        var body = $"{learnerName.Trim()} enviou uma dúvida sobre o {context}.";
        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([instructor]),
            NotificationType.System,
            "Nova dúvida de aluno",
            body,
            href,
            cancellationToken);
    }

    public async Task NotifyUniLioQuestionAnsweredToLearnerAsync(
        Guid learnerPersonId,
        string courseTitle,
        string? moduleTitle,
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        var learner = await personRepository.GetByIdAsync(learnerPersonId, cancellationToken);
        if (learner is null)
        {
            return;
        }

        var context = string.IsNullOrWhiteSpace(moduleTitle)
            ? $"curso \"{courseTitle.Trim()}\""
            : $"módulo \"{moduleTitle.Trim()}\"";
        var href = $"/unilio/minhas-duvidas?question={questionId}";
        var body = $"O instrutor respondeu sua dúvida sobre o {context}.";
        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([learner]),
            NotificationType.System,
            "Resposta à sua dúvida",
            body,
            href,
            cancellationToken);
    }

    private Task BroadcastToAllActivePersonsAsync(
        NotificationType type,
        string title,
        string body,
        string href,
        CancellationToken cancellationToken) =>
        BroadcastAsync(
            () => notificationRepository.GetActivePersonsAsync(cancellationToken),
            type,
            title,
            body,
            href,
            cancellationToken);

    private Task BroadcastToAllPersonsAsync(
        NotificationType type,
        string title,
        string body,
        string href,
        CancellationToken cancellationToken) =>
        BroadcastAsync(
            () => notificationRepository.GetAllPersonsAsync(cancellationToken),
            type,
            title,
            body,
            href,
            cancellationToken);

    private async Task BroadcastAsync(
        Func<Task<IReadOnlyList<Person>>> getPersons,
        NotificationType type,
        string title,
        string body,
        string href,
        CancellationToken cancellationToken)
    {
        var persons = await getPersons();
        if (persons.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var notifications = persons.Select(person => new Notification
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Type = type,
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
