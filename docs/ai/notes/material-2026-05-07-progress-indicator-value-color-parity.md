# Feature: material-2026-05-07-progress-indicator-value-color-parity

## Goal

- Close the next progress-indicator parity slice by adding `valueColor` precedence and live color updates for both `LinearProgressIndicator` and `CircularProgressIndicator`.
- Keep Flutter-like resolution order:
  - `valueColor` -> `color` -> `ProgressIndicatorThemeData.color` -> theme default.

## Non-Goals

- Full Flutter `Animation<Color?>` surface parity (framework keeps `ValueNotifier<Color?>` for this iteration).
- Adaptive Cupertino progress-indicator parity.

## Context Budget Plan

- Budget: max 14 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Material/ProgressIndicatorTheme.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/LinearProgressIndicatorDemoPage.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`
  - `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `LinearProgressIndicator` (`valueColor`)
  - `CircularProgressIndicator` (`valueColor`)
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
  - Theme and widget precedence remains in framework layer.
  - Render-object paint stays framework-owned; host adapters are untouched.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
  - `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - Framework uses `ValueNotifier<Color?>` for `valueColor` instead of Flutter's `Animation<Color?>`; precedence and live update semantics are preserved for current framework scope.

## Planned Changes

- Files edited:
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/LinearProgressIndicatorDemoPage.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`
  - `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
- New tests added:
  - `LinearProgressIndicator_ValueColor_OverridesColorAndUpdatesFromNotifier`
  - `CircularProgressIndicator_ValueColor_OverridesColorAndUpdatesFromNotifier`
- Parity-risk scenarios covered:
  - `valueColor` precedence over `color`/theme/default.
  - Fallback when `valueColor.Value == null`.
  - Runtime color updates via notifier listener (without recreating indicator widget).

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
