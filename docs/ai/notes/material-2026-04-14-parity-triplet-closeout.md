# Feature: material-2026-04-14-parity-triplet-closeout

## Goal

- Close the requested parity trio in one iteration:
  - `MediaQueryData.RemoveViewInsets(...)` edge coverage,
  - `FilledButton` advanced `ButtonStyle` matrix coverage,
  - deeper `endDrawer` drag choreography coverage.

## Non-Goals

- No new framework APIs.
- No sample-app UI restructuring.
- No Cupertino or host-level behavior changes.

## Context Budget Plan

- Budget: max 12 files in initial read.
- Entry files:
  - `src/Flutter.Tests/SafeAreaTests.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Flutter/Widgets/MediaQuery.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/Scaffold.cs`
- Expansion trigger:
  - Expand only if new tests expose implementation divergence.

## Delivery Scope (Required for Control Parity Work)

- Target controls:
  - `MediaQueryData` insets transforms
  - `FilledButton` style-layer behavior
  - `Scaffold` `endDrawer` drag settle choreography
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- Invariants touched:
  - Material state-layer precedence and fallback by state.
  - Drawer gesture settle behavior uses threshold + fling rules consistently per side.
  - Media insets transforms must be side-selective and non-negative.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `flutter/packages/flutter/lib/src/widgets/media_query.dart`
  - `flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `flutter/packages/flutter/lib/src/material/filled_button.dart`
  - `flutter/packages/flutter/lib/src/material/scaffold.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log:
  - none

## Planned Changes

- Files edited:
  - `src/Flutter.Tests/SafeAreaTests.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Intent:
  - add missing parity regressions and sync tracking docs.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/SafeAreaTests.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- New tests added:
  - `MediaQueryData_RemoveViewInsets_AdjustsViewPaddingAndZeroesSelectedInsets`
  - `MediaQueryData_RemoveViewInsets_ClampsViewPaddingToZeroWhenInsetsExceedPadding`
  - `FilledButton_ButtonStyleForegroundResolverNullForEnabled_FallsBackToDefaultEnabledColor`
  - `FilledButton_ButtonStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay`
  - `FilledButton_StyleFrom_OverlayColor_UsesHoverOpacityAndPressedPriority`
  - `FilledButton_StyleFrom_TransparentOverlay_KeepsBaseBackground_AndNoSplash`
  - `Scaffold_EndDrawer_DragReleaseVelocity_OpensDrawer_BelowHalfThreshold`
  - `Scaffold_EndDrawer_DragReleaseVelocity_ClosesDrawer_AboveHalfThreshold`
  - `Scaffold_EndDrawer_DragCancel_SettlesDrawerClosed_BelowHalfThreshold`
  - `Scaffold_EndDrawer_DragCancel_SettlesDrawerOpen_AboveHalfThreshold`
- Parity-risk scenarios covered:
  - Side-selective insets removal and clamped view-padding math.
  - Filled-button overlay priority and transparent overlay behavior.
  - End-drawer release/cancel settle decisions for both sides of threshold.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [ ] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps (if any) are documented with blocker + next action
