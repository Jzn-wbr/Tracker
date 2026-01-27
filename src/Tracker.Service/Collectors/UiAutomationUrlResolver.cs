using System.Globalization;
using System.Windows.Automation;
using Tracker.Service.Models;
using Tracker.Service.Utils;

namespace Tracker.Service.Collectors;

public sealed class UiAutomationUrlResolver : IUrlResolver
{
    private static readonly string[] SupportedProcesses = { "chrome", "msedge", "firefox" };
    private static readonly string[] AddressNameHints = { "address", "adresse", "search", "recherche", "url" };

    public string? ResolveUrl(ActiveWindowInfo info)
    {
        if (!SupportedProcesses.Contains(info.ProcessName, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var element = AutomationElement.FromHandle(info.WindowHandle);
            if (element == null)
            {
                return null;
            }

            var edit = FindAddressBar(element);
            if (edit == null)
            {
                return null;
            }

            if (edit.TryGetCurrentPattern(ValuePattern.Pattern, out var patternObj)
                && patternObj is ValuePattern valuePattern)
            {
                var rawValue = valuePattern.Current.Value;
                var normalized = UrlNormalizer.Normalize(rawValue);
                return normalized;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static AutomationElement? FindAddressBar(AutomationElement root)
    {
        var edits = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
        if (edits == null || edits.Count == 0)
        {
            return null;
        }

        foreach (AutomationElement edit in edits)
        {
            var name = edit.Current.Name ?? string.Empty;
            if (IsAddressName(name))
            {
                return edit;
            }
        }

        foreach (AutomationElement edit in edits)
        {
            if (edit.TryGetCurrentPattern(ValuePattern.Pattern, out var patternObj)
                && patternObj is ValuePattern valuePattern)
            {
                var rawValue = valuePattern.Current.Value;
                if (UrlNormalizer.IsProbablyUrl(rawValue))
                {
                    return edit;
                }
            }
        }

        return null;
    }

    private static bool IsAddressName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var lower = name.Trim().ToLower(CultureInfo.InvariantCulture);
        return AddressNameHints.Any(hint => lower.Contains(hint, StringComparison.Ordinal));
    }
}
