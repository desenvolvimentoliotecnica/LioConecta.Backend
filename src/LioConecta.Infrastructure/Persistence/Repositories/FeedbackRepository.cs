using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class FeedbackRepository(AppDbContext db) : IFeedbackRepository
{
    public async Task AddAsync(FeedbackSubmission feedback, CancellationToken cancellationToken = default)
    {
        db.FeedbackSubmissions.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeedbackSubmission>> ListRhChannelAsync(
        FeedbackStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = db.FeedbackSubmissions
            .AsNoTracking()
            .Include(x => x.Author)
            .Where(x => x.TargetPersonId == null)
            .OrderByDescending(x => x.CreatedAt)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeedbackSubmission>> ListVisibleToPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        return await db.FeedbackSubmissions
            .AsNoTracking()
            .Include(x => x.Author)
            .Include(x => x.TargetPerson)
            .Where(x =>
                x.TargetPersonId != null &&
                (x.AuthorId == personId ||
                 x.TargetPersonId == personId ||
                 (x.TargetPerson != null && x.TargetPerson.ManagerId == personId)))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<FeedbackSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.FeedbackSubmissions
            .Include(x => x.Author)
            .Include(x => x.TargetPerson)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
