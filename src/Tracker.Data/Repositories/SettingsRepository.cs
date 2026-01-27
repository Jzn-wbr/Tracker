using Tracker.Data.Models;

namespace Tracker.Data.Repositories;

public sealed class SettingsRepository : ISettingsRepository
{
    private const string PauseKey = "pause_tracking";
    private const string AppsKey = "excluded_apps";
    private const string SitesKey = "excluded_sites";
    private const string AutoStartKey = "auto_start";

    private readonly TrackerDb _db;

    public SettingsRepository(TrackerDb db)
    {
        _db = db;
    }

    public UserSettings GetSettings()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT key, value FROM settings;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            values[reader.GetString(0)] = reader.GetString(1);
        }

        return new UserSettings
        {
            PauseTracking = GetBool(values, PauseKey),
            AutoStartEnabled = GetBool(values, AutoStartKey),
            ExcludedApps = GetString(values, AppsKey),
            ExcludedSites = GetString(values, SitesKey)
        };
    }

    public void SaveSettings(UserSettings settings)
    {
        using var connection = _db.OpenConnection();
        using var tx = connection.BeginTransaction();

        Upsert(connection, PauseKey, settings.PauseTracking ? "1" : "0");
        Upsert(connection, AutoStartKey, settings.AutoStartEnabled ? "1" : "0");
        Upsert(connection, AppsKey, settings.ExcludedApps ?? string.Empty);
        Upsert(connection, SitesKey, settings.ExcludedSites ?? string.Empty);

        tx.Commit();
    }

    private static void Upsert(Microsoft.Data.Sqlite.SqliteConnection connection, string key, string value)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO settings (key, value)
            VALUES ($key, $value)
            ON CONFLICT(key) DO UPDATE SET value = $value;
            """;
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$value", value);
        cmd.ExecuteNonQuery();
    }

    private static bool GetBool(Dictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var raw)
            && (raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetString(Dictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var raw) ? raw : string.Empty;
    }
}
