# Verified AirPods Pro 3 artwork

Verified on November 17, 2026 from Apple's current AirPods Pro page:

https://www.apple.com/airpods-pro/

The page labels the product as AirPods Pro 3. Its charging-case video uses this original source:

https://www.apple.com/105/media/us/airpods-pro/2025/7acffb13-4adb-40b1-9393-8f1c99bc6c90/anim/case/small.mp4

Source SHA-256: `c8f67847b5d85fb529090c172db7202762f1489823440c5b78510f962d51e983`

The app embeds 49 frames from the first 2.04 seconds. They are cropped, resized and JPEG-compressed for the popup. They are not redrawn, AI generated, recolored or assembled from an older generation. The source sequence starts with a closed case viewed from above, then opens the lid and raises the earbuds. The app plays these frames forward on connection and in reverse on disconnection. This is Apple's public product animation, not Apple's private iPhone pairing animation.

Extraction command:

```text
ffmpeg -i Apple-Pro3-Case-original.mp4 -t 2.04 -vf "fps=24,crop=480:480:201:70,scale=192:192" -q:v 3 Pro3CaseFrame%03d.jpg
```

The app/tray icon and small product images use this separate original Pro 3 asset, also referenced on that page with AirPods Pro 3 alternative text:

https://www.apple.com/v/airpods-pro/t/images/overview/battery/case__b87ou7jna9de_large.png

Source SHA-256: `4c09848596c99626d9f818cc408f0a394a93a4543efb118b726f45478b2df318`

The earlier custom WPF case illustration and first-generation icon asset are no longer used. Apple retains all rights to its artwork. These images are excluded from the application code's MIT license. See THIRD-PARTY-NOTICES.md.
