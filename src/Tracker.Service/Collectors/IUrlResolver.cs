using Tracker.Service.Models;

namespace Tracker.Service.Collectors;

public interface IUrlResolver
{
    string? ResolveUrl(ActiveWindowInfo info);
}
