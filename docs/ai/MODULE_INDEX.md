# Module Index

Purpose: quickly select the smallest context that still lets the agent close one task end-to-end.

Related docs:

- `docs/ai/INVARIANTS.md`
- `docs/ai/PORTING_MODE.md`
- `docs/ai/TEST_MATRIX.md`
- `docs/ai/PARITY_MATRIX.md`
- `docs/ai/DIVERGENCES.md`
- `docs/ai/BACKLOG.md`
- `docs/CUPERTINO_TODO.md`

Current milestone/priority lives only in `docs/FRAMEWORK_PLAN.md` (see its `AI Semantic Snapshot` block); this file does not duplicate it.

## Quick Start

1. Follow the read order from `AGENTS.md` (Context Budget Protocol).
2. For Dart-to-C# ports, execute `docs/ai/PORT_PLAYBOOK.md`. `docs/ai/PORT_MAP.md` resolves the
   Flutter file for an already-ported control (and the tests/demos that go with it) without searching;
   sources over ~800 lines go through `docs/ai/DART_SPEC_PROTOCOL.md`.
3. Pick one subsystem below, open its `Read First` files, then expand along the control you are closing: `src/Plumix.Material/<Control>.cs` + `<Control>Theme.cs` + `src/Plumix.Tests/Material<Control>Tests.cs` + demo pages in both samples.
4. Enter unfamiliar subsystems through their tests, not through hotspot implementation files.

## Subsystems

### Material Layer

- Goal: Flutter-like Material theming/app-shell/control primitives in framework layers.
- Read First:
  - `src/Plumix.Material/ThemeData.cs`
  - `src/Plumix.Material/Theme.cs`
  - `src/Plumix.Material/ColorScheme.cs`
  - `src/Plumix.Material/Typography.cs`
  - `src/Plumix.Material/ElevationOverlay.cs`
  - `src/Plumix.Material/App.cs`
  - `src/Plumix.Material/PageTransitionsTheme.cs`
  - `src/Plumix.Material/AppBarTheme.cs`
  - `src/Plumix.Material/Scaffold.cs`
  - `src/Plumix.Material/DrawerController.cs`
  - `src/Plumix.Material/Buttons.cs`
  - `src/Plumix.Material/ButtonStyle.cs`
  - `src/Plumix.Material/MaterialLocalizations.cs`
- Then per target control: control file + its theme file + its `src/Plumix.Tests/Material*Tests.cs` + demo pages (`src/Sample/Plumix.Sample/Demos/Material/*`, `dart_sample/lib/demos/material/*`).
- Material color/typography work enters through `ColorScheme.cs`, `Typography.cs`, and `ThemeData.cs`, with focused
  coverage in `MaterialColorSchemeTests.cs`, `MaterialColorsTests.cs` and `MaterialThemeAnimationTests.cs`.
- The palette itself is `MaterialColor.cs` (swatch types) plus the generated `Colors.g.cs`
  (`scripts/generate_material_colors.py`), on top of core `src/Plumix/Painting/ColorSwatch.cs`.
  `Plumix.Material.Colors` shadows `Avalonia.Media.Colors`; projects importing both alias one of
  them (`src/Plumix.Tests/GlobalUsings.cs`).
- Badges enter through `Badge.cs` + `BadgeTheme.cs`; physical/logical alignment resolution uses core
  `Rendering/AlignmentGeometry.cs`, with focused coverage in `MaterialBadgeTests.cs`.
- Paired composition controls such as `GridTile` + `GridTileBar` share one focused test/demo surface when their Flutter implementations are directly coupled.
- Animated Material icons enter through `AnimatedIcon.cs` + generated `AnimatedIcons.Data.g.cs`; shared animation
  repaint ownership lives in core `Rendering/CustomPaint.cs`, with coverage in `MaterialAnimatedIconTests.cs`.
- AppBar action controls enter through `ActionButtons.cs` + `ActionIconTheme.cs`, with implied-leading integration covered in `MaterialScaffoldTests.cs`.
- App-bar and drawer shell control state enters through `AppBarTheme.cs` + `DrawerController.cs`; local theme
  precedence and standalone/Scaffold drawer choreography are covered in `MaterialScaffoldTests.cs`.
- Legacy Material buttons enter through `MaterialButton.cs` + `ButtonTheme.cs`; shared interaction/rendering remains in `Buttons.cs`, with focused coverage in `MaterialLegacyButtonTests.cs`.
- Drawer header controls enter through `DrawerHeader.cs`, with geometry and account-details behavior covered in `MaterialDrawerHeaderTests.cs`.
- Transient message controls enter through `SnackBar.cs` + `SnackBarTheme.cs` and `MaterialBanner.cs` +
  `MaterialBannerTheme.cs`; their independent queue/presentation lifecycles live in `ScaffoldMessenger.cs`, with
  Scaffold placement in `Scaffold.cs` and focused coverage in `MaterialSnackBarTests.cs`/`MaterialBannerTests.cs`.
