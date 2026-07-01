# Feature: material-2026-04-05-button-icon-spacing-textscale-parity

## Goal

- Align Material button icon-factory gap behavior with Flutter text-scale semantics (`spacing` interpolates from `8` to `4` as effective text scale moves from `1.0` to `2.0`).

## Non-Goals

- No changes to non-icon button composition.
- No additional app/sample routes.
- No full `TextScaler` implementation beyond current linear `MediaQuery` text scale factor support needed for this parity step.

## Context Budget Plan

- Budget: max 9 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter/Widgets/MediaQuery.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
- Expansion trigger:
  - Expand only if additional Flutter references are required to verify icon-gap interpolation formula/limits.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior stays framework-owned in `src/Flutter.Material`.
  - Ambient media metrics remain framework-owned in `src/Flutter/Widgets`.
  - Flutter Dart source remains parity source of truth for icon-factory scaling behavior.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
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
  - No divergence for icon-gap formula in this iteration; spacing now follows Flutter clamp/interpolation path (`8 -> 4`, `scale 1..2`).

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/MediaQuery.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `MediaQuery.cs`: add `TextScaleFactor` to ambient media data and lookup helpers.
  - `Buttons.cs`: apply Flutter-like icon-gap interpolation in icon-factory content.
  - `MaterialButtonsTests.cs`: add focused spacing coverage for default/interpolated/clamped/style-font-size paths.
  - docs/changelog: record shipped parity increment and coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `TextButton_Icon_DefaultSpacing_Uses8`
  - `TextButton_Icon_TextScaleFactor15_UsesInterpolatedSpacing6`
  - `TextButton_Icon_TextScaleFactor3_ClampsSpacingTo4`
  - `TextButton_Icon_StyleTextSize28_UsesClampedSpacing4`
- Parity-risk scenarios covered:
  - Baseline spacing remains `8` at scale `1`.
  - Interpolation step produces mid-gap (`6`) at scale `1.5`.
  - Clamp behavior enforces min-gap (`4`) for scale `>= 2`.
  - Style-level label font-size influences effective icon-gap scaling like Flutter icon factories.

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
