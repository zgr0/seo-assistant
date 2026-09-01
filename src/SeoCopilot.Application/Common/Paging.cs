namespace SeoCopilot.Application.Common;

/// <summary>Tum listeleme uclarinda ayni sayfalama sinirlari.</summary>
public static class Paging
{
    public const int DefaultSize = 50;
    public const int MaxSize = 200;

    public static (int Page, int Size) Normalize(int page, int size) =>
        (Math.Max(1, page), Math.Clamp(size <= 0 ? DefaultSize : size, 1, MaxSize));
}