- Bottom sheets enter through `BottomSheet.cs` + `BottomSheetTheme.cs`; persistent presentation integrates with `Scaffold.cs` (body scrim, FAB visibility, `DraggableScrollableActuator`/local history), modal presentation with `Widgets/Navigation.cs`, and focused coverage lives in `MaterialBottomSheetTests.cs`.
- Modal barriers are owned by `ModalRoute` in `Widgets/Navigation.cs` (`BuildModalBarrier` plus the barrier/label/curve members) and rendered by `Widgets/ModalBarrier.cs`; routes must not compose their own barrier in `BuildPage`. Coverage lives in `ModalRouteBarrierTests.cs` and `ModalBarrierTests.cs`.
- Sliver app bars enter through `SliverAppBar.cs` + `FlexibleSpaceBar.cs`; persistent-header layout lives in `Widgets/Scroll.cs` and `Rendering/Sliver.cs`, with focused coverage in `MaterialSliverAppBarTests.cs`.
- Material text inputs enter through `TextField.cs` + `TextFormField.cs` + `InputDecorator.cs` + `InputDecoratorTheme.cs`; form lifecycle lives in core `Widgets/Form.cs`, editing/IME behavior stays in `Widgets/TextInput.cs`, and focused coverage is split across `FormTests.cs` and `MaterialTextFieldTests.cs`.
- Material read-only selection enters through `SelectableText.cs` + `SelectionArea.cs` + `TextSelectionTheme.cs`;
  core default inheritance lives in `Widgets/DefaultSelectionStyle.cs` and `Widgets/InheritedTheme.cs`;
  shared registration, keyboard/copy flow, and multi-paragraph coordination live in core `Widgets/Selection.cs`,
  while glyph hit testing/highlight paint live in `RenderParagraph.cs`. Focused coverage is in
  `MaterialSelectionTests.cs`.
- Magnifiers enter through core `Widgets/Magnifier.cs` + `Rendering/Magnifier.cs`, Material `Magnifier.cs`, and
  Cupertino `CupertinoMagnifier.cs`; backdrop capture/composition lives in `Rendering/Layer.cs`, with focused
  coverage in `MagnifierTests.cs`.
- Tooltips enter through core `Widgets/RawTooltip.cs` and Material `Tooltip.cs` + `TooltipTheme.cs`; overlay geometry
  uses `Widgets/Overlay.cs` and `CustomSingleChildLayout.cs`, while hover/touch ownership uses `Widgets/Gestures.cs`,
  with focused coverage in `RawTooltipTests.cs` and `MaterialTooltipTests.cs`.
- Radio grouping enters through core `Widgets/RadioGroup.cs` + `RawRadio.cs`; shared geometry/bidi policy and nested
  group flattening live in `Widgets/FocusTraversal.cs`; Material `Radio.cs` and `RadioListTile.cs` consume the registry
  contract, with group, keyboard, animation, and semantics coverage in `RadioGroupRawRadioTests.cs` plus the existing
  Material radio suites; `src/Plumix.Cupertino/CupertinoRadio.cs` is the macOS-styled `RawRadio` builder that
  `Radio.Adaptive` composes on iOS/macOS, covered by `CupertinoRadioTests.cs`.
- Material text-selection toolbar controls enter through `TextSelectionToolbar.cs` and
  `DesktopTextSelectionToolbar.cs`; delegated viewport placement and size transitions live in core
  `Widgets/TextSelectionToolbarLayoutDelegate.cs`, `Widgets/DesktopTextSelectionToolbarLayoutDelegate.cs`,
  `Widgets/AnimatedSize.cs`, and their rendering counterparts, with focused coverage in
  `MaterialDesktopTextSelectionToolbarTests.cs` and `ImplicitAnimationsTests.cs`.
- Cupertino text-selection toolbar controls enter through `src/Plumix.Cupertino/CupertinoTextSelectionToolbar.cs`,
  `CupertinoDesktopTextSelectionToolbar.cs`, their button/adaptive/spell-check files, and `CupertinoTheme.cs`;
  Material adaptive routing consumes them on iOS/macOS, while the Android sample host registers the native default
  spell-check handler. Coverage shares `MaterialDesktopTextSelectionToolbarTests.cs`.
- Cupertino text and search fields enter through `src/Plumix.Cupertino/CupertinoTextField.cs` and
  `CupertinoSearchTextField.cs`; editing, restoration, formatter, cursor and input-service behavior stays in core
  `Widgets/TextInput.cs`, `UI/TextFormatter.cs` and `Rendering/Editable.cs`, with focused coverage in
  `CupertinoTextFieldTests.cs` and `CupertinoSearchTextFieldTests.cs`.
- Cupertino mobile/desktop selection controls enter through `CupertinoTextSelectionControls.cs` and
  `CupertinoDesktopTextSelectionControls.cs`; Material `TextField` and `SelectableText` choose their handle-only
  instances on iOS/macOS. Coverage lives in `CupertinoTextSelectionControlsTests.cs`.
- Material search routes enter through `SearchDelegate.cs` alongside `SearchAnchor.cs`; route ownership uses core `Widgets/Navigation.cs`, query editing stays on `Widgets/TextInput.cs`, and focused coverage lives in `MaterialSearchTests.cs`.
- Autocomplete enters through core `Widgets/Autocomplete.cs` and Material `Autocomplete.cs`; direct options presentation
  uses `Widgets/Overlay.cs` + `TapRegion.cs`, availability announcements use `UI/SemanticsService.cs`, and focused
  coverage lives in `MaterialAutocompleteTests.cs`.
