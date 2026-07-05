using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Integrations.TotvsRm;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace LioConecta.Infrastructure.Services;

public sealed class TotvsRmConfigurationService(
    AppDbContext db,
    TotvsRmConnectionTester connectionTester,
    ILogger<TotvsRmConfigurationService> logger) : ITotvsRmConfigurationService
{
    private const string EncryptionKey = "LioConecta.Backend::TotvsRmConfiguration::Secret::v1";

    public async Task<TotvsRmConfigurationDto> GetAsync(CancellationToken cancellationToken)
    {
        var entity = await EnsureAndGetEntityAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task<TotvsRmConfigurationDto> SaveAsync(
        UpsertTotvsRmConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await EnsureAndGetEntityAsync(cancellationToken);

        entity.IsEnabled = request.IsEnabled;
        entity.Server = Normalize(request.Server);
        entity.Port = request.Port is > 0 and <= 65535 ? request.Port : 1433;
        entity.Database = Normalize(request.Database);
        entity.UserName = Normalize(request.UserName);
        entity.TrustServerCertificate = request.TrustServerCertificate;
        entity.TimesheetPeriodStartDay = NormalizePeriodDay(
            request.TimesheetPeriodStartDay,
            TimesheetPeriodResolver.DefaultStartDay);
        entity.TimesheetPeriodEndDay = NormalizePeriodDay(
            request.TimesheetPeriodEndDay,
            TimesheetPeriodResolver.DefaultEndDay);
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            entity.PasswordProtected = ProtectSecret(request.Password.Trim());
        }

        await db.SaveChangesAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task<TotvsRmRuntimeConfiguration> GetRuntimeConfigurationAsync(CancellationToken cancellationToken)
    {
        var entity = await EnsureAndGetEntityAsync(cancellationToken);
        return new TotvsRmRuntimeConfiguration(
            entity.IsEnabled,
            entity.Server,
            entity.Port,
            entity.Database,
            entity.UserName,
            TryUnprotectSecret(entity),
            entity.TrustServerCertificate,
            NormalizePeriodDay(entity.TimesheetPeriodStartDay, TimesheetPeriodResolver.DefaultStartDay),
            NormalizePeriodDay(entity.TimesheetPeriodEndDay, TimesheetPeriodResolver.DefaultEndDay));
    }

    public async Task<TotvsRmConnectionTestResponse> TestConnectionAsync(
        UpsertTotvsRmConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await EnsureAndGetEntityAsync(cancellationToken);
        var password = !string.IsNullOrWhiteSpace(request.Password)
            ? request.Password.Trim()
            : TryUnprotectSecret(entity);

        var runtime = new TotvsRmRuntimeConfiguration(
            true,
            Normalize(request.Server),
            request.Port is > 0 and <= 65535 ? request.Port : entity.Port,
            Normalize(request.Database),
            Normalize(request.UserName),
            password,
            request.TrustServerCertificate,
            NormalizePeriodDay(request.TimesheetPeriodStartDay, entity.TimesheetPeriodStartDay > 0
                ? entity.TimesheetPeriodStartDay
                : TimesheetPeriodResolver.DefaultStartDay),
            NormalizePeriodDay(request.TimesheetPeriodEndDay, entity.TimesheetPeriodEndDay > 0
                ? entity.TimesheetPeriodEndDay
                : TimesheetPeriodResolver.DefaultEndDay));

        return await connectionTester.TestAsync(runtime, cancellationToken);
    }

    public async Task EnsureDefaultConfigurationAsync(CancellationToken cancellationToken)
    {
        await EnsureAndGetEntityAsync(cancellationToken);
    }

    private async Task<TotvsRmConfiguration> EnsureAndGetEntityAsync(CancellationToken cancellationToken)
    {
        var entity = await db.TotvsRmConfigurations.FirstOrDefaultAsync(cancellationToken);
        if (entity is not null)
        {
            return entity;
        }

        entity = new TotvsRmConfiguration
        {
            Id = Guid.NewGuid(),
            IsEnabled = false,
            Server = string.Empty,
            Port = 1433,
            Database = string.Empty,
            UserName = string.Empty,
            PasswordProtected = null,
            TrustServerCertificate = true,
            TimesheetPeriodStartDay = TimesheetPeriodResolver.DefaultStartDay,
            TimesheetPeriodEndDay = TimesheetPeriodResolver.DefaultEndDay,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.TotvsRmConfigurations.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static TotvsRmConfigurationDto MapToDto(TotvsRmConfiguration entity)
    {
        return new TotvsRmConfigurationDto(
            entity.Id,
            entity.IsEnabled,
            entity.Server,
            entity.Port,
            entity.Database,
            entity.UserName,
            !string.IsNullOrWhiteSpace(entity.PasswordProtected),
            entity.TrustServerCertificate,
            NormalizePeriodDay(entity.TimesheetPeriodStartDay, TimesheetPeriodResolver.DefaultStartDay),
            NormalizePeriodDay(entity.TimesheetPeriodEndDay, TimesheetPeriodResolver.DefaultEndDay),
            entity.UpdatedAt);
    }

    private static int NormalizePeriodDay(int value, int fallback) =>
        value is >= 1 and <= 28 ? value : fallback;

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string ProtectSecret(string value)
    {
        var plainBytes = Encoding.UTF8.GetBytes(value);
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(EncryptionKey));
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var output = new MemoryStream();

        output.Write(aes.IV, 0, aes.IV.Length);

        using (var cryptoStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
        {
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
            cryptoStream.FlushFinalBlock();
        }

        return Convert.ToBase64String(output.ToArray());
    }

    private string? TryUnprotectSecret(TotvsRmConfiguration entity)
    {
        if (string.IsNullOrWhiteSpace(entity.PasswordProtected))
        {
            return null;
        }

        try
        {
            return UnprotectSecret(entity.PasswordProtected);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Falha ao descriptografar senha TOTVS RM. A integracao sera tratada como indisponivel.");
            return null;
        }
    }

    private static string? UnprotectSecret(string protectedValue)
    {
        var protectedBytes = Convert.FromBase64String(protectedValue);
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(EncryptionKey));

        var ivLength = aes.BlockSize / 8;
        var iv = protectedBytes.Take(ivLength).ToArray();
        var cipherBytes = protectedBytes.Skip(ivLength).ToArray();
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var input = new MemoryStream(cipherBytes);
        using var cryptoStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
