namespace TryMudBlazor.Server.Utilities;

/// <summary>
/// Storage IDs are 16 digits: yyyyMMdd followed by the millisecond of the day, zero padded to 8.
/// The blob path is derived from the ID, so the layout is stable regardless of when it was generated.
/// </summary>
public static class SnippetIds
{
    public const int Length = 16;

    public static string New() => New(DateTime.UtcNow);

    public static string New(DateTime utcNow)
    {
        var millisecondOfDay = (int)utcNow.TimeOfDay.TotalMilliseconds;
        return $"{utcNow:yyyyMMdd}{millisecondOfDay:D8}";
    }

    public static string BlobPath(string snippetId)
    {
        if (snippetId.Length != Length)
        {
            throw new ArgumentException($"Snippet IDs are {Length} digits.", nameof(snippetId));
        }

        return $"{snippetId[..4]}/{snippetId[4..6]}/{snippetId[6..8]}/{snippetId[8..]}";
    }
}