- Dialog-family controls (`Dialog`, `AlertDialog`, `SimpleDialog`, `SimpleDialogOption`) enter through `Dialog.cs` +
  `DialogTheme.cs`; modal stacking/result behavior uses core `Widgets/ModalBarrier.cs` + `Widgets/Navigation.cs` and
  is covered by `ModalBarrierTests.cs` + `MaterialDialogTests.cs`.
- Popup-menu controls (`PopupMenuButton`, item/checked/divider entries) enter through `PopupMenu.cs` + `PopupMenuTheme.cs`; anchor geometry and route lifecycle also touch `Widgets/Navigation.cs`/`Widgets/Scroll.cs` and are covered by `MaterialPopupMenuTests.cs`.
- Legacy Material dropdown controls enter through `Dropdown.cs` + `DropdownButtonFormField.cs`; form lifecycle lives in core `Widgets/Form.cs`, selected-size behavior uses core `IndexedStack`, and positioned route/scroll/focus/form behavior is covered by `MaterialDropdownTests.cs`.
- Modern Material dropdown controls enter through `DropdownMenu.cs` + `DropdownMenuFormField.cs` +
  `DropdownMenuTheme.cs`; cascading controls and accelerators enter through `MenuAnchor.cs` +
  `MenuAccelerator.cs`, with Alt observation in core `UI/KeyboardEvents.cs` and focus-local dispatch through core
  `Widgets/Shortcuts.cs`; filtering/search/controller/form behavior is covered by `MaterialDropdownTests.cs`, and
  accelerator behavior by `MaterialMenuAcceleratorTests.cs`.
- Disclosure/progress controls enter through `ExpandIcon.cs` + `Stepper.cs`; integration with expansion controls and vertical/horizontal step behavior is covered by `MaterialStepperTests.cs`.
- About/license controls enter through `About.cs` plus core `Foundation/Licenses.cs` and `Widgets/Title.cs`;
  dialog/list/detail navigation, title-derived application names, and registry parsing are covered by
  `MaterialAboutTests.cs`.
- Bottom/action bar controls enter through `BottomAppBar.cs` + `BottomAppBarTheme.cs` and `ButtonBar.cs` + `ButtonBarTheme.cs`; standard FAB placement/animator contracts enter through `FloatingActionButtonLocation.cs` and `Scaffold.cs`, with focused coverage in `MaterialFloatingActionButtonLocationTests.cs`.
- Data-table controls enter through `DataTable.cs` + `DataTableTheme.cs` and `PaginatedDataTable.cs`; shared column negotiation lives in core `Widgets/Table.cs` + `Rendering/Table.cs`, with coverage in `MaterialDataTableTests.cs`.
- Row-wide data-table ink enters through `TableRowInkWell` in `InkWell.cs`; its source rectangle comes from
  core `RenderTable.GetRowBox`, with geometry/composition coverage in `MaterialDataTableTests.cs`.
- Material scrollbars enter through `src/Plumix.Material/Scrollbar.cs` + `ScrollbarTheme.cs`; the painter, the `RawScrollbarState` interaction API and its thumb/track recognizers live in core `Widgets/Scrollbar.cs`, with adaptive defaults in `src/Plumix.Cupertino/CupertinoScrollbar.cs` and focused coverage in `MaterialScrollbarTests.cs`.
- Reorderable lists enter through core `Widgets/ReorderableList.cs` and Material `ReorderableListView.cs`; inherited
  and explicit proxy bounds come from core `Widgets/DragBoundary.cs`. Gesture-arena drag ownership, keyed sliver
  items, gap animation, variable extents, boundary clamping, and callback normalization are covered by
  `MaterialReorderableListTests.cs`.
- Animated lists enter through core `Widgets/AnimatedList.cs`; logical/physical index translation, incoming/outgoing
  item animations, separated coordination, and keyed sliver remapping are covered by `AnimatedListTests.cs`.
- Animated grids enter through core `Widgets/AnimatedGrid.cs`; grid delegate layout, logical/physical index
  translation, incoming/outgoing animations, and keyed sliver remapping are covered by `AnimatedListTests.cs`.
- Material tabs enter through `src/Plumix.Material/Tabs.cs` + `TabController.cs` + `TabPageSelector.cs` + `TabBarTheme.cs`; page motion lives in core `Widgets/PageView.cs`/`Rendering/PageView.cs`, indicator layout/paint in `RenderTabBar.cs`, and focused coverage in `MaterialTabsTests.cs`.
- Application shells enter through core `Widgets/App.cs` + `Localizations.cs` and Material `App.cs`; theme motion
  and Material page routing use `Theme.cs` + `ThemeData.cs` + `PageTransitionsTheme.cs`, while route ownership and
  deep-link initial generation live in core `Widgets/Navigation.cs`. Route snapshots and predictive-back dispatch
  enter through core `Widgets/SnapshotWidget.cs`, `Rendering/SnapshotWidget.cs`, and `Widgets/AppLifecycleListener.cs`;
  Cupertino leading-edge drags share the route gesture lifecycle and settle through the route animation controller.
  Focused coverage is in `ApplicationWidgetsTests.cs`, `MaterialThemeAnimationTests.cs`, `NavigationTests.cs`, and
  `MaterialPageTransitionsTests.cs`.
