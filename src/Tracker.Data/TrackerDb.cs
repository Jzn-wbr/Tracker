using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Tracker.Data;

public sealed class TrackerDb
{
    private readonly string _connectionString;

    public TrackerDb(string? databasePath)
    {
        Batteries_V2.Init();
        var path = databasePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = GetDefaultDatabasePath();
            TryMigrateLegacyDatabase(path);
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path
        }.ToString();

        Initialize();
    }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                start_utc TEXT NOT NULL,
                end_utc TEXT NOT NULL,
                duration_seconds INTEGER NOT NULL,
                process_name TEXT NOT NULL,
                process_path TEXT NOT NULL,
                window_title TEXT NOT NULL,
                url TEXT NOT NULL DEFAULT '',
                url_domain TEXT NOT NULL DEFAULT '',
                created_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();

        EnsureColumn(connection, "sessions", "url", "ALTER TABLE sessions ADD COLUMN url TEXT NOT NULL DEFAULT '';");
        EnsureColumn(connection, "sessions", "url_domain", "ALTER TABLE sessions ADD COLUMN url_domain TEXT NOT NULL DEFAULT '';");

        using var indexCmd = connection.CreateCommand();
        indexCmd.CommandText = """
            CREATE INDEX IF NOT EXISTS ix_sessions_start ON sessions(start_utc);
            CREATE INDEX IF NOT EXISTS ix_sessions_process ON sessions(process_name);
            CREATE INDEX IF NOT EXISTS ix_sessions_domain ON sessions(url_domain);
            """;
        indexCmd.ExecuteNonQuery();
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string alterSql)
    {
        using var pragma = connection.CreateCommand();
        pragma.CommandText = $"PRAGMA table_info({table});";
        using var reader = pragma.ExecuteReader();

        while (reader.Read())
        {
            var name = reader.GetString(1);
            if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = alterSql;
        alter.ExecuteNonQuery();
    }

    private static string GetDefaultDatabasePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tracker",
            "tracker.db");
    }

    private static string GetLegacyDatabasePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Tracker",
            "tracker.db");
    }

    private static void TryMigrateLegacyDatabase(string targetPath)
    {
        if (File.Exists(targetPath))
        {
            return;
        }

        var legacyPath = GetLegacyDatabasePath();
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
