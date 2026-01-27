using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Tracker.Service.Models;

namespace Tracker.Service.Collectors;

public sealed class Win32ActiveWindowReader : IActiveWindowReader
{
    public ActiveWindowInfo? GetActiveWindow()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        var windowTitle = GetWindowTitle(handle);
        GetWindowThreadProcessId(handle, out var processId);
        if (processId == 0)
        {
            return null;
        }

        string processName;
        string? processPath = null;

        try
        {
            using var process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;
            try
            {
                processPath = process.MainModule?.FileName;
            }
            catch
            {
                processPath = null;
            }
        }
        catch
        {
            return null;
        }

        return new ActiveWindowInfo(
            handle,
            processName,
            processPath,
            (int)processId,
            windowTitle,
            null);
    }

    private static string? GetWindowTitle(IntPtr handle)
    {
        var length = GetWindowTextLength(handle);
        if (length <= 0)
        {
            return null;
        }

        var sb = new StringBuilder(length + 1);
        _ = GetWindowText(handle, sb, sb.Capacity);
        var title = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(title) ? null : title;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
