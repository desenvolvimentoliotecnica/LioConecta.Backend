using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class FeedbackService(
    IFeedbackRepository repository,
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    IPermissionService permissionService,
    INotificationService notificationService) : IFeedbackService
{
    public async Task<FeedbackSubmissionDto> CreateAsync(
        CreateFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("feedback.submit", cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Assunto e mensagem são obrigatórios.");
        }

        var authorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var isPeer = request.TargetPersonId.HasValue;

        Person? target = null;
        if (isPeer)
        {
            target = await personRepository.GetByIdAsync(request.TargetPersonId!.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Pessoa alvo do feedback não encontrada.");

            if (!target.IsActive)
            {
                throw new ArgumentException("Não é possível enviar feedback para uma pessoa inativa.");
            }

            if (target.Id == authorId)
            {
                throw new ArgumentException("Não é possível enviar feedback 1:1 para si mesmo.");
            }
        }

        var item = new FeedbackSubmission
        {
            Id = Guid.NewGuid(),
            AuthorId = isPeer || !request.IsAnonymous ? authorId : null,
            IsAnonymous = isPeer ? false : request.IsAnonymous,
            Category = request.Category,
            Status = FeedbackStatus.Received,
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            TargetPersonId = target?.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await repository.AddAsync(item, cancellationToken);

        // Reload with navigations for DTO + notifications.
        var saved = await repository.GetByIdAsync(item.Id, cancellationToken) ?? item;

        if (isPeer && target is not null)
        {
            var recipientIds = new HashSet<Guid> { target.Id };
            if (target.ManagerId is Guid managerId && managerId != authorId)
            {
                recipientIds.Add(managerId);
            }

            await notificationService.NotifyPeerFeedbackAsync(saved, recipientIds.ToList(), cancellationToken);
        }

        return ToDto(saved);
    }

    public async Task<IReadOnlyList<FeedbackSubmissionDto>> ListAsync(
        FeedbackStatus? status,
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("feedback.triage", cancellationToken: cancellationToken);
        var items = await repository.ListRhChannelAsync(status, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<FeedbackSubmissionDto>> ListMineAsync(
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("feedback.submit", cancellationToken: cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var items = await repository.ListVisibleToPersonAsync(personId, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<FeedbackSubmissionDto> UpdateAsync(
        Guid id,
        UpdateFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("feedback.triage", cancellationToken: cancellationToken);
        var item = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Feedback não encontrado.");

        if (item.TargetPersonId is not null)
        {
            throw new InvalidOperationException("Feedback 1:1 não é gerenciado pela triagem do RH.");
        }

        item.Status = request.Status;
        item.ResponseText = string.IsNullOrWhiteSpace(request.ResponseText) ? null : request.ResponseText.Trim();
        item.AssigneeId = request.AssigneeId;
        item.RespondedAt = item.ResponseText is null ? null : DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    private static FeedbackSubmissionDto ToDto(FeedbackSubmission f) =>
        new(
            f.Id,
            f.Category,
            f.Status,
            f.Subject,
            f.Message,
            f.IsAnonymous,
            f.ResponseText,
            f.AssigneeId,
            f.IsAnonymous || f.Author is null ? null : PersonMapper.ToSummary(f.Author),
            f.CreatedAt,
            f.RespondedAt,
            f.TargetPersonId,
            f.TargetPerson is null ? null : PersonMapper.ToSummary(f.TargetPerson));
}
