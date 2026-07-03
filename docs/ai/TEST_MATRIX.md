# Test Matrix

Purpose: map framework areas to existing test coverage and identify common gaps quickly.

Document rules:

- Keep every cell short: 2-4 sentences max. This file answers "where are the tests and what do they roughly cover", nothing more.
- Detailed behavior lives in the tests themselves; historical detail lives in `CHANGELOG.md`. Do not append per-iteration coverage prose here.
- One row per area. If a row does not fit, add one before shipping the feature.

## Coverage Map

| Area | Primary tests | What is covered | Typical gap to watch |
| --- | --- | --- | --- |
| Element lifecycle and reconciliation | `src/Plumix.Tests/ElementLifecycleTests.cs` | Keyed/unkeyed reorder, global key reparenting, activation/disposal, nested mixed keyed groups. | Multi-frame stress cases combining nested groups and `forgottenChildren` paths. |
| Inherited dependencies | `src/Plumix.Tests/InheritedWidgetTests.cs`, `InheritedModelTests.cs`, `InheritedNotifierTests.cs` | Dependent registration and selective rebuild semantics. | Deep tree shadowing and dynamic notifier swaps. |
| Frame and scheduler flow | `src/Plumix.Tests/FramePipelineTests.cs` | Begin/draw/post-frame order, persistent callbacks, build scheduling in frame. | Long-running callbacks vs host visibility transitions. |
| Render object parity | `src/Plumix.Tests/RenderingParityTests.cs` | Constraint normalization, relayout boundaries, layout exception propagation, constraint clamping, invalidation no-op guards, `RenderFlex` overflow debug paint. | Constraint edge cases across deep mixed proxy/flex chains. |
| Compositing and layers | `src/Plumix.Tests/CompositingLayerTests.cs`, `LayerV2Tests.cs` | Repaint boundary behavior, layer reuse/updates, layer tree structure, resize-driven clip-layer refresh. | Layer churn under repeated boundary toggles in large trees. |
| Basic proxy/layout/decoration widgets | `src/Plumix.Tests/BasicWidgetProxyTests.cs`, `AlignTests.cs`, `AspectRatioTests.cs`, `FractionallySizedBoxTests.cs`, `FittedBoxTests.cs`, `UnconstrainedLimitedBoxTests.cs`, `OverflowBoxTests.cs`, `OffstageTests.cs`, `StackTests.cs`, `DecoratedBoxTests.cs`, `ContainerTests.cs` | Widget-to-render wiring and rebuild behavior for proxy/layout/decoration widgets (`Opacity`, `Transform`, `ClipRect`, `Align`, `AspectRatio`, `Spacer`, `FittedBox`, `Stack/Positioned`, `Container`, etc.). | Nested layout stress with deep overlay stacks and mixed unconstrained parents. |
| Image providers and decoration images | `src/Plumix.Tests/ImageProviderDecorationTests.cs` | Stream lifetime/error replay, pending/keepAlive/live cache behavior, provider keys, DPR assets, resize guards, painter invalidation, fit/crop/repeat/RTL/nine-patch geometry, and `BoxDecoration` integration. | Animated codecs and backend color/filter effects tracked in `DIVERGENCES.md`. |
| MediaQuery and SafeArea | `src/Plumix.Tests/SafeAreaTests.cs` | `MediaQueryData` insets transformations, `SafeArea` edge/minimum semantics, root ambient `MediaQuery` wiring from `WidgetHost`. | Full `MediaQueryData` field parity and aspect-specific rebuild optimization. |
| Text widget and paragraph layout | `src/Plumix.Tests/TextWidgetTests.cs` | `Text` -> `RenderParagraph` wiring for layout-affecting options, `DefaultTextStyle` resolution, `Icon`/`IconTheme` basics. | Runtime visual parity across hosts/fonts (line metrics, glyph shaping). |
| Material theming and app shell | `src/Plumix.Tests/MaterialScaffoldTests.cs` | `Theme`/`ThemeData` lookup and propagation, `Scaffold` background/app-bar/drawer behavior, app-bar precedence chains (colors, icon themes, text styles, title layout, implied leading), drawer open/close/drag choreography and theme precedence. | Sliver app bars, scroll-under elevation, adaptive title/action spacing. |
| Material buttons | `src/Plumix.Tests/MaterialButtonsTests.cs`, `MaterialFloatingActionButtonTests.cs` | Button family defaults (M2/M3 split), `ButtonStyle`/`styleFrom` precedence and state resolution, overlay/splash/focus/keyboard activation, icon factories, tap-target sizes, FAB variants/theme overrides. | Full Flutter `ButtonStyle` parity matrix and deeper token combinations. |
| Material action buttons | `src/Plumix.Tests/MaterialActionButtonsTests.cs`, `MaterialScaffoldTests.cs` | `BackButton`/`CloseButton` platform glyphs, action-icon theme precedence, localized tooltip semantics (including Android icon duplication), custom/default pop callbacks, style precedence, and AppBar implied-leading integration. | Cross-host accessibility verification with native screen readers. |
| Material selection controls | `src/Plumix.Tests/MaterialCheckboxTests.cs`, `MaterialSwitchTests.cs`, `MaterialRadioTests.cs` | Value/tristate cycles, mode-aware defaults, theme precedence, semantics, keyboard toggle, adaptive Cupertino paths. | Cupertino fidelity extras (haptics, on/off labels, image thumbs). |
| Material progress indicators | `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`, `MaterialCircularProgressIndicatorTests.cs` | Determinate/indeterminate behavior, M2/M3 and `year2023` defaults, theme/widget precedence, `valueColor` precedence, adaptive paths, semantics fallback. | Broader animation timing parity beyond covered scenarios. |
| Material sliders | `src/Plumix.Tests/MaterialSliderTests.cs`, `MaterialRangeSliderTests.cs` | Constructor guards, M2/M3 default colors, theme/widget precedence, drag lifecycle callbacks, discrete snapping, keyboard adjustment, semantics. | Advanced track shapes and full `SliderTheme` surface. |
| Material navigation surfaces | `src/Plumix.Tests/MaterialBottomNavigationBarTests.cs`, `MaterialNavigationSurfacesTests.cs` | Bottom navigation plus `NavigationBar`/`NavigationRail` guards, M2/M3 defaults, theme precedence, label/indicator geometry, tap/disabled states, and localized semantics. | Cross-host golden rendering at extreme text scales. |
| Material Card | `src/Plumix.Tests/MaterialCardTests.cs` | Variant defaults (elevated/filled/outlined), M2 fallback, theme precedence, surface tint, clip behavior, `semanticContainer`. | Full Flutter `ShapeBorder` hierarchy parity. |
| Material list tiles and expansion | `src/Plumix.Tests/MaterialListTileTests.cs`, `MaterialRadioExpansionTileTests.cs`, `MaterialExpansionPanelTests.cs` | `ListTile` baseline, `Checkbox/Switch/RadioListTile` row toggles and affinity, `ExpansionTile`/`ExpansionPanelList` expansion control, callbacks, animated transitions, semantics. | `_RenderListTile` geometry parity, `PageStorage`, per-corner radius interpolation. |
| Material Divider | `src/Plumix.Tests/MaterialDividerTests.cs` | `Divider`/`VerticalDivider` guards, mode-aware defaults, theme/widget precedence. | — |
| Material Badge and Tooltip | `src/Plumix.Tests/MaterialBadgeTests.cs`, `MaterialTooltipTests.cs` | Badge count/geometry/theme/RTL behavior and tooltip defaults/theme/timing/semantics/programmatic lifecycle. | Root-overlay positioning and rich-message support await shared core primitives. |
| Material CircleAvatar | `src/Plumix.Tests/MaterialCircleAvatarTests.cs` | Constructor contracts, radius constraints, M2/M3 colors and typography, circular foreground/background image composition, error fallback, and implicit color/diameter transitions. | Animated image codecs and isolated additive crossfade blending remain backend gaps. |
| Material chips | `src/Plumix.Tests/MaterialChipTests.cs` | `ActionChip`/`ChoiceChip`/`FilterChip`/`InputChip` guards, M2/M3 flat/elevated defaults, theme/widget/state precedence, body/delete callbacks, selection/disabled semantics, localization, checkmarks, density, and slot constraints. | Specialized `_RenderChip` geometry plus exact enable/avatar drawer choreography. |
| Material grid tiles | `src/Plumix.Tests/MaterialGridTileTests.cs` | `GridTile` direct/overlay composition, positioned header/footer geometry, `GridTileBar` heights, typography, icons, backgrounds, directional padding, and RTL slot order. | Cross-host visual verification with complex custom title widgets. |
| Semantics tree | `src/Plumix.Tests/SemanticsTreeTests.cs` | Actions, merge/split, transform/clip, dirty propagation, id reuse contracts. | End-to-end assistive flows with platform accessibility tooling. |
| Host semantics bridge | `src/Plumix.Tests/FlutterHostSemanticsTests.cs` | `FlutterHost` semantics runtime surface and host-level action routing. | Platform adapter wiring to native accessibility APIs. |
| Gestures and hit testing | `src/Plumix.Tests/GesturePipelineTests.cs` | Transform/clip hit testing, recognizer dispatch, arena conflicts, drag deltas/velocity, hover enter/exit. | Multi-pointer interactions and cancellation races. |
| Navigation | `src/Plumix.Tests/NavigationTests.cs`, `HeroNavigatorTests.cs` | Push/pop/replace/remove, named routes, observers, back dispatch across nested navigators, hero flight choreography (tween/shuttle/placeholder, diversion, guards). | Host-native gesture routing integrations. |
| Focus and keyboard flow | `src/Plumix.Tests/FocusTests.cs`, `FlutterHostInputTests.cs` | Focus ownership, autofocus, key dispatch, tab traversal, directional traversal with transform-aware rects. | Rotated/non-invertible transforms, deep scope hierarchies. |
| Editable text input | `src/Plumix.Tests/TextInputTests.cs` | Focused input delivery, selection/caret editing, IME composition lifecycle and host preedit bridge, multiline editing, word/paragraph/clipboard shortcuts, grapheme-aware behavior. | Visual bidi navigation and platform text-action menu parity. |
| Scroll/slivers core | `src/Plumix.Tests/ScrollPipelineTests.cs` | Scroll physics, viewport/sliver layout, cache extent, keep-alive reuse. | Very large child counts, rapid direction changes. |
| Scroll widget infrastructure | `src/Plumix.Tests/ScrollInfrastructureTests.cs` | Notifications, primary controller, keep-alive mixin, list/grid constructor semantics. | Nested scrollables and scrollbar interaction nuances. |
| Sample state behavior | `src/Plumix.Tests/SampleCounterStateTests.cs` | Counter model notifications and scope dependency behavior. | End-to-end sample page regressions across demo routes. |

## How to Use for Iterative Work

1. Pick feature area row.
2. Read listed tests before opening implementation files.
3. Update or add tests in same row when behavior changes; keep the row summary within the cell-size rule.
4. If no row fits, add one before shipping the feature.

## Porting Rule (Dart -> C#)

- For control/widget ports, tests must validate Dart-parity-critical behavior from `docs/ai/PORTING_MODE.md`:
  - defaults,
  - interaction states,
  - high-risk constraint/layout scenarios.
