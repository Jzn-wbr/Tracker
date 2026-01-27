namespace Tracker.Desktop.ViewModels;

public sealed class TopItemViewModel
{
    public string Name { get; init; } = string.Empty;
    public string DurationText { get; init; } = string.Empty;
    public double TotalMinutes { get; init; }
}
