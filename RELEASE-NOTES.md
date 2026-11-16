# PodGlide 1.1

- New PodGlide identity and AirPods Pro artwork in the executable, tray and windows.
- Charcoal gradient popup, mint active-output cards, clear connected/offline badge, and softer hover/focus states.
- Matching Preferences page with dark dropdowns, toggle switches, improved text contrast and spacing.
- Device cards keep keyboard focus during periodic refresh when the audio configuration has not changed.
- Respects Windows' reduced-animation setting for the popup fade.
- Portable distribution with bundled x64 .NET 8.0.32 runtime. Preferences and logs stay in its Data folder, and first-run startup and auto-switch are disabled.
- Installed-copy updates preserve existing audio choices, automatic switching and sign-in behavior.

Visual verification: connected and disconnected sample popups and the actual Preferences window were rendered and inspected. The connected popup is a sample; real diagnostics separately observed the connected AirPods Pro 3Gen and its default playback roles. No forced routing between distinct outputs was performed during this UI update.

Portable runtime diagnostics passed. Another clean Windows machine is still needed to verify redistribution end to end. The app is unsigned. Apple artwork is separately owned; see THIRD-PARTY-NOTICES.md.
