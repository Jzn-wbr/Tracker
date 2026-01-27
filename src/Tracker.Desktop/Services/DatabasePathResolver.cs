using System.IO;

namespace Tracker.Desktop.Services;

public static class DatabasePathResolver
{
    public static string Resolve()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var localPath = Path.Combine(localData ?? string.Empty, "Tracker", "tracker.db");
        TryMigrateLegacyDatabase(localPath);
        return localPath;
    }

    private static void TryMigrateLegacyDatabase(string targetPath)
    {
        if (File.Exists(targetPath))
        {
            return;
        }

        var legacyRoot = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var legacyPath = Path.Combine(legacyRoot ?? string.Empty, "Tracker", "tracker.db");
        if (!File.Exists(legacyPath))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(legacyPath, targetPath, overwrite: false);
        }
        catch
        {
        }
    }
}
