using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UserTeamsToken : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person Person { get; set; } = null!;

    public string EncryptedAccessToken { get; set; } = string.Empty;

    public string EncryptedRefreshToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public string ScopesJson { get; set; } = "[]";
}
