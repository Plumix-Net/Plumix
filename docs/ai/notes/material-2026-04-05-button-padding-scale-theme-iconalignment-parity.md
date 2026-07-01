# Feature: material-2026-04-05-button-padding-scale-theme-iconalignment-parity

## Goal

- Close remaining high-impact Material button parity gaps for text-scale-aware default paddings and icon-alignment precedence to unblock moving to the next Material control.

## Non-Goals

- No expansion of button API surface beyond current project scope (mouse cursor, visual density class, builders, etc.).
- No sample route additions.

## Context Budget Plan

- Budget: max 10 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter/Widgets/MediaQuery.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/{text_button,elevated_button,outlined_button,filled_button}.dart`
- Expansion trigger:
  - Expand only if additional Flutter references are required for default/icon padding scaling semantics.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned in `src/Flutter.Material`.
  - Ambient media metrics remain framework-owned in `src/Flutter/Widgets`.
  - Flutter Dart source remains source of truth for default/icon padding and icon-alignment precedence semantics.

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
  - Current framework keeps linear `TextScaleFactor` (`MediaQueryData.TextScaleFactor`) rather than Flutter's full `TextScaler` strategy object; this is accepted in current project scope, with button padding/spacing formulas mapped to the same multiplier semantics.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Buttons.cs`: apply Flutter `scaledPadding` piecewise interpolation to all button defaults (including icon variants), add direction-aware start/end padding mapping, and align icon-factory `iconAlignment` precedence with theme-level styles.
  - `MaterialButtonsTests.cs`: add focused coverage for scaled paddings, directional icon paddings (LTR/RTL), and theme-level icon-alignment precedence across button variants.
  - docs/changelog: record close-out progress for button parity block.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - TextButton scaled/default/icon padding + RTL directional checks.
  - Elevated/Outlined/Filled scaled/default/icon padding + directional checks.
  - Theme-level `iconAlignment` precedence checks for text/elevated/outlined/filled icon factories.
- Parity-risk scenarios covered:
  - Piecewise interpolation for `scaledPadding` (`1x`, `1-2`, `2-3`, `3+`) mirrors Flutter semantics.
  - Icon padding start/end values honor `Directionality` and swap under RTL where directional insets are used by Flutter.
  - Icon alignment precedence follows `iconAlignment arg -> theme style -> button style -> start`.

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
- [x] Remaining parity gaps (if any) are documented with blocker + next action
