# Changelog

Newest first. One line per change (≤120 chars): what shipped, the Dart source file in parentheses for
ports, and a `Breaking:` prefix when a public API or default changed — even when the change moves
*toward* Flutter (`docs/ai/INVARIANTS.md` > Versioning). No member lists, no test inventory, no
rationale — the commit message and `git log -p` carry the detail. When a release is tagged, collapse
`[Unreleased]` into a few bullets under the version heading, keeping every `Breaking:` item by name.
Detailed per-change history before 2026-08-16 lives in git history (`git log`).

## [Unreleased] (after v0.2.0-alpha.1, 2026-08-13)
- Breaking: `RenderObject.Layout` reports a `PerformResize`/`PerformLayout` failure instead of throwing (`object.dart`).
- Breaking: `PipelineOwner` relayouts only dirty relayout boundaries; the descendant dirty channel is gone.
- Breaking: `MarkNeedsPaint`/`MarkNeedsCompositedLayerUpdate` are public and skip the parentless non-boundary root.
- Breaking: `DropChild` detaches and clears the child's parent data and relayout-boundary state (`object.dart`).
- Breaking: `Attach`/`Detach` recurse into children, and `RedepthChildren` walks them by default (`object.dart`).
- Breaking: `GetTransformTo` accepts any render object in the tree, resolving through the common ancestor.
- Added `LayoutWithoutResize`, `ScheduleInitialLayout`, `DebugResetSize` and `DebugAssertDoesMeetConstraints`.
- Added `RenderObject`'s mutation-during-layout diagnostics, `_reportException` and the `debugNeeds*` getters.
- Added `PaintingContext.RepaintCompositedChild`/`UpdateLayerProperties`/`AddLayer`/`AppendLayer`/`EstimatedBounds`.
- Added `BoxConstraints.DebugAssertIsValid`, `ContainerRenderObjectMixin.RemoveAll` and `DiagnosticsDebugCreator`.
- Added `PipelineOwner.RequestVisualUpdate` and the layout-callback dirty-node merge (`object.dart`).
- Breaking: `Semantics` pushes its properties as one batch, so callbacks land before the config is re-collected.
- Fixed `RenderAnimatedSize` re-dirtying itself from its own `PerformLayout` (`animated_size.dart`).
- Breaking: `SizedByParent`/`PerformResize` are overridable; `RenderBox` resizes from `ComputeDryLayout` (`box.dart`).
- Breaking: viewport, list wheel viewport, offstage, constrained overflow box and both sliders are sized by parent.
- Added the size/geometry setter phase checks and `MarkNeedsLayoutForSizedByParentChange` (`box.dart`, `sliver.dart`).
- Breaking: `Element.UpdateChild` is virtual; `ListWheelElement` overrides it (`list_wheel_scroll_view.dart`).
- Added `RenderOffstage`'s offstage intrinsics/baselines and `PaintsChild`, and the overflow box's dry baseline.
- Added CLR async-runtime elision to `FlutterError.DefaultStackFilter` (`foundation/assertions.dart`).
- Breaking: render objects release retained layers on unmount; layers use handle ownership (`object.dart`, `layer.dart`).
- Breaking: tap/force details use Dart-shaped diagnostic reference classes (`tap.dart`, `force_press.dart`).
- Breaking: `GestureDetector`/`RawGestureDetector` are a strict port; `RawGestureDetector` takes only `gestures:`.
- Added `GestureDetector`'s long-press matrix, force press, drag down/cancel and `supportedDevices`.
- Added `SemanticsGestureDelegate` and `RawGestureDetectorState.ReplaceGestureRecognizers` (`gesture_detector.dart`).
- Breaking: `ScrollableState.SetCanDrag` swaps a recognizer map through the detector, replacing `SetDragEnabled`.
- Added `ForcePressGestureRecognizer` with `ForcePressDetails` and pressure interpolation (`gestures/force_press.dart`).
- Added `PointerEvent.Pressure`/`PressureMin`/`PressureMax` and `Offset.DistanceSquared` (`gestures/events.dart`).
- Added `RenderSemanticsGestureHandler.Behavior` and `PipelineOwner.DebugDoingLayout` (`proxy_box.dart`, `object.dart`).
- Breaking: `State` is a `Diagnosticable`; `GlobalObjectKey.ToString` describes its value's identity (`framework.dart`).
- Breaking: `Listener` moved to `Widgets/Listener.cs` and `MultitouchDragStrategy` is only `Plumix.Gestures`'.
- Breaking: the long-press demo surface uses `GestureDetector` in both samples.
- Added `HitTestResult`'s transform stack (`PushTransform`/`PushOffset`/`PopTransform`, `Wrap`) (`hit_test.dart`).
- Breaking: `HitTestEntry.Transform` is filled during hit testing and `GestureBinding` dispatches `Transformed` events.
- Breaking: `BoxHitTestEntry.LocalPosition` is the hit position; `HitTestEntry.TransformEvent` is gone (`box.dart`).
- Added `BoxHitTestResult.AddWithOutOfBandPosition`; every `HitTestChildren` now pushes its offset (`box.dart`).
- Breaking: `ToggleButtons` hit tests through `MatrixUtils.ForceToPoint`, as Dart does (`toggle_buttons.dart`).
- Added `ScaleGestureRecognizer` with `Scale{Start,Update,End}Details` and pan/zoom aggregation (`gestures/scale.dart`).
- Added `GestureDetector` `OnScale*` and the trackpad scroll-to-scale options (`widgets/gesture_detector.dart`).
- Breaking: `OneSequenceGestureRecognizer.AddAllowedPointer` routes with the event's transform (`recognizer.dart`).
- Added a pinch/zoom/rotate section to the gesture recognizer demo page in both samples.
- Added `PointerPanZoom{Start,Update,End}Event` with pan/scale/rotation and local mapping (`gestures/events.dart`).
- Added `GestureRecognizer.AddPointerPanZoom`/`AddAllowedPointerPanZoom`/`IsPointerPanZoomAllowed` (`recognizer.dart`).
- Added the `DragGestureRecognizer` trackpad pan/zoom arms and pan-space velocity tracking (`gestures/monodrag.dart`).
- Added `Listener`/`RenderPointerListener` pan/zoom callbacks and the binding's routing arms (`gestures/binding.dart`).
- Breaking: `PointerDown`/`Move`/`Up`/`CancelEvent` reject `PointerDeviceKind.Trackpad`, as Dart does (`events.dart`).
- Added `TrackpadPanZoomSynthesizer`, which rebuilds the gesture phase Avalonia's trackpad events omit.
- Added a trackpad pan/zoom demo page to both samples.
- Breaking: `DragGestureRecognizer` is a strict `OneSequenceGestureRecognizer` port (`gestures/monodrag.dart`).
- Breaking: drags accept on accumulated global distance vs `computeHitSlop`/`computePanSlop`, not one 18 px slop.
- Added `MultitouchDragStrategy` with the latest-pointer, sum-all and average-boundary strategies (`monodrag.dart`).
- Breaking: `DragUpdateDetails.PrimaryDelta`/`DragEndDetails.PrimaryVelocity` are `double?`; a pan reports null.
- Breaking: `LongPressGestureRecognizer` is a strict `PrimaryPointerGestureRecognizer` port (`long_press.dart`).
- Added the secondary/tertiary long-press callback sets plus `LongPressDownDetails` and cancel (`long_press.dart`).
- Breaking: `GestureRecognizer.InvokeCallback` reports a throwing callback via `FlutterError` instead of rethrowing.
- Breaking: the gesture arena defers the last-member-standing win the way Dart's microtask does (`gestures/arena.dart`).
- Added `PointerRouter` per-route transforms, reentrancy rules and error reporting (`gestures/pointer_router.dart`).
- Added `PointerEvent.Transform`/`Transformed`/`Synthesized` and `GestureDebug` flags (`events.dart`, `debug.dart`).
- Added a drag/long-press recognizer demo page to both samples.
- Breaking: `SemanticsOwner` drains an incremental dirty set into a `SemanticsUpdate` (`semantics/semantics.dart`).
- Added semantics traversal grafting via `traversalParentIdentifier`/`traversalChildIdentifier` (`semantics/semantics.dart`).
- Added `PlumixHost.SemanticsUpdateProduced`, the per-frame changed-node feed for accessibility bridges.
- Breaking: `SemanticsActions.CustomAction` carries custom actions; `CustomSemanticsAction` gained the id registry.
- Breaking: custom-action dispatch walks merged descendants by action id (`semantics/semantics.dart`).
- Added `SemanticsOwner.GetSemanticsNode`/`PerformActionAt`/`GetRectOfSemanticsNode` and action listeners.
- Added `AccessibilityFocusBlockType` on `Semantics`, with subtree propagation and focus clearing (`object.dart`).
- Added `RenderTapRegionSurface` semantics tap/long-press handling and `GetRectOfSemanticsNodeInViewCoordinates`.
- Added `RenderObjectSemantics` diagnostics and `PipelineOwner.DebugDumpRenderObjectSemanticsTree` (`object.dart`).
- Closed build-time dropdown route disposal and overlay timing (`navigator.dart`, `overlay.dart`, `dropdown.dart`).
- Breaking: `WidgetsLocalizations` labels are abstract; the default is US-English LTR (`widgets/localizations.dart`).
- Added complete `ThemeData` diagnostics and compact debug strings (`theme_data.dart`).
- Breaking: flex checks and overflow labels follow Dart build modes (`rendering/flex.dart`, `widgets/basic.dart`).
- Breaking: `SliverGeometry` matches Dart defaults, visibility, hit testing and corrections (`rendering/sliver.dart`).
- Breaking: layout stops dirtying compositing; `RenderOpacity` uses alpha-driven boundaries (`rendering/object.dart`).
- Added `FlutterError`/`FlutterErrorDetails`/`ErrorSummary`/`ErrorHint`/`DiagnosticsStackTrace` (`foundation/assertions.dart`).
- Added `AssertionError`, `StackFrame`, `PartialStackFrame`, `RepetitiveStackFrameFilter` (`foundation/stack_frame.dart`).
- Added `Print.DebugPrint`/`DebugPrintThrottled`/`DebugPrintSynchronously`/`DebugWordWrap` (`foundation/print.dart`).
- Added `Constants.KDebugMode`/`KReleaseMode`/`KProfileMode`/`KIsWeb`/`KIsWasm` (`foundation/constants.dart`).
- Breaking: a flex/constraints-transform overflow reports through `FlutterError.ReportError` once per render object.
- Breaking: `RenderFlex` paints the overflow indicator only in debug builds, as Dart's `assert`-wrapped call does.
- Breaking: dropped `TextInput.OnError`, `ServicesDebug.OnError`, `KeyboardDebug.DebugPrint`/`OnError` for `FlutterError`.
- Breaking: `DiagnosticsBlock` is no longer sealed and `DiagnosticsNode.ToString(config, minLevel)` is virtual.
- Breaking: `RenderObject` is a `DiagnosticableTree`; the render tree is dumpable (`rendering/object.dart`).
- Added `debugFillProperties`/`debugDescribeChildren` across `rendering/`: render objects, layers, parent data.
- Added `RenderFlex`/`RenderConstraintsTransformBox` ` OVERFLOWING` headers and `SliverGeometry` diagnostics.
- Breaking: `RenderObject._needsCompositingBitsUpdate` starts `false`, as Dart's does, not `true`.
- Added `ColorProperty` (`painting/colors.dart`) and `TransformProperty` (`painting/matrix_utils.dart`).
- Added `Decoration`/`BoxDecoration`/`ShapeDecoration` and `InlineSpan`/`TextSpan`/`PlaceholderSpan` diagnostics.
- Added `RenderViewportBase.IndexOfFirstChild`/`LabelForChildAt` and `PipelineOwner.DebugDumpRenderTree`.
- Breaking: strict `RenderFlex` port; the marker drops `(reference) (approximate)` (`rendering/flex.dart`).
- Breaking: `RenderFlex` asserts Flutter's `textDirection` requirements; a `Row` needs an ambient `Directionality`.
- Breaking: `RenderFlex` throws Flutter's unbounded-flex error from layout and dry layout, not only dry layout.
- Breaking: `CrossAxisAlignment.Stretch` tightens the cross axis even under unbounded cross constraints.
- Added `RenderFlex.ClipBehavior`/`Flex.ClipBehavior`, `DescribeApproximatePaintClip` and the baseline overrides.
- Breaking: `MainAxisAlignment`/`CrossAxisAlignment` members follow Dart's declaration order.
- Breaking: `Flex` resolves the ambient `Directionality` only when Dart's `_needTextDirection` is true.
- Breaking: `Flex` throws when `CrossAxisAlignment.Baseline` is used without a `textBaseline`.
- `PaintingContext.PushClipRect` paints directly for `Clip.None` instead of pushing a no-op clip layer.
- Added `RenderBoxContainerDefaultsMixin.DefaultComputeDistanceToFirst/HighestActualBaseline` (`rendering/box.dart`).
- Breaking: strict `ThemeData` port; the marker drops `(reference) (approximate)` (`theme_data.dart`).
- Breaking: `visualDensity`/`materialTapTargetSize` default per platform — compact + shrinkWrap on desktop.
- Breaking: `VisualDensity.Comfortable` is `(-1, -1)`, not `(0, -1)`; densities outside [-4, 4] now throw.
- Breaking: `VisualDensity.Lerp` no longer clamps `t`; ctor args are lowercase (`horizontal:`/`vertical:`).
- Added `VisualDensity.MinimumDensity`/`MaximumDensity`/`AdaptivePlatformDensity`/`DefaultDensityForPlatform`.
- Breaking: `ThemeData.Brightness` is derived from `ColorScheme`, not stored; use `CopyWith(brightness:)` to change it.
- Breaking: dropped the 20 C#-only `ThemeData` colour mirrors and the five `*ButtonStyle` slots; read `ColorScheme`.
- Added `ThemeData.SecondaryHeaderColor`, `DialogBackgroundColor`, `IndicatorColor` and the `buttonTheme` default.
- Added `ThemeData.From`, `ThemeData.Fallback` and `ThemeData.CopyWith` (`theme_data.dart`).
- Breaking: component themes resolve eagerly, so an explicitly-default component theme now equals an implicit one.
- `ThemeData.Localize` uses Dart's size-5 FIFO cache keyed on input identity (`_FifoCache`).
- `ThemeData.Platform` honours `PlatformDefaults.DebugTargetPlatformOverride`; added `DebugDefaultTargetPlatform`.
- Breaking: strict `BottomNavigationBar` port over Dart's tile/label/bar/painter split (`bottom_navigation_bar.dart`).
- Breaking: `BottomNavigationBar` gains `fixedColor`, `mouseCursor`, `enableFeedback`, `landscapeLayout`,
  `useLegacyColorScheme`.
