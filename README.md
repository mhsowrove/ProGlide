# ProGlide

**Your sound, one tap away.**

A lightweight Windows 11 tray app for AirPods Pro, with quick audio switching, connection animations, and nearby battery monitoring.

## Features

- Switch playback between AirPods and PC speakers.
- Show a bottom-right popup when AirPods audio connects or disconnects.
- Transparent, front-facing AirPods Pro 3 opening and closing animations.
- Green connected and red disconnected indicators.
- Light and dark themes.
- Separate Left, Right, and Case battery cards.
- Optional automatic switching and launch at sign-in.
- Compact executable with embedded artwork.

## Requirements

- Windows 11, x64.
- Bluetooth connectivity for AirPods.
- Bluetooth Low Energy support for battery monitoring.

ProGlide uses the **.NET Framework 4.8 included with Windows 11**. No separate .NET 8 runtime, driver, or background service is required.

## Installation

Download the installer from this repository’s **Releases** section once a release is published, then run it.

For portable use, extract the portable ZIP and launch `ProGlide.exe`. Keep `portable.flag` beside the executable to store preferences in the local `Data` folder.

You can also run the single `ProGlide.exe` directly.

## Setup

1. Connect your AirPods through Windows Bluetooth settings.
2. Click the ProGlide icon in the system tray.
3. Open **Preferences**.
4. Select your AirPods output and PC speakers.
5. Choose your theme and optional settings, then save.

If a device is missing, connect it and reopen Preferences.

## Battery monitoring

The battery cards display nearby AirPods broadcasts when available.

- Levels arrive in **10% steps**.
- **—** means no recent reading for that component, not zero battery.
- Recent component readings expire after 60 seconds.
- Open the case near your PC to encourage fresh readings.
- Battery scanning can be disabled in Preferences.

**Device ownership is unverified.** Another nearby pair may be detected. Firmware and Bluetooth adapter support can affect availability. ProGlide does not save Bluetooth addresses or upload readings.

## Animation and artwork

The animation is rendered from Apple’s original **AirPods Pro 3 3D model**, with a front-facing camera and transparent background.

The camera, lighting, and opening/closing motion are authored for ProGlide. It is **not Apple’s official iPhone pairing animation**.

See [ARTWORK-PROVENANCE.md](ARTWORK-PROVENANCE.md) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for sources and attribution.

## Build from source

On Windows 11, open PowerShell in the source folder and run:

```powershell
.\build.ps1
```

This produces `ProGlide.exe` using the Windows C# compiler. No NuGet packages are used.

To build the installer, install Inno Setup and run:

```powershell
.\BuildSupport\build-installer.ps1 -CompilerPath "C:\Path\To\Inno Setup 6\ISCC.exe"
```

## Limitations

- Connection detection follows active Windows audio endpoints, not physical case-lid movement or ear placement.
- Default-device switching uses an undocumented Windows interface and may be affected by Windows updates.
- Apps with pinned audio outputs or exclusive playback may ignore default-device changes.
- Battery readings may be incomplete or unavailable.
- The app and installer are unsigned.
- Local testing passed; a clean second PC was not available for verification.

See [LIMITATIONS.md](LIMITATIONS.md) for details.

## Developer

**MD SOWROVE HOSSAIN**

- [Email](mailto:mhsowrove@gmail.com)
- [Facebook](https://www.facebook.com/mhsowrove)

### Support development

- **bKash:** `01925070109`
- [PayPal](https://www.paypal.com/paypalme/mhsowrove)

Donations are optional. ProGlide does not process payments inside the app.

## License

Original application code is licensed under the [MIT License](LICENSE). Third-party artwork and other materials retain their respective ownership and license terms.

ProGlide is an independent project and is not affiliated with Apple, Google, Meta, bKash, or PayPal.
