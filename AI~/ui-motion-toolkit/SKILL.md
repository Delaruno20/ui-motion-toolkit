---
name: ui-motion-toolkit
description: Animate Unity UI Toolkit (UXML/USS) interfaces with the UI Motion Toolkit package. Use when the user wants menus, HUDs, popups, toasts, tabs, buttons or any UI Toolkit element animated (fade, slide, scale, color, shake, stagger, hover/focus states, show/hide). Works without Unity access: you write the C# and USS, the user pastes it in.
---

# UI Motion Toolkit: instructions for an AI assistant

You are helping a Unity developer animate **UI Toolkit** UI with **UI Motion Toolkit** (UPM package `com.delaruno20.ui-motion-toolkit`, repo `github.com/Delaruno20/ui-motion-toolkit`, namespace `UIMotionToolkit`, tested on Unity 6000.6). You may have **no access to Unity**: never assume you can open the editor, click, or read the project. Produce **text the user pastes**: USS, UXML, one editor builder script, one runtime script. Be explicit about where each file goes.

If something is not in this file, say you are unsure instead of inventing API. Full docs: `Documentation~/UserGuide.md` in the repo.

## 1. Mental model (explain in one sentence if the user is new)

A **motion** = a Timeline asset whose **tracks are named after USS-like selectors** (`.menu-item`, `#title`, `:scope`). At runtime you give a **scope** element and a **motion id** (the asset name); the toolkit animates the matching elements inside the scope with inline styles, then **clears them** when done, so the USS stays the rest state.

You generate the motion assets **with code** (an editor script using `MotionAsset`), so no Timeline-window clicking is needed.

## 2. Install (tell the user once)

1. Package Manager > + > Install from git URL: `https://github.com/mihakrajnc/UITTimeline.git` (required clips, install FIRST).
2. Package Manager > + > Install from git URL: `https://github.com/Delaruno20/ui-motion-toolkit.git`.
3. Compile errors about `UITTimeline` mean step 1 is missing.

## 3. Runtime API (namespace `UIMotionToolkit`)

```csharp
UIMotion.Library = myLibrary;                       // optional; default loads Resources/UI/UIMotionLibrary

UIMotion.Play(scope, "Id", onFinished, channel, loop);          // one-shot (hit, pulse, intro, loop)
UIMotion.Toggle(scope, "Id", on, onFinished, channel, instant); // reversible state: focus, hover, selected
UIMotion.Show(element, "ShowId", onShown);                      // removes "is-hidden", then animates in
UIMotion.Hide(element, "ShowId", "HideId", onHidden);           // animates out, then adds "is-hidden" (HideId null = reverse ShowId)

UIMotion.Stop(scope, channel); UIMotion.Complete(scope, channel);   // Complete = jump to end, callbacks run (skip)
UIMotion.StopUnder(root); UIMotion.StopAll();
UIMotion.IsPlaying(scope, channel); UIMotion.IsShown(element); UIMotion.Has("Id");

UIMotionTriggers.ToggleOnFocus(el, "Id"); ToggleOnHover(el, "Id"); PlayOnPress(el, "Id");
UIMotionTriggers.BindAll(root, ".selector", "FocusId", "PressId");  // focus + press on every match
```

Facts:
- Ids are **timeline asset names**, resolved through a `UIMotionLibrary`. A missing id is not an error: the state changes instantly and a warning is logged once.
- A channel defaults to the motion id. A new playback in a busy `(scope, channel)` replaces the old one. Use explicit `channel:` to stop a loop later.
- Unscaled time: works while `Time.timeScale = 0`.
- `Show`/`Hide` need `.is-hidden { display: none; }` in USS.
- Playbacks bind selectors when they start. Elements added later need a new play (or `playback.Bind()`).
- Mouse hover does NOT focus a UI Toolkit button. For a hover look, call `element.Focus()` on `PointerEnterEvent`, or use `ToggleOnHover`.

### Getting the root element: UIDocument or PanelRenderer

UIMotion works on any `VisualElement`. Ask which component the user has, or detect it from their code. Do not assume `UIDocument`: PanelRenderer is the newer component (Unity 6000.6+).

UIDocument (read the root in `Start()`):
```csharp
void Start() => Bind(GetComponent<UIDocument>().rootVisualElement);
```

PanelRenderer (it builds and may REBUILD the tree: bind in the reload callback every time; use the 3-argument callback, the 2-argument one is obsolete):
```csharp
PanelRenderer _renderer;
void OnEnable()  { _renderer = GetComponent<PanelRenderer>(); _renderer.RegisterUIReloadCallback(OnUIReload); }
void OnDisable() => _renderer.UnregisterUIReloadCallback(OnUIReload);
void OnUIReload(PanelRenderer renderer, VisualElement root, int version) => Bind(root);
```
Rules for PanelRenderer: register event handlers (`clicked +=`, `RegisterCallback`) and `UIMotion.*` calls inside `Bind(root)`, on the new tree; never cache elements across reloads; wrap in `#if UNITY_6000_6_OR_NEWER` if the project may target older Unity. Everything else (motions, library, builder script, USS) is identical for both components.

