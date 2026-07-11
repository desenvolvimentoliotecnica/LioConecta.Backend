using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace LioConecta.Infrastructure.Persistence.Repositories;
public sealed class NewHireAnnouncementRepository(AppDbContext db) : INewHireAnnouncementRepository
{
 public async Task<IReadOnlyList<Person>> GetUnannouncedRecentHiresAsync(DateOnly from, CancellationToken ct=default) => await db.People.Where(p=>p.IsActive && p.HireDate != null && p.HireDate >= from && !db.NewHireAnnouncements.Any(a=>a.PersonId==p.Id)).ToListAsync(ct);
 public async Task AddAsync(FeedPost post, NewHireAnnouncement announcement, CancellationToken ct=default) { db.FeedPosts.Add(post); db.NewHireAnnouncements.Add(announcement); await db.SaveChangesAsync(ct); }
}
