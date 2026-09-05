namespace TryMudBlazor.Server.Utilities;

/// <summary>
/// Produces candidate storage IDs: sixteen digits, the UTC date as yyyyMMdd followed by an eight-digit suffix
/// that only has to be unique within that day. The format is what <see cref="SnippetsEncoder"/> encodes and
/// what existing links decode to, so it must not change.
/// </summary>
public sealed class SnippetIdAllocator
{
    private const int SuffixSpace = 100_000_000;

    private readonly TimeProvider _timeProvider;
    private readonly Random _random;

    public SnippetIdAllocator(TimeProvider timeProvider, Random random)
    {
        _timeProvider = timeProvider;
        _random = random;
    }

    /// <summary>
    /// The first candidate is the millisecond of the day, so IDs keep sorting by time in storage. If that one is
    /// already taken, every later candidate is drawn at random from the day's space: requests that collided on
    /// the timestamp then pick independent IDs instead of retrying the same next value in lockstep.
    /// </summary>
    public string NextCandidate(int attempt)
    {
        var now = _timeProvider.GetUtcNow();
        var suffix = attempt <= 1
            ? (int)now.TimeOfDay.TotalMilliseconds
            : _random.Next(SuffixSpace);

        return $"{now:yyyyMMdd}{suffix:D8}";
    }
}
