# Feature: material-switch-adaptive-cupertino-interaction-closeout

## Goal

- Close the remaining `Switch.Adaptive` interaction divergence on iOS/macOS by replacing shared Material button composition with Cupertino-style adaptive interaction flow and drag thresholds.

## Non-Goals

- Full Flutter Cupertino painter parity (on/off labels, thumb images, full haptic pipeline).
- Sample route/module restructuring.

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Switch.cs`
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/cupertino/switch.dart`
- Expansion trigger:
  - update tracking docs/changelog after landing adaptive interaction behavior + focused tests.

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
  - Framework switch behavior remains in framework libraries (`src/Flutter.Material`) with no host-side adaptive special-casing.
  - Pointer/gesture routing remains through `GestureBinding` + recognizers.

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
  - `src/Flutter.Material/Switch.cs`: adaptive switch still omits Cupertino haptics/labels/thumb-image pipeline (`HapticFeedback.lightImpact`, on/off labels, image thumb assets). Expected delta: visual/interaction core parity is present, but those advanced Cupertino extras are absent.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Switch.cs`
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-05-switch-adaptive-cupertino-interaction-closeout.md`
- Brief intent per file:
  - `Switch.cs`: adaptive branch composition + drag-threshold choreography + pressed-thumb behavior.
  - `MaterialSwitchTests.cs`: pointer-driven adaptive drag threshold tests.
  - docs/changelog: capture shipped behavior and narrowed remaining divergence.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
- New tests to add:
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
- Parity-risk scenarios covered:
  - adaptive drag commit threshold (`0.7`),
  - adaptive reverse threshold (`0.2`),
  - adaptive non-material composition retaining existing geometry/token behavior.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

Notes:
- No sample structure changes were required in this iteration.

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
