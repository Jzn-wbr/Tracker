namespace Tracker.Service.Utils;

public static class UrlNormalizer
{
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith("chrome://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("edge://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!trimmed.Contains("://", StringComparison.Ordinal))
        {
            trimmed = "https://" + trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var normalized = uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
        return normalized.TrimEnd('/');
    }

    public static string? GetDomain(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri.Host;
    }

    public static bool IsProbablyUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return raw.Contains("://", StringComparison.Ordinal)
            || raw.Contains('.', StringComparison.Ordinal);
    }
}
