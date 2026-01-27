$taskNames = @("Tracker Agent", "Tracker Watchdog")

foreach ($name in $taskNames)
{
    try
    {
        $task = Get-ScheduledTask -TaskName $name -ErrorAction Stop
        $settings = $task.Settings
        $settings.DisallowStartIfOnBatteries = $false
        $settings.StopIfGoingOnBatteries = $false
        Set-ScheduledTask -TaskName $name -Settings $settings | Out-Null
    }
    catch
    {
    }
}
