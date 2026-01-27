using Tracker.Service;
using Tracker.Data;
using Tracker.Data.Repositories;
using Microsoft.Extensions.Options;
using Tracker.Service.Collectors;
using Tracker.Service.Options;
using Tracker.Service.Sessionizer;
using Tracker.Service.Settings;
using Tracker.Service.Diagnostics;

var builder = Host.CreateApplicationBuilder(args);
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    ServiceDiagnostics.Log($"Unhandled exception: {e.ExceptionObject}");
};

builder.Services.Configure<TrackingOptions>(builder.Configuration.GetSection("Tracking"));
builder.Services.AddSingleton(sp =>
{
    var trackingOptions = sp.GetRequiredService<IOptions<TrackingOptions>>().Value;
    return new TrackerDb(trackingOptions.DatabasePath);
});
builder.Services.AddSingleton<IActiveWindowReader, Win32ActiveWindowReader>();
builder.Services.AddSingleton<IUrlResolver, UiAutomationUrlResolver>();
builder.Services.AddSingleton<SessionTracker>();
builder.Services.AddSingleton<ISessionRepository, SessionRepository>();
builder.Services.AddSingleton<ISettingsRepository, SettingsRepository>();
builder.Services.AddSingleton<TrackingSettingsCache>();
builder.Services.AddWindowsService(options => { options.ServiceName = "Tracker Service"; });
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
