using System.Globalization;
using System.Windows.Media;
using Tracker.Data;
using Tracker.Data.Models;
using Tracker.Data.Repositories;
using Tracker.Desktop.Models;

namespace Tracker.Desktop.Services;

public sealed class HabitsDataService
{
    private static readonly string[] DayLabels = { "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim" };
    private readonly ISessionRepository _repository;

    public HabitsDataService(string? databasePath)
    {
        var db = new TrackerDb(databasePath ?? DatabasePathResolver.Resolve());
        _repository = new SessionRepository(db);
    }

    public HabitsSnapshot LoadSnapshot()
    {
        var timeZone = TimeZoneInfo.Local;
        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);
        var currentStartLocal = StartOfWeek(nowLocal.Date);
        var currentEndLocal = nowLocal;
        var elapsed = currentEndLocal - currentStartLocal;
        var previousStartLocal = currentStartLocal.AddDays(-7);
        var previousEndLocal = previousStartLocal + elapsed;
        var currentStartUtc = ToUtc(currentStartLocal, timeZone);
        var currentEndUtc = nowUtc;
        var previousStartUtc = ToUtc(previousStartLocal, timeZone);
        var previousEndUtc = ToUtc(previousEndLocal, timeZone);
        var rows = _repository.GetAll();

        var currentTotal = Total(rows, currentStartUtc, currentEndUtc);
        var previousTotal = Total(rows, previousStartUtc, previousEndUtc);
        var currentDays = GetDailyTotals(rows, currentStartLocal, currentEndLocal, timeZone);
        var previousDays = GetDailyTotals(rows, previousStartLocal, previousEndLocal, timeZone);
        var activeDayCount = currentDays.Count(value => value > 0);
        var previousActiveDayCount = previousDays.Count(value => value > 0);
        var currentAverage = activeDayCount == 0 ? 0 : currentTotal / activeDayCount;
        var previousAverage = previousActiveDayCount == 0 ? 0 : previousTotal / previousActiveDayCount;
        var mostActiveIndex = currentTotal <= 0 ? -1 : Array.IndexOf(currentDays, currentDays.Max());
        var mostActiveDay = mostActiveIndex < 0 ? 0 : currentDays[mostActiveIndex];

        var days = Enumerable.Range(0, 7)
            .Select(index => new HabitsDayPoint
            {
                Label = DayLabels[index],
                CurrentHours = currentDays[index] / 3600d,
                PreviousHours = previousDays[index] / 3600d
            })
            .ToList();

        var changes = BuildChanges(rows, currentStartUtc, currentEndUtc, previousStartUtc, previousEndUtc);
        var rhythm = CalculateRhythm(rows, currentStartLocal, currentEndLocal, previousStartLocal, previousEndLocal, timeZone);

