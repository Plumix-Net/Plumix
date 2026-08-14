# Feature: Sliver-backed PageView

## Goal

- Close the `PageView` divergence by replacing the gesture-driven page viewport with Flutter's
  `Scrollable` > `Viewport` > `SliverFillViewport` composition and a real `PageController`/`_PagePosition`.

## Non-Goals

- Port `RenderObject.showOnScreen` (shared with the scroll pipeline row).
- Turn notification metrics into a reference hierarchy so `PageMetrics` can be a distinct type.
- Adopt restoration buckets (`PageView.restorationId` still round-trips through `PageStorage`).

## Context Plan

- Entry files:
  - `src/Plumix/Widgets/PageView.cs`
  - `src/Plumix/Rendering/Scroll.cs`
  - `src/Plumix/Widgets/Scroll.cs`
- Expansion trigger:
  - `_PagePosition` needs an offset that does not exist before the first layout, which the shared
    `ScrollPosition` could not express and the viewport could not correct within a frame.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `PageView` / `PageController` / `PageScrollPhysics`
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics (the viewport's `clipBehavior`; the family paints nothing itself)
  - [x] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Behavior stays in core; `Plumix.Material` only consumes the new controller API.
- Public defaults changed, so the `CHANGELOG.md` entry is prefixed `Breaking:`.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `flutter/packages/flutter/lib/src/widgets/page_view.dart`
  - `flutter/packages/flutter/lib/src/widgets/sliver_fill.dart`
  - `flutter/packages/flutter/lib/src/rendering/sliver_fill.dart`
  - `flutter/packages/flutter/lib/src/widgets/scroll_position_with_single_context.dart`
  - `flutter/packages/flutter/lib/src/widgets/scroll_physics.dart`
  - `flutter/packages/flutter/test/widgets/page_view_test.dart`
  - `material_ui/lib/src/calendar_date_picker.dart` (`_MonthPicker`)
  - `dart_sample/lib/demos/general/page_view_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergences (rows in `docs/ai/DIVERGENCES.md`):
  - Notification metrics are the `ScrollMetricsSnapshot` value type, so `PageMetrics` is not a separate
    class; `viewportFraction`/`page` live on the shared snapshot.
  - The viewport is offset-pushed at build time rather than offset-pulled during layout, so
    `ScrollPosition.Pixels` reports zero before the first layout instead of asserting, and a position's
    correction is surfaced by re-running the viewport layout from the reported pixels.
  - `showOnScreen` is still missing, so a cached page cannot reveal itself.

## Planned Changes

- `src/Plumix/Rendering/Scroll.cs`: nullable-offset construction plus `HasPixels`/`HasViewportDimension`/
  `HasContentDimensions`/`HaveDimensions`, `KeepScrollOffset`, overridable
  `SaveScrollOffset`/`RestoreScrollOffset`/`RestoreOffset`, and a `Task`-returning `AnimateTo`.
- `src/Plumix/Rendering/Viewport.RenderViewport.cs`: `ViewportMetricsChangedCallback` plus the in-frame
  correction loop.
- `src/Plumix/Widgets/Scroll.cs`: page metrics on the snapshot, position-owned storage round-trip,
  Flutter's runtime-type physics-chain comparison, and no scroll notification for a dimension correction.
- `src/Plumix/Widgets/PageView.cs`: the whole family, ported 1:1.
- `src/Plumix/Rendering/PageView.cs`: deleted with `RenderPageViewport`.
- `src/Plumix.Material/Tabs.cs`, `CalendarDatePicker.cs`: consume the new controller; the calendar moves to
  `PageView.Builder` over the `firstDate`..`lastDate` month range.

## Test Plan

- New tests:
  - `src/Plumix.Tests/PageViewTests.cs`
- Existing tests updated:
  - `src/Plumix.Tests/MaterialTabsTests.cs`, `src/Plumix.Tests/ScrollPipelineTests.cs`
- Parity-risk scenarios covered:
  - Initial page on the first frame, `viewportFraction` above/below/equal to one with `padEnds`, the
    zero-viewport `_cachedPage` state machine, viewport resizes, precision-error page rounding, the lazy
    child window under three cache extents, `onPageChanged` halfway-point rules, snapping ballistics,
    controller swaps, `PageStorage` page round-trip, and every controller assert message.

## Sample Parity Plan

- [x] C# sample impact checked (`PageViewDemoPage` + gallery route)
- [x] Dart sample parity checked (`page_view_demo_page.dart` + route/import)
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` unchanged because milestone status did not change
- [x] `docs/ai/TEST_MATRIX.md` updated
- [x] `docs/ai/DIVERGENCES.md` row replaced with the narrowed remainder

## Done Criteria

- [x] The control is closed end-to-end
- [x] Behavior implemented
- [x] Required validation gates pass
- [x] No architecture invariant violations introduced
- [x] Remaining deltas documented as divergences
