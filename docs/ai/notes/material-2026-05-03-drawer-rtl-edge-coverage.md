# Feature: material-2026-05-03-drawer-rtl-edge-coverage

## Goal

- Close the remaining high-signal drawer parity risk for RTL by adding explicit regression coverage for edge-open gestures and edge activation width behavior with `MediaQuery.padding`.

## Non-Goals

- No framework behavior changes in `Scaffold` drawer physics or animation code.
- No sample route/module changes in this pass.

## Context Budget Plan

- Budget: max 10 files in initial read.
- Entry files:
  - `src/Plumix.Material/Scaffold.cs`
  - `src/Plumix.Tests/MaterialScaffoldTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/PARITY_MATRIX.md`
- Expansion trigger:
  - Expand to changelog + feature-note updates after test scenarios are added and verified.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Scaffold` drawer edge gesture behavior in RTL.
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
  - Framework behavior stays inside framework libraries; this pass only extends regression coverage.
  - Material drawer parity checks should exercise both LTR and RTL edge-activation paths.

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
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/material-2026-05-03-drawer-rtl-edge-coverage.md`
- Brief intent per file:
  - `MaterialScaffoldTests.cs`: add explicit RTL edge-open and RTL media-padding activation tests for start/end drawers.
  - tracking docs: record shipped coverage and narrow remaining drawer-risk wording.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Plumix.Tests/Plumix.Tests.csproj -c Debug --filter "FullyQualifiedName~MaterialScaffoldTests.Scaffold_EdgeDrag"`
  - `dotnet test src/Plumix.Tests/Plumix.Tests.csproj -c Debug --filter "FullyQualifiedName~MaterialScaffoldTests"`
- New tests to add:
  - RTL start-drawer edge-open drag.
  - RTL end-drawer edge-open drag.
  - RTL start-drawer activation width with right-side `MediaQuery.padding`.
  - RTL end-drawer activation width with left-side `MediaQuery.padding`.
- Parity-risk scenarios covered:
  - RTL start/end edge direction mapping (`start` on right, `end` on left).
  - RTL edge width extension by safe-area padding on the opening edge.

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
