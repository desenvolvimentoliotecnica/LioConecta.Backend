using LioConecta.Domain.Entities;

namespace LioConecta.Application.Common;

public static class PersonGroupKey
{
    /// <summary>
    /// Resolves the SignalR group key for a person (oid, slug, or id — same order as NotificationHub).
    /// </summary>
    public static string Resolve(Person person)
    {
        if (person.AzureAdObjectId.HasValue)
        {
            return person.AzureAdObjectId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(person.Slug))
        {
            return person.Slug;
        }

        return person.Id.ToString();
    }
}
