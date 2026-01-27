using Tracker.Service.Models;

namespace Tracker.Service.Collectors;

public interface IActiveWindowReader
{
    ActiveWindowInfo? GetActiveWindow();
}
