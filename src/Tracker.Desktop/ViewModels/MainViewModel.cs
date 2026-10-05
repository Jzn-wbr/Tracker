using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using System.Windows.Input;
using Tracker.Desktop.Models;
using Tracker.Desktop.Services;

namespace Tracker.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly DashboardDataService _dataService;
    private readonly HabitsDataService _habitsDataService;
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
    private string _agentStatus = "Vérification…";
    private string _lastRefreshText = string.Empty;
    private string _errorText = string.Empty;
    private bool _isSettingsOpen;
    private bool _isHabitsPage;
    private readonly RelayCommand _saveSettingsCommand;
    private readonly RelayCommand _clearDataCommand;
    private readonly RelayCommand _exportDataCommand;
    private readonly RelayCommand _toggleSettingsCommand;
    private readonly RelayCommand _showOverviewCommand;
    private readonly RelayCommand _showHabitsCommand;

    public MainViewModel()
    {
        _saveSettingsCommand = new RelayCommand(SaveSettings);
        _clearDataCommand = new RelayCommand(ClearData);
        _exportDataCommand = new RelayCommand(ExportData);
        _toggleSettingsCommand = new RelayCommand(ToggleSettings);
        _showOverviewCommand = new RelayCommand(ShowOverview);
        _showHabitsCommand = new RelayCommand(ShowHabits);
        _dataService = new DashboardDataService(null);
        _habitsDataService = new HabitsDataService(null);
        _settingsService = new SettingsDataService(null);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();

        TimelinePlot = BuildEmptyPlot();
        TopAppsPlot = BuildEmptyPlot();
        TopSitesPlot = BuildEmptyPlot();
        HabitsPlot = BuildEmptyPlot();
        Habits = new HabitsSnapshot();

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

    public string AgentStatus
    {
        get => _agentStatus;
        private set { _agentStatus = value; Notify(); }
    }

    public string LastRefreshText
    {
        get => _lastRefreshText;
        private set { _lastRefreshText = value; Notify(); }
    }

    public string ErrorText
    {
        get => _errorText;
        private set { _errorText = value; Notify(); }
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        private set { _isSettingsOpen = value; Notify(); }
    }

    public bool IsHabitsPage
    {
        get => _isHabitsPage;
        private set { _isHabitsPage = value; Notify(); Notify(nameof(IsOverviewPage)); }
    }

    public bool IsOverviewPage => !IsHabitsPage;

    public ICommand SaveSettingsCommand => _saveSettingsCommand;
    public ICommand ClearDataCommand => _clearDataCommand;
    public ICommand ExportDataCommand => _exportDataCommand;
    public ICommand ToggleSettingsCommand => _toggleSettingsCommand;
    public ICommand ShowOverviewCommand => _showOverviewCommand;
    public ICommand ShowHabitsCommand => _showHabitsCommand;

    public PlotModel TimelinePlot { get; private set; }
    public PlotModel TopAppsPlot { get; private set; }
    public PlotModel TopSitesPlot { get; private set; }
    public PlotModel HabitsPlot { get; private set; }
    public HabitsSnapshot Habits { get; private set; }

    private void Refresh()
    {
        try
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
            RefreshHabits();
            ErrorText = string.Empty;
            LastRefreshText = $"Dernière lecture : {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            ErrorText = $"Impossible de lire les données : {ex.Message}";
        }

        try
        {
            AgentStatus = Process.GetProcessesByName("Tracker.Service").Length > 0
                ? "Agent actif"
                : "Agent arrêté";
        }
        catch
        {
            AgentStatus = "État inconnu";
        }
    }

    private void ApplyTimeline(IReadOnlyList<Tracker.Data.Models.TimelinePoint> points)
    {
        var labels = points.Select(p => p.BucketStartUtc.ToLocalTime().ToString("HH:mm")).ToArray();
        var values = points.Select(p => Math.Round(p.TotalSeconds / 60.0, 1)).ToArray();

        var model = BuildBasePlot();
        var xAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            GapWidth = 0.45,
            IsZoomEnabled = false,
            IsPanEnabled = false,
            TextColor = OxyColor.FromRgb(100, 116, 139),
            TicklineColor = OxyColors.Transparent,
            AxislineColor = OxyColors.Transparent
        };
        for (var i = 0; i < labels.Length; i++)
        {
            xAxis.Labels.Add(i % 2 == 0 ? labels[i] : string.Empty);
        }
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            MinimumPadding = 0,
            Minimum = 0,
            Maximum = Math.Max(60, Math.Ceiling(values.DefaultIfEmpty().Max() / 10.0) * 10),
            MajorStep = 10,
            MinorStep = 5,
            IsZoomEnabled = false,
            IsPanEnabled = false,
            TextColor = OxyColor.FromRgb(100, 116, 139),
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(234, 239, 245),
            MinorGridlineStyle = LineStyle.None,
            TicklineColor = OxyColors.Transparent,
            AxislineColor = OxyColors.Transparent,
            LabelFormatter = value => $"{value:0}m"
        };

        var lineColor = OxyColor.FromRgb(37, 99, 235);
        var series = new StemSeries
        {
            Color = lineColor,
            StrokeThickness = 3,
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

        TopAppsPlot = BuildBarPlot(labels, values, OxyColor.FromAColor(220, OxyColor.FromRgb(37, 99, 235)));
        Notify(nameof(TopAppsPlot));
    }

    private void ApplyTopSites(IReadOnlyList<Tracker.Data.Models.AggregateItem> items)
    {
        var labels = items.Select(i => i.Key).ToArray();
        var values = items.Select(i => Math.Round(i.TotalSeconds / 60.0, 1)).ToArray();

        TopSitesPlot = BuildBarPlot(labels, values, OxyColor.FromAColor(220, OxyColor.FromRgb(20, 137, 94)));
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

    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    private void ShowOverview() => IsHabitsPage = false;

    private void ShowHabits() => IsHabitsPage = true;

    private void RefreshHabits()
    {
        Habits = _habitsDataService.LoadSnapshot();
        Notify(nameof(Habits));
        HabitsPlot = BuildHabitsPlot(Habits.Days);
        Notify(nameof(HabitsPlot));
    }

    private void ClearData()
    {
        var answer = MessageBox.Show(
            "Supprimer définitivement toutes les sessions enregistrées ?",
            "Confirmer la suppression",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        _dataService.DeleteAllData();
        Refresh();
    }

    private void ExportData()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            FileName = $"tracker-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _dataService.ExportCsv(dialog.FileName);
            MessageBox.Show("Export terminé.", "Tracker", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorText = $"Export impossible : {ex.Message}";
        }
    }

    public void Dispose()
    {
        _timer.Stop();
    }

    private static PlotModel BuildBasePlot()
    {
        return new PlotModel
        {
            PlotAreaBorderColor = OxyColors.Transparent,
            TextColor = OxyColor.FromRgb(100, 116, 139),
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
            GapWidth = 0.35,
            IsZoomEnabled = false,
            IsPanEnabled = false,
            TextColor = OxyColor.FromRgb(100, 116, 139),
            TicklineColor = OxyColors.Transparent,
            AxislineColor = OxyColors.Transparent
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
            TextColor = OxyColor.FromRgb(100, 116, 139),
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(234, 239, 245),
            MinorGridlineStyle = LineStyle.None,
            TicklineColor = OxyColors.Transparent,
            AxislineColor = OxyColors.Transparent,
            LabelFormatter = value => $"{value:0}m"
        };

        var series = new BarSeries
        {
            FillColor = fill,
            StrokeColor = OxyColors.Transparent,
            StrokeThickness = 0,
            BarWidth = 0.8
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

    private static PlotModel BuildHabitsPlot(IReadOnlyList<HabitsDayPoint> days)
    {
        var model = new PlotModel
        {
            PlotAreaBorderColor = OxyColors.Transparent,
            TextColor = OxyColor.FromRgb(100, 116, 139),
            DefaultFontSize = 11,
            Background = OxyColors.Transparent,
        };
        model.Legends.Add(new Legend
        {
            IsLegendVisible = true,
            LegendPosition = LegendPosition.BottomCenter,
            LegendOrientation = LegendOrientation.Horizontal,
            LegendTextColor = OxyColor.FromRgb(100, 116, 139),
            LegendSymbolLength = 20,
            LegendFontSize = 11
        });

        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Left,
            GapWidth = 0.25,
            IsZoomEnabled = false,
            IsPanEnabled = false,
            TextColor = OxyColor.FromRgb(100, 116, 139),
            TicklineColor = OxyColors.Transparent,
            AxislineColor = OxyColors.Transparent
        };
        foreach (var day in days) categoryAxis.Labels.Add(day.Label);

        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            MinimumPadding = 0,
            MaximumPadding = 0.1,
            IsZoomEnabled = false,
            IsPanEnabled = false,
            TextColor = OxyColor.FromRgb(100, 116, 139),
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(234, 239, 245),
            MinorGridlineStyle = LineStyle.None,
            TicklineColor = OxyColors.Transparent,
            AxislineColor = OxyColors.Transparent,
            LabelFormatter = value => value >= 1 ? $"{value:0.#}h" : $"{value * 60:0}m"
        };

        var current = new BarSeries
        {
            Title = "Cette semaine",
            FillColor = OxyColor.FromRgb(59, 130, 246),
            StrokeColor = OxyColors.Transparent,
            BarWidth = 0.32,
            TrackerFormatString = "{0}\nCette semaine : {1:0.##}h"
        };
        var previous = new BarSeries
        {
            Title = "Semaine dernière",
            FillColor = OxyColor.FromRgb(203, 213, 225),
            StrokeColor = OxyColors.Transparent,
            BarWidth = 0.32,
            TrackerFormatString = "{0}\nSemaine dernière : {1:0.##}h"
        };
        foreach (var day in days)
        {
            current.Items.Add(new BarItem(day.CurrentHours));
            previous.Items.Add(new BarItem(day.PreviousHours));
        }

        model.Axes.Add(categoryAxis);
        model.Axes.Add(valueAxis);
        model.Series.Add(current);
        model.Series.Add(previous);
        return model;
    }
}
