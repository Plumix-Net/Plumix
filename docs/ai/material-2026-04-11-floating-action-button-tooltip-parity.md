# Feature: material-2026-04-11-floating-action-button-tooltip-parity

## Goal

- Close the high-priority `FloatingActionButton` parity gap for tooltip wiring by adding Flutter-like `tooltip` API support and wrapping behavior in framework Material FAB composition.

## Non-Goals

- No `Hero` parity work in this iteration.
- No `mouseCursor` or `enableFeedback` parity in this iteration.
- No `clipBehavior` parity in this iteration.

## Context Budget Plan

- Budget: max 12 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/FloatingActionButton.cs`
  - `src/Flutter.Material/Tooltip.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/floating_action_button.dart`
- Expansion trigger:
  - Expand only to tracking docs and focused test updates required to close tooltip parity end-to-end.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `FloatingActionButton` tooltip API + runtime wrapper behavior.
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
  - Material behavior remains framework-owned in `src/Flutter.Material` with no Avalonia-control leakage.
  - FAB parity wiring stays in widget/render layers and keeps existing theme/state composition intact.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/floating_action_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - Remaining FAB parity gaps stay unchanged for this pass: `heroTag`, `mouseCursor`, `enableFeedback`, and `clipBehavior`.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/FloatingActionButton.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-11-floating-action-button-tooltip-parity.md`
- Brief intent per file:
  - add `tooltip` API surface for all FAB constructors and wrap built control with `Tooltip` when provided;
  - add focused FAB regression test for tooltip hover show/hide behavior;
  - update tracking docs and changelog for narrowed FAB divergence.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialFloatingActionButtonTests`
- New tests to add:
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`: tooltip hover show/hide coverage.
- Parity-risk scenarios covered:
  - Tooltip appears on hover enter and hides after hover exit animation completion.
  - Existing FAB visual/elevation behaviors remain unaffected.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (not needed in this pass because sample structure/routes did not change)

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
