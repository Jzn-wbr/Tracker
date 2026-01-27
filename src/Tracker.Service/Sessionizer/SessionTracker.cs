using Microsoft.Extensions.Options;
using Tracker.Service.Models;
using Tracker.Service.Options;

namespace Tracker.Service.Sessionizer;

public sealed class SessionTracker
{
    private readonly int _minSessionSeconds;
    private readonly int _flushSeconds;
    private ActiveWindowInfo? _current;
    private DateTime _currentStartUtc;
    private DateTime _lastEmitUtc;

    public SessionTracker(IOptions<TrackingOptions> options)
    {
        _minSessionSeconds = Math.Max(1, options.Value.MinSessionSeconds);
        _flushSeconds = Math.Max(5, options.Value.FlushSeconds);
    }

    public SessionRecord? Update(ActiveWindowInfo? info, DateTime utcNow)
    {
        if (_current == null)
        {
            if (info == null)
            {
                return null;
            }

            _current = info;
            _currentStartUtc = utcNow;
            _lastEmitUtc = utcNow;
            return null;
        }

        if (info != null && _current.IsSameSessionKey(info))
        {
            if ((utcNow - _lastEmitUtc).TotalSeconds < _flushSeconds)
            {
                return null;
            }

            var flushed = BuildSession(_current, _currentStartUtc, utcNow);
            _currentStartUtc = utcNow;
            _lastEmitUtc = utcNow;
            return flushed;
        }

        var completed = BuildSession(_current, _currentStartUtc, utcNow);
        _current = info;
        _currentStartUtc = info != null ? utcNow : default;
        _lastEmitUtc = info != null ? utcNow : default;

        return completed;
    }

    public SessionRecord? Complete(DateTime utcNow)
    {
        if (_current == null)
        {
            return null;
        }

        var completed = BuildSession(_current, _currentStartUtc, utcNow);
        _current = null;
        _currentStartUtc = default;
        _lastEmitUtc = default;
        return completed;
    }

    private SessionRecord? BuildSession(ActiveWindowInfo info, DateTime startUtc, DateTime endUtc)
    {
        var urlDomain = Utils.UrlNormalizer.GetDomain(info.Url);
        var record = new SessionRecord(
            startUtc,
            endUtc,
            info.ProcessName,
            info.ProcessPath,
            info.WindowTitle,
            info.Url,
            urlDomain);
        return record.DurationSeconds >= _minSessionSeconds ? record : null;
    }
}
