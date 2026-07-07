# Module Index

Purpose: quickly select the smallest context that still lets the agent close one task end-to-end.

Related docs:

- `docs/ai/INVARIANTS.md`
- `docs/ai/PORTING_MODE.md`
- `docs/ai/TEST_MATRIX.md`
- `docs/ai/PARITY_MATRIX.md`
- `docs/ai/DIVERGENCES.md`
- `docs/ai/FEATURE_TEMPLATE.md`

Current milestone/priority lives only in `docs/FRAMEWORK_PLAN.md` (see its `AI Semantic Snapshot` block); this file does not duplicate it.

## Quick Start

1. Follow the read order from `AGENTS.md` (Context Budget Protocol).
2. For Dart-to-C# ports, read `docs/ai/PORTING_MODE.md` and open matching Flutter Dart source first.
3. Pick one subsystem below, open its `Read First` files, then expand along the control you are closing: `src/Plumix.Material/<Control>.cs` + `<Control>Theme.cs` + `src/Plumix.Tests/Material<Control>Tests.cs` + demo pages in both samples.
4. Enter unfamiliar subsystems through their tests, not through hotspot implementation files.

## Subsystems

### Material Layer

- Goal: Flutter-like Material theming/app-shell/control primitives in framework layers.
- Read First:
  - `src/Plumix.Material/ThemeData.cs`
  - `src/Plumix.Material/Theme.cs`
  - `src/Plumix.Material/Scaffold.cs`
  - `src/Plumix.Material/Buttons.cs`
  - `src/Plumix.Material/ButtonStyle.cs`
  - `src/Plumix.Material/MaterialLocalizations.cs`
- Then per target control: control file + its theme file + its `src/Plumix.Tests/Material*Tests.cs` + demo pages (`src/Sample/Plumix.Sample/Demos/Material/*`, `dart_sample/lib/demos/material/*`).
- Paired composition controls such as `GridTile` + `GridTileBar` share one focused test/demo surface when their Flutter implementations are directly coupled.
- AppBar action controls enter through `ActionButtons.cs` + `ActionIconTheme.cs`, with implied-leading integration covered in `MaterialScaffoldTests.cs`.
- Legacy Material buttons enter through `MaterialButton.cs` + `ButtonTheme.cs`; shared interaction/rendering remains in `Buttons.cs`, with focused coverage in `MaterialLegacyButtonTests.cs`.
- Drawer header controls enter through `DrawerHeader.cs`, with geometry and account-details behavior covered in `MaterialDrawerHeaderTests.cs`.
- Transient message controls enter through `SnackBar.cs` + `SnackBarTheme.cs`, with queue/presentation lifecycle in `ScaffoldMessenger.cs` and coverage in `MaterialSnackBarTests.cs`.
- Bottom sheets enter through `BottomSheet.cs` + `BottomSheetTheme.cs`; persistent presentation integrates with `Scaffold.cs`, modal presentation with `Widgets/Navigation.cs`, and focused coverage lives in `MaterialBottomSheetTests.cs`.
- Sliver app bars enter through `SliverAppBar.cs` + `FlexibleSpaceBar.cs`; persistent-header layout lives in `Widgets/Scroll.cs` and `Rendering/Sliver.cs`, with focused coverage in `MaterialSliverAppBarTests.cs`.
- Dialog-family controls (`Dialog`, `AlertDialog`, `SimpleDialog`, `SimpleDialogOption`) enter through `Dialog.cs` + `DialogTheme.cs`; modal stacking/result behavior also touches `Widgets/Navigation.cs` and is covered by `MaterialDialogTests.cs`.
- Popup-menu controls (`PopupMenuButton`, item/checked/divider entries) enter through `PopupMenu.cs` + `PopupMenuTheme.cs`; anchor geometry and route lifecycle also touch `Widgets/Navigation.cs`/`Widgets/Scroll.cs` and are covered by `MaterialPopupMenuTests.cs`.
- Legacy Material dropdown controls enter through `Dropdown.cs`; selected-size behavior uses core `IndexedStack`, and positioned route/scroll/focus behavior is covered by `MaterialDropdownTests.cs`.
- Disclosure/progress controls enter through `ExpandIcon.cs` + `Stepper.cs`; integration with expansion controls and vertical/horizontal step behavior is covered by `MaterialStepperTests.cs`.
- About/license controls enter through `About.cs` plus core `Foundation/Licenses.cs`; dialog/list/detail navigation and registry parsing are covered by `MaterialAboutTests.cs`.
- Bottom/action bar controls enter through `BottomAppBar.cs` + `BottomAppBarTheme.cs` and `ButtonBar.cs` + `ButtonBarTheme.cs`; Scaffold/FAB geometry and overflow behavior are covered by `MaterialBarControlsTests.cs`.
- Data-table controls enter through `DataTable.cs` + `DataTableTheme.cs` and `PaginatedDataTable.cs`; shared column negotiation lives in core `Widgets/Table.cs` + `Rendering/Table.cs`, with coverage in `MaterialDataTableTests.cs`.
- Material scrollbars enter through `src/Plumix.Material/Scrollbar.cs` + `ScrollbarTheme.cs`; raw overlay/interaction behavior lives in core `Widgets/Scrollbar.cs`, with adaptive defaults in `src/Plumix.Cupertino/CupertinoScrollbar.cs` and focused coverage in `MaterialScrollbarTests.cs`.
- Material tabs enter through `src/Plumix.Material/Tabs.cs` + `TabController.cs` + `TabBarTheme.cs`; page motion lives in core `Widgets/PageView.cs`/`Rendering/PageView.cs`, indicator layout/paint in `RenderTabBar.cs`, and focused coverage in `MaterialTabsTests.cs`.
- Primary Tests:
  - `src/Plumix.Tests/MaterialScaffoldTests.cs`
  - `src/Plumix.Tests/MaterialButtonsTests.cs`
  - Control-specific `src/Plumix.Tests/Material<Control>Tests.cs`

