# Feature: material-2026-05-07-progress-indicator-year2023-controller-parity

## Goal

- Close the next progress-indicator parity slices:
  - `CircularProgressIndicator` `year2023` behavior toggle (2023 vs 2024 M3 defaults),
  - external controller support for both `LinearProgressIndicator` and `CircularProgressIndicator`.

## Non-Goals

- Full remaining advanced parity (`valueColor` API parity, adaptive Cupertino branch, and deprecated `year2023` migration removal strategy).
- Removing legacy `size`/`circularSize` compatibility fallbacks in this iteration.

## Context Budget Plan

- Budget: max 16 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Material/ProgressIndicatorTheme.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
- Expansion trigger:
  - Expand only if controller lifecycle parity requires shared animation primitives beyond existing `AnimationController` + widget-state subscription wiring.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `CircularProgressIndicator` (`year2023` + controller)
  - `LinearProgressIndicator` (controller)
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
  - Animation state and repaint triggers remain widget/render-owned in framework code.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Plumix.Material/ProgressIndicator.cs`: legacy `size` (`CircularProgressIndicator`) and theme `circularSize` remain as explicit compatibility fallbacks behind `constraints`/`circularConstraints` precedence.

## Planned Changes

- Files to edit:
  - `src/Plumix.Material/ProgressIndicatorTheme.cs`
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - add `year2023` + controller API/theme precedence, rewire state controller lifecycle, extend focused tests, and sync sample/docs parity.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
- New tests to add:
  - expanded controller + year2023 assertions in both test files.
- Parity-risk scenarios covered:
  - circular default 2023 M3 path vs explicit/theme `year2023=false` 2024 path.
  - explicit controller precedence over theme controller and internal fallback.
  - value+controller guard behavior for both indicators.

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
