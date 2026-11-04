; Inno Setup script for the RockBlack Windows x64 installer.
; Build: ISCC.exe /DAppVersion=7.25.6 /DSourceDir=<publish dir> /DOutputDir=<out dir> rockblack.iss
; Produces rockblack-windows-x64-setup.exe.
;
; Per-user install into %LOCALAPPDATA%\Programs\RockBlack (no admin rights needed). The
; install directory stays writable, so the app keeps its portable layout (guiConfigs, bin)
; and the built-in self-update (portable zip from GitHub releases) keeps working.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #error SourceDir must be defined
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif

[Setup]
AppId={{5B0C1E2A-7D36-4C8E-9F1B-26D6B05F6062}
AppName=RockBlack
AppVersion={#AppVersion}
AppVerName=RockBlack {#AppVersion}
AppPublisher=RockBlack
AppPublisherURL=https://my.rockblack.tech
AppSupportURL=https://github.com/RockBlack-VPN/rockblack/issues
AppUpdatesURL=https://github.com/RockBlack-VPN/rockblack/releases
AppCopyright=RockBlack, based on v2rayN by 2dust (GPL-3.0)
VersionInfoVersion={#AppVersion}
VersionInfoProductName=RockBlack
DefaultDirName={localappdata}\Programs\RockBlack
DefaultGroupName=RockBlack
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=rockblack-windows-x64-setup
SetupIconFile=..\v2rayN\v2rayN\Resources\v2rayN.ico
UninstallDisplayIcon={app}\RockBlack.exe
UninstallDisplayName=RockBlack
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\RockBlack"; Filename: "{app}\RockBlack.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\RockBlack"; Filename: "{app}\RockBlack.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\RockBlack.exe"; Description: "{cm:LaunchProgram,RockBlack}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{cmd}"; Parameters: "/C taskkill /F /T /IM RockBlack.exe"; Flags: runhidden; RunOnceId: "KillRockBlack"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\guiTemps"
Type: filesandordirs; Name: "{app}\guiLogs"
