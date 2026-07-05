# Feature: material-2026-04-05-button-m2-default-colors-elevation-parity

## Goal

- Align default M2 color/elevation behavior for framework Material buttons with Flutter source, with primary focus on `ElevatedButton` (`UseMaterial3=false`) and baseline checks for `TextButton`/`OutlinedButton` foreground defaults.

## Non-Goals

- No changes to `styleFrom(...)` API shape.
- No sample route/module updates.
- No broad M2 typography migration (still uses current framework label style baseline).

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
- Expansion trigger:
  - Expand to additional Flutter references only if outlined/text M2 default-value chain cannot be validated from current files.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned in `src/Flutter.Material`.
  - Dart source remains source of truth for mode-aware default behavior.
  - Button style composition order remains unchanged.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
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
  - `Buttons.cs`: make `ElevatedButton` M2 defaults mode-aware for foreground/background/elevation states.
  - `MaterialButtonsTests.cs`: add focused M2 regressions for elevated defaults plus baseline M2 foreground checks for text/outlined.
  - docs/changelog: record parity increment and coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `ElevatedButton_UseMaterial3Disabled_UsesThemePrimaryAndOnPrimaryColorsByDefault`
  - `ElevatedButton_DefaultElevation_UseMaterial3Disabled_UsesMaterial2StateMap`
  - `TextButton_UseMaterial3Disabled_UsesThemePrimaryColorAsDefaultForeground`
  - `OutlinedButton_UseMaterial3Disabled_DefaultForegroundUsesThemePrimaryColor`
- Parity-risk scenarios covered:
  - M2 elevated default enabled color pair (`primary` background + `onPrimary` foreground).
  - M2 elevated elevation state map (`default=2`, `hovered/focused=4`, `pressed=8`, `disabled=0`).
  - M2 text/outlined default foreground baseline remains `primary`.

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
