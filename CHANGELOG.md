# Changelog

- Breaking: closed the `InputDecorator` token/theme pass — the last `ColorScheme` closeout family.
  `InputDecorationThemeData` is now Flutter's subclassable class (37 fields, six of them non-nullable
  with source defaults, plus `CopyWith`/`Merge`/value equality with Dart's `runtimeType` guard)
  instead of a flat record, and the collapsed `InputDecoratorDefaults` record is replaced by the real
  `_InputDecoratorDefaultsM2`/`_InputDecoratorDefaultsM3` subclasses carrying the verbatim M2/M3
  tables. `InputDecorationTheme` gained Flutter's obsolete field-based constructor, forwarding
  getters, `Data` projection, `CopyWith`/`Merge` and `InheritedTheme.Wrap`.
  Behavior changes: the floating-label chain now merges `baseStyle` (it was dropped); prefix/suffix
  icon colors fall back to an ambient `IconButtonTheme` foreground between the decoration and the
  defaults; the M2 zero-width border test compares `Border == InputBorder.None` by value instead of
  by reference; and theme/decoration style, color and border-side slots accept state-resolving values
  (`WidgetStateTextStyle`, `WidgetStateColor`, `WidgetStateBorderSide`), so an
  `inputDecorationTheme.fillColor`/`labelStyle`/`activeIndicatorBorder` can itself resolve per state.
  Prerequisite primitives: new `WidgetStateTextStyle` in core and `WidgetState.Error` on the core
  state enum (with the `MaterialState` mapping and the `MaterialStateSet` bridge).
  `InputDecorationThemeData`'s constructor parameters are camelCase now, matching `InputDecoration`.
- Rotated the older half of `CHANGELOG.md` into `CHANGELOG-2026-H2.md` (the file had passed 100 KB).

- Breaking: ported `widgets/scroll_metrics.dart` in full and gave notifications a real metrics
  hierarchy. `ScrollMetricsSnapshot` (a `readonly record struct`) is gone: `IScrollMetrics` now
  carries Dart's whole mixin (`hasPixels`/`hasContentDimensions`/`hasViewportDimension`,
  `axisDirection`, `axis`, `atEdge`, `extentTotal`, `copyWith`) with `ScrollMetricsUtils` holding the
  shared bodies, and `FixedScrollMetrics` is an unsealed class with nullable-backed values and a
  virtual `CopyWith`. Every `ScrollNotification`, `ScrollMetricsNotification` and
  `ScrollIncrementDetails` carries `IScrollMetrics`, and `ScrollPosition.CopyWith` takes Dart's six
  optional overrides. `PageMetrics` and `NestedScrollMetrics` are now `FixedScrollMetrics`
  subclasses, so `notification.Metrics is PageMetrics` type-tests as in Flutter. Behavior changes:
  `AtEdge` is Dart's exact `pixels == min || pixels == max` instead of a 0.0001 tolerance,
  `ExtentTotal` is `max - min + viewportDimension` instead of the sum of the three extents, and the
  `FixedScrollMetrics` constructor takes Flutter's argument order. Closes the nested-metrics and
  page-metrics divergences.
- Fixed `RenderViewport.GetOffsetToReveal` counting a descendant's paint offset twice whenever
  slivers nest (a padded list, a `PageView`'s fractional padding): Plumix's `RenderSliver` is a
  `RenderBox`, so the pivot walk picked the inner sliver as its pivot where Dart's type test cannot.
  `ShowOnScreen`/`EnsureVisible` now reveal the right offset, including a `PageView`'s cached page.

- Breaking: ported `widgets/nested_scroll_view.dart` in full — `NestedScrollView`,
  `NestedScrollViewState` (`InnerController`/`OuterController`),
  `NestedScrollView.SliverOverlapAbsorberHandleFor`, `SliverOverlapAbsorber`/`SliverOverlapInjector`
  with their render objects, `NestedScrollViewViewport`/`RenderNestedScrollViewViewport`, and the
  private coordinator/controller/position/ballistic-activity stack (`UnnestOffset`/`NestOffset`,
  `_getMetrics` ranges and correction offset, the clamped/full drag and clamped pointer-signal
  updates, `FloatHeaderSlivers`).
  Prerequisite primitives: Flutter's `ScrollActivityDelegate` now exists as `IScrollActivityDelegate`
  and every `ScrollActivity` plus `ScrollDragController` targets it instead of a concrete
  `ScrollPosition` (their constructors take `@delegate`, and `DrivenScrollActivity`/
  `BallisticScrollActivity` take explicit `from`/`vsync`); `BallisticScrollActivity` is subclassable
  with `ApplyMoveTo`/`ResetActivity`; `ScrollPosition.SetPixels` is public and `Hold`/`GoIdle`/
  `ApplyPointerScrollDelta` are virtual; `ScrollPosition` gained Flutter's `DidStartScroll`/
  `DidEndScroll`/`DidUpdateScrollPositionBy`/`DidOverscrollBy`/`DidUpdateScrollDirection` and
  `ExtentBefore`/`ExtentInside`/`ExtentAfter`, and `ApplyPointerScrollDelta` now dispatches its own
  start/update/end notifications the way `ScrollPositionWithSingleContext.pointerScroll` does instead
  of `ScrollableState` doing it. New `UserScrollNotification`; `CustomScrollView` is no longer sealed
  and gained Flutter's `BuildViewport` hook plus `hitTestBehavior`; `Scrollable` accepts a viewport
  builder.

- Breaking: ported `rendering/viewport_offset.dart`, `rendering/viewport.dart` and
  `widgets/viewport.dart` in full. New `ViewportOffset` (with `ViewportOffset.Fixed`/`Zero`,
  `CorrectBy`, `MoveTo(clamp:)`, `UserScrollDirection`, `AllowImplicitScrolling`) is the protocol
  viewports lay out against; `ScrollPosition` now extends it, gained `CorrectForNewDimensions`,
  `DidUpdateScrollMetrics` and `CopyWith`, applies Flutter's `applyViewportDimension`/
  `applyContentDimensions` return contract, and `CorrectPixels` no longer notifies. The single
  approximate `RenderViewport` is replaced by `RenderViewportBase<TParentData>` plus
  `RenderViewport` (physical parent data, `Center`, `Anchor`, Flutter's `_attemptLayout` correction
  loop bounded by `10 * childCount`) and `RenderShrinkWrappingViewport` (logical parent data,
  `maxPaintExtent`-driven shrink-wrap extent), with `LayoutChildSequence`, `SliverPaintOrder`,
  `ChildrenInPaintOrder`/`ChildrenInHitTestOrder`, `ScrollOffsetOf`/`MaxScrollObstructionExtentBefore`
  per growth direction, `ComputeChildMainAxisPosition`, `IndexOfFirstChild` and the visual-overflow
  gated clip. The offset is now pulled during layout instead of pushed at build time, so
  `ViewportMetricsChangedCallback`/`ViewportMoveToCallback` are gone and `IRenderAbstractViewport`
  exposes `Offset`. New `Viewport`/`ShrinkWrappingViewport` widgets with a `center` key and
  `ViewportElement`; `Scrollable` and `CustomScrollView` gained `center`, `SliverConstraints` gained
  `CrossAxisDirection`, `RenderSliver` gained `CenterOffsetAdjustment`, and
  `RenderSliverSingleBoxAdapter.SetChildParentData` now uses Flutter's reversed-axis formula.
  `ScrollDirection` moved to the new `ViewportOffset.cs` and gained `FlipScrollDirection`. Default
  cache extent is now 250 (`RenderAbstractViewport.DefaultCacheExtent`) rather than zero, and
  `ScrollPosition.MoveTo` clamps into the scroll extents. `PlumixHost` ignores frames pumped from a
  thread other than the one that created it, which an Avalonia control may not be touched from.

- Breaking: ported `scheduler/ticker.dart` and `animation/animation_controller.dart` in full.
  `Ticker` now owns Flutter's lifecycle (`Start` returning a `TickerFuture`, `Stop(canceled:)`,
  `Muted`, `IsActive`/`IsTicking`, `ScheduleTick`/`UnscheduleTick`, `AbsorbTicker`, `DebugLabel`), and
  its callback reports **time elapsed since the ticker started** instead of the previous frame delta,
  so the first frame after a start reports zero. New `TickerFuture` (primary/secondary completer split,
  `Task`, `OrCancel`, `WhenCompleteOrCancel`, `TickerFuture.Completed()`), `TickerCanceled`, and the
  `TickerCallback` delegate; `Scheduler` gained `CurrentFrameTimeStamp`, `FramesEnabled` and
  `ScheduleForcedFrame`. `AnimationController` is now simulation-driven exactly as in Dart: Flutter's
  constructor shape (`value`/`duration`/`reverseDuration`/`debugLabel`/`lowerBound`/`upperBound`/
  `animationBehavior`/`vsync`), `Toggle`, `AnimateBackWith`, `Repeat(min, max, reverse, period, count)`,
  `Fling(springDescription:, animationBehavior:)`, `Resync`, `LastElapsedDuration`, `Behavior`,
  `AnimationBehavior` with the `DisableAnimations` hook, and ported `_InterpolationSimulation`/
  `_RepeatingSimulation`. Every entry point returns a `TickerFuture`, `Velocity` is simulation-backed for
  duration-driven runs too, and `Stop`/`Dispose` cancel the outstanding future rather than completing it —
  closing the `DraggableScrollableSheet`/`Velocity` divergence and the `AnimationController.Resync` half of
  the persistent-header one. `DrawerController` now drives its local history entry from the animation
  status the way Dart does, and `DraggableScrollableController.AnimateTo` returns the `TickerFuture`.

