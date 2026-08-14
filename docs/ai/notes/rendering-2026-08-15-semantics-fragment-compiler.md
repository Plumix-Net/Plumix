# Feature: semantics fragment compiler

## Goal

- Replace Plumix's node-based semantics compiler with Flutter's fragment model, so a render object's
  configuration merges into the nearest contributing ancestor instead of always forming a node, the
  `ChildConfigurationsDelegate` sees descendant configurations, `SemanticsNode` carries a local rect
  plus a transform, and the geometry-driven traversal sort exists.

## Non-Goals

- Flutter 3.47's `accessibilityFocusBlockType`, `localeForSubtree`/`locale`, `AttributedString`,
  traversal grafting (`OverlayPortal`) and `SemanticsNode.getSemanticsData` merging — none of these
  exist in Plumix's semantics model and none are needed by the three divergences this closes.
- Propagating `Directionality` into the semantics tree automatically; the new `TextDirection` is
  wired through `Semantics`/`RenderSemanticsAnnotations` only, exactly like Flutter.

## Delivery Scope

- Target: the render-object semantics pipeline (`_RenderObjectSemantics`, `_SemanticsGeometry`,
  `_SemanticsFragment`, `_childrenInDefaultOrder`, `RenderSemanticsGestureHandler`).
- Completion checklist:
  - [x] API/default values
  - [x] Composition order (four-phase flush, `MergeSiblingGroup` before `BuildSemanticsSubtree`)
  - [x] State transitions (parent-data/geometry/built dirtying, blocked branches)
  - [x] Geometry (local rect, transform, both clips, hidden versus clipped-out)
  - [x] Traversal order
  - [x] Focused tests

## Dart Reference Mapping

- `flutter/packages/flutter/lib/src/rendering/object.dart` — `_SemanticsParentData`,
  `_SemanticsConfigurationProvider`, `_SemanticsFragment`, `_IncompleteSemanticsFragment`,
  `_RenderObjectSemantics`, `_SemanticsGeometry`, `PipelineOwner.flushSemantics`.
- `flutter/packages/flutter/lib/src/semantics/semantics.dart` — `SemanticsNode` geometry members,
  `_BoxEdge`, `_SemanticsSortGroup`, `_TraversalSortNode`, `_childrenInDefaultOrder`.
- `flutter/packages/flutter/lib/src/rendering/proxy_box.dart` — `RenderSemanticsGestureHandler`.
- `flutter/packages/flutter/lib/src/widgets/gesture_detector.dart` — `_GestureSemantics`,
  `_DefaultSemanticsGestureDelegate`, `replaceSemanticsActions`.
- `flutter/packages/flutter/lib/src/widgets/scroll_position.dart` — `_updateSemanticActions`.
- `flutter/packages/flutter/lib/src/rendering/table.dart` — `assembleSemanticsNode` transform shifts.

## Divergences

- Closed: the per-node-transform row (`SemanticsNode` had no transform, `RenderTable` positioned
  synthesized nodes with absolute rects).
- Narrowed: the scroll-semantics row now covers only viewport child ordering; the actions are back on
  `RenderSemanticsGestureHandler` and `explicitChildNodes` comes from the `Semantics` widget below the
  gesture detector, as in Flutter. The `InputDecorator`/`DataTable` row now covers only the
  data-table header nesting; delegates receive descendant configurations.
- Added: `MergeSiblingGroup` refuses to let an incomplete fragment donate or adopt the delegate
  owner's own `CachedSemanticsNode`. Flutter's code does allow it, which hands a purely synthesized
  sibling group the owner's node and then overwrites the owner's cache with it. See
  `docs/ai/DIVERGENCES.md`.

## Test Plan

- Updated: `SemanticsTreeTests.cs`, `ScrollSemanticsTests.cs`, `MetaDataIndexedSemanticsTests.cs`,
  `TableTests.cs`, `ModalBarrierTests.cs`, `ModalRouteBarrierTests.cs`, plus the Material suites whose
  finders assumed one label per node.
- New: `src/Plumix.Tests/SemanticsTraversalTests.cs`.

## Sample Parity Plan

- [x] C# sample impact checked — none; this is compiler infrastructure with no demo surface.
- [x] Dart sample parity checked — no change needed.
- [x] `docs/ai/PARITY_MATRIX.md` — no row affected.

## Remaining parity gaps

- `RenderViewport.VisitChildrenForSemantics` still walks first-to-last instead of
  `childrenInPaintOrder`. The traversal sort that would put paint order back into reading order is
  ported, but it only engages when an ancestor supplies a `textDirection`, and nothing above a
  viewport does yet. Next action: propagate `Directionality` into the semantics tree the way Flutter's
  text and `Semantics` widgets do, then switch the walk.
- `DataTable`'s sortable column header still nests its action instead of merging into one header node;
  the compiler loses an inner role while bucketing cells.
