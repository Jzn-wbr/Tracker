using Microsoft.Data.Sqlite;
using Tracker.Data.Models;

namespace Tracker.Data.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly TrackerDb _db;

    public SessionRepository(TrackerDb db)
    {
        _db = db;
    }

    public void Insert(SessionWriteModel record)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO sessions
            (start_utc, end_utc, duration_seconds, process_name, process_path, window_title, url, url_domain, created_utc)
            VALUES ($start, $end, $duration, $process, $path, $title, $url, $domain, $created);
            """;

        cmd.Parameters.AddWithValue("$start", record.StartUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$end", record.EndUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$duration", record.DurationSeconds);
        cmd.Parameters.AddWithValue("$process", record.ProcessName);
        cmd.Parameters.AddWithValue("$path", record.ProcessPath);
        cmd.Parameters.AddWithValue("$title", record.WindowTitle);
        cmd.Parameters.AddWithValue("$url", record.Url);
        cmd.Parameters.AddWithValue("$domain", record.UrlDomain);
        cmd.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void DeleteAll()
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM sessions;";
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<SessionExportRow> GetAll()
    {
        var rows = new List<SessionExportRow>();
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT start_utc, end_utc, duration_seconds, process_name, process_path,
                   window_title, url, url_domain
            FROM sessions
            ORDER BY start_utc;
            """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (!DateTime.TryParse(reader.GetString(0), null, System.Globalization.DateTimeStyles.RoundtripKind, out var startUtc)
                || !DateTime.TryParse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind, out var endUtc))
            {
                continue;
            }

            rows.Add(new SessionExportRow
            {
                StartUtc = startUtc,
                EndUtc = endUtc,
                DurationSeconds = reader.GetInt64(2),
                ProcessName = reader.GetString(3),
                ProcessPath = reader.GetString(4),
                WindowTitle = reader.GetString(5),
                Url = reader.GetString(6),
                UrlDomain = reader.GetString(7)
            });
        }

        return rows;
    }

    public long GetTotalSeconds(DateTime utcStart, DateTime utcEnd)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT start_utc, end_utc
            FROM sessions
            WHERE start_utc < $end AND end_utc > $start;
            """;
        cmd.Parameters.AddWithValue("$start", utcStart.ToString("O"));
        cmd.Parameters.AddWithValue("$end", utcEnd.ToString("O"));
        using var reader = cmd.ExecuteReader();
        long total = 0;
        while (reader.Read())
        {
            if (!TryReadRange(reader, 0, out var start, out var end))
            {
                continue;
            }

            total += OverlapSeconds(start, end, utcStart, utcEnd);
        }

        return total;
    }

    public IReadOnlyList<AggregateItem> GetTopApps(DateTime utcStart, DateTime utcEnd, int limit)
    {
        return GetGroupedTotals("process_name", utcStart, utcEnd, limit, includeEmpty: false);
    }

    public IReadOnlyList<AggregateItem> GetTopSites(DateTime utcStart, DateTime utcEnd, int limit)
    {
        return GetGroupedTotals("url_domain", utcStart, utcEnd, limit, includeEmpty: false);
    }

    public IReadOnlyList<TimelinePoint> GetTimelineByHour(DateTime utcStart, DateTime utcEnd)
    {
        var totalsByHour = new Dictionary<DateTime, long>();
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT start_utc, end_utc
            FROM sessions
            WHERE start_utc < $end AND end_utc > $start;
            """;
        cmd.Parameters.AddWithValue("$start", utcStart.ToString("O"));
        cmd.Parameters.AddWithValue("$end", utcEnd.ToString("O"));

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (!DateTime.TryParse(reader.GetString(0), null, System.Globalization.DateTimeStyles.RoundtripKind, out var startUtc))
            {
                continue;
            }
            if (!DateTime.TryParse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind, out var endUtc))
            {
                continue;
            }

            var current = startUtc < utcStart ? utcStart : startUtc;
            var end = endUtc > utcEnd ? utcEnd : endUtc;

            while (current < end)
            {
                var bucketStart = new DateTime(current.Year, current.Month, current.Day, current.Hour, 0, 0, DateTimeKind.Utc);
                var bucketEnd = bucketStart.AddHours(1);
                var segmentEnd = end < bucketEnd ? end : bucketEnd;
                var seconds = (long)Math.Max(0, (segmentEnd - current).TotalSeconds);

                if (seconds > 0)
                {
                    totalsByHour.TryGetValue(bucketStart, out var existing);
                    totalsByHour[bucketStart] = existing + seconds;
                }

                current = segmentEnd;
            }
        }

        var firstBucket = new DateTime(utcStart.Year, utcStart.Month, utcStart.Day, utcStart.Hour, 0, 0, DateTimeKind.Utc);
        var lastBucket = new DateTime(utcEnd.Year, utcEnd.Month, utcEnd.Day, utcEnd.Hour, 0, 0, DateTimeKind.Utc);
        for (var bucket = firstBucket; bucket <= lastBucket; bucket = bucket.AddHours(1))
        {
            totalsByHour.TryAdd(bucket, 0);
        }

        return totalsByHour
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new TimelinePoint
            {
                BucketStartUtc = kvp.Key,
                TotalSeconds = kvp.Value
            })
            .ToList();
    }

    private IReadOnlyList<AggregateItem> GetGroupedTotals(
        string column,
        DateTime utcStart,
        DateTime utcEnd,
        int limit,
        bool includeEmpty)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT {column} AS key, start_utc, end_utc
            FROM sessions
            WHERE start_utc < $end AND end_utc > $start
            {(includeEmpty ? string.Empty : $"AND {column} <> ''")}
            ;
            """;
        cmd.Parameters.AddWithValue("$start", utcStart.ToString("O"));
        cmd.Parameters.AddWithValue("$end", utcEnd.ToString("O"));
        using var reader = cmd.ExecuteReader();
        var totals = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            if (!TryReadRange(reader, 1, out var start, out var end))
            {
                continue;
            }

            var seconds = OverlapSeconds(start, end, utcStart, utcEnd);
            if (seconds <= 0)
            {
                continue;
            }

            var key = reader.GetString(0);
            totals.TryGetValue(key, out var existing);
            totals[key] = existing + seconds;
        }

        return totals
            .OrderByDescending(item => item.Value)
            .Take(Math.Max(0, limit))
            .Select(item => new AggregateItem { Key = item.Key, TotalSeconds = item.Value })
            .ToList();
    }

    private static bool TryReadRange(SqliteDataReader reader, int offset, out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = default;
        endUtc = default;
        return DateTime.TryParse(reader.GetString(offset), null, System.Globalization.DateTimeStyles.RoundtripKind, out startUtc)
            && DateTime.TryParse(reader.GetString(offset + 1), null, System.Globalization.DateTimeStyles.RoundtripKind, out endUtc);
    }

    private static long OverlapSeconds(DateTime start, DateTime end, DateTime rangeStart, DateTime rangeEnd)
    {
        var overlapStart = start > rangeStart ? start : rangeStart;
        var overlapEnd = end < rangeEnd ? end : rangeEnd;
        return overlapEnd <= overlapStart ? 0 : (long)(overlapEnd - overlapStart).TotalSeconds;
    }
}
