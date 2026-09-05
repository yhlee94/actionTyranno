#define MyAppName "ActionTyranno"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "ActionTyranno"
#define MyAppExeName "ActionTyranno.App.exe"

[Setup]
AppId={{88D86DD6-197E-44FE-BA39-987C52CBC3F7}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Per-user install under LocalAppData - no admin/UAC prompt required.
PrivilegesRequired=lowest
OutputDir=output
OutputBaseFilename=ActionTyranno-Setup
SetupIconFile=..\src\ActionTyranno.App\Assets\AppIcon.ico
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
WizardStyle=modern
; Copyright / usage-restriction agreement the user must accept before installing.
LicenseFile=LICENSE.txt
; Install key required to run the installer at all (Setup -> Enter Password screen).
Password=tyrannodev9401
Encryption=yes

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\win-x64\ActionTyranno.App.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
