namespace LioConecta.Application.Common;

public sealed class CursorPageRequest
{
    public string? Cursor { get; init; }

    public int Limit { get; init; } = 20;
}
