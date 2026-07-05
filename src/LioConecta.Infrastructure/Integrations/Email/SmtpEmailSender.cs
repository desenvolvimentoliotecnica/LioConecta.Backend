using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LioConecta.Infrastructure.Integrations.Email;

public sealed class SmtpEmailSender : ISmtpEmailSender
{
    public async Task<SmtpSendResult> SendAsync(
        EmailRuntimeConfiguration config,
        SmtpSendRequest request,
        CancellationToken cancellationToken)
    {
        if (!config.IsEnabled)
        {
            return new SmtpSendResult(false, null, "Envio de e-mail desabilitado na configuracao.");
        }

        if (string.IsNullOrWhiteSpace(config.SmtpHost))
        {
            return new SmtpSendResult(false, null, "Host SMTP nao configurado.");
        }

        try
        {
            var message = await BuildMessageAsync(config, request, cancellationToken);
            using var client = new SmtpClient
            {
                Timeout = config.TimeoutSeconds * 1000,
            };

            await client.ConnectAsync(
                config.SmtpHost,
                config.SmtpPort,
                config.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(config.SmtpUsername))
            {
                await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword, cancellationToken);
            }

            var response = await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return new SmtpSendResult(true, message.MessageId ?? response, null);
        }
        catch (Exception ex)
        {
            return new SmtpSendResult(false, null, ex.Message);
        }
    }

    public async Task<EmailConnectionTestResponse> TestAsync(
        EmailRuntimeConfiguration config,
        string? testRecipient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.SmtpHost))
        {
            return new EmailConnectionTestResponse(false, "Host SMTP obrigatorio.", null);
        }

        try
        {
            using var client = new SmtpClient
            {
                Timeout = config.TimeoutSeconds * 1000,
            };

            await client.ConnectAsync(
                config.SmtpHost,
                config.SmtpPort,
                config.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(config.SmtpUsername))
            {
                await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(testRecipient))
            {
                var testMessage = await BuildMessageAsync(
                    config,
                    new SmtpSendRequest(
                        [testRecipient.Trim()],
                        [],
                        [],
                        "LioConecta — teste SMTP",
                        "<p>Este e um e-mail de teste enviado pelo LioConecta.</p>",
                        "Este e um e-mail de teste enviado pelo LioConecta."),
                    cancellationToken);

                await client.SendAsync(testMessage, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
                return new EmailConnectionTestResponse(
                    true,
                    $"Conexao SMTP OK. E-mail de teste enviado para {testRecipient.Trim()}.",
                    null);
            }

            await client.DisconnectAsync(true, cancellationToken);
            return new EmailConnectionTestResponse(true, "Conexao SMTP autenticada com sucesso.", null);
        }
        catch (Exception ex)
        {
            return new EmailConnectionTestResponse(false, "Falha ao conectar ou autenticar no SMTP.", ex.Message);
        }
    }

    private static async Task<MimeMessage> BuildMessageAsync(
        EmailRuntimeConfiguration config,
        SmtpSendRequest request,
        CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        var fromAddress = string.IsNullOrWhiteSpace(config.FromAddress)
            ? config.SmtpUsername
            : config.FromAddress;

        message.From.Add(new MailboxAddress(config.FromName, fromAddress));
        message.Subject = request.Subject;

        foreach (var to in request.To)
        {
            message.To.Add(MailboxAddress.Parse(to));
        }

        foreach (var cc in request.Cc)
        {
            message.Cc.Add(MailboxAddress.Parse(cc));
        }

        foreach (var bcc in request.Bcc)
        {
            message.Bcc.Add(MailboxAddress.Parse(bcc));
        }

        var builder = new BodyBuilder();
        if (!string.IsNullOrWhiteSpace(request.BodyHtml))
        {
            builder.HtmlBody = request.BodyHtml;
        }

        if (!string.IsNullOrWhiteSpace(request.BodyText))
        {
            builder.TextBody = request.BodyText;
        }

        if (string.IsNullOrWhiteSpace(builder.HtmlBody) && string.IsNullOrWhiteSpace(builder.TextBody))
        {
            builder.TextBody = request.Subject;
        }

        if (request.Attachments is { Count: > 0 })
        {
            foreach (var attachment in request.Attachments)
            {
                if (string.IsNullOrWhiteSpace(attachment.StoragePath) || !File.Exists(attachment.StoragePath))
                {
                    continue;
                }

                await using var stream = File.OpenRead(attachment.StoragePath);
                await builder.Attachments.AddAsync(
                    attachment.FileName,
                    stream,
                    ContentType.Parse(attachment.ContentType),
                    cancellationToken);
            }
        }

        message.Body = builder.ToMessageBody();
        return message;
    }
}
