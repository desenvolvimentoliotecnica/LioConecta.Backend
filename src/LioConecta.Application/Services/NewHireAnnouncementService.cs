using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
namespace LioConecta.Application.Services;
public sealed class NewHireAnnouncementService(INewHireAnnouncementRepository repository, IAppSettingsProvider settings, INotificationService notificationService) : INewHireAnnouncementService
{
 public async Task<int> AnnounceRecentHiresAsync(CancellationToken ct=default) { if(!settings.GetBool("feed.new_hire_announce.enabled",true)) return 0; var days=Math.Clamp(settings.GetInt("feed.new_hire_announce.window_days",7),1,90); var hires=await repository.GetUnannouncedRecentHiresAsync(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days)),ct); var now=DateTimeOffset.UtcNow; foreach(var person in hires) { var post=new FeedPost { Id=Guid.NewGuid(), AuthorId=person.Id, Type=PostType.News, Content=$"Damos as boas-vindas a {person.Name.Trim()}!", MetadataJson="{\"kind\":\"new-hire\"}", CreatedAt=now, UpdatedAt=now }; await repository.AddAsync(post,new NewHireAnnouncement { Id=Guid.NewGuid(), PersonId=person.Id, FeedPostId=post.Id, AnnouncedAt=now, CreatedAt=now, UpdatedAt=now },ct); await notificationService.NotifyNewHireAsync(post,person,ct); } return hires.Count; }
}
