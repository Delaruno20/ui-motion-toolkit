# Dark Fantasy Showcase sample

A game-style screen: title, menu, HUD bars, boss card, tabbed journal, popup and toasts, all animated by 24 motions.

**Run it:** open `AdvancedShowcase.unity` and press Play. Click the stage during the intro to skip it. The bottom bar triggers each effect.

| File | Role |
| --- | --- |
| `AdvancedShowcase.uxml` / `.uss` | The UI and its rest states (pure USS, no textures) |
| `AdvancedShowcase.cs` | Gameplay-style wiring: bars, damage trail, tabs, toasts, enrage state (about 250 lines) |
| `Motions/` | The 24 generated motions and their library |
| `Editor/AdvancedShowcaseMotions.cs` | The authoring code, commented block by block (`Tools > UI Motion Toolkit > Samples > Rebuild Advanced Showcase Motions`) |

Start with `Motions/Showcase.Intro` (26 tracks, one timeline for the whole screen), then `Showcase.Boss.Enrage` (reversible state), `Showcase.Bar.Hit` (one motion reused on every bar) and `Showcase.Page.*` (direction-aware transitions).

Fonts: MedievalSharp and Manufacturing Consent, SIL OFL 1.1 (see the `OFL-*.txt` files in `Fonts/`).
