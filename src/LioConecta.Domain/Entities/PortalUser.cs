using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class PortalUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Guid PersonId { get; set; }

    public Person Person { get; set; } = null!;

    public string RolesJson { get; set; } = "[\"Admin\"]";

    public bool IsSuperAdmin { get; set; }

    public bool IsActive { get; set; } = true;
}
