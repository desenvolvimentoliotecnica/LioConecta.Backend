using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
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
        db.ServiceRequests.Update(request);
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
