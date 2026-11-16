# Limitations and verification — ProGlide 1.4 Compact

Verified locally on Windows 11 x64. The app targets the .NET Framework 4.8 already supplied with Windows; no .NET 8 runtime is distributed. The single executable embeds its artwork. A separate clean PC was not available.

## Hardware and behavior

- Audio output enumeration and default-role queries work on this PC. The user confirmed the earlier switching app worked with their AirPods. The compact build passes read-only audio diagnostics; no forced routing between different devices was done during this visual update.
- Connection/disconnection is detected through active playback endpoints, not actual physical case lid or ear placement. Bluetooth driver timing can delay events.
- Default playback changes use undocumented Windows IPolicyConfig for console, multimedia and communications roles. Partial role changes are possible on failure; per-app pinned outputs and exclusive streams can ignore default switching.
- The popup uses the primary display's work area. Secondary displays, unusually high DPI and high-contrast modes require further manual testing.
- Original Pro 3 geometry was imported from Apple's USDZ model and rendered from a front-facing camera with true transparency. Lighting and hinge motion are app-authored. This is not an untouched Apple animation. No physical lid telemetry is read.
- The animation can be turned off. Theme changes preview instantly and only persist on Save.

## Battery

- A passive Bluetooth LE scan received a real nearby Pro 3 packet with right 90%; left and case were absent. See BATTERY-TEST.txt.
- Reports are in 10% steps. A dash is unknown, not zero. No exact single-percent accuracy is claimed.
- Broadcast identity is unverified. Another nearby AirPods pair can be selected. No Bluetooth addresses are stored.
- Firmware and adapter support vary, some components may stop broadcasting while closed, and cached readings expire after 60 seconds. Open the case close to the PC to encourage updates. Scanning is optional.
- Malformed/unknown/weak packets are rejected. No wireless setting is changed, no pairing is performed and nothing is uploaded.

## Packaging and ownership

The app and installer are unsigned. It relies on normal Windows 11 components, so it is not independent of the operating system. Stripped-down Windows installations or missing Bluetooth components need separate validation. Original code is MIT licensed; Apple, Google, Meta, bKash and PayPal artwork has separate ownership notices. Donations are opened externally or copied, never processed automatically.

Settings and logs remain under the current user's AirPodsSwitch folder for compatibility, or under Data beside a portable copy. Logs are not size-capped. The shareable packages contain no personal settings or logs.
