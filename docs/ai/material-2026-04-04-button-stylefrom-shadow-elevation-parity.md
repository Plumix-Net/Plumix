# Feature: material-2026-04-04-button-stylefrom-shadow-elevation-parity

## Goal

- Expand Material button `styleFrom(...)` parity by adding `shadowColor` and `elevation` support for `TextButton`, `OutlinedButton`, and `FilledButton`, matching Flutter helper API shape and behavior.

## Non-Goals

- No cursor/feedback/visual-density parity in this iteration.
- No app-bar/system-bar/host integration changes.
- No sample route or Dart-sample updates required.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/filled_button.dart`
- Expansion trigger:
  - Expand only if style-layer precedence cannot be validated from existing button tests.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior stays in framework layer (`src/Flutter.Material`).
  - Button style layering remains unchanged (`default -> theme -> widget -> legacy`).
  - No sample module/route divergence introduced.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/filled_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - None for this scoped API addition.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Buttons.cs`: add `shadowColor`/`elevation` args to `styleFrom(...)` for Text/Outlined/Filled and map them into `ButtonStyle`.
  - `MaterialButtonsTests.cs`: add focused assertions that style overrides produce shadow layers on those button types.
  - tracking docs/changelog: record parity increment.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
  - `dotnet build src/Flutter.Net.sln -c Debug`
- New tests to add:
  - `TextButton_StyleFrom_ElevationAndShadowColor_AppliesShadow`
  - `OutlinedButton_StyleFrom_ElevationAndShadowColor_AppliesShadow`
  - `FilledButton_StyleFrom_ElevationAndShadowColor_AppliesShadow`
- Parity-risk scenarios covered:
  - Non-elevated button types opt into explicit shadow/elevation via style helper API.

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
