# Verification and limitations

Tested November 16, 2026 on Windows build 26300, x64, .NET 8.0.32.

## Completed here

- All application source compiled successfully against the installed .NET 8 runtime; launcher compiled successfully.
- Actual execution on .NET 8: case-insensitive name detection, saved endpoint ID precedence, renamed-device matching.
- COM notification subscription and unsubscription, active output enumeration, friendly-name property reading and all three default-role queries.
- Found one connected output: Realtek speakers.
- Invalid endpoint switching rejected before touching defaults.
- PolicyConfig activation and actual SetDefaultEndpoint calls for all three roles succeeded when re-selecting the existing speakers. Readback confirmed those roles.
- WPF popup rendered successfully to popup-preview.png; inspected for legibility and clipping.
- Tray controller stayed running during the launch check. Computer-use inspection did not expose the transient popup; full interactive tray/menu/settings tests remain unverified.
- No startup setting was enabled for the user during testing.

## Requires manual testing with hardware

- AirPods Pro connect/disconnect, repeated rapid reconnections, sleep/resume and event debounce. No AirPods playback endpoint was connected here.
- Audible routing between two distinct devices; only one output was available.
- Auto-switch on real connection; disabling/re-enabling startup and a Windows sign-in cycle.
- Settings persistence through normal UI interaction, duplicate-instance dialog, context-menu Exit and keyboard navigation.
- Multiple Bluetooth profiles and multiple AirPods devices, renamed endpoints, stale saved IDs and unplugged PC speakers.
- Secondary monitors, taskbar positions, very high display scaling, accessibility/high-contrast themes. Placement uses the primary display's WPF work area; it does not follow the mouse to another display.
- Standard .NET SDK publish/self-contained package and other Windows builds/ARM64. Included binary targets x64 only.

## Product boundaries

- Detection means an **active Windows playback endpoint**, not case opening, ear placement, Bluetooth pairing, or proof of an audible stream. Driver timing may delay endpoint activation; drivers that leave an endpoint active can prevent connection prompts.
- No battery percentage, Apple pairing animation/protocol, microphone switching, Bluetooth pairing/unpairing, or device volume manipulation.
- Name matching is a configurable heuristic and can match other AirPods models. Selecting an explicit endpoint is more precise.
- Default switching relies on an undocumented Windows interface that can break after an update. Errors may leave a partial role change; Windows Sound settings offers manual recovery.
- Per-app pinned outputs and exclusive streams may stay on their previous output.
- No installer, update service, code-signing certificate, or SmartScreen reputation. Windows may show an unknown-publisher prompt. Do not disable Windows security features.
- The supplied runtime-dependent launcher assumes the standard x64 .NET installation path under Program Files. For a custom runtime location, run the DLL with that dotnet host or build with the SDK.
