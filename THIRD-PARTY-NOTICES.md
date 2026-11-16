# Third-party material

## Battery protocol reference

The BLE manufacturer-packet reader was informed by the MIT-licensed PodBattery project: https://github.com/hammad20120/PodBattery . Copyright (c) 2026 Hammad. The complete MIT license is provided as `PodBattery-LICENSE.txt` in the portable distribution and embedded in the executable as `ProGlide.Assets.PodBattery-LICENSE.txt`. See also `Assets/PodBattery-LICENSE.txt` in the source.

The compact release does not distribute a Microsoft .NET runtime. It uses Windows' built-in .NET Framework and Bluetooth APIs.

## Gmail and Facebook icons

The contact buttons use downloaded official icon artwork:

- Gmail: https://www.gstatic.com/images/branding/product/2x/gmail_2020q4_48dp.png
- Facebook: https://www.facebook.com/images/fb_icon_325x325.png

The respective companies retain their rights and trademarks. These assets identify the user's contact links, imply no affiliation or endorsement, and are excluded from the application code's MIT license.

## Donation provider logos

PayPal logo: https://www.paypalobjects.com/webstatic/icon/pp258.png

bKash logo mirror: https://raw.githubusercontent.com/mdmubarokhosin/Bd-MFS-Logo/refs/heads/main/logos/png/bkash.png

PayPal and bKash retain their respective logo and trademark rights. The logos identify the user's chosen donation methods; they do not imply endorsement and are excluded from the code's MIT license. No payment is processed within PodGlide. The bKash button copies a number, while the PayPal button opens the supplied PayPal.me link.

## AirPods product artwork

`Assets/AirPodsPro3.png` is Apple's original third-generation AirPods Pro product artwork, downloaded from:

https://www.apple.com/v/airpods-pro/t/images/overview/battery/case__b87ou7jna9de_large.png

Version 1.3 used this original Pro 3 movie (no longer used by 1.4):

https://www.apple.com/105/media/us/airpods-pro/2025/7acffb13-4adb-40b1-9393-8f1c99bc6c90/anim/case/small.mp4

See ARTWORK-PROVENANCE.md for source hashes and the crop/resize command. The case geometry is not redrawn or AI generated.

`Assets/PodGlide.ico` contains resized versions of that image. Apple retains rights to its artwork and trademarks; these assets are excluded from this repository's MIT license. Attribution does not grant a redistribution license. Anyone publishing or redistributing the Apple artwork must obtain any necessary rights or replace it with their own artwork. PodGlide is an independent project with no Apple affiliation or endorsement.

## Microsoft .NET runtime

Older 1.2 releases bundled a Microsoft .NET 8 runtime with its own notices. The compact 1.3 release distributes no runtime; it uses the .NET Framework and Windows APIs supplied with Windows 11.

Version 1.4 instead renders Apple's original Pro 3 USDZ model from a front-facing camera. See ARTWORK-PROVENANCE.md for the source, hash and distinction between the original model and app-authored motion.
