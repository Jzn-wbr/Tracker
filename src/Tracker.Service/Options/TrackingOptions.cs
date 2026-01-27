namespace Tracker.Service.Options;

using System.IO;

public class TrackingOptions
{
    public int TickSeconds { get; set; } = 1;
    public int MinSessionSeconds { get; set; } = 2;
    public int FlushSeconds { get; set; } = 60;
    public string? DatabasePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Tracker",
        "tracker.db");
}
