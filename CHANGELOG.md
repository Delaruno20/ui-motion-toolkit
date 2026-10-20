# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project adheres to [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-10-20

First public release.

### Added
- `UIMotion` static player: `Play` (one-shot, loop), `Toggle` (reversible state), `Show` / `Hide` (presence with the `is-hidden` class), `Stop`, `StopUnder`, `StopAll`, `Complete`, `Tick`.
- `UIMotionPlayback` with time, direction, speed, loop, `Finished` event, `Bind()`.
- Channels: one playback per scope and channel name.
- `UIMotionTrack` (track name = selector, stagger, reverse stagger, automatic usage hints).
- Clips: color (background / image tint / text / border) and shake, alongside the UI Toolkit Timeline clips (position, scale, rotation, opacity, class, display, visibility).
- `UIMotionSelector`: `#id`, `.class`, compound, descendant, child, union, `:scope`.
- `UIMotionLibrary` (id to timeline) and `UIMotionTriggers` (focus / hover / press).
- Timeline-window preview (`UIMotionDirectorMixer`, `UIMotionPreviewScope`, *Create Preview Director* menu).
- Editor authoring API `MotionAsset` / `MotionTrackBuilder` / `MotionEase` and `UIMotionLibraryBuilder`.
- Offscreen frame capture `UIMotionPreviewRenderer`.
- Samples: *Basic Menu* (8 motions) and *Dark Fantasy Showcase* (24 motions).
- 18 EditMode tests.
