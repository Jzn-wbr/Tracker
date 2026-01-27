using System.Linq;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Windows.Input;
using Tracker.Desktop.Services;

namespace Tracker.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly DashboardDataService _dataService;
    private readonly SettingsDataService _settingsService;
    private readonly DispatcherTimer _timer;

    private string _totalToday = "0 min";
    private string _totalWeek = "0 min";
    private string _topAppName = "-";
    private string _topAppDuration = "";
    private bool _isPaused;
    private bool _autoStartEnabled;
    private string _excludedAppsText = string.Empty;
    private string _excludedSitesText = string.Empty;

    public MainViewModel()
    {
        _dataService = new DashboardDataService(null);
        _settingsService = new SettingsDataService(null);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();

        TimelinePlot = BuildEmptyPlot();
        TopAppsPlot = BuildEmptyPlot();
        TopSitesPlot = BuildEmptyPlot();

        LoadSettings();
        Refresh();
    }

    public string TotalToday
    {
        get => _totalToday;
        private set { _totalToday = value; Notify(); }
    }

    public string TotalWeek
    {
        get => _totalWeek;
        private set { _totalWeek = value; Notify(); }
    }

    public string TopAppName
    {
        get => _topAppName;
        private set { _topAppName = value; Notify(); }
    }

    public string TopAppDuration
    {
        get => _topAppDuration;
        private set { _topAppDuration = value; Notify(); }
    }

    public bool IsPaused
    {
        get => _isPaused;
        set { _isPaused = value; Notify(); }
    }

    public bool AutoStartEnabled
    {
        get => _autoStartEnabled;
        set { _autoStartEnabled = value; Notify(); }
    }

    public string ExcludedAppsText
    {
        get => _excludedAppsText;
        set { _excludedAppsText = value; Notify(); }
    }

    public string ExcludedSitesText
    {
        get => _excludedSitesText;
        set { _excludedSitesText = value; Notify(); }
    }

    public ICommand SaveSettingsCommand => new RelayCommand(SaveSettings);

    public PlotModel TimelinePlot { get; private set; }
    public PlotModel TopAppsPlot { get; private set; }
    public PlotModel TopSitesPlot { get; private set; }

    private void Refresh()
    {
        var snapshot = _dataService.LoadSnapshot();

        TotalToday = FormatDuration(snapshot.TotalTodaySeconds);
        TotalWeek = FormatDuration(snapshot.TotalWeekSeconds);

        var topApp = snapshot.TopApps.FirstOrDefault();
        TopAppName = topApp?.Key ?? "-";
        TopAppDuration = topApp == null ? string.Empty : FormatDuration(topApp.TotalSeconds);

        ApplyTimeline(snapshot.Timeline);
        ApplyTopApps(snapshot.TopApps);
        ApplyTopSites(snapshot.TopSites);
    }

    private void ApplyTimeline(IReadOnlyList<Tracker.Data.Models.TimelinePoint> points)
    {
        var labels = points.Select(p => p.BucketStartUtc.ToLocalTime().ToString("HH:mm")).ToArray();
        var values = points.Select(p => Math.Round(p.TotalSeconds / 60.0, 1)).ToArray();

        var model = BuildBasePlot();
        var xAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            GapWidth = 0.2,
            IsZoomEnabled = false,
            IsPanEnabled = false
        };
        foreach (var label in labels)
        {
            xAxis.Labels.Add(label);
        }
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            MinimumPadding = 0,
            Minimum = 0,
            Maximum = 60,
            MajorStep = 10,
            MinorStep = 5,
            IsZoomEnabled = false,
            IsPanEnabled = false,
            LabelFormatter = value => $"{value:0}m"
        };

        var lineColor = OxyColors.White;
        var series = new StemSeries
        {
            Color = lineColor,
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = lineColor,
            LabelFormatString = "{2:0}m",
            LabelMargin = 6
        };

        for (var i = 0; i < values.Length; i++)
        {
            series.Points.Add(new DataPoint(i, values[i]));
        }

        model.Axes.Add(xAxis);
        model.Axes.Add(yAxis);
        model.Series.Add(series);

        TimelinePlot = model;
        Notify(nameof(TimelinePlot));
    }

    private void ApplyTopApps(IReadOnlyList<Tracker.Data.Models.AggregateItem> items)
    {
        var labels = items.Select(i => i.Key).ToArray();
        var values = items.Select(i => Math.Round(i.TotalSeconds / 60.0, 1)).ToArray();

        TopAppsPlot = BuildBarPlot(labels, values, OxyColor.FromAColor(230, OxyColors.White));
        Notify(nameof(TopAppsPlot));
    }

    private void ApplyTopSites(IReadOnlyList<Tracker.Data.Models.AggregateItem> items)
    {
        var labels = items.Select(i => i.Key).ToArray();
        var values = items.Select(i => Math.Round(i.TotalSeconds / 60.0, 1)).ToArray();

        TopSitesPlot = BuildBarPlot(labels, values, OxyColor.FromAColor(230, OxyColors.White));
        Notify(nameof(TopSitesPlot));
    }

    private static string FormatDuration(long totalSeconds)
    {
        if (totalSeconds <= 0)
        {
            return "0 min";
        }

        var ts = TimeSpan.FromSeconds(totalSeconds);
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours} h {ts.Minutes:D2}";
        }

        return $"{ts.Minutes} min";
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        IsPaused = settings.PauseTracking;
        AutoStartEnabled = settings.AutoStartEnabled;
        ExcludedAppsText = settings.ExcludedApps.Replace(",", Environment.NewLine);
        ExcludedSitesText = settings.ExcludedSites.Replace(",", Environment.NewLine);
    }

    private void SaveSettings()
    {
        var settings = new Tracker.Data.Models.UserSettings
        {
            PauseTracking = IsPaused,
            AutoStartEnabled = AutoStartEnabled,
            ExcludedApps = ExcludedAppsText ?? string.Empty,
            ExcludedSites = ExcludedSitesText ?? string.Empty
        };

        _settingsService.Save(settings);
        _settingsService.ApplyAutoStart(AutoStartEnabled);
    }

    private static PlotModel BuildBasePlot()
    {
        return new PlotModel
        {
            PlotAreaBorderColor = OxyColors.Transparent,
            TextColor = OxyColors.White,
            DefaultFontSize = 11,
            Background = OxyColors.Transparent
        };
    }

    private static PlotModel BuildBarPlot(string[] labels, double[] values, OxyColor fill)
    {
        var model = BuildBasePlot();
        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Left,
            GapWidth = 0.15,
            IsZoomEnabled = false,
            IsPanEnabled = false
        };
        foreach (var label in labels)
        {
            categoryAxis.Labels.Add(label);
        }

        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            MinimumPadding = 0,
            IsZoomEnabled = false,
            IsPanEnabled = false,
            LabelFormatter = value => $"{value:0}m"
        };

        var series = new BarSeries
        {
            FillColor = fill,
            StrokeColor = OxyColors.Transparent,
            StrokeThickness = 0
        };

        foreach (var value in values)
        {
            series.Items.Add(new BarItem { Value = value });
        }

        model.Axes.Add(categoryAxis);
        model.Axes.Add(valueAxis);
        model.Series.Add(series);
        return model;
    }

    private static PlotModel BuildEmptyPlot()
    {
        var model = BuildBasePlot();
        model.Series.Add(new LineSeries());
        return model;
    }
}
