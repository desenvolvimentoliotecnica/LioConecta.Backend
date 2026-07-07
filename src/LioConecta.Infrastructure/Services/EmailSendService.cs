using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class EmailSendService(
    AppDbContext db,
    IEmailQueueService emailQueueService,
    IEmailAttachmentService emailAttachmentService) : IEmailSendService
{
    public async Task<SendEmailResponse> SendAsync(
        SendEmailRequest request,
        Guid senderPersonId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new ArgumentException("Assunto e obrigatorio.", nameof(request));
        }

        var sender = await db.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == senderPersonId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Remetente nao encontrado.");

        if (string.IsNullOrWhiteSpace(sender.Email))
        {
            throw new InvalidOperationException("Seu perfil nao possui e-mail cadastrado para envio.");
        }

        var senderEmail = sender.Email.Trim();
        var senderName = string.IsNullOrWhiteSpace(sender.Name) ? senderEmail : sender.Name.Trim();

        var toAddresses = await ResolveRecipientsAsync(request, sender, cancellationToken);

        var cc = EmailAddressValidator.ParseAndValidate(request.Cc);
        var bcc = EmailAddressValidator.ParseAndValidate(request.Bcc);
        var bodyHtml = EmailHtmlSanitizer.Sanitize(request.BodyHtml);
        var bodyText = EmailHtmlSanitizer.ToPlainText(bodyHtml);

        if (string.IsNullOrWhiteSpace(bodyText))
        {
            throw new ArgumentException("Corpo do e-mail e obrigatorio.", nameof(request));
        }

        var attachments = await emailAttachmentService.ConsumeAsync(
            request.AttachmentIds ?? Array.Empty<Guid>(),
            senderPersonId,
            cancellationToken);

        if (request.DirectAttachments is { Count: > 0 })
        {
            attachments = attachments
                .Concat(request.DirectAttachments)
                .ToList();
        }

        var metadata = JsonSerializer.Serialize(new
        {
            source = string.IsNullOrWhiteSpace(request.Source) ? "compose" : request.Source.Trim(),
            recipientSlug = request.RecipientSlug,
            senderSlug = sender.Slug,
            senderEmail,
            senderName,
            attachmentCount = attachments.Count,
        });

        var enqueued = await emailQueueService.EnqueueAsync(
            new EmailEnqueueRequest(
                toAddresses,
                request.Subject.Trim(),
                bodyHtml,
                bodyText,
                cc,
                bcc,
                attachments,
                MetadataJson: metadata,
                CreatedById: senderPersonId),
            cancellationToken);

        return new SendEmailResponse(enqueued.Id, enqueued.Status);
    }

    private async Task<IReadOnlyList<string>> ResolveRecipientsAsync(
        SendEmailRequest request,
        Domain.Entities.Person sender,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RecipientSlug))
        {
            var recipient = await db.People
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Slug == request.RecipientSlug.Trim() && p.IsActive,
                    cancellationToken)
                ?? throw new InvalidOperationException("Destinatario nao encontrado.");

            if (string.IsNullOrWhiteSpace(recipient.Email))
            {
                throw new InvalidOperationException("Destinatario nao possui e-mail cadastrado.");
            }

            return EmailAddressValidator.ParseAndValidate([recipient.Email]);
        }

        var explicitTo = EmailAddressValidator.ParseAndValidate(request.To);
        if (explicitTo.Count == 0)
        {
            throw new ArgumentException("Informe destinatario ou recipientSlug.", nameof(request));
        }

        return explicitTo;
    }
}