- Material elevation color treatment enters through `ElevationOverlay.cs`; `Material.cs` consumes its M2/M3 policy,
  while component surface helpers share its tint interpolation. Focused coverage is in
  `MaterialElevationOverlayTests.cs`.
- Material ink reactions enter through `src/Plumix.Material/InkWell.cs`; pluggable `InkSplash`/`InkRipple`/
  `InkSparkle`/`NoSplash` features and factories live in `src/Plumix.Material/InkFeatures.cs`; source-required gesture
  callbacks live in core `Widgets/Gestures.cs`, with focused coverage in `MaterialInkResponseTests.cs`.
- Material date/time/range pickers enter through `src/Plumix.Material/CalendarDatePicker.cs` + `InputDatePickerFormField.cs` + `DatePickerDialog.cs` + `DateRangePickerDialog.cs` + `TimePickerDialog.cs` + `Date.cs`/`Time.cs` + their theme files; form lifecycle lives in core `Widgets/Form.cs`, dialog presentation uses `Dialog.cs`, localization hooks live in `MaterialLocalizations.cs`, and focused coverage lives in `MaterialDatePickerTests.cs`.
- Primary Tests:
  - `src/Plumix.Tests/MaterialScaffoldTests.cs`
  - `src/Plumix.Tests/MaterialButtonsTests.cs`
  - Control-specific `src/Plumix.Tests/Material<Control>Tests.cs`

### Cupertino Layer

- Goal: strict ports of `cupertino_ui/lib/src/*` into `src/Plumix.Cupertino` (depends only on `Plumix`;
  Material composes it for `.Adaptive` factories). Work list and per-file status: `docs/CUPERTINO_TODO.md`.
- Read First:
  - `src/Plumix.Cupertino/CupertinoApp.cs` (navigator/router shell, app theme/localizations/selection,
    `CupertinoScrollBehavior`)
  - `src/Plumix.Cupertino/CupertinoTheme.cs` (`CupertinoThemeData`, `CupertinoTheme`, `CupertinoDynamicColor`,
    `CupertinoColors`, `CupertinoUserInterfaceLevel` — a subset today; the foundation rows in the TODO tighten it)
  - `src/Plumix.Cupertino/CupertinoLocalizations.cs`
  - `src/Plumix.Cupertino/CupertinoDialog.cs`, `CupertinoDialogRoute.cs` (dialog family + `showCupertinoDialog`)
  - `src/Plumix.Cupertino/CupertinoRoute.cs` (page routes/descriptions, page/fullscreen transitions,
    leading-edge back gestures, modal popup route + `ShowCupertinoModalPopup`)
  - `src/Plumix.Cupertino/CupertinoTextSelectionToolbar.cs` + `CupertinoTextSelectionToolbarButton.cs`
    (+ desktop/adaptive/spell-check variants) — the text-selection toolbar family
  - `src/Plumix.Material/PageTransitionsTheme.cs` (the compatibility `CupertinoPageTransitionsBuilder` adapter
    delegates to the Cupertino-owned implementation)
- Then per target control: `src/Plumix.Cupertino/Cupertino<Control>.cs` + `src/Plumix.Tests/Cupertino<Control>Tests.cs`
  + demo pages (`src/Sample/Plumix.Sample/Demos/Cupertino/*`, `dart_sample/lib/demos/cupertino/*`).
- Existing controls (`CupertinoScrollbar`, `CupertinoMagnifier`, the selection toolbars) were
  written to serve Material adaptive controls; several carry `(reference)`/`(adapted)` markers — diff against
  Dart before extending them.
- Cupertino tab bars enter through `src/Plumix.Cupertino/CupertinoTabBar.cs`; the shared
  `BottomNavigationBarItem` lives in core `Widgets`, with focused coverage in `CupertinoTabBarTests.cs`.
- Cupertino tab scaffolds enter through `src/Plumix.Cupertino/CupertinoTabScaffold.cs`; controller/restoration,
  lazy offstage tab caching, per-tab focus, and inset behavior are covered in `CupertinoTabScaffoldTests.cs`.
- Cupertino sheets enter through `src/Plumix.Cupertino/CupertinoSheet.cs`; route delegation, nested navigation,
  drag/scroll handoff and system-overlay sampling are covered in `CupertinoSheetTests.cs` and
  `AnnotatedRegionTests.cs`.
- Per-tab navigation enters through `src/Plumix.Cupertino/CupertinoTabView.cs`; it owns a core `Navigator`,
  Cupertino page-route generation, Hero observation and active-tab back handling, with focused coverage in
  `CupertinoTabViewTests.cs`.
- Cupertino list rows and disclosure enter through `CupertinoListTile.cs` + `CupertinoExpansionTile.cs`; expansion
  uses core `Expansible` and `OverlayPortal`, with focused coverage in the matching Cupertino test files.
- Cupertino list sections enter through `CupertinoListSection.cs`; rounded-superellipse clipping uses core
  `Widgets/Clip.cs`, with focused coverage in `CupertinoListSectionTests.cs`.
- Cupertino form rows and sections enter through `CupertinoFormRow.cs`, `CupertinoTextFormFieldRow.cs` and
  `CupertinoFormSection.cs`; the text row owns controller/form synchronization, while the section delegates
  base/inset decoration and divider layout to `CupertinoListSection.cs`, with focused coverage in the matching
  Cupertino form test files.
