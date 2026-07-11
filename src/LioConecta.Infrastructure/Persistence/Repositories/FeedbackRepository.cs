using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace LioConecta.Infrastructure.Persistence.Repositories;
public sealed class FeedbackRepository(AppDbContext db) : IFeedbackRepository
{
 public async Task AddAsync(FeedbackSubmission feedback, CancellationToken ct = default) { db.FeedbackSubmissions.Add(feedback); await db.SaveChangesAsync(ct); }
 public async Task<IReadOnlyList<FeedbackSubmission>> ListAsync(FeedbackStatus? status, CancellationToken ct = default) { var q=db.FeedbackSubmissions.AsNoTracking().Include(x=>x.Author).OrderByDescending(x=>x.CreatedAt).AsQueryable(); if(status is not null) q=q.Where(x=>x.Status==status); return await q.ToListAsync(ct); }
 public Task<FeedbackSubmission?> GetByIdAsync(Guid id, CancellationToken ct=default)=>db.FeedbackSubmissions.Include(x=>x.Author).FirstOrDefaultAsync(x=>x.Id==id,ct);
 public Task SaveChangesAsync(CancellationToken ct=default)=>db.SaveChangesAsync(ct);
}
