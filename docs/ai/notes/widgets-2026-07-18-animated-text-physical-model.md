# Feature: AnimatedDefaultTextStyle + AnimatedPhysicalModel

## Goal

- Port the paired Flutter implicit-animation controls with source-shaped APIs, transitions, rendering, tests, and
  mirrored C#/Dart sample probes.

## Non-Goals

- Add a new backend renderer for Flutter-exact first/last line-height trimming or isolated anti-aliased save layers.

## Context Plan

- Entry files:
  - `src/Plumix/Widgets/ImplicitAnimations.cs`
  - `src/Plumix/Widgets/DefaultTextStyle.cs`
  - `src/Plumix/Rendering/PhysicalModel.cs`
- Expansion trigger:
  - `AnimatedPhysicalModel` required the missing core `PhysicalModel` render/widget primitive, and
    `AnimatedDefaultTextStyle` required the complete inherited text-layout option surface.

## Delivery Scope (Required for Control Parity Work)

- Target controls:
  - `AnimatedDefaultTextStyle`
  - `AnimatedPhysicalModel`
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
- Invariants touched:
  - Framework behavior remains in `Plumix` through `Widget -> Element -> RenderObject`.
  - Dart source remains authoritative for API defaults, tween state, composition, and paint behavior.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/implicit_animations.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/basic.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/rendering/proxy_box.dart`
  - `dart_sample/lib/demos/general/align_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergences:
  - `docs/ai/DIVERGENCES.md` records Avalonia's missing first/last line-height trimming and distinct
    anti-aliased save-layer clip modes, including expected deltas and close conditions.

## Planned Changes

- Files edited:
  - `src/Plumix/Widgets/ImplicitAnimations.cs`: both implicit controls and interrupted tween continuity.
  - `src/Plumix/Widgets/DefaultTextStyle.cs`: `TextStyle.Lerp` and complete inherited layout options.
  - `src/Plumix/Widgets/PhysicalModel.cs`: source-shaped widget-to-render wiring.
  - `src/Plumix/Rendering/PhysicalModel.cs`: physical surface fill/shadow/shape clipping.
  - `src/Plumix.Tests/ImplicitAnimationsTests.cs`: API, state, layout, paint, and completion coverage.

## Test Plan

- Existing tests run/update:
  - `src/Plumix.Tests/ImplicitAnimationsTests.cs`
  - `src/Plumix.Tests/TextWidgetTests.cs`
  - `src/Plumix.Tests/MaterialListTileTests.cs`
  - `src/Plumix.Tests/MaterialGridTileTests.cs`
- Parity-risk scenarios covered:
  - Interrupted target changes continue from the visible value.
  - Non-animated text/shape fields apply immediately.
  - `animateColor`/`animateShadowColor` false paths switch immediately while geometry keeps animating.
  - Circle surfaces create an ellipse geometry clip and preserve child layout.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] Both paired controls are closed end-to-end within documented backend limits
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining backend deltas are documented with blocker and next action
