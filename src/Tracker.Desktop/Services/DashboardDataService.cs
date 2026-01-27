using Tracker.Data;
using Tracker.Data.Repositories;
using Tracker.Desktop.Models;

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
}
