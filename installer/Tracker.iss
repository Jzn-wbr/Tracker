[Setup]
AppId={{D2F28B06-6844-4A62-9AD0-1CEB1B5C4C3B}
AppName=Tracker
AppVersion=0.1.0
AppPublisher=Tracker
DefaultDirName={autopf}\Tracker
DefaultGroupName=Tracker
DisableProgramGroupPage=yes
OutputBaseFilename=TrackerSetup
OutputDir=..\dist\installer
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
SetupIconFile=..\icons\favicon.ico
CloseApplications=yes
CloseApplicationsFilter=Tracker.Service.exe
RestartApplications=no
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\Tracker.Desktop.exe

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Creer un raccourci sur le bureau"; GroupDescription: "Raccourcis:"; Flags: unchecked

[Files]
Source: "..\src\Tracker.Desktop\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\src\Tracker.Service\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}\service"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "TrackerWatchdog.ps1"; DestDir: "{app}\service"; Flags: ignoreversion
Source: "TrackerWatchdog.vbs"; DestDir: "{app}\service"; Flags: ignoreversion
Source: "TrackerTaskSettings.ps1"; DestDir: "{app}\service"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Tracker"; Filename: "{app}\Tracker.Desktop.exe"
Name: "{autodesktop}\Tracker"; Filename: "{app}\Tracker.Desktop.exe"; Tasks: desktopicon

[Run]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM Tracker.Service.exe /T"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "stop ""Tracker Service"""; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "delete ""Tracker Service"""; Flags: runhidden
Filename: "{sys}\schtasks.exe"; Parameters: "/Create /TN ""Tracker Agent"" /SC ONLOGON /RL HIGHEST /RU ""{username}"" /IT /TR ""\""{app}\service\Tracker.Service.exe\"""" /F"; Flags: runhidden
Filename: "{sys}\schtasks.exe"; Parameters: "/Create /TN ""Tracker Watchdog"" /SC MINUTE /MO 1 /RL HIGHEST /RU ""{username}"" /IT /TR ""\""{sys}\wscript.exe\"" \""{app}\service\TrackerWatchdog.vbs\"""" /F"; Flags: runhidden
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\service\TrackerTaskSettings.ps1"""; Flags: runhidden
Filename: "{sys}\schtasks.exe"; Parameters: "/Run /TN ""Tracker Agent"""; Flags: runhidden
Filename: "{app}\Tracker.Desktop.exe"; Description: "Lancer Tracker"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\schtasks.exe"; Parameters: "/End /TN ""Tracker Agent"""; Flags: runhidden
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /TN ""Tracker Agent"" /F"; Flags: runhidden
Filename: "{sys}\schtasks.exe"; Parameters: "/End /TN ""Tracker Watchdog"""; Flags: runhidden
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /TN ""Tracker Watchdog"" /F"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "stop ""Tracker Service"""; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "delete ""Tracker Service"""; Flags: runhidden

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM Tracker.Service.exe /T', '', SW_HIDE,
    ewWaitUntilTerminated, ResultCode);
  Result := True;
end;
