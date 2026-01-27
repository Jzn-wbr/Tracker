using Tracker.Data.Models;

namespace Tracker.Data.Repositories;

public interface ISessionRepository
{
    void Insert(SessionWriteModel record);
    long GetTotalSeconds(DateTime utcStart, DateTime utcEnd);
    IReadOnlyList<AggregateItem> GetTopApps(DateTime utcStart, DateTime utcEnd, int limit);
    IReadOnlyList<AggregateItem> GetTopSites(DateTime utcStart, DateTime utcEnd, int limit);
    IReadOnlyList<TimelinePoint> GetTimelineByHour(DateTime utcStart, DateTime utcEnd);
}