- Breaking: `BottomNavigationBarThemeData` gains feedback/landscape/cursor fields (`bottom_navigation_bar_theme.dart`).
- Added `MaterialConstants.BottomNavigationBarHeight` (`constants.dart`).
- Breaking: strict tap port — `BaseTap` over `PrimaryPointerGestureRecognizer`, deferred tap-down (`tap.dart`).
- Added `DoubleTap`/`MultiTap`/`SerialTap` recognizers (`multitap.dart`) and arena `Hold`/`Release` (`arena.dart`).
- Added `PointerSignalResolver` + inertia-cancel event; wheels resolve to the innermost scrollable (`scrollable.dart`).
- Breaking: tap callbacks take `TapDownDetails`/`TapUpDetails`; tertiary + double-tap trios (`gesture_detector.dart`).
- Fixed the C#-only click-to-focus adaptation so the deepest `Focus` under a press wins instead of the shallowest.
- Breaking: a drag starts as soon as it wins the arena; `OnlyAcceptDragOnThreshold` gates the slop (`monodrag.dart`).
- Breaking: `GestureDetector.Behavior` is nullable, defaulting to translucent without a child (`gesture_detector.dart`).
- Breaking: the bottom sheet drags through a threshold-gated `RawGestureDetector` (`bottom_sheet.dart`).
- Breaking: `ColoredBox` is opaque-hit-testable and adds `isAntiAlias`; zero-size paint matches Dart (`basic.dart`).
- Breaking: `Switch` drops the C#-only `semanticLabel`; label it with `Semantics`, as Dart does (`switch.dart`).
- Added ancestor-aware `RenderObject.SendSemanticsEvent`; merged toggle events use their owning node (`object.dart`).
- Breaking: strict `Switch` port — stateless `Switch` over `_MaterialSwitch` + `_SwitchPainter` (`switch.dart`).
- Breaking: `Switch.adaptive` no longer builds a `CupertinoSwitch`; it keeps the Material stack with Cupertino config.
- Breaking: M2/M3/Cupertino `_SwitchConfig` + defaults replace the C#-only `SwitchConfig` record and colour helpers.
- Removed `MaterialButtonCore`; the `Switch` was its last consumer (`toggleable.dart` drives the switch now).
- Added `Adaptation<T>` and `ThemeData.Adaptations`/`GetAdaptation<T>()` (`theme_data.dart`).
- Breaking: `ToggleablePainter` owns Dart's colour/state properties; the five painters stop redeclaring them.
- Breaking: `ToggleablePainter.PaintRadialReaction` follows Dart: dismissed-animation early-out, flag-driven radius.
- Added `AnimationStatus.IsDismissed()` (`animation.dart`).
- Breaking: `RawMaterialButton` builds Dart's `Semantics`/`_InputPadding`/`Material`/`InkWell` stack (`button.dart`).
- Breaking: `RawMaterialButton.shape` is a `ShapeBorder`, defaulting to `RoundedRectangleBorder()`, not a radius.
- Added `MaterialStateMixin` as an abstract `State` base, since C# has no mixins (`material_state_mixin.dart`).
- Breaking: `FloatingActionButton` composes `RawMaterialButton` and merges `iconSize` into its icon theme.
- Breaking: M3 `IconButton` builds `_SelectableIconButton` over `_IconButtonM3 : ButtonStyleButton` (`icon_button`).
- Breaking: `IconButton.styleFrom` drops the C#-only `splashColor`, which Dart's signature does not have.
- Breaking: `RawChip` builds `Material` + `InkWell` + `Ink` + `_ChipRedirectingHitDetectionWidget` (`chip.dart`).
- Breaking: a chip reports selection through `Semantics.selected`, never `checked`, as Dart does off the web.
- Breaking: `NavigationBar` puts its destinations in a `Material` over `_IndicatorInkWell` (`navigation_bar.dart`).
- Breaking: `NavigationDrawer`/`NavigationRail` destinations build `InkWell`/`InkResponse` (`navigation_rail.dart`).
- Breaking: a `NavigationDrawerDestination.backgroundColor` paints through `Ink`, not a `DecoratedBox`.
- Breaking: `ToggleButtons` builds Dart's `TextButton` per button (`toggle_buttons.dart`).
- Breaking: removed the C#-only `ButtonStyle.splashColor`; `InkResponse` derives the splash from `overlayColor`.
- Removed `RenderButtonTapTargetPadding`; the remaining consumer uses `_InputPadding` (`button_style_button.dart`).
- Added `Color.WithOpacity` in `Plumix.Painting`; `MaterialButtonCore`'s colour/padding/style helpers are gone.
- Breaking: strict `Hero` port — `HeroController` observer, `_HeroFlight` per tag, overlay entries (`heroes.dart`).
- Breaking: flights ride the route's own animation; the C#-only navigator flight engine and its controller are gone.
- Breaking: `HeroControllerScope` carries a `HeroController`; `Navigator` picks it up and wraps its subtree in `.None`.
- Breaking: `MaterialApp`/`CupertinoApp` install the controller through that scope; Material's arcs heroes (`app.dart`).
- Breaking: `Hero` gains Dart's `curve`/`reverseCurve`; its placeholder is `SizedBox` + `Offstage` + `TickerMode`.
- Breaking: duplicate hero tags now throw when a flight forms, not when the route subtree builds (`Hero._allHeroesFor`).
- Breaking: removed `TransitionRoute.Suspend`/`RestoreEntryOpacityForFlight` and the navigator's deferred-disposal set.
- Added `MaterialPointArcTween`/`MaterialRectArcTween`/`MaterialRectCenterArcTween` (`arc.dart`).
- Added `ReverseTween<T>`, `EdgeInsetsTween`, `ModalRoute.SubtreeContext`; `RectTween` unsealed with virtual endpoints.
- Breaking: strict `ButtonStyleButton` port — `Material` + `InkWell`, `_InputPadding` (`button_style_button.dart`).
- Breaking: the four Material buttons extend it, with Dart's constructors, `.icon`/`.tonal` factories and defaults.
- Breaking: they drop the C#-only `foregroundColor`/`backgroundColor`/`padding`/`borderRadius`/`min*` shortcuts.
- Breaking: `ButtonStyle` takes Dart's field order; `Padding` is now an `EdgeInsetsGeometry` (`button_style.dart`).
- Breaking: `ButtonStyle.Lerp` no longer clamps `t`; `Side` fades through Dart's `WidgetStateBorderSide.lerp`.
- Breaking: `styleFrom(shape:)` takes an `OutlinedBorder`; `styleFrom` matches Dart's overlay/elevation tables.
- Breaking: Elevated/Filled/Outlined `styleFrom(iconColor:)` resolves null when disabled, so the default wins.
- Breaking: a resolved `ButtonStyle.textStyle` reaches `Material` whole; the default's weight is not merged in.
- Breaking: disabling a pressed button adds `disabled` before clearing `pressed`, as `_ButtonStyleState` does.
- Added `ButtonStyle.CopyWith`/`DebugFillProperties` and the five `*ButtonThemeData.DebugFillProperties`.
- Split `Buttons.cs`/`ButtonThemes.cs` per Dart file; `MaterialButtonCore` is flagged C#-only infrastructure.
- Breaking: full 8,825-icon `Icons` catalog + `Icons.Adaptive` generated from the pin (`icons.dart`).
- Breaking: Material icons carry Dart's `FontFamily: "MaterialIcons"`; `Icons.MaterialIconsFontFamily` is gone.
- Breaking: corrected drifted hand-written code points (`Edit`, `Search`, `Visibility`, `ChevronLeft`, ...).
- `IconFontRegistry.Register` accepts a null package, matching Dart's unqualified `fontPackage` (`icon_data.dart`).
- Added `scripts/generate_material_icons.py`; it re-vendors the pinned font and checks glyph coverage.
- Breaking: `Slider` semantics moved onto its render object — value/increase/decrease/focus (`slider.dart`).
- Breaking: removed the C#-only `Slider.semanticLabel`; the semantics label now comes from `Slider.label`, as in Dart.
- Breaking: `RangeSlider` drops `focusNode`/`autofocus` for per-thumb focus nodes (`range_slider.dart`).
- Breaking: `RangeSlider` has no keyboard adjustment (Dart has none); Tab moves focus between thumbs, taps focus one.
- Breaking: `RangeSlider` emits one semantics node per thumb with per-thumb rects and RTL swap (`range_slider.dart`).
- Breaking: `RangeSlider` steps by `0.05` on macOS, not `Slider`'s `0.1` (`range_slider.dart`).
- Added `SemanticsConfiguration.IsSlider`/`IsEnabled`/`IsFocused`/`OnIncrease`/`OnDecrease`/`OnFocus`.
- Breaking: removed the C#-only `Widgets.Scrollbar` wrapper, `ScrollbarInteractionState` and the overlay render object.
- Breaking: strict `RawScrollbar` interaction port — `CustomPaint` + thumb/track recognizers (`scrollbar.dart`).
- Breaking: `RawScrollbar` drops the C#-only resolver/overlay API; subclasses override `UpdateScrollbarPainter`.
- Breaking: `ScrollbarPainter.GetTrackToScroll` maps a track *delta*; added `GetThumbScrollOffset`/`ThumbOffset`.
- Breaking: scrollbar track taps page by `0.8 * viewportDimension`; the thumb drags from pointer-down.
- Breaking: strict Material/Cupertino scrollbar states — `WidgetState` painter resolution, Dart haptics.
- Unsealed `TapGestureRecognizer` and the drag recognizers; `DragEndDetails` carries global/local positions.
- Breaking: strict `Scaffold` port — `Material` root, `restorationId`, dismissed-sheet stack (`scaffold.dart`).
- Breaking: `Scaffold.Of`/`MaybeOf` no longer register a dependency; `_ScaffoldScope` carries only `hasDrawer`.
- Breaking: `PersistentBottomSheetController` extends `ScaffoldFeatureController`; `StandardBottomSheet` is public.
- Breaking: strict `AppBar` port in its own `AppBar.cs` — toolbar container, title box, M2/M3 defaults (`app_bar.dart`).
- Breaking: `AppBar` drops the non-Dart `titleText`/`padding`; `bottom` is `IPreferredSizeWidget?`; `Bottom`/`SliverAppBar.Bottom` follow.
- Breaking: `Scaffold.body` is optional and `Scaffold.appBar` takes any `IPreferredSizeWidget` (`scaffold.dart`).
- Added `ScaffoldState.HasAppBar`/`AppBarMaxHeight`, `Scaffold.HasDrawerOf`, and `AppBar.PreferredAppBarSize`.
- Added `SystemUiOverlayStyle.StatusBarBrightness`, which the app bar derives from its background (`system_chrome.dart`).
- `CupertinoPicker.HandleChildTap` guards its post-animation continuation on `Mounted` (`picker.dart`).
- Breaking: ported `GlobalMaterialLocalizations` with all 119 locale bundles (`global_material_localizations.dart`).
- Breaking: `MaterialLocalizations` matches Dart's member set — 1-based `TabLabel`, `TimePickerHourLabel`.
- Added `DateFormat.Parse`/`ParseStrict` and `NumberFormat(pattern, locale)` to the intl subset (`date_format.dart`).
- Breaking: Material `Theme` installs a `MaterialBasedCupertinoThemeData`; added `ThemeData.CupertinoOverrideTheme`.
- Material `Theme.Of` falls back to `CupertinoBasedMaterialThemeData` under a bare `CupertinoTheme` (`theme.dart`).
- Ported `GlobalCupertinoLocalizations` with all 116 generated locale bundles (`global_cupertino_localizations.dart`).
- Ported `GlobalWidgetsLocalizations` and its locale bundles (`flutter_localizations/widgets_localizations.dart`).
- Added a pinned `package:intl` subset (`DateFormat`, `NumberFormat`, plural rules, CLDR data) under `Foundation/Intl`.
- Breaking: `WidgetsLocalizations` gained toolbar labels; unselected radio reads `Not selected` (`localizations.dart`).
- Breaking: re-ported `CupertinoTextSelectionToolbar` — chevron paging, fade, arrow clip (`text_selection_toolbar`).
- Breaking: strict Cupertino selection-toolbar buttons and desktop surface (`desktop_text_selection_toolbar.dart`).
- Added `Container.ClipBehavior`, `MediaQuery.DevicePixelRatioOf` and a shared core `ToolbarItemsParentData`.
- Breaking: strict `CupertinoMagnifier` port — elliptical rim, themed border, curved in/out (`magnifier.dart`).
- Magnifier lens and `BorderRadiusGeometry.Resolve` keep elliptical per-corner radii; added `BorderRadius.All`.
- Breaking: re-ported `CupertinoScrollbar` on `RawScrollbar`; track rect spans the padded viewport (`scrollbar.dart`).
- Breaking: re-ported `CupertinoSlider` strictly — dynamic colors, drag recognizer, track animation (`slider.dart`).
- Ported `CupertinoActionSheet`/`CupertinoActionSheetAction` with cancel button and slide-to-select (`dialog.dart`).
- Breaking: re-ported `CupertinoButton` strictly — size styles, tinted/filled, focus ring, tap-move slop (`button.dart`).
- Breaking: re-ported `CupertinoRadio` strictly — `RawRadio`-backed, painter-drawn, no `isDark`/`tapTargetSize` (`radio.dart`).
- Breaking: re-ported `CupertinoCheckbox` strictly — painter-drawn, `fillColor`, stateful side, no `isDark` (`checkbox.dart`).
- Breaking: `WidgetStateBorderSide`/`WidgetStateMouseCursor` moved to core with set-based `Resolve` (`widget_state.dart`).
- Breaking: re-ported `CupertinoActivityIndicator` strictly (no `isDark`) and added `CupertinoLinearActivityIndicator` (`activity_indicator.dart`).
- Ported `CupertinoNavigationBar`/`CupertinoSliverNavigationBar` with search, bottoms and hero transitions (`nav_bar.dart`).
- Breaking: `HeroFlightShuttleBuilder` now takes Flutter's `(flightContext, animation, direction, from, to)` (`heroes.dart`).
- Ported `CupertinoMenuAnchor` with menu items, dividers, swipe and long-press opening (`menu_anchor.dart`).
- Ported the `MultiDragGestureRecognizer` family with pending-delta and velocity semantics (`multidrag.dart`).
- Breaking: `RawMenuAnchor` forwards every close request to `onCloseRequested` (`raw_menu_anchor.dart`).
- Added `WidgetsBinding.AccessibilityFeatures` observer plumbing and `TickerFuture.WhenComplete`.
- Ported `CupertinoDatePicker` and `CupertinoTimerPicker` with bounded wheel correction (`date_picker.dart`).
- Ported `CupertinoContextMenu` and action rows with overlay flight and drag dismissal (`context_menu*.dart`).
- Ported `CupertinoSearchTextField` with collapse fading and accessibility icon scaling (`search_field.dart`).
- Ported `CupertinoTextFormFieldRow` with controller, validation and restoration lifecycle (`text_form_field_row.dart`).
- Breaking: ported `CupertinoTextField` and aligned editable cursor/length behavior (`text_field.dart`).
- Ported Cupertino sheet routes, drag/scroll handoff and painted system-overlay sampling (`sheet.dart`).
- Ported `CupertinoSegmentedControl` and exact rounded-superellipse paths (`segmented_control.dart`).
- Ported `CupertinoSlidingSegmentedControl` with gesture-team drag and spring thumb (`sliding_segmented_control.dart`).
- Ported `CupertinoSliverRefreshControl` with held sliver geometry and native states (`refresh.dart`).
- Ported `CupertinoPicker` and its selection overlay (`picker.dart`).
- Breaking: ported Cupertino switch/thumb painter; rewired `Switch.Adaptive` (`switch.dart`, `thumb_painter.dart`).
- Ported `CupertinoFormRow` and `CupertinoFormSection` (`form_row.dart`, `form_section.dart`).
- Ported `CupertinoListSection` with base/inset groups and rounded-superellipse clipping (`list_section.dart`).
- Breaking: ported `CupertinoTabScaffold` with restoration and retained per-tab focus (`tab_scaffold.dart`).
- Breaking: ported Cupertino mobile/desktop text-selection controls (`text_selection.dart`,
  `desktop_text_selection.dart`).
