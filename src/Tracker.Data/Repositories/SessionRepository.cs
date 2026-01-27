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

    public long GetTotalSeconds(DateTime utcStart, DateTime utcEnd)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COALESCE(SUM(duration_seconds), 0)
            FROM sessions
            WHERE start_utc >= $start AND end_utc <= $end;
            """;
        cmd.Parameters.AddWithValue("$start", utcStart.ToString("O"));
        cmd.Parameters.AddWithValue("$end", utcEnd.ToString("O"));
        return Convert.ToInt64(cmd.ExecuteScalar());
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
            WHERE start_utc >= $start AND end_utc <= $end;
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

            if (endUtc <= utcStart || startUtc >= utcEnd)
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
        var results = new List<AggregateItem>();
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT {column} AS key, COALESCE(SUM(duration_seconds), 0) AS total
            FROM sessions
            WHERE start_utc >= $start AND end_utc <= $end
            {(includeEmpty ? string.Empty : $"AND {column} <> ''")}
            GROUP BY key
            ORDER BY total DESC
            LIMIT $limit;
            """;
        cmd.Parameters.AddWithValue("$start", utcStart.ToString("O"));
        cmd.Parameters.AddWithValue("$end", utcEnd.ToString("O"));
        cmd.Parameters.AddWithValue("$limit", limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new AggregateItem
            {
                Key = reader.GetString(0),
                TotalSeconds = reader.GetInt64(1)
            });
        }

        return results;
    }
}
