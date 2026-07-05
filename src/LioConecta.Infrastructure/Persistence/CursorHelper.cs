namespace LioConecta.Infrastructure.Persistence;

internal static class CursorHelper
{
    public static (DateTimeOffset? CreatedAt, Guid? Id) Parse(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return (null, null);
        }

        var parts = cursor.Split('|', 2);
        if (parts.Length != 2)
        {
            return (null, null);
        }

        if (!DateTimeOffset.TryParse(parts[0], out var createdAt))
        {
            return (null, null);
        }

        if (!Guid.TryParse(parts[1], out var id))
        {
            return (null, null);
        }

        return (createdAt, id);
    }

    public static string Encode(DateTimeOffset createdAt, Guid id) => $"{createdAt:O}|{id}";
}
