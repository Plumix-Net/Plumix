# Feature: material-2026-04-05-button-icon-alignment-directionality-parity

## Goal

- Align Material button icon-factory `IconAlignment.Start/End` behavior with Flutter directionality semantics (`LTR`/`RTL`) using ambient text direction.

## Non-Goals

- No text-scale-dependent icon-gap interpolation changes.
- No API changes to non-icon button constructors.
- No sample route/module updates.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter/Widgets/Directionality.cs` (new)
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
- Expansion trigger:
  - Expand only if additional Flutter references are needed to resolve start/end precedence under RTL.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned in `src/Flutter.Material`.
  - Core ambient directionality behavior remains framework-owned in `src/Flutter/Widgets`.
  - Flutter Dart behavior remains source of truth for icon alignment semantics.

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
  - None in this iteration for icon order; behavior now resolves against ambient directionality for start/end.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/Directionality.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Directionality.cs`: add ambient text-direction inherited widget.
  - `Buttons.cs`: resolve icon-row order by `Directionality.Of(context)`.
  - `MaterialButtonsTests.cs`: add RTL icon-order regressions.
  - docs/changelog: record parity increment and coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `TextButton_Icon_IconAlignmentStart_Rtl_PlacesLabelBeforeIcon`
  - `TextButton_Icon_IconAlignmentEnd_Rtl_PlacesIconBeforeLabel`
- Parity-risk scenarios covered:
  - `Start` and `End` icon-alignment modes swap icon/label order correctly under RTL.
  - Existing LTR icon-alignment tests remain green (no regression in previous behavior).

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
