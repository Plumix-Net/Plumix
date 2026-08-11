# Feature: scroll-physics gesture tuning closeout

## Goal

Close the `ScrollPhysics` row in `docs/ai/DIVERGENCES.md`: the physics carried Flutter's whole gesture-tuning
surface, but the drag pipeline never read it, and the always/never physics plus the deferred-loading
recommendation were missing.

## Non-Goals

- Flutter's factory-map `RawGestureDetector` (`replaceGestureRecognizers` with a recognizer-type map).
- `ScrollAwareImageProvider` — the recommendation now exists; the provider stays in the `Image` row.
- Resetting `ScrollPosition.UserScrollDirection` to idle on non-scrolling activities (pre-existing, untouched).
- 2D/diagonal scrollables, `MultitouchDragStrategy`, and precise-pointer hit slop.

## Dart Reference Mapping

- `flutter/packages/flutter/lib/src/widgets/scroll_physics.dart` (spec taken through `dart-spec`)
- `flutter/packages/flutter/lib/src/widgets/scroll_activity.dart` (`ScrollDragController`, `HoldScrollActivity`)
- `flutter/packages/flutter/lib/src/widgets/scroll_position_with_single_context.dart` (`hold`/`drag`)
- `flutter/packages/flutter/lib/src/widgets/scrollable.dart` (`setCanDrag`, recognizer setup,
  `recommendDeferredLoadingForContext`)
- `flutter/packages/flutter/lib/src/gestures/monodrag.dart` (`isFlingGesture`/`considerFling`, `onDown`)

## Divergence Introduced

One row in `docs/ai/DIVERGENCES.md` (`Rendering/ScrollPhysics.cs`, `Rendering/Scroll.cs`):
`RecommendDeferredLoading` reads the view's physical size from the nearest `MediaQuery` because core has no
`View.of(context)`, and it passes only the activity velocity because `ScrollPosition` has no `forcePixels`
path to stamp a one-frame implied velocity.

## Notes For The Next Iteration

- `ScrollableState` gates drag registration imperatively through a `GlobalObjectKey` on its `RawGestureDetector`
  (`SetDragEnabled`), because Flutter applies `setCanDrag` during layout and a rebuild would land a frame late.
  Migrating `RawGestureDetector` to Flutter's recognizer-factory map would replace that with
  `replaceGestureRecognizers`.
- Keys are records with value equality, so a global key must be given per-instance identity
  (`GlobalObjectKey(new object())`); a shared `LabeledGlobalKey` trips the duplicate-global-key guard once two
  instances of the owner are mounted.
- `DragGestureRecognizer` now reports exactly one cancel for a pointer that never becomes a drag. The hold/drag
  lifecycle depends on it: without the cancel, tapping a flinging list would hold it forever.
