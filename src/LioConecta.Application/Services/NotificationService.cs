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

    public Task NotifyNewsPublishedAsync(
        FeedPost post,
        CancellationToken cancellationToken = default)
    {
        var metadata = JsonMapper.DeserializeObjectDictionary(post.MetadataJson);
        metadata.TryGetValue("title", out var titleObj);
        var newsTitle = titleObj?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(newsTitle))
        {
            newsTitle = TruncateBody(post.Content, 120);
        }

        var body = string.IsNullOrWhiteSpace(newsTitle) ? "Veja a nova notícia no feed." : newsTitle;
        return BroadcastToAllActivePersonsAsync(
            NotificationType.Feed,
            "Nova notícia",
            body,
            $"/?post={post.Id}",
            cancellationToken);
    }

    public async Task NotifyPeerFeedbackAsync(
        FeedbackSubmission feedback,
        IReadOnlyList<Guid> recipientPersonIds,
        CancellationToken cancellationToken = default)
    {
        if (recipientPersonIds.Count == 0)
        {
            return;
        }

        var recipients = await personRepository.GetByIdsAsync(recipientPersonIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var body = string.IsNullOrWhiteSpace(feedback.Subject)
            ? TruncateBody(feedback.Message, 160)
            : feedback.Subject.Trim();

        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.ServiceRequest,
            "Novo feedback 1:1",
            body,
            $"/feedback?tab=meus&id={feedback.Id}",
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
            string.IsNullOrWhiteSpace(title) ? "Nova solicitaÃ§Ã£o de fÃ©rias" : title.Trim(),
            summary.Trim(),
            href,
            cancellationToken);
    }

    public async Task NotifyLeaveRequestDecisionAsync(
        Guid requesterPersonId,
        Guid leaveRecordId,
        string serviceKey,
        string periodLabel,
        bool approved,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var requester = await personRepository.GetByIdAsync(requesterPersonId, cancellationToken);
        if (requester is null)
        {
            return;
        }

        var isMedical = string.Equals(serviceKey, "atestado", StringComparison.OrdinalIgnoreCase);
        var period = string.IsNullOrWhiteSpace(periodLabel) ? "sem período" : periodLabel.Trim();
        string title;
        string body;
        if (isMedical)
        {
            title = approved ? "Atestado médico aprovado" : "Atestado médico rejeitado";
            body = approved
                ? $"Seu atestado médico ({period}) foi aprovado."
                : $"Seu atestado médico ({period}) foi rejeitado."
                  + (string.IsNullOrWhiteSpace(reason) ? "" : $" Motivo: {reason.Trim()}");
        }
        else
        {
            title = approved ? "Solicitação de férias aprovada" : "Solicitação de férias rejeitada";
            body = approved
                ? $"Sua solicitação de férias ({period}) foi aprovada."
                : $"Sua solicitação de férias ({period}) foi rejeitada."
                  + (string.IsNullOrWhiteSpace(reason) ? "" : $" Motivo: {reason.Trim()}");
        }

        var href = $"/servicos/ferias-ausencias?requestId={leaveRecordId}";
        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([requester]),
            NotificationType.ServiceRequest,
            title,
            body,
            href,
            cancellationToken);
    }

    public async Task NotifyPontoAdjustmentCreatedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid adjustmentRecordId,
        string summary,
        CancellationToken cancellationToken = default,
        string? title = null)
    {
        var recipients = await personRepository.GetByIdsAsync(recipientPersonIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var href = $"/servicos/ponto-eletronico/gestao?requestId={adjustmentRecordId}";
        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.ServiceRequest,
            string.IsNullOrWhiteSpace(title) ? "Nova solicitação de ajuste de ponto" : title.Trim(),
            summary.Trim(),
            href,
            cancellationToken);
    }

    public async Task NotifyPontoAdjustmentDecisionAsync(
        Guid requesterPersonId,
        Guid adjustmentRecordId,
        string dayLabel,
        bool approved,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var requester = await personRepository.GetByIdAsync(requesterPersonId, cancellationToken);
        if (requester is null)
        {
            return;
        }

        var days = string.IsNullOrWhiteSpace(dayLabel) ? "ajuste" : dayLabel.Trim();
        var title = approved ? "Ajuste de ponto aprovado" : "Ajuste de ponto rejeitado";
        var body = approved
            ? $"Sua solicitação de ajuste de ponto ({days}) foi aprovada."
            : $"Sua solicitação de ajuste de ponto ({days}) foi rejeitada."
              + (string.IsNullOrWhiteSpace(reason) ? "" : $" Motivo: {reason.Trim()}");

        var href = $"/servicos/ponto-eletronico?requestId={adjustmentRecordId}";
        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([requester]),
            NotificationType.ServiceRequest,
            title,
            body,
            href,
            cancellationToken);
    }

    public async Task NotifyServiceRequestCreatedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid serviceRequestId,
        string summary,
        CancellationToken cancellationToken = default,
        string? title = null)
    {
        var recipients = await personRepository.GetByIdsAsync(recipientPersonIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var href = $"/servicos/solicitacoes-rh?requestId={serviceRequestId}";
        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.ServiceRequest,
            string.IsNullOrWhiteSpace(title) ? "Novo pedido de RH" : title.Trim(),
            summary.Trim(),
            href,
            cancellationToken);
    }

    public async Task NotifyServiceRequestDecisionAsync(
        Guid requesterPersonId,
        Guid serviceRequestId,
        string requestType,
        bool approved,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var requester = await personRepository.GetByIdAsync(requesterPersonId, cancellationToken);
        if (requester is null)
        {
            return;
        }

        var typeLabel = requestType.ToLowerInvariant() switch
        {
            "servicos-beneficios" => "benefício",
            "servicos-contracheque" => "contracheque",
            _ => "RH",
        };
        var title = approved ? $"Pedido de {typeLabel} aprovado" : $"Pedido de {typeLabel} rejeitado";
        var body = approved
            ? $"Seu pedido de {typeLabel} foi aprovado."
            : $"Seu pedido de {typeLabel} foi rejeitado."
              + (string.IsNullOrWhiteSpace(reason) ? "" : $" Motivo: {reason.Trim()}");
        var href = $"/servicos/solicitacoes-rh?mine=1&requestId={serviceRequestId}";

        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([requester]),
            NotificationType.ServiceRequest,
            title,
            body,
            href,
            cancellationToken);
    }

    public async Task NotifyServiceRequestMessageAsync(
        Guid recipientPersonId,
        Guid serviceRequestId,
        string requestType,
        string actorName,
        bool fromRh,
        string preview,
        CancellationToken cancellationToken = default)
    {
        var recipient = await personRepository.GetByIdAsync(recipientPersonId, cancellationToken);
        if (recipient is null)
        {
            return;
        }

        var typeLabel = ServiceRequestTypeLabel(requestType);
        var safeActor = string.IsNullOrWhiteSpace(actorName) ? (fromRh ? "RH" : "Colaborador") : actorName.Trim();
        var title = fromRh
            ? $"Nova resposta do RH — {typeLabel}"
            : $"Nova resposta do colaborador — {typeLabel}";
        var body = string.IsNullOrWhiteSpace(preview)
            ? $"{safeActor} enviou uma mensagem no pedido de {typeLabel}."
            : $"{safeActor}: {preview.Trim()}";
        var href = fromRh
            ? $"/servicos/solicitacoes-rh?mine=1&requestId={serviceRequestId}"
            : $"/servicos/solicitacoes-rh?requestId={serviceRequestId}";

        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([recipient]),
            NotificationType.ServiceRequest,
            title,
            body.Length > 280 ? $"{body[..277]}…" : body,
            href,
            cancellationToken);
    }

    public async Task NotifyServiceRequestFinalizedAsync(
        Guid requesterPersonId,
        Guid serviceRequestId,
        string requestType,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var requester = await personRepository.GetByIdAsync(requesterPersonId, cancellationToken);
        if (requester is null)
        {
            return;
        }

        var typeLabel = ServiceRequestTypeLabel(requestType);
        var body = $"O RH finalizou seu pedido de {typeLabel}. Confirme o encerramento para concluir."
                   + (string.IsNullOrWhiteSpace(comment) ? "" : $" Observação: {comment.Trim()}");
        var href = $"/servicos/solicitacoes-rh?mine=1&requestId={serviceRequestId}";

        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([requester]),
            NotificationType.ServiceRequest,
            $"Pedido finalizado — confirme o encerramento",
            body.Length > 280 ? $"{body[..277]}…" : body,
            href,
            cancellationToken);
    }

    public async Task NotifyServiceRequestClosureConfirmedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid serviceRequestId,
        string requestType,
        string requesterName,
        CancellationToken cancellationToken = default)
    {
        var recipients = await personRepository.GetByIdsAsync(recipientPersonIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var typeLabel = ServiceRequestTypeLabel(requestType);
        var safeName = string.IsNullOrWhiteSpace(requesterName) ? "Colaborador" : requesterName.Trim();
        var href = $"/servicos/solicitacoes-rh?requestId={serviceRequestId}";

        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.ServiceRequest,
            $"Encerramento confirmado — {typeLabel}",
            $"{safeName} confirmou o encerramento do pedido de {typeLabel}.",
            href,
            cancellationToken);
    }

    private static string ServiceRequestTypeLabel(string requestType) => requestType.ToLowerInvariant() switch
    {
        "servicos-beneficios" => "benefício",
        "servicos-contracheque" => "contracheque",
        _ => "RH",
    };

    public async Task NotifyBirthdayCongratsAsync(
        FeedPost post,
        Person celebrated,
        Person author,
        CancellationToken cancellationToken = default)
    {
        // Sempre notifica o parabenizado â€” inclusive se autor == celebrado (auto-parabÃ©ns).
        var now = DateTimeOffset.UtcNow;
        var title = celebrated.Id == author.Id
            ? "VocÃª publicou parabÃ©ns no feed"
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

    public Task NotifyNewHireAsync(FeedPost post, Person newHire, CancellationToken cancellationToken = default) =>
        BroadcastToAllActivePersonsAsync(NotificationType.Feed, "Novo colaborador", $"D? as boas-vindas a {newHire.Name.Trim()}!", $"/?post={post.Id}", cancellationToken);

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
            Title = $"{liker.Name.Trim()} curtiu sua publicaÃ§Ã£o",
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
            Title = $"{commenter.Name.Trim()} comentou sua publicaÃ§Ã£o",
            Body = string.IsNullOrWhiteSpace(body) ? "Veja o comentÃ¡rio no feed." : body,
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
        var body = $"{submitterName.Trim()} enviou o curso \"{courseTitle.Trim()}\" para aprovaÃ§Ã£o.";
        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.System,
            "Curso aguardando aprovaÃ§Ã£o",
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
            "Novo curso disponÃ­vel",
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
            : $"mÃ³dulo \"{moduleTitle.Trim()}\" do curso \"{courseTitle.Trim()}\"";
        var href = $"/unilio/instrutor/duvidas?question={questionId}";
        var body = $"{learnerName.Trim()} enviou uma dÃºvida sobre o {context}.";
        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([instructor]),
            NotificationType.System,
            "Nova dÃºvida de aluno",
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
            : $"mÃ³dulo \"{moduleTitle.Trim()}\"";
        var href = $"/unilio/minhas-duvidas?question={questionId}";
        var body = $"O instrutor respondeu sua dÃºvida sobre o {context}.";
        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([learner]),
            NotificationType.System,
            "Resposta Ã  sua dÃºvida",
            body,
            href,
            cancellationToken);
    }

    public async Task NotifyGroupCreationRequestedAsync(
        Guid managerPersonId,
        Guid groupId,
        string groupName,
        string requesterName,
        CancellationToken cancellationToken = default)
    {
        var manager = await personRepository.GetByIdAsync(managerPersonId, cancellationToken);
        if (manager is null)
        {
            return;
        }

        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([manager]),
            NotificationType.System,
            "AprovaÃ§Ã£o de grupo pendente",
            $"{requesterName.Trim()} solicitou a criaÃ§Ã£o do grupo \"{groupName.Trim()}\".",
            "/grupos/aprovacoes",
            cancellationToken);
    }

    public async Task NotifyGroupCreationDecisionAsync(
        Guid ownerPersonId,
        Guid groupId,
        string groupName,
        bool approved,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var owner = await personRepository.GetByIdAsync(ownerPersonId, cancellationToken);
        if (owner is null)
        {
            return;
        }

        var title = approved ? "Grupo aprovado" : "Grupo rejeitado";
        var body = approved
            ? $"Seu grupo \"{groupName.Trim()}\" foi aprovado."
            : $"Seu grupo \"{groupName.Trim()}\" foi rejeitado."
              + (string.IsNullOrWhiteSpace(reason) ? "" : $" Motivo: {reason.Trim()}");
        var href = approved ? $"/grupos/{groupId}" : "/grupos/meus-grupos";
        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([owner]),
            NotificationType.System,
            title,
            body,
            href,
            cancellationToken);
    }

    public async Task NotifyGroupCreationExpiredAsync(
        Guid ownerPersonId,
        Guid groupId,
        string groupName,
        CancellationToken cancellationToken = default)
    {
        var owner = await personRepository.GetByIdAsync(ownerPersonId, cancellationToken);
        if (owner is null)
        {
            return;
        }

        await BroadcastAsync(
            () => Task.FromResult<IReadOnlyList<Person>>([owner]),
            NotificationType.System,
            "Pedido de grupo expirado",
            $"O pedido do grupo \"{groupName.Trim()}\" expirou. VocÃª pode solicitar aprovaÃ§Ã£o novamente.",
            "/grupos/meus-grupos",
            cancellationToken);
    }

    public async Task NotifyGroupWallPostAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid groupId,
        string groupName,
        string authorName,
        CancellationToken cancellationToken = default)
    {
        if (recipientPersonIds.Count == 0)
        {
            return;
        }

        var recipients = await personRepository.GetByIdsAsync(recipientPersonIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.Feed,
            "Novo post no mural",
            $"{authorName.Trim()} publicou no grupo \"{groupName.Trim()}\".",
            $"/grupos/{groupId}",
            cancellationToken);
    }

    public async Task NotifyGroupTopicCreatedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid groupId,
        Guid topicId,
        string groupName,
        string topicTitle,
        string authorName,
        CancellationToken cancellationToken = default)
    {
        if (recipientPersonIds.Count == 0)
        {
            return;
        }

        var recipients = await personRepository.GetByIdsAsync(recipientPersonIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.Feed,
            "Novo tÃ³pico no grupo",
            $"{authorName.Trim()} criou \"{topicTitle.Trim()}\" em \"{groupName.Trim()}\".",
            $"/grupos/{groupId}?tab=topicos&topic={topicId}",
            cancellationToken);
    }

    public async Task NotifyGroupTopicReplyAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid groupId,
        Guid topicId,
        string groupName,
        string topicTitle,
        string authorName,
        CancellationToken cancellationToken = default)
    {
        if (recipientPersonIds.Count == 0)
        {
            return;
        }

        var recipients = await personRepository.GetByIdsAsync(recipientPersonIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.Feed,
            "Nova resposta em tÃ³pico",
            $"{authorName.Trim()} respondeu em \"{topicTitle.Trim()}\" ({groupName.Trim()}).",
            $"/grupos/{groupId}?tab=topicos&topic={topicId}",
            cancellationToken);
    }

    public async Task NotifyGroupOwnershipTransferRequestedAsync(
        Guid toPersonId,
        Guid managerPersonId,
        Guid groupId,
        string groupName,
        string fromOwnerName,
        string toPersonName,
        CancellationToken cancellationToken = default)
    {
        var recipients = await personRepository.GetByIdsAsync([toPersonId, managerPersonId], cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.System,
            "TransferÃªncia de dono de grupo",
            $"{fromOwnerName.Trim()} quer transferir a direÃ§Ã£o de \"{groupName.Trim()}\" para {toPersonName.Trim()}.",
            "/grupos/aprovacoes",
            cancellationToken);
    }

    public async Task NotifyGroupOwnershipTransferDecisionAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid groupId,
        string groupName,
        bool approved,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (recipientPersonIds.Count == 0)
        {
            return;
        }

        var recipients = await personRepository.GetByIdsAsync(recipientPersonIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var title = approved ? "TransferÃªncia de dono aprovada" : "TransferÃªncia de dono rejeitada";
        var body = approved
            ? $"A direÃ§Ã£o do grupo \"{groupName.Trim()}\" foi transferida."
            : $"A transferÃªncia do grupo \"{groupName.Trim()}\" foi rejeitada."
              + (string.IsNullOrWhiteSpace(reason) ? "" : $" Motivo: {reason.Trim()}");
        await BroadcastAsync(
            () => Task.FromResult(recipients),
            NotificationType.System,
            title,
            body,
            $"/grupos/{groupId}",
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

    private static string TruncateBody(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        }

        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized[..(maxLength - 1)].TrimEnd() + "…";
    }
}