- Cupertino wheel pickers enter through `src/Plumix.Cupertino/CupertinoPicker.cs`; scrolling and layout live in
  core `Widgets/ListWheelScrollView.cs`, with focused coverage in `CupertinoPickerTests.cs`.
- Cupertino date and duration wheels enter through `src/Plumix.Cupertino/CupertinoDatePicker.cs`; localized order,
  bounds/predicate correction and timer labels compose `CupertinoPicker`, with focused coverage in
  `CupertinoDatePickerTests.cs`.
- Cupertino pull-to-refresh enters through `src/Plumix.Cupertino/CupertinoRefresh.cs`; overscroll and held-extent
  layout stay in its sliver render object, with focused coverage in `CupertinoRefreshTests.cs`.
- Cupertino segmented controls enter through `src/Plumix.Cupertino/CupertinoSegmentedControl.cs`; equalized layout,
  exact rounded-superellipse paint and rectangular hit testing stay in its render object, with focused coverage in
  `CupertinoSegmentedControlTests.cs`.
- Focus halos enter through `src/Plumix.Cupertino/CupertinoFocusHalo.cs`; descendant focus ownership lives in
  core `Widgets/Focus.cs`, with shape/color primitives in `Rendering/RoundedSuperellipseBorder.cs` and
  `Painting/HSLColor.cs`. Coverage lives in `CupertinoFocusHaloTests.cs` and `FocusTests.cs`.
- Primary Tests:
  - `src/Plumix.Tests/CupertinoAppTests.cs`
  - `src/Plumix.Tests/CupertinoLocalizationsTests.cs`
  - `src/Plumix.Tests/CupertinoDialogTests.cs`
  - `src/Plumix.Tests/CupertinoRouteTests.cs`
  - `src/Plumix.Tests/CupertinoFocusHaloTests.cs`
  - `src/Plumix.Tests/MaterialDesktopTextSelectionToolbarTests.cs`, `MaterialSelectionTests.cs` (toolbar family)
  - Adaptive coverage inside `MaterialCheckboxTests.cs`, `MaterialRadioTests.cs`, `MaterialSwitchTests.cs`,
    `MaterialSliderTests.cs`, `MaterialCircularProgressIndicatorTests.cs`

### Runtime and Host

- Goal: frame scheduling, pipeline wiring, Avalonia host integration.
- Read First:
  - `src/Plumix/FlutterHost.cs`
  - `src/Plumix/AndroidLifecycleChannel.cs`
  - `src/Plumix/WidgetHost.cs`
  - `src/Plumix/Widgets/AppLifecycleListener.cs`
  - `src/Plumix/UI/AppLifecycle.cs`
  - `src/Plumix/Widgets/StatusTransitionWidget.cs`
  - `src/Plumix/Widgets/DisposableBuildContext.cs`
  - `src/Plumix/Widgets/View.cs`
  - `src/Plumix/UI/SystemChrome.cs`
  - `src/Plumix/Scheduler.cs`
  - `src/Plumix/PipelineOwner.cs`
  - `src/Plumix/RenderView.cs`
- Primary Tests:
  - `src/Plumix.Tests/FramePipelineTests.cs`
  - `src/Plumix.Tests/RenderingParityTests.cs`
  - `src/Plumix.Tests/AppLifecycleListenerTests.cs`

### Animation and Ticking

- Goal: Flutter-shaped frame callbacks, ticker lifecycle and simulation-driven animation.
- Read First:
  - `src/Plumix/Ticker.cs` (`Ticker`, `TickerFuture`, `TickerCanceled`, `ITickerProvider`)
  - `src/Plumix/AnimationController.cs`
  - `src/Plumix/Scheduler.cs`
  - `src/Plumix/Widgets/TickerProvider.cs`
  - `src/Plumix/Physics/Simulation.cs`
- A ticker callback receives the time elapsed since the ticker started, so the first frame after a start
  reports zero; anything needing a per-frame delta keeps its own previous elapsed value.
- Primary Tests:
  - `src/Plumix.Tests/AnimationControllerTickerTests.cs`
  - `src/Plumix.Tests/TickerProviderTickerModeTests.cs`

### Widget/Element Lifecycle

- Goal: reconciliation, state retention/disposal, dependency propagation.
- Read First:
  - `src/Plumix/Widgets/Framework.Widget.cs`
  - `src/Plumix/Widgets/Framework.Element.cs`
  - `src/Plumix/Widgets/Framework.BuildOwner.cs`
  - `src/Plumix/Widgets/Framework.RenderObject.cs`
  - `src/Plumix/Widgets/StatefulBuilder.cs`
  - `src/Plumix/Widgets/LookupBoundary.cs`
  - `src/Plumix/Widgets/InheritedTheme.cs`
  - `src/Plumix/Widgets/DefaultSelectionStyle.cs`
  - `src/Plumix/Widgets/Title.cs`
  - `src/Plumix/Foundation/Key.cs`
  - `src/Plumix/Foundation/Diagnosticable.cs` (diagnostics layer entry point; nodes/properties/text
    tree live in `Diagnostics.Node.cs`, `DiagnosticProperties.cs`, `Diagnostics.TextTree.cs`)
