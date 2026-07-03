# Divergence Registry

Single source of truth for intentional behavior/structure divergences from Flutter (Dart).

Rules:

- Every intentional divergence from Flutter behavior must have exactly one row here, added in the same iteration that introduces it (see `docs/ai/PORTING_MODE.md`).
- A row is removed only when the divergence is closed; record the closure in `CHANGELOG.md`.
- Divergences recorded before 2026-07-01 live in archived feature notes under `docs/ai/notes/`. Migration rule: when you touch a control/subsystem, check its notes for still-active divergences, move them into this table, and do not add new divergence text to notes.

Row format:

- **Area/File**: framework file(s) the divergence lives in.
- **Divergence**: what differs from Flutter and why (platform/runtime constraint).
- **Expected delta**: user-visible or behavioral difference.
- **Close condition**: what must land to remove the divergence.

## Active Divergences

| Area/File | Divergence | Expected delta | Close condition |
| --- | --- | --- | --- |
| _(migrate entries from `docs/ai/notes/` as controls are touched)_ | | | |
| `src/Plumix.Material/Badge.cs` | `Badge.Alignment` uses concrete `Alignment` because the core framework does not yet expose Flutter's `AlignmentGeometry`/`AlignmentDirectional` hierarchy. Default top-end behavior and offsets still resolve against ambient `Directionality`. | Custom directional alignment values cannot be supplied as a first-class object; callers must choose an LTR/RTL concrete alignment. | Add the shared alignment-geometry hierarchy in `Plumix`, migrate alignment-taking widgets, then switch `Badge` to `AlignmentGeometry`. |
| `src/Plumix.Material/Tooltip.cs` | Tooltip content is composed in a local `Stack`, and plaintext is supported, because shared root `Overlay`/`RawTooltip`, `InlineSpan` rich text, and pointer-ignoring primitives are not yet available in core. | Tooltips cannot escape all ancestor bounds or auto-flip against window edges; `richMessage`, custom position delegate, and interactive rich tooltip content are unavailable. | Land core overlay/raw-tooltip positioning, rich-text spans, and ignore-pointer primitives, then adopt Flutter's exact `RawTooltip` composition. |
| `src/Plumix/Rendering/ImageProvider.cs`, `ImageStream.cs` | Avalonia's public bitmap decoder exposes a single decoded image rather than Flutter's codec/frame API, so providers currently complete through `OneFrameImageStreamCompleter`. | Animated image formats render as one static frame; frame timing and repetition-count behavior are unavailable. | Add a platform codec abstraction that exposes frame count/durations and port `MultiFrameImageStreamCompleter` scheduling. |
| `src/Plumix/Rendering/DecorationImage.cs`, `Object.PaintingContext.cs` | Avalonia's public `DrawingContext` path does not expose Flutter-equivalent per-draw color matrices, smart inversion, or an isolated `saveLayer` for additive decoration-image blending. Sampling quality and edge antialiasing are now mapped through Avalonia `RenderOptions`. | `colorFilter` and `invertColors` do not change pixels; mid-transition image crossfades use source-over rather than Flutter's isolated additive blend and can have a small alpha difference. | Add a backend image-effect/save-layer bridge, then wire color matrices, inversion, and isolated additive blending. |
| `src/Plumix/Widgets/ImplicitAnimations.cs`, `Rendering/Decoration.cs` | The current core `BoxDecoration` exposes Avalonia `IBrush`/`BoxShadows` rather than Flutter's gradient/shadow value hierarchy, so `AnimatedContainer` interpolates colors, borders, radii, images, and constraints but selects arbitrary brushes/shadow collections at the midpoint. | Animated brush or shadow changes snap at 50%; `CircleAvatar` is unaffected because it animates color, images, and constraints. | Introduce framework-native gradient/shadow lerp contracts and use them from `BoxDecoration.Lerp`. |
| `src/Plumix.Material/Chips.cs`, `FilterInputChips.cs` | Flutter's `RawChip` uses a private three-slot render object; the current framework lacks slotted render-object widgets, so the same avatar/checkmark/label/delete composition uses `Row`/`Stack`/factor layout while selection and delete drawers use framework controllers. | Extreme text scaling and custom avatar/delete constraints can differ by a few pixels; enable and avatar-drawer timing is not yet exact. | Add shared slotted render-object widget support and port `_RenderChip` geometry plus enable/avatar animation choreography. |
| `src/Plumix.Material/ToggleButtons.cs`, `SegmentedButton.cs` | Core painting currently exposes uniform `BorderRadius` rather than Flutter's full per-corner `OutlinedBorder` hierarchy, and ink effects use the framework splash pipeline rather than pluggable `InteractiveInkFeatureFactory`. | Default stadium/rounded geometry and state borders match, but asymmetric custom segment shapes and custom `splashFactory` implementations cannot be supplied. | Add the shared `OutlinedBorder`/directional-radius hierarchy and pluggable ink-feature factory, then widen segmented-control shape/style APIs. |
| `src/Plumix.Material/MaterialBanner.cs` | Plumix does not yet provide Flutter's `ScaffoldMessenger` feature queue/controller or generic `Animation<double>` surface, so static banners match and animated banners accept `AnimationController`; semantic dismiss reverses that controller instead of removing a messenger-owned feature. Insets use physical `Thickness`, with directional Flutter defaults resolved at build time. | Direct-tree banners, visuals, action layout, transitions, and accessibility flags match; queued show/hide/remove/swipe lifecycle, closed-reason futures, and custom directional inset objects are unavailable. | Port `ScaffoldMessenger`/`ScaffoldFeatureController`, generic animation adapters, and shared directional edge-insets, then route banner presentation/dismissal through those primitives. |
