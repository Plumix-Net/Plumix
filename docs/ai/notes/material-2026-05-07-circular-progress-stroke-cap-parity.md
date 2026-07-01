# Feature: material-2026-05-07-circular-progress-stroke-cap-parity

## Goal

- Close the next `CircularProgressIndicator` parity slice by adding Flutter-like `strokeCap` API + theme/widget precedence + render behavior, with focused tests and C#/Dart sample parity updates.

## Non-Goals

- Full advanced circular progress-indicator parity matrix (`strokeAlign`, full `constraints` surface, `year2023` behavior toggle, external `controller`, and adaptive Cupertino branch).
- Host-side accessibility role/value mapping beyond current semantics-label behavior.

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
  - Expand only if painter parity requires new rendering primitives outside the existing progress-indicator path.

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
  - Theme precedence remains in framework layer (`widget -> local theme -> ThemeData defaults`).
  - Render-owned paint behavior stays in `src/Plumix.Material/ProgressIndicator.cs`.

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
  - `src/Plumix.Material/ProgressIndicator.cs`: `strokeAlign`, full `constraints` matrix, `year2023`, external `controller`, and adaptive Cupertino branch remain out of scope for this iteration.

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
  - add circular `strokeCap` API + precedence + painter behavior, verify with focused tests, and sync sample/docs parity.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
- New tests to add:
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs` (expanded assertions)
- Parity-risk scenarios covered:
  - M3 default null-cap behavior (determinate butt / indeterminate square) remains intact.
  - Theme + widget override precedence for explicit circular `strokeCap`.
  - Existing track-gap and semantics behavior remains intact.

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
