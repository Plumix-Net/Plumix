# Feature: material-2026-04-04-button-tap-target-size-parity

## Goal

- Align framework Material button tap-target sizing with Flutter `ButtonStyleButton` behavior by supporting style/theme-driven tap-target modes (`padded` vs `shrinkWrap`) instead of always forcing `48x48`.

## Non-Goals

- No new Material controls or sample routes.
- No visual-density implementation in this iteration.
- No app-bar/system-bars or host/toolchain changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
- Expansion trigger:
  - Expand only if tap-target precedence (`style -> theme -> default`) cannot be validated from these files.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned in `src/Flutter.Material`.
  - Button style resolution remains parity-first and layered (`default -> theme -> widget -> legacy`).
  - No sample route/module divergence introduced.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style.dart`
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
  - Visual-density adjustment on tap-target size is intentionally deferred; this iteration only ports `padded` vs `shrinkWrap` mode selection.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `ButtonStyle.cs`: add `TapTargetSize` and merge/resolve wiring.
  - `ThemeData.cs`: add `MaterialTapTargetSize` with `Padded` default.
  - `Buttons.cs`: extend `styleFrom(...)`, default styles, and tap-target wrapper size resolution.
  - `MaterialButtonsTests.cs`: add theme default, theme shrink-wrap, and `styleFrom` precedence tests.
  - tracking docs/changelog: record shipped parity increment.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
  - `dotnet build src/Flutter.Net.sln -c Debug`
- New tests to add:
  - `ThemeData_Light_DefaultMaterialTapTargetSize_IsPadded`
  - `TextButton_ThemeMaterialTapTargetSizeShrinkWrap_DoesNotExpandTapTarget`
  - `TextButton_StyleFromTapTargetSize_OverridesThemeTapTargetSize`
- Parity-risk scenarios covered:
  - Default theme tap-target policy.
  - Theme-level tap-target shrink-wrap behavior.
  - Widget-level `styleFrom` precedence over theme defaults.

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
