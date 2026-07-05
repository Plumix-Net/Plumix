# Feature: material-checkbox-baseline-parity

## Goal

- Add framework Material `Checkbox` with Flutter-like core behavior for current framework scope: controlled value (`bool?`), `tristate` cycle, selected/unselected/disabled visuals, focus/hover/pressed overlays, and tap-target policy parity via `ThemeData.MaterialTapTargetSize`.

## Non-Goals

- Full Flutter API surface in one step (`Checkbox.adaptive`, `mouseCursor`, `semanticLabel`, `splashRadius`, dedicated `CheckboxThemeData`).
- Toggleable painter animation parity (stroke morph animation between unchecked/check/dash states).

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/IconButton.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
- Expansion trigger:
  - Add missing control files/tests/sample routes needed to close `Checkbox` baseline in one iteration.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Checkbox`
- Completion checklist (must be closed in this iteration unless explicitly blocked):
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework behavior stays in framework layers (`src/Flutter.Material`), not host adapters.
  - Sample parity is updated in both C# and Dart samples in the same iteration.
  - Dart source is treated as reference for defaults/state cycle, with documented divergences.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/checkbox.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/toggleable.dart`
  - `dart_sample/lib/demos/cupertino/checkbox_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/Checkbox.cs`: no toggleable stroke animation (`_CheckboxPainter`) yet; current framework lacks reusable custom-painter/toggleable primitive stack. Expected delta: check/dash switches immediately without tweened morph animation. Follow-up condition: add/port toggleable painter primitives, then wire animated checkbox transitions.
  - `src/Flutter.Material/Checkbox.cs`: API is scoped baseline (no adaptive constructor, no dedicated checkbox theme class/property lookup). Expected delta: smaller theming/API surface than upstream Flutter. Follow-up condition: introduce `CheckboxThemeData` and adaptive branch in subsequent Material parity passes.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Checkbox.cs`
  - `src/Flutter.Material/Icons.cs`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `src/Sample/Flutter.Net/Demos/Cupertino/CheckboxDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/cupertino/checkbox_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/PARITY_MATRIX.md`
- Brief intent per file:
  - `src/Flutter.Material/Checkbox.cs`: implement control/widget behavior and mode-aware defaults.
  - `src/Flutter.Material/Icons.cs`: add `Icons.Check` glyph used by checkbox indicator.
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`: add focused parity-critical tests.
  - sample files: add runtime parity page and route wiring on both C# and Dart samples.
  - docs/changelog files: update iteration tracking and parity matrices.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
- New tests to add:
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
- Parity-risk scenarios covered:
  - `tristate` state cycle (`false -> true -> null -> false`)
  - mode-aware visual defaults for selected/unselected/disabled states
  - tap-target policy (`padded` vs `shrinkWrap`)
  - check/dash indicator rendering paths

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps (if any) are documented with blocker + next action
