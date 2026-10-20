# Third-party notices

UI Motion Toolkit itself is MIT-licensed (see [LICENSE.md](LICENSE.md)). This file lists everything else that is shipped in this repository or required to use it.

## Shipped in this repository

### MedievalSharp (font)

- Files: `Samples~/BasicMenu/Fonts/MedievalSharp-Regular.ttf`, `Samples~/AdvancedShowcase/Fonts/MedievalSharp-Regular.ttf`
- Copyright (c) 2011, wmk69 (wmk69@o2.pl), with Reserved Font Name "MedievalSharp"
- License: SIL Open Font License, Version 1.1. Full text next to each font: `OFL-MedievalSharp.txt`
- Source: https://fonts.google.com/specimen/MedievalSharp
- Used unmodified, bundled with the samples. The OFL permits bundling and redistribution with software provided the license text stays with the font and the font is not sold on its own.

### Manufacturing Consent (font)

- File: `Samples~/AdvancedShowcase/Fonts/ManufacturingConsent-Regular.ttf`
- Copyright 2019 The Manufacturing Consent Project Authors (https://github.com/googlefonts/manufacturing-consent-font)
- License: SIL Open Font License, Version 1.1. Full text: `Samples~/AdvancedShowcase/Fonts/OFL-ManufacturingConsent.txt`
- Used unmodified, bundled with the Advanced sample.

All other files in this repository (code, USS, UXML, motion assets, GIFs, documentation) are original work covered by the MIT license.

## Required but not shipped

| Package | License | Notes |
| --- | --- | --- |
| [UI Toolkit Timeline](https://github.com/mihakrajnc/UITTimeline) (`dev.krajnc.uitoolkittimeline`) by Miha Krajnc | MIT | Provides the position / scale / rotation / opacity / class / display / visibility clips. Install it from its Git URL; it is not copied into this repository. |
| `com.unity.timeline` | Unity Companion / package license | Resolved by the Unity Package Manager. |

## Deliberately NOT included

An early plan was to illustrate the samples with the "Bloodlines - Dark UI" asset pack (xGaida and Shieldomirs). Its license forbids uploading its files to a public repository or including them in another asset pack or product (sections 3a to 3c), so **none of its textures, prefabs or scripts are in this repository**, and the animated previews in `Documentation~/Images` were rendered from the USS-only samples. Only the two fonts above are used: they are OFL-licensed (the OFL is independent of that pack's own license, as section 4 of the pack's license states), and they are redistributed here under the OFL with their license texts, unmodified.