- Ported `CupertinoListTile` and `CupertinoExpansionTile` (`list_tile.dart`, `expansion_tile.dart`).
- Ported `CupertinoTabView` with independent named-route history and active-tab back handling (`tab_view.dart`).
- Fixed `Navigator` restorable named-route history across restart with null-safe page-group keys (`navigator.dart`).
- Breaking: ported `CupertinoTabBar`; moved `BottomNavigationBarItem` into core (`bottom_tab_bar.dart`).
- Ported `CupertinoFocusHalo` with descendant focus and all three outline shapes (`cupertino_focus_halo.dart`).
- Ported the generated `CupertinoIcons` catalog and bundled package font (`cupertino_ui/icons.dart`).
- Ported `CupertinoApp` and `CupertinoScrollBehavior` with navigator/router shell defaults (`app.dart`).
- Ported `CupertinoPageScaffold` with obstruction, inset, background and status-bar behavior (`page_scaffold.dart`).
- Ported Cupertino page routes, transitions, back gestures and modal popups (`cupertino_ui/route.dart`).
- Breaking: closed Cupertino localizations (`cupertino_ui/localizations.dart`) with picker formats and strict lookup.
- Breaking: ported core and Cupertino icon themes (`widgets/icon_theme_data.dart`, `widgets/icon_theme.dart`,
  `cupertino_ui/icon_theme_data.dart`); dynamic colours now resolve at each consumer.
