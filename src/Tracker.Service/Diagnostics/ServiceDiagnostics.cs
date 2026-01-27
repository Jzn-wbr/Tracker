using System.IO;

namespace Tracker.Service.Diagnostics;

public static class ServiceDiagnostics
{
    private static readonly object Sync = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Tracker",
        "service.log");

    public static void Log(string message)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}";
            lock (Sync)
            {
                File.AppendAllText(LogPath, line);
            }
        }
        catch
        {
        }
    }
}
