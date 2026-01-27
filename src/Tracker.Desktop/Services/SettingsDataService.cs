using Microsoft.Win32;
using Tracker.Data;
using Tracker.Data.Models;
using Tracker.Data.Repositories;

namespace Tracker.Desktop.Services;

public sealed class SettingsDataService
{
    private readonly ISettingsRepository _repository;

    public SettingsDataService(string? databasePath)
    {
        var db = new TrackerDb(databasePath ?? DatabasePathResolver.Resolve());
        _repository = new SettingsRepository(db);
    }

    public UserSettings Load()
    {
        return _repository.GetSettings();
    }

    public void Save(UserSettings settings)
    {
        _repository.SaveSettings(settings);
    }

    public void ApplyAutoStart(bool enabled)
    {
        const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string valueName = "Tracker";
        using var key = Registry.CurrentUser.CreateSubKey(keyPath);

        if (enabled)
        {
            var exePath = Environment.ProcessPath ?? string.Empty;
            key?.SetValue(valueName, $"\"{exePath}\"");
        }
        else
        {
            key?.DeleteValue(valueName, false);
        }
    }
}
