# UI Motion Toolkit: User Guide

This is the complete guide. If you only want to see it work, read the [README](../README.md) quick start and import the *Basic Menu* sample first.

**Contents**

1. [Mental model](#1-mental-model)
2. [Setup](#2-setup)
3. [Core concepts](#3-core-concepts)
4. [Authoring motions in the Timeline window](#4-authoring-motions-in-the-timeline-window)
5. [Authoring motions from code](#5-authoring-motions-from-code)
6. [Playing motions](#6-playing-motions)
7. [The rest-state contract](#7-the-rest-state-contract)
8. [Selector reference](#8-selector-reference)
9. [Recipes](#9-recipes)
10. [Previewing and capturing](#10-previewing-and-capturing)
11. [Lifecycle, performance and platform notes](#11-lifecycle-performance-and-platform-notes)
12. [Troubleshooting](#12-troubleshooting)
13. [API cheat sheet](#13-api-cheat-sheet)

---

## 1. Mental model

Three things, in order:

1. **A motion** is a Timeline asset. It says *"these selectors animate like this over time"*. It knows nothing about your game.
2. **A scope** is the `VisualElement` you hand to the player. The motion's selectors resolve **inside** that element (and the first part of a selector may match the scope itself).
3. **A playback** is a motion running on a scope. You start it with an id: `UIMotion.Play(scope, "Menu.Show")`.

```
UIMotion.Play( scope: menu , motion: "Menu.Show" )
                    │                  │
                    │                  └─ asset name, looked up in the UIMotionLibrary
                    └──────────────── selectors of the tracks (".menu-item", "#title"...) are resolved inside this element
```

Because the scope is chosen at play time, **the same asset animates every instance**: a "card" motion with a track named `:scope` plays on any card you pass.

While a motion runs it writes **inline styles** (translate, scale, rotate, opacity, colors, classes). When it ends those inline styles are removed and your USS applies again. Your USS describes how things *rest*; motions describe how they *travel*.

---

## 2. Setup

### 2.1 Requirements

| | |
| --- | --- |
| Unity | 6000.0 or newer declared; developed and tested on **6000.6.0f1** only |
| Packages | `com.unity.timeline` (automatic) and `dev.krajnc.uitoolkittimeline` (manual, see below) |
| UI | UI Toolkit runtime: `UIDocument`, `PanelRenderer` (6000.6+), or any `VisualElement` tree attached to a panel |

### 2.2 Install

1. Install **UI Toolkit Timeline** from its Git URL (Unity cannot resolve Git dependencies declared by another package):
   `https://github.com/mihakrajnc/UITTimeline.git`
2. Install **UI Motion Toolkit** from its Git URL: `https://github.com/Delaruno20/ui-motion-toolkit.git`
3. If you want to run the tests, add the package to `testables` in `Packages/manifest.json`.

To work on a local copy instead, use a `file:` dependency: `"com.delaruno20.ui-motion-toolkit": "file:../path/to/ui-motion-toolkit"`.

### 2.3 The motion library

`UIMotion` finds motions by **id** through a `UIMotionLibrary` asset.

- **Default location:** `Resources/UI/UIMotionLibrary` (any `Resources` folder in your project). `UIMotion.Library` loads it on first use.
- **Create + fill it:** menu `Tools > UI Motion Toolkit > Rebuild Default Motion Library` creates `Assets/Resources/UI/UIMotionLibrary.asset` and adds **every** timeline in `Assets/` that contains a UI motion track.
- **Custom library:** `Create > UI Motion Toolkit > Motion Library`, fill the list by hand or with `UIMotionLibraryBuilder.Rebuild(path, folders...)`, then assign it from code: `UIMotion.Library = myLibrary;` (this is what the samples do).
- **Ids are asset names.** `Menu.Show` is the timeline asset named `Menu.Show`. Names must be unique; duplicates log a warning at rebuild time.
- Why a library: it keeps the motion assets in builds without scene wiring, and game code stays free of asset references.

> A missing id is not an exception. The state changes **instantly** and a warning is logged once per id.

### 2.4 UIDocument and PanelRenderer

UI Motion Toolkit never talks to `UIDocument` or `PanelRenderer`: every call takes a **`VisualElement`**. Only the way you obtain that element differs.

**UIDocument**: read `rootVisualElement` in `Start()` (not `Awake`/`OnEnable`, the tree may not exist yet).

```csharp
void Start() => Bind(GetComponent<UIDocument>().rootVisualElement);
```

**PanelRenderer** (Unity 6000.6+): the component builds the tree itself and may **rebuild it** (asset reimport, settings change), so register for its reload callback and bind **every time** it gives you a root. Use the 3-argument version (the 2-argument one is obsolete):

```csharp
PanelRenderer _renderer;

void OnEnable()
{
    _renderer = GetComponent<PanelRenderer>();
    _renderer.RegisterUIReloadCallback(OnUIReload);
}

void OnDisable() => _renderer.UnregisterUIReloadCallback(OnUIReload);

void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
{
    UIMotion.Show(root.Q("menu"), "Menu.Show");
    UIMotionTriggers.BindAll(root, ".menu-item", "Item.Focus", "Item.Press");
}
```

Notes:

- A playback bound to the old tree is dropped automatically when its scope leaves the panel; the new tree starts clean.
- Event handlers (`clicked +=`, `RegisterCallback`) must be registered inside the reload callback, on the new elements.
- Guard PanelRenderer code in scripts that must compile on older Unity versions with `#if UNITY_6000_6_OR_NEWER`.
- The *Basic Menu* sample contains both scenes: `BasicMenu.unity` (UIDocument) and `BasicMenu_PanelRenderer.unity` (PanelRenderer). The same `BasicMenuSample` script serves both.
- The Timeline-window preview (`UIMotionPreviewScope`, *Create Preview Director*) also finds a `PanelRenderer` on the object or its parents (Unity 6000.6+).
- Support was written for Unity's PanelRenderer API but is **community-validated only**: please report issues.

---

## 3. Core concepts

| Concept | What it is | Where |
| --- | --- | --- |
| **Motion** | A `TimelineAsset` with one or more `UIMotionTrack`s | Project asset |
| **Track** | Animates the elements matched by its **name (a selector)** | `UIMotionTrack` |
| **Clip** | A time range on a track: position, scale, rotation, opacity, color, shake, class, display or visibility | Timeline clips |
| **Selector** | USS-like expression resolved inside the scope | `UIMotionSelector` |
| **Scope** | The element you pass to `UIMotion.*` | your code |
| **Library** | Maps motion ids to assets | `UIMotionLibrary` |
| **Playback** | A motion running on a scope: time, direction, speed, loop | `UIMotionPlayback` |
| **Channel** | A named lane on a scope; a new playback in a busy channel replaces the old one | string |
| **Stagger** | Per-track delay between matched elements | `UIMotionTrack.Stagger` |
| **Neutral** | The "no animation" value a clip blends toward (offset 0, scale 1, opacity 1, or a color) | see 4.4 |

### Channels

Each playback lives in `(scope, channel)`. The channel defaults to the **motion id**, so playing the same motion again on the same scope restarts it, while different motions on the same element run side by side.

```csharp
UIMotion.Play(button, "Button.Press");                        // channel "Button.Press"
UIMotion.Toggle(button, "Button.Focus", true);                // channel "Button.Focus"
UIMotion.Play(bar, "Bar.Critical", channel: "critical", loop: true);  // explicit channel: stop it by name later
UIMotion.Stop(bar, "critical");
```

When two playbacks drive the **same property of the same element** at once, the last one written wins. Keep competing motions in the **same channel** so the newest replaces the oldest.

---

## 4. Authoring motions in the Timeline window

### 4.1 Create a motion

1. `Assets > Create > Timeline`, name it with the **id** you will use in code, e.g. `Menu.Show`.
2. Open it in `Window > Sequencing > Timeline`. A timeline asset does not need a `PlayableDirector` to be authored or played by this package.
3. In the track area, right-click and add a **UIMotionTrack** (listed by its type name in the Add Track menu). The *UI Toolkit* track of the Timeline package is also accepted by the player, but it lacks stagger, color and shake.
4. **Rename the track to the selector.** This is the key idea. `.menu-item`, `#title`, `:scope`, `#panel .row`.
   - Text after `|` is a free label: `#panel | slide` selects `#panel`.
   - Timeline adds ` (1)` to duplicate names; it is ignored.
5. Add clips by right-clicking the track row. The clip types are listed in 4.3.

Keep **one property per track**: an opacity clip and a position clip on the same selector go on two tracks named `.menu-item | fade` and `.menu-item | move`. Do not overlap clips of the same track: Timeline would turn the overlap into a blend.

### 4.2 Track settings (Inspector)

| Field | Meaning |
| --- | --- |
| **Stagger** | Seconds between consecutive matched elements. Element *i* plays the track *i x stagger* later. The motion's duration grows by `stagger x (count - 1)`. |
| **Stagger Reverse** | Last matched element starts first. |
| **Automatic Usage Hints** | Adds `UsageHints.DynamicTransform` / `DynamicColor` to the elements the track drives, which UI Toolkit wants for frequently changing transforms and colors. Leave on. |

### 4.3 Clip reference

All numeric clips are **offsets from the neutral state**, multiplied by the clip **weight**. The weight comes from the clip's **ease in** and **ease out** curves (the blend curves in the Inspector). Outside its clips, a track **holds** the nearest edge value for position, scale, rotation, opacity and color. Class, display, visibility and shake only act inside their clip.

| Clip | Value | Neutral | Notes |
| --- | --- | --- | --- |
| `UITPositionClip` | `Position` px (x, y) | (0, 0) | Written to `style.translate`. |
| `UITScaleClip` | `Scale` (x, y) as offset | (1, 1) | Value is **added** to 1: to scale to 0 set `-1`. Written to `style.scale`. |
| `UITRotationClip` | `Rotation` degrees | 0 | Written to `style.rotate`. |
| `UITOpacityClip` | `Opacity` as offset | 1 | Value is added to 1: fully transparent is `-1`. |
| `UIMotionColorClip` | `Color`, `Target` (BackgroundColor, ImageTint, TextColor, BorderColor), optional `Neutral` | USS color at bind time, or `Neutral` when *Override Neutral* is on | Blends toward `Color` by the weight. |
| `UIMotionShakeClip` | `Amplitude` (px), `RotationAmplitude` (deg), `Frequency` (Hz), `Seed` | none | Smooth deterministic noise added to position (and rotation), scaled by the weight. Fade it with ease out. |
| `UITClassClip` | `ClassName` | none | Adds the class inside the clip, removes it after. |
| `UITDisplayClip` | `Display` | none | Forces display on/off inside the clip. |
| `UITVisibilityClip` | `Visible` | none | Forces visibility inside the clip. |

**Reading the math:** *"FadeIn from 0"* is an opacity clip with value `-1` whose **ease out** runs over the whole clip (weight 1 to 0): at the start opacity = 1 + (-1 x 1) = 0, at the end = 1 + (-1 x 0) = 1. That is exactly what the code helpers `Enter` / `Exit` / `Pulse` generate (section 5).

### 4.4 Colors and the neutral color

A color clip blends from the **neutral** color toward its target by the weight. Neutral is, in order of preference:

1. the clip's **Neutral** when *Override Neutral* is checked (recommended for state motions: you state the exact USS color of the "off" look);
2. otherwise the element's resolved USS color **when the playback was bound**.

Use the override for any reversible state motion; otherwise the "off" color depends on what the element looked like at the moment the playback started.

### 4.5 Easing

Ease in/out curves are normal Timeline blend curves. **Overshooting curves work** (the weight is not clamped), so a back ease gives a springy entrance. The code authoring API offers presets (`MotionEase`: Linear, InSine, OutSine, InOutSine, InCubic, OutCubic, InOutCubic, OutQuint, OutExpo, InExpo, OutBack, InBack).

---

## 5. Authoring motions from code

Editor-only (`UIMotionToolkit.Editor`). Useful when many motions share a style, when you want diff-able generation, or to retune numbers in one place. The output is **plain Timeline assets**: still editable in the Timeline window. Re-running a builder rewrites the asset **in place** (its GUID is kept, references survive).

```csharp
MotionAsset m = MotionAsset.Begin("Assets/Motions/Popup.Show.asset");   // creates or empties the asset
m.Track(".popup__backdrop").FadeIn(0, 0.30);
m.Track(".popup__window")
    .FadeIn(0.05, 0.20)
    .Pop(0.05, 0.45, from: 0.80f)                                         // springy scale
    .MoveFrom(new Vector2(0, 30), 0.05, 0.45, MotionEase.OutCubic);
m.Track(".popup__line", stagger: 0.08f)                                   // cascade
    .FadeIn(0.30, 0.30)
    .MoveFrom(new Vector2(0, 10), 0.30, 0.35, MotionEase.OutCubic);
m.Save();
```

### 5.1 Naming of the helpers

- `*From` / `FadeIn` / `Pop`: **enter**. Starts displaced, ends at the neutral (USS) state. Before `at` the track holds the displaced value.
- `*To` / `FadeOut`: **exit**. Starts at neutral, ends displaced, and holds it afterwards.
- `*Pulse`: goes to the value and comes back. `inShare` is the fraction of the time spent going.
- `Hold<T>`: constant value over a range.

| Group | Helpers |
| --- | --- |
| Opacity | `FadeIn(at, dur, ease, from)`, `FadeOut(at, dur, ease, to)`, `FadePulse(at, dur, to, ease, inShare)` |
| Position (px) | `MoveFrom(offset, ...)`, `MoveTo(offset, ...)`, `MovePulse(offset, ...)` |
| Scale (absolute factor) | `ScaleFrom(float or Vector2, ...)`, `ScaleTo(...)`, `ScalePulse(...)`, `Pop(at, dur, from)` |
| Rotation (deg) | `RotateFrom`, `RotateTo`, `RotatePulse` |
| Color | `ColorFrom(target, color, ..., neutral)`, `ColorTo(...)`, `ColorPulse(..., inShare, neutral)` |
| Shake | `Shake(at, dur, amplitude, rotation, frequency, seed)` fades out over the clip |
| Discrete | `Class(name, at, dur)`, `Display(bool, at, dur)`, `Visible(bool, at, dur)` |
| Low level | `Enter<T>`, `Exit<T>`, `Pulse<T>`, `Hold<T>` with your own clip setup |

`MotionAsset.Track(selector, stagger, staggerReverse, label)` returns a builder; chained calls on the same selector create **one track per property** (named `selector | fade`, `selector | move`...). Pass a `label` to create a second track of the same property when clips must overlap.

### 5.2 Building a library

```csharp
UIMotionLibraryBuilder.Rebuild("Assets/Resources/UI/UIMotionLibrary.asset", "Assets/Motions");
// or everything: UIMotionLibraryBuilder.RebuildDefault();
```

---

## 6. Playing motions

All entry points live on `UIMotion` (namespace `UIMotionToolkit`).

### 6.1 One-shot: `Play`

```csharp
UIMotionPlayback Play(VisualElement scope, string motionId, Action onFinished = null,
                      string channel = null, bool loop = false);
UIMotionPlayback Play(VisualElement scope, TimelineAsset motion, ...);   // by asset, no library needed
```

Use for hits, pulses, errors, intros, ambient loops. Returns `null` (and calls `onFinished` immediately) when the scope or the motion is missing. A `Stop`ped playback does **not** call `onFinished`; `Complete` does.

### 6.2 Reversible state: `Toggle`

```csharp
UIMotionPlayback Toggle(VisualElement scope, string motionId, bool on, Action onFinished = null,
                        string channel = null, bool instant = false);
```

For states that come and go: focus, hover, selected, enraged. `on = true` plays forward, `false` plays **backward from the current time**; quick flicker never pops. One playback is kept per element and channel. `instant: true` jumps to the end state without animating. If the element is already at rest in the asked state, nothing happens.

**Contract:** the motion's **end** is the *on* look and its **start** the *off* look; your USS must match them at rest. See [section 7](#7-the-rest-state-contract).

### 6.3 Presence: `Show` / `Hide`

```csharp
UIMotionPlayback Show(VisualElement element, string showMotionId, Action onShown = null,
                      string hiddenClass = "is-hidden", bool instant = false);
UIMotionPlayback Hide(VisualElement element, string showMotionId, string hideMotionId = null,
                      Action onHidden = null, string hiddenClass = "is-hidden", bool instant = false);
bool IsShown(VisualElement element, string hiddenClass = "is-hidden");   // visible and not on its way out
bool IsHiding(VisualElement element);
```

- `Show` removes the hidden class **first**, then plays the motion (so the element exists on screen at its start pose in the same frame).
- `Hide` plays `hideMotionId`, or **`showMotionId` in reverse** when no hide motion is given, then adds the hidden class and calls `onHidden`.
- A `Show` during a `Hide` resumes the same motion forward from where it was.
- You define `.is-hidden { display: none; }` in your USS.

### 6.4 Control

| Call | Effect |
| --- | --- |
| `Stop(scope, channel = null, restore = true)` | Stops a channel, or every playback of the scope. No callback. `restore` clears inline styles. |
| `Complete(scope, channel = null)` | Jumps to the end of the current direction and finishes (callbacks run). Use for "skip". |
| `StopUnder(root)` | Stops every playback whose scope is `root` or inside it (call before removing a subtree). |
| `StopAll()` | Everything. |
| `IsPlaying(scope, channel = null)`, `Get(scope, channel)` | Query. |
| `Has(id)`, `Find(id)` | Query the library. |
| `Tick(deltaTime)` | Advance by hand; used with `ManualTick = true` (tests, offscreen rendering). |

### 6.5 `UIMotionPlayback`

Returned by `Play` / `Toggle` / `Show` / `Hide`:

| Member | Meaning |
| --- | --- |
| `Duration` | Timeline duration plus the longest stagger delay |
| `Time`, `Progress` | Playhead in seconds (settable: scrub) and 0..1 |
| `IsPlaying`, `Reversed`, `Loop`, `Speed` | State and controls (`Speed` 1 = authored speed) |
| `RestoreOnFinish` | Clear inline styles at the end (default true; set false to keep the last frame) |
| `Finished` | Event raised when it reaches its end (not on `Stop`) |
| `PlayForward()`, `PlayReverse()`, `Complete()`, `Stop(restore)` | Control |
| `Bind()` | Re-resolve selectors (elements added to the scope since it started) |
| `Evaluate()`, `Restore()` | Write the current frame / clear inline styles |

### 6.6 Triggers: wire events in one call

`UIMotionTriggers` removes the boilerplate for interaction feedback:

```csharp
UIMotionTriggers.ToggleOnFocus(element, "Item.Focus");     // FocusIn forward, FocusOut backward
UIMotionTriggers.ToggleOnHover(element, "Item.Hover");     // PointerEnter / Leave
UIMotionTriggers.PlayOnPress(element, "Item.Press");       // pointer down + navigation submit (Enter / gamepad A)
UIMotionTriggers.BindAll(root, ".menu-item", "Item.Focus", "Item.Press");   // focus + press on every match
```

Binding the same motion twice on one element is ignored. In UI Toolkit a mouse hover does not focus a button; the samples add `RegisterCallback<PointerEnterEvent>(_ => element.Focus())` so hover and keyboard/gamepad focus share one look.

### 6.7 Chaining

There is no sequencer object: chain with callbacks.

```csharp
UIMotion.Play(toast, "Toast.Show", onFinished: () =>
    UIMotion.Play(toast, "Toast.Life", onFinished: () =>
        UIMotion.Play(toast, "Toast.Hide", onFinished: () => toast.RemoveFromHierarchy())));
```

For choreography inside one screen, prefer **one motion with several tracks** (see the Advanced sample's `Showcase.Intro`): it is scrubbable, retunable and cheaper than callbacks.

### 6.8 Time

Playbacks advance on **unscaled time** (`Time.unscaledDeltaTime`), each frame's delta clamped to 0.1 s. A hidden runner object (`[UIMotion]`, `DontDestroyOnLoad`, hidden in the hierarchy) ticks them in play mode; in edit mode an `EditorApplication.update` hook does. Set `UIMotion.ManualTick = true` to drive them yourself.

---

## 7. The rest-state contract

This is the one rule to internalize.

> **USS describes how an element rests. A motion animates to or from that rest state. When a playback ends, its inline styles are removed and the USS shows through.**

Consequences:

1. **Enter motions end at the USS state.** `FadeIn` ends at opacity 1, `MoveFrom` ends at translate 0, so the USS state must be "opacity 1, translate 0" (or you must not touch that property in USS, which is the normal case).
2. **State motions (`Toggle`) must match a USS selector at both ends.** Example for a focus effect:

   ```css
   .menu-button {                                   /* OFF look = start of the motion */
       background-color: rgb(30, 25, 32);
       border-color: rgb(70, 48, 54);
   }
   .menu-button:focus {                             /* ON look = end of the motion */
       translate: 10px 0;
       background-color: rgb(74, 24, 32);
       border-color: rgb(214, 78, 84);
   }
   ```
   ```csharp
   m.Track(":scope")
       .MoveTo(new Vector2(10, 0), 0, 0.16, MotionEase.OutCubic)                                           // ends at translate 10px
       .ColorTo(UIMotionColorTarget.BackgroundColor, new Color(74/255f, 24/255f, 32/255f), 0, 0.16,
                MotionEase.OutCubic, neutral: new Color(30/255f, 25/255f, 32/255f))                       // neutral = OFF color
       .ColorTo(UIMotionColorTarget.BorderColor, new Color(214/255f, 78/255f, 84/255f), 0, 0.16,
                MotionEase.OutCubic, neutral: new Color(70/255f, 48/255f, 54/255f));
   ```
   If the end of the motion and the USS `:focus` disagree you get a pop at the last frame.
3. **Transform properties are absolute while running.** `translate`, `scale` and `rotate` are written as *the sum of all clip offsets from (0, 0) / (1, 1) / 0*, replacing the USS value for the duration. If your USS rests at `scale: 0 0` (a marker that appears on focus), the state motion's `ScaleFrom(0)` goes from 0 to 1 and the USS `:focus` rule says `scale: 1 1`. Do not mix an unrelated USS `rotate` with a rotation motion on the same element.
4. **Do not use USS `transition` on properties a motion drives** (transform, opacity, the animated colors). The two fight. Use motions for movement; keep USS transitions for anything else.
5. For something that must stay after the motion (a new state), change the **USS class**, not the inline style. The sample's enrage toggle sets `.is-enraged` first, then calls `Toggle`.

---

## 8. Selector reference

Written as the **track name**; resolved inside the scope.

| Syntax | Meaning |
| --- | --- |
| `#name` | element with that `name` |
| `.class` | element with that USS class |
| `#name.a.b` | compound: all parts must match |
| `*` | any element |
| `a b` | descendant |
| `a > b` | direct child |
| `a, b` | union (result order: the alternatives in order, each in tree order) |
| `:scope` or `&` | the scope element itself (also as first part: `:scope > .item`) |
| `text \| label` | everything after `\|` is a free label |

Notes:

- The **first part also matches the scope itself**, so a component motion can name its own block class (`.card`) instead of `:scope`.
- Selectors are resolved **once**, when the playback binds (at `Play`). Elements added later need `playback.Bind()` or a new play.
- Matches are returned in **tree order**; that order feeds `Stagger`.
- No attribute selectors, no pseudo-classes other than `:scope`, no sibling combinators.

---

## 9. Recipes

All of these exist working in the samples.

### 9.1 Screen entrance + exit

```csharp
UIMotion.Show(screen, "Screen.Show");
UIMotion.Hide(screen, "Screen.Show", "Screen.Hide", onHidden: () => { /* unload, navigate... */ });
```
`Screen.Show`: `:scope` fade + move, `.row` tracks with `stagger`. `Screen.Hide`: the same with `staggerReverse` and `*To` helpers.

### 9.2 Button feedback

```csharp
UIMotionTriggers.BindAll(root, ".button", "Button.Focus", "Button.Press");
foreach (var b in root.Query(className: "button").ToList())
    b.RegisterCallback<PointerEnterEvent>(_ => b.Focus());
```

### 9.3 Tabs with direction

Keep each page `is-hidden` except the current one. On tab change, play an *out* motion on the old page and an *in* motion on the new one, choosing the variant by the index delta (`InRight` / `OutLeft` going forward, mirrored going back). Toggle the tab itself with a `Tab.Select` state motion (underline grows, text warms).

### 9.4 Popup

`Show(popup, "Popup.Show")` / `Hide(popup, "Popup.Show", "Popup.Hide")`. Track `.popup__backdrop` fades, `.popup__window` pops, `.popup__line` staggers. Trap focus yourself if needed.

### 9.5 Reusing one motion on many instances

```csharp
UIMotion.Play(playerBar, "Bar.Hit");
UIMotion.Play(bossBar,   "Bar.Hit");        // same asset, different scope, independent playbacks
```
Tracks target `:scope` and `.bar__fill`: relative selectors, so any bar works.

### 9.6 Runtime-built elements (toasts, list rows)

Build the element, `Add` it to its parent, then `Play(element, ...)` immediately: the playback binds to the children that already exist. A `Stagger` track on `.row` cascades the rows of a list you just filled.

### 9.7 Loops

```csharp
UIMotion.Play(stage, "Ambient", channel: "ambient", loop: true);
UIMotion.Stop(stage, "ambient");
```
Build loopable motions from **pulse** clips (neutral to value to neutral) so the loop point is invisible.

### 9.8 Skippable intros

```csharp
UIMotion.Play(stage, "Intro", onFinished: StartAmbient);
stage.RegisterCallback<PointerDownEvent>(_ => UIMotion.Complete(stage, "Intro"));   // jump to the end, callbacks still run
```

### 9.9 Layered effects in one timeline

Hits and impacts combine several selectors in one asset: a `#stage` shake, a `#flash` overlay color pulse and a `#player-card` shake, started with a single `Play(screen, "Screen.Hurt")`.

### 9.10 Pause menus

Nothing to do: playbacks use unscaled time, so a game with `Time.timeScale = 0` still animates its UI.

### 9.11 Reduce motion

Pass `instant: true` to `Show` / `Hide` / `Toggle` when your "reduce motion" setting is on; the state changes with no animation. The simplest way is a small wrapper in your own code that forwards the flag.

---

## 10. Previewing and capturing

### 10.1 In the Timeline window

1. Select the GameObject holding your `UIDocument`.
2. `GameObject > UI Toolkit > UI Motion Preview Director` (or `Tools > UI Motion Toolkit > Create Preview Director`). This adds a child with a `PlayableDirector` and a `UIMotionPreviewScope`, pre-assigned to the timeline selected in the Project window.
3. Open the Timeline window, select that child, scrub or press play. The first motion track's mixer evaluates the **whole** motion with the runtime evaluator, so what you see is what plays.
4. For component motions (tracks named `:scope`), set **Scope Selector** on `UIMotionPreviewScope` (for example `#new-game`): the first matching element becomes the scope.

The preview needs the UI panel to be alive, so it works while the scene is running (or when the editor has built the document's visual tree).

### 10.2 Offscreen frame capture

`UIMotionPreviewRenderer.Render` captures PNG frames of a motion at chosen times, without opening any window. This is how the GIFs of this repository were produced.

```csharp
UIMotionPreviewRenderer.Render(new MotionPreviewRequest
{
    Uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Menu.uxml"),
    Panel = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/Panel.asset"),
    Motion = AssetDatabase.LoadAssetAtPath<TimelineAsset>("Assets/Motions/Menu.Show.asset"),
    Scope = root => root.Q("menu"),
    Times = new[] { 0.0, 0.2, 0.5, 1.0 },
    Size = new Vector2Int(1280, 720),
    Output = "shots/menu_{t}.png"
}, ok => Debug.Log("done " + ok));
```

Run it from a menu item, or in batch mode **with graphics** (omit `-nographics`) via `-executeMethod`. It relies on reflection into UI Toolkit internals: treat it as a convenience that may need a fix on a new Unity version.

---

## 11. Lifecycle, performance and platform notes

- **Binding:** selectors resolve when a playback starts. Cost grows with the size of the scope subtree and the number of tracks; avoid `*` over huge trees and prefer scoping to the screen or component you animate.
- **Evaluation:** per frame, per playing playback: tracks add into per-element state, then each element is written once. A screen and a few widgets animating at a time is the intended load. No benchmarks are published; profile on your target.
- **Compiled cache:** in builds a motion is compiled once and cached; in the editor it is recompiled on every play so Timeline edits apply immediately.
- **Panel removal:** a playback whose scope leaves its panel is dropped at the next tick. Call `UIMotion.StopUnder(root)` before discarding a subtree to be explicit.
- **Domain reload:** statics reset in `SubsystemRegistration`, so Enter Play Mode with domain reload disabled works.
- **Threading:** main thread only, like UI Toolkit.
- **Platforms:** nothing platform specific; the runtime assembly does not use reflection (the preview renderer is editor only). Only validated in the Unity 6000.6 editor.
- **Panels:** the scope can be any `VisualElement` attached to a panel (`UIDocument`, a `PanelRenderer` on Unity 6000.6+, or an editor window if you provide the scope and tick it).

---

## 12. Troubleshooting

| Symptom | Likely cause and fix |
| --- | --- |
| Nothing animates, console warns "No motion 'X' in the UIMotionLibrary" | The id is not in the library: rebuild it (`Tools > UI Motion Toolkit > Rebuild Default Motion Library`), check the asset **name** (not the path), or assign `UIMotion.Library` in code. |
| Warns once, then the UI just changes state | Expected behaviour for a missing id. |
| The motion runs but an element does not move | Its track selector matched nothing in the scope. Check the track name, that the element is a **descendant of the scope** (or the scope itself with `:scope`), and that it existed when the playback started (otherwise `Bind()`). |
| It works from the Timeline preview but not in play mode | The preview scope is the whole document; at runtime your scope is the element you passed. Pass the screen/root, or write `:scope` / relative selectors. |
| A pop or flash at the **end** of a motion | Rest-state mismatch: the end of the motion differs from the USS (see section 7). Align the USS (class / pseudo-class) or the clip end values. |
| Hover/focus feels jumpy | Use `Toggle` (reverses from the current time), not two separate `Play`s. Check there is only one motion for that state in the same channel. |
| Mouse hover does not trigger the focus motion | UI Toolkit hover is not focus. Use `ToggleOnHover`, or call `element.Focus()` on `PointerEnterEvent`. |
| Color does not change | The clip's `Target` does not match what the element uses (text uses `TextColor`; `ImageTint` needs a background image). |
| Color flashes the wrong value at the start | A color clip without *Override Neutral* reads the USS color at bind; set the neutral explicitly for state motions. |
| Scale/opacity values look "off by one" in the Inspector | Those clips store offsets from the neutral (1): fully transparent is `-1`, scale 0 is `-1`. Use the code helpers or read section 4.3. |
| The element ends in the wrong place after the motion | Inline styles are removed at the end. Put the final position/size in USS or a class, not in a motion-only end value. |
| `Hide` never hides | `.is-hidden` has no rule. Add `.is-hidden { display: none; }`. |
| Animations stop when switching scenes or removing elements | Playbacks drop when their scope leaves the panel. Expected. |
| Tests do not appear in the Test Runner | Add the package to `testables` in `Packages/manifest.json`. |
| Compile error: assembly `UITTimeline` not found | Install *UI Toolkit Timeline* from its Git URL (section 2.2). |
| *Create Preview Director* asks for a UIDocument | Select the GameObject with the `UIDocument` (or one of its children) first. |

---

## 13. API cheat sheet

```csharp
// Namespace: UIMotionToolkit
UIMotion.Library = myLibrary;                                  // optional (default: Resources/UI/UIMotionLibrary)

UIMotion.Play(scope, "id", onFinished, channel, loop);        // one-shot / loop
UIMotion.Toggle(scope, "id", on, onFinished, channel, instant); // reversible state
UIMotion.Show(element, "showId", onShown, hiddenClass, instant);
UIMotion.Hide(element, "showId", "hideId", onHidden, hiddenClass, instant);

UIMotion.Stop(scope, channel, restore);   UIMotion.Complete(scope, channel);
UIMotion.StopUnder(root);                 UIMotion.StopAll();
UIMotion.IsPlaying(scope, channel);       UIMotion.Get(scope, channel);
UIMotion.IsShown(element);                UIMotion.IsHiding(element);
UIMotion.Has("id");                       UIMotion.Find("id");
UIMotion.ManualTick = true;               UIMotion.Tick(dt);

UIMotionTriggers.ToggleOnFocus / ToggleOnHover / PlayOnPress / BindAll

// Editor (namespace UIMotionToolkit.Editor)
MotionAsset.Begin(path).Track(selector, stagger, staggerReverse, label)
    .FadeIn / FadeOut / FadePulse / MoveFrom / MoveTo / MovePulse / ScaleFrom / ScaleTo / ScalePulse / Pop
    .RotateFrom / RotateTo / RotatePulse / ColorFrom / ColorTo / ColorPulse / Shake / Class / Display / Visible
MotionAsset.Save();
UIMotionLibraryBuilder.Rebuild(path, folders...);  UIMotionLibraryBuilder.RebuildDefault();
UIMotionPreviewRenderer.Render(request, done);
```

Menus: `Tools > UI Motion Toolkit > Rebuild Default Motion Library`, `Create Preview Director`; `GameObject > UI Toolkit > UI Motion Preview Director`; `Assets > Create > UI Motion Toolkit > Motion Library`.
