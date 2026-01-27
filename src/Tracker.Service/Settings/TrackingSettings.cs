namespace Tracker.Service.Settings;

public sealed class TrackingSettings
{
    public bool PauseTracking { get; init; }
    public HashSet<string> ExcludedApps { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExcludedSites { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
