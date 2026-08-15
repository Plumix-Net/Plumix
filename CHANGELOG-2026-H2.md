# Changelog — 2026 H2 archive

Historical entries rotated from `CHANGELOG.md`.

## [Unreleased]

### Planned


- Breaking: ported Flutter's `_MenuLayout` and rebased the menu buttons on `TextButton`. Menu overlays are now placed
  by `MenuStyle.Alignment` resolved within the anchor rect (`MenuStyle.Alignment` widened from `Alignment?` to
  `AlignmentGeometry?`; the panel defaults are the source `AlignmentDirectional.BottomStart`/`TopEnd` instead of
  `Alignment.BottomLeft`/`TopRight`), with directional `alignmentOffset` mirroring, cascade-flip vs screen-clamp chosen
  by parent orientation, `MediaQuery` padding/view-insets deflation and `DisplayFeatureSubScreen` sub-screens.
  `MenuItemButton` is now stateful (own focus node, so `requestFocusOnHover` works without an external one) and both
  it and `SubmenuButton` build a `TextButton` over `_MenuButtonDefaultsM3` + `_MenuItemLabel`: square shape, 64x48
  minimum, 24 icon size, `onSurface`/`onSurfaceVariant` roles, the 0.08/0.1 overlay ladder and the density- and
  text-scale-driven `_scaledPadding`. **Breaking:** a `SubmenuButton` in a `MenuBar` no longer paints a submenu arrow
  (Flutter shows it only inside a vertical menu), the default arrow is `Icons.ArrowRight`, and menu button metrics,
  colors and label spacing all change. Core gained `Alignment.WithinRect`/`AlongSize`/`Inscribe`, public
  `AlignmentGeometry.IsDirectional` (with Flutter's mixed-lerp semantics, so `Alignment.Center` and
  `AlignmentDirectional.Center` are no longer equal) and public `DisplayFeatureSubScreen.SubScreensInBounds`.

- Breaking: closed the `InputBorder`/`InputDecorator` divergence and landed the two core primitives it needed.
  `RenderObject.ApplyPaintTransform` is now Flutter's protocol: `GetTransformTo`/`LocalToGlobal`/the new
  `GlobalToLocal` compose the parent chain instead of the semantics walk, so they also resolve inside subtrees
  hidden from semantics; `RenderBox`, `RenderTransform`, `RenderFittedBox`, `RenderFractionalTranslation`,
  `RenderRotatedBox`, `RenderFlow` and `RenderFollowerLayer` implement it, and `GetPaintOffsetToRoot` is no longer
  a translations-only approximation. Core semantics gained `SemanticsTag`, `SemanticsConfiguration.AddTagForChildren`/
  `TagsChildrenWith`, `SemanticsNode.Tags` and `ChildSemanticsConfigurationsResultBuilder`; `Semantics` takes
  `tagForChildren`. `UI/Path` gained `Combine(PathOperation, …)`. **Breaking:** `InputBorder` is now a `ShapeBorder`
  record (so `Dimensions` is an `EdgeInsetsGeometry`, `LerpFrom`/`LerpTo` take `ShapeBorder?`, and the removed
  `InputBorder.Lerp` is `ShapeBorder.Lerp` — which switches at the midpoint between unlike borders instead of scaling
  through it, and no longer short-circuits t=0/1); `UnderlineInputBorder` paints its rounded branch through
  `BoxBorder.PaintNonUniformBorder`; new `ShapedInputBorder` wraps an arbitrary `ShapeBorder`. `_RenderDecoration`
  records the label transform in its own coordinate space and overrides `ApplyPaintTransform`, so the floating label's
  global rect is now the painted one; the decorator ports Flutter's semantics visit order and the affix
  `childConfigurationsDelegate`, so prefix/suffix/prefixIcon/suffixIcon form sibling nodes with per-decorator ordinal
  sort keys (0/1/2) instead of merging into one label.

- Breaking: rebased `Navigator` on the ported `Overlay`. `Route` gained `OverlayEntries`/`ChangedInternalState`/
  `ChangedExternalState`/`HasActiveRouteBelow`, the new `OverlayRoute` owns `CreateOverlayEntries`/`FinishedWhenPopped`,
  and `ModalRoute` now installs Flutter's `[barrier, scope]` entry pair with `Filter` (`BackdropFilter` on the barrier),
  `MaintainState`, `Offstage`, `CanPop` and the `_ModalScope` composition (`Offstage`/`PageStorage`/`Actions`+
  `DismissModalAction`/`PrimaryScrollController`/`FocusScope`/`RepaintBoundary`/transitions over a cached page).
  `TransitionRoute` drives `overlayEntries.first.opaque` from its animation status; `NavigatorState` exposes `Overlay`
  and `UserGestureInProgressNotifier`, rearranges entries to history order, and defers route disposal until the entries
  unmount. **Breaking:** routes below an opaque route now stay mounted (`maintainState` defaults to `true`) instead of
  being dropped from the tree, and they are no longer rebuilt by a push; `PageRoute`/`PageRouteBuilder`/`PopupRoute`
  take `maintainState`/`filter`; `ImpliesAppBarDismissal` is per-route (`HasActiveRouteBelow`) rather than
  `Navigator.CanPop`. Fixes: `OverlayEntry.Opaque` is settable before insertion and theater children carry the entry
  identity at both levels; focus traversal descends into nested scopes; `BuildOwner` no longer keeps dirty elements in
  a depth-ordered set (reparenting could corrupt it and crash a flush).

- Breaking: ported `material/time_picker.dart` and `material/time_picker_theme.dart` strictly. The dialog is now
  Flutter's widget tree — `_TimePickerModel` (aspect-based `InheritedModel`), `_DialTimePickerHeader`,
  `_DialTimeSelectorControl`/`_DialHourControl`/`_DialMinuteControl`, `_TimeSelectorSeparator`, `_DayPeriodControl`
  with `_AmPmButton` and `_RenderInputPadding`, `_TimePickerInput`/`_HourMinuteTextField`, and `_Dial`/`_DialPainter`
  with the source theta/radius math, shortest-path animation, inner/outer 24-hour ring and selector-dot label clip.
  `_TimePickerDefaultsM2`/`_TimePickerDefaultsM3` carry the exact M2/M3 tables (including the entry-mode-dependent
  `hourMinuteTextStyle` and both `inputDecorationTheme`s), and the dialog reproduces Flutter's size tables, minimum
  sizes, text-scale clamp, tap-target offset and `AnimatedContainer` resize.
  **Breaking:** `TimePickerThemeData` now matches Dart's field types — `DayPeriodColor`/`DayPeriodTextColor`/
  `DialTextColor`/`HourMinuteColor`/`HourMinuteTextColor` are `WidgetStateColor?` (a plain `Color` is auto-wrapped to
  selected-only, as in Dart), `DayPeriodShape` is `OutlinedBorder?`, and `Padding` is `EdgeInsetsGeometry?`;
  `TimePickerDialog.SwitchTo*EntryModeIcon` are `Icon?`; `TimePickerTheme` is an `InheritedTheme`.
  New primitives: `TextScaler.Clamp`, `WidgetStateColor.IsConstantColor`, `HapticFeedback.Vibrate`,
  `RenderObject.GetPaintOffsetToRoot`, `Semantics`/`RenderSemanticsAnnotations` increase/decrease values and actions,
  `TextFormField` `keyboardType`/`textInputAction`, and the `timePickerHourModeAnnouncement`/
  `timePickerMinuteModeAnnouncement` localizations.

- Breaking: ported Flutter's rich-text span model and rebased the paragraph stack on it —
  `painting/inline_span.dart`, `text_span.dart`, `placeholder_span.dart`, `text_scaler.dart`,
  `widgets/widget_span.dart`, `RichText` from `widgets/basic.dart`, and `Text`/`Text.rich` from
  `widgets/text.dart`. `InlineSpan`/`TextSpan`/`PlaceholderSpan`/`WidgetSpan` carry the source
  traversal, `ToPlainText`, `CodeUnitAt`, affinity-based `GetSpanForPosition`, semantics information
  with spell-out/locale attributes, and `CompareTo`. `RenderParagraph` is now a multi-child render
  object driven by an `InlineSpan`: the `RenderComparison` setter switch, styled runs and inline
  placeholders through one Avalonia `TextLayout`, the six `PlaceholderAlignment` rules, span hit
  testing that adds the hit `TextSpan` as the hit-test entry, and `AssembleSemanticsNode` with
  per-run `OrdinalSortKey` nodes plus tap/long-press actions.
  **Breaking:** `RenderParagraph.Text` is an `InlineSpan` (the flattened string moved to `PlainText`),
  `Text` is a `StatelessWidget` that builds `RichText`, and text scaling now lives on
  `RenderParagraph.TextScaler` instead of being folded into `FontSize`.
  Closes the `Tooltip.richMessage` divergence (Material `Tooltip` takes `message` or `richMessage`
  with the source mutual-exclusion guard) and the `RichText` half of the `MenuAccelerator` one (the
  default label is one paragraph whose accelerator run carries the underline).
  New primitives: `RenderComparison`, `TextStyle.CompareTo`, `TextScaler`, `PlaceholderDimensions`,
  `PlaceholderAlignment`, `TextParentData`, `RenderInlineChildrenContainerDefaults`, `IHitTestTarget`
  (now the `HitTestEntry` target type) and `IMouseTrackerAnnotation`.

- Breaking: moved Flutter's stateful menu-anchor tree into core and closed the `RawMenuAnchor`/`MenuAnchor`
  divergence. `widgets/raw_menu_anchor.dart` is now a strict port: `MenuController` (subclassable, with
  `Open(position)`/`Close`/`CloseChildren`/`IsOpen`/`MaybeOf`/`MaybeIsOpenOf`), the shared anchor/group state
  with parent/child registration, sibling exclusivity, root-anchor ancestor-scroll and view-size closure, the
  intercepted `onOpenRequested`/`onCloseRequested` protocol, `DismissMenuAction`, and the six-entry traversal
  shortcut map. Material `MenuAnchor` now sits on top of it: `_MenuAnchorScope`, the `_Submenu`/`_MenuPanel`
  composition (menu `FocusScope`, `Actions`+`Shortcuts`, `ScrollConfiguration`/`PrimaryScrollController`),
  the source eight-entry shortcut map, `_MenuDirectionalFocusAction`'s open/close/traverse behavior on
  `SubmenuButton`, the staggered per-item fades, the completion-gated panel `Scrollbar`, and `MenuBar` built
  on `RawMenuAnchorGroup`.
  **Breaking:** `MenuController` moved from `Plumix.Material` to `Plumix.Widgets` and no longer derives from
  `ChangeNotifier`; `Open`/`CloseChildren` throw when detached (`Close` stays silent). `RawMenuAnchor` is a
  `StatefulWidget` taking a `MenuController` (`RawMenuAnchorController` is gone), `RawMenuAnchorGroup` requires
  a controller, `MenuStyle.Padding` moved from `Thickness` to `EdgeInsetsGeometry`, `MenuAnchor.ReservedPadding`
  likewise, `MenuAnchor.AnchorTapClosesMenu` was dropped, and `MenuItemButton.OnPressed` now runs in a
  post-frame callback (Flutter restores focus first).
  New primitives: `TraversalDirection`, `DirectionalFocusIntent`/`Action`, `NextFocusIntent`/`Action`,
  `PreviousFocusIntent`/`Action`, `RequestFocusIntent`/`Action`, `FocusTraversalPolicy.InDirection`/
  `FindFirstFocus`/`FindLastFocus`/`InvalidateScopeData`, `FocusNode.NextFocus`/`PreviousFocus`/
  `FocusInDirection`/`HasPrimaryFocus`, `FocusScopeNode.HasFocusInScope`, `Scheduler.Phase` with
  `SchedulerPhase`, `Curves.TweenCurve`, and `EdgeInsetsGeometry.Clamp`/`Infinity`.

- Breaking: ported the `painting` border hierarchy strictly — `borders.dart`, `box_border.dart`,
  `rounded_rectangle_border.dart`, `stadium_border.dart`, `circle_border.dart`, `oval_border.dart`,
  `beveled_rectangle_border.dart`, `continuous_rectangle_border.dart`, `linear_border.dart`,
  `star_border.dart` and `shape_decoration.dart`. `ShapeBorder` is now the abstract Flutter class
  (`Dimensions`, `Add`/`operator +`, `Scale`, `LerpFrom`/`LerpTo`/`Lerp`, `GetOuterPath`/`GetInnerPath`,
  `PreferPaintInterior`/`PaintInterior`, `Paint`) with `CompoundBorder`, `OutlinedBorder` and the concrete
  shapes, including the private stadium/rounded-rect-to-circle interpolators and `StarBorder`'s conic path
  generator. `BoxBorder`/`Border`/`BorderDirectional` replace the old four-side record, and `ShapeDecoration`
  is Flutter's decoration (padding, `FromBoxDecoration`, hit testing, clip path, shape-driven paint).
  **Breaking:** `ShapeBorder.RoundedRectangle`/`Circle`/`Stadium`/`Border` factories are gone in favor of the
  real types; `BoxDecoration.Border` is a `BoxBorder?` and `BorderSides` was removed; `ButtonStyle.Shape`,
  `MenuStyle`, `SegmentedButton` and the list-tile controls take `OutlinedBorder`; `BorderSide.None` is black
  (was transparent) and `BorderSide.Scale` no longer carries `strokeAlign`. `Material` now paints through
  `ShapeDecoration` and clips with `ShapeBorderClipper`, so arbitrary shapes reach clips, hit tests and the
  bottom-app-bar notch.
  New primitives: `Path.AddRRect`/`AddPolygon`/`ConicTo`/`Reset`/`Transform`/`GetBounds`, `RRect.ShortestSide`/
  `InflateEdges`/`DeflateEdges`, `EdgeInsetsGeometry.Add`, `BorderRadiusGeometry * double`, `BorderSide.Merge`/
  `CanMerge`/`ToPen`, and `PaintingContext.DrawRRect`/`DrawDRRect`/`DrawOval`/`DrawPath`.

- Breaking: ported `material/input_decorator.dart` and `material/input_border.dart` strictly. The decoration is
  now laid out by `RenderDecoration` (`RenderInputDecoration.cs`), a slotted render object carrying Flutter's
  `_layout`/`performLayout` verbatim — baseline-driven slot placement, `_interpolateThree` outline alignment,
  container/subtext split, intrinsics, dry layout and dry baselines. `InputBorder`/`UnderlineInputBorder`/
  `OutlineInputBorder` moved to `InputBorder.cs` with the real paint math (`_gapBorderPath` arcs, `strokeOffset`
  inflation, bottom-radius clamping, `lerpFrom`/`lerpTo`/`scale`/equality). `_BorderContainer`, `_HelperError`,
  the affix opacity fades, the shaking label and the M2/M3 defaults (fill, indicator/outline sides, label/hint/
  helper/error styles, the full `contentPadding` table, the M3 input gap) are ported too.
  **Breaking:** `InputDecoration` is a record with `init` properties, `ContentPadding` moved from `Thickness` to
  `EdgeInsetsGeometry`, `InputBorder.CopyWith` takes a nullable side, per-state border slots (`disabledBorder`,
  `errorBorder`, …) are used verbatim instead of being state-resolved (only `border` resolves an
  `IStateInputBorder`), and `Hovered` is masked while disabled. `InputDecorationThemeData` gained
  `ActiveIndicatorBorder`, `OutlineBorder`, `VisualDensity`, `HintFadeDuration`, `AlignLabelWithHint`, the icon
  constraints and `Merge`; `InputDecorator`/`TextField` gained `textAlignVertical`, and `TextField` no longer
  forces `expands` for multiline.
  New core primitives: `TextAlignVertical`, `TextStyle.Merge`, `Listenable.Merge`, `RRect` (with `ScaleRadii`/
  `Inflate`/`ToPath`), `Path.AddArc`/`ArcTo`/`AddPath` plus open-contour stroking, `BorderSide.StrokeAlign`/
  `StrokeInset`/`StrokeOutset`/`StrokeOffset`/`Lerp`/`Scale`/`CopyWith`, `Radius.Clamp`, `BorderRadius * double`
  and `BorderRadius.ToRRect`.

- Closed the `RenderTable` semantics divergence: the shared pipeline gained Flutter's
  `RenderObject.AssembleSemanticsNode` hook (called for every semantic boundary, default annotates the node and
  adds the children), `SemanticsNode.UpdateWith` with public `Rect`/`IndexInParent` mutation, and
  `RenderObject.ClearSemantics` (recursive) plus the non-recursive `ClearOwnSemantics` used by `Detach`.
  `RenderTable` now synthesizes one semantics node per non-empty row (`SemanticsRole.Row`, `IndexInParent`, row
  box geometry) and wraps a cell in a `SemanticsRole.Cell` node when it produced several nodes or a node whose
  role is neither `Cell` nor `ColumnHeader`; cells narrower than their column edge are skipped, children are
  bucketed by geometry with an id-to-index map, and row/cell nodes are reused across passes and released on
  detach. Remaining pipeline-level gap (no per-node transform, no `showOnScreen` node callback): see
  `docs/ai/DIVERGENCES.md`.