- Breaking: closed the Cupertino theme foundation (`cupertino_ui/colors.dart`, `theme.dart`, `text_theme.dart`,
  `interface_level.dart`); `CupertinoColors` is the full table and `CupertinoThemeData` resolves dynamic colors.
- Docs: opened M6 Cupertino port (`docs/CUPERTINO_TODO.md`); retired per-iteration notes, plan archive and
  changelog rotation files into git history; added `docs/ai/BACKLOG.md`; `PORT_MAP.md` now lists qualified markers.
- Fixed the platform-dependent `FixedExtentScrollPhysics` fling test (pins iOS target platform, DPR 3).
- Breaking: ported `ScrollContext` (`widgets/scroll_context.dart`); `ScrollPosition`/`ScrollController` take Dart ctors.
- Breaking: ported the list wheel (`widgets/list_wheel_scroll_view.dart`, `rendering/list_wheel_viewport.dart`).
- Breaking: ported the rest of the text input service layer (`services/text_input.dart`, `text_editing_delta.dart`).
- Breaking: ported the diagnostics layer (`foundation/diagnostics.dart`); `Widget` now extends `DiagnosticableTree`.
- Breaking: ported the autofill subsystem (`services/autofill.dart`, `widgets/autofill.dart`); `EditableText` wired.
- Breaking: ported the platform-channel layer (`services/platform_channel.dart`, codecs, `SystemChannels`).
- Breaking: closed the Material 2 scheme derivation (`ColorSwatch`, `MaterialColor`, `Colors`, `FromSwatch`).
- Breaking: closed the `TextSelectionTheme` family; `Theme` wraps its subtree in `DefaultSelectionStyle`.
- Breaking: closed the `SnackBar` family (`SnackBar`/`SnackBarAction`/`SnackBarThemeData`, `ScaffoldMessenger` queue).
- Added `Curves.EaseInCirc`/`EaseInOutQuart` and a measurement-only `TextPainter`.
- Breaking: closed the legacy `DropdownButton` family (`DropdownButton`/`DropdownMenuItem`/`DropdownButtonFormField`).
- Added `kElevationToShadow`, `kMaterialListPadding`, `WidgetStateMouseCursor.Clickable`, `Scrollbar.thumbVisibility`.
- Breaking: closed the `CarouselView` family with real `RenderSliverFixedExtentCarousel`/`RenderSliverWeightedCarousel`.
- Added `RenderSliverFixedExtentBoxAdaptor` (`rendering/sliver_fixed_extent_list.dart`) with `SliverLayoutDimensions`.
- Breaking: closed the `DropdownMenu` family (`DropdownMenu`/`DropdownMenuFormField`) rebuilt on `MenuAnchor`.
- Added `TextInputFormatter` family (`services/text_formatter.dart`), `EditableText.CursorHeight`, expand/collapse.
- Breaking: closed the menus token/theme pass (`MenuAnchor`/`MenuBar`/`SubmenuButton`/`MenuItemButton`, `MenuStyle`).
- Breaking: closed the `InputDecorator` token/theme pass (`InputDecorationThemeData` class, `WidgetStateTextStyle`).
- Breaking: ported `widgets/scroll_metrics.dart` in full; `ScrollMetricsSnapshot` replaced by `IScrollMetrics`.
- Fixed `RenderViewport.GetOffsetToReveal` double-counting a descendant's paint offset when slivers nest.
- Breaking: ported `NestedScrollView` (`widgets/nested_scroll_view.dart`) with `IScrollActivityDelegate` primitives.
- Breaking: ported `rendering/viewport_offset.dart`, `rendering/viewport.dart`, `widgets/viewport.dart` in full.
- Breaking: ported `scheduler/ticker.dart` and `animation/animation_controller.dart` in full (`TickerFuture`).
- Breaking: replaced the 2D affine transform pipeline with `Matrix4` (`vector_math`, `painting/matrix_utils.dart`).
- Breaking: ported `painting/gradient.dart` and `painting/box_shadow.dart` in full; Avalonia shadow structs removed.
- Breaking: ported the keyboard identity stack (`keyboard_key.g.dart`, `hardware_keyboard.dart`, `raw_keyboard.dart`).
- Breaking: replaced the semantics compiler with Flutter's fragment model (`rendering/object.dart` semantics).
- Breaking: ported the scrollable semantics layer (`ScrollSemantics`, viewport semantics clip, scroll actions).
- Breaking: closed the `SelectableRegion` gesture/shortcut/overlay divergence (`gestures/tap_and_drag.dart`, shortcuts).
- Breaking: replaced the bespoke text-selection stack (`rendering/selection.dart`, `widgets/selectable_region.dart`).
- Breaking: closed the sliver persistent-header divergence (`rendering/sliver_persistent_header.dart`, `SliverAppBar`).
- Breaking: closed the show-on-screen / reveal protocol (`RevealedOffset`, `ShowOnScreen`, `Scrollable.EnsureVisible`).
- Breaking: closed the `Router` divergence (`widgets/router.dart`), incl. `WidgetsApp.Router`/`MaterialApp.Router`.
- Breaking: closed the `About`/`LicensePage` divergence (`material_ui/about.dart`) with the master-detail shell.
- Breaking: closed the remaining `Scaffold` slot divergence (`material_ui/scaffold.dart`): footer/statusBar/drawers.
- Breaking: closed the `PageView` divergence (`widgets/page_view.dart`); `PageController` is a `ScrollController`.
- Breaking: closed the live-`ScaffoldGeometry`/FAB-motion divergence (`_ScaffoldLayout`, FAB transition animator).
- Breaking: closed the `Navigator` divergence (`widgets/navigator.dart`): pages API, restoration, staged lifecycle.
- Breaking: ported the state-restoration subsystem (`services/restoration.dart`, `widgets/restoration.dart`).

