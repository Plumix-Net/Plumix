# Feature: Slider Custom Shape Contracts

## Goal

- Close the remaining `Slider` and `RangeSlider` parity gap by allowing Flutter-shaped pluggable track, thumb,
  overlay, tick-mark, and value-indicator painters plus range thumb selection.

## Non-Goals

- Reworking the now-covered controlled values, callbacks, keyboard behavior, semantics, built-in 2023/2024
  geometry, labels, cursors, or padding behavior.

## Context Plan

- Entry files:
  - `src/Plumix.Material/SliderTheme.cs`
  - `src/Plumix.Material/Slider.cs`
  - `src/Plumix.Material/RangeSlider.cs`
- Expansion trigger:
  - Port the shared paint parameter and shape contracts before routing both render objects through custom shapes.

## Delivery Scope (Required for Control Parity Work)

- Target controls:
  - `Slider`
  - `RangeSlider`
- Completion checklist:
  - [x] API/default values for built-in controls
  - [x] State transitions/interaction states for built-in controls
  - [x] Constraint/layout behavior for built-in controls
  - [x] Built-in paint/visual semantics
  - [x] Focused tests for built-in behavior
  - [x] Public custom shape contracts and defaults
  - [x] Render delegation to custom shape implementations
  - [x] Range thumb-selector and minimum-separation contracts

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Rendering remains framework-owned; no Avalonia control implementation receives slider behavior.
- Dart remains the source of truth for public contracts and paint ordering.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `packages/flutter/lib/src/material/slider.dart`
  - `packages/flutter/lib/src/material/range_slider.dart`
  - `packages/flutter/lib/src/material/slider_theme.dart`
  - `packages/flutter/lib/src/material/slider_parts.dart`
  - `packages/flutter/lib/src/material/range_slider_parts.dart`
  - `packages/flutter/lib/src/material/slider_value_indicator_shape.dart`
- Parity mapping checklist:
  - [x] Built-in API/default values mapped
  - [x] Built-in state transitions mapped
  - [x] Built-in constraint/layout behavior mapped
  - [x] Built-in paint behavior mapped
  - [x] Custom shape subclass API mapped
  - [x] Custom shape paint dispatch mapped
- Divergences:
  - None. This is an open parity gap, not an intentional platform divergence.

## Planned Changes

- `src/Plumix.Material/SliderTheme.cs`: add Flutter-shaped shape base classes, paint parameter objects, default
  implementations, and range thumb-selector delegate.
- `src/Plumix.Material/Slider.cs`: resolve shape precedence and delegate layout/paint to the selected shapes.
- `src/Plumix.Material/RangeSlider.cs`: add equivalent range shape delegation, overlap ordering, and selector logic.
- `src/Plumix.Tests/MaterialSliderTests.cs`: cover custom single-slider shape sizing and paint dispatch.
- `src/Plumix.Tests/MaterialRangeSliderTests.cs`: cover range shape sizing, overlap, separation, and thumb selection.

## Test Plan

- Keep the full `Plumix.Tests` suite green.
- Add recording custom shapes that assert geometry, state, direction, activation, and paint ordering.
- Cover zero-size/no-paint shapes and custom preferred sizes in constrained and unbounded layouts.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated for the completed built-in behavior
- [x] Add mirrored custom-shape probes when the shape layer is implemented

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` updated
- [x] `docs/ai/TEST_MATRIX.md` updated with the remaining gap

## Done Criteria

- [x] Both controls accept and execute every Flutter public shape slot in `SliderThemeData`.
- [x] Range thumb selection and minimum separation match the Dart contracts.
- [x] Focused shape tests and the full test suite pass.
- [x] The custom-shape gap is removed from `docs/ai/TEST_MATRIX.md` and this note is archived as complete context.
