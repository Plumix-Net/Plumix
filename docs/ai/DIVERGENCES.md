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
