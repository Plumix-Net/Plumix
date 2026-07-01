# Feature: material-2026-03-29-button-focus-autofocus-parity

## Goal

- Align framework Material button API/behavior with Flutter `ButtonStyleButton` focus controls by adding `focusNode` and `autofocus` to `TextButton`, `ElevatedButton`, `FilledButton`, and `OutlinedButton`, with correct external/owned focus-node lifecycle behavior.

## Non-Goals

- No expansion of button visual style matrix beyond existing `ButtonStyle` fields.
- No new sample routes/pages in this iteration.
- No cross-host toolchain/environment fixes (Android/iOS blockers remain unchanged).

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `src/Flutter/Widgets/Focus.cs`
- Expansion trigger:
  - Expand only if focus lifecycle behavior required checking framework `Focus` internals and existing keyboard/focus regression patterns.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework behavior remains in framework libraries (`src/Flutter`, `src/Flutter.Material`).
  - Focus and interaction behavior remains widget-owned (`Widget -> Element -> RenderObject`) and not host-control-specific.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `flutter/packages/flutter/lib/src/material/text_button.dart`
  - `flutter/packages/flutter/lib/src/material/elevated_button.dart`
  - `flutter/packages/flutter/lib/src/material/filled_button.dart`
  - `flutter/packages/flutter/lib/src/material/outlined_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped (`focusNode`, `autofocus` defaults)
  - [x] Widget composition order mapped (focus wrapping remains in button core)
  - [x] State transitions/interaction states mapped (focused overlay reacts to external node focus changes)
  - [x] Constraint/layout behavior mapped (no layout/constraint delta from this change)
- Divergence log (only if needed):
  - none

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Buttons.cs`: add `focusNode`/`autofocus` API surface to Material button family and wire lifecycle-safe focus-node ownership in `MaterialButtonCore`.
  - `MaterialButtonsTests.cs`: add focused regressions for external focus-node driven overlay state and autofocus behavior (mount + update toggle).
  - docs/changelog files: record shipped parity step and coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `TextButton_ExternalFocusNode_RequestFocus_AppliesFocusedOverlay`
  - `TextButton_Autofocus_RequestsProvidedFocusNodeOnMount`
  - `TextButton_Autofocus_RequestIsAppliedWhenToggledFromFalseToTrue`
- Parity-risk scenarios covered:
  - external focus-node state changes update Material focused visuals,
  - autofocus requests focus through provided external node,
  - autofocus transition from disabled to enabled requests focus after rebuild.

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
