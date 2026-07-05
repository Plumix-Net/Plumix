# Feature: material-2026-04-05-outlined-filled-stylefrom-background-mapping-parity

## Goal

- Align `OutlinedButton.styleFrom(...)` and `FilledButton.styleFrom(...)` background-color mapping with Flutter source semantics for disabled-state behavior.

## Non-Goals

- No new public APIs for Material buttons.
- No changes to button layout, animation, or ripple behavior.
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
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/filled_button.dart`
- Expansion trigger:
  - Expand only if style-layer fallback behavior cannot be validated through existing button tests.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Dart source remains source of truth for Material control behavior.
  - Button behavior remains framework-owned in `src/Flutter.Material`.
  - Style composition order remains `default -> theme -> widget -> legacy`.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/filled_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - None for this mapping correction.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Buttons.cs`: apply outlined all-state special-case, remove filled all-state special-case.
  - `MaterialButtonsTests.cs`: add targeted regression tests for outlined theme-override and filled disabled fallback behavior.
  - tracking docs/changelog: capture parity increment and test coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `OutlinedButton_StyleFrom_BackgroundOnly_OverridesThemeDisabledBackground`
  - `FilledButton_StyleFrom_BackgroundOnly_DisabledFallsBackToThemeDisabledBackground`
- Parity-risk scenarios covered:
  - Outlined background-only style must still apply for disabled state even when theme supplies a disabled background.
  - Filled background-only style must not override disabled-state fallback when `disabledBackgroundColor` is absent.

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
