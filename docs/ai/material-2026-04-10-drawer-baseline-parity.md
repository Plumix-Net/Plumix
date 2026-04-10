# Feature: material-2026-04-10-drawer-baseline-parity

## Goal

- Add baseline Material `Drawer` support to framework `Scaffold` with Flutter-like API surface and implied app-bar menu leading behavior.

## Non-Goals

- Full Flutter `DrawerController` animation/drag pipeline (`edgeDragWidth`, fling settle physics, drawer route semantics).
- `endDrawer` parity and drawer-theme object parity.

## Context Budget Plan

- Budget: max 18 files in initial read.
- Entry files:
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/scaffold.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/drawer.dart`
  - `docs/FRAMEWORK_PLAN.md`
- Expansion trigger:
  - Expand into docs/tracking files once drawer behavior and tests are stable.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Drawer` + `Scaffold.drawer` integration.
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
  - Framework behavior stays in `src/Flutter.Material`/`src/Flutter.Tests`.
  - `Widget -> Element -> RenderObject` composition boundaries preserved.
  - Navigation/app-shell implied-leading behavior remains framework-driven.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/scaffold.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/drawer.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/Scaffold.cs`: no `DrawerController` drag/animation pipeline yet; drawer opens/closes through state API and scrim tap dismissal only. Expected delta: no swipe-open/close and no animated transition. Follow-up condition: add `DrawerController`-equivalent primitive in Material shell phase.
  - `src/Flutter.Material/Scaffold.cs`: drawer shape/theme/end-drawer parity is intentionally deferred. Expected delta: baseline rectangular/start-drawer behavior only.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-10-drawer-baseline-parity.md`
- Brief intent per file:
  - `src/Flutter.Material/Scaffold.cs`: add `Drawer`, stateful `Scaffold`, scaffold scope, drawer-aware implied leading.
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`: add focused drawer behavior/regression tests.
  - docs: register shipped behavior and known follow-up divergences.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- New tests to add:
  - same file (`MaterialScaffoldTests`).
- Parity-risk scenarios covered:
  - default drawer width (`304`),
  - implied `menu` leading for `Scaffold.drawer`,
  - `ScaffoldState.OpenDrawer/CloseDrawer` visibility toggles,
  - no-op open path when drawer is absent.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [ ] `docs/ai/PARITY_MATRIX.md` updated (if needed)

Notes:
- No sample route/page behavior was changed in this iteration, so parity matrix update is not required.

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
