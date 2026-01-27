namespace Tracker.Data.Models;

public sealed class UserSettings
{
    public bool PauseTracking { get; init; }
    public bool AutoStartEnabled { get; init; }
    public string ExcludedApps { get; init; } = string.Empty;
    public string ExcludedSites { get; init; } = string.Empty;
}
