namespace LioConecta.Application.Common;

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public string? NextCursor { get; init; }

    public bool HasMore { get; init; }

    public static PagedResult<T> Empty => new()
    {
        Items = [],
        NextCursor = null,
        HasMore = false
    };

    public static PagedResult<T> FromItems(
        IReadOnlyList<T> items,
        string? nextCursor,
        bool hasMore) => new()
    {
        Items = items,
        NextCursor = nextCursor,
        HasMore = hasMore
    };
}
