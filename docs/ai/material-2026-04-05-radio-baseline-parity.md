# Feature: material-radio-baseline-parity

## Goal

- Add framework Material `Radio` parity baseline so C# control rewrites can rely on a built-in mutually-exclusive selection control with Flutter-like API/default/state behavior for current framework scope.

## Non-Goals

- Full Cupertino-native adaptive radio fidelity matrix (haptics/accessibility labels/advanced motion nuances).
- Full Flutter `RadioThemeData` surface (`mouseCursor`, visual-density, and stateful border-side/inner-radius property types beyond current framework primitives).

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Checkbox.cs`
  - `src/Flutter.Material/Switch.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/radio.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/radio_theme.dart`
- Expansion trigger:
  - Add/update framework primitives required to close radio parity in one request (`RadioTheme`, `ThemeData.RadioTheme`, sample parity route/page, focused radio tests).

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Radio`
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
  - Framework control behavior remains in `src/Flutter.Material` without host-side radio logic.
  - Shared interaction primitives remain centralized in `MaterialButtonCore` and focus/input layers.
  - Dart source remains source-of-truth for radio API/default precedence within available framework primitives.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/radio.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/radio_theme.dart`
  - `dart_sample/lib/radio_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - Baseline note: at the time of this iteration, `Radio.adaptive` Cupertino path was not yet implemented.
  - Follow-up status (2026-04-05): closed by `docs/ai/material-2026-04-05-radio-adaptive-cupertino-parity.md` (`CupertinoRadio<T>` + `Radio<T>.Adaptive(...)` platform split).

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Radio.cs`
  - `src/Flutter.Material/RadioTheme.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialRadioTests.cs`
  - `src/Sample/Flutter.Net/RadioDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/radio_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - Material files: add radio control + radio theme integration with Flutter-like precedence and baseline interaction behavior.
  - Test file: add focused radio coverage for defaults, precedence, toggleable transitions, keyboard, and tap-target behavior.
  - Sample files: add C#/Dart demo page parity route for runtime verification.
  - docs/changelog: update M4 tracking and documented adaptive divergence.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialRadioTests.cs`
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
- New tests to add:
  - `src/Flutter.Tests/MaterialRadioTests.cs`
- Parity-risk scenarios covered:
  - selected/unselected/disabled visual defaults for fill/border/dot,
  - widget-vs-theme precedence for fill-color resolution,
  - toggleable selected->null transition behavior,
  - keyboard activation and tap-target policy.

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
