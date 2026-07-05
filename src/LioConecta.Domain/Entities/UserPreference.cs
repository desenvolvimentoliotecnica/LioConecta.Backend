using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UserPreference : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public string BookmarksJson { get; set; } = "[]";

    public string FavoritesJson { get; set; } = "[]";

    public string ShortcutsJson { get; set; } = "[]";
}
