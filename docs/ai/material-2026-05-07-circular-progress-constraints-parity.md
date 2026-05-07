# Feature: material-2026-05-07-circular-progress-constraints-parity

## Goal

- Close the next `CircularProgressIndicator` parity slice by adding Flutter-like `constraints` support (`widget -> theme -> defaults`) with focused tests and C#/Dart sample parity updates.

## Non-Goals

- Full remaining advanced circular progress parity (`year2023`, external `controller`, adaptive Cupertino branch).
- Removing the existing framework `size` API in this iteration (kept as backward-compatible fallback).

## Context Budget Plan

- Budget: max 14 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Material/ProgressIndicatorTheme.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
- Expansion trigger:
  - Expand only if layout parity needs additional shared primitives beyond existing `ConstrainedBox` + render layout path.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `CircularProgressIndicator`
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
  - Theme precedence stays in framework code (`widget -> local theme -> ThemeData defaults`).
  - Layout/paint ownership remains in framework widget/render layers, not host adapters.

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
  - `src/Plumix.Material/ProgressIndicator.cs`: kept legacy `size` as compatibility fallback while new Flutter-like `constraints` precedence is primary. Follow-up for hard removal can be done in a dedicated breaking-change pass.

## Planned Changes

- Files to edit:
  - `src/Plumix.Material/ProgressIndicatorTheme.cs`
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - add circular constraints API + precedence + defaults, keep sample parity, and document/test the new behavior.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
- New tests to add:
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs` (expanded assertions)
- Parity-risk scenarios covered:
  - M3/M2 default constraints resolution (`40` vs `36`) under loose parent constraints.
  - theme/widget override precedence for `constraints`.
  - legacy `size` fallback behavior retained intentionally.

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
