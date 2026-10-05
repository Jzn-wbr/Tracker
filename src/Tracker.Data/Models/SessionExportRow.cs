namespace Tracker.Data.Models;

public sealed class SessionExportRow
{
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public long DurationSeconds { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public string ProcessPath { get; init; } = string.Empty;
    public string WindowTitle { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string UrlDomain { get; init; } = string.Empty;
}
