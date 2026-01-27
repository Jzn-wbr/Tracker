namespace Tracker.Service.Models;

public sealed class ActiveWindowInfo
{
    public ActiveWindowInfo(
        IntPtr windowHandle,
        string processName,
        string? processPath,
        int processId,
        string? windowTitle,
        string? url)
    {
        WindowHandle = windowHandle;
        ProcessName = processName;
        ProcessPath = processPath;
        ProcessId = processId;
        WindowTitle = windowTitle;
        Url = url;
    }

    public IntPtr WindowHandle { get; }
    public string ProcessName { get; }
    public string? ProcessPath { get; }
    public int ProcessId { get; }
    public string? WindowTitle { get; }
    public string? Url { get; }

    public bool IsSameSessionKey(ActiveWindowInfo other)
    {
        return WindowHandle == other.WindowHandle
            && string.Equals(ProcessName, other.ProcessName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(WindowTitle ?? string.Empty, other.WindowTitle ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(Url ?? string.Empty, other.Url ?? string.Empty, StringComparison.Ordinal);
    }
}
