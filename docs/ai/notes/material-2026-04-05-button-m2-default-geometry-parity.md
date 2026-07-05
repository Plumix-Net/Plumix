# Feature: material-2026-04-05-button-m2-default-geometry-parity

## Goal

- Align default geometry tokens for `TextButton`, `ElevatedButton`, and `OutlinedButton` with Flutter M2 behavior when `ThemeData.UseMaterial3 = false` (`minimumSize`, `padding`, and `shape`).

## Non-Goals

- No changes to `styleFrom(...)` API shape.
- No changes to sample routes/pages.
- No full M2 color/elevation parity pass in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
- Expansion trigger:
  - Open additional Flutter references only if mode-specific outlined defaults remain ambiguous after `outlined_button.dart` verification.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material control behavior remains framework-owned in `src/Flutter.Material`.
  - Dart source remains source of truth for default-value and mode-aware behavior.
  - Style composition order remains unchanged (`default -> theme -> widget -> legacy`).

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - None for this geometry-focused M2 pass.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Buttons.cs`: make `TextButton`/`ElevatedButton`/`OutlinedButton` default geometry mode-aware for M2.
  - `MaterialButtonsTests.cs`: add focused regression tests for M2 `minimumSize`, `padding`, and clip `shape`.
  - docs/changelog: record parity increment and coverage expansion.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `TextButton_DefaultMinSize_UseMaterial3Disabled_UsesMaterialBaseline64x36`
  - `TextButton_DefaultPadding_UseMaterial3Disabled_UsesAll8`
  - `ElevatedButton_DefaultPadding_UseMaterial3Disabled_UsesHorizontal16AndZeroVertical`
  - `ElevatedButton_DefaultMinSize_UseMaterial3Disabled_UsesMaterialBaseline64x36`
  - `OutlinedButton_DefaultPadding_UseMaterial3Disabled_UsesHorizontal16AndZeroVertical`
  - `OutlinedButton_DefaultMinSize_UseMaterial3Disabled_UsesMaterialBaseline64x36`
  - `TextButton_UsesRoundedClipRadius4_WhenUseMaterial3Disabled`
  - `ElevatedButton_UsesRoundedClipRadius4_WhenUseMaterial3Disabled`
  - `OutlinedButton_UsesRoundedClipRadius4_WhenUseMaterial3Disabled`
- Parity-risk scenarios covered:
  - M2 default min-height contract (`36`) for non-filled Material buttons.
  - M2 default horizontal/vertical padding tokens for text/elevated/outlined.
  - M2 rounded-corner default shape token (`radius: 4`) for ink clip bounds.

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
