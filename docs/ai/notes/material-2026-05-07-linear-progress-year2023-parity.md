# Feature: material-2026-05-07-linear-progress-year2023-parity

## Goal

- Close `LinearProgressIndicator.year2023` parity:
  - API and theme precedence wiring (`widget -> ProgressIndicatorTheme.year2023 -> default true` for M3),
  - Flutter-like 2023/2024 default switching for stop indicator, track gap, and default border radius.

## Non-Goals

- Full advanced `valueColor` parity surface for progress indicators.
- Additional adaptive Cupertino progress-indicator behavior.

## Context Budget Plan

- Budget: max 14 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Material/ProgressIndicatorTheme.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/LinearProgressIndicatorDemoPage.cs`
  - `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `LinearProgressIndicator` (`year2023`)
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
  - Theme precedence remains in framework layer (`widget -> local theme -> ThemeData defaults`).
  - Rendering behavior remains render-object owned; host-specific UI behavior was not introduced.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
  - `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - none for `year2023` in current framework scope.

## Planned Changes

- Files edited:
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/LinearProgressIndicatorDemoPage.cs`
  - `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
- New tests added:
  - explicit M3 `year2023=true` and `year2023=false` default coverage.
  - theme/widget precedence coverage updated with `Year2023=false` path.

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
