#ifndef PayloadDir
  #define PayloadDir "..\..\PodGlide-Portable"
#endif
#ifndef ReleaseDir
  #define ReleaseDir "..\.."
#endif

[Setup]
AppId={{67D68F1D-7E6C-420D-BAF6-A8410DBB4F58}
AppName=PodGlide
AppVersion=1.2.0
AppVerName=PodGlide 1.2
AppPublisher=MD SOWROVE HOSSAIN
AppPublisherURL=https://www.facebook.com/mhsowrove
AppSupportURL=mailto:mhsowrove@gmail.com
DefaultDirName={localappdata}\Programs\AirPodsSwitch
DefaultGroupName=PodGlide
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir={#ReleaseDir}
OutputBaseFilename=PodGlide-1.2-Setup-Windows-x64
SetupIconFile=..\Assets\PodGlide.ico
UninstallDisplayIcon={app}\Assets\PodGlide.ico
WizardStyle=modern
Compression=lzma2/fast
SolidCompression=yes
LicenseFile=..\LICENSE
CloseApplications=yes
RestartApplications=no
AppMutex=Local\AirPodsSwitch.Tray
DisableProgramGroupPage=yes
UninstallDisplayName=PodGlide
VersionInfoProductName=PodGlide
VersionInfoDescription=PodGlide standalone installer
VersionInfoCompany=MD SOWROVE HOSSAIN

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"
Name: "startup"; Description: "Launch PodGlide when I sign in"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Excludes: "portable.flag,Data\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\PodGlide"; Filename: "{app}\PodGlide.exe"; IconFilename: "{app}\Assets\PodGlide.ico"
Name: "{autodesktop}\PodGlide"; Filename: "{app}\PodGlide.exe"; IconFilename: "{app}\Assets\PodGlide.ico"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AirPodsSwitch"; ValueData: """{app}\PodGlide.exe"" --startup"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\PodGlide.exe"; Description: "Launch PodGlide"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\portable.flag"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    DeleteFile(ExpandConstant('{app}\portable.flag'));
end;
