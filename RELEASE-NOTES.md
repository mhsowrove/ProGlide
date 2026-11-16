# ProGlide 1.3 Compact — release notes

- Retargeted to Windows' built-in .NET Framework 4.8. No bundled .NET 8 runtime, external image folder, driver or third-party runtime is needed.
- The actual app is one executable with embedded artwork. The installer provides per-user shortcuts, optional sign-in startup and an uninstaller.
- Verified Apple AirPods Pro 3 imagery replaces the custom case drawing and older-generation icon. Opening plays original product-animation frames forward; closing reverses them. Source URLs, hashes and extraction details are in ARTWORK-PROVENANCE.md.
- This is Apple's public product animation, not its private iPhone pairing animation. Its closed view is top-down, as in the original footage.
- Connected is green; disconnected/not connected is red. The ProGlide name appears with the Pro 3 icon.
- Light and Dark preview through Preferences, with persistence on Save and restoration if closed unsaved. Official Gmail and Facebook icons identify contact links.
- Optional nearby battery monitoring uses Windows Bluetooth LE APIs. Levels are reported in 10% steps, unknown components show a dash, and readings expire after 60 seconds. Another nearby pair can be detected; ownership is unverified.
- A live scan observed a nearby AirPods Pro 3 broadcast: right 90%, left/case unavailable. This was not a simulated percentage.
- Embedded-artwork app diagnostics, both animation directions and light/dark rendering were verified. A clean second PC and fresh Windows account remain untested.

Existing user audio preferences are retained under the historical AirPodsSwitch settings location. Older large 1.2 installers are superseded by the compact package. Previous releases' build/test artifacts remain historical rather than evidence about this final build.
