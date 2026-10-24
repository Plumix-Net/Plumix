# Changelog

Newest first. One line per change (≤120 chars): what shipped, the Dart source file in parentheses for
ports, and a `Breaking:` prefix when a public API or default changed — even when the change moves
*toward* Flutter (`docs/ai/INVARIANTS.md` > Versioning). No member lists, no test inventory, no
rationale — the commit message and `git log -p` carry the detail. When a release is tagged, collapse
`[Unreleased]` into a few bullets under the version heading, keeping every `Breaking:` item by name.
Detailed per-change history before 2026-08-16 lives in git history (`git log`).

## v0.4.0-beta.3 — 2026-10-24
- Android: the Activity's back callback steps aside while the soft keyboard is shown, so back hides it first (C#-only).

## v0.4.0-beta.2 — 2026-10-24
- Variable-font outlines fill with the non-zero rule, as Skia fills glyphs; overlaps were punched out (C#-only).
- Hosts answer `Clipboard.hasStrings` from the clipboard's formats; reading it showed iOS's paste alert (C#-only).
- Hosts take `viewInsets.bottom` from the view area the input pane covers; hidden iOS keyboards left it (C#-only).
- iOS Backspace runs the plugin's `deleteBackward`; hosts no longer type a key's control characters (C#-only).

## v0.4.0-beta.1 — 2026-10-24 (collapses v0.2.0-alpha.2 … v0.3.0-alpha.4, 2026-08-20 … 2026-10-24)
- Cupertino (M6): every `cupertino_ui/lib/src/` file is a strict port — theme/colours/text theme, localizations
  (116 locale bundles), routes and sheets, `CupertinoApp`, page/tab scaffolds, navigation bars, pickers, context
  menu, menu anchor, text field and selection, segmented controls, refresh control, list sections/tiles, icons.
- Hosts are pure engine embedders: retained dart:ui `Scene`s through `SceneBuilder`/`EngineLayer` and
  `FlutterView.Render`, `PlatformDispatcher` metrics/locales/accessibility/key data, physical `PointerDataPacket`s,
  `flutter/textinput`/`keyevent`/`platform`/`mousecursor` channels, the windowing API, `FrameTiming` reports.
- dart:ui surface: `Canvas`/`Picture`/`Paint` (mask filters, blend modes, isolated `saveLayer`), framework `Color`,
  native `Path` geometry and metrics, `Paragraph` over Avalonia with SkParagraph metrics plus a headless FlutterTest
  engine, `drawShadow`, gradients, animated image codecs, Skia-exact anti-aliasing for RRect/DRRect/path draws.
- Strict framework ports: `framework.dart` (elements, build scopes, keys, notifications), the rendering
  object/box/sliver/proxy/shifted/flex/viewport protocols, layers and the `PipelineOwner` tree, every binding, the
  focus tree and traversal, the scrolling stack (2D viewports, nested scroll), gestures (tap, drag, long-press,
  scale, force press, multi-drag, trackpad pan/zoom), mouse tracking, the semantics fragment model, text
  input/editing/selection, curves/tweens/implicit animations, `Hero`, `Navigator`/`Router`, state restoration.
- Material: strict re-ports of buttons and `ButtonStyle`, `Switch`, sliders, ink features, `Material`, `Scaffold`,
  `AppBar`, date/time pickers, `TextField`/`InputDecorator`, `ThemeData`, `BottomNavigationBar`, `Stepper`,
  `PaginatedDataTable`, list tiles; the 8,825-icon `Icons` catalog; `GlobalMaterialLocalizations` (119 locales).
- Platform bridges: accessibility (UI Automation, macOS, AT-SPI, Android, VoiceOver, browser ARIA), platform views
  (framework side), `PlatformMenuBar`, system context menu, `SystemChrome`, clipboard, Scribe/Live Text, ProcessText.
- Hosts: the `Plumix.Android` package (`PlumixMainActivity` over `AndroidActivityDelegate`), Android system back,
  Windows/Linux key messages; CI also runs the suite on the newest Avalonia of the major.
- Tooling: `FrameworkDartTester` with flutter_test's finders, matchers and `TestTextInput`; Flutter's test files
  ported per subsystem; timeline events, service extensions and the widget inspector; a `Profile` configuration;
  tests green in Debug/Profile/Release; opt-in screen capture of every sample route in both samples.
- Breaking (hosts): Avalonia 12.1.4 minimum; `PlumixHost` never lays out or paints, it rasterizes
  `FlutterView.RenderRequested` scenes (`PipelineOwner.CompositeFrame(DrawingContext)` gone); render-only hosts
  register views; one process-wide channel set across windows; Android `Run` sets `MainViewFactory`; the Android
  sample is `Plumix.Sample.Android`; back reaches `WidgetsApp.DidPopRoute` via `TopLevel.BackRequested`
  (`TryHandleBackButton` gone, `Key.Back` is no back button); hosts send `flutter/keyevent` (`HostRawKeyEventData`
  gone) and speak `flutter/textinput` through an instance `HostTextInputPlugin`; host clicks get distinct gesture IDs;
  hot reload runs `reassembleApplication` (host hooks gone); the framework thread, microtasks and async continuations
  live on `EventLoop`, vsync and clock on `FrameClock`.
- Breaking (painting): framework `Color`/`ColorSpace` replace Avalonia's `Color` in every API, with dart:ui's
  `lerp`/`alphaBlend`/`withOpacity` and `toString`; `ColorSwatch`/`MaterialColor`/`CupertinoDynamicColor`/
  `WidgetStateColor` derive from `Color`; `Matrix4` replaces the affine pipeline; `Canvas` is unsealed,
  `saveLayer(Rect?, Paint)` isolates, `drawImage*` take a `Paint`, clips push no edge mode; `TextStyle`,
  `TextPainter`, `StrutStyle`-aware `Paragraph`, `LineMetrics`/`TextBox`, `WordBoundary` (to `Plumix.Painting`),
  `TextOverflow.Visible`/vertical `Fade`, `TextHeightBehavior.LeadingDistribution` follow Dart
  (`TextLayoutFallback`, `Canvas.DrawTextLayout*` gone); headless text uses FlutterTest metrics and glyph boxes;
  `BoxDecoration.BorderRadius` is `BorderRadiusGeometry?` (`EffectiveBorderRadius` gone), decorations paint through
  Dart's painters with `IsComplex` as in Dart; `ShapeBorder.PaintInterior` takes a `Paint` and `Lerp` tries the
  reversed timeline; `paintImage`/`DecorationImage`/`lerp`, `TableBorder`, gradients, `BoxShadow` (Avalonia shadow
  structs gone), `Path`, rounded geometry, `Alignment`, `FittedSizes`, image streams, memory-allocation events and
  `PaintingBinding`'s cache are strict.
- Breaking (rendering): `RenderView` is a bare `RenderObject` with a DPR `TransformLayer` root; `RenderSliver` is a
  `RenderObject`; `SliverGeometry`, sliver parent data (`SliverLogicalContainerParentData`, nullable
  `SliverMultiBoxAdaptorParentData.Index`/`LayoutOffset`, `IKeepAliveParentData`), `IRenderSliverBoxChildManager`
  (`ChildCount`/`EstimatedChildCount`), fixed/varied extent lists (variable-extent types gone, `SetItemExtent`),
  `RenderSliverGrid` (RTL, `SliverGridGeometry` class), persistent headers (abstract extents, no `ChildBuilder`,
  `SliverFloatingHeader`/`SliverResizingHeader`/`PinnedHeaderSliver`), `RenderSliverOpacity` follow Dart;
  proxy_box is split and unsealed (`RenderProxyBox` concrete; `RenderOpacity`, `RenderLimitedBox`,
  `RenderAspectRatio`, `RenderConstrainedBox`, `RenderDecoratedBox.Decoration`, `RenderBackdropFilter`,
  `RenderTransform`, `RenderPhysicalModel`, clip hit tests and `Clip.AntiAlias` defaults, `RenderPointerListener`,
  `RenderIgnorePointer`/`RenderAbsorbPointer`, `RenderSemanticsGestureHandler`, `RenderAnnotatedRegion`);
  `RenderAlign`/`RenderFractionallySizedBox` become `RenderPositionedBox`/`RenderFractionallySizedOverflowBox` over
  `AlignmentGeometry` + `TextDirection`, `RenderPadding.Padding` is `EdgeInsetsGeometry`, `RenderBaseline`,
  `RenderAnimatedSize`, `RenderIndexedStack`, `RenderFlex` (Directionality, unbounded errors, enum order),
  `RenderEditable`, `RenderParagraph`, `RenderErrorBox` are strict; `RenderBox` hit tests are half-open;
  `PaintingContext` (a `ClipContext`, `Push*` return layers), `ClipRRectLayer`/`ClipPathLayer`, 8-bit `OpacityLayer`,
  `Layer.AddToScene`/`ContainerLayer.BuildScene`, nullable `ShaderMaskLayer` fields; clips/transforms are no repaint
  boundaries; `PipelineOwner` dirty queues, child owners and nullable `SemanticsOwner`; `MarkNeedsPaint`,
  `SizedByParent`/`PerformResize`, `DropChild`/`Attach`/`Detach`, `GetTransformTo`, layer release on unmount;
  `MouseCursor` is a class, `MouseCursorManager` per tracker, `SystemMouseCursors` complete, `MouseRegion` a
  render-object widget; hit tests carry transforms (`HitTestEntry.TransformEvent` gone).
- Breaking (framework): `BuildContext` is an interface `Element` implements; `ComponentElement`, abstract
  `ProxyElement`, `InheritedWidget` as a `ProxyWidget`, `...WidgetOfExactType` lookups, `PerformRebuild`, `Mounted`,
  `UnmountRoot`, `_InactiveElements`, `RootWidget`/`RootElement` (`IRootRenderObjectHost` gone),
  `View`/`RawView`/`ViewCollection`, `BuildScope` and Dart's build errors, public lifecycle and render-object hooks;
  `State` (`base.Dispose()`, `SetState` rules, `Context`, `Mounted`; `InvokeSetState` gone); keys (`ObjectKey`,
  `GlobalObjectKey`, `UniqueKey` by identity, `ValueKey` NaN), `IndexedSlot` equality; a throwing `build()` becomes
  `ErrorWidget`; `BuildOwner` owns its `FocusManager` (`ScheduleBuild`/`IsBuilding`/`RegisterElement` gone);
  `RunApp`/`RunWidget` keep one root.
- Breaking (widgets): `Directionality` is required (`Stack`/`IndexedStack` default to `TopStart`); `Text` (no flat
  style parameters, `Locale?`), `RichText.locale`, `DefaultTextStyle.Of`, `Icon`, `Container`, `DecoratedBox`,
  `Padding.Insets`, `SafeArea.minimum`, `ColoredBox`, `Positioned`, `IndexedStack`, `AnimatedCrossFade.ClipBehavior`;
  `MediaQuery` aspects, `PlatformBrightnessOf`, `WithClampedTextScaling`, `RemoveDisplayFeatures`, `fromView`
  accessibility; `SharedAppData`, `PageStorage`, `ChangeNotifier`, `Title`; localizations (`WidgetsLocalizations`
  abstract, async batches, `LocalizationsResolver`); `Semantics` takes one `SemanticsProperties` (`flags`/
  `mergeDescendants` gone); focus (`FocusNode` tree, `Unfocus(disposition)`, void `RequestFocus`, microtask changes,
  `FocusTraversalPolicy`, `ExcludeFocus`; `OnTextInput*`, `HandleText*`, `FocusScopeMarker` gone; no focus on
  pointer down); `WidgetsApp` shortcuts/actions, overlays, inspector, back handling; `Actions.Invoke`;
  `ContextMenuController.Show`; `Overlay`/`OverlayPortal`/`RenderOverlayTheater.SkipCount`; `Navigator` (async pops,
  `Task<bool>` route handlers, post-frame `NavigationNotification`); strict `Hero` (`HeroControllerScope`,
  `HeroFlightShuttleBuilder`, `CreateRectTween`, `TransitionRoute.Suspend` gone); `AutomaticKeepAlive`, `KeepAlive`;
  strict `scroll_delegate.dart` (`NullableIndexedWidgetBuilder`, nullable `itemCount`, abstract `ShouldRebuild`);
  `EditableText` (no `maxLength`/`semanticsLabel`, Actions keys, own `Scrollable`, async clipboard, cursor timer,
  toolbar, selection boxes, non-null `Composing`), `TextEditingValue`, `UndoHistory`, `TextSelectionOverlay`,
  selection gesture builder, `SelectableRegion`, `WidgetStateTextStyle`, `WidgetStateBorderSide`/
  `WidgetStateMouseCursor` and `BottomNavigationBarItem` moved to core, shortcut diagnostics.
- Breaking (scrolling): strict `Scrollable` (requires a `View`, `viewportBuilder`/`axisDirection`, scoped `Of`,
  `SetCanDrag`), `ScrollView`/`BoxScrollView` (`cacheExtent`/`scrollCacheExtent`), `ScrollPosition` split into
  `ScrollPositionWithSingleContext` (`PointerScroll`, activities on `AnimationController`, `PageStorage` keys),
  `ScrollContext`, `IScrollMetrics` (`ScrollMetricsSnapshot` gone), `NestedScrollView`, viewports, list wheel,
  `PageController`, show-on-screen, `EdgeDraggingAutoScroller`, `SingleChildScrollView`, `RawScrollbar` (C#-only
  `Widgets.Scrollbar` gone), glow on Android/Fuchsia.
- Breaking (gestures, keyboard): strict pointer events (trackpad kinds rejected), `GestureBinding` queueing and
  resampling, `SamplingClock` (`DartStopwatch`), arena, tap (`BaseTap`, `TapDownDetails`/`TapUpDetails`), drag
  (accumulated slop, nullable `PrimaryDelta`), long press, `GestureDetector`/`RawGestureDetector` (`gestures:` only,
  nullable `Behavior`), `InvokeCallback` reporting; `KeyEventManager`, `HardwareKeyboard.SyncKeyboardState`, the
  keyboard identity stack.
- Breaking (semantics, services): all 33 `SemanticsRole`s; `SemanticsFlags` is `long` (`IsInvalid`, `IsDialog`
  gone); `SemanticsData`, `SemanticsOwner` dirty set, custom actions, `Absorb`, empty-string text defaults;
  `AccessibilityFeatures` class on `SemanticsBinding`; `SemanticsService.SendAnnouncement`; `FormState.validate`
  announces; strict `SystemChrome`/`SystemUiOverlayStyle` (`SystemUiIconBrightness` gone); platform channels,
  restoration, autofill, text input layer; `FlutterError` replaces `TextInput.OnError`/`ServicesDebug.OnError`;
  diagnostics layer (`Widget`/`RenderObject`/`Element` are `DiagnosticableTree`s).
- Breaking (scheduler, animation): `SchedulerBinding.Instance`, `Priority` class, `lockEvents`, warm-up frame,
  `AddPostFrameCallback`, read-only `FramesEnabled`, `CurrentFrameTimeStamp`, `MidFrameMicrotasks`, reported frame
  errors; `Ticker`/`AnimationController`; `Curve` class hierarchy (`Curves.*` factories gone, `EaseOutCubic`);
  `Tween` (`Transform`, `Lerp(t)`, nullable `ColorTween`/`RectTween`/`SizeTween`, `CurveTween`, `TweenSequence`);
  `CurvedAnimation`; `Animation.ToString`; `AnimationStyle`; `ImplicitlyAnimatedWidget`-based `Animated*`.
- Breaking (Material): `WidgetState` replaces `[Flags] MaterialState`; `Material` (physical model,
  `MaterialInkController` interface, `MaterialEdges.KMaterialEdges`); ink as Dart's `InkFeature`s (`InkResponse`
  stateless, intent activation, stateful `Ink`, native `InkSparkle`); `ThemeData` (per-platform density,
  `VisualDensity`, derived `Brightness`, colour mirrors and `*ButtonStyle` slots gone, eager component themes);
  `Theme` (no `DefaultTextStyle`, `MaterialBasedCupertinoThemeData`); `ButtonStyleButton` and the four buttons
  (shortcuts gone, `ButtonStyle` field order/`Padding`/`Lerp`/`styleFrom`, `splashColor` gone), `IconButton`,
  `RawMaterialButton`, FAB, `ToggleButtons`, `ToggleablePainter`; `Switch` (`semanticLabel` and `SwitchConfig`
  gone); slider family (`SliderThemeData`, `RangeValues`/`RangeLabels`, semantics, keys); time and date pickers
  (`DatePickerThemeData`, `DateTimeRange<T>`, `SelectableDayPredicate`, C#-only calendar types gone); `TextField`,
  `InputDecoration`/`InputDecorationThemeData`, `DesktopTextSelectionToolbarButton`; `AppBar` (`titleText`/`padding`
  gone), `AppBarThemeData`; `Scaffold`/`ScaffoldMessenger`/`PersistentBottomSheetController`; list tiles, chips,
  navigation bar/drawer/rail, `BottomNavigationBar`, `Stepper`, `showDialog` via `showRawDialog`, `DialogTheme`,
  menus and `RawMenuAnchor`, dropdowns, `CarouselView`, `SnackBar`, `TextSelectionTheme`, M2 scheme derivation,
  `Icons` (code points, `MaterialIcons` family), `MaterialLocalizations`, `About`/`LicensePage`, `MaterialApp.Router`.
- Breaking (Cupertino): `CupertinoButton`, `CupertinoCheckbox`, `CupertinoRadio`, `CupertinoSwitch`,
  `CupertinoSlider`, `CupertinoActivityIndicator`, `CupertinoScrollbar`, `CupertinoMagnifier`, `CupertinoTextField`,
  `CupertinoTabScaffold`, `CupertinoTabBar`, `CupertinoMenuAnchor`/`CupertinoMenuItem`, `CupertinoActionSheet`,
  text-selection toolbar and controls re-ported strictly (no `isDark`); theme, colours, icon themes and localizations
  closed with dynamic colours resolved at each consumer.

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