## v0.2.0-alpha.1 — 2026-08-13
- Moved the Flutter parity pin to 3.47.0; Material/Cupertino source of truth is now `material_ui`/`cupertino_ui` 1.0.0.
- Strict ports: `Overlay`-based `Navigator`, rich-text span model, `painting` borders, `Table`/`RenderTable`,
  `DraggableScrollableSheet`, `Plumix.Physics` (simulations, `BouncingScrollPhysics`), `RawMenuAnchor`, `_MenuLayout`.
- Material closeouts: `Dialog` family (+ Cupertino `dialog.dart`), `SearchAnchor`/`SearchBar`, `TimePicker`,
  `InputDecorator`/`InputBorder`, `TabBar`, `MenuAnchor`/`MenuAcceleratorLabel`, `AppBar`, `Drawer`, `PopupMenu`,
  `Slider`/`RangeSlider`, `Autocomplete`, `FlexibleSpaceBar`, `ToggleButtons`, `Stepper`, chips, `ExpansionTile`,
  `BottomSheet`, `Tooltip`, `ButtonBar`, `SegmentedButton`, `DataTable`, date/range pickers, `ReorderableListView`,
  `MergeableMaterial`, action buttons; Cupertino text-selection toolbar.
- Core: nonlinear text scaling (`TextScaler`), `RadioGroup` traversal, intrinsic/dry-layout caching, `ScrollPhysics`
  gesture tuning, deferred-loading scroll, `RenderTable` semantics, `View.Of`.
