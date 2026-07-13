using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class ServiceRequestRepository(AppDbContext db) : IServiceRequestRepository
{
    public Task<IReadOnlyList<ServiceRequest>> GetByRequesterAsync(
        Guid requesterId,
        CancellationToken cancellationToken = default) =>
        db.ServiceRequests
            .Include(r => r.Events).ThenInclude(e => e.Actor)
            .AsNoTracking()
            .Where(r => r.RequesterId == requesterId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ServiceRequest>)t.Result, cancellationToken);

    public Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ServiceRequests
            .Include(r => r.Requester)
            .Include(r => r.Events).ThenInclude(e => e.Actor)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<ServiceRequest?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ServiceRequests
            .Include(r => r.Requester)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ServiceRequest>> ListManagementAsync(
        IReadOnlyList<string> types,
        ServiceRequestStatus? status,
        string? query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 200);
        var q = db.ServiceRequests
            .AsNoTracking()
            .Include(r => r.Requester)
            .Include(r => r.Events).ThenInclude(e => e.Actor)
            .Where(r => types.Contains(r.Type));

        if (status is not null)
        {
            q = q.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            q = q.Where(r =>
                (r.Requester != null && r.Requester.Name.ToLower().Contains(term)) ||
                (r.Requester != null && r.Requester.Email != null && r.Requester.Email.ToLower().Contains(term)) ||
                (r.Requester != null && r.Requester.EmployeeId != null && r.Requester.EmployeeId.ToLower().Contains(term)) ||
                r.Type.ToLower().Contains(term) ||
                r.PayloadJson.ToLower().Contains(term));
        }

        return await q
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ServiceRequest request, CancellationToken cancellationToken = default)
    {
        db.ServiceRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddEventAsync(
        ServiceRequestEvent serviceRequestEvent,
        CancellationToken cancellationToken = default)
    {
        db.ServiceRequestEvents.Add(serviceRequestEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ServiceRequest request, CancellationToken cancellationToken = default)
    {
        var affected = await db.ServiceRequests
            .Where(r => r.Id == request.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, request.Status)
                    .SetProperty(r => r.UpdatedAt, request.UpdatedAt)
                    .SetProperty(r => r.PayloadJson, request.PayloadJson)
                    .SetProperty(r => r.AssigneeTeam, request.AssigneeTeam)
                    .SetProperty(r => r.ExternalRef, request.ExternalRef)
                    .SetProperty(r => r.TimelineJson, request.TimelineJson),
                cancellationToken);

        if (affected == 0)
        {
            throw new InvalidOperationException("Pedido não encontrado para atualização.");
        }

        foreach (var ev in request.Events)
        {
            var exists = await db.ServiceRequestEvents.AnyAsync(e => e.Id == ev.Id, cancellationToken);
            if (!exists)
            {
                // Avoid attaching navigation graphs (Requester/Actor) that can cause concurrency failures.
                ev.ServiceRequest = null;
                ev.Actor = null;
                db.ServiceRequestEvents.Add(ev);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAndAddEventAsync(
        Guid id,
        ServiceRequestStatus status,
        ServiceRequestEvent serviceRequestEvent,
        CancellationToken cancellationToken = default)
    {
        var affected = await db.ServiceRequests
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, status)
                    .SetProperty(r => r.UpdatedAt, serviceRequestEvent.UpdatedAt),
                cancellationToken);

        if (affected == 0)
        {
            throw new InvalidOperationException("Pedido não encontrado para atualização.");
        }

        serviceRequestEvent.ServiceRequestId = id;
        serviceRequestEvent.ServiceRequest = null;
        serviceRequestEvent.Actor = null;
        db.ChangeTracker.Clear();
        db.ServiceRequestEvents.Add(serviceRequestEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetExternalRefAsync(
        Guid id,
        string externalRef,
        string assigneeTeam,
        CancellationToken cancellationToken = default)
    {
        var updatedAt = DateTimeOffset.UtcNow;
        await db.ServiceRequests
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.ExternalRef, externalRef)
                    .SetProperty(r => r.AssigneeTeam, assigneeTeam)
                    .SetProperty(r => r.UpdatedAt, updatedAt),
                cancellationToken);
    }
}
