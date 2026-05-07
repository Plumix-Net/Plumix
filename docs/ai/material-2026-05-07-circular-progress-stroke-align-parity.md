# Feature: material-2026-05-07-circular-progress-stroke-align-parity

## Goal

- Close the next `CircularProgressIndicator` parity slice by adding Flutter-like `strokeAlign` API + theme/widget precedence + painter geometry behavior, with focused tests and C#/Dart sample parity updates.

## Non-Goals

- Full advanced circular progress-indicator parity matrix (`constraints` surface, `year2023` behavior toggle, external `controller`, and adaptive Cupertino branch).
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
  - Expand only if arc-path geometry needs new shared rendering primitives beyond existing `DrawArc` path.

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
  - Render-owned geometry/paint behavior stays in `src/Plumix.Material/ProgressIndicator.cs`.

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
  - `src/Plumix.Material/ProgressIndicator.cs`: full `constraints` matrix, `year2023`, external `controller`, and adaptive Cupertino branch remain out of scope for this iteration.

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
  - add circular `strokeAlign` API + precedence + painter geometry parity and sync test/sample/docs coverage.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
- New tests to add:
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs` (expanded assertions)
- Parity-risk scenarios covered:
  - Default center-aligned stroke geometry and finite-value guards.
  - Theme + widget override precedence for `strokeAlign`.
  - Existing `trackGap`/`strokeCap` and semantics behavior remains intact.

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
