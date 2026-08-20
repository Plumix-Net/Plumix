# Changelog

Newest first. One line per change (≤120 chars): what shipped, the Dart source file in parentheses for
ports, and a `Breaking:` prefix when a public API or default changed — even when the change moves
*toward* Flutter (`docs/ai/INVARIANTS.md` > Versioning). No member lists, no test inventory, no
rationale — the commit message and `git log -p` carry the detail. When a release is tagged, collapse
`[Unreleased]` into a few bullets under the version heading, keeping every `Breaking:` item by name.
Detailed per-change history before 2026-08-16 lives in git history (`git log`).

## [Unreleased] (after v0.2.0-alpha.1, 2026-08-13)
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
