namespace Tracker.Data.Models;

public sealed class AggregateItem
{
    public string Key { get; init; } = string.Empty;
    public long TotalSeconds { get; init; }
}

public sealed class TimelinePoint
{
    public DateTime BucketStartUtc { get; init; }
    public long TotalSeconds { get; init; }
}