- Breaking API changes: `Route`, `Overlay`, `MenuStyle.Alignment`, `ButtonStyle.Alignment`, `TabBar`/`TabController`,
  `SearchController`, `DialogThemeData`, `MaterialDialogs.ShowDialog`.

## v0.1.0-alpha.4 … v0.1.0-alpha.14 — 2026-07-05 … 2026-08-09
- Material: theme foundation (`ColorScheme`, `TextTheme`, `Typography`, interpolation), `WidgetsApp`/`MaterialApp`,
  and the bulk of the control library ported — buttons, chips, navigation (bar/rail/drawer/`BottomNavigationBar`),
  `AppBar`/`SliverAppBar`, dialogs, `SnackBar`, banners, menus, dropdowns, pickers, `TextField`, `DataTable`, sliders,
  progress indicators, `Stepper`, `ExpansionPanel`, `Badge`, `Scrollbar`, `RefreshIndicator`, `About`, ink/ripples.
- Cupertino: `CupertinoColors`/dynamic colors, adaptive routing for toolbars, checkbox/radio/switch/progress adaptives.
- Core widgets: implicit/explicit animation family, `Draggable`/`Dismissible`, `Overlay`/`OverlayPortal`, `Actions`/
  `Shortcuts`, `Form`/`FormField`, `Image` pipeline, magnifier, layout builders, `Visibility`, `Wrap`.
