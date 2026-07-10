using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class Group : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public GroupType Type { get; set; }

    public GroupAccessMode AccessMode { get; set; } = GroupAccessMode.Open;

    public string Icon { get; set; } = "fa-users";

    public GroupStatus Status { get; set; } = GroupStatus.PendingApproval;

    public bool IsPrivate { get; set; }

    public Guid OwnerId { get; set; }

    public Person? Owner { get; set; }

    public Guid? ApproverId { get; set; }

    public Person? Approver { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public int ResubmitCount { get; set; }

    public Guid? ReviewedById { get; set; }

    public Person? ReviewedBy { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }

    public ICollection<GroupMember> Members { get; set; } = [];

    public ICollection<GroupPost> Posts { get; set; } = [];

    public ICollection<GroupTopic> Topics { get; set; } = [];

    public ICollection<GroupOwnershipTransfer> OwnershipTransfers { get; set; } = [];
}
