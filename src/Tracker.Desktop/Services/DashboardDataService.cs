using Tracker.Data;
using Tracker.Data.Repositories;
using Tracker.Desktop.Models;
using System.IO;
using System.Text;

namespace Tracker.Desktop.Services;

public sealed class DashboardDataService
{
    private readonly ISessionRepository _repository;

    public DashboardDataService(string? databasePath)
    {
        var db = new TrackerDb(databasePath ?? DatabasePathResolver.Resolve());
        _repository = new SessionRepository(db);
    }

    public DashboardSnapshot LoadSnapshot()
    {
        var nowLocal = DateTime.Now;
        var localDayStart = nowLocal.Date;
        var localWeekStart = localDayStart.AddDays(-6);

        var utcDayStart = TimeZoneInfo.ConvertTimeToUtc(localDayStart);
        var utcWeekStart = TimeZoneInfo.ConvertTimeToUtc(localWeekStart);
        var utcNow = DateTime.UtcNow;

        return new DashboardSnapshot
        {
            TotalTodaySeconds = _repository.GetTotalSeconds(utcDayStart, utcNow),
            TotalWeekSeconds = _repository.GetTotalSeconds(utcWeekStart, utcNow),
            TopApps = _repository.GetTopApps(utcWeekStart, utcNow, 6),
            TopSites = _repository.GetTopSites(utcWeekStart, utcNow, 6),
            Timeline = _repository.GetTimelineByHour(utcDayStart, utcNow)
        };
    }

    public void DeleteAllData()
    {
        _repository.DeleteAll();
    }

    public void ExportCsv(string path)
    {
        var csv = new StringBuilder();
        csv.AppendLine("start_utc;end_utc;duration_seconds;process_name;process_path;window_title;url;url_domain");
        foreach (var row in _repository.GetAll())
        {
            csv.AppendLine(string.Join(";", new[]
            {
                Escape(row.StartUtc.ToString("O")),
                Escape(row.EndUtc.ToString("O")),
                row.DurationSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Escape(row.ProcessName), Escape(row.ProcessPath), Escape(row.WindowTitle),
                Escape(row.Url), Escape(row.UrlDomain)
            }));
        }

        File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