## 4. Authoring API (editor-only; namespace `UIMotionToolkit.Editor`)

```csharp
MotionAsset m = MotionAsset.Begin("Assets/Motions/Menu.Show.asset");   // create/empty the asset, name = id
m.Track(".menu-item", stagger: 0.07f, staggerReverse: false)           // selector; stagger = delay between matched elements
    .FadeIn(at, duration)                      // seconds
    .MoveFrom(new Vector2(-48, 0), at, dur, MotionEase.OutCubic);
m.Save();
UIMotionLibraryBuilder.RebuildDefault();       // MUST run after building: adds every motion to Assets/Resources/UI/UIMotionLibrary.asset
```

Helpers (all `at`/`duration` in seconds; chain several on one `Track(...)`):

| Kind | Helpers |
| --- | --- |
| Enter (starts displaced, ends at USS state) | `FadeIn(at,dur,ease,from)`, `MoveFrom(offsetPx,at,dur,ease)`, `ScaleFrom(float\|Vector2,at,dur,ease)`, `RotateFrom(deg,...)`, `Pop(at,dur,from)` (springy scale), `ColorFrom(target,color,at,dur,ease,neutral)` |
| Exit (starts at USS state, ends displaced, holds) | `FadeOut`, `MoveTo`, `ScaleTo`, `RotateTo`, `ColorTo(target,color,at,dur,ease,neutral)` |
| Pulse (goes and comes back; for loops/feedback) | `FadePulse(at,dur,to)`, `MovePulse`, `ScalePulse(scale,at,dur,ease,inShare)`, `RotatePulse`, `ColorPulse(target,color,at,dur,ease,inShare,neutral)` |
| Other | `Shake(at,dur,amplitudePx,rotation,frequency,seed)`, `Class(name,at,dur)`, `Display(bool,at,dur)`, `Visible(bool,at,dur)` |

- `UIMotionColorTarget`: `BackgroundColor`, `ImageTint`, `TextColor`, `BorderColor`.
- `MotionEase`: `Linear, InSine, OutSine, InOutSine, InCubic, OutCubic, InOutCubic, OutQuint, OutExpo, InExpo, OutBack, InBack`.
- Each property of a selector becomes its own Timeline track; do not overlap two clips of the same property on one selector (use a second `Track(selector, label: "x")`).
- `:scope` targets the scope element itself.
- `neutral:` on color helpers = the exact USS color of the "off" look. **Always pass it for state (Toggle) motions.**
- Scale values are absolute factors (`0.95f` = 95%); position is in px; `from` for fades is the start opacity (0 default).

## 5. Selectors (track names)

`#name`, `.class`, `#a.b`, `*`, `a b` (descendant), `a > b` (child), `a, b` (union), `:scope`. Text after `|` is a label. No attribute selectors, no pseudo-classes except `:scope`.

## 6. The rest-state contract (most common source of bugs)

USS defines how elements **rest**. A motion animates to/from that. When it ends, inline styles are removed.

- **Enter motions** (`FadeIn`, `MoveFrom`...) end at opacity 1 / translate 0 / scale 1: do not set conflicting USS for those properties on the animated element.
- **State motions** (used with `Toggle`): the motion END must equal the USS state (`:focus`, `.is-selected`), its START the base USS state. Example:

```css
.btn { background-color: rgb(30,25,32); }
.btn:focus { translate: 10px 0; background-color: rgb(74,24,32); }
```
```csharp
m.Track(":scope")
 .MoveTo(new Vector2(10, 0), 0, 0.16, MotionEase.OutCubic)
 .ColorTo(UIMotionColorTarget.BackgroundColor, new Color(74/255f, 24/255f, 32/255f), 0, 0.16,
          MotionEase.OutCubic, neutral: new Color(30/255f, 25/255f, 32/255f));
```
- `translate`, `scale`, `rotate` are written as absolute values while a motion runs (they replace the USS value for that time).
- Never use USS `transition` on properties a motion drives (transform, opacity, animated colors).
- To keep a state after the animation, change a **USS class** in code *before* calling `Toggle`.

## 7. What you deliver (no Unity access needed)

For each request, output in this order and name every file path:

1. **UXML** (only if the UI does not exist yet) and **USS** with the rest states and `.is-hidden { display: none; }`.
2. **Editor builder script** `Assets/Editor/<Name>Motions.cs` (needs `using UIMotionToolkit; using UIMotionToolkit.Editor; using UnityEditor; using UnityEngine;`) with a `[MenuItem("Tools/<Project>/Build <Name> Motions")]` method that builds every motion, ends with `UIMotionLibraryBuilder.RebuildDefault();`.
3. **Runtime script** that calls `UIMotion.*` (a MonoBehaviour next to the `UIDocument` or `PanelRenderer`; for UIDocument query elements in `Start()`, not `Awake`/`OnEnable`; for PanelRenderer bind in the reload callback, see section 3).
4. **Steps for the user**: install packages, add files, run the menu item once, press Play. Tell them to re-run the menu item after any change to the builder.

