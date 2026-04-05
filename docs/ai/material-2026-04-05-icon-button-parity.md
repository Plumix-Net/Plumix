# Feature: material-2026-04-05-icon-button-parity

## Goal

- Port Flutter `IconButton` into `Flutter.Material` with Material-style defaults, `styleFrom(...)`, toggle selection (`isSelected`/`selectedIcon`), M3 variants (filled/filled tonal/outlined), and theme wiring (`IconButtonTheme`, `ThemeData.iconButtonTheme`).

## Non-Goals

- Full Flutter parity for legacy M2-only `IconButton` APIs that depend on missing framework primitives (`Tooltip`, `MouseCursor`, `VisualDensity`, and raw `InkResponse` specialization).
- Introducing a full core `Icon`/`Icons` glyph library in framework widgets.

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/ButtonThemes.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`
  - `dart_sample/lib/material_buttons_demo_page.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/icon_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/icon_button_theme.dart`
- Expansion trigger:
  - Extend `MaterialButtonCore` state plumbing when selected-state/hover/long-press parity is required by `IconButton`.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `IconButton`
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
  - Framework behavior remains in `src/Flutter.Material` with shared render/interactions in framework primitives (`MaterialButtonCore`), not Avalonia controls.
  - Sample parity between `src/Sample/Flutter.Net` and `dart_sample` is preserved in the same iteration.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/icon_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/icon_button_theme.dart`
  - `dart_sample/lib/material_buttons_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/IconButton.cs`: legacy M2 `InkResponse`-specific knobs (`mouseCursor`, `enableFeedback`, `tooltip`, `visualDensity`) remain unported because equivalent framework primitives are currently absent; `IconButton` is implemented through shared `MaterialButtonCore` state/style pipeline.
  - `src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`: sample uses `DemoIconGlyph` (IconTheme-driven text glyph probe) instead of Flutter `Icon` because framework-level `Icon`/`Icons` primitives are not yet implemented.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/IconButton.cs`
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/ButtonThemes.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`
  - `dart_sample/lib/material_buttons_demo_page.dart`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `IconButton.cs`: new `IconButton` implementation + style defaults + theme/style composition.
  - `ButtonStyle.cs`: add `MaterialState.Selected`.
  - `ButtonThemes.cs`/`ThemeData.cs`: add `IconButtonThemeData` + inherited theme + `ThemeData` wiring.
  - `Buttons.cs`: extend `MaterialButtonCore` for selected state and optional hover/long-press callbacks.
  - `MaterialButtonsTests.cs`: add focused `IconButton` parity coverage.
  - sample files: add icon-button runtime probes and counters in C#/Dart parity demos.
  - docs/changelog files: reflect new control parity status and coverage map.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
- New tests to add:
  - `IconButton` defaults/theme/style/selection/tap-target tests inside `MaterialButtonsTests`.
- Parity-risk scenarios covered:
  - M3 default icon color+size tokens and min size.
  - M2 size fallback (`48x48`) when `UseMaterial3=false`.
  - Theme precedence (`IconButtonTheme` over ambient `IconTheme`, widget style over theme).
  - Selected icon rendering path and outlined selected-border behavior.
  - Tap-target size override via `styleFrom(tapTargetSize: ...)`.

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
