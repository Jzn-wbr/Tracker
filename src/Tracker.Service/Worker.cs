using Microsoft.Extensions.Options;
using Tracker.Service.Collectors;
using Tracker.Service.Models;
using Tracker.Data.Models;
using Tracker.Data.Repositories;
using Tracker.Service.Options;
using Tracker.Service.Sessionizer;
using Tracker.Service.Settings;
using Tracker.Service.Diagnostics;

namespace Tracker.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IActiveWindowReader _windowReader;
    private readonly IUrlResolver _urlResolver;
    private readonly SessionTracker _sessionTracker;
    private readonly ISessionRepository _sessionRepository;
    private readonly TrackingSettingsCache _settingsCache;
    private readonly TrackingOptions _options;

    public Worker(
        ILogger<Worker> logger,
        IActiveWindowReader windowReader,
        IUrlResolver urlResolver,
        SessionTracker sessionTracker,
        ISessionRepository sessionRepository,
        TrackingSettingsCache settingsCache,
        IOptions<TrackingOptions> options)
    {
        _logger = logger;
        _windowReader = windowReader;
        _urlResolver = urlResolver;
        _sessionTracker = sessionTracker;
        _sessionRepository = sessionRepository;
        _settingsCache = settingsCache;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ServiceDiagnostics.Log("Tracker agent started.");
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.TickSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var nowUtc = DateTime.UtcNow;
                var settings = _settingsCache.GetSnapshot();
                if (settings.PauseTracking)
                {
                    PersistCompletedSession(_sessionTracker.Update(null, nowUtc));
                    continue;
                }

                ActiveWindowInfo? info = null;

                try
                {
                    info = _windowReader.GetActiveWindow();
                    if (info != null)
                    {
                        var url = _urlResolver.ResolveUrl(info);
                        info = new ActiveWindowInfo(
                            info.WindowHandle,
                            info.ProcessName,
                            info.ProcessPath,
                            info.ProcessId,
                            info.WindowTitle,
                            url);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read active window.");
                    ServiceDiagnostics.Log($"Active window read failed: {ex.Message}");
                }

                if (info != null && IsExcluded(settings, info))
                {
                    info = null;
                }

                var completed = _sessionTracker.Update(info, nowUtc);
                PersistCompletedSession(completed);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ServiceDiagnostics.Log($"Worker exception: {ex}");
        }
        finally
        {
            var finalSession = _sessionTracker.Complete(DateTime.UtcNow);
            if (finalSession != null)
            {
                try
                {
                    _sessionRepository.Insert(Map(finalSession));
                    ServiceDiagnostics.Log(
                        $"Final session saved: {finalSession.ProcessName} {finalSession.WindowTitle} {finalSession.DurationSeconds}s");
                }
                catch (Exception ex)
                {
                    ServiceDiagnostics.Log($"Final session save failed: {ex.Message}");
                }
            }
        }
    }

    private void PersistCompletedSession(SessionRecord? completed)
    {
        if (completed == null)
        {
            return;
        }

        try
        {
            _sessionRepository.Insert(Map(completed));
            ServiceDiagnostics.Log($"Session saved: {completed.ProcessName} {completed.WindowTitle} {completed.DurationSeconds}s");
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Session saved: {App} {Title} {Seconds}s", completed.ProcessName, completed.WindowTitle, completed.DurationSeconds);
            }
        }
        catch (Exception ex)
        {
            ServiceDiagnostics.Log($"Session save failed: {ex.Message}");
        }
    }

    private static SessionWriteModel Map(SessionRecord record)
    {
        return new SessionWriteModel
        {
            StartUtc = record.StartUtc,
            EndUtc = record.EndUtc,
            DurationSeconds = record.DurationSeconds,
            ProcessName = record.ProcessName,
            ProcessPath = record.ProcessPath ?? string.Empty,
            WindowTitle = record.WindowTitle ?? string.Empty,
            Url = record.Url ?? string.Empty,
            UrlDomain = record.UrlDomain ?? string.Empty
        };
    }

    private static bool IsExcluded(TrackingSettings settings, ActiveWindowInfo info)
    {
        if (settings.ExcludedApps.Contains(info.ProcessName))
        {
            return true;
        }

        var domain = info.Url != null ? Tracker.Service.Utils.UrlNormalizer.GetDomain(info.Url) : null;
        if (!string.IsNullOrWhiteSpace(domain) && settings.ExcludedSites.Contains(domain))
        {
            return true;
        }

        return false;
    }
}
