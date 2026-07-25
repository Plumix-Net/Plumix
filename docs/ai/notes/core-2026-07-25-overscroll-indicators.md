# Feature: Overscroll indicators

## Goal

- Port Flutter's glow and stretch overscroll controls together with source-shaped interaction, motion, and paint.

## Non-Goals

- Add a fragment-shader backend, pluggable velocity trackers, or Shift+wheel axis swapping.

## Context Plan

- Entry files:
  - `src/Plumix/Widgets/Scroll.cs`
  - `src/Plumix/Widgets/ScrollConfiguration.cs`
  - Flutter `widgets/overscroll_indicator.dart`
- Expansion trigger:
  - Add drag-detail notifications, conditional `ClipRect` behavior, and Material policy selection required by the
    source composition.

## Delivery Scope

- Target controls:
  - `GlowingOverscrollIndicator`
  - `StretchingOverscrollIndicator`
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for both controls

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Touched invariants:
  - Core owns scroll behavior and rendering.
  - Dart source remains the API/state/layout/paint source of truth.
  - C# and Dart sample probes remain mirrored.

## Dart Reference Mapping

- Flutter/Dart sources:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/overscroll_indicator.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/stretch_effect.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app.dart`
  - `dart_sample/lib/demos/general/state_storage_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence:
  - `docs/ai/DIVERGENCES.md` records the Avalonia fragment-shader gap; the port uses Flutter's documented affine
    fallback until backend shader filters are available.

## Planned Changes

- `src/Plumix/Widgets/OverscrollIndicator.cs`: both controls, notification, glow painter, stretch effect/controllers.
- `src/Plumix/Widgets/Scroll.cs`: source drag details and velocity-bearing notifications.
- `src/Plumix.Material/MaterialScrollBehavior.cs`: Material platform/M2/M3 indicator selection.
- `src/Plumix.Tests/OverscrollIndicatorTests.cs`: focused API/state/layout/paint/policy coverage.

## Test Plan

- Existing tests:
  - `ScrollInfrastructureTests`, `ScrollPipelineTests`, `MaterialRefreshIndicatorTests`, `MaterialScrollbarTests`
- New tests:
  - `src/Plumix.Tests/OverscrollIndicatorTests.cs`
- Risks covered:
  - leading/trailing direction, nested-depth filtering, veto/paint offset, horizontal paint transforms, real pointer
    drag delivery, return motion, conditional clipping, and Material platform selection.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] Both controls are closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining backend-only pixel gap documented with close condition
