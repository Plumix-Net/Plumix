# Feature: material-2026-04-05-button-m2-overlay-opacity-parity

## Goal

- Align default M2 pressed/focused overlay opacity for `TextButton`, `ElevatedButton`, and `OutlinedButton` with Flutter (`0.12` when `UseMaterial3=false`).

## Non-Goals

- No changes to `styleFrom(...)` overlay mapping (remains Flutter `styleFrom` semantics).
- No sample route/module updates.
- No changes to non-default custom style overlay precedence.

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
  - Expand only if outlined-button M2 default overlay chain cannot be validated from current references.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned in `src/Flutter.Material`.
  - Dart source remains source of truth for mode-aware button defaults.
  - Existing style-layer precedence contracts remain unchanged.

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
  - None in this iteration.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Buttons.cs`: make default overlay resolver support mode-specific pressed/focused opacity and wire M2 `0.12` for text/elevated/outlined defaults.
  - `MaterialButtonsTests.cs`: add focused M2 overlay regression tests for text/elevated/outlined defaults.
  - docs/changelog: record this parity increment.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `TextButton_DefaultOverlay_UseMaterial3Disabled_UsesPressedFocusedOpacity012`
  - `ElevatedButton_DefaultFocusedOverlay_UseMaterial3Disabled_UsesOnPrimaryOpacity012`
  - `OutlinedButton_DefaultFocusedOverlay_UseMaterial3Disabled_UsesPrimaryOpacity012`
- Parity-risk scenarios covered:
  - M2 pressed/focused overlay alpha is `0.12` for text/outlined buttons.
  - M2 elevated overlay uses `onPrimary` color with `0.12` alpha over `primary` background.
  - M3 and `styleFrom(...)` overlay behavior remains intact.

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
