# Feature: material-2026-04-04-button-icon-theme-parity

## Goal

- Align framework Material button behavior with Flutter `ButtonStyleButton` icon-theme semantics by propagating resolved button icon color/size into button subtrees (`IconTheme`) and extending `styleFrom(...)` APIs with icon-specific overrides.

## Non-Goals

- No new Material controls or route/sample additions.
- No AppBar/system-bar changes in this iteration.
- No cross-host Android/iOS toolchain remediation.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter/Widgets/IconTheme.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
- Expansion trigger:
  - Expand only if button icon-theme resolution order or style-layer fallback behavior cannot be validated from these files.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned in `src/Flutter.Material` and core widget primitives.
  - Button style/state resolution remains parity-first to Flutter source behavior.
  - No sample route/module divergence introduced.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/filled_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - None.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `ButtonStyle.cs`: add state-aware `iconColor`/`iconSize` properties and merge/resolve support.
  - `Buttons.cs`: wire icon properties through style composition, add icon arguments to `styleFrom(...)`, align default icon tokens, and apply resolved icon theme in `MaterialButtonCore`.
  - `MaterialButtonsTests.cs`: add coverage for default icon-theme propagation and `styleFrom(...)` icon overrides (enabled/disabled).
  - tracking docs/changelog: record delivered parity increment.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
  - `dotnet build src/Flutter.Net.sln -c Debug`
- New tests to add:
  - `TextButton_DefaultIconTheme_UsesForegroundAndIconSizeDefaults`
  - `TextButton_StyleFrom_IconColorAndSizeOverrideDefaults`
  - `TextButton_StyleFrom_DisabledIconColorOverridesForegroundFallback`
- Parity-risk scenarios covered:
  - Icon color/size inheritance in button subtree from composed style defaults.
  - `styleFrom(...)` icon overrides for enabled and disabled states.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
