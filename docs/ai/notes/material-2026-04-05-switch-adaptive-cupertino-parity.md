# Feature: material-switch-adaptive-cupertino-parity

## Goal

- Close the documented `Switch.Adaptive` iOS/macOS fallback gap by introducing a Cupertino-like adaptive path in framework scope with parity-critical platform mapping/defaults/tests.

## Non-Goals

- Full native `CupertinoSwitch` painter parity (press/drag choreography, painter internals, and all motion nuances).
- `Checkbox.Adaptive` Cupertino-native implementation (tracked separately).

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Switch.cs`
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/switch.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/cupertino/switch.dart`
- Expansion trigger:
  - verify adaptive-platform semantics and coverage updates in docs/changelog after landing control behavior and tests.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Switch` (`Switch.Adaptive` iOS/macOS path)
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
  - Framework control behavior remains in `src/Flutter.Material` (no host-side adaptive switch logic).
  - Adaptive platform behavior follows Flutter Dart source as the source of truth for mapping/default precedence.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/switch.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/cupertino/switch.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/Switch.cs`: adaptive iOS/macOS now uses Cupertino-like defaults and platform mapping, but still renders through shared `MaterialButtonCore` composition instead of dedicated Cupertino painter/motion internals. Expected delta: drag/press motion nuances are still simplified versus Flutter native Cupertino implementation. Follow-up condition: add dedicated Cupertino switch primitives/painter in framework scope.
  - Follow-up status (2026-04-05): closed by `docs/ai/material-2026-04-05-switch-adaptive-cupertino-interaction-closeout.md` (adaptive path no longer uses `MaterialButtonCore`; Cupertino drag commit/reverse thresholds were implemented).

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Switch.cs`
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-05-switch-adaptive-cupertino-parity.md`
- Brief intent per file:
  - `Switch.cs`: adaptive platform branching + defaults/mapping/opacity updates.
  - `MaterialSwitchTests.cs`: focused adaptive behavior coverage.
  - docs/changelog: record shipped status and remaining divergence.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
- New tests to add:
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
- Parity-risk scenarios covered:
  - adaptive `activeColor` mapping by platform,
  - adaptive iOS disabled opacity behavior,
  - adaptive iOS Cupertino geometry defaults.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

Notes:
- No sample route/module structure changes were required in this iteration.

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
