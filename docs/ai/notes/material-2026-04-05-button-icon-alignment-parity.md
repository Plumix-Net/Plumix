# Feature: material-2026-04-05-button-icon-alignment-parity

## Goal

- Align Material button icon-factory icon-order behavior with Flutter by introducing `iconAlignment` support and style-level icon-alignment wiring.

## Non-Goals

- No text-scale-dependent spacing interpolation changes in this iteration.
- No text-direction-aware start/end remapping changes.
- No sample route/module updates.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
- Expansion trigger:
  - Expand only if per-button Flutter icon-factory precedence differs in a way not covered by current references.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned in `src/Flutter.Material`.
  - Flutter Dart behavior remains source of truth for button icon-factory defaults.
  - Existing button style composition/layering contracts remain intact.

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
  - `start/end` currently maps to fixed visual left/right order in framework icon row; text-direction-aware remapping is tracked as a follow-up.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `ButtonStyle.cs`: add `IconAlignment` enum/property + merge support.
  - `Buttons.cs`: thread `iconAlignment` through `styleFrom`, icon factories, and composed style precedence.
  - `MaterialButtonsTests.cs`: add icon-alignment regression coverage.
  - docs/changelog: record parity increment and tests.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `TextButton_Icon_IconAlignmentEnd_PlacesLabelBeforeIcon`
  - `TextButton_Icon_StyleFromIconAlignmentEnd_PlacesLabelBeforeIcon`
  - `TextButton_Icon_IconAlignmentParameter_OverridesStyleFromIconAlignment`
  - `FilledButtonTonal_Icon_IconAlignmentEnd_PlacesLabelBeforeIcon`
- Parity-risk scenarios covered:
  - Icon row order resolves to end-aligned mode when requested.
  - Style-level icon alignment is consumed by icon factories.
  - Explicit icon-factory alignment parameter has precedence over style-level value.

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
