using LioConecta.Domain.Entities;

namespace LioConecta.Application.Common;

public static class PersonGroupKey
{
    /// <summary>
    /// Resolves the SignalR group key for a person. Matches JWT <c>oid</c> and NotificationHub (always Person.Id).
    /// </summary>
    public static string Resolve(Person person) => person.Id.ToString();
}
