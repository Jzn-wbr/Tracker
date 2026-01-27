$serviceExe = Join-Path $PSScriptRoot "Tracker.Service.exe"
$logRoot = if ($env:LOCALAPPDATA) { $env:LOCALAPPDATA } else { $env:ProgramData }
$logPath = Join-Path $logRoot "Tracker\watchdog.log"

function Write-Log($message)
{
    try
    {
        $dir = Split-Path $logPath -Parent
        if ($dir -and -not (Test-Path $dir))
        {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
        $line = "{0} {1}`r`n" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $message
        Add-Content -Path $logPath -Value $line
    }
    catch
    {
    }
}

if (-not (Test-Path $serviceExe))
{
    Write-Log "Service exe missing."
    exit 0
}

$running = Get-Process -Name "Tracker.Service" -ErrorAction SilentlyContinue
if ($null -ne $running)
{
    exit 0
}

try
{
    Write-Log "Starting Tracker.Service.exe"
    $proc = Start-Process -FilePath $serviceExe -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -PassThru
    Start-Sleep -Seconds 2
    if ($proc.HasExited)
    {
        Write-Log ("Service exited with code {0}" -f $proc.ExitCode)
    }
}
catch
{
    Write-Log ("Failed to start service: {0}" -f $_.Exception.Message)
}
