# ProGlide 1.4 Compact

A small Windows 11 x64 tray app for AirPods audio switching, connection popups, and nearby AirPods battery broadcasts.

## Downloads and running

- **ProGlide-1.4-Compact-Setup.exe**: offline per-user installer, Start menu/desktop shortcuts, optional startup and Windows uninstaller.
- **ProGlide.exe**: the app itself, with all icons and artwork embedded. This single file can run directly and be shared.
- **ProGlide-Compact-Portable.zip**: the app, licenses and a portable-mode marker. Extract first; settings stay in its Data folder.

No .NET 8 download, bundled runtime, service, driver, or third-party runtime package is required. The compact build targets the **.NET Framework 4.8 already included with Windows 11**, and uses built-in Windows Bluetooth APIs. It is not independent of Windows' system components. Earlier 63 MB installers included the entire .NET 8 runtime; this version does not.

Exit another running copy before launching. Open the AirPods tray icon and Preferences, connect your AirPods in Windows, then select the AirPods and speaker outputs. Reopen Preferences to refresh selectors after connecting hardware. New installs default to name detection and leave auto-switch/startup off; existing audio preferences are preserved on upgrade.

## Features

- Connection: closed-to-open lid animation and green connected badge.
- Disconnection: open-to-closed animation and red disconnected badge. An already-offline popup starts closed.
- ProGlide name paired with the AirPods icon, plus Preferences, Sound settings and About buttons.
- Light and Dark themes preview instantly. Save persists the selection; closing without saving restores the saved theme.
- Separate animation and battery-monitoring toggles in Preferences.
- About credits MD SOWROVE HOSSAIN, with supplied Gmail, Facebook, bKash and PayPal links. bKash copies a number; PayPal opens a link. The app processes no payments.

## Battery readings

The app passively reads recognized AirPods manufacturer broadcasts through Windows' Bluetooth LE watcher. Left, right and case levels appear when present, with charging flags, in **10% steps**. A dash means the packet did not include that level. Readings expire after 60 seconds. Open the case close to the PC to encourage new broadcasts.

Broadcasts have rotating addresses and do not prove ownership. The UI labels them **nearby AirPods battery**; another nearby pair can be selected. Unknown models and weak/malformed packets are ignored. Bluetooth scanning can be disabled. No Bluetooth addresses are saved, and no data is sent over the internet. Firmware, Bluetooth adapter and broadcast availability affect compatibility.

A local 20-second test observed a nearby AirPods Pro 3 packet with right earbud 90%, while left/case were absent. That was an actual broadcast, not a guarantee about the user's paired device. See BATTERY-TEST.txt.

## Build without an SDK

On Windows 11, run in PowerShell:

```powershell
.\build.ps1
```

This uses the Windows C# compiler and .NET Framework/WPF/WinRT metadata already installed on Windows 11. Produces ProGlide.exe. Exit the running app first if rebuilding that same copy. No NuGet packages are used.

An SDK project targeting net48 is also provided for development, but the verified build here used build.ps1, not an SDK.

To build the compact installer with the official Inno Setup compiler:

```powershell
.\BuildSupport\build-installer.ps1 -CompilerPath 'C:\Path\To\Inno Setup 6\ISCC.exe'
```

## Diagnostics

```powershell
# Wait for these GUI executables to finish before reading their reports.
Start-Process .\ProGlide.exe -ArgumentList '--diagnose --report diagnostics.txt' -Wait
Start-Process .\ProGlide.exe -ArgumentList '--test-animation --report opening.txt' -Wait
Start-Process .\ProGlide.exe -ArgumentList '--test-animation --disconnect --report closing.txt' -Wait
Start-Process .\ProGlide.exe -ArgumentList '--battery-scan --report battery.txt' -Wait
```

The battery test runs for 20 seconds. Diagnostics does not switch audio by default. Optional --test-current-switch re-selects the existing device only if all three roles already agree. Render helpers --render-test, --render-preferences and --render-about accept --report image.png and --light. Render samples are illustrations, not hardware observations.

## Technical limits and storage

Detection uses active Windows playback endpoints, with notification debounce plus a recovery scan. It is not physical case-lid or ear-placement detection. The front-facing animation is rendered from Apple's original Pro 3 USDZ geometry with an app-authored hinge motion, camera and lighting. Frames have true alpha transparency, with no white image tile. It is not Apple's private pairing animation or an untouched Apple movie. See ARTWORK-PROVENANCE.md.

Default playback switching affects console, multimedia and communications roles through undocumented IPolicyConfig. Windows updates can affect it; partial role changes are reported. Per-app routing and exclusive audio can ignore the default change. No microphone settings are changed. Disconnecting offers speakers rather than forcing a fallback.

Installed/direct-exe settings: %LOCALAPPDATA%\AirPodsSwitch\settings.xml. The historical folder/value name preserves compatibility. A portable.flag beside the EXE instead uses Data beside the app. Error logs stay local and are not size-capped. Do not include Data when sharing a used portable copy. Startup uses the current user's Run value AirPodsSwitch.

The app is unsigned and targets Windows 11 x64. No clean second PC or fresh Windows account was available for validation. See RELEASE-NOTES.md and local test reports for actual verification. The original code is MIT licensed; artwork and protocol references have separate notices in THIRD-PARTY-NOTICES.md.

## Compact controls and battery cards (1.4)

Preferences uses a shorter fixed-height window with expandable name matching and a shared About/Save footer. Battery states always appear as Left earbud, Right earbud and Case cards. Missing/expired values show a dash; scanning-off and unavailable states are explicit. Valid partial updates retain recent values from the same advertiser for up to 60 seconds, with per-component freshness.
