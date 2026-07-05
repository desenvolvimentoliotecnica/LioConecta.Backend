using System.Text.Json;
using LioConecta.Application.Common.Audit;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LioConecta.Infrastructure.Persistence.Interceptors;

public sealed class ChangeAuditInterceptor(IAuditContextAccessor contextAccessor) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Process(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Process(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Process(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var auditContext = contextAccessor.Current;
        if (auditContext is null || auditContext.SuppressChangeAudit)
        {
            return;
        }

        Guid? actorId = auditContext.ActorId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditEvent)
            {
                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var entityType = entry.Entity.GetType().Name;
            var entityId = ResolveEntityId(entry);
            var state = entry.State switch
            {
                EntityState.Added => EntityStateKind.Added,
                EntityState.Modified => EntityStateKind.Modified,
                EntityState.Deleted => EntityStateKind.Deleted,
                _ => EntityStateKind.Modified,
            };

            var details = SerializeChange(entry, entry.State);
            auditContext.PendingEvents.Add(new PendingAuditEvent
            {
                Action = $"Entity.{state}.{entityType}",
                TargetType = entityType,
                TargetId = entityId,
                Source = AuditSource.EntityChange,
                ActorId = actorId,
                DetailsJson = AuditRedactor.RedactJson(details),
            });
        }
    }

    private static string ResolveEntityId(EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        if (idProperty?.CurrentValue is Guid guid)
        {
            return guid.ToString();
        }

        if (idProperty?.CurrentValue is not null)
        {
            return idProperty.CurrentValue.ToString() ?? "unknown";
        }

        return "unknown";
    }

    private static string SerializeChange(EntityEntry entry, EntityState state)
    {
        var payload = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            switch (state)
            {
                case EntityState.Added:
                    payload[property.Metadata.Name] = property.CurrentValue;
                    break;

                case EntityState.Deleted:
                    payload[property.Metadata.Name] = property.OriginalValue;
                    break;

                case EntityState.Modified when property.IsModified:
                    payload[property.Metadata.Name] = new
                    {
                        before = property.OriginalValue,
                        after = property.CurrentValue,
                    };
                    break;
            }
        }

        return JsonSerializer.Serialize(payload);
    }
}