- Primary Tests:
  - `src/Plumix.Tests/ElementLifecycleTests.cs`
  - `src/Plumix.Tests/InheritedWidgetTests.cs`
  - `src/Plumix.Tests/InheritedModelTests.cs`
  - `src/Plumix.Tests/InheritedNotifierTests.cs`
  - `src/Plumix.Tests/StatefulBuilderLookupBoundaryTests.cs`
  - `src/Plumix.Tests/TitleDefaultSelectionStyleTests.cs`
  - `src/Plumix.Tests/DiagnosticsTests.cs`

### Core Layout/Paint/Compositing

- Goal: box constraints, relayout boundaries, repaint boundaries, layers.
- Read First:
  - `src/Plumix/Rendering/Object.RenderObject.cs`
  - `src/Plumix/Rendering/Box.RenderBox.cs`
  - `src/Plumix/Rendering/Proxy.RenderBox.cs`
  - `src/Plumix/Widgets/Basic.cs`
  - `src/Plumix/Widgets/ConstraintsTransformBox.cs`
  - `src/Plumix/Rendering/DebugOverflowIndicator.cs`
  - `src/Plumix/Rendering/Baseline.cs`
  - `src/Plumix/Widgets/Baseline.cs`
  - `src/Plumix/Widgets/Intrinsic.cs`
  - `src/Plumix/Widgets/LayoutBuilder.cs`
  - `src/Plumix/Widgets/OrientationBuilder.cs`
  - `src/Plumix/Widgets/SliverLayoutBuilder.cs`
  - `src/Plumix/Widgets/SafeArea.cs`
  - `src/Plumix/Widgets/SliverFill.cs`
  - `src/Plumix/Rendering/SliverFill.cs`
  - `src/Plumix/Widgets/Placeholder.cs`
  - `src/Plumix/Widgets/GridPaper.cs`
  - `src/Plumix/Widgets/Clip.cs`
  - `src/Plumix/Widgets/PhysicalModel.cs`
  - `src/Plumix/Rendering/CustomClip.cs`
  - `src/Plumix/Rendering/PhysicalModel.cs`
  - `src/Plumix/Widgets/Flow.cs`
  - `src/Plumix/Widgets/RepaintBoundary.cs`
  - `src/Plumix/Rendering/Flow.cs`
  - `src/Plumix/Rendering/RepaintBoundary.cs`
  - `src/Plumix/Widgets/CompositedTransform.cs`
  - `src/Plumix/Rendering/CompositedTransform.cs`
  - `src/Plumix/Widgets/AnnotatedRegion.cs`
  - `src/Plumix/Rendering/AnnotatedRegion.cs`
  - `src/Plumix/Rendering/Layer.cs`
  - `src/Plumix/Widgets/CustomMultiChildLayout.cs`
  - `src/Plumix/Rendering/CustomMultiChildLayout.cs`
  - `src/Plumix/Widgets/Table.cs`
  - `src/Plumix/Rendering/Table.cs`
  - `src/Plumix/Widgets/NavigationToolbar.cs`
  - `src/Plumix/Widgets/Dismissible.cs`
  - `src/Plumix/Widgets/SizeChangedLayoutNotifier.cs`
  - `src/Plumix/Rendering/SizeChangedLayoutNotifier.cs`
  - `src/Plumix/UI/Path.cs`
  - `src/Plumix/Rendering/Layer.cs`
  - `src/Plumix/Rendering/Object.PaintingContext.cs`
- Primary Tests:
  - `src/Plumix.Tests/BasicWidgetProxyTests.cs`
  - `src/Plumix.Tests/IntrinsicQueryParityTests.cs`
  - `src/Plumix.Tests/RenderingParityTests.cs`
  - `src/Plumix.Tests/UnconstrainedLimitedBoxTests.cs`
  - `src/Plumix.Tests/BaselineTests.cs`
  - `src/Plumix.Tests/IntrinsicWidgetsTests.cs`
  - `src/Plumix.Tests/LayoutBuilderTests.cs`
  - `src/Plumix.Tests/SliverFillTests.cs`
  - `src/Plumix.Tests/DebugPaintingWidgetsTests.cs`
  - `src/Plumix.Tests/ClipWidgetsTests.cs`
  - `src/Plumix.Tests/FlowRepaintBoundaryTests.cs`
  - `src/Plumix.Tests/CompositedTransformTests.cs`
  - `src/Plumix.Tests/AnnotatedRegionTests.cs`
  - `src/Plumix.Tests/CustomMultiChildLayoutTests.cs`
  - `src/Plumix.Tests/DirectionalPositionedTableCellTests.cs`
  - `src/Plumix.Tests/DismissibleSizeChangedLayoutTests.cs`
  - `src/Plumix.Tests/CompositingLayerTests.cs`
  - `src/Plumix.Tests/LayerV2Tests.cs`

### Core Animation and Transitions

