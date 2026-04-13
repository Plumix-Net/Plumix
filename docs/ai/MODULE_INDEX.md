# Module Index

Purpose: quickly select the smallest context that still lets the agent close one task end-to-end.

Related docs:

- `docs/ai/INVARIANTS.md`
- `docs/ai/PORTING_MODE.md`
- `docs/ai/TEST_MATRIX.md`
- `docs/ai/PARITY_MATRIX.md`
- `docs/ai/FEATURE_TEMPLATE.md`

## Quick Start

1. Read `AGENTS.md`.
2. Read `docs/FRAMEWORK_PLAN.md`.
3. For Dart-to-C# ports, read `docs/ai/PORTING_MODE.md` and open matching Dart reference files first.
4. Pick one subsystem below and open its `Read First` files plus directly related control files/tests needed to close the target control.
5. Expand context proactively whenever needed to finish the same control in one request; avoid partial-parity checkpoints unless there is a hard blocker.

Current priority (2026-03-12):

- Active milestone is `M4` (Material library rewrite). Cross-host parity/stability moved to final `M5`.

## Subsystems

### Material Layer (M4 Focus)

- Goal: establish Flutter-like Material theming/app-shell/control primitives in framework layers.
- Read First:
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Theme.cs`
  - `src/Flutter.Material/MaterialLocalizations.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Material/BottomNavigationBar.cs`
  - `src/Flutter.Material/BottomNavigationBarTheme.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/IconButton.cs`
  - `src/Flutter.Material/FloatingActionButton.cs`
  - `src/Flutter.Material/FloatingActionButtonTheme.cs`
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter/Widgets/MouseCursor.cs`
  - `src/Flutter/UI/Feedback.cs`
  - `src/Flutter/UI/Clip.cs`
  - `src/Flutter.Material/Checkbox.cs`
  - `src/Flutter.Material/Switch.cs`
  - `src/Flutter.Material/Radio.cs`
  - `src/Flutter/Widgets/Framework.Widget.cs`
  - `src/Flutter/Widgets/DefaultTextStyle.cs`
  - `src/Flutter/Widgets/Text.cs`
  - `src/Sample/Flutter.Net/CounterApp.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `src/Sample/Flutter.Net/Demos/Material/MaterialButtonsDemoPage.cs`
  - `src/Sample/Flutter.Net/Demos/Material/FloatingActionButtonDemoPage.cs`
  - `src/Sample/Flutter.Net/Demos/Cupertino/CheckboxDemoPage.cs`
  - `src/Sample/Flutter.Net/Demos/Cupertino/SwitchDemoPage.cs`
  - `src/Sample/Flutter.Net/Demos/Cupertino/RadioDemoPage.cs`
  - `dart_sample/lib/counter_app.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/demos/material/material_buttons_demo_page.dart`
  - `dart_sample/lib/demos/material/floating_action_button_demo_page.dart`
  - `dart_sample/lib/demos/cupertino/checkbox_demo_page.dart`
  - `dart_sample/lib/demos/cupertino/switch_demo_page.dart`
  - `dart_sample/lib/demos/cupertino/radio_demo_page.dart`
- Primary Tests:
  - `src/Flutter.Tests/TextWidgetTests.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
  - `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
  - `src/Flutter.Tests/MaterialRadioTests.cs`

### Runtime and Host

- Goal: frame scheduling, pipeline wiring, Avalonia host integration.
- Read First:
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter/WidgetHost.cs`
  - `src/Flutter/Scheduler.cs`
  - `src/Flutter/PipelineOwner.cs`
  - `src/Flutter/RenderView.cs`
- Primary Tests:
  - `src/Flutter.Tests/FramePipelineTests.cs`
  - `src/Flutter.Tests/RenderingParityTests.cs`
- Usually Skip Initially:
  - `src/Flutter/Rendering/Sliver.cs`
  - `src/Flutter/Widgets/Scroll.cs`

### Widget/Element Lifecycle

