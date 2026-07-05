# Feature: material-2026-05-08-circular-progress-adaptive-semantics-parity

## Goal

- Close adaptive semantics parity for `CircularProgressIndicator` so iOS/macOS Cupertino branch preserves the same progress semantics label/value behavior as the Material branch.

## Non-Goals

- Full `Animation<Color?>` parity for `valueColor` (framework still uses `ValueNotifier<Color?>`).
- Any visual changes to Cupertino spinner paint/geometry.

## Context Budget Plan

- Budget: max 12 files in initial read.
- Entry files:
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
- Expansion trigger:
  - Expand only if semantics plumbing is missing in framework core wrappers.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `CircularProgressIndicator.adaptive`
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
  - Adaptive behavior remains in framework widgets, not host adapters.
  - Semantics parity must be preserved across adaptive and non-adaptive branches.

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
  - None added in this iteration.

## Planned Changes

- Files to edit:
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
- Brief intent per file:
  - `src/Plumix.Material/ProgressIndicator.cs`: preserve semantics wrapping for adaptive Cupertino path and keep label/value fallback behavior shared with Material path.
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`: add regression test to lock adaptive iOS semantics label + computed percent behavior.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
- New tests to add:
  - `CircularProgressIndicator_AdaptiveIOS_SemanticsLabel_IncludesComputedPercentForDeterminateValue`
- Parity-risk scenarios covered:
  - Adaptive early-return branch no longer bypasses semantics metadata for assistive tooling.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [ ] `docs/ai/PARITY_MATRIX.md` updated (if needed)

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
