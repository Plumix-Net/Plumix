# Feature: material-2026-05-09-slider-secondary-track-semantics-parity

## Goal

- Expand framework Material `Slider` parity with Flutter-like secondary track support and semantic formatter behavior while preserving existing interaction/layout defaults.

## Non-Goals

- Full Flutter slider value-indicator/label visuals and custom shape-class APIs.
- Reworking slider gesture architecture beyond the current pointer/keyboard baseline.

## Context Budget Plan

- Budget: max 14 files in initial read.
- Entry files:
  - `src/Plumix.Material/Slider.cs`
  - `src/Plumix.Material/SliderTheme.cs`
  - `src/Plumix.Tests/MaterialSliderTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/SliderDemoPage.cs`
  - `dart_sample/lib/demos/material/slider_demo_page.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/slider.dart`
- Expansion trigger:
  - Expand into tracking docs and parity matrix once implementation/tests are complete.

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
  - Dart source remains the parity source of truth for slider API/default behavior.
  - Behavior remains in framework layers (`src/Plumix*`) and does not move into host adapters.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/slider.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/slider_parts.dart`
  - `dart_sample/lib/demos/material/slider_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - Current framework semantics surface uses a single semantics label field; `semanticFormatterCallback` is mapped as label precedence over `semanticLabel` instead of a separate semantics value field.

## Planned Changes

- Files to edit:
  - `src/Plumix.Material/Slider.cs`
  - `src/Plumix.Material/SliderTheme.cs`
  - `src/Plumix.Tests/MaterialSliderTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/SliderDemoPage.cs`
  - `dart_sample/lib/demos/material/slider_demo_page.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `src/Plumix.Material/Slider.cs`: add `secondaryTrackValue`, `secondaryActiveColor`, semantic formatter API, color resolution, and paint behavior.
  - `src/Plumix.Material/SliderTheme.cs`: add secondary track theme fields.
  - `src/Plumix.Tests/MaterialSliderTests.cs`: add constructor/normalization/color precedence/semantics regression tests.
  - sample files: add runtime probes for secondary track value/color in both C# and Dart demos.
  - docs files: sync parity/test/plan/changelog tracking.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialSliderTests.cs`
- New tests to add:
  - `Slider_SecondaryTrack_DefaultAndNormalizationFollowFlutterParity`
  - `Slider_SecondaryTrack_ThemeAndWidgetColorsFollowPrecedence`
  - `Slider_SecondaryTrack_DisabledUsesDisabledThemeColor`
  - `Slider_SemanticFormatterCallback_OverridesSemanticLabel`
- Parity-risk scenarios covered:
  - Secondary value validation and normalization consistency.
  - Secondary track color precedence and disabled-color fallback.
  - Semantic formatter precedence behavior.

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
