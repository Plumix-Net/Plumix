# Feature: material-2026-05-03-drawer-alternating-drag-stress

## Goal

- Add focused stress coverage for rapid alternating drawer drags so `Scaffold` preserves start/end mutual exclusion and stable visibility after settle.

## Non-Goals

- No runtime behavior changes in `Scaffold`.
- No sample parity route changes.

## Context Budget Plan

- Budget: max 6 files in initial read.
- Entry files:
  - `src/Plumix.Material/Scaffold.cs`
  - `src/Plumix.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Add tracking note + docs updates only after green test validation.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Scaffold` drawer gesture interop (alternating start/end drag choreography).
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
  - Drawer state must remain mutually exclusive (`start` xor `end`) across gesture-driven transitions.
  - Framework behavior remains in framework/test layers; no host-specific behavior changes.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `flutter/packages/flutter/lib/src/material/scaffold.dart`
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
  - `docs/ai/material-2026-05-03-drawer-alternating-drag-stress.md`
- Brief intent per file:
  - `MaterialScaffoldTests.cs`: add alternating start/end drag stress scenarios with settle-step assertions.
  - tracking docs: record shipped coverage and refined residual risk wording.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Plumix.Tests/Plumix.Tests.csproj -c Debug --filter FullyQualifiedName~MaterialScaffoldTests`
  - `dotnet test src/Plumix.Tests/Plumix.Tests.csproj -c Debug`
- New tests to add:
  - `Scaffold_AlternatingDrawerDrags_StartThenEnd_KeepSingleDrawerVisible`
  - `Scaffold_AlternatingDrawerDrags_EndThenStart_KeepSingleDrawerVisible`
- Parity-risk scenarios covered:
  - rapid alternating edge-open + panel-close choreography on both sides;
  - mutual-exclusion and visibility consistency after each animation settle.

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
