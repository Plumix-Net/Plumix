# Feature: CarouselView family closeout

## Goal

- `CarouselView`/`.weighted`/`.builder`/`.weightedBuilder` are a strict port of
  `material_ui/lib/src/carousel.dart` + `carousel_theme.dart` at the 3.47.0 pin: Dart's two render
  slivers, scroll position, metrics, physics, controller and item composition, not an approximation
  built on Plumix's extent-strategy slivers.

## Non-Goals

- Gesture-driven drag/fling sequences and spring timing in tests (covered at the physics/position
  level instead).
- Rebasing the pre-existing `RenderSliverFixedExtentList` onto the new adaptor (see Divergences).

## Context Plan

- Entry files:
  - `material-ui-src/lib/src/carousel.dart` (2101 lines — read through `docs/ai/DART_SPEC_PROTOCOL.md`)
  - `src/Plumix/Rendering/Sliver.cs`
  - `src/Plumix/Widgets/PageView.cs` (nearest existing controller/position/metrics port)
- Expansion trigger:
  - The weighted carousel needs per-index layout hooks on the render object, which no existing
    Plumix sliver exposes.

## Delivery Scope

- Target control: `CarouselView` family.
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Invariants touched:
  - Core never references Material — the new adaptor lives in `src/Plumix/Rendering`, the carousel
    render objects derive from it inside `src/Plumix.Material`.
  - Missing primitives land before the control.

## Dart Reference Mapping

- Source of truth:
  - `material-ui-src/lib/src/carousel.dart`, `material-ui-src/lib/src/carousel_theme.dart`
  - `material-ui-src/test/carousel_test.dart`, `material-ui-src/test/carousel_theme_test.dart`
  - `flutter-src/packages/flutter/lib/src/rendering/sliver_fixed_extent_list.dart`
- Parity mapping checklist: all mapped (API/defaults, composition, states, layout, paint).
- Divergences:
  - One row added to `docs/ai/DIVERGENCES.md`: Flutter derives `RenderSliverFixedExtentList` from
    `RenderSliverFixedExtentBoxAdaptor`; Plumix now has a strict port of the Dart base, but the
    pre-existing fixed/variable-extent lists still carry their own simplified layout loop rather
    than deriving from it. Close condition: rebase them and delete the duplicated loop.

## Planned Changes

- `src/Plumix/Rendering/SliverFixedExtentList.cs` (new): `RenderSliverFixedExtentBoxAdaptor`.
- `src/Plumix.Material/Carousel.cs`: full rewrite of the control, theme wrapper, physics, metrics,
  position, controller and both render slivers.
- `src/Plumix.Tests/MaterialCarouselTests.cs`: rewritten against the Dart test assertions.
- Both carousel demo pages.

## Test Plan

- `src/Plumix.Tests/MaterialCarouselTests.cs` — 32 tests mapped onto Flutter's own assertions
  (uncontained layout `250 @800` → `0/250/500/750` with a 50 px trailing item, weighted `[4,3,2,1]`
  → `320/240/160/80`, `consumeMaxWeight` placing item 0 at `[240, 560]` for `[1,2,4,2,1]`, the
  `40/120/240/240/120/40` interpolation after a 40 px scroll, snapping targets, controller and
  position semantics, theme precedence, tap/splash wiring).

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [ ] `docs/FRAMEWORK_PLAN.md` — no milestone status change (M4 still in progress)
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] Control closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing (3487 total green)
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] The one remaining structural gap is registered in `docs/ai/DIVERGENCES.md`