- Rendering: `Flow`, `RepaintBoundary`, clip/filter/shader widgets, `Stack` clipping, sliver family
  (`SliverFill*`, groups, resizing/floating headers, `SliverPrototypeExtentList`, `DecoratedSliver`).
- Text/input: `RenderEditable`, `TextSelectionControls`, selection toolbars, `SelectableText`/`SelectionArea`,
  `InputDecorator` state borders, `KeyboardListener`.
- Scroll: `ScrollConfiguration`/`PrimaryScrollController`, overscroll indicators, `ScrollNotificationObserver`,
  `PageStorage`, `ReorderableList`, `AnimatedList`/`AnimatedGrid`, scroll input policy.
- Navigation/animation: `TransitionRoute`/`PageRouteBuilder`, page transitions (Android/Apple), `PopScope`,
  `TickerMode`, `TweenAnimationBuilder`, `Future`/`Stream`/`ValueListenable` builders.
- Semantics/hosts/tooling: `IndexedSemantics`, `AnnotatedRegion`, `AppLifecycleListener` (cross-host lifecycle),
  hot reload (`HotReloadManager`), `PORT_PLAYBOOK`, machine-checked code style.
- F#: `Plumix.FSharp` DSL and `Plumix.Elmish` (MVU) packages added and wired into CI.

