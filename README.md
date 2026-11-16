# PodGlide — Your sound, one tap away

A Windows 11 x64, C# .NET 8 WPF tray application. No third-party packages, account, administrator rights, or background network access are required.

## Portable release

Download PodGlide-Portable-Windows-x64.zip, extract the whole folder, then run PodGlide.exe. The runtime is included. Fresh portable settings are stored in Data next to the app; no personal device IDs or logs are included. See PORTABLE-README.txt.

## Run the developer / installed build

1. Extract the entire ZIP into a permanent folder.
2. Install the **Microsoft .NET 8 Desktop Runtime, x64** if absent: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
3. Double-click **PodGlide.exe**. Keep the EXE, DLL and runtimeconfig JSON together.
4. Click its information icon in the notification area (possibly under the hidden-icons arrow), then **Settings**.
5. Connect your AirPods Pro in Windows Bluetooth settings. Choose their playback endpoint, and choose your PC speakers. Save.

The included EXE is a small .NET Framework launcher that starts the compiled .NET 8 WPF DLL using the system's x64 dotnet host. Windows 11 normally includes .NET Framework 4.x. This is a framework-dependent build, not a self-contained or signed installer. The standard SDK build below produces a normal .NET app executable instead.

## Everyday use

- A rounded dark popup appears at the primary display's bottom right when a matching audio endpoint becomes active. It does not activate itself or steal keyboard focus. Left-click the tray icon to reopen it; right-click for Settings, Sound settings, and Exit.
- Click an available AirPods or saved PC speakers button to make it the default playback output. A checkmark shows the current multimedia default.
- The app changes all three playback roles: console, multimedia, communications. It checks the defaults after switching, and reports failures. Partial changes are possible if Windows rejects a later role; use Sound settings to correct them.
- Popup closes after 20 seconds, pauses while hovered or keyboard-focused, and supports Escape and a dismiss button.
- Connection auto-switch is **off by default**. Enable it in Settings if wanted. It triggers only on new active endpoints after the initial scan, not merely because the app started.
- Disconnection shows a popup offering the saved speakers. The app does not force speaker switching on disconnect; Windows may choose a fallback itself.
- Optional **Start when I sign in to Windows** uses only the current user's Registry Run entry. `--startup` launches quietly. Save in the final folder before enabling it; moving the folder requires disabling and re-enabling startup.
- To refresh the output selectors after connecting hardware, close and reopen Settings.

## Detection and implementation

The app subscribes to Core Audio `IMMNotificationClient`, debounces notifications for 700 ms on the WPF dispatcher, and also rescans every five seconds for recovery after missed notifications or sleep. It enumerates **active render endpoints**, not microphones or a list of paired Bluetooth devices. A saved endpoint ID takes precedence over the editable name match (default: `AirPods`, case insensitive). Use explicit selection if several AirPods endpoints exist, or if your device has a custom name. Re-pairing can change endpoint IDs; select it again in Settings.

Switching uses the undocumented Windows `IPolicyConfig::SetDefaultEndpoint` interface. Windows has no supported public equivalent for this system-wide behavior. It works on the tested Windows build but could change in future versions. Windows Sound settings remains available from the popup and tray.

## Build with the .NET 8 SDK (recommended)

Install the .NET 8 SDK x64 from Microsoft. In PowerShell in this folder:

```powershell
.\build.ps1
# Output: .\publish\PodGlide.exe (requires .NET 8 Desktop Runtime x64)

.\build.ps1 -SelfContained
# Output: .\publish\PodGlide.exe (includes the runtime; larger download)
```

Equivalent command:

```powershell
dotnet publish .\AirPodsSwitch.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\publish
```

SDK publishing may need internet access to restore Windows runtime/build packs. The SDK build could not be executed here: no SDK was installed and the Microsoft installer download failed. The delivered DLL was compiled successfully against the installed .NET 8.0.32 runtime assemblies using the Windows C# compiler, then executed on .NET 8. `BuildSupport\build-local.ps1` reproduces that alternative build without an SDK; exit the running app first. It is not a replacement for normal SDK release validation.

## Diagnostics and tests

```powershell
dotnet .\AirPodsSwitch.dll --diagnose --report .\diagnostics.txt
dotnet .\AirPodsSwitch.dll --render-test --report .\popup-test.png
# Optional: invokes switching, only if all roles already use the same endpoint:
dotnet .\AirPodsSwitch.dll --diagnose --test-current-switch --report .\switch-test.txt
```

The first command is read-only. The render test opens and closes a sample WPF popup with the production popup layout and button styling; it does not launch the tray controller or alter preferences. The optional switch test re-selects the current device for all three roles and verifies the result. It refuses if those roles differ. The launcher forwards only `--startup`; use the DLL commands for diagnostics.

See **TEST-RESULTS.txt** for actual results and **LIMITATIONS.md** for remaining hardware checks. `popup-preview.png` is the rendered disconnected-state sample, not evidence of a Bluetooth connection.

## Storage, troubleshooting, removal

- Preferences: `%LOCALAPPDATA%\AirPodsSwitch\settings.xml`.
- Errors: `%LOCALAPPDATA%\AirPodsSwitch\app.log`. Local logs may include endpoint identifiers; they are never uploaded. Log size is currently not capped.
- Startup: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, value `AirPodsSwitch`.
- If startup does not run, check Windows Startup apps; Windows or organization policy can disable it.
- If AirPods are absent, first connect them and verify they appear in Windows Sound settings, then reopen app Settings. Select the intended stereo endpoint rather than a hands-free endpoint when both appear.
- If switching succeeds but an app keeps playing elsewhere, select System default in that app or restart playback. Per-app routing and exclusive-mode streams may ignore default-device changes.
- To remove: turn off startup and save, choose Exit in the tray menu, delete the extracted folder, and optionally delete the app's LocalAppData folder. No service or driver is installed.

## API references

- https://learn.microsoft.com/en-us/windows/win32/coreaudio/mmdevice-api
- https://learn.microsoft.com/en-us/windows/win32/api/mmdeviceapi/nn-mmdeviceapi-immnotificationclient
- https://github.com/microsoft/psi/blob/master/Sources/Audio/Microsoft.Psi.Audio.Windows/ComInterop/Guids.cs
- https://github.com/frgnca/AudioDeviceCmdlets/blob/master/SOURCE/PolicyConfigClient.cs (reference for undocumented interface identity/order)

## Design and project license

The charcoal and mint interface includes an AirPods Pro product image from Apple. See THIRD-PARTY-NOTICES.md for artwork and runtime attribution. Original application code is MIT licensed. The previous AirPodsSwitch name remains in some internal filenames and preference locations for update compatibility.
