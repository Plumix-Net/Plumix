# Feature: material-2026-04-05-outlined-button-m2-border-focus-parity

## Goal

- Align `OutlinedButton` default border-side behavior with Flutter for `UseMaterial3=false`, where focused and unfocused enabled states keep the same `onSurface(0.12)` border color.

## Non-Goals

- No changes to `OutlinedButton.styleFrom(...)` argument surface.
- No changes to elevated/text/filled default geometry tokens in this step.
- No sample route/module updates.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
- Expansion trigger:
  - Expand only if focused-state resolution cannot be verified through existing button focus/state test harness.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material control behavior remains framework-owned in `src/Flutter.Material`.
  - Dart source remains source of truth for mode-aware default behavior.
  - Style composition/layering contracts remain unchanged.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - None for this focused-border mode split.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Buttons.cs`: make outlined default side resolver mode-aware for M2 vs M3 focused state.
  - `MaterialButtonsTests.cs`: add focused and non-focused M2 border regressions.
  - docs/changelog: record this parity increment.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `OutlinedButton_DefaultBorder_UseMaterial3Disabled_UsesOnSurfaceOpacity`
  - `OutlinedButton_FocusedBorder_UseMaterial3Disabled_StaysOnSurfaceOpacity`
- Parity-risk scenarios covered:
  - M2 focused outlined border must not switch to primary accent.
  - M2 outlined enabled default border must follow `onSurface` opacity path even when `OutlineColor` differs.

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
