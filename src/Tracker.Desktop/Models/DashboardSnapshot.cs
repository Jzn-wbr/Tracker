using Tracker.Data.Models;

namespace Tracker.Desktop.Models;

public sealed class DashboardSnapshot
{
    public long TotalTodaySeconds { get; init; }
    public long TotalWeekSeconds { get; init; }
    public IReadOnlyList<AggregateItem> TopApps { get; init; } = Array.Empty<AggregateItem>();
    public IReadOnlyList<AggregateItem> TopSites { get; init; } = Array.Empty<AggregateItem>();
    public IReadOnlyList<TimelinePoint> Timeline { get; init; } = Array.Empty<TimelinePoint>();
}