        return new HabitsSnapshot
        {
            CurrentTotalText = FormatDuration(currentTotal),
            TotalChangeText = FormatChange(currentTotal, previousTotal),
            TotalChangeBrush = ChangeBrush(currentTotal, previousTotal),
            DailyAverageText = FormatDuration(currentAverage),
            DailyAverageChangeText = FormatChange(currentAverage, previousAverage),
            DailyAverageChangeBrush = ChangeBrush(currentAverage, previousAverage),
            MostActiveDayText = mostActiveIndex < 0 ? "-" : FullDayName(mostActiveIndex),
            MostActiveDayDurationText = mostActiveIndex < 0 ? string.Empty : FormatDuration(mostActiveDay),
            MostActiveDayComparisonText = mostActiveIndex < 0 ? string.Empty : FormatChange(mostActiveDay, previousDays[mostActiveIndex]),
            Days = days,
            Changes = changes,
            AverageStartText = rhythm.AverageStartText,
            AverageStartComparisonText = rhythm.AverageStartComparisonText,
            AverageEndText = rhythm.AverageEndText,
            AverageEndComparisonText = rhythm.AverageEndComparisonText,
            PeakHourText = rhythm.PeakHourText,
            PeakHourSubtitle = rhythm.PeakHourSubtitle
        };
    }

    private static IReadOnlyList<HabitChangeItem> BuildChanges(
        IReadOnlyList<SessionExportRow> rows,
        DateTime currentStartUtc,
        DateTime currentEndUtc,
        DateTime previousStartUtc,
        DateTime previousEndUtc)
    {
        var currentApps = Aggregate(rows, currentStartUtc, currentEndUtc, row => row.ProcessName);
        var previousApps = Aggregate(rows, previousStartUtc, previousEndUtc, row => row.ProcessName);
        var currentSites = Aggregate(rows, currentStartUtc, currentEndUtc, row => row.UrlDomain);
        var previousSites = Aggregate(rows, previousStartUtc, previousEndUtc, row => row.UrlDomain);
        var candidates = new List<(string Name, long Current, long Previous)>();
        AddCandidates(candidates, currentApps, previousApps);
        AddCandidates(candidates, currentSites, previousSites);

        return candidates
            .Where(item => item.Current + item.Previous >= 20 * 60 && Math.Abs(item.Current - item.Previous) >= 10 * 60)
            .OrderByDescending(item => Math.Abs(item.Current - item.Previous))
            .Take(6)
            .Select(item => new HabitChangeItem
            {
                Name = item.Name,
                CurrentDurationText = FormatDuration(item.Current),
                ChangeText = FormatChange(item.Current, item.Previous),
                ChangeBrush = ChangeBrush(item.Current, item.Previous)
            })
            .ToList();
    }

    private static void AddCandidates(
        ICollection<(string Name, long Current, long Previous)> candidates,
        IReadOnlyDictionary<string, long> current,
        IReadOnlyDictionary<string, long> previous)
    {
        foreach (var name in current.Keys.Union(previous.Keys, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            candidates.Add((name, current.TryGetValue(name, out var currentValue) ? currentValue : 0,
                previous.TryGetValue(name, out var previousValue) ? previousValue : 0));
        }
    }

    private static RhythmResult CalculateRhythm(
        IReadOnlyList<SessionExportRow> rows,
        DateTime currentStartLocal,
        DateTime currentEndLocal,
        DateTime previousStartLocal,
        DateTime previousEndLocal,
        TimeZoneInfo timeZone)
    {
        var currentDays = GetDayWindows(rows, currentStartLocal, currentEndLocal, timeZone);
        var previousDays = GetDayWindows(rows, previousStartLocal, previousEndLocal, timeZone);
        var currentActive = currentDays.Where(day => day.TotalSeconds >= 5 * 60).ToList();
        var previousActive = previousDays.Where(day => day.TotalSeconds >= 5 * 60).ToList();
        var currentStart = currentActive.Count == 0 ? (double?)null : currentActive.Average(day => day.StartMinutes);
        var previousStart = previousActive.Count == 0 ? (double?)null : previousActive.Average(day => day.StartMinutes);
        var currentEnd = currentActive.Count == 0 ? (double?)null : currentActive.Average(day => day.EndMinutes);
        var previousEnd = previousActive.Count == 0 ? (double?)null : previousActive.Average(day => day.EndMinutes);
        var peak = new long[24];

        foreach (var row in rows)
        {
            AddHourlyOverlap(row, currentStartLocal, currentEndLocal, timeZone, peak);
        }

        var peakHour = Array.IndexOf(peak, peak.Max());
        return new RhythmResult
        {
            AverageStartText = currentStart.HasValue ? FormatClock(currentStart.Value) : "-",
            AverageStartComparisonText = FormatClockDifference(currentStart, previousStart, earlierIsPositive: true),
            AverageEndText = currentEnd.HasValue ? FormatClock(currentEnd.Value) : "-",
            AverageEndComparisonText = FormatClockDifference(currentEnd, previousEnd, earlierIsPositive: false),
            PeakHourText = peakHour < 0 || peak[peakHour] <= 0 ? "-" : $"{peakHour:00}h – {(peakHour + 1) % 24:00}h",
            PeakHourSubtitle = peakHour < 0 || peak[peakHour] <= 0 ? string.Empty : $"{FormatDuration(peak[peakHour])} cumulées cette semaine"
        };
    }

    private static void AddHourlyOverlap(SessionExportRow row, DateTime rangeStartLocal, DateTime rangeEndLocal, TimeZoneInfo timeZone, long[] totals)
    {
        var startUtc = row.StartUtc.ToUniversalTime();
        var endUtc = row.EndUtc.ToUniversalTime();
        var rangeStartUtc = ToUtc(rangeStartLocal, timeZone);
        var rangeEndUtc = ToUtc(rangeEndLocal, timeZone);
        var start = startUtc > rangeStartUtc ? startUtc : rangeStartUtc;
        var end = endUtc < rangeEndUtc ? endUtc : rangeEndUtc;
        if (end <= start) return;

        var cursor = start;
        while (cursor < end)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(cursor, timeZone);
            var nextLocal = local.Date.AddHours(local.Hour + 1);
            var nextUtc = ToUtc(nextLocal, timeZone);
            var segmentEnd = end < nextUtc ? end : nextUtc;
            totals[local.Hour] += Math.Max(0, (long)(segmentEnd - cursor).TotalSeconds);
            cursor = segmentEnd;
        }
    }

    private static IReadOnlyList<DayWindow> GetDayWindows(IReadOnlyList<SessionExportRow> rows, DateTime startLocal, DateTime endLocal, TimeZoneInfo timeZone)
    {
        var result = new List<DayWindow>();
        for (var day = 0; day < 7; day++)
        {
            var dayStart = startLocal.Date.AddDays(day);
            var dayEnd = dayStart.AddDays(1);
            var effectiveEnd = endLocal < dayEnd ? endLocal : dayEnd;
            if (effectiveEnd <= dayStart)
            {
                result.Add(new DayWindow());
                continue;
            }
            var startUtc = ToUtc(dayStart, timeZone);
            var endUtc = ToUtc(effectiveEnd, timeZone);
            var sessions = rows.Select(row => (Row: row, Seconds: Overlap(row.StartUtc.ToUniversalTime(), row.EndUtc.ToUniversalTime(), startUtc, endUtc)))
                .Where(item => item.Seconds > 0)
                .ToList();
            var first = sessions.Count == 0 ? dayStart : TimeZoneInfo.ConvertTimeFromUtc(sessions.Min(item => item.Row.StartUtc.ToUniversalTime() > startUtc ? item.Row.StartUtc.ToUniversalTime() : startUtc), timeZone);
            var last = sessions.Count == 0 ? dayStart : TimeZoneInfo.ConvertTimeFromUtc(sessions.Max(item => item.Row.EndUtc.ToUniversalTime() < endUtc ? item.Row.EndUtc.ToUniversalTime() : endUtc), timeZone);
            result.Add(new DayWindow
            {
                TotalSeconds = sessions.Sum(item => item.Seconds),
                StartMinutes = first.Hour * 60 + first.Minute,
                EndMinutes = last.Hour * 60 + last.Minute
            });
        }
        return result;
    }

    private static long[] GetDailyTotals(IReadOnlyList<SessionExportRow> rows, DateTime startLocal, DateTime endLocal, TimeZoneInfo timeZone)
    {
        var windows = GetDayWindows(rows, startLocal, endLocal, timeZone);
        var totals = new long[7];
        for (var i = 0; i < windows.Count && i < totals.Length; i++) totals[i] = windows[i].TotalSeconds;
        return totals;
    }

    private static IReadOnlyDictionary<string, long> Aggregate(IReadOnlyList<SessionExportRow> rows, DateTime startUtc, DateTime endUtc, Func<SessionExportRow, string> keySelector)
    {
        var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var seconds = Overlap(row.StartUtc.ToUniversalTime(), row.EndUtc.ToUniversalTime(), startUtc, endUtc);
            var key = keySelector(row);
            if (seconds <= 0 || string.IsNullOrWhiteSpace(key)) continue;
            result[key] = result.TryGetValue(key, out var current) ? current + seconds : seconds;
        }
        return result;
    }

    private static long Total(IReadOnlyList<SessionExportRow> rows, DateTime startUtc, DateTime endUtc) =>
        rows.Sum(row => Overlap(row.StartUtc.ToUniversalTime(), row.EndUtc.ToUniversalTime(), startUtc, endUtc));

    private static long Overlap(DateTime start, DateTime end, DateTime rangeStart, DateTime rangeEnd)
    {
        var overlapStart = start > rangeStart ? start : rangeStart;
        var overlapEnd = end < rangeEnd ? end : rangeEnd;
        return overlapEnd <= overlapStart ? 0 : (long)(overlapEnd - overlapStart).TotalSeconds;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var offset = date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)date.DayOfWeek - 1;
        return date.AddDays(-offset);
    }

    private static DateTime ToUtc(DateTime local, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZone);

    private static string FullDayName(int index) => new CultureInfo("fr-FR").DateTimeFormat.GetDayName((DayOfWeek)((index + 1) % 7));

    private static string FormatDuration(long seconds)
    {
        if (seconds < 60) return "< 1 min";
        var span = TimeSpan.FromSeconds(seconds);
        return span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes:00}m" : $"{span.Minutes} min";
    }

    private static string FormatChange(double current, double previous)
    {
        if (previous <= 0) return current > 0 ? "Nouveau" : "Pas de comparaison";
        var percentage = (current - previous) / previous * 100;
        if (Math.Abs(percentage) < 2) return "Stable";
        return $"{(percentage >= 0 ? "+" : string.Empty)}{percentage:0}% vs semaine dernière";
    }

    private static Brush ChangeBrush(double current, double previous)
    {
        if (previous <= 0 || Math.Abs((current - previous) / Math.Max(previous, 1)) < 0.02) return Brushes.SlateGray;
        return current > previous ? new SolidColorBrush(Color.FromRgb(234, 88, 12)) : new SolidColorBrush(Color.FromRgb(22, 163, 74));
    }

    private static string FormatClock(double minutes) => TimeSpan.FromMinutes(minutes).ToString(@"hh\:mm");

    private static string FormatClockDifference(double? current, double? previous, bool earlierIsPositive)
    {
        if (!current.HasValue || !previous.HasValue) return "Pas assez de données";
        var difference = current.Value - previous.Value;
        if (Math.Abs(difference) < 2) return "Stable";
        var minutes = Math.Abs((int)Math.Round(difference));
        var direction = (difference < 0) == earlierIsPositive ? "plus tôt" : "plus tard";
        return $"{minutes} min {direction}";
    }

    private sealed class DayWindow
    {
        public long TotalSeconds { get; init; }
        public int StartMinutes { get; init; }
        public int EndMinutes { get; init; }
    }

    private sealed class RhythmResult
    {
        public string AverageStartText { get; init; } = "-";
        public string AverageStartComparisonText { get; init; } = string.Empty;
        public string AverageEndText { get; init; } = "-";
        public string AverageEndComparisonText { get; init; } = string.Empty;
        public string PeakHourText { get; init; } = "-";
        public string PeakHourSubtitle { get; init; } = string.Empty;
    }
}
