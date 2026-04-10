# Feature: material-switch-baseline-parity

## Goal

- Add framework Material `Switch` parity baseline so C# control rewrites can depend on a built-in on/off toggle with Flutter-like API/default/state behavior for current framework scope.

## Non-Goals

- Full Cupertino-native adaptive switch rendering parity (adaptive path currently falls back to Material rendering because Cupertino switch primitives are not yet implemented).
- Switch image-thumb support and full pointer-state thumb-size choreography from Flutter painter internals.

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Checkbox.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/switch.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/switch_theme.dart`
- Expansion trigger:
  - Add/update framework primitives required to close switch parity in one request (`SwitchTheme`, `ThemeData.SwitchTheme`, sample parity route/page, focused switch tests).

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Switch`
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
  - Framework control behavior remains in `src/Flutter.Material` without host-side switch logic.
  - Shared interaction primitives remain centralized in `MaterialButtonCore` and gesture/focus layers.
  - Dart source remains source-of-truth for switch API/default precedence within available framework primitives.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/switch.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/switch_theme.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/toggleable.dart`
  - `dart_sample/lib/demos/cupertino/switch_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/Switch.cs`: adaptive constructor currently uses Material rendering path on iOS/macOS because Cupertino switch primitives are not yet available. Expected delta: adaptive switch is not platform-native on Cupertino targets. Follow-up condition: add Cupertino switch primitives and route adaptive branch accordingly.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Switch.cs`
  - `src/Flutter.Material/SwitchTheme.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
  - `src/Sample/Flutter.Net/Demos/Cupertino/SwitchDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/cupertino/switch_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - Material files: add switch control + switch theme integration with Flutter-like precedence and baseline interaction behavior.
  - Test file: add focused switch coverage for defaults, precedence, tap-target, keyboard, and icon rendering.
  - Sample files: add C#/Dart demo page parity route for runtime verification.
  - docs/changelog: update M4 tracking and documented adaptive divergence.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
- New tests to add:
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
- Parity-risk scenarios covered:
  - selected/unselected/disabled visual defaults for track+thumb,
  - widget vs theme precedence for thumb/track/outline properties,
  - thumb-icon rendering in selected state,
  - keyboard activation and tap-target behavior.

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
