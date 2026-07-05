using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Security;
using LioConecta.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Services;

public sealed class EmailConfigurationService(
    AppDbContext db,
    ISmtpEmailSender smtpEmailSender,
    ILogger<EmailConfigurationService> logger) : IEmailConfigurationService
{
    private const string EncryptionKey = "LioConecta.Backend::EmailConfiguration::Secret::v1";

    public async Task<EmailConfigurationDto> GetAsync(CancellationToken cancellationToken)
    {
        var entity = await EnsureAndGetEntityAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task<EmailConfigurationDto> SaveAsync(
        UpsertEmailConfigurationRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var entity = await EnsureAndGetEntityAsync(cancellationToken);

        entity.IsEnabled = request.IsEnabled;
        entity.FromAddress = Normalize(request.FromAddress);
        entity.FromName = Normalize(request.FromName);
        entity.SmtpHost = Normalize(request.SmtpHost);
        entity.SmtpPort = request.SmtpPort is > 0 and <= 65535 ? request.SmtpPort : 587;
        entity.SmtpUsername = Normalize(request.SmtpUsername);
        entity.UseStartTls = request.UseStartTls;
        entity.TimeoutSeconds = Clamp(request.TimeoutSeconds, 5, 300, 30);
        entity.MaxAttempts = Clamp(request.MaxAttempts, 1, 20, 5);
        entity.InitialRetryDelaySeconds = Clamp(request.InitialRetryDelaySeconds, 10, 86400, 60);
        entity.MaxRetryDelaySeconds = Clamp(request.MaxRetryDelaySeconds, 60, 604800, 21600);
        entity.DispatchBatchSize = Clamp(request.DispatchBatchSize, 1, 100, 20);
        entity.DispatchIntervalSeconds = Clamp(request.DispatchIntervalSeconds, 5, 3600, 30);
        entity.UpdatedById = updatedById;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.SmtpPassword))
        {
            entity.SmtpPasswordProtected = SecretProtector.Protect(request.SmtpPassword.Trim(), EncryptionKey);
        }

        await db.SaveChangesAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task<EmailRuntimeConfiguration> GetRuntimeConfigurationAsync(CancellationToken cancellationToken)
    {
        var entity = await EnsureAndGetEntityAsync(cancellationToken);
        return MapToRuntime(entity);
    }

    public async Task<EmailConnectionTestResponse> TestConnectionAsync(
        EmailSmtpTestRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await EnsureAndGetEntityAsync(cancellationToken);
        var password = !string.IsNullOrWhiteSpace(request.SmtpPassword)
            ? request.SmtpPassword.Trim()
            : TryUnprotectPassword(entity);

        var runtime = new EmailRuntimeConfiguration(
            request.IsEnabled,
            Normalize(request.FromAddress),
            Normalize(request.FromName),
            Normalize(request.SmtpHost),
            request.SmtpPort is > 0 and <= 65535 ? request.SmtpPort : entity.SmtpPort,
            Normalize(request.SmtpUsername),
            password,
            request.UseStartTls,
            Clamp(request.TimeoutSeconds, 5, 300, entity.TimeoutSeconds),
            entity.MaxAttempts,
            entity.InitialRetryDelaySeconds,
            entity.MaxRetryDelaySeconds,
            entity.DispatchBatchSize,
            entity.DispatchIntervalSeconds);

        return await smtpEmailSender.TestAsync(runtime, request.TestRecipient, cancellationToken);
    }

    public async Task EnsureDefaultConfigurationAsync(CancellationToken cancellationToken)
    {
        await EnsureAndGetEntityAsync(cancellationToken);
    }

    private async Task<EmailConfiguration> EnsureAndGetEntityAsync(CancellationToken cancellationToken)
    {
        var entity = await db.EmailConfigurations.FirstOrDefaultAsync(cancellationToken);
        if (entity is not null)
        {
            return entity;
        }

        var now = DateTimeOffset.UtcNow;
        entity = new EmailConfiguration
        {
            Id = Guid.NewGuid(),
            IsEnabled = EmailConfigurationDefaults.DevIsEnabled,
            FromAddress = EmailConfigurationDefaults.DevFromAddress,
            FromName = EmailConfigurationDefaults.DevFromName,
            SmtpHost = EmailConfigurationDefaults.DevSmtpHost,
            SmtpPort = EmailConfigurationDefaults.DevSmtpPort,
            SmtpUsername = EmailConfigurationDefaults.DevSmtpUsername,
            SmtpPasswordProtected = null,
            UseStartTls = EmailConfigurationDefaults.DevUseStartTls,
            TimeoutSeconds = 30,
            MaxAttempts = 5,
            InitialRetryDelaySeconds = 60,
            MaxRetryDelaySeconds = 21600,
            DispatchBatchSize = 20,
            DispatchIntervalSeconds = 30,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.EmailConfigurations.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static EmailConfigurationDto MapToDto(EmailConfiguration entity) =>
        new(
            entity.Id,
            entity.IsEnabled,
            entity.FromAddress,
            entity.FromName,
            entity.SmtpHost,
            entity.SmtpPort,
            entity.SmtpUsername,
            !string.IsNullOrWhiteSpace(entity.SmtpPasswordProtected),
            entity.UseStartTls,
            entity.TimeoutSeconds,
            entity.MaxAttempts,
            entity.InitialRetryDelaySeconds,
            entity.MaxRetryDelaySeconds,
            entity.DispatchBatchSize,
            entity.DispatchIntervalSeconds,
            entity.UpdatedAt);

    private EmailRuntimeConfiguration MapToRuntime(EmailConfiguration entity) =>
        new(
            entity.IsEnabled,
            entity.FromAddress,
            entity.FromName,
            entity.SmtpHost,
            entity.SmtpPort,
            entity.SmtpUsername,
            TryUnprotectPassword(entity),
            entity.UseStartTls,
            entity.TimeoutSeconds,
            entity.MaxAttempts,
            entity.InitialRetryDelaySeconds,
            entity.MaxRetryDelaySeconds,
            entity.DispatchBatchSize,
            entity.DispatchIntervalSeconds);

    private string? TryUnprotectPassword(EmailConfiguration entity)
    {
        if (string.IsNullOrWhiteSpace(entity.SmtpPasswordProtected))
        {
            return null;
        }

        try
        {
            return SecretProtector.Unprotect(entity.SmtpPasswordProtected, EncryptionKey);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Falha ao descriptografar senha SMTP.");
            return null;
        }
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static int Clamp(int value, int min, int max, int fallback) =>
        value >= min && value <= max ? value : fallback;
}
