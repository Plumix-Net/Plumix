# Feature: material-radio-adaptive-cupertino-parity

## Goal

- Close the remaining `Radio.adaptive` divergence by introducing a Cupertino adaptive path for iOS/macOS with parity-critical defaults and focused regression coverage.

## Non-Goals

- Full Flutter `CupertinoRadio` fidelity matrix (haptics, accessibility labels, and all motion nuances).
- Sample route/module expansion for a dedicated adaptive radio page in this iteration.

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Radio.cs`
  - `src/Flutter.Cupertino/CupertinoCheckbox.cs`
  - `src/Flutter.Tests/MaterialRadioTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/radio.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/cupertino/radio.dart`
- Expansion trigger:
  - add a dedicated Cupertino radio primitive if adaptive behavior cannot be cleanly closed inside Material-only composition.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Radio` (`Radio.Adaptive` iOS/macOS path)
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
  - Framework control behavior remains in framework libraries (`src/Flutter.Material`, `src/Flutter.Cupertino`) without host-side platform branching logic.
  - Flutter Dart source remains source-of-truth for adaptive parameter behavior and platform split (`ThemeData.Platform`).

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/radio.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/cupertino/radio.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Cupertino/CupertinoRadio.cs`: current scope does not implement full native Cupertino fidelity details (haptics/accessibility labels/advanced motion choreography). Follow-up condition: extend Cupertino radio internals during later M4/M5 fidelity passes.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Radio.cs`
  - `src/Flutter.Cupertino/CupertinoRadio.cs`
  - `src/Flutter.Tests/MaterialRadioTests.cs`
  - `src/Sample/Flutter.Net/Demos/Cupertino/RadioDemoPage.cs`
  - `dart_sample/lib/demos/cupertino/radio_demo_page.dart`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/material-2026-04-05-radio-adaptive-cupertino-parity.md`
- Brief intent per file:
  - Material/Cupertino files: add adaptive platform split and Cupertino radio primitive.
  - Tests: add focused adaptive parity checks.
  - docs/changelog: close the documented divergence and update residual gaps.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialRadioTests.cs`
- New tests to add:
  - `src/Flutter.Tests/MaterialRadioTests.cs`
- Parity-risk scenarios covered:
  - adaptive iOS default visual tokens and geometry,
  - adaptive ignored Material-only parameter behavior (`fillColor`),
  - adaptive Cupertino checkmark-style indicator branch,
  - adaptive macOS visual-width parity (`18x18`).

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

Notes:
- No sample route/module structure changes were required in this iteration.
- C#/Dart `Radio` demo page content was extended with adaptive runtime probes (platform/style toggles and `Radio.adaptive` rows) to validate the shipped adaptive control behavior.

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