Template for the editor script:

```csharp
using UIMotionToolkit;
using UIMotionToolkit.Editor;
using UnityEditor;
using UnityEngine;

public static class MenuMotions
{
    [MenuItem("Tools/MyGame/Build Menu Motions")]
    public static void Build()
    {
        MotionAsset show = MotionAsset.Begin("Assets/Motions/Menu.Show.asset");
        show.Track(":scope").FadeIn(0, 0.35).MoveFrom(new Vector2(0, 28), 0, 0.5, MotionEase.OutCubic);
        show.Track(".menu-item", stagger: 0.07f).FadeIn(0.3, 0.3).MoveFrom(new Vector2(-48, 0), 0.3, 0.45, MotionEase.OutCubic);
        show.Save();

        MotionAsset hide = MotionAsset.Begin("Assets/Motions/Menu.Hide.asset");
        hide.Track(".menu-item", stagger: 0.05f, staggerReverse: true).FadeOut(0, 0.2).MoveTo(new Vector2(-36, 0), 0, 0.25, MotionEase.InCubic);
        hide.Track(":scope").FadeOut(0.2, 0.25);
        hide.Save();

        UIMotionLibraryBuilder.RebuildDefault();
    }
}
```

Template for the runtime script:

```csharp
using UIMotionToolkit;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MenuController : MonoBehaviour
{
    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        VisualElement menu = root.Q("menu");
        UIMotion.Show(menu, "Menu.Show");
        UIMotionTriggers.BindAll(root, ".menu-item", "Item.Focus", "Item.Press");
        foreach (var item in root.Query(className: "menu-item").ToList())
            item.RegisterCallback<PointerEnterEvent>(_ => item.Focus());
    }
}
```

## 8. Recipes (pick, adapt, keep durations 0.15 to 0.6 s for UI feedback)

- **Screen entrance**: `:scope` fade+move, children with `stagger` 0.05 to 0.1, `OutCubic`. Exit: same with `*To` and `staggerReverse`, `InCubic`.
- **Button feedback**: `Item.Focus` (Toggle, move/color, `neutral` colors) + `Item.Press` (`ScalePulse(0.95f, 0, 0.2, MotionEase.OutSine, 0.35)`).
- **Popup**: backdrop `FadeIn`, window `Pop(...)` + `FadeIn` + `MoveFrom`, lines staggered. `UIMotion.Show/Hide`.
- **Hit/damage**: `Shake` + `ColorPulse` (BackgroundColor to white) on a bar; reuse one motion for every bar using relative selectors (`:scope`, `.bar__fill`).
- **Tabs with direction**: `Page.InRight/InLeft/OutLeft/OutRight` variants chosen from the index delta; selected tab as a `Toggle` state.
- **Toast built at runtime**: add element to parent, then chain `Play(Show)` -> `Play(Life)` -> `Play(Hide)` -> `RemoveFromHierarchy()` through `onFinished`.
- **Loop**: only `*Pulse` clips; `UIMotion.Play(scope, "Id", channel: "ambient", loop: true)`; stop with `UIMotion.Stop(scope, "ambient")`.
- **Skippable intro**: `UIMotion.Complete(scope, "Intro")` on click.
- **Reduce motion setting**: pass `instant: true` to `Show/Hide/Toggle`.

## 9. Troubleshooting checklist (use when the user reports a bug)

1. Console says "No motion 'X' in the UIMotionLibrary": run the builder menu item again, check the id equals the asset name, or `UIMotion.Library` is null.
2. Nothing moves: the track selector matches nothing inside the scope (typo, element outside the scope, class missing). Use `:scope` for the scope itself.
3. Pop/flash at the end: the motion end differs from the USS rest/`:focus` state (section 6).
4. `Hide` does nothing visible: `.is-hidden` rule missing.
5. Hover does nothing: UI Toolkit hover is not focus (section 3).
6. Colors wrong at start: missing `neutral:` on a state motion.
7. Compile error `UITTimeline`: dependency not installed (section 2).
8. PanelRenderer: nothing animates after a reload, or handlers fire twice: bindings must live in the reload callback on the new root, with the 3-argument signature.
9. Motions work in play mode only after the library was rebuilt (menu item or `UIMotionLibraryBuilder.RebuildDefault()`).

## 10. Rules for you

- Prefer generating motions by code (section 4); do not tell the user to hand-author Timeline clips unless they ask.
- Do not invent methods. The API above is complete for typical use.
- Do not use USS `transition` for anything a motion drives.
- Keep rest states in USS, never in motions.
- Ask at most one clarifying question; otherwise state your assumption (names, selectors, durations) and deliver the files.
- Answer in the user's language; keep code and identifiers in English.
