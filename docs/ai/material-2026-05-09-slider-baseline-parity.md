# Feature: material-2026-05-09-slider-baseline-parity

## Goal

- Add framework Material `Slider` baseline parity (API/defaults/interaction/layout/paint/semantics) with C#/Dart sample route parity and focused regression tests.

## Non-Goals

- Full Flutter `Slider` feature matrix (`secondaryTrackValue`, value indicator/label UI, custom shape classes, and range slider variants).
- Host-specific haptics/audio feedback parity.

## Context Budget Plan

- Budget: max 16 files in initial read.
- Entry files:
  - `src/Plumix.Material/ThemeData.cs`
  - `src/Plumix.Material/Slider.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_routes.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/slider.dart`
- Expansion trigger:
  - Expand into tests and sample demo files if needed to close control parity in one iteration.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Slider`
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
  - Dart-source parity is the default target for Material control behavior.
  - Framework behavior remains in `src/Plumix*` layers (no host-side UI logic migration).

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/slider.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/slider_theme.dart`
  - `dart_sample/lib/demos/material/slider_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - Current framework scope intentionally excludes `secondaryTrackValue`, value-indicator label visuals, and shape-class customization APIs; baseline behavior is documented and covered for current API surface.

## Planned Changes

- Files to edit:
  - `src/Plumix.Material/Slider.cs`
  - `src/Plumix.Material/SliderTheme.cs`
  - `src/Plumix.Material/ThemeData.cs`
  - `src/Plumix.Tests/MaterialSliderTests.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/SliderDemoPage.cs`
  - `dart_sample/lib/sample_routes.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/demos/material/slider_demo_page.dart`
- Brief intent per file:
  - `src/Plumix.Material/Slider.cs`: add control API/state/render wiring for drag/tap/keyboard + semantics.
  - `src/Plumix.Material/SliderTheme.cs`: add theme data and inherited theme access.
  - `src/Plumix.Material/ThemeData.cs`: integrate `SliderTheme` into global theme surface.
  - `src/Plumix.Tests/MaterialSliderTests.cs`: add focused parity regression coverage.
  - sample files: add route/menu wiring and runtime parity probe page in both C# and Dart samples.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialSliderTests.cs`
- New tests to add:
  - `Slider_Constructor_Throws_OnInvalidArguments`
  - `Slider_DefaultM3_UsesPrimaryAndSurfaceContainerHighestColors`
  - `Slider_DefaultM2_UsesPrimaryTrackWithOpacityForInactive`
  - `Slider_ThemeColors_Apply_WhenWidgetColorsAreMissing`
  - `Slider_WidgetColors_OverrideThemeColors`
  - `Slider_Drag_InvokesOnChangeStartOnChangedAndOnChangeEnd_WithDiscreteSnapping`
  - `Slider_KeyboardArrowRight_IncrementsValueInLtr`
  - `Slider_KeyboardArrowLeft_InRtl_IncrementsValue`
  - `Slider_Semantics_ExposeSliderFlagEnabledFlagAndLabel`
- Parity-risk scenarios covered:
  - Pointer update path correctness for continuous/discrete slider behavior.
  - Keyboard delta behavior with platform and directionality-sensitive adjustments.
  - M2/M3 token default split and widget/theme override precedence.

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