## v0.1.0-alpha.1..3 — 2026-04-26 and earlier (2026-03 .. 2026-04)
- Core framework: `Widget`/`Element`/`State`, `BuildOwner`, inherited widgets, `Scheduler`/`PipelineOwner`, layers.
- Rendering: `RenderBox`/`RenderFlex`/`RenderStack`, proxy boxes, decorations, `Container` composition, box helpers
  (`FittedBox`, `AspectRatio`, `OverflowBox`, `LimitedBox`, `Offstage`, `Align`, `Opacity`, `Transform`, `ClipRect`).
- Text/input: `Text`/`RenderParagraph`, `TextStyle`/`DefaultTextStyle`, `EditableText`, IME/clipboard, focus system.
- Gestures/scroll: gesture arena and recognizers, `Scrollable`/`Viewport`/slivers, `ListView`/`GridView`, `Scrollbar`.
- Navigation/semantics: `Navigator`/routes/observers, `Hero` transitions, semantics tree and host bridge.
- Material (M4 start): theming baseline, project split, `Scaffold`/`AppBar`/`Drawer`, button set, `IconButton`, `FAB`,
  `Checkbox`/`Switch`/`Radio` (+ Cupertino adaptives), `Icon`, `BottomNavigationBar`, `Card`, `ListTile`, `Tooltip`.
- Hosts/tooling: `SafeArea`/`MediaQuery`, system bars, desktop/browser/Android/iOS hosts, `dart_sample`, docs, CI, MIT.
