# Feature: material-2026-04-04-text-outlined-stylefrom-disabled-color-parity

## Goal

- Align `TextButton`/`OutlinedButton` `styleFrom(...)` disabled-state color mapping with Flutter where specific fields (`backgroundColor`, and for text buttons `iconColor`) use all-state mapping when disabled override is omitted.

## Non-Goals

- No new button types or sample route changes.
- No cursor/feedback parity expansion in this iteration.
- No host/platform behavior changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Expand only if style-layer fallback interactions cannot be validated from current tests.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material style behavior remains framework-owned.
  - Style composition precedence remains unchanged.
  - No sample parity drift introduced.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
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
  - `Buttons.cs`: update `styleFrom(...)` mapping for text/outlined disabled-color special cases.
  - `MaterialButtonsTests.cs`: add disabled-state behavior assertions for icon/background cases.
  - tracking docs/changelog: record parity fix.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
  - `dotnet build src/Flutter.Net.sln -c Debug`
- New tests to add:
  - `TextButton_StyleFrom_IconColorWithoutDisabledIcon_UsesIconColorWhenDisabled`
  - `TextButton_StyleFrom_BackgroundOnly_AppliesBackgroundWhenDisabled`
  - `OutlinedButton_StyleFrom_BackgroundOnly_AppliesBackgroundWhenDisabled`
- Parity-risk scenarios covered:
  - disabled-state mapping for text button icon/background and outlined button background when only enabled color is supplied.

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
