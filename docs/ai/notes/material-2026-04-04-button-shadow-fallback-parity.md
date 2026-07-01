# Feature: material-2026-04-04-button-shadow-fallback-parity

## Goal

- Align Material button shadow behavior with Flutter fallback semantics so style-level `elevation` still produces shadows when `shadowColor` is omitted, by using `ThemeData.ShadowColor`.

## Non-Goals

- No API shape changes for button constructors in this iteration.
- No sample route changes.
- No host/platform or app-bar updates.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/material.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
- Expansion trigger:
  - Expand only if shadow fallback precedence against style/theme layers cannot be validated with current tests.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework-owned Material behavior remains inside `src/Flutter.Material`.
  - Style resolution precedence remains unchanged; fallback applies only when style layers do not supply shadow color.
  - No sample parity divergence introduced.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/material.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/filled_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - None for this fallback behavior increment.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Buttons.cs`: resolve shadow color with fallback to theme when elevation is active and style does not specify shadow color.
  - `MaterialButtonsTests.cs`: add focused tests for fallback path on Text/Outlined/Filled buttons.
  - tracking docs/changelog: record parity step.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
  - `dotnet build src/Flutter.Net.sln -c Debug`
- New tests to add:
  - `TextButton_StyleFrom_ElevationWithoutShadowColor_UsesThemeShadowColorFallback`
  - `OutlinedButton_StyleFrom_ElevationWithoutShadowColor_UsesThemeShadowColorFallback`
  - `FilledButton_StyleFrom_ElevationWithoutShadowColor_UsesThemeShadowColorFallback`
- Parity-risk scenarios covered:
  - Positive style-level elevation with unresolved shadow color still paints elevation shadow.

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
