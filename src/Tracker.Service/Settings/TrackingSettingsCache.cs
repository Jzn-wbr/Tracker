using Tracker.Data.Repositories;

namespace Tracker.Service.Settings;

public sealed class TrackingSettingsCache
{
    private readonly ISettingsRepository _repository;
    private DateTime _lastLoadUtc = DateTime.MinValue;
    private TrackingSettings _current = new();
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(10);

    public TrackingSettingsCache(ISettingsRepository repository)
    {
        _repository = repository;
        Load();
    }

    public TrackingSettings GetSnapshot()
    {
        if (DateTime.UtcNow - _lastLoadUtc >= _refreshInterval)
        {
            Load();
        }

        return _current;
    }

    private void Load()
    {
        var settings = _repository.GetSettings();
        _current = new TrackingSettings
        {
            PauseTracking = settings.PauseTracking,
            ExcludedApps = ParseList(settings.ExcludedApps),
            ExcludedSites = ParseList(settings.ExcludedSites)
        };
        _lastLoadUtc = DateTime.UtcNow;
    }

    private static HashSet<string> ParseList(string? raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return set;
        }

        var parts = raw.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0)
            {
                set.Add(trimmed);
            }
        }

        return set;
    }
}
