#ifndef PayloadDir
  #define PayloadDir "..\..\ProGlide-Compact-Portable"
#endif
#ifndef ReleaseDir
  #define ReleaseDir "..\.."
#endif

[Setup]
AppId={{67D68F1D-7E6C-420D-BAF6-A8410DBB4F58}
AppName=ProGlide
AppVersion=1.4.0
AppVerName=ProGlide 1.4
AppPublisher=MD SOWROVE HOSSAIN
AppPublisherURL=https://www.facebook.com/mhsowrove
AppSupportURL=mailto:mhsowrove@gmail.com
DefaultDirName={localappdata}\Programs\AirPodsSwitch
DefaultGroupName=ProGlide
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir={#ReleaseDir}
OutputBaseFilename=ProGlide-1.4-Compact-Setup
SetupIconFile=..\Assets\PodGlide.ico
UninstallDisplayIcon={app}\ProGlide.exe
WizardStyle=modern
Compression=lzma2/fast
SolidCompression=yes
LicenseFile=..\LICENSE
CloseApplications=yes
RestartApplications=no
AppMutex=Local\AirPodsSwitch.Tray
DisableProgramGroupPage=yes
UninstallDisplayName=ProGlide
VersionInfoProductName=ProGlide
VersionInfoDescription=ProGlide standalone installer
VersionInfoCompany=MD SOWROVE HOSSAIN

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"
Name: "startup"; Description: "Launch ProGlide when I sign in"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Excludes: "portable.flag,Data\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\ProGlide"; Filename: "{app}\ProGlide.exe"; IconFilename: "{app}\ProGlide.exe"
Name: "{autodesktop}\ProGlide"; Filename: "{app}\ProGlide.exe"; IconFilename: "{app}\ProGlide.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AirPodsSwitch"; ValueData: """{app}\ProGlide.exe"" --startup"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\ProGlide.exe"; Description: "Launch ProGlide"; Flags: nowait postinstall skipifsilent

[InstallDelete]
Type: filesandordirs; Name: "{app}\Runtime"; Check: LegacyRuntime
Type: files; Name: "{autodesktop}\PodGlide.lnk"
Type: files; Name: "{autoprograms}\PodGlide.lnk"
Type: files; Name: "{app}\PodGlide.exe"

[UninstallDelete]
Type: files; Name: "{app}\portable.flag"

[Code]
function LegacyRuntime: Boolean;
begin
  Result := FileExists(ExpandConstant('{app}\AirPodsSwitch.dll')) and
            FileExists(ExpandConstant('{app}\AirPodsSwitch.runtimeconfig.json')) and
            FileExists(ExpandConstant('{app}\Runtime\dotnet.exe'));
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    DeleteFile(ExpandConstant('{app}\portable.flag'));
end;
