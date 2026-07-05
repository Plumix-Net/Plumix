# Feature: material-2026-05-03-drawer-interaction-gating-coverage

## Goal

- Close the next drawer parity-risk slice by adding explicit regression coverage for interaction gating paths in `Scaffold` (scrim dismissibility, drag-enable flags, and desktop drag suppression).

## Non-Goals

- No `Scaffold` runtime behavior changes in this pass.
- No sample route/module updates.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Plumix.Material/Scaffold.cs`
  - `src/Plumix.Tests/MaterialScaffoldTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Expansion trigger:
  - Expand to tracking docs only after targeted tests are added and validated.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Scaffold` drawer interaction gating behavior.
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
  - Framework behavior remains in framework/test layers; this pass extends regression coverage only.
  - Drawer interaction parity must include both permissive and restrictive interaction branches.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `flutter/packages/flutter/lib/src/material/scaffold.dart`
  - `dart_sample/lib/demos/material/drawer_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - none

## Planned Changes

- Files to edit:
  - `src/Plumix.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-05-03-drawer-interaction-gating-coverage.md`
- Brief intent per file:
  - `MaterialScaffoldTests.cs`: add focused tests for scrim dismissibility and gesture-enable gating branches.
  - tracking docs: capture shipped test coverage and narrow remaining risk wording.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Plumix.Tests/Plumix.Tests.csproj -c Debug --filter FullyQualifiedName~MaterialScaffoldTests`
- New tests to add:
  - scrim tap closes open drawer when `drawerBarrierDismissible=true`;
  - scrim tap does not close drawer when `drawerBarrierDismissible=false`;
  - edge drag does not open start drawer when `drawerEnableOpenDragGesture=false`;
  - edge drag does not open end drawer when `endDrawerEnableOpenDragGesture=false`;
  - edge drag does not open drawer on desktop platform.
- Parity-risk scenarios covered:
  - dismissible vs non-dismissible overlay behavior;
  - gesture-gating flags for start/end drawer entry;
  - platform-based suppression of mobile edge-gesture affordances.

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
