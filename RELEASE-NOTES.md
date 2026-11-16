# PodGlide 1.2

- Icon buttons for Preferences, Sound settings and About; vector dropdown chevrons.
- Case-sensitive PodGlide branding throughout the interface.
- Spring slide/scale entrance, floating artwork and fade-out dismissal, with a dedicated animation preference.
- About panel credits MD SOWROVE HOSSAIN and links to the supplied email and Facebook profile. Provider-logo buttons copy the bKash donation number or open the supplied PayPal.me page.
- Tighter icon crop fills the Windows-managed notification-area slot more clearly. Windows determines its physical size.
- Single-file offline Setup.exe with bundled runtime, per-user install, desktop/Start menu shortcuts, optional sign-in startup and Windows uninstaller.

Installer validation: installed into a temporary workspace folder, passed diagnostics using the packaged runtime, and successfully uninstalled. Existing user preference bytes were unchanged. The tested installer then upgraded this PC's installed app. The animation integration test sampled an intermediate offset, scale and opacity and confirmed the final settled state. No donations were initiated during tests.

- New PodGlide identity and AirPods Pro artwork in the executable, tray and windows.
- Charcoal gradient popup, mint active-output cards, clear connected/offline badge, and softer hover/focus states.
- Matching Preferences page with dark dropdowns, toggle switches, improved text contrast and spacing.
- Device cards keep keyboard focus during periodic refresh when the audio configuration has not changed.
- The separate animation toggle can be turned off for reduced motion; no Windows settings are changed.
- Portable distribution with bundled x64 .NET 8.0.32 runtime. Preferences and logs stay in its Data folder, and first-run startup and auto-switch are disabled.
- Installed-copy updates preserve existing audio choices, automatic switching and sign-in behavior.

Visual verification: connected and disconnected sample popups and the actual Preferences window were rendered and inspected. The connected popup is a sample; real diagnostics separately observed the connected AirPods Pro 3Gen and its default playback roles. No forced routing between distinct outputs was performed during this UI update.

Portable runtime diagnostics passed. Another clean Windows machine is still needed to verify redistribution end to end. The app is unsigned. Apple artwork is separately owned; see THIRD-PARTY-NOTICES.md.
