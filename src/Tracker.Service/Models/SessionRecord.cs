namespace Tracker.Service.Models;

public sealed class SessionRecord
{
    public SessionRecord(
        DateTime startUtc,
        DateTime endUtc,
        string processName,
        string? processPath,
        string? windowTitle,
        string? url,
        string? urlDomain)
    {
        StartUtc = startUtc;
        EndUtc = endUtc;
        ProcessName = processName;
        ProcessPath = processPath;
        WindowTitle = windowTitle;
        Url = url;
        UrlDomain = urlDomain;
    }

    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }
    public string ProcessName { get; }
    public string? ProcessPath { get; }
    public string? WindowTitle { get; }
    public string? Url { get; }
    public string? UrlDomain { get; }

    public long DurationSeconds => Math.Max(0, (long)(EndUtc - StartUtc).TotalSeconds);
}
