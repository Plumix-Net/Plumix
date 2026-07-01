# Feature: material-2026-05-09-range-slider-baseline-parity

## Goal

- Add framework Material `RangeSlider` baseline parity (API/defaults/interaction/layout/paint/semantics) with C#/Dart sample route parity and focused regression tests.

## Non-Goals

- Full Flutter `RangeSlider` matrix (`labels` value indicator visuals, custom shape classes, and full dual-focus keyboard model).
- Host-level haptics/advanced accessibility announcements beyond current framework semantics-label scope.

## Context Budget Plan

- Budget: max 16 files in initial read.
- Entry files:
  - `src/Plumix.Material/Slider.cs`
  - `src/Plumix.Material/SliderTheme.cs`
  - `src/Plumix.Material/ThemeData.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_routes.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/range_slider.dart`
- Expansion trigger:
  - Expand into focused tests/demo pages/docs tracking when required to close control parity in one iteration.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `RangeSlider`
- Completion checklist (must be closed in this iteration unless explicitly blocked):
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Dart-source parity remains the default target for Material controls.
  - Control behavior remains inside framework layers (`src/Plumix*`), not host adapters.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/range_slider.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/slider_theme.dart`
  - `dart_sample/lib/demos/material/range_slider_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - Current framework scope uses a single focus node for keyboard interaction (thumb chosen by last interaction/default end-thumb) instead of Flutter's full dual-focus thumb model.
  - Current scope does not include Material value-indicator label visuals (`RangeLabels`) or custom shape-class parity.

## Planned Changes

- Files to edit:
  - `src/Plumix.Material/RangeSlider.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/RangeSliderDemoPage.cs`
  - `src/Plumix.Tests/MaterialRangeSliderTests.cs`
  - `dart_sample/lib/sample_routes.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/demos/material/range_slider_demo_page.dart`
- Brief intent per file:
  - `src/Plumix.Material/RangeSlider.cs`: add range control API/state/render wiring and semantics formatting hook.
  - C#/Dart sample files: add route/menu wiring and parity demo page for runtime probes.
  - `src/Plumix.Tests/MaterialRangeSliderTests.cs`: add focused baseline parity regression coverage.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialRangeSliderTests.cs`
- New tests to add:
  - `RangeSlider_Constructor_Throws_OnInvalidArguments`
  - `RangeSlider_DefaultM3_UsesPrimaryAndSurfaceContainerHighestColors`
  - `RangeSlider_ThemeColors_Apply_WhenWidgetColorsAreMissing`
  - `RangeSlider_WidgetColors_OverrideThemeColors`
  - `RangeSlider_DragStartThumb_InvokesLifecycleCallbacksAndUpdatesStartValue`
  - `RangeSlider_DiscreteDrag_SnapsToDivisions`
  - `RangeSlider_KeyboardArrowRight_IncrementsEndValueInLtr`
  - `RangeSlider_Semantics_ExposeSliderFlagEnabledFlagAndFormattedLabel`
- Parity-risk scenarios covered:
  - Thumb selection and drag update behavior for start/end bounds.
  - Discrete snapping behavior with non-null divisions.
  - Keyboard update baseline and semantics label propagation for range values.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

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
