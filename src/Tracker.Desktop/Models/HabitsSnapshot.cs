using System.Windows.Media;

namespace Tracker.Desktop.Models;

public sealed class HabitsSnapshot
{
    public string CurrentTotalText { get; init; } = "0 min";
    public string TotalChangeText { get; init; } = "Pas de comparaison";
    public Brush TotalChangeBrush { get; init; } = Brushes.SlateGray;
    public string DailyAverageText { get; init; } = "0 min";
    public string DailyAverageChangeText { get; init; } = "Pas de comparaison";
    public Brush DailyAverageChangeBrush { get; init; } = Brushes.SlateGray;
    public string MostActiveDayText { get; init; } = "-";
    public string MostActiveDayDurationText { get; init; } = string.Empty;
    public string MostActiveDayComparisonText { get; init; } = string.Empty;
    public IReadOnlyList<HabitsDayPoint> Days { get; init; } = Array.Empty<HabitsDayPoint>();
    public IReadOnlyList<HabitChangeItem> Changes { get; init; } = Array.Empty<HabitChangeItem>();
    public string AverageStartText { get; init; } = "-";
    public string AverageStartComparisonText { get; init; } = "Pas assez de données";
    public string AverageEndText { get; init; } = "-";
    public string AverageEndComparisonText { get; init; } = "Pas assez de données";
    public string PeakHourText { get; init; } = "-";
    public string PeakHourSubtitle { get; init; } = string.Empty;
}

public sealed class HabitsDayPoint
{
    public string Label { get; init; } = string.Empty;
    public double CurrentHours { get; init; }
    public double PreviousHours { get; init; }
}

public sealed class HabitChangeItem
{
    public string Name { get; init; } = string.Empty;
    public string CurrentDurationText { get; init; } = string.Empty;
    public string ChangeText { get; init; } = string.Empty;
    public Brush ChangeBrush { get; init; } = Brushes.SlateGray;
}
