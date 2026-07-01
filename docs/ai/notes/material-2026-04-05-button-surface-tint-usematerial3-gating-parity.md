# Feature: material-2026-04-05-button-surface-tint-usematerial3-gating-parity

## Goal

- Align button surface-tint behavior with Flutter by ensuring `surfaceTintColor` is applied only when `ThemeData.UseMaterial3` is enabled.

## Non-Goals

- No new button APIs in this step.
- No app-bar/system-bar changes.
- No sample route/module changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/material.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
- Expansion trigger:
  - Expand only if mode-aware tint behavior cannot be validated from local button tests.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material button visual behavior remains framework-owned.
  - Style composition order is unchanged.
  - No sample parity drift introduced.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/material.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - None for this mode gating adjustment.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Buttons.cs`: gate surface-tint application on `ThemeData.UseMaterial3`.
  - `MaterialButtonsTests.cs`: add M2-mode assertions where tint is not applied.
  - tracking docs/changelog: record parity increment.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
  - `dotnet build src/Flutter.Net.sln -c Debug`
- New tests to add:
  - `ElevatedButton_StyleFrom_SurfaceTintColor_DoesNotTintBackground_WhenUseMaterial3IsDisabled`
  - `ElevatedButton_ThemeStyleSurfaceTintColor_DoesNotTintBackground_WhenUseMaterial3IsDisabled`
- Parity-risk scenarios covered:
  - M2 mode (`UseMaterial3=false`) disables tint overlay despite style/theme tint inputs.

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