- Breaking: closed the Material `BottomSheet`/`Scaffold` divergence against `DraggableScrollableSheet`, and moved
  modal-barrier ownership into `ModalRoute`. `BottomSheet` and the scaffold's `_StandardBottomSheet` now listen for
  `DraggableScrollableNotification`, so a draggable-scrollable child closes the sheet at its minimum extent
  (unless `shouldCloseOnMinExtent` is false), drives the new `Scaffold.bottomSheetScrimBuilder` body scrim
  (`max(0.1, 0.6 - extentRemaining * 3)` black by default) and shrinks the floating action button through
  Flutter's `extentRemaining * 3` visibility curve. A `Scaffold.bottomSheet` is wrapped in a
  `DraggableScrollableActuator` and registers a `LocalHistoryEntry` once dragged past its initial extent, so back
  resets the sheet instead of closing it.
  **Breaking:** `ModalRoute` now owns the barrier: `BarrierColor`/`BarrierDismissible`/`BarrierLabel`/`BarrierCurve`/
  `SemanticsDismissible` and an overridable `BuildModalBarrier()` build a barrier painted below the page, outside the
  route's transition, wrapped in `IgnorePointer` while the route animates out and sorted after the page
  (`OrdinalSortKey(1.0)` versus `0.0`). Every `ModalRoute` therefore contributes a barrier, and modal routes block
  the semantics of the routes below them. `ModalBottomSheetRoute`, `DialogRoute`, `PopupMenuRoute` and
  `DropdownRoute` stopped composing their own barriers in `BuildPage`; `DialogRoute.BarrierColor` is now `Color?`.
  Core semantics gained `SemanticsHitTestBehavior` (`Defer`/`Opaque`/`Transparent`) on `Semantics`,
  `RenderSemanticsAnnotations`, `SemanticsConfiguration` (with Flutter's absorb/compatibility rules) and
  `SemanticsNode`; the modal bottom sheet marks its page opaque so taps inside it never reach the barrier.
  `Scaffold` builds its overlay stack unconditionally and keys the snackbar/scrim/sheet/banner slots, so a slot
  appearing no longer rebuilds the body's elements (which re-registered its heroes and detached sheet controllers).
  `DraggableScrollableSheetTests` joined the serial scheduler collection; it drives the process-wide frame clock and
  could be rewound by another class mid-animation.

- Breaking: ported `widgets/draggable_scrollable_sheet.dart` — `DraggableScrollableSheet`,
  `DraggableScrollableController`, `DraggableScrollableNotification` and `DraggableScrollableActuator`, with
  Flutter's extent math, drag-versus-list hand-off, constant-velocity snapping (including `snapAnimationDuration`),
  implied min/max snap sizes, ballistic hand-off velocity boost, and the `hasDragged`/`hasChanged` rules that decide
  when a new `initialChildSize` moves the sheet.
  Supporting core primitives: `ScrollPosition.Absorb` plus virtual `ApplyUserOffset`/`GoBallistic`/`GoIdle`/
  `BeginActivity`/`Drag`, `ScrollPosition.NotificationContext`, virtual `ScrollController.Attach`/`Detach`,
  `AnimationController.Unbounded`/`AnimateWith`/`Velocity`, and `ChangeNotifier.HasListeners`.
  **Breaking:** a `Scrollable` replacing its `ScrollPosition` (physics or controller change) now absorbs the old
  position's pixels, extents, activity and in-flight drag instead of resetting to the stored offset.
  Test classes no longer run in parallel: the frame clock is process-wide, so concurrent classes could rewind each
  other's tickers mid-animation.

- Fixed `ModalBarrier` resolving its target platform from the host OS instead of `PlatformDefaults.TargetPlatform`,
  so barrier semantics (label, tap/dismiss actions, `SemanticsClipper`) ignored
  `PlatformDefaults.DebugTargetPlatformOverride` and varied by the machine running the tests. The private
  `ModalBarrierTargetPlatform` enum is gone; `PlatformSupportsDismissingBarrier` now takes `TargetPlatform`.
  Modal barrier/dialog/bottom-sheet semantics tests pin the platform instead of branching on `OperatingSystem`.

- Breaking: closed the `Table`/`RenderTable` divergence with a strict port of `rendering/table.dart`,
  `rendering/table_border.dart` and `widgets/table.dart`. The full `TableColumnWidth` algebra is available
  (`FlexColumnWidth`, `FractionColumnWidth`, `MaxColumnWidth`, `MinColumnWidth` join the existing fixed/intrinsic
  modes) and column sizing now runs Flutter's exact flex-grow/deficit-shrink algorithm instead of an approximation.
  `RenderTable` stores its cells as a flat row-major grid with `SetFlatChildren`/`SetChildren`/`AddRow`/`SetChild`/
  `Column`/`Row`, supports null cells, implements every intrinsic, dry-layout and dry-baseline path, paints arbitrary
  row `Decoration`s through cached `BoxPainter`s, and declares the `SemanticsRole.Table` boundary.
  `Table` now uses Flutter's `TableElement`, reconciling one `TableRow` at a time so keyed rows keep their state, and
  rejects irregular/empty rows and duplicate row or cell keys. **Breaking:** `Table.defaultColumnWidth` defaults to
  `FlexColumnWidth()` (was `IntrinsicColumnWidth()`), `TableRow.decoration` takes `Decoration` (was `BoxDecoration`),
  and `TableBorder` sides are non-nullable `BorderSide.None`-defaulted with `TableBorder.All(color:, width:, style:,
  borderRadius:)`/`Symmetric`/`Scale`/`Lerp` replacing `All(BorderSide)`.
  `PaginatedDataTable` restores its page index from `PageStorage`, and `ScrollView`/`SingleChildScrollView` only
  insert `PrimaryScrollController.None` when a primary controller was actually resolved.

- Breaking: completed the strict Material `BottomSheet`/`showModalBottomSheet`/`showBottomSheet` closeout. The sheet
  surface is now a real `Material` (elevation, surface tint, shadow, shape, clip) and the M3 default shape is
  top-only 28px corners instead of a uniform radius; drag handles resolve their color through hovered/dragged
  `WidgetState`s and are ordered before the content in the stack; drag release uses Flutter's fling/threshold math and
  ignores drags while the sheet is closing. `ModalBottomSheetRoute<T>` is now a `PopupRoute` driven by the route's own
  transition controller (`transitionDuration`/`reverseTransitionDuration`, caller-supplied controllers are never
  disposed), composes `AnimatedModalBarrier` with the localized `scrimLabel`/`scrimOnTapHint` and barrier-semantics
  clipping, animates through `ProxyAnimation`/`CurvedAnimation` with `Split` on drag release, and accepts
  `anchorPoint`. Scaffold-hosted sheets grow with `Align.heightFactor` on `fastOutSlowIn` rather than translating.
  New core primitives: `DisplayFeatureSubScreen` + `MediaQueryData.RemoveDisplayFeatures`, `PopupRoute`,
  `TransitionRoute.WillDisposeAnimationController`, `Curves.Split`/`EaseOutCubic`/`LegacyDecelerate`, and
  `MouseRegion.Opaque` (Flutter's `true` default, so mouse regions now hit-test themselves).
  `BottomSheet.CreateAnimationController` takes a ticker provider first and sets `ReverseDuration`;
  `MaterialLocalizations` gained `ScrimLabel`/`BottomSheetLabel`/`ScrimOnTapHint`.

- Breaking: closed the `ScrollPhysics` gesture-tuning divergence. `AlwaysScrollableScrollPhysics` and
  `NeverScrollableScrollPhysics` are ported, `ScrollPhysics.RecommendDeferredLoading` plus
  `ScrollPosition.RecommendDeferredLoading`/`Scrollable.RecommendDeferredLoadingForContext` are available, and
  `ShouldAcceptUserOffset` now registers or removes the scrollable's drag recognizers (and gates wheel scrolling)
  instead of being ignored. Drags run through a ported `ScrollDragController`/`HoldScrollActivity` pair, so iOS
  carried momentum and the 3.5px drag-start motion threshold apply; `DragGestureRecognizer` gained
  `OnDown`/`MinFlingDistance`/`MinFlingVelocity`/`MaxFlingVelocity` with Flutter's `considerFling` gate, so a release
  under the physics' fling floor now reports zero velocity and the reported fling is axis-projected and clamped.
  A pointer that never becomes a drag now reports one cancel, and `MediaQueryData.PhysicalSize` was added.

- Fixed `RenderTransform.EffectiveTransform` composing the alignment anchor in Flutter's column-vector order while
  Avalonia matrices are row-vector based, so every aligned `Transform` (`ScaleTransition`, `RotationTransition`,
  `MatrixTransition`, `RefreshProgressIndicator`) rotated/scaled around a mirrored anchor instead of the alignment
  point. Most visibly, the pull-to-refresh arrow flew outside its indicator circle while rotating.

- Breaking: completed the strict `BouncingScrollPhysics` (iOS rubber-band scrolling) closeout. A new
  `Plumix.Physics` library ports `Simulation`/`Tolerance`/`FrictionSimulation`/`SpringDescription`/`SpringSimulation`/
  `ScrollSpringSimulation`/`ClampedSimulation` with Flutter's exact math, plus `BouncingScrollSimulation` and
  `ClampingScrollSimulation`. `ScrollPhysics` gained the full source surface (`ApplyTo`/`Spring`/`ToleranceFor`/
  fling limits/`CarriedMomentum`/`AdjustPositionForNewDimensions`), `RangeMaintainingScrollPhysics` is now the real
  algorithm, `ScrollPosition.SetPixels` returns overscroll instead of clamping, ballistic activities follow the
  simulation and re-settle through `GoBallistic`/`ApplyNewDimensions`, and the viewports keep out-of-range offsets so
  the overscroll is visible. Pointer (wheel) scrolling follows the source rule and clamps its target into range, so
  only drags and flings rubber-band. `Simulation`/`FrictionSimulation` moved from `Plumix.Rendering` to `Plumix.Physics` and
  `FrictionSimulation`'s `drag` now has Flutter's meaning; `CarouselScrollPhysics` moved to `ScrollSpringSimulation`.

- Breaking: completed the strict Material `AppBar` closeout. The standard app bar now uses direct M2/M3
  `ColorScheme` roles, state-resolving scrolled-under surfaces/elevation, source `Material`/`NavigationToolbar`
  composition, visual configuration fields, system-overlay policy, and semantic ordering. Shared widget-state color
  and ordinal semantics-sort primitives, focused coverage, and the mirrored scroll-under demo probe were added.

- Breaking: completed the strict Material `Drawer` ColorScheme/theme closeout. The control now uses direct M3
  `surfaceContainerLow`, exact M2/M3 surface/shadow/tint/elevation defaults, direction-aware inner-edge shapes,
  source-shaped theme copy/lerp/capture, host-platform route semantics, and zero-width-compatible constraints.
  The source-ordered constructor, focused Flutter-test coverage, and mirrored runtime probe were updated.

- Breaking: completed the strict Material `PopupMenu` closeout. The family now uses source-shaped inherited-theme
  capture, directional padding, direct M2/M3 surface and label roles, navigator-owned route transitions, display-
  feature-aware placement, selected-item scrolling, stateful cursors, and zero-area-safe checked entries. Focused
  Flutter-test coverage and the mirrored M2/M3/directional-theme demo probes expanded.

- Breaking: completed the strict Material `Slider`/`RangeSlider` shape closeout. The family now exposes and
  executes Flutter-shaped track, thumb, overlay, tick, and value-indicator contracts; `SliderThemeData` carries
  the source fields/copy/lerp behavior, range selection and separation are pluggable, and `Slider.adaptive` routes
  Apple platforms through the new `CupertinoSlider`. Focused coverage and the mirrored custom-thumb probe expanded.

- Breaking: completed the strict Material `Autocomplete` closeout. The wrapper now matches Flutter's field/options
  composition, sizing, scrolling, selection, overlay, and option semantics; shared M2/M3 canvas, focus, and shadow
  defaults are source-shaped. Focused Flutter-test coverage and the mirrored live M2/M3 demo probe were expanded.

- Breaking: completed the strict Material `FlexibleSpaceBar` closeout. The control now uses the source stateful/
  layout-builder composition, logical title padding and scaled-width constraint, exact collapse and M2/M3 title
  rules, all zoom/blur/fade stretch modes, repaint-aware background opacity, and strict settings extents. Focused
  coverage and the mirrored SliverAppBar stretch-mode demo were expanded.

- Breaking: completed the strict Material `ToggleButtons` closeout. Direct `ColorScheme` defaults, state-resolving
  fills, exact checked/theme/TextButton composition, axis-aware tap targets, adjacent border ownership, intrinsic/
  baseline layout, RTL/vertical paint, and elliptical corner clipping now match Flutter; focused coverage and the
  mirrored state-fill probe were expanded.

- Breaking: closed the `RadioGroup` traversal divergence. Shared focus traversal groups now apply Flutter's stable
  geometry/bidi reading order and nested policies; radios use source shortcut-manager composition, selected-only Tab
  entry, enabled-only wrapping arrows, Space toggling, and non-radio shortcut fall-through with focused coverage.

- Breaking: completed the strict Material `Stepper` ColorScheme/API closeout. The control now uses direct M2/M3
  roles, `WidgetStateProperty`, directional inset APIs, `BoxBorder`, framework linear gradients, exact icon/error
  transitions and connector geometry, with expanded focused coverage and mirrored runtime probes.

- Breaking: closed the shared intrinsic/dry-layout divergence. `RenderBox` now caches and invalidates intrinsic,
  dry-layout, and nullable baseline queries with relayout-boundary propagation; flex, rotated box, flow, image,
  custom layout, fill/header slivers, intrinsic widgets, and extended-FAB overflow now use direct source algorithms.

- Breaking: completed the strict Material action-button closeout. Back, close, drawer, and end-drawer buttons now
  use the source `IconButton` inheritance/composition, standard-component keys, default-platform Android labels,
  direct M3 `onSurfaceVariant` and legacy M2 icon colors, plus source-shaped action-icon theme copying. Focused
  coverage and the mirrored M2/M3 scheme probe were expanded.

- Breaking: completed the Material chips ColorScheme/theme closeout. Exact M2 derived-color alpha behavior,
  source-shaped `ChipThemeData.copyWith`/lerp null-endpoint rules, shared icon-theme interpolation, and inherited-theme
  capture now match Flutter; focused coverage and the mirrored local-theme demo were updated.

- Breaking: completed the strict Material `ExpansionTile` closeout. The control and theme now expose source-shaped
  directional geometry, `ShapeBorder`, shared `AnimationStyle`, state-controller, and semantics APIs; direct M2/M3
  roles, exact `Expansible`/`ListTileTheme` composition, per-side border paint, controller lookup, `PageStorage`
  restoration, disabled/programmatic behavior, and mirrored live scheme probes now match the pinned Flutter source.

- Breaking: completed the strict Material `Radio`/`RadioListTile` closeout. The family now uses the shared
  `RawRadio` toggleable path, direct M2/M3 scheme roles, exact state/theme precedence, source painter geometry and
  timing, density-adjusted targets, expanded theme/list-tile APIs, adaptive registry behavior, and merged semantics.
  Added focused parity coverage and a mirrored live M2/M3 plus disabled-state demo probe.

- Breaking: closed the Material chips render divergence. `RawChip` now uses Flutter's three-slot intrinsic/dry
  layout, mirrored avatar/label/delete geometry and hit routing, painted checkmarks/scrims, enabled-state fading,
  minimum delete semantics bounds, and independent forward/reverse selection/avatar/delete/enable animation styles.
  Stateful chip sides/shapes and `ChipThemeData.fromDefaults` are source-shaped, with focused parity coverage.

- Breaking: completed the strict Material `ListTile` closeout. The widget and theme now use source-shaped M2/M3
  defaults, state resolution, ink/semantics/SafeArea composition, directional padding, and a dedicated slotted
  render object with intrinsic/dry layout. Added focused coverage and a mirrored M2/M3 demo probe.

- Breaking: completed the strict Material `Scrollbar` closeout. The theme now uses Flutter's
  `WidgetStateProperty` API and direct `ColorScheme.onSurface` roles; public painter/state extension contracts,
  controller validation, fade/hover/track motion, exact margin geometry, and adaptive Cupertino dark/resize/haptic
  behavior are covered, with a mirrored state-theme demo probe.

- Breaking: completed the strict Material `Switch` closeout. M2/M3 and adaptive defaults now read the exact direct
  roles, state colors, geometry, and 140/200/300 ms motion paths; thumb images, cursor, drag-start, padding, adaptive
  theme policy, theme copy/lerp, and source precedence are covered, with a mirrored M2/M3 demo probe.

- Breaking: completed the strict `ExpandIcon` closeout. Directional padding, half-turn transition composition,
  M2/M3 enabled and disabled colors, callback/state behavior, and action-specific semantic hints now match Flutter;
  shared IconButton state fallback and opacity rounding were corrected, with focused tests and a mirrored demo probe.

- Breaking: completed the `LinearProgressIndicator`/`CircularProgressIndicator` ColorScheme and API closeout.
  Defaults now read direct M2/M3 roles; the shared theme, controller precedence, circular padding, adaptive path,
  constructor contracts, and progress semantics are source-shaped. Added focused coverage and mirrored padding probes.

- Breaking: closed the shared Material ink-ownership divergence. `Material` now owns ordered descendant ink
  features, `Ink` decorations and responses paint beneath Material children, rapid splashes fade independently,
  pressed/hover/focus highlights use source timing, nested responses coordinate press ownership, and circular
  materials use oval clipping. Added focused source-test coverage and a mirrored timed-hover/rapid-tap demo probe.

- Breaking: completed the strict Material `Checkbox` closeout. The control now uses shared Flutter-shaped toggleable
  state and custom-paint geometry, direct M2/M3 `ColorScheme` defaults, mixed semantics, stateful sides, outlined
  shapes, cursor/density/theme copy/lerp APIs, exact tap-target and transition timing, focused source-test coverage,
  and a mirrored M2/M3 plus local-theme demo probe.

- Breaking: closed the remaining Apple page-transition divergence. Cupertino routes now use leading-edge drag
  ownership, linear finger tracking, exact velocity/position settle rules and timing, directional parallax and edge
  shadow paint, balanced navigator gesture callbacks, and LTR/RTL focused coverage.

- Breaking: completed the pinned Material page-transition closeout. Android now defaults to
  `PredictiveBackPageTransitionsBuilder`; shared/fullscreen predictive peek, cancel, commit, display-corner radii,
  exact fade/zoom timing, delegated transitions, retained subtree snapshots, and route snapshot permissions are
  framework-owned. Android 14+ now forwards native predictive-back progress, with focused source-test coverage and
  the mirrored nested-navigation demo updated to use Material routes.

- Breaking: completed the strict `ExpansionPanel`/`ExpansionPanelList` closeout. Public constructor ordering,
  salted keys, directional header geometry, exact `InkWell`/`IgnorePointer`/`ExpandIcon` composition, independent
  header/body/gap animations, radio ownership, callback ordering, colors, and focused source-test coverage now match.

- Completed the strict `GridTile` Dart closeout. The constructor now enforces the source non-null child contract,
  while focused coverage locks the exact direct-child and ordered fill/header/footer `Stack` composition.

- Breaking: completed the legacy and locale-aware Material typography foundation. `Typography.material2014`/
  `material2018`, exact platform color/font themes, dense/tall script geometry, localized `Theme.of` merging, M2/M3
  `ThemeData` selection, and the expanded Flutter-shaped `TextStyle` metadata now match the pinned source.

- Breaking: completed the strict `SearchDelegate` closeout. Its transition contract is now the source-shaped
  `Animation<double>` proxy backed by the shared 300 ms page-route fade; search fields forward keyboard type,
  action, correction, and suggestion configuration through editable/platform input, and search-input semantics,
  keyed body cross-fades, theme defaults, focused coverage, and the mirrored demo now match Flutter.

- Breaking: completed the `MaterialBanner` ColorScheme/theme closeout. M2/M3 surfaces and M3 divider now read
  direct scheme roles, local banner themes participate in inherited-theme capture, and entrance/exit composition
  uses shared Flutter-shaped threshold/vector animation primitives. Added focused coverage and a mirrored M2/M3
  direct-scheme demo probe.

- Breaking: completed the strict `DrawerHeader`/`UserAccountsDrawerHeader` closeout. Account surfaces now read
  `ColorScheme.primary`, directional insets and generic decorations match Flutter, and pictures/details use the
  source stack/custom-layout/ink/animation composition. Core icon labels and semantics container/merge behavior
  now follow Flutter; focused source-test coverage and the mirrored default-scheme demo were expanded.

- Breaking: completed the `FilledButton`/tonal ColorScheme and API closeout. Defaults now read the exact primary,
  secondary-container, on-surface, and shadow roles; callbacks, state controllers, clipping, cursor/density/timing,
  layer builders, inherited-theme capture, focused coverage, and the mirrored direct-scheme probe match Flutter.

- Breaking: completed the `OutlinedButton` ColorScheme/theme closeout. M2/M3 foreground, disabled, overlay,
  outline, tint, icon, cursor, density, and timing defaults now match Flutter; constructor callbacks/state/semantics
  and inherited-theme capture are source-shaped. Added focused coverage and a mirrored direct-scheme probe.

- Breaking: completed the `ElevatedButton` ColorScheme/theme closeout. M2/M3 enabled, disabled, overlay,
  shadow, tint, and icon defaults now read the exact Flutter roles; constructor callbacks/state/semantics, style
  metadata, and inherited-theme capture match the source. Added focused coverage and a mirrored scheme probe.

- Breaking: completed the strict `GridTileBar` Dart closeout. The control now uses source-shaped directional
  padding, inherited row/column/text direction, and `IconTheme.Merge`; shared `Padding`, `Flex`, and `Text`
  primitives now resolve omitted direction from `Directionality`. Added constructor, RTL, zero-area, layout,
  typography, icon, background, and overlay coverage against the pinned Flutter tests.

- Breaking: closed core scroll input-policy parity. `ScrollBehavior` now selects Flutter's base/iOS/macOS
  velocity trackers per pointer, drag recognizers honor custom tracker builders, and mouse-wheel axes flip for the
  configured logical modifiers while trackpads remain unchanged. Pointer-scroll responses now report accepted versus
  rejected platform-default handling, with focused estimator, behavior, modifier, and gesture integration coverage.

- Breaking: closed `MaterialBanner` presentation parity. Banner animation and inset APIs now accept generic
  `Animation<double>` and directional `EdgeInsetsGeometry`; `ScaffoldMessenger` owns the source FIFO queue,
  close reasons, accessible dismissal, and root-Scaffold presentation, while `Scaffold` pushes or overlays its body
  according to banner elevation. Added focused queue/layout/semantics coverage and a mirrored messenger demo probe.

- Breaking: completed the `TextButton` ColorScheme/theme closeout. M2 and M3 foreground, disabled, icon, and
  overlay defaults now read `ColorScheme.primary`/`onSurface` directly; M2 follows the pinned executable 0.10
  pressed/focused opacity. Added source callback/state/semantic plumbing, inherited-theme capture, focused tests,
  and a mirrored direct-scheme runtime probe.

- Breaking: completed the `CircleAvatar` ColorScheme closeout. Material 3 foreground/background defaults now read
  `onPrimaryContainer`/`primaryContainer` directly, with focused precedence and Material 2 brightness coverage plus
  a mirrored local-scheme demo probe.

- Breaking: completed the `RefreshIndicator`/`RefreshProgressIndicator` ColorScheme and composition closeout.
  Default value colors now read `ColorScheme.primary` directly, refresh surfaces use circular `Material`, pull and
  dismissal use the source two-controller transition tree, and active pulls suppress leading glow/stretch chrome.
  Added arbitrary-target `AnimationController.AnimateTo`, focused parity coverage, and a mirrored scheme-color probe.

- Breaking: closed Material `Stepper` animation and scrolling parity. Vertical headers now animate into view before
  callbacks, panels/icons/text use the shared 200 ms implicit-animation primitives, horizontal content preserves state
  through `Visibility`, and the retained Flutter `margin` metadata no longer adds non-source layout padding.

- Breaking: moved `MenuAnchor` panels onto the shared raw-menu/`OverlayPortal` pipeline. Menus now escape ancestor
  clips, honor nearest/root overlay selection, grouped outside-tap consumption, reserved padding, keyboard insets,
  display-feature sub-screens, explicit controller positions, and Flutter's panel fade/height timing. The animation
  callback now reports `AnimationStatus`, and unattached `MenuController.Open` is a no-op; mirrored demos and focused
  layout/default/lifecycle tests cover the new behavior. The remaining raw-controller-tree, ancestor-scroll,
  item-stagger, directional inset/focus, and scrollbar gaps stay tracked in `docs/ai/DIVERGENCES.md`.

- Breaking: completed the Material `Badge` ColorScheme/layout closeout. M3 defaults now read `error` and `onError`
  roles directly, narrow decorated children preserve Flutter's negative alignment space, and focused tests plus the
  mirrored runtime probe cover generated-token precedence and large-label stadium geometry.

- Breaking: closed `TickerMode` parity. The widget now composes a state-owned effective inherited mode with nested
  enabled/force-frame AND/OR semantics, merge/value/notifier APIs, reparent-safe ticker providers, and scheduler-level
  muting that preserves elapsed time without requesting hidden-subtree frames. Framework animation controllers now
  register with their owning state, with focused Flutter-test coverage and a mirrored maintained-visibility demo.

- Breaking: completed the strict `Divider`/`VerticalDivider` Dart closeout. Both controls now use direct M2/M3
  color roles and the source `SizedBox -> Center -> Container` composition, resolve directional indents and
  physical/directional per-corner radii, preserve hairline paint, and accept Flutter's non-negative numeric domain.
  Added per-side box borders, source null-child `Container` expansion, `DividerThemeData.CopyWith`/`Lerp`, inherited
  theme capture, focused Flutter-test parity coverage, and an expanded mirrored runtime probe.

- Breaking: closed `ReorderableList`/`ReorderableListView` overlay parity. Dragged items now use a theme-captured
  overlay proxy with source 250 ms pickup/drop choreography, constraints preservation, continuously ticking edge
  auto-scroll, and localized custom reorder semantics. Reorder callbacks now complete after the drop animation;
  deprecated `cacheExtent` is nullable and the controls expose `ScrollCacheExtent` plus sliver child-index lookup.
  Internal item keys now include their source index, preventing sliver child-list corruption when callbacks mutate a
  keyed backing list.

- Breaking: closed legacy dropdown and cross-fade directional-alignment parity. `DropdownMenuItem`,
  `DropdownButton`, `DropdownButtonFormField`, `Stack`, `IndexedStack`, `AnimatedSize`, and `AnimatedCrossFade`
  now accept `AlignmentGeometry`, retain Flutter's logical defaults, and resolve mixed physical/logical values from
  ambient text direction. Focused LTR/RTL tests and mirrored sample probes cover the new path.

- Breaking: closed magnifier overlay-order parity. `MagnifierController.OverlayEntry` and `Show(... below:)` now
  use core `OverlayEntry` instead of a navigator route, capture inherited themes into the root overlay, preserve
  source animation lifecycle, and let `SelectionOverlay` keep selection handles above lenses that exclude handles.

- Closed cross-host app lifecycle delivery parity. Browser focus/visibility, Android activity/window-focus, and iOS
  foreground/background notifications now feed the Flutter-shaped lifecycle synthesizer, including hidden-state
  transitions, duplicate suppression, and focused Android channel coverage.

- Breaking: closed `RawAutocomplete<T>`/Material `Autocomplete<T>` overlay parity. Suggestions now use the
  source `OverlayPortal` + grouped `TextFieldTapRegion` composition instead of pushing a route, follow live field
  transforms and safe insets, preserve inherited state, announce localized availability changes, and use the exact
  elevated Material surface plus keyed highlight scrolling. `WidgetsApp` now supplies the root overlay required by
  portal-backed framework controls; focused tests and the mirrored runtime probe cover the new path.

- Breaking: closed Material theme interpolation parity. Every component `*ThemeData` now exposes its source-shaped
  `Lerp` contract and participates in `ThemeData.Lerp`; theme extensions interpolate with Flutter's union semantics,
  and non-interpolable policy fields switch at the exact midpoint without endpoint identity shortcuts.

- Breaking: closed Material text-field selection handles end to end. Core now renders editable text through
  `RenderEditable`, drives `TextSelectionOverlay` from retained caret/line/viewport geometry, and supports in-field
  handle drags, adaptive touch magnifiers, and explicit/default spell-check services. `SelectableText` now uses the
  source read-only `EditableText` composition; Material supplies handle controls, misspelling defaults, suggestion
  replacement actions, focused tests, and a mirrored runtime probe.

- Ported the text selection handle overlay: core gains `TextSelectionControls`/`EmptyTextSelectionControls`,
  `TextSelectionHandleType`, `TextSelectionPoint`, `ITextSelectionDelegate`, `ClipboardStatusNotifier`, and
  `SelectionOverlay` with the source handle/toolbar overlay entries, 150 ms linear fades, `kMinInteractiveDimension`
  hit padding, and the touch-gated drag state machine. Material gains `MaterialTextSelectionControls`,
  `MaterialTextSelectionHandleControls`, the exact 22 px single-path handle painter, source anchors, and the legacy
  Cut/Copy/Paste/Select all toolbar. Landed the supporting primitives (`PanGestureRecognizer`,
  `DeviceGestureSettings`, drag details carrying pointer kind/local position/timestamp, `RawGestureDetector` pan
  callbacks) and made `EditableTextState` a `ITextSelectionDelegate`. `TextSelectionOverlay` and the automatic
  in-field magnifier remain blocked on a `RenderEditable` render object
  (`docs/ai/notes/widgets-2026-08-01-selection-handle-overlay.md`).

- Agent/contributor tooling: the code-style contract is now machine-checked instead of review-only.
  `EnforceCodeStyleInBuild` makes IDE0008 (explicit types for built-ins) a build error, nullable warnings
  are errors, and `scripts/check_line_length.sh` enforces the 120-char rule on new/edited lines
  (Claude Code hook + PR gate). CI now builds `src/Plumix.Ci.slnf`, so a public API change can no longer
  break `Plumix.FSharp`/`Plumix.Elmish` unnoticed until pack time.

- Agent/contributor tooling: added `docs/ai/PORT_PLAYBOOK.md` (executable port sequence, including target
  selection), `docs/ai/DART_SPEC_PROTOCOL.md` (reading large Dart sources without exhausting context) and
  generated `docs/ai/PORT_MAP.md` (Flutter file -> C# files/tests/demos, from the existing parity markers).
  Pinned the Flutter parity revision to 3.44.0 in `AGENTS.md` and moved the reference checkouts behind the
  `flutter-src`/`avalonia-src` symlinks. Rotated closed milestones out of `docs/FRAMEWORK_PLAN.md`
  (156 KB -> 6 KB) into `docs/FRAMEWORK_PLAN-archive.md`.

- Breaking: completed the `Card` Dart closeout: elevated/filled/outlined variants now use direct M2/M3
  `ColorScheme` roles and the source `Semantics -> Padding -> Material(type: card) -> Semantics` composition,
  including exact tint, shadow, shape, clipping, border paint order, and theme precedence. Added
  `CardThemeData.CopyWith`/`Lerp`, the source-compatible local `CardTheme`, `ThemeData.Lerp` integration, focused
  tests, and an expanded mirrored runtime probe; advanced `Plumix.Material` to `0.20.0-alpha.1`.

- Breaking: completed the `IconButton` Dart closeout: all four constructors now expose the source API, Material 2
  uses the legacy `InkResponse` composition, and Material 3 uses direct `ColorScheme` roles, stadium geometry,
  standard density, external state controllers, tooltips, adaptive cursors, and source style precedence. Added
  `IconButtonThemeData.CopyWith`/`Lerp`, `ButtonStyle.Lerp`, Material `Theme` icon inheritance, focused tests, and a
  mirrored M2/M3 runtime probe; advanced `Plumix.Material` to `0.19.0-alpha.1`.

- Breaking: completed the `FloatingActionButton` ColorScheme/theme/layout closeout: M2/M3 defaults now read exact
  source roles and state colors, all variants use source shapes and adaptive cursors, omitted/default versus explicit
  null hero tags match Flutter, extended content uses the source overflow layout, and output merges semantics. Added
  `FloatingActionButtonThemeData.CopyWith`/`Lerp`, inherited-theme capture, and an M2/M3 runtime probe whose
  secondary FABs explicitly disable hero registration; advanced `Plumix.Material` to `0.18.0-alpha.1`.

- Breaking: completed the `BottomAppBar` ColorScheme/theme/geometry closeout: M2/M3 defaults now read exact source
  roles, surface tint and elevation overlays follow Flutter, full-strength physical shadows and transparent
  `Material` composition are restored, and notches track the configured FAB rectangle while excluding the cutout
  from hit testing. Added `BottomAppBarThemeData.CopyWith`/`Lerp`, inherited-theme capture, and a mirrored
  center-docked runtime probe; advanced `Plumix.Material` to `0.17.0-alpha.1`.

- Breaking: completed the legacy `BottomNavigationBar` ColorScheme/theme closeout: fixed and shifting defaults now
  read source roles directly, dark fixed selection uses `secondary`, shifting content uses `surface`, icon-theme
  opacity is preserved, and the default body typography/elevation/shadow paths match Flutter. Added
  `BottomNavigationBarThemeData.CopyWith`/`Lerp` and `ThemeData.Lerp` integration; advanced `Plumix.Material` to
  `0.16.0-alpha.1`.

- Breaking: completed the `NavigationDrawer` ColorScheme/theme closeout: M3 drawer surfaces and destination
  defaults now read source roles directly, selected/disabled colors and stadium indicator geometry match Flutter,
  and `NavigationDrawerThemeData.CopyWith`/state-aware `Lerp` now participates in `ThemeData.Lerp`. Advanced
  `Plumix.Material` to `0.15.0-alpha.1`.

- Breaking: completed the `NavigationRail` ColorScheme/theme closeout: M2/M3 defaults and disabled/ink states now
  read source roles directly, M2 preserves the source unselected-icon opacity contract, and M3 uses a stadium
  indicator. Added `NavigationRailThemeData.CopyWith`/`Lerp`, shared component-theme lerp helpers, and
  `ThemeData.Lerp` integration; advanced `Plumix.Material` to `0.14.0-alpha.1`.

- Breaking: completed the `NavigationBar` ColorScheme/theme closeout: M2/M3 defaults now read source roles directly,
  the M2 surface uses Flutter's elevation-overlay formula, the M3 indicator uses a stadium shape, and
  `NavigationBarThemeData` now supports `CopyWith`/state-aware `Lerp` through `ThemeData.Lerp`. Advanced
  `Plumix.Material` to `0.13.0-alpha.1`.

- Breaking: added a Flutter-shaped Material theme foundation with `ColorScheme`, `TextTheme`, and
  `Typography`. Added all Material 3 color roles, exact HCT seed generation for every dynamic scheme variant and
  contrast level, complete 2021 type-scale composition, scheme-driven `ThemeData` defaults/interpolation, focused
  coverage, and a mirrored palette/typography runtime probe. Advanced `Plumix.Material` to `0.12.0-alpha.1`.
- Continue `M4` Material library rewrite with advanced Material control refinements (hover/ripple/style-system expansion) after shipping baseline theming + shell + first button set plus initial interaction polish.
- Run cross-host parity/stability validation in final `M5` phase after Material rewrite sequencing completes.
- Improve architecture docs and migration guidance for Dart-to-C# rewrites.

### Changed

- Breaking: added Flutter-structured `ElevationOverlay` with interpolated M3 surface-tint levels, logarithmic M2
  dark-surface overlays, ambient theme policy, and shared Material surface integration. Added focused coverage and
  advanced `Plumix.Material` to `0.11.0-alpha.1`.

- Added the joint Flutter-structured `WidgetsApp` + `MaterialApp` app shell: named/deep-link initial routing,
  localization and directionality resolution, title/builder/shortcut/action infrastructure, Material page routes,
  messenger/selection defaults, animated system/light/dark/high-contrast themes, and Material/Cupertino default
  localizations. The mirrored C# sample now boots through `MaterialApp`; added focused app-shell tests and advanced
  `Plumix` to `0.16.0-alpha.1`, `Plumix.Material` to `0.10.0-alpha.1`, and `Plumix.Cupertino` to `0.2.0-alpha.1`.

- Added the shared app-motion foundation: core `TransitionRoute`/`PageRouteBuilder` now own primary and secondary
  animations, reverse-exit retention, duration contracts, and hero-safe finalization; Material adds
  `PageTransitionsTheme`, `MaterialPageRoute`, `ThemeData.Lerp`, and interruptible `AnimatedTheme`. Added focused
  navigation/theme tests and advanced `Plumix` to `0.15.0-alpha.1` and `Plumix.Material` to `0.9.0-alpha.1`.

- Breaking: completed Material `Badge` directional-alignment parity by widening widget/theme alignment to
  `AlignmentGeometry`, resolving physical/logical/mixed alignment in the render object for LTR/RTL, restoring
  source `Clip.none` overlay and anti-aliased badge clipping, and adding inherited-theme capture plus
  `BadgeThemeData.CopyWith`/`Lerp`. Added focused tests and a mirrored runtime probe; advanced `Plumix` to
  `0.14.0-alpha.1` and `Plumix.Material` to `0.8.0-alpha.1`.

- Added Flutter core `DragBoundary` parity with generic delegate contracts, local/global/free rectangle boundaries,
  shortest-distance clamping, oversized-object errors, and always-notifying inherited behavior. Reorderable lists now
  resolve the nearest boundary or explicit `dragBoundaryProvider` and clamp their dragged proxy accordingly, with
  focused tests and a mirrored runtime probe. Advanced `Plumix` to `0.13.0-alpha.1` and `Plumix.Material` to
  `0.7.0-alpha.1`.

- Added Flutter core `AnnotatedRegion<T>` parity with source-shaped widget/render-object composition, typed
  front-to-back layer annotation lookup, exact-type matching, sized/local-position results, opaque traversal,
  offset/transform/clip propagation, focused tests, and a mirrored composited-layer runtime probe. Advanced `Plumix`
  to `0.12.0-alpha.1`.

- Added Flutter core `DecoratedBoxTransition` + `DecorationTween` parity with polymorphic/null-endpoint decoration
  interpolation, generic `DecoratedBox` paint ownership, source-shaped `Animatable.animate` lifecycle forwarding,
  foreground/background composition, focused tests, and a mirrored C#/Dart runtime probe. Advanced `Plumix` to
  `0.11.0-alpha.1`.

- Breaking: added paired Material `AppBarTheme` + `DrawerController` parity with inherited local app-bar precedence,
  `copyWith`/lerp contracts, standalone start/end drawer open/close and drag/fling behavior, safe-area edge activation,
  animated scrims, focus/history/back handling, semantics, and Scaffold drawer scopes. Expanded `Drawer` theme,
  shape/clip/surface/semantic composition and focused shell coverage; advanced `Plumix.Material` to `0.6.0-alpha.1`.

- Breaking: added paired Flutter core `FocusableActionDetector` + `ExcludeFocusTraversal` parity with exact
  shortcuts/actions gating, focus/hover highlight callbacks, input-driven highlight modes, directional-navigation
  focusability, independent descendant focus/traversal policies, state-preserving wrapper changes, focused tests,
  and an expanded mirrored C#/Dart keyboard demo. `Focus` now exposes source-shaped descendant and focus-change
  contracts, `MediaQueryData` includes `navigationMode`, and `MouseRegion` defaults to `MouseCursor.defer`; advanced
  `Plumix` to `0.10.0-alpha.1`.

- Breaking: added paired Flutter core `Actions` + `Shortcuts` parity with typed intents/actions, hierarchical
  override/dispatcher lookup, enabled and key-consumption policy, exact modifier/repeat/character/NumLock
  activators, nested/modal focus dispatch, callback shortcuts, deferred `ShortcutRegistrar` entries, focused tests,
  and a mirrored C#/Dart keyboard demo. The host now forwards key symbols and repeat state; advanced `Plumix` to
  `0.9.0-alpha.1`. Flutter's `Action<T>` is named `FlutterAction<T>` to avoid the CLR `System.Action<T>` collision.

- Added paired Material `MenuAcceleratorCallbackBinding` + `MenuAcceleratorLabel` parity with source marker parsing,
  grapheme-safe indexes, Alt-driven underlining/invocation, submenu suppression, Apple-platform policy, automatic
  menu-button bindings, focused tests, and a mirrored C#/Dart menu-bar probe. Added shared `HardwareKeyboard` and text
  decoration plumbing; advanced `Plumix` to `0.8.0-alpha.1` and `Plumix.Material` to `0.5.0-alpha.1`. Rich-span
  kerning and focus-local shortcut priority remain tracked in `DIVERGENCES.md`.

- Breaking: added paired Flutter core `AppLifecycleListener` + `StatusTransitionWidget` parity with source state-
  transition synthesis, per-transition callbacks, collective cancelable exit requests, status-only animation
  rebuilding, listener replacement/disposal, Avalonia focus/minimize/attach host wiring, focused tests, and a
  mirrored C#/Dart runtime probe. Added source-required `DisposableBuildContext<T>` and aligned `State.Mounted`/
  `State.Context` with Flutter inactive/unmounted behavior; advanced `Plumix` to `0.7.0-alpha.1`.

- Added paired Material `NoSplash` + `TableRowInkWell` parity with a non-painting public splash factory,
  source-shaped row-rectangle callbacks, translation-aware whole-row ink geometry, and exact `DataTable` cell-versus-
  row gesture composition. Added focused tests and mirrored NoSplash/DataTable runtime probes; advanced
  `Plumix.Material` to `0.4.0-alpha.1`.

- Added paired Flutter core `BackdropFilter` + `BackdropGroup` parity with direct/bounded/composed
  `ImageFilterConfig`, retained backdrop layers, bitmap-backed scene-prefix sampling, grouped input reuse, blend
  modes, focused tests, and mirrored C#/Dart image probes. Advanced `Plumix` to `0.6.0-alpha.1`; Avalonia CPU-filters
  each grouped output separately until the backend exposes native backdrop-ID fusion.

- Added paired Flutter core `ShaderMask` + `PhysicalShape` parity with source-shaped widget/render-object APIs,
  retained shader-mask layers, all Flutter blend modes, origin-sized shader callbacks, custom-path hit testing,
  clip/reclip lifecycle, physical fill/shadow composition, focused tests, and mirrored C#/Dart image/proxy probes.
  Advanced `Plumix` to `0.5.0-alpha.1`; exact save-layer clipping and arbitrary-path shadow rasterization remain
  tracked in `DIVERGENCES.md`.

- Added paired Flutter core `ColorFiltered` + `ImageFiltered` parity with retained color-filter layers,
  enabled-dependent image-filter repaint boundaries, layer-only filter updates, Flutter-shaped color/image filter
  values, CPU-backed matrix/mode/blur/matrix/morphology/compose rendering, focused tests, and mirrored C#/Dart image
  probes. Filter layers render directly into bitmap-compatible offscreen contexts so image children are supported.
  Added Flutter `BlendMode`/`TileMode` surfaces while retaining Avalonia blend-mode compatibility, and advanced
  `Plumix` to `0.4.0-alpha.1`.

- Breaking: added paired Flutter core `RadioGroup<T>` + `RawRadio<T>` parity with registry/client ownership,
  toggleable selection animation, checked/group semantics, selected-only Tab traversal, wraparound arrow/Space
  keyboard behavior, and focused tests. Material `Radio`/`RadioListTile` now consume the shared registry while
  retaining deprecated direct-value compatibility; the mirrored radio demo uses the modern ancestor API. Moved
  `RadioGroup<T>` from `Plumix.Material` to `Plumix` and advanced both packages to `0.3.0-alpha.1`.

- Breaking: added paired Flutter core `RawTooltip` + Material `Tooltip` parity with `OverlayPortal` ownership,
  viewport-aware auto-flipping and custom position delegates, source timing/trigger/feedback/global-dismiss behavior,
  tooltip semantics, focused tests, and an expanded mirrored C#/Dart demo. `WidgetHost` now installs the baseline root
  overlay required by portal controls. Moved shared tooltip trigger/callback types into core and advanced
  `Plumix`/`Plumix.Material` to `0.2.0-alpha.1`; Material rich messages await shared spans.

- Added paired Flutter core `OverlayPortal` + `TapRegion` ports with nearest/root overlay targeting, controller
  show/hide/toggle and z-order promotion, inherited-subtree ownership, overlay child layout information, grouped
  inside/outside down/up callbacks, route-current filtering, outside-tap arena consumption, focused tests, and an
  expanded mirrored C#/Dart drag demo. Advanced `Plumix` to `0.5.0-alpha.1`.

- Breaking: added Flutter core `LongPressDraggable<T>` parity with delayed per-pointer recognition, pre-delay
  touch-slop rejection, configurable delay/button filtering, child/feedback lifecycle, and selection haptics only
  after a successful drag start. Completed the paired `Overlay`/`OverlayEntry` follow-up with a framework-owned
  theater render path, onstage/maintained entry handling, `canSizeOverlay`/`alwaysSizeToContent`, `Overlay.Wrap`,
  mutable opacity/state retention, mounted listeners, visibility checks, and atomic entry rearrangement. Added
  focused tests and an expanded mirrored C#/Dart drag demo, removed the closed overlay divergence, and advanced
  `Plumix` to `0.4.0-alpha.1`.

- Added paired Flutter core `Draggable<T>` + `DragTarget<T>` ports with source-shaped overlay feedback, child
  replacement, anchor/axis/affinity/button policies, accepted/rejected target traversal, candidate/rejected data,
  move/leave/drop and source completion/cancellation callbacks, velocity/offset details, focused tests, and a
  mirrored C#/Dart runtime probe. Added the required `Overlay`/`OverlayEntry` baseline and advanced `Plumix` to
  `0.3.0-alpha.1`; advanced theater sizing/rearrangement and `LongPressDraggable` remain follow-up controls.

- Stabilized `ConstraintsTransformBox` clip regression coverage across Debug/Release by painting actual child content
  instead of relying on the debug-only overflow indicator to create a picture layer.

- Breaking: completed paired Flutter core `IntrinsicWidth` + `IntrinsicHeight` parity with source-shaped zero-step
  normalization, positive render-step guards, speculative intrinsic-axis sizing, parent constraint clamping,
  tight-height fast paths, tallest-child Row stretch behavior, focused tests, and a mirrored C#/Dart runtime probe.
  Advanced `Plumix` to `0.2.0-alpha.1`; cached intrinsic/dry-layout queries remain tracked in `DIVERGENCES.md`.

- Breaking: added paired Flutter core `PositionedDirectional` + `TableCell` parity with ambient LTR/RTL inset
  resolution, cell semantics, all six vertical-alignment modes, baseline reporting, fill/intrinsic-height relayout,
  RTL table columns, focused tests, and mirrored Stack/DataTable probes. Direct core `Table` cells now use Flutter's
  `top` default instead of the former implicit center; Material `DataTable` retains its source `middle` default.
  Advanced `Plumix` to `0.1.0-alpha.17`.

- Added paired Flutter core `ConstraintsTransformBox` + `UnconstrainedBox` parity with all seven source constraint
  transforms, directional alignment, normalized-output guards, overflow-only clipping, debug overflow indicators,
  source-shaped widget composition, focused tests, and a mirrored C#/Dart runtime probe. Added the sample-root LTR
  `Directionality` supplied automatically by Dart's `MaterialApp`. Advanced `Plumix` to
  `0.1.0-alpha.16`; isolated `antiAliasWithSaveLayer` clipping remains tracked in `DIVERGENCES.md`.

- Added paired Flutter core `MetaData` + `IndexedSemantics` ports with source hit-test behavior, opaque payload
  updates, first-child semantic indexes, stable semantics-node identity, focused tests, and a mirrored C#/Dart
  state-storage probe. Exposed semantic indexes through the host-visible tree and advanced `Plumix` to
  `0.1.0-alpha.15`.

- Added paired Flutter core `Title` + `DefaultSelectionStyle` ports with opaque-color validation, application-
  switcher metadata dispatch and desktop window-title updates, fallback/merge selection inheritance, mouse-cursor
  propagation, source-shaped `InheritedTheme` capture, Material selection consumers, `About*` title ancestry,
  focused tests, and mirrored C#/Dart sample probes. Advanced `Plumix` to `0.1.0-alpha.14` and `Plumix.Material`
  to `0.1.0-alpha.13`.

- Added paired Flutter core `NotificationListener<T>` + `ScrollNotificationObserver` parity with typed/cancellable
  bubbling, dependency-scoped multi-listener registration, mutation-safe/error-isolated delivery, initial and
  dimension-change `ScrollMetricsNotification` forwarding, focused tests, and a mirrored C#/Dart observer readout.
  Expanded scroll metrics with Flutter extent calculations and advanced `Plumix` to `0.1.0-alpha.13`.

- Added paired Flutter core `GlowingOverscrollIndicator` + `StretchingOverscrollIndicator` ports with leading/
  trailing edge policy, notification veto/paint offsets, source glow/stretch formulas and return motion,
  direction-aware paint/transform geometry, conditional clipping, Material platform/M2/M3 selection, focused tests,
  and a mirrored C#/Dart runtime switch. Expanded scroll drag notification detail and advanced `Plumix`/
  `Plumix.Material` to `0.1.0-alpha.12`; Avalonia uses Flutter's affine stretch fallback until fragment-shader
  filters are available.

- Breaking: added paired Flutter core `PrimaryScrollController` + `ScrollConfiguration` ports with nullable/none
  scopes, platform-and-axis automatic inheritance, nested primary shielding, behavior copying, platform physics,
  drag-device filtering, keyboard dismissal, and desktop scrollbar chrome. Moved `TargetPlatform` from
  `Plumix.Material` to the shared `Plumix` namespace, added focused tests and a mirrored C#/Dart state-storage
  probe, and advanced `Plumix`/`Plumix.Material` to `0.1.0-alpha.11`; overscroll visuals and advanced pointer
  velocity/axis policies remain tracked in `DIVERGENCES.md`.

- Added paired Flutter core `StatefulBuilder` + `LookupBoundary` ports with local state-setter rebuilds, exact-type
  inherited lookup/dependency registration, bounded widget/state/render-object ancestor queries, boundary-aware
  element visitors, focused tests, and a mirrored C#/Dart runtime probe. Added the source-required `BuildContext`
  element/root-state/render-object/child lookup helpers and advanced `Plumix` to `0.10.0-alpha.1`.

- Breaking: added paired Flutter core `SliverResizingHeader` + `SliverFloatingHeader` ports with measured prototypes,
  pinned resize geometry, user-direction reveal/hide, overlay/scroll snap modes, animation-style overrides, focused
  tests, and mirrored C#/Dart custom-sliver probes. Added shared scroll-direction/is-scrolling contracts and advanced
  `Plumix` to `0.9.0-alpha.1`; `Curves.EaseInOut` now uses Flutter's exact cubic instead of smoothstep.

- Added paired Flutter core `SliverPrototypeExtentList` + `SliverVariedExtentList` ports with offstage prototype
  measurement, per-index layout dimensions, exact constrained extents, lazy keyed child reuse, focused tests, and
  mirrored C#/Dart custom-sliver probes. Reorderable lists now honor `prototypeItem` and share the varied-extent
  render path; advanced `Plumix` to `0.8.0-alpha.1`.

- Fixed `DropdownButton` route-result delivery to complete on the navigation lifecycle instead of a thread-pool
  continuation, removing a keyboard-selection race under CI load.

- Added paired Flutter core `SliverFillViewport` + `SliverFillRemaining` ports with fractional page extents,
  centered end padding, lazy child lifecycle, implicit-scrolling semantics policy, all three remaining-space layout
  modes, focused tests, and mirrored runtime probes. Advanced `Plumix` to `0.7.0-alpha.1`.

- Breaking: expanded paired Material `Slider` + `RangeSlider` parity with Flutter-shaped labels/value indicators,
  discrete tick marks, cursor and padding resolution, slider interaction policies, 2023/2024 thumb and gapped-track
  geometry, shared `SliderThemeData` tokens, focused tests, and mirrored C#/Dart runtime probes.

- Added paired Flutter core `FutureBuilder<T>` + `StreamBuilder<T>` ports with source-shaped `AsyncSnapshot<T>` and
  `ConnectionState`, initial/retained data, waiting/active/done/error transitions, stream-fold hooks, source
  replacement, stale-completion suppression, focused tests, and a mirrored C#/Dart runtime probe. The C# async
  substrate maps Dart `Future`/`Stream` to `Task<T>`/`IObservable<T>`; advanced `Plumix` to `0.6.0-alpha.1`.

- Added paired Flutter core `SliverLayoutBuilder` + `SliverSafeArea` ports with layout-phase sliver-constraint
  building, equivalent-constraint rebuild suppression, exact child geometry forwarding, safe-inset/minimum/edge
  composition, focused tests, and a mirrored C#/Dart custom-sliver probe. `SliverConstraints` now implements the
  shared constraints contract; advanced `Plumix` to `0.5.0-alpha.1`.

- Breaking: added paired Flutter core `CompositedTransformTarget` + `CompositedTransformFollower` ports with shared
  `LayerLink`/leader/follower compositing primitives, anchor and offset alignment, linked/unlinked visibility,
  transformed hit testing and semantics, focused tests, and a mirrored C#/Dart runtime probe. Nested root-transform
  composition now follows child-to-parent paint order; advanced `Plumix` to `0.4.0-alpha.1`.

- Added paired Flutter core `PopScope<T>` + `NavigatorPopHandler<T>` ports with route-scoped pop entries,
  collective veto, successful/rejected result callbacks, dynamic `canPop`, nested-navigator navigation
  notifications, focused tests, and a mirrored C#/Dart runtime probe. `Form` now delegates its modern pop-veto and
  result APIs through `PopScope`; advanced `Plumix` to `0.3.0-alpha.1`.

- Added paired Flutter core `PageStorage` + `SharedAppData` ports with composite keyed bucket identity, explicit
  identifiers, lazy typed values, aspect-selective inherited-model rebuilds, route-owned storage buckets, and
  automatic `ScrollController.keepScrollOffset` save/restore across scrollable disposal. Added focused tests and a
  mirrored C#/Dart runtime probe; advanced `Plumix` to `0.2.0-alpha.1`.

- Breaking: added paired Flutter core `Dismissible` + `SizeChangedLayoutNotifier` ports with keyed directional
  drags, threshold/fling/confirmation/update contracts, clipped primary/secondary backgrounds, move/collapse
  choreography, bubbled post-initial size notifications, focused tests, and mirrored C#/Dart probes. Shared drag
  recognizers now honor `DragStartBehavior.start`, expose two-axis velocity, and `AnimationController.Fling(...)`
  uses Flutter's default critically damped spring; `ClipRect` now accepts a listenable `CustomClipper<Rect>`.

- Added paired Flutter core `CustomMultiChildLayout` + `NavigationToolbar` ports with source-shaped `LayoutId`
  parent data, exactly-once delegate layout contracts, listenable relayout, dependent child constraints, default
  paint/hit-test/semantics order, centered/start LTR/RTL toolbar geometry, focused tests, and mirrored C#/Dart probes.

- Breaking: added paired Flutter core `FadeInImage` + `ImageIcon` ports with memory/asset-network factories,
  gapless stream replacement, cached-image bypass, sequential placeholder/target fades, independent image styling,
  IconTheme size/color/opacity resolution, single-node semantics, focused tests, and mirrored C#/Dart probes.
  `Curves.EaseIn`/`EaseOut` now use Flutter's exact cubic Bézier definitions instead of quadratic approximations;
  synchronized published packages at `1.0.0-alpha.1`.

- Added paired Flutter core `Image` + `RawImage` ports with provider constructors, stream/cache lifecycle,
  frame/loading/error builder ordering, gapless replacement, paused-stream keep-alive, aspect-preserving layout,
  fit/repeat/directional paint, opacity updates, semantics, focused tests, and mirrored C#/Dart probes. Advanced
  `Plumix` to `0.12.0-alpha.1`; animated codecs, backend pixel effects, intrinsic/dry queries, scroll-aware deferral,
  and cloneable raw image handles remain tracked in `DIVERGENCES.md`.

- Added paired Flutter core `Flow` + `RepaintBoundary` ports with source-shaped automatic child isolation,
  delegate/listenable layout and repaint ownership, paint-order transforms/opacity, transformed hit testing and
  semantics, focused tests, and mirrored C#/Dart probes. Advanced `Plumix` to `0.11.0-alpha.1`; shared Matrix4,
  intrinsic/dry-layout, and render-subtree image-capture gaps remain tracked in `DIVERGENCES.md`.

- Added paired Flutter core `FractionalTranslation` + `RotatedBox` ports with source-shaped paint-offset hit testing,
  layout-time quarter-turn constraint/size transposition, transformed paint/semantics geometry, focused tests, and
  mirrored C#/Dart probes. Advanced `Plumix` to `0.10.0-alpha.1`; the shared intrinsic/dry-layout query gap remains
  tracked in `DIVERGENCES.md`.

- Added paired Flutter core `ClipOval` + `ClipPath` ports with source-shaped `CustomClipper<T>` reclip lifecycle,
  shape-aware hit testing, geometry-layer clipping, `ClipPath.Shape(...)`, focused tests, and mirrored C#/Dart
  probes. Added the framework-owned path subset required by custom clips; advanced `Plumix` to `0.9.0-alpha.1`.

- Added paired Flutter core `Placeholder` + `GridPaper` ports with source-shaped custom-paint composition,
  unbounded fallback sizing, foreground grid hierarchy, hit-test/repaint behavior, focused tests, and mirrored
  C#/Dart probes. Advanced `Plumix` to `0.8.0-alpha.1`.

- Added paired Flutter core `DualTransitionBuilder` + `RepeatingAnimationBuilder<T>` ports with nested directional
  transitions, interruption continuity, restart/reverse loops, pause/resume, curve/duration updates, stable children,
  focused tests, and mirrored C#/Dart probes. Added source-required `Animatable<T>`, `ProxyAnimation`, and
  `ReverseAnimation`; advanced `Plumix` to `0.7.0-alpha.1`.

- Added paired Flutter core `KeyboardListener` + deprecated-compatible `RawKeyboardListener` ports with
  source-shaped focus-node ownership, key-down/up delivery, raw listener attach/detach/rebind lifecycle,
  `includeSemantics` focus actions/flags, focused tests, and mirrored C#/Dart runtime probes. Advanced `Plumix` to
  `0.6.0-alpha.1`; platform physical/logical raw-key metadata remains tracked in `DIVERGENCES.md`.

- Added paired Flutter core `LayoutBuilder` + `OrientationBuilder` ports with layout-phase child construction,
  constraint-change rebuild suppression, widget/dependency invalidation, portrait/landscape reduction, focused tests,
  and mirrored C#/Dart runtime probes. Advanced `Plumix` to `0.5.0-alpha.1`.

- Added paired Flutter core `Baseline` + `IgnoreBaseline` ports with source-shaped child positioning, bottom-edge
  fallback, proxy baseline forwarding, paragraph metrics, Flex/Row/Column `textBaseline` wiring, focused tests, and
  mirrored C#/Dart runtime probes. Advanced `Plumix` to `0.4.0-alpha.1`; Avalonia ideographic metrics remain tracked.

- Added paired Flutter core `ListenableBuilder` + `AnimatedBuilder` ports with source-identical inheritance,
  shared `AnimatedWidget` listener rebinding/disposal, stable-child fast paths, focused tests, and mirrored C#/Dart
  runtime probes. Advanced `Plumix` to `0.3.0-alpha.1`.

- Added automatic selection context menus for paired Material `TextField`/`TextFormField` and
  `SelectableText`/`SelectionArea`: pointer drag and word selection, source-shaped context-menu builders,
  Copy/Cut/Paste/Select all policies, adaptive toolbar anchors, route-backed presentation, focused tests, and
  mirrored runtime instructions. Text fields now use the complete decorated area for focus, caret placement, and
  pointer selection. Automatic touch magnifiers and draggable selection handles remain tracked.

- Stabilized the image-cache lifetime regression test by waiting for pending completion and last-listener cleanup as
  one final cache state, avoiding a thread-scheduling race in CI.

- Added the Flutter magnifier family: core `RawMagnifier`/controller/configuration, Material `Magnifier`/
  `TextMagnifier`, and adaptive Cupertino lens/positioning controls, backed by framework-owned scene magnification.
  Added focused geometry/layer tests and a mirrored C#/Dart runtime probe; advanced `Plumix`, `Plumix.Cupertino`,
  and `Plumix.Material` to `0.2.0-alpha.1`. Fixed rounded-clip recording in the magnifier backdrop capture path.

- Added paired Flutter core `ValueListenableBuilder<T>` + `TweenAnimationBuilder<T>` ports with source-shaped
  listener rebinding, stable-child builder contracts, owned tween retargeting, interrupted-animation continuity,
  curve/duration/`onEnd` behavior, focused tests, and mirrored C#/Dart runtime probes.

- Added paired Flutter core `ModalBarrier` + `AnimatedModalBarrier` ports with source-shaped dismiss/pop behavior,
  any-button opaque gesture capture, platform-aware accessibility actions, semantic focus clipping, system-alert
  dispatch,
  animated colors, and `BlockSemantics`; Material dialog routes now use the shared animated barrier.

- Added paired Material `AdaptiveTextSelectionToolbar` + `SpellCheckSuggestionsToolbar` ports with shared
  context-menu button/anchor contracts, localized button mapping, Android/desktop platform routing, safe-inset and
  keyboard-aware spell-check placement, focused tests, and mirrored C#/Dart probes. Advanced `Plumix` and
  `Plumix.Material` to `0.2.0-alpha.1`; Cupertino visuals and automatic editable selection/spell-check integration
  remain tracked in `DIVERGENCES.md`.

- Added paired Flutter core `SliverCrossAxisGroup` + `SliverMainAxisGroup` ports with inflexible/proportional
  cross-axis allocation, sequential scroll/cache constraints, pinned-child confinement, correction propagation,
  focused tests, and mirrored C#/Dart probes. Added source-required `SliverConstrainedCrossAxis` and
  `SliverCrossAxisExpanded`; advanced `Plumix` to `0.1.0-alpha.10`.

- Added paired Flutter core `DecoratedSliver` + `PinnedHeaderSliver` ports with general `Decoration`/`BoxPainter`
  support, max/cache-extent decoration paint, measured pinned geometry, overlapping viewport layout, focused tests,
  and mirrored C#/Dart probes. Advanced `Plumix` to `0.1.0-alpha.9`; viewport semantics-tag partitioning remains
  tracked in `DIVERGENCES.md`.

- Breaking: added paired Material `InkRipple` + `InkSparkle` ports and pluggable
  `InteractiveInkFeatureFactory` selection through ink responses, button styles, and `ThemeData`; M3 Android now
  defaults to sparkle, other M3 platforms to ripple, and M2 to splash. Added focused tests and mirrored C#/Dart
  probes; shader-identical sparkle noise remains documented. Advanced `Plumix.Material` to `0.4.0-alpha.1`.

- Added Material `AnimatedIcon` + complete 14-entry `AnimatedIcons` catalog with Flutter-generated vector frames,
  linear frame interpolation, animation-driven `CustomPainter` repaint, icon-theme size/color/opacity, RTL mirroring,
  semantics, focused tests, and mirrored C#/Dart probes. Advanced `Plumix` to `0.15.0-alpha.1` and
  `Plumix.Material` to `0.3.0-alpha.1`.

- Added paired Flutter core `AnimatedGrid` + `SliverAnimatedGrid` ports with source-shaped insert/remove APIs,
  fixed/max-extent grid delegate composition, keyed child remapping, MediaQuery padding, viewport cache/clip policy,
  focused tests, and mirrored C#/Dart probes. Advanced `Plumix` to `0.14.0-alpha.1`.

- Added paired Flutter core `AnimatedList` + `SliverAnimatedList` ports with source-shaped insert/remove APIs,
  separated-list coordination, keyed child remapping, MediaQuery padding, viewport cache/clip policy, focused tests,
  and mirrored C#/Dart probes. Advanced `Plumix` to `0.13.0-alpha.1`.

- Added paired Flutter core `ReorderableList`/`SliverReorderableList` + Material `ReorderableListView` ports with
  keyed drag state, immediate/long-press listeners, animated gaps/proxy elevation, adjusted and legacy callback
  indices, variable extents, desktop/mobile handles, focused tests, and mirrored C#/Dart probes. Advanced `Plumix`
  to `0.12.0-alpha.1` and `Plumix.Material` to `0.2.0-alpha.1`; remaining overlay/prototype/semantics gaps are
  tracked in `DIVERGENCES.md`.

- Fixed `RawScrollbar` pointer filtering so clicks routed through its content subtree no longer trigger thumb or
  track handling outside the scrollbar's interactive bounds; direct content interaction now leaves scroll offset
  unchanged while thumb dragging and track paging remain active.

- Added paired Flutter core `AlignTransition` + `DefaultTextStyleTransition` ports with source-shaped
  `AnimatedWidget` lifecycle, directional alignment geometry, shrink factors, inherited animated typography,
  immediate text-layout options, focused tests, and mirrored C#/Dart runtime probes. Advanced `Plumix` to
  `0.11.0-alpha.1`.
- Breaking: `Align.Alignment` and the F# `Ui.align` binding now use Flutter-shaped `AlignmentGeometry`; `Align`
  resolves `AlignmentDirectional` from ambient `Directionality` and rejects negative/NaN width and height factors.

- Added paired Flutter core `SlideTransition` + `SizeTransition` ports with source-shaped explicit-animation
  listener lifecycle, RTL-aware fractional translation, configurable hit-test transforms, clipped main/cross-axis
  factors, deprecated `axisAlignment` compatibility, full alignment overrides, focused tests, and mirrored C#/Dart
  runtime probes. Advanced `Plumix` to `0.10.0-alpha.1`.

- Added paired Flutter core `PositionedTransition` + `RelativePositionedTransition` ports with source-shaped
  `AnimatedWidget` listener lifecycle, direct `RelativeRect` insets, declared-size `Rect` conversion, null-rect
  fallback, focused tests, and mirrored C#/Dart runtime probes. Added source-required `RelativeRectTween`,
  `RelativeRect.Lerp`, and `Positioned.FromRelativeRect`; advanced `Plumix` to `0.9.0-alpha.1`.

- Added paired Flutter core `ScaleTransition` + `RotationTransition` ports with source-shaped `AnimatedWidget` and
  `MatrixTransition`, shared-listenable lifecycle, centered/custom pivots, active-animation-only filter quality,
  exact turn matrices, focused tests, and mirrored C#/Dart runtime probes. Advanced `Plumix` to `0.8.0-alpha.1`.
- Fixed `AnimationController` terminal-frame notification ordering so value listeners observe the completed or
  dismissed status while rebuilding the final frame, matching Flutter's filter-layer teardown behavior.

- Added paired Flutter core `Visibility` + `SliverVisibility` ports with source-matching replacement and maintain
  APIs, state/focus/size/semantics/interactivity policies, nested visibility lookup, focused lifecycle/render/sliver
  tests, and mirrored C#/Dart runtime probes. Added source-required `SliverIgnorePointer`, `SliverOffstage`, and
  non-composited visibility render proxies; advanced `Plumix` to `0.7.0-alpha.1`. Descendant ticker muting remains
  tracked in `DIVERGENCES.md` against the shared `TickerMode` ownership gap.

- Added paired Flutter core `AnimatedFractionallySizedBox` + `SliverAnimatedOpacity` ports with source-matching
  defaults/guards, fractional factor and alignment interpolation, interrupted-transition continuity, sliver
  geometry preservation, zero-opacity paint/compositing and semantics policy, `onEnd`, focused tests, and mirrored
  C#/Dart runtime probes. Added source-required `RenderProxySliver`, static/animated render opacity,
  `SliverOpacity`, and `SliverFadeTransition` primitives; advanced `Plumix` to `0.6.0-alpha.1`.

- Added paired Flutter core `AnimatedSwitcher` + `AnimatedCrossFade` ports with keyed rapid-replacement retention,
  customizable transition/layout builders, independent forward/reverse durations and curves, reversible two-child
  fade/size choreography, focus/pointer/semantics isolation, `onEnd`, focused tests, and mirrored C#/Dart runtime
  probes. Added source-required `Animation<T>`, `CurvedAnimation`, `FadeTransition`, `TickerMode`, and
  `ExcludeSemantics` primitives; advanced `Plumix` to `0.5.0-alpha.1`.
- Breaking: `Stack` now defaults to Flutter's `Clip.hardEdge` overflow policy and exposes `clipBehavior`; pass
  `Clip.none` to preserve the previous unclipped behavior.

- Added paired Flutter core `AnimatedDefaultTextStyle` + `AnimatedPhysicalModel` ports with source-matching API
  defaults, immediate non-animated text/shape fields, interrupted-transition continuity, optional color/shadow
  animation, `onEnd`, focused text/layout/paint tests, and mirrored C#/Dart runtime probes. Added the source-required
  `PhysicalModel`/`RenderPhysicalModel`, expanded `DefaultTextStyle` text-layout inheritance, and advanced `Plumix`
  to `0.4.0-alpha.1`.
- Breaking: `Text` layout-option defaults are now nullable and inherit `DefaultTextStyle` values for alignment,
  wrapping, overflow, line count, width basis, and height behavior, matching Flutter.

- Fixed inactive element finalization to retain descendant parent links and unmount only inactive subtree roots,
  preventing nested state objects such as `AnimatedOpacity` from receiving `Dispose()` twice when leaving a route.

- Wrapped the mirrored C#/Dart implicit-animations demo in an always-visible Material scrollbar backed by a shared
  scroll controller, keeping all animation probes reachable on short desktop viewports without flex overflow.

- Added paired Flutter core `AnimatedPositioned` + `AnimatedPositionedDirectional` ports with source-matching
  constructors/guards, `fromRect`, physical/logical Stack insets, RTL/LTR resolution, interrupted-transition
  continuity, nullable-property behavior, `onEnd`, focused tests, and mirrored C#/Dart runtime probes. Added the
  source-required `Positioned.Directional` factory and advanced `Plumix` to `0.3.0-alpha.1`.

- Added paired Material `TextSelectionToolbar` + `TextSelectionToolbarTextButton` ports with source-matching
  safe-area anchors, Android surface/button styling, animated horizontal-to-vertical overflow paging, RTL geometry,
  semantics, focused tests, and mirrored C#/Dart runtime probes. Added the source-required core `AnimatedSize` and
  `TextSelectionToolbarLayoutDelegate` primitives.

- Added paired Material `DesktopTextSelectionToolbar` + `DesktopTextSelectionToolbarButton` ports with the shared
  core `CustomSingleChildLayout` primitive, source-matching safe-area/viewport placement, card/button styling,
  disabled/cursor/tap behavior, focused tests, and mirrored C#/Dart runtime probes.

- Added paired Flutter core `AnimatedScale` + `AnimatedRotation` ports with source-matching defaults,
  centered/custom transform origins, `filterQuality` propagation, interrupted-transition continuity,
  curve/duration updates, `onEnd`, focused tests, and mirrored C#/Dart runtime probes.

- Added paired Flutter core `AnimatedOpacity` + `AnimatedSlide` ports with source-matching defaults,
  interrupted-transition continuity, curve/duration updates, `onEnd`, semantics policy, focused tests, and mirrored
  C#/Dart runtime probes. Advanced `Plumix` to `0.2.0-alpha.1`.
- Breaking: zero-opacity `Opacity` now omits descendant semantics unless `alwaysIncludeSemantics` is enabled,
  matching Flutter.

- Added paired Material `SelectableText` + `SelectionArea` ports on a shared core selectable-region registrar,
  including glyph-range highlight paint, cross-widget pointer drag, select-all/copy shortcuts,
  `TextSelectionTheme`, focused tests, and mirrored C#/Dart runtime probes. Selection handles, context menus,
  magnifiers, and rich spans remain tracked in `docs/ai/DIVERGENCES.md`.

- Added paired Flutter core `AnimatedAlign` + `AnimatedPadding` ports with alignment/factor and inset
  interpolation, interrupted-transition continuity, curve/duration updates, `onEnd`, focused tests, and mirrored
  C#/Dart runtime probes. Advanced `Plumix` to `0.2.0-alpha.1`.

- Added paired Material `CheckboxMenuButton` + `RadioMenuButton<T>` ports with Dart-matching value transitions,
  disabled/toggleable states, leading-control constraints, focus/pointer isolation, state-controller forwarding,
  close policy, focused tests, and mirrored C#/Dart menu probes. Advanced `Plumix.Material` to `0.2.0-alpha.1`.

- Closed paired `CheckboxListTile` + `SwitchListTile` parity for visual density, external material-state control,
  checkbox title alignment, and semantic-button policy; shared `ListTile` geometry/state wiring, focused tests, and
  mirrored C#/Dart runtime probes were updated.

- Added paired Material `MenuTheme` + `SubmenuButton` theme integration, including global/local/widget precedence for submenu-panel styles and disclosure icons, focused coverage, and mirrored C#/Dart menu probes.

- Added paired Material `MenuBarTheme` + `MenuButtonTheme` ports with global/local/theme/widget style precedence for `MenuBar`, `SubmenuButton`, and `MenuItemButton`, focused coverage, and mirrored C#/Dart menu-theme probes.

- Added paired state-aware `MaterialStateOutlineInputBorder` and `MaterialStateUnderlineInputBorder` ports. `InputDecorator` now resolves focus, hover, error, and disabled state sets for supported borders; focused tests and mirrored text-field demo probes were added.

- Added paired Material `MenuBar` + `SubmenuButton` ports with nested `MenuController` ownership, sibling submenu closing, horizontal/side panel placement, focused coverage, and mirrored C#/Dart dropdown-demo probes. Root-overlay/follower positioning, keyboard traversal, animation, and complete menu theming remain tracked in `docs/ai/DIVERGENCES.md`.

- Added paired Material `MenuAnchor` + `MenuItemButton` ports with shared `MenuController` ownership, programmatic open/close, inherited nearest-anchor lookup, anchored overlay layout, menu-item state/focus/semantics composition, close-on-activate behavior, focused coverage, and mirrored C#/Dart dropdown-demo probes. Root-overlay placement, cascading submenus, menu animation, and complete menu theming remain tracked in `docs/ai/DIVERGENCES.md`.

- Added Flutter-structured Material surface and completed its `MergeableMaterial` integration: surface type/color/elevation/tint/shape/clip/default-text behavior, animated visual values, focused regression coverage, and mirrored C#/Dart runtime probes. Shared ink ownership, oval clipping, and text-style interpolation remain tracked in `docs/ai/DIVERGENCES.md`.

- Added Material `SearchDelegate<T>` and `MaterialSearch.ShowSearch<T>` full-screen route support, focused lifecycle coverage, and mirrored C#/Dart demo probes. Advanced `Plumix.Material` to `0.3.0-alpha.1`; generic transition/input configuration gaps are tracked in `docs/ai/DIVERGENCES.md`.

- Added paired Material `Ink` and `TooltipVisibility` ports with Flutter-shaped decoration/image shorthand, inherited tooltip suppression, focused tests, and mirrored C#/Dart demo probes. Added core `BoxConstraints.Expand` and advanced `Plumix`/`Plumix.Material` to `0.2.0-alpha.1`. Ancestor-owned ink features remain documented in `docs/ai/DIVERGENCES.md`.

Earlier entries are archived in [`CHANGELOG-2026-H1.md`](CHANGELOG-2026-H1.md) and
[`CHANGELOG-2026-H2.md`](CHANGELOG-2026-H2.md).

All notable framework changes are documented in this file.

This project follows the spirit of [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

- Added paired Flutter core `AlignTransition` + `DefaultTextStyleTransition` ports with directional alignment,
  shrink factors, inherited animated typography, immediate text-layout options, focused tests, and mirrored C#/Dart
  probes.
- Added paired Flutter core `PositionedTransition` + `RelativePositionedTransition` ports with source-matching
  animated inset/rect composition, declared-size conversion, null fallback, focused tests, and mirrored C#/Dart
  probes.
- Added paired Flutter core `Visibility` + `SliverVisibility` ports with replacement and maintain-state/animation/
  size/semantics/interactivity/focusability composition, nested visibility aggregation, source-required sliver
  pointer/offstage proxies, focused coverage, and mirrored C#/Dart probes.
- Added paired Flutter core `IgnorePointer` + `AbsorbPointer` ports with Dart-shaped hit-test behavior, default
  semantics action blocking, `ignoringSemantics` subtree omission, and focused widget/render/semantics coverage.
- Added paired Material `FloatingActionButtonLocation` + `FloatingActionButtonAnimator` baseline with all standard static FAB placement formulas, `Scaffold` API wiring, focused geometry/RTL/animator tests, and mirrored C#/Dart centered-FAB sample probes. Location-transition animation remains documented in `DIVERGENCES.md` pending live scaffold geometry.
- Added paired Flutter `Wrap` + Material `Chip` ports. `Wrap` now owns multi-run layout, alignment, RTL/vertical ordering, and optional overflow clipping; `Chip` composes through `RawChip` with Flutter-shaped delete-only semantics. Added focused tests and mirrored C#/Dart chips-demo coverage.
- Added Flutter-structured Material `CarouselView`/`CarouselViewTheme` with fixed, weighted, and lazy constructors; a carousel-specific controller/scroll position preserves the leading item across viewport changes and supports item snapping. Added a core variable-extent sliver adapter, focused carousel coverage, and mirrored C#/Dart gallery demos.
- Added `Plumix.Elmish` `0.1.0-alpha.1` — Elmish (MVU) integration hosting an `Elmish.Program` inside a `StatefulWidget` (`Program.toWidget`/`toWidgetWith`), with framework element reconciliation as the diffing layer and command dispatch marshaled to the UI thread; the F# sample's root counter is now the MVU variant. Known gap (tracked in the F# note): program termination/subscription teardown is not wired to widget disposal yet.
- Added `Plumix.FSharp` `0.1.0-alpha.1` — F# bindings with Feliz-style `Ui.*` widget factory functions over `Plumix`/`Plumix.Material` plus a one-`open` prelude re-exporting common widget/layout/painting types; `src/Sample/Plumix.FSharpSample` (F# desktop counter) now builds its UI with the DSL. Requires an explicit `FSharp.Core` reference under central package management. Interop findings: `docs/ai/notes/2026-07-10-fsharp-sample.md`.
- Added paired Flutter-structured `TabPageSelector` + `TabPageSelectorIndicator` ports with explicit/default controllers, animated and drag-offset color interpolation, theme-secondary defaults, border-style/size controls, localized semantics, zero-area coverage, and expanded mirrored tabs demos. Added source-required core `BorderStyle` support to `BorderSide`/`BoxDecoration`; included in the `Plumix` and `Plumix.Material` `0.2.0-alpha.1` feature train.
- Added paired Flutter-structured core `RawAutocomplete<T>` + Material `Autocomplete<T>` ports with sync/async option builders, stale-result suppression, split/custom fields, anchored up/down/most-space views, keyboard highlighting/selection, Material defaults, focused tests, and a mirrored C#/Dart demo. Added source-required `TextEditingValue`/controller-value and `ListView.shrinkWrap` APIs; advanced `Plumix` and `Plumix.Material` to `0.2.0-alpha.1`; overlay-portal and semantics-announcement gaps are tracked in `DIVERGENCES.md`.
- Added paired Flutter-structured `SearchBar` + `SearchAnchor` ports with `SearchController`, search view routes, `SearchBarTheme`/`SearchViewTheme`, M3 defaults, suggestions, focused tests, and a mirrored C#/Dart demo; exact grow-from-anchor animation, async suggestions, and advanced text-input configuration remain tracked in `DIVERGENCES.md`.
- Added paired Flutter-structured `DropdownMenu<T>` + `DropdownMenuFormField<T>` ports with editable/select-only anchors, filtering/search, disabled-entry keyboard traversal, external `MenuController`, close behavior, intrinsic/explicit sizing, `DropdownMenuTheme`/`MenuStyle`, form validation/save/reset, focused tests, and mirrored C#/Dart demo probes. Added source-required editable key interception; advanced `Plumix` to `0.10.0-alpha.1` and `Plumix.Material` to `0.13.0-alpha.1`; shared cascading-menu, text-input configuration, restoration, and advanced style gaps are tracked in `DIVERGENCES.md`.
- Added paired Flutter-structured `TimePickerDialog` + `DateRangePickerDialog` ports with `TimeOfDay`, 12/24-hour dial and validated input modes, `TimePickerTheme`, lazy multi-month range selection, connected endpoint highlighting, range input validation, M2/M3 dialog geometry, typed route results, focused tests, and mirrored C#/Dart demo probes. Added source-required `MediaQuery.alwaysUse24HourFormat`; advanced `Plumix` to `0.9.0-alpha.1` and `Plumix.Material` to `0.12.0-alpha.1`; restoration, IME hint, foldable-anchor, haptic, and advanced keyboard-navigation gaps are tracked in `DIVERGENCES.md`.
- Added paired Flutter-structured `InputDatePickerFormField` + `DatePickerDialog` ports with localized compact-date parsing, range/predicate validation, save/submit flow, calendar/input-only modes, M2/M3 portrait/landscape geometry, theme/action/header precedence, typed `ShowDatePicker` results, focused tests, and mirrored C#/Dart demo probes. Advanced `Plumix.Material` to `0.11.0-alpha.1`; restoration, keyboard-type, locale-override, foldable-anchor, and short-height layout gaps are tracked in `DIVERGENCES.md`.
- Added paired Flutter-structured `TextFormField` + `DropdownButtonFormField<T>` ports on new core `Form`/`FormField<T>` lifecycle primitives, including typed `GlobalKey.CurrentState`, validation/autovalidation, save/reset/error state, controller/focus synchronization, decorated errors/hints, callback ordering, semantics, focused tests, and mirrored C#/Dart demo probes. Advanced `Plumix` to `0.8.0-alpha.1` and `Plumix.Material` to `0.10.0-alpha.1`; route-pop and restoration gaps are tracked in `DIVERGENCES.md`.
- Added paired Flutter-structured `CalendarDatePicker` + `YearPicker` ports with Gregorian/custom calendar delegates, date utilities/localization, M2/M3 `DatePickerTheme`, month paging, day/year selection, predicates, keyboard/focus/live-region semantics, focused tests, and a mirrored C#/Dart demo. Added source-required circle/stadium shape metadata; advanced `Plumix` to `0.7.0-alpha.1` and `Plumix.Material` to `0.9.0-alpha.1`; the bounded rotating page window remains documented in `DIVERGENCES.md`.
- Added paired Flutter-structured `InkResponse` + `InkWell` ports with shared primary/secondary gesture lifecycle, tap-up/long-press-up callbacks, focus/hover/pressed state controller, circle versus contained-rectangle ink geometry, overlay/theme color resolution, feedback, semantics, focused tests, and a mirrored C#/Dart demo. Added source-required gesture and long-press semantics plumbing and fixed tab-indicator geometry invalidation exposed by the stable ink subtree; advanced `Plumix` to `0.6.0-alpha.1` and `Plumix.Material` to `0.8.0-alpha.1`.
- Added paired Flutter-structured `InputDecorator` + Material `TextField` ports with underline/outline borders, filled/hover/focus/error/disabled states, floating labels, hint/helper/error/counter and prefix/suffix slots, `InputDecorationTheme`, read-only/obscured/multiline input, grapheme-aware max length, submission callbacks, focused tests, and a mirrored C#/Dart demo. Expanded core `EditableText` with the source-required style/read-only/obscure/submit/max-length surface; advanced `Plumix` to `0.5.0-alpha.1` and `Plumix.Material` to `0.7.0-alpha.1`.
- Added paired Flutter-structured `SliverAppBar` + `FlexibleSpaceBar` ports with persistent-header layout, parallax/pin collapse, pinned/floating behavior, M3 medium/large variants, app-bar theme elevation/surface fields, aligned transforms, focused tests, and a mirrored C#/Dart demo. Viewport paint/hit order now matches Flutter's `firstIsTop`, keeping pinned headers above scrolling list children; advanced `Plumix` to `0.4.0-alpha.1` and `Plumix.Material` to `0.6.0-alpha.1`.
- Added paired Flutter-structured `BottomSheet` + `ModalBottomSheetRoute<T>` ports with `BottomSheetTheme`, persistent `Scaffold.bottomSheet`/controller/LocalHistory flow, typed modal results, barriers, SafeArea, 9/16 height policy, drag dismissal, animation styles, focused tests, and a mirrored C#/Dart demo; advanced `Plumix.Material` to `0.5.0-alpha.1`.
- Added paired Flutter-structured `TabBar` + `TabBarView` ports with `Tab`, `TabController`/`DefaultTabController`, primary/secondary M2/M3 defaults, `TabBarTheme`, fill/center/scrollable layouts, intrinsic label indicators, divider/custom decoration paint, tap/hover/focus/semantics states, synchronized swipe/indicator animation, and non-adjacent page warping. Added source-required core `PageView`/`PageController`, preferred-size and text-fade primitives, `AppBar.bottom`, focused tests, and a mirrored C#/Dart demo; advanced `Plumix` to `0.3.0-alpha.1` and `Plumix.Material` to `0.4.0-alpha.1`.
- Added paired Flutter-structured `RawScrollbar` + Material `Scrollbar` ports with non-layout overlay paint, vertical/horizontal/RTL geometry, fade/visibility, draggable thumbs, track paging, stateful Material theming, platform defaults, focused tests, and a mirrored C#/Dart demo. Fixed retained hit-test entries to advance local pointer coordinates during move/up routing so host-driven thumb dragging works, and matched Flutter's mouse-proximity fade-in within the scrollbar's expanded 48px hover target without reacting to ordinary content hover. Added the source-required adaptive `CupertinoScrollbar`; advanced `Plumix`/`Plumix.Cupertino` to `0.2.0-alpha.1` and `Plumix.Material` to `0.3.0-alpha.1`.
- Added paired Flutter-structured `MaterialButton` + `RawMaterialButton` ports with the full legacy API/default surface, `ButtonThemeData` color/geometry precedence, focus/hover/press/elevation state resolution, long-press-only enablement, density/tap-target behavior, highlight callbacks, focused tests, and mirrored C#/Dart demo probes. Advanced `Plumix.Material` to `0.2.0-alpha.1`.
- Added paired Flutter-structured `DataTable` + `PaginatedDataTable` ports with `DataColumn`/`DataRow`/`DataCell`, intrinsic/fixed/flex table layout, sorting, checkbox selection, row/cell interactions, `DataTableTheme`, source caching, selected-count headers, rows-per-page and first/previous/next/last navigation, focused tests, and a mirrored C#/Dart demo. Added shared core `Table`/`RenderTable`, complete tap/double-tap/down/cancel gesture plumbing, pagination localizations/icons, and a Flutter-shaped single-child viewport that preserves finite cross-axis sizing for nested horizontal/vertical scroll views; advanced `Plumix`/`Plumix.Material` to `0.1.0-alpha.11`.
- Added hot reload support: assembly-level `MetadataUpdateHandler` (`HotReloadManager`) reassembles all live hosts on the UI thread after .NET Hot Reload deltas apply (`dotnet watch`, IDE Hot Reload), rebuilding the widget tree and re-laying-out/repainting the render tree while preserving `State` objects. Ported Flutter's reassemble chain (`State.Reassemble`, `Element.Reassemble`, `BuildOwner.Reassemble`, host `ReassembleApplication`/`PerformReassemble`) and aligned `BuildOwner.BuildScope` with Flutter's increasing-depth dirty-list processing with a `Dirty` skip so ancestors rebuild before descendants without duplicate rebuilds. Hot reload logs `[Plumix] Hot reload: ...` diagnostics to the console, and a `Ctrl/Cmd+Shift+R` manual reassemble shortcut (active only when metadata updates are enabled or a debugger is attached) works around IDEs that apply deltas without invoking `MetadataUpdateHandler` callbacks (Rider, RIDER-124189). Advanced `Plumix` to `0.1.0-alpha.10`.
- Added paired Flutter-structured `BottomAppBar` + `ButtonBar` ports with M2/M3 defaults, local/global themes, SafeArea sizing, FAB-aware circular notch geometry/clipping, legacy button-theme propagation, row-to-column overflow, RTL/direction handling, focused tests, and a mirrored C#/Dart demo. Added shared notched-shape and geometry-clip primitives; advanced `Plumix` to `0.1.0-alpha.9` and `Plumix.Material` to `0.1.0-alpha.10`.
- Added paired Flutter-structured `RefreshProgressIndicator` + `RefreshIndicator` ports with arrowhead/elevation paint, Material/adaptive/no-spinner variants, drag/armed/snap/refresh/done/canceled lifecycle, programmatic show, localized semantics, focused tests, and a mirrored C#/Dart demo. Expanded core scroll notifications with axis/extents, drag deltas, and overscroll data; advanced `Plumix` to `0.1.0-alpha.8` and `Plumix.Material` to `0.1.0-alpha.9`.
- Added paired Flutter-structured `AboutDialog` + `LicensePage` ports with `AboutListTile`, dialog/license route helpers, localized labels, lazy package license registry/parser, package grouping/detail navigation, focused tests, and a mirrored C#/Dart demo; advanced `Plumix` to `0.1.0-alpha.7` and `Plumix.Material` to `0.1.0-alpha.8`.
- Breaking: `MaterialDialogs.ShowDialog` now follows Flutter's root-navigator default and exposes `useRootNavigator` for explicit nearest/root selection.
- Added paired Flutter-structured `ExpandIcon` + `Stepper` ports with animated disclosure, vertical/horizontal step layouts, state icons/error paint, connectors, default/custom controls, disabled interactions, focused tests, and a mirrored C#/Dart demo. Added semantics hints and polygon paint support; advanced `Plumix` to `0.1.0-alpha.6` and `Plumix.Material` to `0.1.0-alpha.7`.
- Added paired Flutter-structured `DropdownButton<T>` + `DropdownMenuItem<T>` ports with controlled/null/disabled states, hints, selected builders, dense/expanded sizing, underline policy, positioned scroll routes, staged animation, keyboard/focus/semantics, focused tests, and mirrored C#/Dart demo coverage. Added source-required `IndexedStack` and dropdown `ButtonTheme` support; advanced `Plumix`/`Plumix.Material` to `0.1.0-alpha.6`.
- Added paired Flutter-structured `CheckedPopupMenuItem<T>` + `PopupMenuDivider` ports with checkmark fade, selected-state typography, checkbox-role semantics, divider geometry/theming, mixed-entry keyboard traversal, focused tests, and mirrored C#/Dart demo coverage. Expanded semantics-role propagation and advanced `Plumix`/`Plumix.Material` to `0.1.0-alpha.5`.
- Added paired Flutter-structured `PopupMenuButton<T>` + `PopupMenuItem<T>` ports with anchored non-opaque routes, M2/M3 theming, selection/cancel callbacks, disabled items, keyboard traversal, localized semantics/tooltips, shrink-wrapped scrolling, and focused tests plus a mirrored C#/Dart demo. Added `PopupMenuTheme`, `RelativeRect`, `AnimationStyle`, expanded semantics, root-navigator lookup, stable keyed route stacking, and mutable animation duration; advanced `Plumix` to `0.10.0-alpha.1` and `Plumix.Material` to `1.15.0-alpha.1`.
- Added paired Flutter-structured `SimpleDialog` + `SimpleDialogOption` ports with scrollable `ListBody` choices, scaled padding, dialog typography/theme precedence, localized route semantics, enabled/disabled ink interaction, typed-result sample flow, and focused tests. Added shared `ListBody`, `SingleChildScrollView.padding`, and `InkWell` primitives; advanced `Plumix` to `0.9.0-alpha.1` and `Plumix.Material` to `1.14.0-alpha.1`.
- Added paired Flutter-structured `Dialog` + `AlertDialog` ports with `DialogTheme`, M2/M3/fullscreen defaults, intrinsic sizing, scaled slot padding, scrollable content, overflow actions, route semantics, modal barriers, reverse transitions, and typed results. Added non-opaque/result-aware Navigator routes, `Builder`, `IntrinsicWidth`, focused tests, and a mirrored C#/Dart demo; advanced `Plumix` to `0.8.0-alpha.1` and `Plumix.Material` to `1.13.0-alpha.1`.
- Added paired Flutter-structured `SnackBarAction` + `SnackBar` ports with M2/M3 defaults, `SnackBarTheme`, fixed/floating composition, measured action overflow, one-shot actions, close/swipe/dismiss semantics, animation, and a `ScaffoldMessenger` queue with closed reasons. Added focused tests and a mirrored C#/Dart runtime demo; advanced `Plumix.Material` to `1.12.0-alpha.1`.
- Added Flutter-structured `Banner` + `MaterialBanner` ports with exact diagonal ribbon geometry/paint, M2/M3 defaults, `MaterialBannerTheme`, single-row/below/overflow action layout, elevation/divider/text-scale behavior, animation/accessibility semantics, focused tests, and a mirrored C#/Dart demo. Added shared `CustomPaint`, `FractionalTranslation`, `OverflowBar`, clamped text scaling, accessible-navigation data, and live-region/dismiss semantics; advanced `Plumix` to `0.7.0-alpha.1` and `Plumix.Material` to `1.11.0-alpha.1`. Banner presentation through `ScaffoldMessenger` remains a documented divergence.
- Added paired Flutter-structured `ToggleButtons` + `SegmentedButton<T>` ports with bool-list and typed-set selection models, exclusive/multi/empty rules, horizontal/vertical equalized layout, expanded insets, selected icons, per-segment enablement/tooltips, M2/M3 state colors, borders, tap targets, themes, `styleFrom`, focus/hover/press behavior, checked/selected/group semantics, focused tests, and a mirrored C#/Dart runtime demo. Expanded shared `ButtonStyle` cursor/density/duration/feedback fields and semantics grouping; advanced `Plumix` to `0.6.0-alpha.1` and `Plumix.Material` to `1.10.0-alpha.1`.
- Added paired Flutter-structured `NavigationDrawer` + `NavigationDrawerDestination` ports with header/footer slots, mixed custom children, destination-only indexing, controlled/nullable selection, disabled states, animated indicators, localized semantics, `NavigationDrawerTheme`/`ThemeData.NavigationDrawerTheme`, focused tests, and a mirrored C#/Dart runtime demo. Added Drawer surface-tint plumbing required by the source composition; advanced `Plumix.Material` to `1.9.0-alpha.1`.
- Added paired Flutter-structured `DrawerHeader` + `UserAccountsDrawerHeader` ports with status-bar-aware sizing/padding, animated decoration, bottom divider, account picture slots, details toggle/arrow, localized semantics, RTL geometry, focused tests, and a mirrored runtime demo. Added the shared `fastOutSlowIn` cubic curve; advanced `Plumix` to `0.5.0-alpha.1` and `Plumix.Material` to `1.8.0-alpha.1`.
- Added paired Flutter-structured `DrawerButton` + `EndDrawerButton` ports with standalone icon widgets, `ActionIconTheme` builders, localized tooltip/Android semantics, custom and default `Scaffold` open actions, AppBar implied drawer-action integration, focused tests, and expanded mirrored action-button demos. Advanced `Plumix.Material` to `1.7.0-alpha.1`.
- Added paired Flutter-structured `BackButton` + `CloseButton` ports with platform-specific icon widgets, `ActionIconTheme`/`ThemeData.ActionIconTheme`, localized tooltips, custom/default `Navigator.MaybePop` actions, style precedence, AppBar implied-leading integration, focused tests, and a mirrored C#/Dart demo. Advanced `Plumix.Material` to `1.6.0-alpha.1`.
- Added paired Flutter-structured `GridTile` + `GridTileBar` ports with exact header/footer overlay geometry, one/two-line bar sizing and typography, leading/trailing slots, directional padding/RTL behavior, focused tests, and a mirrored C#/Dart demo. Added inherited text overflow/wrapping, explicit flex text direction, and dark `bodySmall` theme tokens; advanced `Plumix` to `0.4.0-alpha.1` and `Plumix.Material` to `1.5.0-alpha.1`.
- Extended the Flutter-structured chip family with paired `FilterChip` + `InputChip` ports: M2/M3 and elevated filter defaults, selectable/pressable/delete-only input states, independent animated delete slots, localized delete tooltips, delete constraints/colors/semantics, focused coverage, and expanded mirrored demos. Advanced `Plumix.Material` to `1.4.0-alpha.1`.
- Added paired Flutter-structured `ActionChip` + `ChoiceChip` ports over a shared `RawChip`, with flat/elevated variants, M2/M3 defaults, `ChipTheme`/`ThemeData.ChipTheme`, state-color precedence, avatar/checkmark composition, focus/hover/press/tap semantics, `VisualDensity`, scaled label padding, focused tests, and a mirrored C#/Dart runtime demo. Advanced `Plumix.Material` to `1.3.0-alpha.1`.
- Added paired Flutter-structured `NavigationBar` + `NavigationRail` ports with M2/M3 defaults, themes, destination/disabled states, selection indicators, label modes, safe-area handling, semantics, and mirrored C#/Dart runtime probes. Expanded Material typography/surface tokens and advanced `Plumix.Material` to `1.2.0-alpha.1`.
- Added a Flutter-structured `CircleAvatar` port with M2/M3 color defaults, radius constraints, foreground/background image fallback, error callbacks, child typography/icon theming, implicit 200ms transitions, and a mirrored C#/Dart runtime demo. Core prerequisites now include circular/foreground decoration paint, `AnimatedContainer`, decoration/image interpolation, and bitmap sampling controls; advanced `Plumix` to `0.3.0-alpha.1` and `Plumix.Material` to `1.1.0-alpha.1`.
- Hardened failed image-stream caching so listeners attached after asynchronous completion still receive the cached error instead of observing a prematurely disposed completer.
- Added Flutter-shaped `ImageProvider`/`ImageStream`/`ImageCache` primitives and `DecorationImage` painting for memory, file, network, asset-resolution, resize, fit, repeat, RTL flip, opacity, clipping, and nine-patch paths. Animated frames and backend color-matrix/inversion effects remain documented divergences.
- Breaking: expanded Material `Tooltip` from its baseline constructor to Flutter-shaped nullable/theme-resolved defaults and advanced `Plumix.Material` to `1.0.0-alpha.1`; also added paired `Badge` support, focused coverage, and a mirrored C#/Dart runtime demo. Remaining shared overlay/rich-text gaps are tracked in `docs/ai/DIVERGENCES.md`.
- Fixed recursive `BoxConstraints.ToString()` diagnostics so layout failures report constraints instead of overflowing the stack.
- Added paired Material `ExpansionPanel` + `ExpansionPanelList` parity baseline:
  - introduced Flutter-shaped `ExpansionPanel`/`ExpansionPanelRadio` descriptors and normal/radio list constructors with controlled expansion, unique radio values, initial-open selection, mutually exclusive state, and Dart callback ordering;
  - added reusable `MergeableMaterial`, `MaterialSlice`, and `MaterialGap` primitives with keyed animated gaps, merged slice groups, divider/color/elevation handling, and card-edge clipping;
  - matched header/icon tap gating, 48px minimum headers, expanded-header padding, rotating expand icon, clipped height/body-opacity animation, material-gap sizing, per-panel backgrounds, and expanded-state semantics;
  - added focused regression coverage and a mirrored C#/Dart runtime page for controlled and radio panel-list paths.
- Added paired Material `RadioListTile<T>` + `ExpansionTile` parity baseline:
  - introduced modern inherited `RadioGroup<T>` coordination plus legacy `groupValue/onChanged`, toggleable selection, adaptive Cupertino routing, control affinity, selected-color precedence, scale transforms, and merged radio semantics;
  - introduced reusable framework `ExpansibleController`/`Expansible` with animated clipped height, external controller lifecycle, `maintainState`, and forward/reverse curves, plus self-bounds `ClipRect` support;
  - added `ExpansionTileThemeData`/`ExpansionTileTheme`/`ThemeData.ExpansionTileTheme`, controller/tap expansion, animated arrow/header/background/shape transitions, leading/trailing affinity, disabled behavior, and expanded-state semantics;
  - added focused regression coverage and a mirrored C#/Dart runtime page for group/toggle/adaptive radio paths and controller/theme/animation expansion paths.
- Added paired Material `CheckboxListTile` + `SwitchListTile` parity baseline:
  - introduced Flutter-like controlled APIs, whole-row toggle behavior, checkbox tristate cycling, `ListTileControlAffinity` widget/theme resolution, selected-color precedence, shrink-wrap embedded controls, adaptive factories, and merged checked/enabled/tap semantics;
  - added shared `MergeSemantics` and `ExcludeFocus` widget primitives, `MaterialButtonCore`/`ListTile` focus-change propagation, and the Material secondary color token used by selected list-tile controls;
  - fixed `ListTile` layout-field wiring and leading-slot shrink sizing so leading controls no longer expand a tile to the available viewport height;
  - added focused `MaterialListTileTests` coverage and a mirrored C#/Dart runtime page for enabled/disabled, affinity, tristate, selected, and adaptive paths.
- Expanded framework Material `Slider` parity in `src/Plumix.Material/Slider.cs`:
  - added Flutter-like `secondaryTrackValue` + `secondaryActiveColor` API surface with constructor guards for finite/range-valid secondary values;
  - expanded slider theming surface in `src/Plumix.Material/SliderTheme.cs` with `secondaryActiveTrackColor` and `disabledSecondaryActiveTrackColor`, and wired resolution precedence (`widget -> SliderTheme -> defaults`) for interactive/disabled secondary-track paint;
  - added semantics formatter parity to `Slider` (`semanticFormatterCallback`) and precedence over static `semanticLabel` within current framework semantics-label surface;
  - expanded focused coverage in `src/Plumix.Tests/MaterialSliderTests.cs` for secondary-track validation/normalization, default/theme/widget secondary color precedence (including disabled path), and semantic formatter precedence;
  - updated C#/Dart runtime parity demos (`src/Sample/Plumix.Sample/Demos/Material/SliderDemoPage.cs`, `dart_sample/lib/demos/material/slider_demo_page.dart`) with `secondaryTrackValue`/`secondaryActiveColor` probes and secondary-track stepping controls.
- Added framework Material `RangeSlider` baseline:
  - introduced `RangeSlider` + `RangeValues` in `src/Plumix.Material/RangeSlider.cs` with two-thumb controlled range API (`values`, `onChanged`, `onChangeStart`, `onChangeEnd`), range/division guards, pointer drag/tap thumb selection, focused keyboard adjustment baseline, and range semantics formatting support (`semanticFormatterCallback`);
  - reused slider theming surface (`SliderThemeData` + inherited `SliderTheme`) with `ThemeData.SliderTheme` integration for mode-aware fallback defaults and widget-over-theme precedence for active/inactive/overlay color paths;
  - added focused regression coverage in `src/Plumix.Tests/MaterialRangeSliderTests.cs` for constructor guards, M2/M3 defaults, theme/widget color precedence, discrete snapping + change lifecycle callbacks, keyboard adjustment baseline, and semantics label/flags propagation;
  - added C#/Dart sample parity runtime probes (`src/Sample/Plumix.Sample/Demos/Material/RangeSliderDemoPage.cs`, `dart_sample/lib/demos/material/range_slider_demo_page.dart`) and route/menu wiring updates (`src/Sample/Plumix.Sample/SampleGalleryScreen.cs`, `dart_sample/lib/sample_routes.dart`, `dart_sample/lib/sample_gallery_screen.dart`).
- Added framework Material `Slider` baseline:
  - introduced `Slider` in `src/Plumix.Material/Slider.cs` with controlled-value API (`value`, `onChanged`, `onChangeStart`, `onChangeEnd`), range/division guards, pointer drag/tap updates, keyboard adjustments (including RTL-aware direction handling), and slider semantics wiring;
  - added slider theming surface via `SliderThemeData` + inherited `SliderTheme` (`src/Plumix.Material/SliderTheme.cs`) and integrated `ThemeData.SliderTheme` in `src/Plumix.Material/ThemeData.cs`;
  - added focused regression coverage in `src/Plumix.Tests/MaterialSliderTests.cs` for constructor guards, M2/M3 defaults, theme/widget color precedence, discrete snapping + change callback lifecycle, keyboard adjustment behavior, and semantics flags/label propagation;
  - added C#/Dart sample parity runtime probes (`src/Sample/Plumix.Sample/Demos/Material/SliderDemoPage.cs`, `dart_sample/lib/demos/material/slider_demo_page.dart`) and route/menu wiring updates (`src/Sample/Plumix.Sample/SampleGalleryScreen.cs`, `dart_sample/lib/sample_routes.dart`, `dart_sample/lib/sample_gallery_screen.dart`).
- Closed sample route/page parity drift for `Bloc counter`:
  - added Dart sample route constant `/bloc-counter` and menu wiring (`dart_sample/lib/sample_routes.dart`, `dart_sample/lib/sample_gallery_screen.dart`);
  - added Dart `Bloc counter` demo page mirroring C# behavior (`dart_sample/lib/demos/general/bloc_counter_demo_page.dart`) with `BlocProvider` + `BlocBuilder` + `BlocListener` + `BlocSelector` and restartable refresh-event handling.
- Closed remaining progress-indicator `valueColor` API divergence with Flutter usage patterns:
  - introduced framework `IValueListenable<T>` and `AlwaysStoppedAnimation<T>` (`src/Plumix/Foundation/Listenable.cs`);
  - switched `LinearProgressIndicator` and `CircularProgressIndicator` `valueColor` surface from `ValueNotifier<Color?>` to `IValueListenable<Color?>` (`src/Plumix.Material/ProgressIndicator.cs`) while preserving live listener-driven updates and null fallback behavior;
  - added focused regression coverage for constant animation-style color sources in `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs` and `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`;
  - aligned C# progress-indicator demos with Dart usage style by wiring `AlwaysStoppedAnimation<Color?>` for the explicit `valueColor` toggle path (`src/Sample/Plumix.Sample/Demos/Material/LinearProgressIndicatorDemoPage.cs`, `src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`).
- Closed adaptive semantics parity for framework `CircularProgressIndicator`:
  - adaptive iOS/macOS branch now preserves progress semantics wrapping (`Semantics`) instead of bypassing it via early return;
  - adaptive determinate indicators now expose the same computed percentage fallback (`semanticsLabel + semanticsValue/percent`) as the Material path;
  - expanded focused coverage in `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs` with an iOS adaptive semantics-label regression test.
- Closed adaptive-platform parity for framework `CircularProgressIndicator`:
  - added `CircularProgressIndicator.Adaptive(...)` in `src/Plumix.Material/ProgressIndicator.cs` with Flutter-like platform routing (`iOS/macOS -> Cupertino`, other platforms -> Material);
  - introduced reusable `CupertinoActivityIndicator` in `src/Plumix.Cupertino/CupertinoActivityIndicator.cs` with animated and partially-revealed modes used by adaptive circular progress;
  - adaptive iOS/macOS path now maps determinate progress to partially revealed Cupertino ticks and keeps Material-only circular styling parameters out of the Cupertino render path;
  - expanded focused coverage in `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs` for adaptive iOS indeterminate/determinate behavior and Android adaptive fallback to Material render path.
- Expanded framework progress-indicator color parity in `src/Plumix.Material/ProgressIndicator.cs`:
  - added Flutter-like `valueColor` precedence for both `LinearProgressIndicator` and `CircularProgressIndicator` (`valueColor -> color -> ProgressIndicatorThemeData.color -> theme primary`);
  - introduced live `valueColor` updates through `ValueNotifier<Color?>` listener wiring in indicator state lifecycles, including null-value fallback to `color`/theme/default paths;
  - expanded focused regression coverage in `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs` and `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs` for precedence, null fallback, and runtime notifier-driven color updates;
  - updated C#/Dart runtime parity demos (`src/Sample/Plumix.Sample/Demos/Material/LinearProgressIndicatorDemoPage.cs`, `src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`, `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`, `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`) with explicit `valueColor` toggles.
- Expanded framework Material `LinearProgressIndicator` parity in `src/Plumix.Material/ProgressIndicator.cs`:
  - added Flutter-like `year2023` API/theme precedence (`widget.year2023 -> ProgressIndicatorThemeData.year2023 -> default true` in M3);
  - aligned M3 2023/2024 default switching behavior for stop-indicator visibility, track-gap visibility, and default border radius (`2023`: square/no stop/no gap; `2024`: rounded with stop+gap defaults);
  - expanded focused regression coverage in `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs` for M3 `year2023` true/false defaults and updated theme/widget precedence assertions;
  - updated C#/Dart runtime parity demos (`src/Sample/Plumix.Sample/Demos/Material/LinearProgressIndicatorDemoPage.cs`, `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`) with an explicit `year2023` toggle and theme/widget probe wiring.
- Expanded framework Material `LinearProgressIndicator` parity in `src/Plumix.Material/ProgressIndicator.cs`:
  - added Flutter-like API surface for linear M3 stop/gap styling (`stopIndicatorColor`, `stopIndicatorRadius`, and `trackGap`) with constructor guards for non-finite/negative values;
  - wired precedence for new fields as `widget -> ProgressIndicatorThemeData -> mode defaults` (`M3` defaults: stop color `primary`, stop radius `2`, track gap `4`; `M2`: stop/gap disabled);
  - added Flutter-like external controller support (`controller`) with animation-source precedence `widget.controller -> ProgressIndicatorThemeData.controller -> internal controller`, plus constructor guard for invalid `value + controller` usage;
  - updated linear painter choreography to apply track-gap logic across determinate and indeterminate phases and draw the determinate stop-indicator cap at the trailing edge;
  - expanded focused regression coverage in `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs` for new constructor guards, M2/M3 stop/gap defaults, and theme/widget precedence for `stopIndicator*` + `trackGap` + explicit/theme controller usage;
  - updated C#/Dart runtime parity demos (`src/Sample/Plumix.Sample/Demos/Material/LinearProgressIndicatorDemoPage.cs`, `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`) to probe `ProgressIndicatorTheme` and widget-level stop/gap overrides.
- Expanded framework Material `CircularProgressIndicator` parity in `src/Plumix.Material/ProgressIndicator.cs`:
  - added Flutter-like API surface for circular M3 track-gap styling (`trackGap`) with constructor guards for non-finite/negative values;
  - wired circular `trackGap` precedence as `widget -> ProgressIndicatorThemeData -> mode defaults` (`M3` default `4`; `M2` disabled/null);
  - updated circular determinate track paint choreography to render a Flutter-like gap between active arc and background track when `trackGap` is set;
  - added Flutter-like `strokeCap` API for circular progress (enum `StrokeCap`) with `widget -> ProgressIndicatorThemeData.CircularStrokeCap -> default-null` precedence;
  - updated circular render paint behavior to apply Flutter-like null-cap defaults (`determinate=butt`, `indeterminate=square`) and explicit `strokeCap` mapping for both foreground and track arc paint;
  - added Flutter-like `strokeAlign` API for circular progress with finite-value guards and `widget -> ProgressIndicatorThemeData.CircularStrokeAlign -> default(0.0)` precedence;
  - updated circular arc geometry resolution to apply Flutter-like stroke alignment offsets in paint (`inside/center/outside` via signed stroke offset);
  - added Flutter-like circular `constraints` support with precedence `widget.constraints -> widget.size (legacy fallback) -> theme.circularConstraints -> theme.circularSize (legacy fallback) -> mode defaults` (`M3`: `40x40`, `M2`: `36x36`) and `ConstrainedBox` composition;
  - added Flutter-like `year2023` support (`widget.year2023 -> ProgressIndicatorThemeData.year2023 -> default true` in M3) to switch 2023/2024 defaults (track visibility, track-gap enablement, stroke-align default, default constraints, and implicit indicator line-cap behavior);
  - added Flutter-like external controller support (`controller`) with animation-source precedence `widget.controller -> ProgressIndicatorThemeData.controller -> internal controller`, plus constructor guard for invalid `value + controller` usage;
  - expanded focused regression coverage in `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs` for constructor guards, M2/M3 `trackGap` defaults, and theme/widget precedence for `trackGap` + `strokeCap` + `strokeAlign` + `constraints` + `year2023` + explicit/theme controller usage (including retained legacy `size` fallback checks);
  - updated C#/Dart runtime parity demos (`src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`, `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`) to probe `ProgressIndicatorTheme` and widget-level `trackGap` + `strokeCap` + `strokeAlign` + `constraints` + `year2023` overrides.
- Expanded framework Material drawer gesture parity coverage with focused RTL regression tests in `src/Plumix.Tests/MaterialScaffoldTests.cs`:
  - added RTL edge-open coverage for `Scaffold.drawer` (start drawer opens from the right edge in RTL with leftward drag);
  - added RTL edge-open coverage for `Scaffold.endDrawer` (end drawer opens from the left edge in RTL with rightward drag);
  - added RTL `MediaQuery.padding` edge-activation coverage for both sides (start drawer uses right padding extension zone; end drawer uses left padding extension zone).
- Expanded framework Material drawer interaction gating coverage in `src/Plumix.Tests/MaterialScaffoldTests.cs`:
  - added scrim-tap dismiss coverage for open drawers when `drawerBarrierDismissible=true`;
  - added negative scrim-tap coverage to confirm drawers stay open when `drawerBarrierDismissible=false`;
  - added edge-drag disable coverage for `drawerEnableOpenDragGesture=false` and `endDrawerEnableOpenDragGesture=false`;
  - added desktop-platform gesture gating coverage to confirm edge drag does not open drawers on `TargetPlatform.Windows`.
- Expanded framework Material drawer stress coverage for rapid alternating gestures in `src/Plumix.Tests/MaterialScaffoldTests.cs`:
  - added start->end alternating drag choreography coverage (open start by edge drag, close by panel drag, then open end by edge drag);
  - added end->start alternating drag choreography coverage (open end by edge drag, close by panel drag, then open start by edge drag);
  - both scenarios assert mutual exclusion (`start` and `end` drawers cannot remain open together) and panel visibility consistency after each settle step.
- Added C#/Dart sample parity runtime probes for framework Material `Drawer`:
  - added `Drawer` demo pages in both samples (`src/Sample/Plumix.Sample/Demos/Material/DrawerDemoPage.cs`, `dart_sample/lib/demos/material/drawer_demo_page.dart`) with explicit start/end drawer open/close choreography controls;
  - added mode/theme/widget precedence probes for drawer visuals and scrim resolution (`UseMaterial3`, `DrawerTheme`, and widget-level `Drawer`/`Scaffold.drawerScrimColor` overrides);
  - wired the new route into both sample menus (`/drawer`) under the Material tab.
- Added framework Material `Divider` baseline:
  - introduced `Divider` and `VerticalDivider` in `src/Plumix.Material/Divider.cs` with Flutter-like mode-aware defaults (`M3`: `outlineVariant` + `thickness=1`; `M2`: `dividerColor` + hairline logical thickness), plus widget overrides for `space`/`thickness`/`indent`/`endIndent`/`color`/`radius`;
  - added `DividerThemeData` + inherited `DividerTheme` in `src/Plumix.Material/DividerTheme.cs` and integrated `ThemeData.DividerTheme` + `ThemeData.DividerColor` in `src/Plumix.Material/ThemeData.cs`;
  - added focused regression coverage in `src/Plumix.Tests/MaterialDividerTests.cs` for constructor guards, M2/M3 defaults, theme/widget precedence, and vertical-divider layout behavior;
  - added C#/Dart sample parity runtime probes (`src/Sample/Plumix.Sample/Demos/Material/DividerDemoPage.cs`, `dart_sample/lib/demos/material/divider_demo_page.dart`) with route/menu wiring updates.
- Added framework Material `LinearProgressIndicator` baseline:
  - introduced `LinearProgressIndicator` in `src/Plumix.Material/ProgressIndicator.cs` with determinate (`value`) and indeterminate (`value: null`) modes, Flutter-like two-segment indeterminate timing, RTL-aware fill direction, and mode-aware defaults (`M3`: `primary` on `secondaryContainer` with rounded radius; `M2`: `primary` on `canvas` with square radius);
  - added progress-indicator theming surface via `ProgressIndicatorThemeData` + inherited `ProgressIndicatorTheme` (`src/Plumix.Material/ProgressIndicatorTheme.cs`) and `ThemeData.ProgressIndicatorTheme` integration (`src/Plumix.Material/ThemeData.cs`);
  - added focused regression coverage in `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs` for constructor guards, M2/M3 defaults, theme/widget precedence, value clamping, indeterminate animation advancement, RTL resolution, and semantics percentage fallback;
  - added C#/Dart sample parity runtime probes (`src/Sample/Plumix.Sample/Demos/Material/LinearProgressIndicatorDemoPage.cs`, `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`) with route/menu wiring updates.
- Added framework Material `CircularProgressIndicator` baseline:
  - introduced `CircularProgressIndicator` in `src/Plumix.Material/ProgressIndicator.cs` with determinate (`value`) and indeterminate (`value: null`) modes, Flutter-like indeterminate arc timing/choreography (`1333*2222` timeline with head/tail/rotation composition), and mode-aware defaults (`M3` determinate default track uses `secondaryContainer`; `M2` has no default track);
  - expanded progress-indicator theming surface in `src/Plumix.Material/ProgressIndicatorTheme.cs` with circular baseline fields (`CircularTrackColor`, `CircularStrokeWidth`, `CircularSize`) and reused `ThemeData.ProgressIndicatorTheme` precedence wiring;
  - added a reusable arc drawing primitive in `src/Plumix/Rendering/Object.PaintingContext.cs` (`DrawArc(...)`) used by circular progress paint;
  - added focused regression coverage in `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs` for constructor guards, M2/M3 defaults, theme/widget precedence, value clamping, indeterminate arc animation progression, and semantics percentage fallback;
  - added C#/Dart sample parity runtime probes (`src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`, `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`) and route/menu wiring updates.
- Fixed renamed `Plumix` satellite assembly access by granting `Plumix.Material` and `Plumix.Cupertino` friend access to framework internals needed for inherited/render-object widget implementations.
- Added framework Material `Card` baseline:
  - introduced `Card` in `src/Flutter.Material/Card.cs` with elevated, filled, and outlined variants, Flutter-like default margin (`4`), clipping policy, elevation/shadow rendering, surface-tint application, outlined border defaults, and `semanticContainer` wiring;
  - added `CardThemeData` + inherited `CardTheme` and `ThemeData.CardTheme` integration, plus M3 card tokens (`surface`, `outlineVariant`) and M2 `cardColor` fallback;
  - added a minimal rounded-rectangle `ShapeBorder` primitive used by card shape/side resolution and extended framework `Semantics` annotations with `container` / `explicitChildNodes`;
  - added focused regression coverage in `src/Flutter.Tests/MaterialCardTests.cs` for constructor guards, M3/M2 defaults, variant behavior, theme/widget precedence, surface tint, clipping, and semantic-container wiring;
  - added C#/Dart sample parity runtime probes (`src/Sample/Flutter.Net/Demos/Material/CardDemoPage.cs`, `dart_sample/lib/demos/material/card_demo_page.dart`) and route/menu wiring updates.
- Added framework Material `ListTile` baseline:
  - introduced `ListTile` in `src/Flutter.Material/ListTile.cs` with one/two/three-line composition (`leading`/`title`/`subtitle`/`trailing`), M3 baseline heights (`56`/`72`/`88`) plus dense defaults (`48`/`64`/`76`), selected/disabled color handling, and interaction wiring through `MaterialButtonCore` (`tap`, `long-press`, hover/focus/pressed states, cursor, semantics);
  - added list-tile theming surface in `src/Flutter.Material/ListTileTheme.cs` and `ThemeData.ListTileTheme` integration in `src/Flutter.Material/ThemeData.cs`;
  - added focused regression coverage in `src/Flutter.Tests/MaterialListTileTests.cs` for constructor guards, default heights, selected/disabled colors, theme precedence, tap dispatch, and selected/enabled semantics;
  - added C#/Dart sample parity runtime probes (`src/Sample/Flutter.Net/Demos/Material/ListTileDemoPage.cs`, `dart_sample/lib/demos/material/list_tile_demo_page.dart`) and route wiring updates in sample menus;
  - aligned `ListTile` text/layout behavior with Flutter defaults: title now defaults to one line, subtitle defaults to one line (`two-line`) or two lines (`three-line`) with ellipsis for plain `Text`, and tile body now shrink-wraps vertically (`Align.heightFactor=1`) so long subtitles do not trigger `RenderFlex` bottom overflow stripes in demo states;
  - fixed `RenderAlign` unbounded-axis sizing in `src/Flutter/Rendering/Proxy.RenderBox.cs` (shrink-wrap when parent axis is unbounded), which removed `ListTile` demo `RenderFlex` right-overflow indicators.
- Closed the requested parity triplet for Material shell/buttons and environment insets coverage:
  - expanded `src/Flutter.Tests/SafeAreaTests.cs` with explicit `MediaQueryData.RemoveViewInsets(...)` edge coverage (selected-side zeroing, view-padding adjustment, and clamp-to-zero behavior when insets exceed padding);
  - expanded `src/Flutter.Tests/MaterialButtonsTests.cs` with missing `FilledButton` advanced style-matrix coverage:
    - resolver-null fallback parity (`ForegroundColor`/`OverlayColor` fallback to lower-priority defaults),
    - `FilledButton.StyleFrom(overlayColor: ...)` hover/pressed opacity priority and transparent-overlay behavior;
  - expanded `src/Flutter.Tests/MaterialScaffoldTests.cs` with deeper end-drawer choreography coverage:
    - velocity-based open/close settle for `endDrawer`,
    - drag-cancel settle below/above threshold for `endDrawer`.
- Closed dedicated Material drawer-theming parity in framework `Scaffold`/`Drawer`:
  - added `DrawerThemeData` + inherited `DrawerTheme` (`src/Flutter.Material/DrawerTheme.cs`) and global `ThemeData.DrawerTheme` surface (`src/Flutter.Material/ThemeData.cs`);
  - `Drawer` visuals now resolve by `widget -> drawerTheme -> mode-aware defaults` for background/elevation/shadow/width with finite/non-negative guards for themed width/elevation values;
  - `Scaffold` scrim color now resolves by `drawerScrimColor -> drawerTheme.scrimColor -> default`;
  - drawer drag progress/settle width math now respects themed drawer width (`ThemeData.DrawerTheme.Width`) instead of only widget width/default width.
- Added focused drawer-theme regression coverage in `src/Flutter.Tests/MaterialScaffoldTests.cs`:
  - drawer visual precedence (`widget` overrides `DrawerTheme`; `DrawerTheme` overrides defaults),
  - invalid themed width/elevation guards,
  - themed-width drag-threshold behavior for cancel-settle decisions,
  - scaffold scrim precedence (`widget` scrim override vs `DrawerTheme` scrim fallback).
- Stabilized full test-suite ordering for navigation/material interaction tests:
  - added test-only `NavigatorBackButtonDispatcher.ResetForTests()` in `src/Flutter/Widgets/Navigation.cs`;
  - moved `NavigationTests`, `MaterialScaffoldTests`, and `MaterialButtonsTests` into serial scheduler collection (`SchedulerTestCollection`) and reset relevant global test state in constructors;
  - removed order-dependent `Navigator.TryHandleBackButton`/fullscreen-dialog app-bar leading/button-overlay flakes in full `Flutter.Tests` runs.
- Closed the remaining framework drawer gesture-controller parity gaps in `src/Flutter.Material/Scaffold.cs` and shared gesture primitives:
  - `GestureDetector`/`RawGestureDetector` now expose horizontal and vertical drag-cancel callbacks;
  - `DragGestureRecognizer` now reports `DragEndDetails.PrimaryVelocity` in real pixels per second from pointer timestamps instead of a frame-rate-scaled delta hint;
  - drawer drag release now consumes the px/s velocity directly, and pointer cancel settles open/closed by the Flutter half-progress threshold.
- Added focused drawer and gesture regression coverage:
  - verifies horizontal drag velocity is reported in px/s;
  - verifies drawer drag cancel settles closed below half progress and open above half progress.
- Fixed Material button ripple clipping composition in `src/Flutter.Material/Buttons.cs`: rounded button splashes now rely on the surrounding `ClipRRect` instead of enabling an extra internal `RenderInkSplash` bounds clip, matching existing clip-shape coverage.
- Closed the remaining framework `BottomNavigationBar` localization gap:
  - added Material localization primitives in `src/Flutter.Material/MaterialLocalizations.cs` (`MaterialLocalizations`, `DefaultMaterialLocalizations`, and inherited `MaterialLocalizationsScope`);
  - bottom-navigation index-label semantics now resolve through `MaterialLocalizations.TabLabel(...)` instead of fixed string formatting in `src/Flutter.Material/BottomNavigationBar.cs`;
  - added focused regression coverage in `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs` to verify local `MaterialLocalizationsScope` override for index-label semantics.
- Hardened framework drawer interaction parity in `src/Flutter.Material/Scaffold.cs`:
  - edge-drag activation width now follows Flutter behavior (`20dp + MediaQuery.padding` on the opening edge) when `drawerEdgeDragWidth` is not explicitly provided;
  - settle choreography now uses Flutter-aligned constants (`_kMinFlingVelocity=365`, `_kBaseSettleDuration=246ms`) with linear settle curve and velocity-aware settle duration;
  - drag-release open/close decisions now prioritize fling threshold and only fall back to progress threshold when fling velocity is not met.
- Added focused drawer regression coverage in `src/Flutter.Tests/MaterialScaffoldTests.cs`:
  - verifies start-drawer edge drag can begin from the `MediaQuery.padding` extension zone.
- Extended framework Material `FloatingActionButton` parity in `src/Flutter.Material/FloatingActionButton.cs`:
  - added `tooltip` API support for all FAB constructors (`regular`, `small`, `large`, `extended`);
  - FAB build composition now wraps with framework `Tooltip` when a non-empty message is provided.
- Extended framework Material `FloatingActionButton` API parity in `src/Flutter.Material/FloatingActionButton.cs`:
  - added constructor/factory API fields for `heroTag`, `mouseCursor`, `enableFeedback`, and `clipBehavior`;
  - `clipBehavior` is now wired into shared button composition (`MaterialButtonCore`) and controls whether FAB content/splash is clipped to shape.
- Added focused tooltip regression coverage in `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`:
  - verifies FAB tooltip appears on hover enter and hides after hover exit animation completion.
- Added focused FAB regression coverage in `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`:
  - verifies default `clipBehavior` does not insert `RenderClipRRect`,
  - verifies explicit `clipBehavior` inserts `RenderClipRRect`,
  - verifies FAB stores `heroTag`/`mouseCursor`/`enableFeedback` values.
- Closed framework-scope runtime cursor + feedback wiring for Material FAB/buttons:
  - introduced framework feedback primitive (`src/Flutter/UI/Feedback.cs`) and hooked `MaterialButtonCore` tap/long-press + keyboard activation paths to dispatch feedback when `enableFeedback` resolves true;
  - `FloatingActionButton` now resolves `enableFeedback` by Flutter-like precedence (`widget -> floatingActionButtonTheme -> defaults`) with new theme surface support (`FloatingActionButtonThemeData.EnableFeedback`);
  - `MaterialButtonCore` now applies interactive mouse cursor requests through `MouseCursorManager`, and `FloatingActionButton` resolves cursor by precedence (`widget -> floatingActionButtonTheme -> defaults`) with new theme surface support (`FloatingActionButtonThemeData.MouseCursor`);
  - `FlutterHost` now subscribes to framework cursor and feedback channels (`MouseCursorManager` / `Feedback`) to apply host pointer cursor updates and provide host feedback dispatch hook (`OnFrameworkFeedback`).
- Expanded focused FAB regression coverage in `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`:
  - verifies default hover cursor fallback (`click`) and theme-level cursor override application via `MouseCursorManager`,
  - verifies keyboard activation feedback dispatch for default FAB behavior,
  - verifies feedback suppression for widget-level and theme-level `enableFeedback: false`.
- Closed framework-scope runtime hero transitions for FAB tags:
  - added framework `Hero` primitive in `src/Flutter/Widgets/Hero.cs` (tag registration + render-bounds snapshotting + hero hide/show during active flights);
  - extended `Navigator` in `src/Flutter/Widgets/Navigation.cs` with shared-tag push/pop hero-flight choreography (temporary dual-route composition, animated overlay flight, and deferred disposal of popped routes until flight completion);
  - `FloatingActionButton` build output is now wrapped with `Hero(tag: heroTag, ...)` when `heroTag` is provided in `src/Flutter.Material/FloatingActionButton.cs`;
  - hero flight bounds interpolation now supports destination-priority `Hero.createRectTween` with linear `RectTween` fallback (`src/Flutter/Widgets/Hero.cs`, `src/Flutter/Widgets/Navigation.cs`, `src/Flutter/AnimationController.cs`);
  - hero flight shuttle composition now supports destination-priority `Hero.flightShuttleBuilder` (with source fallback and destination-child default) in `src/Flutter/Widgets/Hero.cs` and `src/Flutter/Widgets/Navigation.cs`;
  - hidden-hero placeholder composition now supports `Hero.placeholderBuilder` with size metadata resolved from flight snapshots in `src/Flutter/Widgets/Hero.cs`;
  - default hidden-hero placeholder behavior now follows Flutter-like push/pop semantics in `src/Flutter/Widgets/Hero.cs`: push-source hero keeps child under `Offstage` in a fixed-size `SizedBox`, while push-destination and both pop placeholders use fixed-size empty `SizedBox`.
  - hero flight lifecycle now supports push-to-pop diversion in `src/Flutter/Widgets/Navigation.cs`: when a pop interrupts an active push hero flight between the same routes, the existing flight/tween is reused and reversed instead of creating a new pop flight.
  - hero registration now validates duplicate tags within the same route subtree and throws `InvalidOperationException` when multiple active heroes share one `tag` in `src/Flutter/Widgets/Hero.cs`.
  - hero build now validates nested hero composition and throws `InvalidOperationException` when a `Hero` is rendered under another `Hero` in `src/Flutter/Widgets/Hero.cs`.
  - added `HeroMode(enabled: ...)` in `src/Flutter/Widgets/Hero.cs`; disabled hero subtrees are excluded from registration/flight placeholder resolution, so matching tags no longer trigger hero flights when one side is wrapped in disabled `HeroMode`.
  - added `Hero.transitionOnUserGestures` in `src/Flutter/Widgets/Hero.cs` and wired navigator hero-session filtering in `src/Flutter/Widgets/Navigation.cs`; user-gesture pop transitions now animate heroes only when both matching heroes opt in.
  - added nested-navigator hero orchestration in `src/Flutter/Widgets/Hero.cs`; heroes from nested navigators now participate in ancestor navigator flights when they belong to the nested navigator's current route, matching Flutter's nested hero candidate rules.
- Added focused hero regression coverage:
  - new `src/Flutter.Tests/HeroNavigatorTests.cs` verifies shared-tag push/pop hero transitions keep both routes during flight and settle to a single destination route after completion;
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now also verifies destination `Hero.createRectTween` precedence and custom tween evaluation during flight;
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now verifies destination `Hero.flightShuttleBuilder` precedence (over source) and source-builder fallback when destination builder is absent;
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now verifies `Hero.placeholderBuilder` on push/pop flights for source/destination hidden heroes, including placeholder size metadata (`44x44`) from hero bounds;
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now verifies default hidden-hero placeholder semantics (push-source includes an offstage child placeholder; pop placeholders do not include offstage child placeholders).
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now verifies duplicate `Hero(tag)` detection in one route subtree (`InvalidOperationException`).
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now verifies nested-hero detection (`Hero` under `Hero`) throws `InvalidOperationException`.
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now verifies push-flight interruption by pop diverts the active hero flight (no new pop `createRectTween` invocation, active tween reverses in-place).
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now verifies disabled destination `HeroMode` prevents hero-flight startup on push.
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now verifies user-gesture pop hero gating (default disabled path skips flights, opt-in path animates flights when both heroes set `transitionOnUserGestures: true`).
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now verifies hero flights across outer-route push transitions when matching heroes live inside nested navigators.
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs` now verifies FAB composition is wrapped by `Hero` when `heroTag` is set.
- Expanded navigation nested back-dispatch regression coverage in `src/Flutter.Tests/NavigationTests.cs`:
  - verifies host back dispatch prefers innermost active navigator when nested stacks can pop,
  - verifies dispatch falls back to outer navigator when nested navigator cannot pop.
- Synced tracking docs for this parity pass:
  - `docs/FRAMEWORK_PLAN.md`,
  - `docs/ai/MODULE_INDEX.md`,
  - `docs/ai/TEST_MATRIX.md`,
  - `docs/ai/material-2026-04-12-fab-hero-transition-closeout.md`,
  - `docs/ai/material-2026-04-12-hero-create-rect-tween.md`,
  - `docs/ai/material-2026-04-13-hero-flight-shuttle-builder.md`,
  - `docs/ai/material-2026-04-13-hero-placeholder-builder.md`,
  - `docs/ai/material-2026-04-13-hero-default-placeholder-parity.md`,
  - `docs/ai/material-2026-04-13-hero-duplicate-tag-guard.md`,
  - `docs/ai/material-2026-04-13-hero-nested-guard.md`,
  - `docs/ai/material-2026-04-13-hero-flight-diversion.md`,
  - `docs/ai/material-2026-04-13-hero-mode-disable-parity.md`,
  - `docs/ai/material-2026-04-13-hero-parity-closeout.md`,
  - `docs/ai/navigation-2026-04-13-nested-back-dispatch.md`.
- Added framework semantics annotation plumbing for interactive controls:
  - introduced `Semantics` widget + `RenderSemanticsAnnotations` (`src/Flutter/Widgets/Semantics.cs`, `src/Flutter/Rendering/Proxy.RenderBox.cs`);
  - `MaterialButtonCore` now emits accessibility semantics (`label`, enabled/tap action, button/selected/checked flags) and `Checkbox`/`Switch`/`Radio` now wire toggle-state semantics (`IsChecked`) through shared control composition;
  - adaptive Cupertino checkbox path now propagates semantic label and toggle-state flags via framework semantics wrapper.
- Added focused regression coverage for control semantics labels/states:
  - `src/Flutter.Tests/MaterialCheckboxTests.cs` now asserts semantic-label propagation plus checked/enabled/tap semantics;
  - `src/Flutter.Tests/MaterialSwitchTests.cs` now asserts semantic-label propagation plus enabled/unchecked/tap semantics.
- Hardened Material checkbox/switch test isolation for global scheduler/focus state by placing `MaterialCheckboxTests` and `MaterialSwitchTests` in `SchedulerTestCollection`.
- Expanded framework Material drawer support in `src/Flutter.Material/Scaffold.cs`: `Scaffold` now supports both `drawer` and `endDrawer`, plus `ScaffoldState` APIs (`OpenDrawer/CloseDrawer` and `OpenEndDrawer/CloseEndDrawer`) with mutual-exclusion behavior.
- Added drawer gesture+motion baseline parity in `Scaffold`: edge swipe open (`drawerEdgeDragWidth`, `drawerEnableOpenDragGesture`, `endDrawerEnableOpenDragGesture`), horizontal drag-to-close for both start/end drawers, settle animation on open/close transitions, and velocity-aware drag-release settle (`fling`-style open/close decision) with scrim opacity tied to drawer progress.
- Added app-bar end-drawer implied action support in `src/Flutter.Material/Scaffold.cs`: when `Scaffold.endDrawer` exists and actions are absent, `AppBar` now auto-inserts trailing `IconButton(Icons.Menu)` (`automaticallyImplyActions` opt-out).
- Added drawer route-history handling baseline in `src/Flutter.Material/Scaffold.cs`: `ScaffoldState` now synchronizes a `LocalHistoryEntry` while drawer interaction is active so navigator back closes the active drawer before route pop.
- Updated navigator local-pop semantics in `src/Flutter/Widgets/Navigation.cs`: `NavigatorState.MaybePop` now treats route-level `WillPop` handling as consumed (handled) even on root routes, matching Flutter local-history behavior.
- Expanded app-bar dismiss-implied leading behavior in `src/Flutter.Material/Scaffold.cs`: non-drawer implied leading now resolves through `ModalRoute.ImpliesAppBarDismissal` (with `Navigator.CanPop` fallback), enabling root-route back affordance when local history is present.
- Expanded `src/Flutter.Tests/MaterialScaffoldTests.cs` with focused drawer coverage for end-drawer implied actions, `ScaffoldState` end-drawer transitions, start/end mutual exclusion, and start/end edge-drag open flows.
- Expanded test coverage with route-history/back handling regressions: `src/Flutter.Tests/MaterialScaffoldTests.cs` now verifies root-route drawer close on `Navigator.MaybePop`, and `src/Flutter.Tests/NavigationTests.cs` now verifies root-route local-history consume semantics.
- Completed framework `AppBar` fullscreen implied-leading branch: default implied leading now resolves to `IconButton(Icons.Close)` for fullscreen dialog routes (`PageRoute.FullscreenDialog == true`) and keeps `IconButton(Icons.ArrowBack)` for regular dismissible routes, with focused regression coverage in `src/Flutter.Tests/MaterialScaffoldTests.cs`.
- Aligned framework `AppBar` with Flutter implied-leading behavior: added `automaticallyImplyLeading` (`true` default) and default back leading resolution for non-root navigator routes (`Navigator.CanPop` -> `IconButton(Icons.ArrowBack)` -> `Navigator.MaybePop`), with focused regression coverage in `src/Flutter.Tests/MaterialScaffoldTests.cs`.
- Updated sample gallery demo shells in both C# and Dart samples to use title-only app bars so back affordance comes from default implied leading (`src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`).
- Documentation policy update: Dart-to-C# control/widget work now uses mandatory parity-first porting mode (`docs/ai/PORTING_MODE.md`) with strict `1:1` default behavior, required divergence logging, and explicit parity-validation workflow references in `AGENTS.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/INVARIANTS.md`, `docs/ai/MODULE_INDEX.md`, `docs/ai/FEATURE_TEMPLATE.md`, `docs/ai/TEST_MATRIX.md`, and `docs/ai/PARITY_MATRIX.md`.
- Agent workflow scope update: parity tasks now default to `one request = one control closed end-to-end` (not micro-iterations), with expanded context-budget guidance for control work (`12-20` initial files, up to `20`) and aligned rules in `AGENTS.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/PORTING_MODE.md`, `docs/ai/MODULE_INDEX.md`, and `docs/ai/FEATURE_TEMPLATE.md`.
