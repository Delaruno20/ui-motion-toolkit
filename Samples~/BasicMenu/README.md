# Basic Menu sample

A small menu showing the four ways to use UI Motion Toolkit.

**Run it:** open `BasicMenu.unity` and press Play.

| File | Role |
| --- | --- |
| `BasicMenu.uxml` / `BasicMenu.uss` | The UI and its **rest states** (pure USS, no textures) |
| `BasicMenuSample.cs` | When to play which motion (about 90 lines) |
| `Motions/` | The 8 generated Timeline motions and their library |
| `Editor/BasicMenuMotions.cs` | The code that authored the motions (`Tools > UI Motion Toolkit > Samples > Rebuild Basic Menu Motions`) |

Things to try: hover/focus the buttons (reversible `Toggle`), press them (`Play` pulse), press *Continue* (shake), *Options* (`Show`/`Hide`), *New Game* (`Hide` + callback + `Show`). Then open `Motions/Basic.Menu.Show` in the Timeline window and read the tracks: each track name is a selector.

Fonts: MedievalSharp, SIL OFL 1.1 (see `Fonts/OFL-MedievalSharp.txt`).
