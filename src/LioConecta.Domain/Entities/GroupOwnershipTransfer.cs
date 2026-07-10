using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class GroupOwnershipTransfer : BaseEntity
{
    public Guid GroupId { get; set; }

    public Group? Group { get; set; }

    public Guid FromOwnerId { get; set; }

    public Person? FromOwner { get; set; }

    public Guid ToPersonId { get; set; }

    public Person? ToPerson { get; set; }

    public Guid? ApproverId { get; set; }

    public Person? Approver { get; set; }

    public GroupOwnershipTransferStatus Status { get; set; } = GroupOwnershipTransferStatus.Pending;

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }
}
