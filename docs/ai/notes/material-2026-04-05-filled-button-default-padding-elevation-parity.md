# Feature: material-2026-04-05-filled-button-default-padding-elevation-parity

## Goal

- Align `FilledButton` default padding and elevation behavior with Flutter defaults, including mode-aware horizontal padding and hovered elevation.

## Non-Goals

- No changes to `FilledButton.styleFrom(...)` API.
- No sample route/module updates.
- No changes to non-default theme/widget style precedence behavior.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/filled_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
- Expansion trigger:
  - Expand only if tonal variant defaults cannot be validated through current references.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned in `src/Flutter.Material`.
  - Dart source remains source of truth for default behavior.
  - Button style composition/layering contracts remain unchanged.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/filled_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
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
  - `Buttons.cs`: update default `FilledButton` style with mode-aware horizontal padding and hovered elevation map.
  - `MaterialButtonsTests.cs`: add focused regressions for filled/tonal M2 padding and filled default hovered elevation.
  - docs/changelog: record parity increment and new coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `FilledButton_DefaultPadding_UseMaterial3Disabled_UsesHorizontal16AndZeroVertical`
  - `FilledButtonTonal_DefaultPadding_UseMaterial3Disabled_UsesHorizontal16AndZeroVertical`
  - `FilledButton_DefaultElevation_Hovered_UsesOneAndDefaultUsesZero`
- Parity-risk scenarios covered:
  - `FilledButton`/`FilledButton.Tonal` default horizontal padding follows `UseMaterial3` (`24` vs `16`).
  - default filled elevation resolves `hovered=1`, default `0`.

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