- Breaking: replaced the 2D affine transform pipeline with Flutter's 4x4 `Matrix4`.
  New `Plumix.UI.Matrix4` (a column-major port of `vector_math` 2.4.2, with `Vector3`/`Vector4`/
  `Quaternion`/`Matrix3`) and `Plumix.Rendering.MatrixUtils` (`painting/matrix_utils.dart` in full).
  `RenderObject.ApplyPaintTransform` now takes a `Matrix4` mutated in place and post-multiplies its own
  step, exactly as in Dart, so the previously reversed row-vector composition is gone; `GetTransformTo`,
  `LocalToGlobal`/`GlobalToLocal` (now Flutter's unprojection), `SemanticsNode.Transform`, `TransformLayer`,
  `FollowerLayer`, `PaintingContext.PushTransform`, `FlowPaintingContext.PaintChild` and
  `OverlayChildLayoutInfo.ChildPaintTransform` all carry `Matrix4`. `Transform` gained Flutter's
  `Rotate`/`Scale`/`Translate`/`Flip` factories plus `origin` and `transformHitTests`; `RenderTransform`
  gained `Origin`, `TransformHitTests`, `SetIdentity`/`RotateX`/`RotateY`/`RotateZ`/`Translate`/`Scale`,
  Flutter's `T(origin)*T(a)*M*T(-a)*T(-origin)` conjugation and its singular/non-finite paint short circuit
  through the new `RenderObject.PaintsChild`. Hit testing goes through the new
  `BoxHitTestResult.AddWithPaintTransform`/`AddWithRawTransform`/`AddWithPaintOffset` and
  `PointerEventUtils.RemovePerspectiveTransform`. `RotationTransition` now uses `Matrix4.RotationZ`
  verbatim instead of snapping quarter turns, `RenderFittedBox` zeroes the matrix for a child it does not
  paint and composes `T(dest)*S*T(-source)` in Dart's order, and `AnimatedContainer` interpolates its
  transform through the new `Matrix4Tween` decomposition. Avalonia's `Matrix` is a full 3x3 projective
  matrix that the Skia backend feeds to `SKMatrix44`, so perspective and X/Y-axis rotations now render;
  the corresponding `DIVERGENCES.md` row is closed and replaced by a narrower one covering
  `RenderTransform`'s retained-layer composition.

- Breaking: ported `painting/gradient.dart` and `painting/box_shadow.dart` in full.
  `Gradient` now carries `Colors`/`Stops`/`Transform` with `ImpliedStops`, `CreateShader(rect, textDirection)`,
  `Scale`, `WithOpacity`, `FromColor` and the `LerpFrom`/`LerpTo` dispatch, alongside `LinearGradient`
  (rewritten), `RadialGradient`, `SweepGradient`, `GradientTransform`/`GradientRotation` and a `TileMode`
  on every gradient. Stop interpolation is Dart's union-of-stops resample, and `ShapeDecoration.Lerp`
  bridges a plain color into the other side's gradient through `FromColor`. `Shadow`/`BoxShadow`
  (`BlurStyle`, `SpreadRadius`, `BlurSigma`, `Scale`, `CopyWith`, `Lerp`, `LerpList`) replace the Avalonia
  shadow structs on `BoxDecoration`/`ShapeDecoration`/`MagnifierDecoration`/`BannerPainter`, so shadow
  lists interpolate instead of snapping at 50%. `Alignment`/`AlignmentGeometry` gained Dart-shaped
  `ToString` plus `AlignmentGeometry.Resolve(TextDirection?)`, which throws when a directional value has
  no direction.
  **Breaking:** `BoxDecoration.BoxShadows`, `ShapeDecoration.Shadows`, `MagnifierDecoration.Shadows`,
  `CupertinoMagnifier.Shadows`, `Magnifier.Shadows`, `BannerPainter.Shadow` and `StepStyle.BoxShadow` take
  the framework `BoxShadow` instead of `Avalonia.Media.BoxShadow(s)`; `Gradient.CreateBrush()` is replaced
  by `CreateShader(rect, textDirection)`; `LinearGradient.Begin`/`End` are `AlignmentGeometry`; and
  `BoxDecoration.Brush` (the Avalonia-brush escape hatch) is gone — use a `Gradient`.

- Breaking: ported the keyboard identity stack. `keyboard_key.g.dart` is now generated into
  `src/Plumix/UI/KeyboardKey.g.cs` by `scripts/generate_keyboard_keys.py` (444 `LogicalKeyboardKey` and
  269 `PhysicalKeyboardKey` constants with Flutter's key ids, USB HID usages, labels, debug names,
  planes and synonyms), and `hardware_keyboard.dart`/`raw_keyboard.dart` land as `KeyEvent` +
  `KeyDownEvent`/`KeyUpEvent`/`KeyRepeatEvent`, `KeyEventDeviceType`, `KeyboardLockMode`, the full
  `HardwareKeyboard` state machine (pressed keys, lock toggling, `LookUpLayout`, `SyncKeyboardState`,
  handler copy-on-write and OR-accumulated dispatch), `RawKeyEventData`/`RawKeyEvent`/`RawKeyboard`
  with modifier synchronization, and `KeyEventManager`, which regularizes host events and synthesizes
  the sync events. The Avalonia host maps `PhysicalKey`/`Key`/`KeyDeviceType` through the new
  `HostKeyboardMap`.
  **Breaking:** `KeyEvent` is abstract and carries `PhysicalKey`/`LogicalKey`/`TimeStamp`/`DeviceType`/
  `Synthesized` instead of `Key`/`IsDown`/`IsRepeat`/`IsNumLockOn`/`IsXPressed` — test for the subclass
  and read modifiers from `HardwareKeyboard.Instance`; `LogicalKeySet`, `SingleActivator.Trigger`,
  `ShortcutActivator.Triggers` and `ShortcutSerialization.Modifier` take `LogicalKeyboardKey` instead of
  key-name strings (the channel entry now carries `keyId`), `LogicalKeySet.NormalizeKey` is gone, and
  the `LogicalKeyboardKey` placeholder enum in `ScrollConfiguration.cs` is replaced by the real class.

- Breaking: replaced the semantics compiler with Flutter's fragment model. `RenderObjectSemantics` is
  now a 1:1 port of `_RenderObjectSemantics` (four-phase `UpdateChildren`/`EnsureGeometry`/
  `EnsureSemanticsNode`, `ISemanticsFragment`/`IncompleteSemanticsFragment`, `SemanticsParentData`,
  `SemanticsGeometry`, sibling merge groups), so a render object merges its configuration into the
  nearest contributing ancestor unless it is a boundary, the parent imposed `explicitChildNodes`, or it
  conflicts with a sibling. `SemanticsNode.Rect` is now **local** with a `Transform` into the parent
  node (plus `ParentSemanticsClipRect`/`ParentPaintClipRect`/`IsMergedIntoParent`/`IsInvisible`/
  `GlobalRect`), and `RenderTable` positions synthesized rows/cells by shifting transforms.
  `SemanticsTraversal` ports the geometry-driven default sort behind the new
  `SemanticsNode.ChildrenInTraversalOrder`, fed by a new `TextDirection` on `SemanticsConfiguration`,
  `SemanticsNode`, `Semantics` and `RenderSemanticsAnnotations`. `RenderSemanticsGestureHandler` and
  `_GestureSemantics` land, so `RawGestureDetector`/`GestureDetector` gained `excludeFromSemantics` and
  the scroll actions now merge up from the gesture detector into the scroll node the way Flutter does
  (`ScrollPosition` filters them through the new `SemanticActionsChanged`).
  **Breaking:** `VisitChildrenForSemantics` takes `Action<RenderObject>` (positions come from
  `ApplyPaintTransform`); `RenderObject.PerformSemantics` and `SemanticsConfiguration.IsExcluded`/
  `ExplicitRect` are gone; merged labels/hints/tooltips join with `\n` instead of a space; a
  configuration that blocks user actions now clears them on the node (`AreUserActionsBlocked`);
  `SemanticsNode.Children` is paint order and sort keys apply to `ChildrenInTraversalOrder`;
  `SortKey` now counts as an annotation. Closes the per-node-transform divergence and the action half
  of the scroll-semantics one; narrows the `InputDecorator`/`DataTable` delegate divergence.

- Breaking: ported the scrollable semantics layer, so screen readers can now scroll Plumix views.
  `RenderViewport` gained the `UseTwoPaneSemantics`/`ExcludeFromScrolling` tags, a semantics clip that
  spans the cache extent (offscreen-but-cached children are now reported hidden instead of dropped),
  the overlap-corrected paint clip, and the `visible || cacheExtent > 0 || ensureSemantics` child
  filter; `RenderSliver` gained `EnsureSemantics` and `HasSliverConstraints`, `SliverGeometry` gained
  `Visible`. New `ScrollSemantics`/`RenderScrollSemantics` (Flutter's `_ScrollSemantics`) sit above the
  viewport and split its nodes into a scrolling node and the non-scrolling siblings a pinned header
  contributes. `SemanticsConfiguration`/`SemanticsNode` gained `ScrollPosition`, `ScrollExtentMin`,
  `ScrollExtentMax`, `ScrollChildCount`, `ScrollIndex`, `HasImplicitScrolling`, `OnScrollUp`/`Down`/
  `Left`/`Right`, `OnScrollToOffset` and `IsTagged`; `RenderSliverPersistentHeader` and
  `PinnedHeaderSliver` now tag themselves out of the scrolling pane, closing both divergences.
  `Scrollable`/`CustomScrollView`/`ListView` gained `excludeFromSemantics`/`semanticChildCount`, the
  child delegates gained `addSemanticIndexes`/`semanticIndexCallback`/`semanticIndexOffset`
  (`ListView.Separated` gives separators no index), `RenderTable` rows carry a row-scoped
  `showOnScreen`, and `RenderSingleChildViewport` describes a content-spanning semantics clip.
  **Breaking:** semantics action handlers are now `SemanticsActionHandler` (`void(object? args)`);
  `SemanticsOwner.PerformAction`/`SemanticsNode.PerformAction` take an optional argument;
  `SemanticsNode.UpdateWith` accepts a null configuration; and list/grid child delegates wrap children
  in `IndexedSemantics` by default.

- Breaking: closed the `SelectableRegion` gesture/shortcut/overlay divergence by porting the three
  primitives it needed. `gestures/tap_and_drag.dart` lands as `BaseTapAndDragGestureRecognizer` +
  `TapAndPanGestureRecognizer`/`TapAndHorizontalDragGestureRecognizer` (and the deprecated
  `TapAndDragGestureRecognizer`) with the five `TapDrag*Details` types, the consecutive-tap tracker,
  `eagerVictoryOnDrag`, `dragUpdateThrottleFrequency` and the `kPressTimeout` deadline; supporting
  `gestures/constants.dart`, `OffsetPair`, `OneSequenceGestureRecognizer`, `computeHitSlop`/
  `computePanSlop`, `DeviceGestureSettings.PanSlop` and `GestureRecognizer`'s
  `debugOwner`/`allowedButtonsFilter`/`getKindForPointer`/`invokeCallback` surface landed with it.
  `widgets/default_text_editing_shortcuts.dart` lands complete (all seven platform maps, the numpad
  numLock maps, the iOS/macOS/web disabling maps and `intentForMacOSSelector`) and `WidgetsApp` now
  nests it inside the app shortcuts exactly as Flutter does; `text_editing_intents.dart` gained the
  eleven missing intents. `RawGestureDetector` accepts Flutter's `gestures` recognizer map through
  `GestureRecognizerFactory`/`GestureRecognizerFactoryWithHandlers`, `LongPressGestureRecognizer`
  reports `onLongPressStart`/`MoveUpdate`/`End`, and `ScrollIntent`/`ScrollIncrementType`/
  `ScrollIncrementDetails`/`ScrollAction` plus `Scrollable.incrementCalculator` are ported.
  `SelectableRegion` now drives the real recognizers, builds a `SelectionOverlay` (handles, toolbar,
  magnifier, handle-drag math and `SelectionResult.Pending` retries), exposes the source `_*Action`
  classes, and composes `TapRegion` > `CompositedTransformTarget` > `RawGestureDetector` > `Actions` >
  `Focus` in source order. **Breaking:** the gesture arena now rejects every loser before accepting the
  winner (Flutter's order), `TapGestureRecognizer` no longer competes for a button with no callbacks,
  `ScrollToDocumentBoundaryIntent` derives from `DirectionalTextEditingIntent` and drops
  `collapseSelection`, and `ExtendSelectionToNextWordBoundaryOrCaretLocationIntent`/
  `ExpandSelectionToDocumentBoundaryIntent`/`ExpandSelectionToLineBreakIntent` derive from
  `DirectionalCaretMovementIntent`. Added `PlatformDefaults.IsWeb` (Flutter's `kIsWeb`) and a
  test-swappable `GestureTimer`.
- Breaking: replaced the bespoke text-selection stack with Flutter's selection protocol
  (`rendering/selection.dart`, `widgets/selection_container.dart`, `widgets/selectable_region.dart`,
  the `_SelectableFragment` half of `rendering/paragraph.dart`, `services/text_boundary.dart`,
  `services/text_layout_metrics.dart`, `widgets/text_editing_intents.dart`). New core contract:
  `ISelectionRegistrar`/`ISelectionHandler`/`ISelectable`, `SelectionRegistrant`, `SelectionGeometry`,
  `SelectionPoint`, `SelectionStatus`, `SelectionResult`, `SelectedContent`/`SelectedContentRange`,
  `SelectionUtils`, and the full `SelectionEvent` family (edge updates with `TextGranularity`, select
  all/word/paragraph, clear, granular and directional extension). `SelectionContainer`
  (+ `SelectionContainer.Disabled`), `SelectionRegistrarScope` and `SelectionContainerDelegate` are
  ported, with `MultiSelectableSelectionContainerDelegate` and `StaticSelectionContainerDelegate`
  supplying screen-order registration, edge-init/adjust sweeps, inactive-selection flushing, handle-layer
  ownership and replayed edge updates for late children. `RenderParagraph` splits its text into
  `SelectableFragment`s at placeholder boundaries, paints highlights before the text and handle
  `LeaderLayer`s after it, and gained the text metrics the fragments need (`GetBoxesForSelection` with
  `BoxHeightStyle`/`BoxWidthStyle`, `GetOffsetForCaret`, `GetFullHeightForCaret`, `PreferredLineHeight`,
  `GetPositionForOffset`, `GetWordBoundary`, `GetLineBoundary`, `ComputeLineMetrics`). New
  `TextBoundary`/`CharacterBoundary`/`LineBoundary`/`WordBoundary`/`ParagraphBoundary`/`DocumentBoundary`
  and `ITextLayoutMetrics` primitives back granular movement. `SelectableRegion` is now a
  `SelectionRegistrar` over one `StaticSelectionContainerDelegate` with Flutter's platform tap-count,
  right-click, long-press and drag tables, `SelectableRegionSelectionStatus`(+`Scope`), the
  `GetSelectableButtonItems` menu, and an `Actions`/`Shortcuts` layer over the ported text-editing
  intents. **Breaking:** `ITextSelectionRegistrar`/`TextSelectionRegistrar` are gone,
  `RenderParagraph.SelectionColor` is nullable and its cursor/`SetSelection`/pointer-selection members
  are removed (a paragraph no longer owns a caret), `SelectableRegion` requires `selectionControls` and
  no longer takes cursor/`enabled`/`showCursor` parameters, `SelectionArea` resolves the selection color
  through `DefaultSelectionStyle`, and a selection menu now shows Flutter's `[Copy, SelectAll]` pair
  instead of a single item. Because Plumix's context menu is route-backed, showing it hands focus to the
  menu's modal scope; the region no longer clears its selection (or tears the half-pushed route down) on
  that focus loss.

- Breaking: closed the sliver persistent-header divergence end-to-end
  (`rendering/sliver_persistent_header.dart`, `widgets/sliver_persistent_header.dart`,
  `material_ui/lib/src/app_bar.dart`). `RenderSliverPersistentHeader` is now Flutter's abstract base with
  `RenderSliverScrollingPersistentHeader`, `RenderSliverPinnedPersistentHeader`,
  `RenderSliverFloatingPersistentHeader` and `RenderSliverFloatingPinnedPersistentHeader`; new
  `OverScrollHeaderStretchConfiguration` and `FloatingHeaderSnapConfiguration` bring overscroll stretch
  (with the edge-triggered `onStretchTrigger`) and snap animations, and a floating header expands itself
  for a `ShowOnScreen` request through `PersistentHeaderShowOnScreenConfiguration`.
  `SliverPersistentHeaderDelegate` gained `Vsync`/`SnapConfiguration`/`StretchConfiguration`/
  `ShowOnScreenConfiguration`, `SliverPersistentHeader` is stateless and picks one of the four render
  objects, and its element builds the child during layout (`_FloatingHeader` drives snapping from the
  scroll position). `SliverAppBar` builds all three configurations, supplies `vsync`, and composes
  `FlexibleSpaceBar.CreateSettings(child: AppBar(...))` the way Flutter does instead of hand-painting its
  own surface; `AppBar` now reads `FlexibleSpaceBarSettings.IsScrolledUnder`. **Breaking:**
  `SliverPersistentHeader` is a `StatelessWidget`, the old single `RenderSliverPersistentHeader(minExtent,
  maxExtent, pinned, floating, onLayout)` constructor is gone, `LastShrinkOffset` is now clamped to
  `MaxExtent` (not `MaxExtent - MinExtent`), a pinned header's `LastOverlapsContent` comes from
  `constraints.Overlap > 0` instead of the shrink offset, and the app-bar surface/elevation/shape are
  resolved by `AppBar`'s `Material` rather than by the header delegate.

- Breaking: closed the show-on-screen / reveal protocol end-to-end (`rendering/viewport.dart`,
  `widgets/single_child_scroll_view.dart`, `widgets/scrollable.dart`, `widgets/scroll_position.dart`).
  New core primitives: `RevealedOffset` (+ `ClampOffset`), the `IRenderAbstractViewport` contract with the
  `RenderAbstractViewport.MaybeOf`/`Of`/`DefaultCacheExtent`/`ShowInViewport` statics,
  `RenderObject.ShowOnScreen`/`PaintBounds`, `RenderSliver.ChildMainAxisPosition`/`ChildCrossAxisPosition`/
  `ChildScrollOffset` with the padding/adaptor/grid/group/header overrides, `ScrollPositionAlignmentPolicy`,
  `ScrollPosition.MoveTo`/`EnsureVisible`/`AllowImplicitScrolling`, and
  `PersistentHeaderShowOnScreenConfiguration` (wired through `SliverFloatingHeader`). Both viewports now
  implement `GetOffsetToReveal` and `ShowOnScreen`, pinned headers trim a reveal against their leading edge,
  and a `SemanticsNode` with no explicit handler falls back to its render object's reveal request.
  **Breaking:** `Scrollable.EnsureVisible` returns `Task` instead of `bool`, walks every enclosing
  scrollable innermost-first (it used to move only the nearest one from a flat root-transform delta), takes
  an `alignmentPolicy`, and no longer rejects an alignment outside `[0, 1]`;
  `SemanticsOwner.PerformAction(id, ShowOnScreen)` now returns true for any node backed by a render object;
  a pinned `RenderSliverPersistentHeader` reports `MaxScrollObstructionExtent = MinExtent`. New
  `Ensure visible` demo page in both samples.

- Breaking: closed the `Router` divergence end-to-end (`widgets/router.dart`). `RouteInformation`,
  `RouterConfig`, `Router`/`Router.WithConfig` with the `_RouterScope`/`_RouterState` transaction and
  post-frame reporting machinery, `RouteInformationParser`, `RouterDelegate`,
  `PopNavigatorRouterDelegateMixin`, `RouteInformationProvider`, `PlatformRouteInformationProvider`, the
  `BackButtonDispatcher`/`RootBackButtonDispatcher`/`ChildBackButtonDispatcher` priority chain and
  `BackButtonListener` are all ported, plus `WidgetsApp.Router`/`MaterialApp.Router`. New host plumbing:
  `SystemNavigator` (history mode, route-information reporting, `DefaultRouteName`) and
  `WidgetsBindingObserver.DidPopRoute`/`DidPushRouteInformation` with
  `WidgetsBinding.HandlePopRoute`/`HandlePushRouteInformation`. **Breaking:**
  `Navigator.TryHandleBackButton` now offers the pop to binding observers before the navigator handler
  stack, so a `RootBackButtonDispatcher` wins over a nested `Navigator`; `WidgetsApp` registers itself as a
  binding observer and resolves its initial route through `SystemNavigator.DefaultRouteName`. New
  `Router` demo page in both samples.

- Breaking: closed the `About`/`LicensePage` divergence end-to-end (`material_ui/about.dart`). The whole
  private master-detail shell is now ported — `_MasterDetailFlow`, `_MasterDetailScaffold`, `_MasterPage`,
  `_DetailView`, `_PackagesView`, `_PackageListTile`, `_PackageLicensePage(Title)`, `_LicenseData`,
  `_DetailArguments`, `_AboutProgram` — so `LicensePage` runs Flutter's nested navigator below 840 logical
  pixels and the side-by-side layout at or above it, with the detail page in a `DraggableScrollableSheet`
  and a 500 ms fade-upwards swap. `AboutDialog.Adaptive` now really adapts: `CupertinoDialogAction`s on
  iOS/macOS, `TextButton`s elsewhere. **Breaking:** `ShowAboutDialog`/`ShowAdaptiveAboutDialog`/`ShowLicensePage`
  return `void` like Dart instead of a `Task`; `ShowLicensePage` captures the ambient `InheritedTheme`s and
  pushes a `MaterialPageRoute`; `AboutDialog` is no longer `sealed`; `MaterialLocalizations.LicensesPackageDetailText`
  now returns Flutter's `"No licenses."`/`"1 license."`/`"N licenses."` (the trailing period was missing) and
  rejects a negative count. Supporting core work: `Scheduler` gained Flutter's task queue
  (`ScheduleTask`/`HandleEventLoopCallback`/`SchedulingStrategy`/`DefaultSchedulingStrategy`) plus the
  `Priority` value type, `BorderRadius` gained `Vertical`/`Horizontal`, and
  `FadeUpwardsPageTransitionsBuilder` was added.

- Breaking: closed the remaining `Scaffold` slot divergence end-to-end (`material_ui/scaffold.dart`).
  `_ScaffoldSlot` now carries Flutter's full member list, and `_ScaffoldLayout` lays out the four missing
  slots: `persistentFooter`, `statusBar`, `drawer` and `endDrawer`. New `Scaffold` options
  `PersistentFooterButtons`, `PersistentFooterAlignment`, `PersistentFooterDecoration`, `Primary`,
  `OnDrawerChanged`, `OnEndDrawerChanged` and `DrawerDragStartBehavior`. The ported `_BodyBuilder` restores the
  `MediaQuery` padding the body extends behind for `extendBody`/`extendBodyBehindAppBar`, and a `_bodyKey`
  `KeyedSubtree` keeps the body's state when either flag changes. On iOS/macOS a `primary` scaffold installs
  the ported `_HitTestableAtOrigin` status-bar target, so `WidgetsBinding.HandleStatusBarTap` scrolls only the
  foreground scaffold's primary scrollable to the top (1000 ms, `Curves.EaseOutCirc`). **Breaking:** the
  hand-rolled drawer machinery in `ScaffoldState` is gone — both drawers are now `DrawerController`s in their
  layout slots, so the opened end drawer paints above the start drawer, an open drawer no longer sets
  `ImpliesAppBarDismissal`, `OpenDrawer`/`CloseDrawer` animate through the controller's fling instead of
  settling a scaffold-local animation, and `DismissIntent` (Escape) closes an open dismissible drawer.
  Supporting core work: `BoxConstraints` gained an `IBoxConstraintsMetadata` carrier (the value-type stand-in
  for subclassing, used by `_BodyBoxConstraints`), `WidgetsBindingObserver`/`WidgetsBinding` gained
  `HandleStatusBarTap`, `RenderOverflowBar` gained its four intrinsic-size methods, `Curves.EaseOutCirc` was
  added, `LabeledGlobalKey<T>` compares by identity like Dart's class instead of by its debug label, and an
  element reparented through a global key now hands its new slot to the render objects it attaches.

- Breaking: closed the `PageView` divergence end-to-end (`widgets/page_view.dart`). `PageView` is no longer a
  gesture-driven widget over a bespoke `RenderPageViewport`: it now builds Flutter's composition
  (`NotificationListener<ScrollNotification>` > `Scrollable` > `Viewport` > `SliverFillViewport`), so pages are
  lazy, `pageSnapping`/physics/overscroll/mouse-wheel come from the shared scroll pipeline, and page state is
  measured in scroll pixels. `PageController` is now a `ScrollController` (`Page`, `JumpToPage`, `AnimateToPage`,
  `NextPage`, `PreviousPage`, `OnAttach`/`OnDetach`, Flutter's four attach asserts) backed by the ported
  `_PagePosition` (`getPageFromPixels`/`getPixelsFromPage`, `_initialPageOffset` centering for
  `viewportFraction > 1`, the zero-viewport `_cachedPage` state machine, page-valued `PageStorage` round-trip),
  and `PageScrollPhysics` gained its real `createBallisticSimulation`. New `PageView` options: `PadEnds`,
  `RestorationId`, `ScrollCacheExtent`, `HitTestBehavior`, `ScrollBehavior`, plus `PageView.Builder` and the
  `SliverChildDelegate` (`PageView.custom`) constructor. Supporting core work: `ScrollPosition` gained
  `HasPixels`/`HasViewportDimension`/`HasContentDimensions`/`HaveDimensions`, a null-initial-offset constructor,
  `KeepScrollOffset` and overridable `SaveScrollOffset`/`RestoreScrollOffset`/`RestoreOffset`; `AnimateTo` returns
  Flutter's completion future as a `Task`; `RenderViewport` re-runs its layout in the same frame when the position
  corrects the offset (`ViewportMetricsChangedCallback`); `Scrollable` compares physics chains by runtime type
  (Flutter's `_shouldUpdatePosition`) instead of by identity, and no longer reports a `ScrollUpdateNotification`
  for an offset corrected while applying fresh viewport dimensions. **Breaking:** `PageController.EffectivePage`,
  `PageViewport` and `RenderPageViewport` are gone, `PageController.Page` throws Flutter's messages instead of
  returning null when unattached, `PageView` children are built lazily (offscreen pages no longer keep state
  alive unless `allowImplicitScrolling`/`scrollCacheExtent` says so), and `ScrollMetricsSnapshot` carries
  `ViewportFraction`/`Page`. `TabBarView` and `CalendarDatePicker` route through the new page view;
  the calendar now uses `PageView.Builder` over the whole `firstDate`..`lastDate` month range instead of
  rotating a three-page window.

- Breaking: closed the live-`ScaffoldGeometry`/FAB-motion divergence (`material_ui/scaffold.dart`,
  `floating_action_button_location.dart`). `Scaffold` now lays its slots out through the ported
  `_ScaffoldLayout` (`CustomMultiChildLayout` + `ScaffoldSlot`) instead of a Column/Stack, so
  `ScaffoldPrelayoutGeometry` carries measured snack-bar, bottom-sheet and material-banner sizes. New public
  `ScaffoldGeometry` + `Scaffold.GeometryOf` (paint-phase only, backed by the ported
  `_ScaffoldGeometryNotifier`), and the ported `_FloatingActionButtonTransition` drives entrance/exit/move
  scale, rotation and cross-fade; a `floatingActionButtonLocation` change now animates over the 400 ms segue
  and restarts from `GetAnimationRestart` when interrupted, relayouting through the delegate's `relayout`
  listenable without rebuilding. `BottomAppBar` tracks the moving FAB through that listenable. New `Scaffold`
  options: `ExtendBody`, `ExtendBodyBehindAppBar`, `ResizeToAvoidBottomInset`. Core gained
  `CompoundAnimation`/`AnimationMin`/`AnimationMax`/`AnimationMean`, `TrainHoppingAnimation`,
  `Animatable.Chain`, `Animation<double>.Drive` and `PipelineOwner.DebugDoingPaint`. **Breaking:**
  `FloatingActionButtonAnimator.GetScale`/`GetRotation` became `GetScaleAnimation`/`GetRotationAnimation`
  (`Animation<double>`); `FloatingActionButtonLocation.MiniButtonOffsetAdjustment` is Flutter's `4.0`, not
  `8.0`; a fixed `SnackBar` and a zero-elevation `MaterialBanner` now follow Flutter's overlay/`contentTop`
  placement instead of being Column children; and `ScaffoldState.FloatingActionButtonSize` is gone (the
  layout measures the button, so the estimate had no callers). Remaining deltas are tracked in
  `DIVERGENCES.md`.

- Breaking: closed the `Navigator` divergence end-to-end (`widgets/navigator.dart`). The navigator now runs
  Flutter's staged route lifecycle: `_RouteLifecycle`/`_RouteEntry` (as internal `RouteLifecycle`/`RouteEntry`),
  `_flushHistoryUpdates` with its observer queues (`didPush`/`didReplace` drain LIFO before `didPop`/`didRemove`
  drain FIFO), `_flushRouteAnnouncement`, deferred subtree-aware disposal, and the new `NavigatorObserver`
  `DidChangeTop`. Declarative routing landed: `Page`, `Navigator.Pages`/`OnDidRemovePage`, the full
  `_updatePages` page-diff, `RouteTransitionRecord`, `TransitionDelegate`/`DefaultTransitionDelegate`. Restoration
  landed: `Route.RestorationScopeId`, `_HistoryProperty`, named/anonymous `_RestorationInformation`, and the
  `RestorablePush*`/`RestorableReplace*` family; `NavigatorState` is now a `RestorationState` and its build
  composes `FocusTraversalGroup > Focus > UnmanagedRestorationScope > Overlay`. `_ModalScopeStatus` became an
  `InheritedModel` with the seven `ModalRouteAspect`s, adding `ModalRoute.CanPopOf`/`SettingsOf`/`IsActiveOf`/
  `IsFirstOf`/`PopDispositionOf`; `_ModalScopeState.build` now wraps the scope in a `RestorationScope` and uses
  `FocusScope.WithExternalFocusNode`. Core gained `FocusScopeNode.SetFirstFocus`, and `Focus(autofocus: true)`
  now defers to a scope that already has a focused child. **Breaking:** `Route.DidPop(Route? previousRoute)` is
  now `bool DidPop(object? result)`; `ModalRoute.RequestFocus` moved to `Route` as a non-nullable getter fed by a
  `requestFocus` constructor argument; `RouteSettings` is no longer `sealed`; initial routes are *added* rather
  than pushed, so their transition starts completed; and `didRemove`'s `previousRoute` now skips routes that are
  themselves leaving. New `Navigator` options: `TransitionDelegate`, `RequestFocus`, `ClipBehavior`,
  `RestorationScopeId`, `RouteTraversalEdgeBehavior`, `RouteDirectionalTraversalEdgeBehavior`,
  `ReportsRouteUpdateToEngine`. Remaining deltas are tracked in `DIVERGENCES.md`.

- Breaking: ported the state-restoration subsystem end-to-end (`services/restoration.dart`,
  `widgets/restoration.dart`, `widgets/restoration_properties.dart`). New `RestorationManager`/
  `RestorationBucket` in `Plumix.UI` (claim/adopt/rename/drop, duplicate-id detection, post-frame
  serialization, `isReplacing`, `flushData`), real `RestorationScope`/`UnmanagedRestorationScope`/
  `RootRestorationScope`, `RestorableProperty<T>`, the `RestorationMixin` equivalent
  `RestorationState : State`, and the full `Restorable*` property family (num/double/int/string/bool
  and their nullable forms, `DateTime`, `Enum`, `Listenable`, `ChangeNotifier`,
  `TextEditingController`). `FormField.restorationId` now persists `error_text` and
  `has_interacted_by_user`. Core gained `Scheduler.ScheduleMicrotask` and an
  `AddPostFrameCallback(..., scheduleFrame: false)` overload. **Breaking:** the placeholder
  `RootRestorationScope` is gone — `MaybeRestorationIdOf` was removed, the constructor takes
  `(restorationId, child)`, and `FormFieldState` now derives from `RestorationState`. Host transport,
  codec and first-frame-deferral deltas are tracked in `DIVERGENCES.md`.

- Breaking: closed the `Dialog` family divergence end-to-end. Material `Dialog`/`AlertDialog`/`SimpleDialog` now
  render on a real `Material(type: card)` surface with `AnimatedPadding`, `EdgeInsetsGeometry` slot paddings,
  `AlignmentGeometry` alignment, `Curves.Decelerate` inset animation, host-platform (`defaultTargetPlatform`) route
  labels, and icon-driven `TextAlign.Center` titles; `AlertDialog.Adaptive`/`ShowAdaptiveDialog` route to the new
  Cupertino dialog on iOS/macOS. `DialogRoute<T>` is rebuilt on the new core `RawDialogRoute<T>` (`PopupRoute` +
  `DisplayFeatureSubScreen` + scopesRoute semantics + `ShowGeneralDialog`): captured inherited themes, safe area,
  opaque-surface semantics, the source 150ms easeOut fade (`AnimationStyle`-overridable), and the shared barrier
  pipeline — its future now completes on pop, not after the exit fade. Ported Cupertino `dialog.dart` at the 3.47
  shape: `CupertinoAlertDialog` (`_PriorityColumn`/`_AlertDialogActionsLayout` `RenderFlex` subclasses, overscroll
  backgrounds, 270/310 widths, exact styles/colors), `CupertinoDialogAction`, blur+saturation `CupertinoPopupSurface`,
  sliding-tap press/slide/confirm targets, and `CupertinoDialogRoute`/`ShowCupertinoDialog` with the critically-damped
  spring (scale 1.3 fade-in, fade-only exit) via the new `TransitionRoute.CreateSimulation` hook. Core gained
  `TraversalEdgeBehavior` (Tab wraps in a closed loop per scope by default — **Breaking** for edge-stop assumptions),
  route `RequestFocus`/traversal-edge wiring in `ModalScope`, directional-edge handling, `RenderStack` intrinsics,
  reversed `AnimationController.AnimateWith`, and Cupertino gained elevation-aware `CupertinoDynamicColor`,
  `CupertinoUserInterfaceLevel`, `SystemRed`/`Separator`/`Label` colors. **Breaking:** `DialogThemeData.Alignment` is
  `AlignmentGeometry?`, `ActionsPadding` is `EdgeInsetsGeometry?`, `DialogTheme` is an `InheritedTheme`, and
  `MaterialDialogs.ShowDialog` replaced `transitionDuration` with `animationStyle` and gained
  `anchorPoint`/`traversalEdgeBehavior`/`requestFocus`. Remaining deltas (slide-vs-scroll arena, superellipse clip,
  high-contrast colors, legacy `DialogTheme` shims) are tracked in `DIVERGENCES.md`.

- Breaking: completed the strict `SearchAnchor`/`SearchBar` closeout. The search view now uses the source
  `PopupRoute` with the 600ms `easeInOutCubicEmphasized` grow/fade choreography from the anchor rect (navigator-
  relative geometry, LTR/RTL clamping, fullscreen top-padding lerp, interval-staggered icon/divider/list fades),
  `CapturedThemes` for local inherited themes, docked-close-on-resize, and the exact `_SearchBarDefaultsM3`/
  `_SearchViewDefaultsM3` tables on `Material` surfaces. `SuggestionsBuilder` is now async
  (`ValueTask<IReadOnlyList<Widget>>`, Dart `FutureOr`) with source dedupe/coalescing; `SearchViewTheme` is an
  `InheritedTheme` and both theme records use source types (`OutlinedBorder` shapes, `EdgeInsetsGeometry`
  paddings, the upstream `headerHintStyle` lerp quirk). Core gained `TextCapitalization`/`SmartDashesType`/
  `SmartQuotesType` in `TextInputConfiguration`/`EditableText` (moved from `Plumix.Material`), plus
  `scrollPadding`; `TextField` gained `textCapitalization`, `smartDashesType`/`smartQuotesType`, `onTapOutside`,
  `onTapAlwaysCalled`, and `scrollPadding`. **Breaking:** `SearchViewBuilder` was renamed `ViewBuilder`,
  `SearchController.CloseView` takes a required argument and `IsOpen`/`OpenView`/`CloseView` throw when detached,
  non-source constructor validation was removed, and `SearchAnchor.Bar` forwards `scrollPadding`/
  `contextMenuBuilder`.

- Moved the pinned Flutter parity revision from 3.44.0 to 3.47.0 (`4cf24164269`) and switched the
  Material/Cupertino source of truth to the extracted `material_ui`/`cupertino_ui` pub packages
  (pinned 1.0.0, code-identical to the SDK's frozen copies at these pins). `dart_sample` now imports
  `package:material_ui`/`package:cupertino_ui` (via `dart fix --code=migrate_design_widgets`); all
  material/cupertino parity markers were rewritten to `material_ui/lib/src/...` /
  `cupertino_ui/lib/src/...` and `generate_port_map.py` resolves them against the new
  `material-ui-src`/`cupertino-ui-src` symlinks. Six stale markers were fixed (`visibility.dart` →
  `indexed_stack.dart` rename plus five pre-existing wrong paths) and `PORT_MAP.md` regenerated
  clean. All 66 ported files that changed upstream were audited; the 40 behavior-bearing deltas are
  recorded as the re-port backlog in `docs/ai/notes/migration-2026-08-13-flutter-3.47-pin.md`.
  Two pre-existing `dart_sample` analyzer errors surfaced by the SDK update were fixed
  (`WidgetStateProperty.resolveWith` static-call form; `SearchDelegate<String?>` nullable result).

- Breaking: completed strict Material `MergeableMaterial` parity. The constructor now follows Flutter's field
  order and accepts arbitrary source-shaped gap/elevation values; keyed gap/chunk reconciliation, 200ms extent,
  corner and divider transitions, transparent slice materials, directional list-body layout, and one render-owned
  card shadow per connected slice group now match the pinned implementation. Focused tests and the mirrored Card
  demo cover live merge/separate choreography.

- Completed Cupertino text-selection toolbar parity: mobile/desktop surfaces, buttons, adaptive routing, overflow,
  spell-check suggestions, Cupertino theme/color primitives, and mirrored gallery probes are now available. Material
  adaptive toolbars select Cupertino controls on iOS/macOS, and the Android host registers the native default
  sentence spell checker through `DefaultSpellCheckService`. Rounded-superellipse/path-shadow/retained-clip backend
  limits remain documented in `DIVERGENCES.md`.

- Breaking: completed strict Material `ReorderableListView` parity. The public wrapper now preserves nullable
  padding and auto-scroll defaults, forwards anchor/drag/keyboard/restoration/clip contracts, resolves desktop
  cursors from dragged state, animates the default proxy elevation, and follows horizontal RTL axis direction.
  Shared scrolling now supports anchored viewport geometry and restoration-ID-keyed page-storage offsets; process
  restoration still awaits the framework restoration manager tracked in `DIVERGENCES.md`.

- Breaking: closed deferred-loading scroll parity. The widget root now exposes the raw platform view through
  `View.Of`/`View.MaybeOf`; the default physics threshold ignores nested `MediaQuery` overrides, and `JumpTo` plus
  pointer scrolling contribute their forced displacement as implied velocity until the next frame. The direct
  widget property is named `ViewHandle` because C# forbids a member named `View` on the `View` class.

- Breaking: completed the Material `BottomSheet`/`BottomSheetThemeData` direct-token and theme closeout. M3 sheets
  now read `surfaceContainerLow`/`onSurfaceVariant` from `ColorScheme` directly; drag-handle colors use the
  source-shaped `WidgetStateColor` contract, and bottom-sheet themes now provide exact copy/lerp, diagnostics, and
  inherited-theme capture. The mirrored demo exercises captured theme overrides and hover-state handle colors.

- Breaking: completed the Material date/range-picker direct-token and theme closeout. `DatePickerThemeData` now
  uses Flutter's `OutlinedBorder` state-shape contract, locale/copy/lerp surface, inherited-theme capture, and exact
  M2/M3 `ColorScheme`/`TextTheme` defaults; picker dialogs honor source theme precedence and range overlays, and
  `showDatePicker`/`showDateRangePicker` apply explicit or themed locale overrides.

- Breaking: completed strict Material `DataTable`/`DataTableThemeData` parity. Tables now resolve local and global
  theme fields in Flutter order, use direct `ColorScheme` row roles and source divider defaults, accept arbitrary
  decorations, compose through clipped transparent `Material`, merge ambient text styles, expose column-header
  semantics, and animate sort arrows over 150ms. Focused tests and mirrored demos cover M2/M3 roles, row states,
  theme fallback, layout, clipping, semantics, and sort transitions.

- Breaking: completed strict Material `SegmentedButton<T>`/`SegmentedButtonThemeData` parity. Expanded insets now
  use `EdgeInsets`, `styleFrom` accepts any `OutlinedBorder`, segment state controllers survive updates, and the
  source `Material`/`TextButtonTheme`/`TextButton` composition carries selected and enabled semantics. A dedicated
  render object now equalizes intrinsic sizes, honors 48px tap targets, mirrors RTL placement, clips segment shapes,
  and paints source dividers and mixed-state borders; focused tests cover defaults, style/theme precedence,
  lifecycle, selection content, semantics, layout and paint.

- Breaking: completed strict Material `ButtonBar`/`ButtonBarThemeData` parity. Button padding now remains
  directional through the legacy `ButtonThemeData`/`MaterialButton`/`RawMaterialButton` path; the bar uses
  Flutter's `RenderFlex`-based unconstrained probe, constrained row retry, vertical overflow and dry layout; and its
  theme now has source copy/lerp/diagnostics and validation. Focused tests and mirrored C#/Dart probes cover theme
  precedence, logical padding, LTR/RTL overflow alignment, spacing, direction, and constrained/padded sizing.

- Breaking: completed strict Material `Tooltip`/`TooltipThemeData` parity. Tooltip padding and margin now use
  directional `EdgeInsetsGeometry`, decoration accepts any `Decoration`, rich-message overlays remain interactive by
  default, cursor/text-direction/style composition matches Flutter, local tooltip themes participate in inherited
  theme capture, and theme copy/lerp follows the pinned source fields exactly. Core `Container` now resolves
  directional padding/margin, tooltip presentation emits `TooltipSemanticEvent`, and the framework has reusable
  diagnostic-property nodes for source-shaped `DebugFillProperties` output. Focused tests and the mirrored demo cover
  plain/rich pointer policy, arbitrary shape decoration, directional insets, semantic events, diagnostics, and theme
  copy/lerp behavior. Advanced `Plumix` and `Plumix.Material` to `0.2.0-alpha.1`.

- Breaking: closed `MenuAcceleratorLabel` parity by replacing its global deepest/latest Alt dispatcher with
  per-label `CharacterActivator` entries in the nearest `ShortcutRegistry`. Accelerator callbacks now participate
  in normal focus-local `Shortcuts` precedence, entries follow Alt/dependency/submenu/disposal lifecycle, and labels
  without a registrar remain display-only. The default builder now matches Flutter's direct `RichText`/ambient-style
  span composition. Focused coverage mirrors Flutter's marker table, submenu replacement, Apple policy, and zero-area
  layout; the paired dropdown demos include a focus-local Alt+N override probe.

- Breaking: closed the nonlinear text-scaling divergence. `MediaQueryData` now owns the exact `TextScaler` strategy,
  adds scaler-aware `CopyWith`, and keeps `TextScaleFactor` as a derived compatibility surface; `MediaQuery` adds
  scaler accessors and aspect-scoped dependencies, while its no-scaling/clamped wrappers preserve strategy behavior.
  `Text`/`RichText` retain their legacy scale-factor inputs with Flutter's mutual-exclusion rules, and `TabBar` now
  passes widget/theme/ambient scalers unchanged, including custom and clamped nonlinear implementations.

- Breaking: closed the `MenuAnchor` divergence by landing its three missing primitives. Core gained
  `IMenuSerializableShortcut`/`ShortcutSerialization` (`SingleActivator`/`CharacterActivator` now serialize for
  menus) and `MouseRegion.OnHover`; `MaterialLocalizations` gained the 47 `KeyboardKey*` strings; and
  `_LocalizedShortcutLabeler` is ported, so `MenuItemButton`/`CheckboxMenuButton`/`RadioMenuButton` take a
  display-only `shortcut` whose label renders between the trailing icon and the submenu arrow with the source
  per-platform modifier order, separator and Apple ⌃⌥⇧⌘ symbols. `MenuItemButton` and `SubmenuButton` now read hover
  from `MouseRegion.OnHover` (edge-detected) instead of `onEnter`/`TextButton.onHover`, matching Flutter's
  scroll-under focus behavior, and `MenuItemButton` invalidates the traversal scope after taking focus.
  **Breaking:** `ButtonStyle.Alignment` and every `styleFrom` `alignment` parameter widen from `Alignment?` to
  `AlignmentGeometry?`, and `_MenuButtonDefaultsM3.alignment` is now the source `AlignmentDirectional.CenterStart`,
  so a menu button's content aligns to the text-direction start and mirrors under RTL.

- Breaking: ported Flutter's `tabs.dart`, `tab_bar_theme.dart`, `tab_controller.dart` and `tab_indicator.dart`
  strictly. `TabBar` now composes `_TabStyle` + `_TabLabelBar` (a `RenderFlex` subclass reporting tab offsets) under a
  `CustomPaint` driven by the ported `_IndicatorPainter`, replacing the bespoke `RenderTabBar`; the M2/M3 primary and
  secondary default tables, `_ChangeAnimation`/`_DragAnimation`, `_TabBarScrollController`/`_TabBarScrollPosition`
  initial-offset correction, the elastic/linear indicator math, the scrollable M3 divider/`Align` wrapper and
  `_warpToNonAdjacentTab` staging are all source-shaped. New `UnderlineTabIndicator`, `TabBarScrollController`,
  `TabBar.Secondary`, `TabValueChanged<T>`, `TabBar.SplashFactory`/`TextScaler`/`TabHasTextAndIcon` and
  `TabBarThemeData.SplashFactory`/`TextScaler`. **Breaking:** `TabBar.Indicator` and `TabBarThemeData.Indicator`
  widen from `BoxDecoration?` to `Decoration?`; `LabelColor` becomes `WidgetStateColor?`; `Padding`/`LabelPadding`/
  `IndicatorPadding`/`Tab.IconMargin` become `EdgeInsetsGeometry`; `MouseCursor` is a plain `MouseCursor?`;
  `OnHover`/`OnFocusChange` take `TabValueChanged<bool>`; `TabController.AnimationValue` is replaced by
  `TabController.Animation` (null after dispose); `TabBar.ScrollController` is a `TabBarScrollController`; and M2/M3
  label colors, indicator weights (M3 primary label indicators are 3px and rounded), divider defaults and scrollable
  `startOffset`/`start` alignment now follow the Dart tables. Core gained `AnimationWithParentMixin<T>` and
  `BuildContext.Size`, `AnimationController.AnimateTo` no longer clamps unbounded controllers to `[0, 1]`, and
  `PageView` now dispatches `ScrollUpdateNotification`/`ScrollEndNotification` so `TabBarView` syncs its controller
  through Flutter's `_handleScrollNotification` instead of a page-controller listener.