- Goal: Flutter-shaped animation values, controller status, transition widgets, and implicit replacement motion.
- Read First:
  - `src/Plumix/Animation.cs`
  - `src/Plumix/AnimationController.cs`
  - `src/Plumix/Widgets/TickerProvider.cs`
  - `src/Plumix/Widgets/Transitions.cs`
  - `src/Plumix/Rendering/Decoration.cs`
  - `src/Plumix/Widgets/Basic.cs`
  - `src/Plumix/Widgets/AnimatedSwitcher.cs`
  - `src/Plumix/Widgets/AnimatedSize.cs`
  - `src/Plumix/Widgets/ImplicitAnimations.cs`
  - `src/Plumix/Widgets/ValueListenableBuilder.cs`
  - `src/Plumix/Widgets/TweenAnimationBuilder.cs`
  - `src/Plumix/Widgets/DualTransitionBuilder.cs`
  - `src/Plumix/Widgets/RepeatingAnimationBuilder.cs`
  - `src/Plumix/Widgets/Async.cs`
- Primary Tests:
  - `src/Plumix.Tests/BuilderWidgetsTests.cs`
  - `src/Plumix.Tests/AsyncBuilderTests.cs`
  - `src/Plumix.Tests/AnimatedSwitcherTests.cs`
  - `src/Plumix.Tests/ImplicitAnimationsTests.cs`
  - `src/Plumix.Tests/TickerProviderTickerModeTests.cs`
  - `src/Plumix.Tests/TransitionsTests.cs`
  - `src/Plumix.Tests/SliverOpacityTests.cs`

### Images and Decoration Paint

- Goal: Flutter-like image resolution, caching, stream lifetime, and decoration paint geometry.
- Read First:
  - `src/Plumix/Rendering/ImageProvider.cs`
  - `src/Plumix/Rendering/ImageStream.cs`
  - `src/Plumix/Rendering/ImageCache.cs`
  - `src/Plumix/Rendering/DecorationImage.cs`
  - `src/Plumix/Rendering/Image.cs`
  - `src/Plumix/Widgets/Image.cs`
  - `src/Plumix/Widgets/FadeInImage.cs`
  - `src/Plumix/Widgets/ImageIcon.cs`
  - `src/Plumix/Widgets/ColorFiltered.cs`
  - `src/Plumix/Widgets/ImageFiltered.cs`
  - `src/Plumix/Widgets/ShaderMask.cs`
  - `src/Plumix/Widgets/BackdropFilter.cs`
  - `src/Plumix/Rendering/ImageFilter.cs`
  - `src/Plumix/Rendering/ImageFilterConfig.cs`
  - `src/Plumix/Rendering/Filter.RenderBox.cs`
  - `src/Plumix/Rendering/FilterLayerRasterizer.cs`
  - `src/Plumix/Rendering/Proxy.RenderBox.cs`
- Primary Tests:
  - `src/Plumix.Tests/ImageProviderDecorationTests.cs`
  - `src/Plumix.Tests/ImageWidgetTests.cs`
  - `src/Plumix.Tests/FilterWidgetsTests.cs`

### Gestures and Input

- Goal: pointer dispatch, hit testing, arena resolution, recognizer callbacks.
- Read First:
  - `src/Plumix/UI/PointerEvents.cs`
  - `src/Plumix/Gestures/GestureBinding.cs`
  - `src/Plumix/Gestures/GestureArena.cs`
  - `src/Plumix/Widgets/Gestures.cs`
  - `src/Plumix/Widgets/DragTarget.cs`
  - `src/Plumix/Widgets/DragBoundary.cs`
  - `src/Plumix/Widgets/Overlay.cs`
  - `src/Plumix/Rendering/Overlay.cs`
  - `src/Plumix/Widgets/TapRegion.cs`
  - `src/Plumix/Rendering/TapRegion.cs`
  - `src/Plumix/Widgets/MetaData.cs`
  - `src/Plumix/Rendering/Object.HitTest.cs`
- Primary Tests:
  - `src/Plumix.Tests/GesturePipelineTests.cs`
  - `src/Plumix.Tests/DragTargetTests.cs`
  - `src/Plumix.Tests/TapRegionTests.cs`

### Focus, Keyboard, Actions, and Shortcuts

- Goal: focus ownership/traversal, host key dispatch, focused keyboard listeners, intent/action routing, shortcuts,
  and focus semantics.
- Read First:
  - `src/Plumix/Widgets/Focus.cs`
  - `src/Plumix/Widgets/KeyboardListener.cs`
  - `src/Plumix/Widgets/Actions.cs`
  - `src/Plumix/Widgets/Shortcuts.cs`
  - `src/Plumix/UI/KeyboardEvents.cs`
- Primary Tests:
  - `src/Plumix.Tests/FocusTests.cs`
  - `src/Plumix.Tests/KeyboardListenerTests.cs`
  - `src/Plumix.Tests/ActionsShortcutsTests.cs`
  - `src/Plumix.Tests/FocusableActionDetectorTests.cs`
  - `src/Plumix.Tests/FlutterHostInputTests.cs`

### Navigation

