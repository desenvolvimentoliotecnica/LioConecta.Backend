using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public static class EmailMessageMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static EmailMessageDto ToDto(EmailMessage entity)
    {
        return new EmailMessageDto(
            entity.Id,
            entity.Status.ToString(),
            DeserializeAddresses(entity.ToAddressesJson),
            DeserializeAddresses(entity.CcAddressesJson),
            DeserializeAddresses(entity.BccAddressesJson),
            entity.Subject,
            entity.BodyHtml,
            entity.BodyText,
            entity.TemplateKey,
            entity.MetadataJson,
            entity.Priority,
            entity.IdempotencyKey,
            entity.CorrelationId,
            entity.AttemptCount,
            entity.MaxAttempts,
            entity.LastError,
            entity.ProviderMessageId,
            entity.ScheduledAt,
            entity.NextRetryAt,
            entity.ProcessingStartedAt,
            entity.SentAt,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public static string SerializeAddresses(IReadOnlyList<string>? addresses)
    {
        if (addresses is null || addresses.Count == 0)
        {
            return "[]";
        }

        return JsonSerializer.Serialize(addresses.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()), JsonOptions);
    }

    public static IReadOnlyList<string> DeserializeAddresses(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
