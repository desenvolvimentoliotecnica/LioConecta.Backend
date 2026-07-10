using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class GroupRepository(AppDbContext db) : IGroupRepository
{
    public async Task<IReadOnlyList<Group>> ExpireOverduePendingAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var overdue = await db.Groups
            .Where(g => g.Status == GroupStatus.PendingApproval && g.ExpiresAt != null && g.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0)
        {
            return overdue;
        }

        foreach (var group in overdue)
        {
            group.Status = GroupStatus.Expired;
            group.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return overdue;
    }

    public async Task<IReadOnlyList<Group>> GetByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        await db.GroupMembers
            .AsNoTracking()
            .Where(m => m.PersonId == personId)
            .Include(m => m.Group)!.ThenInclude(g => g!.Owner)
            .Include(m => m.Group)!.ThenInclude(g => g!.Approver)
            .Include(m => m.Group)!.ThenInclude(g => g!.Members)
            .Include(m => m.Group)!.ThenInclude(g => g!.Posts)
            .Include(m => m.Group)!.ThenInclude(g => g!.Topics)
            .Select(m => m.Group!)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Group>> GetPendingForApproverAsync(
        Guid approverId,
        CancellationToken cancellationToken = default) =>
        await db.Groups
            .Include(g => g.Owner)
            .Include(g => g.Approver)
            .Include(g => g.Members)
            .Include(g => g.Posts)
            .Include(g => g.Topics)
            .AsNoTracking()
            .Where(g => g.Status == GroupStatus.PendingApproval && g.ApproverId == approverId)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Group>> GetExpiredAsync(CancellationToken cancellationToken = default) =>
        await db.Groups
            .Include(g => g.Owner)
            .Include(g => g.Approver)
            .Include(g => g.Members)
            .Include(g => g.Posts)
            .Include(g => g.Topics)
            .AsNoTracking()
            .Where(g => g.Status == GroupStatus.Expired)
            .OrderBy(g => g.ExpiresAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Group>> GetActiveForExploreAsync(CancellationToken cancellationToken = default) =>
        await db.Groups
            .Include(g => g.Owner)
            .Include(g => g.Approver)
            .Include(g => g.Members)
            .Include(g => g.Posts)
            .Include(g => g.Topics)
            .AsNoTracking()
            .Where(g => g.Status == GroupStatus.Active)
            .OrderByDescending(g => g.Members.Count)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Groups
            .Include(g => g.Owner)
            .Include(g => g.Approver)
            .Include(g => g.Members).ThenInclude(m => m.Person)
            .Include(g => g.Posts)
            .Include(g => g.Topics)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<Group?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Groups
            .Include(g => g.Owner)
            .Include(g => g.Approver)
            .Include(g => g.Members).ThenInclude(m => m.Person)
            .Include(g => g.Posts)
            .Include(g => g.Topics)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        db.Groups.Add(group);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMemberAsync(GroupMember member, CancellationToken cancellationToken = default)
    {
        db.GroupMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMemberAsync(GroupMember member, CancellationToken cancellationToken = default)
    {
        db.GroupMembers.Remove(member);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        group.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Group group, CancellationToken cancellationToken = default)
    {
        db.Groups.Remove(group);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsMemberAsync(
        Guid groupId,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.PersonId == personId, cancellationToken);

    public Task<GroupMember?> GetMembershipAsync(
        Guid groupId,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.GroupMembers.FirstOrDefaultAsync(
            m => m.GroupId == groupId && m.PersonId == personId,
            cancellationToken);

    public Task<GroupMember?> GetMemberByIdAsync(
        Guid groupId,
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        db.GroupMembers
            .Include(m => m.Person)
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.Id == memberId, cancellationToken);

    public async Task<IReadOnlyList<GroupMember>> GetMembersAsync(
        Guid groupId,
        CancellationToken cancellationToken = default) =>
        await db.GroupMembers
            .Include(m => m.Person)
            .AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);

    public async Task UpdateMemberAsync(GroupMember member, CancellationToken cancellationToken = default)
    {
        member.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GroupPost>> GetWallPostsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default) =>
        await db.GroupPosts
            .Include(p => p.Author)
            .Include(p => p.Reactions)
            .AsNoTracking()
            .Where(p => p.GroupId == groupId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<GroupPost?> GetWallPostAsync(
        Guid groupId,
        Guid postId,
        CancellationToken cancellationToken = default) =>
        db.GroupPosts
            .Include(p => p.Author)
            .Include(p => p.Reactions)
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.Id == postId, cancellationToken);

    public async Task AddWallPostAsync(GroupPost post, CancellationToken cancellationToken = default)
    {
        db.GroupPosts.Add(post);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteWallPostAsync(GroupPost post, CancellationToken cancellationToken = default)
    {
        db.GroupPosts.Remove(post);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<GroupPostReaction?> GetReactionAsync(
        Guid postId,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.GroupPostReactions.FirstOrDefaultAsync(
            r => r.PostId == postId && r.PersonId == personId,
            cancellationToken);

    public async Task AddReactionAsync(GroupPostReaction reaction, CancellationToken cancellationToken = default)
    {
        db.GroupPostReactions.Add(reaction);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveReactionAsync(GroupPostReaction reaction, CancellationToken cancellationToken = default)
    {
        db.GroupPostReactions.Remove(reaction);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GroupTopic>> GetTopicsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default) =>
        await db.GroupTopics
            .Include(t => t.Author)
            .Include(t => t.Replies)
            .AsNoTracking()
            .Where(t => t.GroupId == groupId)
            .OrderByDescending(t => t.LastActivityAt)
            .ToListAsync(cancellationToken);

    public Task<GroupTopic?> GetTopicAsync(
        Guid groupId,
        Guid topicId,
        CancellationToken cancellationToken = default) =>
        db.GroupTopics
            .Include(t => t.Author)
            .Include(t => t.Replies).ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(t => t.GroupId == groupId && t.Id == topicId, cancellationToken);

    public async Task AddTopicAsync(GroupTopic topic, CancellationToken cancellationToken = default)
    {
        db.GroupTopics.Add(topic);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateTopicAsync(GroupTopic topic, CancellationToken cancellationToken = default)
    {
        topic.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTopicAsync(GroupTopic topic, CancellationToken cancellationToken = default)
    {
        db.GroupTopics.Remove(topic);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTopicReplyAsync(GroupTopicReply reply, CancellationToken cancellationToken = default)
    {
        db.GroupTopicReplies.Add(reply);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<GroupTopicReply?> GetTopicReplyAsync(
        Guid topicId,
        Guid replyId,
        CancellationToken cancellationToken = default) =>
        db.GroupTopicReplies
            .Include(r => r.Author)
            .FirstOrDefaultAsync(r => r.TopicId == topicId && r.Id == replyId, cancellationToken);

    public async Task DeleteTopicReplyAsync(GroupTopicReply reply, CancellationToken cancellationToken = default)
    {
        db.GroupTopicReplies.Remove(reply);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddOwnershipTransferAsync(
        GroupOwnershipTransfer transfer,
        CancellationToken cancellationToken = default)
    {
        db.GroupOwnershipTransfers.Add(transfer);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<GroupOwnershipTransfer?> GetOwnershipTransferAsync(
        Guid transferId,
        CancellationToken cancellationToken = default) =>
        db.GroupOwnershipTransfers
            .Include(t => t.Group)
            .Include(t => t.FromOwner)
            .Include(t => t.ToPerson)
            .Include(t => t.Approver)
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken);

    public async Task<IReadOnlyList<GroupOwnershipTransfer>> GetPendingOwnershipTransfersForApproverAsync(
        Guid approverId,
        CancellationToken cancellationToken = default) =>
        await db.GroupOwnershipTransfers
            .Include(t => t.Group)
            .Include(t => t.FromOwner)
            .Include(t => t.ToPerson)
            .Include(t => t.Approver)
            .AsNoTracking()
            .Where(t => t.Status == GroupOwnershipTransferStatus.Pending && t.ApproverId == approverId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task UpdateOwnershipTransferAsync(
        GroupOwnershipTransfer transfer,
        CancellationToken cancellationToken = default)
    {
        transfer.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasPendingOwnershipTransferAsync(
        Guid groupId,
        CancellationToken cancellationToken = default) =>
        db.GroupOwnershipTransfers.AnyAsync(
            t => t.GroupId == groupId && t.Status == GroupOwnershipTransferStatus.Pending,
            cancellationToken);
}