- Goal: reconciliation, state retention/disposal, dependency propagation.
- Read First:
  - `src/Flutter/Widgets/Framework.Widget.cs`
  - `src/Flutter/Widgets/Framework.Element.cs`
  - `src/Flutter/Widgets/Framework.BuildOwner.cs`
  - `src/Flutter/Widgets/Framework.RenderObject.cs`
  - `src/Flutter/Foundation/Key.cs`
- Primary Tests:
  - `src/Flutter.Tests/ElementLifecycleTests.cs`
  - `src/Flutter.Tests/InheritedWidgetTests.cs`
  - `src/Flutter.Tests/InheritedModelTests.cs`
  - `src/Flutter.Tests/InheritedNotifierTests.cs`

### Core Layout/Paint/Compositing

- Goal: box constraints, relayout boundaries, repaint boundaries, layers.
- Read First:
  - `src/Flutter/Rendering/Object.RenderObject.cs`
  - `src/Flutter/Rendering/Box.RenderBox.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter/Rendering/Layer.cs`
  - `src/Flutter/Rendering/Object.PaintingContext.cs`
- Primary Tests:
  - `src/Flutter.Tests/RenderingParityTests.cs`
  - `src/Flutter.Tests/CompositingLayerTests.cs`
  - `src/Flutter.Tests/LayerV2Tests.cs`

### Gestures and Input

- Goal: pointer dispatch, hit testing, arena resolution, recognizer callbacks.
- Read First:
  - `src/Flutter/UI/PointerEvents.cs`
  - `src/Flutter/Gestures/GestureBinding.cs`
  - `src/Flutter/Gestures/GestureArena.cs`
  - `src/Flutter/Gestures/TapGestureRecognizer.cs`
  - `src/Flutter/Gestures/DragGestureRecognizer.cs`
  - `src/Flutter/Widgets/Gestures.cs`
  - `src/Flutter/Rendering/Object.HitTest.cs`
- Primary Tests:
  - `src/Flutter.Tests/GesturePipelineTests.cs`

### Navigation

- Goal: route stack operations, named routes, observers, back handling.
- Read First:
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Sample/Flutter.Net/Demos/General/NavigatorDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
- Primary Tests:
  - `src/Flutter.Tests/NavigationTests.cs`

### Scroll and Slivers

- Goal: scroll activities, viewport behavior, sliver child lifecycle, keep-alive.
- Read First:
  - `src/Flutter/Widgets/Scroll.cs`
  - `src/Flutter/Rendering/Scroll.cs`
  - `src/Flutter/Rendering/Viewport.RenderViewport.cs`
  - `src/Flutter/Rendering/Sliver.cs`
- Primary Tests:
  - `src/Flutter.Tests/ScrollPipelineTests.cs`
  - `src/Flutter.Tests/ScrollInfrastructureTests.cs`
- Note:
  - `Scroll.cs` and `Sliver.cs` are large. Enter through tests first.

### Semantics

- Goal: semantics tree generation, action dispatch, merge/split behavior.
- Read First:
  - `src/Flutter/Rendering/Semantics.cs`
  - `src/Flutter/Rendering/Object.RenderObjectSemantics.cs`
  - `src/Flutter/Rendering/SemanticsConfigurationProvider.cs`
- Primary Tests:
  - `src/Flutter.Tests/SemanticsTreeTests.cs`

### Sample and Dart Parity

- Goal: keep sample feature/route/module parity between C# and Dart.
- Read First:
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `src/Sample/Flutter.Net/Demos/General/NavigatorDemoPage.cs`
  - `dart_sample/lib/demos/general/navigator_demo_page.dart`
  - `docs/ai/PARITY_MATRIX.md`

## Large File Hotspots

Open these only when task scope explicitly requires them:

- `src/Flutter/Rendering/Sliver.cs`
- `src/Flutter/Widgets/Scroll.cs`
- `src/Flutter/Widgets/Navigation.cs`
- `src/Flutter/Widgets/Framework.Element.cs`
- `src/Flutter.Tests/SemanticsTreeTests.cs`