- Goal: route stack operations, named routes, declarative pages, observers, back handling, hero flights.
- Read First:
  - `src/Plumix/Widgets/Navigation.cs` (routes, `Navigator` widget, modal scope)
  - `src/Plumix/Widgets/Navigation.NavigatorState.cs` (history flush, imperative API, `_updatePages`)
  - `src/Plumix/Widgets/Navigation.RouteEntry.cs` (`RouteLifecycle`, `RouteEntry`, `Page`, `TransitionDelegate`)
  - `src/Plumix/Widgets/Navigation.Restoration.cs` (route restoration information and history property)
  - `src/Plumix/Widgets/Hero.cs`
  - `src/Sample/Plumix.Sample/Demos/General/NavigatorDemoPage.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
- Primary Tests:
  - `src/Plumix.Tests/NavigationTests.cs`
  - `src/Plumix.Tests/NavigatorPagesTests.cs`
  - `src/Plumix.Tests/ModalRouteAspectTests.cs`
  - `src/Plumix.Tests/HeroNavigatorTests.cs`

### Scroll and Slivers

- Goal: scroll activities, viewport behavior, sliver child lifecycle, keep-alive.
- Read First:
  - `src/Plumix/Widgets/Scroll.cs`
  - `src/Plumix/Widgets/ScrollContext.cs` (`IScrollContext`: the contract `ScrollableState` gives its `ScrollPosition` — vsync, axis, DPR, notification/storage contexts, `SetIgnorePointer`/`SetCanDrag`/`SetSemanticsActions`/`SaveOffset`; `Rendering/Scroll.cs` holds `ScrollPosition`/activities)
  - `src/Plumix/Widgets/ListWheelScrollView.cs` (list wheel: delegates, `FixedExtentScrollController`/physics, `ListWheelElement`)
  - `src/Plumix/Rendering/ListWheelViewport.cs` (`RenderListWheelViewport`: cylindrical layout/paint/hit test)
  - `src/Plumix/Widgets/Notifications.cs`
  - `src/Plumix/Widgets/ScrollNotificationObserver.cs`
  - `src/Plumix/Widgets/ScrollConfiguration.cs`
  - `src/Plumix/Gestures/VelocityTracker.cs`
  - `src/Plumix/Gestures/LeastSquaresSolver.cs`
  - `src/Plumix/Widgets/OverscrollIndicator.cs`
  - `src/Plumix.Material/MaterialScrollBehavior.cs`
  - `src/Plumix/Widgets/PageStorage.cs`
  - `src/Plumix/Widgets/SharedAppData.cs`
  - `src/Plumix/Rendering/Scroll.cs`
  - `src/Plumix/Rendering/Viewport.RenderViewport.cs`
  - `src/Plumix/Rendering/Viewport.Reveal.cs`
  - `src/Plumix/Rendering/Sliver.cs`
  - `src/Plumix/Rendering/SliverPersistentHeaderReveal.cs`
  - `src/Plumix/Widgets/SliverHeaders.cs`
  - `src/Plumix/Rendering/SliverHeaders.cs`
  - `src/Plumix/Widgets/SliverDecoratedPinned.cs`
  - `src/Plumix/Rendering/DecoratedSliver.cs`
  - `src/Plumix/Widgets/SliverGroup.cs`
  - `src/Plumix/Rendering/SliverGroup.cs`
  - `src/Plumix/Widgets/DraggableScrollableSheet.cs`
- Primary Tests:
  - `src/Plumix.Tests/ScrollPipelineTests.cs`
  - `src/Plumix.Tests/ListWheelScrollViewTests.cs`
  - `src/Plumix.Tests/ViewportRevealTests.cs`
  - `src/Plumix.Tests/ScrollInfrastructureTests.cs`
  - `src/Plumix.Tests/ScrollBehaviorParityTests.cs`
  - `src/Plumix.Tests/ScrollNotificationObserverTests.cs`
  - `src/Plumix.Tests/OverscrollIndicatorTests.cs`
  - `src/Plumix.Tests/StateStorageWidgetsTests.cs`
  - `src/Plumix.Tests/MaterialScrollbarTests.cs`
  - `src/Plumix.Tests/MaterialSliverAppBarTests.cs`
  - `src/Plumix.Tests/DraggableScrollableSheetTests.cs`
  - `src/Plumix.Tests/DecoratedPinnedSliverTests.cs`
  - `src/Plumix.Tests/SliverGroupTests.cs`
  - `src/Plumix.Tests/SliverHeaderTests.cs`
- Note:
  - `Scroll.cs` and `Sliver.cs` are large. Enter through tests first.

### Semantics

- Goal: semantics tree generation, action dispatch, merge/split behavior.
- Read First:
  - `src/Plumix/Rendering/Semantics.cs`
  - `src/Plumix/Rendering/Object.RenderObjectSemantics.cs`
  - `src/Plumix/Rendering/SemanticsConfigurationProvider.cs`
  - `src/Plumix/Widgets/Semantics.cs`
  - `src/Plumix/Widgets/MetaData.cs`
  - `src/Plumix/Rendering/Proxy.RenderBox.cs`
  - `src/Plumix/Widgets/ModalBarrier.cs`
- Primary Tests:
  - `src/Plumix.Tests/SemanticsTreeTests.cs`
  - `src/Plumix.Tests/MetaDataIndexedSemanticsTests.cs`
  - `src/Plumix.Tests/ModalBarrierTests.cs`

### Sample and Dart Parity

- Goal: keep sample feature/route/module parity between C# and Dart samples.
- Read First:
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `docs/ai/PARITY_MATRIX.md`

## Large File Hotspots

Open these only when task scope explicitly requires them:

- `src/Plumix/Rendering/Sliver.cs`
- `src/Plumix/Widgets/Scroll.cs`
- `src/Plumix/Widgets/Navigation.cs`
- `src/Plumix/Widgets/Framework.Element.cs`
- `src/Plumix.Tests/SemanticsTreeTests.cs`
