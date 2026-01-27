using Tracker.Data.Models;

namespace Tracker.Data.Repositories;

public interface ISettingsRepository
{
    UserSettings GetSettings();
    void SaveSettings(UserSettings settings);
}