### Runtime and Host

- Goal: frame scheduling, pipeline wiring, Avalonia host integration.
- Read First:
  - `src/Plumix/FlutterHost.cs`
  - `src/Plumix/WidgetHost.cs`
  - `src/Plumix/Scheduler.cs`
  - `src/Plumix/PipelineOwner.cs`
  - `src/Plumix/RenderView.cs`
- Primary Tests:
  - `src/Plumix.Tests/FramePipelineTests.cs`
  - `src/Plumix.Tests/RenderingParityTests.cs`

### Widget/Element Lifecycle

- Goal: reconciliation, state retention/disposal, dependency propagation.
- Read First:
  - `src/Plumix/Widgets/Framework.Widget.cs`
  - `src/Plumix/Widgets/Framework.Element.cs`
  - `src/Plumix/Widgets/Framework.BuildOwner.cs`
  - `src/Plumix/Widgets/Framework.RenderObject.cs`
  - `src/Plumix/Foundation/Key.cs`
- Primary Tests:
  - `src/Plumix.Tests/ElementLifecycleTests.cs`
  - `src/Plumix.Tests/InheritedWidgetTests.cs`
  - `src/Plumix.Tests/InheritedModelTests.cs`
  - `src/Plumix.Tests/InheritedNotifierTests.cs`

### Core Layout/Paint/Compositing

- Goal: box constraints, relayout boundaries, repaint boundaries, layers.
- Read First:
  - `src/Plumix/Rendering/Object.RenderObject.cs`
  - `src/Plumix/Rendering/Box.RenderBox.cs`
  - `src/Plumix/Rendering/Proxy.RenderBox.cs`
  - `src/Plumix/Rendering/Layer.cs`
  - `src/Plumix/Rendering/Object.PaintingContext.cs`
- Primary Tests:
  - `src/Plumix.Tests/RenderingParityTests.cs`
  - `src/Plumix.Tests/CompositingLayerTests.cs`
  - `src/Plumix.Tests/LayerV2Tests.cs`

### Images and Decoration Paint

- Goal: Flutter-like image resolution, caching, stream lifetime, and decoration paint geometry.
- Read First:
  - `src/Plumix/Rendering/ImageProvider.cs`
  - `src/Plumix/Rendering/ImageStream.cs`
  - `src/Plumix/Rendering/ImageCache.cs`
  - `src/Plumix/Rendering/DecorationImage.cs`
  - `src/Plumix/Rendering/Proxy.RenderBox.cs`
- Primary Tests:
  - `src/Plumix.Tests/ImageProviderDecorationTests.cs`

### Gestures and Input

- Goal: pointer dispatch, hit testing, arena resolution, recognizer callbacks.
- Read First:
  - `src/Plumix/UI/PointerEvents.cs`
  - `src/Plumix/Gestures/GestureBinding.cs`
  - `src/Plumix/Gestures/GestureArena.cs`
  - `src/Plumix/Widgets/Gestures.cs`
  - `src/Plumix/Rendering/Object.HitTest.cs`
- Primary Tests:
  - `src/Plumix.Tests/GesturePipelineTests.cs`

### Navigation

- Goal: route stack operations, named routes, observers, back handling, hero flights.
- Read First:
  - `src/Plumix/Widgets/Navigation.cs`
  - `src/Plumix/Widgets/Hero.cs`
  - `src/Sample/Plumix.Sample/Demos/General/NavigatorDemoPage.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
- Primary Tests:
  - `src/Plumix.Tests/NavigationTests.cs`
  - `src/Plumix.Tests/HeroNavigatorTests.cs`

### Scroll and Slivers

- Goal: scroll activities, viewport behavior, sliver child lifecycle, keep-alive.
- Read First:
  - `src/Plumix/Widgets/Scroll.cs`
  - `src/Plumix/Rendering/Scroll.cs`
  - `src/Plumix/Rendering/Viewport.RenderViewport.cs`
  - `src/Plumix/Rendering/Sliver.cs`
- Primary Tests:
  - `src/Plumix.Tests/ScrollPipelineTests.cs`
  - `src/Plumix.Tests/ScrollInfrastructureTests.cs`
  - `src/Plumix.Tests/MaterialScrollbarTests.cs`
  - `src/Plumix.Tests/MaterialSliverAppBarTests.cs`
- Note:
  - `Scroll.cs` and `Sliver.cs` are large. Enter through tests first.

### Semantics

- Goal: semantics tree generation, action dispatch, merge/split behavior.
- Read First:
  - `src/Plumix/Rendering/Semantics.cs`
  - `src/Plumix/Rendering/Object.RenderObjectSemantics.cs`
  - `src/Plumix/Rendering/SemanticsConfigurationProvider.cs`
- Primary Tests:
  - `src/Plumix.Tests/SemanticsTreeTests.cs`

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
