# Feature: material-2026-04-05-button-icon-factory-padding-parity

## Goal

- Align Material button icon-factory API surface and default padding behavior with Flutter, including mode-aware M2/M3 defaults.

## Non-Goals

- No changes to `styleFrom(...)` parameter surface.
- No sample route/module updates.
- No changes to non-default style/theme precedence rules.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
- Expansion trigger:
  - Expand only if filled/outlined icon-factory padding mapping cannot be validated with current references.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned in `src/Flutter.Material`.
  - Flutter Dart defaults remain source of truth for API/default values.
  - Existing button style composition/layering contracts remain unchanged.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
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
  - None in this iteration.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Buttons.cs`: add icon factory methods and mode-aware icon-factory default padding across button types.
  - `MaterialButtonsTests.cs`: add focused regressions for icon-factory default padding in M3/M2.
  - docs/changelog: record parity increment and coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `TextButton_Icon_DefaultPadding_UsesStart12TopBottom8End16`
  - `TextButton_Icon_DefaultPadding_UseMaterial3Disabled_UsesAll8`
  - `ElevatedButton_Icon_DefaultPadding_UsesStart16AndEnd24`
  - `ElevatedButton_Icon_DefaultPadding_UseMaterial3Disabled_UsesStart12AndEnd16`
  - `OutlinedButton_Icon_DefaultPadding_UsesStart16AndEnd24`
  - `OutlinedButton_Icon_DefaultPadding_UseMaterial3Disabled_UsesHorizontal16AndZeroVertical`
  - `FilledButton_Icon_DefaultPadding_UsesStart16AndEnd24`
  - `FilledButton_Icon_DefaultPadding_UseMaterial3Disabled_UsesStart12AndEnd16`
  - `FilledButtonTonal_Icon_DefaultPadding_UseMaterial3Disabled_UsesStart12AndEnd16`
- Parity-risk scenarios covered:
  - Icon-factory defaults respect `UseMaterial3` splits and per-button-type padding differences.
  - `OutlinedButton.Icon` M2 behavior preserves non-icon default padding (no extra asymmetry in M2).

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
