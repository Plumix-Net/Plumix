# Framework Master Plan

This is the single source of truth for framework status, direction, and implementation priorities.

## AI Semantic Snapshot

Use this block as the fastest machine-readable status summary.

```yaml
framework_plan_version: 1
last_updated: 2026-04-04
north_star: "Flutter-like widget/rendering framework in C# with Avalonia as host infrastructure."
current_phase: "M4 material library rewrite (theme/scaffold/material controls) in progress."
status:
  widget_element_state_lifecycle: done
  render_pipeline_layout_paint_compositing_semantics: done
  scheduler_ticker_frame_flow: done
  gesture_arena_and_recognizers: done
  navigation_stack_and_observers: done
  scroll_sliver_list_grid_pipeline: done
  desktop_widget_host_app_flow: done
  material_library_rewrite: in_progress
  browser_android_ios_sample_hosts: planned
  dart_to_csharp_control_porting_readiness: in_progress
  docs_alignment_and_tracking: in_progress
next_milestones:
  - id: M1
    title: "Core parity hardening"
    status: done
  - id: M2
    title: "Input/focus/accessibility completion"
    status: done
  - id: M3
    title: "Port-first widget set expansion"
    status: done
  - id: M4
    title: "Material library rewrite"
    status: in_progress
  - id: M5
    title: "Cross-host sample parity and stability"
    status: planned
```

## Confirmed Done (Repository Baseline)

- [x] Flutter-like core abstractions exist and are wired: `Widget -> Element -> RenderObject`.
- [x] Stateful lifecycle and build scheduling are implemented (`State`, `SetState`, `BuildOwner`).
- [x] Inherited dependency model is implemented (`InheritedWidget`, `InheritedModel`, `InheritedNotifier`).
- [x] Render pipeline is implemented (`PipelineOwner`, layout/compositing/paint/semantics phases).
- [x] Layer tree primitives are implemented (offset/opacity/transform/clip/picture layers).
- [x] Gesture system is implemented (pointer router, arena, tap/drag/long-press recognizers).
- [x] Navigation stack is implemented (`Navigator`, routes, named routes, observers, back handling).
- [x] Scroll/sliver stack is implemented (`Scrollable`, `Viewport`, sliver lists/grids, keep-alive, notifications).
- [x] Widget host path is active on desktop (`FlutterExtensions.Run` + `WidgetHost`).
- [x] Sample gallery demonstrates navigation, scrolling, and editable text/focus demos through framework widgets.
- [x] Automated test project exists and covers lifecycle, rendering, layers, semantics, gestures, navigation, and scrolling.

## Global Plan

### M1. Core Parity Hardening

Status: `done`

Completion note:

- Closed on 2026-03-10 after targeted parity hardening passes for layout exception surfacing, constraint clamping behavior, and render/compositing invalidation no-op guards.

Exit criteria:

- Core render object semantics remain compatible with expected Flutter behavior in covered scenarios.
- Existing test suites stay green while adding missing parity edge-cases.
- Layout/paint/semantics invalidation rules are documented and predictable for contributors.

### M2. Input, Focus, and Accessibility Completion

Status: `done`

Completion note:

- Closed on 2026-03-10 after delivering text-editing ergonomics baselines (word/paragraph shortcuts, clipboard copy/cut/paste, grapheme-safe caret/delete behavior), transform-aware directional traversal rect resolution, and host semantics bridge runtime surface (`SemanticsRoot`, `SemanticsUpdated`, action dispatch) with coverage in framework tests.

Progress update (2026-03-11):

- Keyboard/focus baseline is implemented and host-wired (`KeyEvent`, `FocusNode`, `FocusManager`, `Focus`).
- Focus scopes are now available (`FocusScopeNode`, `FocusScope`) and traversal is bounded to the active scope (Tab + directional keys).
- Directional traversal includes a geometry-aware policy when focus bounds are available, with deterministic sequential fallback.
- Directional traversal now resolves traversal geometry through attached render-object transforms (including `RenderTransform`) before directional candidate ranking.
- Editable text baseline is integrated (`EditableText`, `TextEditingController`, host text input dispatch into focused node callbacks).
- Editable controller/selection baseline is integrated (`TextSelection`, `TextRange`, selection-aware insert/delete/navigation, `Ctrl/Meta+A`).
- Editable composition lifecycle baseline is integrated (focus-manager composition update/commit dispatch, controller composing state, editable-widget composition handling).
- Host-native IME preedit bridge baseline is integrated (`TextInputMethodClientRequested` -> `TextInputMethodClient.SetPreeditText` -> focus-manager composition updates).
- IME state sync baseline is integrated (`surrounding text`, selection sync, and cursor rectangle exposure from focused editable state through host text-input client).
- Multiline editing + glyph-aware caret baseline is integrated (multiline mode with `Enter` newline insertion, `ArrowUp/ArrowDown` vertical navigation, and `TextLayout`-driven caret rectangle with host-less fallback).
- Word-level text-editing shortcuts baseline is integrated (`Ctrl/Alt + ArrowLeft/ArrowRight` word navigation and `Ctrl/Alt + Backspace/Delete` word deletion in `EditableText`).
- Paragraph-level caret shortcuts baseline is integrated for multiline editing (`Ctrl/Alt + ArrowUp/ArrowDown` paragraph start/end navigation with selection extension support).
- Clipboard/action shortcut baseline is integrated (`Ctrl/Meta + C/X/V`) with framework clipboard cache and host clipboard synchronization hooks in `FlutterHost`.
- Grapheme-aware editing baseline is integrated (caret and delete operations move by text elements instead of UTF-16 code units).
- Host accessibility bridge documentation baseline is now captured (host-side semantics tree consumption and action dispatch expectations per target host).
- Host accessibility bridge runtime baseline is integrated in `FlutterHost` (semantics root exposure, semantics-updated event, and action routing API), with regression coverage.

Exit criteria:

- Keyboard/focus flow is implemented as first-class framework behavior.
- Semantics actions and tree updates are stable for interactive controls.
- Platform accessibility bridge expectations are documented per host.

### M3. Port-First Widget Set Expansion

Status: `done`

Completion note:

- Closed on 2026-03-11 after delivering the planned port-first widget baseline set (proxy, alignment, stack/positioned, decoration/container composition, ratio/fractional/fitted sizing, unconstrained/overflow/offstage) with mirrored C#/Dart sample routes and focused regression coverage.
- Included post-baseline text-rendering parity hardening needed for control-port fidelity: `Text` now wires `textAlign`/`softWrap`/`maxLines`/`overflow`/`textDirection`, and `RenderParagraph` no longer applies a synthetic `maxWidth=1000` cap for unbounded layout.
- Continued post-M3 typography parity hardening: framework `Text` now exposes `fontWeight`, `fontStyle`, `height`, and `letterSpacing`, with matching `RenderParagraph` layout support and host-default font-family defaults across paragraph/button/editable text layout paths.
- Continued text-style inheritance parity hardening: framework now includes `DefaultTextStyle`/`TextStyle`, and `Text` resolves inherited typography defaults (`fontFamily`, `fontSize`, `color`, `fontWeight`, `fontStyle`, `height`, `letterSpacing`) with local override precedence; sample root now provides a Material-like default body style to reduce C#/Dart menu text wrapping and line-height drift.
- Continued paragraph-alignment parity hardening: `RenderParagraph` now normalizes loose-width `center/right/end` layout width to content width when host layout reports positive internal glyph offset, eliminating right-shifted intrinsic label paint in sample list/button scenarios while retaining tight-width aligned behavior.
- Counter sample viewport hardening: `CounterScreen` now uses outer `SingleChildScrollView` in both C# and Dart samples so smaller desktop heights avoid `RenderFlex` bottom-overflow debug zones while preserving existing demo modules on the page.
- Overflow-debug parity progression: `RenderFlex` now paints Flutter-style yellow/black overflow indicators with clipped overflow child paint, 45-degree marker geometry, and edge-aligned/rotated labels for main-axis overflow; both samples include a dedicated overflow-indicator demo page for runtime verification.

Completion snapshot:

- Added first proxy-widget port baseline in framework widget layer: `Opacity`, `Transform`, and `ClipRect` wrappers over existing render primitives (`RenderOpacity`, `RenderTransform`, `RenderClipRect`) with focused rebuild/update regression coverage.
- Added sample parity route/page in both C# and Dart sample galleries for interactive proxy-widget composition checks (`Opacity`, `Transform`, `ClipRect`).
- Fixed compositing edge case where repaint-boundary layer-property updates could be dropped when repaint and composited-layer invalidation happened in the same frame.
- Added single-child alignment baseline in framework widget layer: `Align` and `Center` over new `RenderAlign`, including width/height shrink factors and parity sample route/page in both C# and Dart galleries.
- Added multi-child overlay baseline in framework widget layer: `Stack` and `Positioned` over new `RenderStack`/`StackParentData`, including positioned insets/size behavior and parity sample route/page in both C# and Dart galleries.
- Added decoration baseline in framework widget layer: `DecoratedBox` over `RenderDecoratedBox` plus value objects (`BoxDecoration`, `BorderSide`, `BorderRadius`) and parity sample route/page in both C# and Dart galleries.
- Extended `Container` composition baseline with `alignment`, `margin`, `constraints`, and `transform` support (including Flutter-like width/height tightening against explicit constraints), plus parity sample route/page updates in both C# and Dart galleries.
- Added ratio/flex layout primitive baseline in framework widget layer: `AspectRatio` over new `RenderAspectRatio` plus `Spacer` (expanded tight-flex gap helper), with regression coverage for ratio sizing, widget update wiring, and flex parent-data propagation.
- Added sample parity route/page in both C# and Dart sample galleries for interactive `AspectRatio` and `Spacer` behavior checks.
- Hardened `Spacer` demo observability and coverage: updated parity demo to compare two spacer slots with asymmetric flex and added regression coverage for proportional `RenderFlex` distribution across two spacer allocations.
- Added fractional sizing baseline in framework widget layer: `FractionallySizedBox` over new `RenderFractionallySizedBox` with bounded-axis factor constraints and alignment-aware child placement, plus parity sample route/page in both C# and Dart galleries.
- Added fitted scaling baseline in framework widget layer: `FittedBox` over new `RenderFittedBox` with `BoxFit` sizing semantics, transform-aware paint/hit-test mapping, and alignment-controlled placement.
- Added sample parity route/page in both C# and Dart sample galleries for interactive `FittedBox` (`contain/cover/fill/none/scaleDown`) and alignment checks.
- Added unconstrained/limited constraints baseline in framework widget layer: `UnconstrainedBox` over `RenderUnconstrainedBox` (axis-specific unconstraining + alignment) and `LimitedBox` over `RenderLimitedBox` (max clamp applied only on unbounded axes).
- Added sample parity route/page in both C# and Dart sample galleries for interactive `UnconstrainedBox + LimitedBox` behavior checks (`constrainedAxis` and `maxWidth/maxHeight`).
- Added overflow constraints baseline in framework widget layer: `OverflowBox` over `RenderConstrainedOverflowBox` (optional min/max overrides + `OverflowBoxFit` sizing mode) and `SizedOverflowBox` over `RenderSizedOverflowBox` (fixed own size with parent constraints passed through to child).
- Added sample parity route/page in both C# and Dart sample galleries for interactive `OverflowBox + SizedOverflowBox` behavior checks (`fit`, `alignment`, override max constraints, requested own size).
- Added offstage layout baseline in framework widget layer: `Offstage` over `RenderOffstage` with Flutter-like offstage behavior (child still laid out, parent collapses to smallest size, and paint/hit-test/semantics participation suppressed while offstage).
- Added sample parity route/page in both C# and Dart sample galleries for interactive `Offstage` behavior checks in row layout (state toggle and zero-space collapse).

Exit criteria:

- Priority controls needed for Dart-to-C# rewrites are identified and implemented in framework layers.
- New widgets use the same architecture boundaries (`Widget -> Element -> RenderObject`) without leaking behavior into Avalonia controls.
- Sample gallery includes representative real-world compositions beyond demos.

### M4. Material Library Rewrite

Status: `in_progress`

Kickoff note (2026-03-12):

- Prioritized immediately after M3 to unblock practical control rewrites and reduce sample-level styling drift by introducing a Flutter-like Material layer in framework widgets.

Progress update (2026-03-19):

- Added dedicated framework Material assembly: `src/Flutter.Material/Flutter.Material.csproj`.
- Introduced initial theming primitives: `ThemeData`, `MaterialTextTheme`, and inherited `Theme`.
- `Theme` now propagates baseline `TextTheme.BodyMedium` through `DefaultTextStyle`, enabling framework `Text` defaults without sample-only wrappers.
- C# sample app root now uses `Theme(data: ThemeData.Light, child: ...)`; Dart sample root now sets explicit `MaterialApp` text-theme baseline (`bodyMedium` 14/1.43/0.25) for parity.
- Added regression coverage for theme-to-text propagation in `src/Flutter.Tests/TextWidgetTests.cs`.
- Added Material shell primitives: `Scaffold` and `AppBar` in `src/Flutter.Material` with baseline slot wiring (`body`, `appBar`, `floatingActionButton`, `bottomNavigationBar`, title/leading/actions).
- C# sample gallery pages now use framework `Scaffold`/`AppBar` composition for menu/demo shells; Dart sample gallery mirrors the same structural shell usage.
- Added regression coverage for scaffold/app-bar theme resolution and widget composition behavior in `src/Flutter.Tests/MaterialScaffoldTests.cs`.
- Extended `AppBar` title-layout parity with Flutter-like controls: `centerTitle` and `titleSpacing` (default `16`) with non-negative finite spacing validation, plus title insets driven by `titleSpacing`.
- Updated centered-title composition: when `leading` exists and `actions` are absent, a symmetric trailing slot now matches effective leading width to keep title centering stable.
- Added focused `MaterialScaffoldTests` coverage for centered-title alignment wiring, `titleSpacing` horizontal padding, and negative-spacing argument guard.
- Added `ThemeData.Platform` and `ThemeData.AppBarTheme` (`AppBarThemeData.CenterTitle`) for app-bar default policy wiring in `Flutter.Material`.
- Switched `AppBar` center-title default resolution to Flutter-like precedence (`widget centerTitle` -> `theme appBarTheme.centerTitle` -> platform fallback) with macOS/iOS fallback rule (`actions.Count < 2` centers title).
- Expanded `MaterialScaffoldTests` with deterministic center-title default/precedence coverage for theme override and platform fallback behavior.
- Expanded app-bar theme parity in `Flutter.Material` with `AppBarThemeData` fields `titleSpacing`, `toolbarTextStyle`, and `titleTextStyle`.
- `AppBar` now resolves `titleSpacing` by Flutter-like precedence (`widget` -> `theme appBarTheme` -> `16`) and applies title/toolbar text styles by precedence (`widget` -> `theme appBarTheme` -> framework defaults using app-bar foreground color).
- Added `MaterialTextTheme.TitleLarge` and switched default app-bar title fallback from hardcoded style constants to token-based `titleLarge` resolution with foreground-color override.
- `AppBar` toolbar and title composition now uses nested `DefaultTextStyle` wrappers so custom title/action widgets inherit app-bar text defaults rather than relying only on hardcoded title text parameters.
- Expanded `MaterialScaffoldTests` with precedence coverage for `titleSpacing`, `titleTextStyle`, and `toolbarTextStyle` (theme defaults and widget override behavior).
- Expanded `AppBarThemeData` with `backgroundColor`, `foregroundColor`, `iconTheme`, `actionsIconTheme`, `leadingWidth`, `toolbarHeight`, and `actionsPadding`, and aligned `AppBar` fallback precedence for these fields to Flutter-like order (`widget -> theme appBarTheme -> theme/default`) with argument guards for non-finite/non-positive themed leading width and toolbar height.
- Expanded `MaterialScaffoldTests` with focused precedence coverage for app-bar background/foreground color resolution, icon-theme resolution for leading/actions slots, leading-width and toolbar-height fallback/override behavior, actions-padding fallback/override behavior, plus regression coverage for non-positive themed leading-width/toolbar-height failure.
- Expanded app-bar icon/theme test hardening for fallback-chain edges and mixed actions context: actions now have explicit regression coverage for fallback to widget `iconTheme` when `actionsIconTheme` is unset, foreground-color fallback when icon-theme color is unset, and simultaneous inheritance of `toolbarTextStyle` plus `actionsIconTheme` in the same actions subtree.
- Added app-bar leading-width runtime parity probe route/page in both C# and Dart samples (`AppBar leadingWidth theme`) with controls for theme leading-width and widget override values plus themed/default app-bar preview comparison.
- Added app-bar actions-padding runtime parity probe route/page in both C# and Dart samples (`AppBar actionsPadding theme`) with controls for theme actions-padding and widget override values plus themed/default app-bar preview comparison.
- Added app-bar icon-theme runtime parity probe route/page in both C# and Dart samples (`AppBar icon themes`) with controls for foreground, theme/widget icon-theme overrides, and leading/actions context probes for precedence verification.
- Added app-bar text-style runtime parity probe route/page in both C# and Dart samples (`AppBar text styles`) with controls for foreground and theme/widget `titleTextStyle`/`toolbarTextStyle` overrides plus expected-style summary and themed/default preview comparison.
- Added first Material control set: `TextButton`, `ElevatedButton`, and `OutlinedButton` in `src/Flutter.Material` with inherited-theme defaults and disabled-state styling behavior.
- Added Material buttons demo route/page in both C# and Dart sample galleries for parity/runtime validation.
- Added regression coverage for Material button default color resolution and disabled visuals in `src/Flutter.Tests/MaterialButtonsTests.cs`.
- Extended Material control set with `FilledButton` plus `FilledButton.Tonal(...)` in `src/Flutter.Material`, including theme/style composition (`default -> theme -> widget -> legacy`) and `StyleFrom(...)` support.
- Added Filled-button theming surface in `Flutter.Material`: `ThemeData.SecondaryContainerColor`, `ThemeData.OnSecondaryContainerColor`, `ThemeData.FilledButtonStyle`, and `FilledButtonThemeData`/`FilledButtonTheme`.
- Expanded Material button regression coverage for Filled variants in `src/Flutter.Tests/MaterialButtonsTests.cs` (default filled/tonal token mapping, disabled-state tones, and theme precedence for `ThemeData.FilledButtonTheme` plus local `FilledButtonTheme` overrides).
- Extended Material buttons demo parity route/page in both C# and Dart samples with `FilledButton` and tonal variant runtime probes (enabled/disabled flow, tap counters, and custom color overrides).
- Added initial Material button interaction polish: pointer-pressed visuals, focus visuals, and keyboard activation (`Enter`/`Return`/`Space`) through `Focus` integration in `MaterialButtonCore`.
- Sample gallery shell buttons (menu entries and demo-page back action) now use Material button controls on both C# and Dart samples; Material-buttons demo control-strip actions now also use Material buttons instead of `CounterTapButton`.
- Added core framework support for stateful widgets implemented in external assemblies (`State.StateWidget` protected accessor) to keep `src/Flutter.Material` decoupled while preserving stateful widget patterns.
- Applied strict parity follow-up for button defaults/state layers from Flutter Dart source: `TextButton`/`ElevatedButton`/`OutlinedButton` now enforce baseline minimum size `64x40`, use stadium-like default radius, and use normalized state-layer overlay (`pressed/focused`) instead of custom focus-border widening heuristics.
- Continued strict parity follow-up for button theming tokens/defaults: `ThemeData` now exposes `onSurfaceColor`, `outlineColor`, and `surfaceContainerLowColor`; `ElevatedButton` defaults now use surface-container/primary color pairing with on-surface disabled tones; `OutlinedButton` default border now resolves from outline token while foreground remains primary.
- Added hover interaction baseline for Material buttons: framework pointer stack now dispatches enter/exit transitions (`PointerEnterEvent`/`PointerExitEvent` via `GestureBinding` hover-hit tracking), and `MaterialButtonCore` applies hover state-layer opacity (`0.08`) in addition to pressed/focused overlays.
- Fixed pointer-focus visual parity for Material buttons: pointer clicks no longer leave persistent focus tint after release (`PointerUp`), while keyboard activation still enables focus overlay behavior.
- Improved Material ripple visibility parity on wider buttons by delaying splash alpha fade until the tail phase of expansion, plus added regression coverage that `RenderInkSplash` matches full tight button bounds.
- Fixed clip-layer resize invalidation for rounded/rect clips used by button ripple paths (`RenderClipRRect`/`RenderClipRect`): implicit size-based clip bounds now refresh on layout size changes, preventing stale ripple zones after viewport resize.
- Added initial state-aware `ButtonStyle` layer for Material buttons (`MaterialState`, `MaterialStateProperty<T>`, `ButtonStyle`) and moved button visual resolution in `MaterialButtonCore` to style-driven state resolution while retaining legacy constructor-parameter compatibility.
- Extended button-style ergonomics with `StyleFrom(...)` builders on `TextButton`/`ElevatedButton`/`OutlinedButton` and added regression coverage for disabled color overrides, text-style propagation, and constructor-parameter precedence over style-specified foreground.
- Aligned Material button overlay conflict priority with Flutter Dart defaults (`pressed > hovered > focused`) and added regression coverage for combined-state conflicts (`pressed+hovered`, `pressed+focused`, `focused+hovered`).
- Continued `StyleFrom(...)` parity: `foregroundColor` now derives default overlay/splash state colors when explicit values are absent, explicit `overlayColor` now uses Flutter-like state opacities (`0.10/0.08/0.10`) plus splash fallback semantics (including transparent highlight/splash suppression), splash color is now captured at activation to match InkWell's non-shifting ripple tint during release/hover transitions, overlay tint application is now gated to interactive states only (no idle tint for `MaterialStateProperty.All(...)` overlays), `ButtonStyle.Merge(...)` is aligned with Flutter semantics (fills only null fields), and button style layering now uses internal per-state fallback composition to preserve default disabled tokens when higher-priority resolvers return null.
- Expanded resolver-null parity coverage for `ButtonStyle` on non-text buttons: `ElevatedButton` foreground/background and `OutlinedButton` foreground/side now have explicit regression checks that per-state `null` from high-priority resolvers falls back to default enabled/disabled tokens.
- Added theme-level button style overrides in `ThemeData` (`textButtonStyle`, `elevatedButtonStyle`, `outlinedButtonStyle`) and wired style composition order to `default -> theme -> widget -> legacy`, with regression coverage for precedence and theme-level state fallback.
- Added local inherited button-theme wrappers (`TextButtonTheme`/`ElevatedButtonTheme`/`OutlinedButtonTheme`) with `*ThemeData` and switched button theme-style lookup to subtree-aware `*ButtonTheme.Of(context).Style`, matching Flutter per-subtree theme override semantics (including local null-style clearing of global `ThemeData` button styles).
- Aligned `ThemeData` with Flutter button-theme shape by adding top-level `TextButtonTheme`/`ElevatedButtonTheme`/`OutlinedButtonTheme` (`*ThemeData`) and routing inherited fallback through these properties; explicit theme-data objects now override legacy `*ButtonStyle` fields when both are set, while legacy style-only configuration remains supported for backward compatibility.
- Expanded Material button size-constraint parity: `ButtonStyle` now includes `fixedSize` and `maximumSize` state properties, `StyleFrom(...)` builders accept these values, and `MaterialButtonCore` now computes effective constraints using Flutter-like ordering (`minimumSize` + `maximumSize`, then finite-axis tightening from `fixedSize`).
- Aligned Material button `minimumSize` validation with Flutter constraint semantics: zero width/height is now accepted (negative values remain rejected), enabling style-level min-size reset scenarios.
- Added `ButtonStyle` alignment parity for Material buttons: `ButtonStyle.Alignment` is now supported (including `StyleFrom(...)` and style-layer composition), and button child alignment now resolves from style instead of always being hardcoded to center.
- Aligned `ButtonStyle.textStyle` with Flutter state-property semantics: text style is now resolved as a state-aware property with per-state layer fallback (`default/theme/widget/legacy`) instead of a single static style value.
- Refined keyboard-activation parity for Material buttons: keyboard-triggered tap now sets a transient pressed state layer (`~100ms`) before returning to focus-only visual state, matching Flutter `InkWell.activateOnIntent` pressed-highlight behavior instead of focus-only tint during activation.
- Refined keyboard shortcut filtering for Material buttons: activation now includes `NumPadEnter` and ignores modified activation chords (`Ctrl/Alt/Meta/Shift + Space/Enter`) to align with Flutter `SingleActivator` semantics.
- Added Flutter-like focus-control API parity for Material buttons: `TextButton`, `ElevatedButton`, `OutlinedButton`, and `FilledButton` now expose `focusNode` and `autofocus`, and `MaterialButtonCore` now supports owned vs external focus-node lifecycle without disposing externally provided nodes.
- Expanded `MaterialButtonsTests` with focused focus-control coverage for external focus-node overlay updates and autofocus behavior (mount path and `false -> true` rebuild transition).
- Added host keyboard release dispatch baseline: `FlutterHost` now forwards `OnKeyUp` into framework `FocusManager` so focused widgets receive both key-down and key-up events (required for complete keyboard interaction parity on Material controls and editable widgets).
- Added core environment-insets primitives: framework now includes `MediaQueryData`, inherited `MediaQuery`, and `SafeArea` with Flutter-like `removePadding` composition semantics and `maintainBottomViewPadding` behavior.
- `WidgetHost` now injects ambient root `MediaQuery` data so framework widgets can read window metrics/insets without app-level bootstrap wrappers.
- `FlutterHost` now derives `MediaQuery` insets from Avalonia host features (`InsetsManager.SafeAreaPadding` + `InputPane.OccludedRect`) and sets `DisplayEdgeToEdgePreference=true` when available (legacy Android non-edge-to-edge path intentionally out of scope).
- Added focused regression coverage in `SafeAreaTests` for `SafeArea`/`MediaQueryData` parity behavior and root ambient `MediaQuery` availability.
- Added ink/ripple baseline for Material buttons with rounded clipping parity: framework now includes animated radial splash paint support (`RenderInkSplash` + `InkSplash`), rounded clip primitives (`ClipRRect` widget/render/layer + `PaintingContext.PushClipRRect`), and `MaterialButtonCore` triggers splash animation from pointer origin (keyboard fallback: center origin) while clipping splash by button border radius.
- Aligned framework `AppBar` toolbar-edge geometry with Flutter defaults: removed implicit outer horizontal toolbar padding (`0` default instead of framework-only `16`) and removed hardcoded actions-row inter-item spacing so actions rely on their own widget-level sizing/padding.
- Added focused `MaterialScaffoldTests` regression coverage for app-bar geometry parity: default zero outer toolbar padding and zero extra actions-row spacing.
- Aligned framework `AppBar` status-bar inset behavior with Flutter `primary` defaults: `AppBar` now exposes `primary` (`true` by default) and applies top safe-area padding from ambient `MediaQuery` when available (`SafeArea(bottom: false)`), with focused `MaterialScaffoldTests` coverage for `primary=true` application and `primary=false` opt-out.
- Added framework system-bar styling bridge for Material app shells: `SystemChrome` + `SystemUiOverlayStyle` now allow runtime status/navigation bar styling from C# code, `AppBar` now resolves `systemOverlayStyle` by Flutter-like precedence (`widget -> theme appBarTheme -> default`) and pushes it through `FlutterHost`, Android host theme defaults are now transparent/non-gray with light-icon baseline and API 31+ contrast overrides, and `FlutterHost` now switches edge-to-edge mode adaptively (transparent bars -> edge-to-edge, opaque bars -> non-edge-to-edge) so Android API 33/34 system-bar colors are applied reliably.
- Aligned framework `AppBar` default string-title behavior with Flutter: `titleText` now renders as single-line, non-wrapping text with ellipsis overflow (`softWrap: false`, `maxLines: 1`, `overflow: ellipsis`), with focused `MaterialScaffoldTests` coverage.
- Added widget-level `mainAxisSize` wiring for `Flex`/`Row`/`Column` and aligned `AppBar` actions-row layout to Flutter-style shrink wrapping (`mainAxisSize: min`) with focused `MaterialScaffoldTests` assertion coverage.
- Aligned app-bar leading-slot geometry with Flutter toolbar layout: leading slot is now constrained by both resolved `leadingWidth` and resolved `toolbarHeight`, with focused `MaterialScaffoldTests` width+height constraint coverage.
- Aligned empty-string app-bar title parity: `titleText: ""` now renders as `Text("")` (instead of collapsing as missing title), while `titleText: null` still maps to an absent default title; covered by focused `MaterialScaffoldTests`.
- Added `ThemeData.UseMaterial3` (default `true`) to `Flutter.Material` and wired app-bar actions-row cross-axis behavior to Flutter Material mode semantics (`useMaterial3: true` -> `CrossAxisAlignment.Center`, `useMaterial3: false` -> `CrossAxisAlignment.Stretch`) with focused `MaterialScaffoldTests` coverage for both paths.
- Aligned app-bar default toolbar-height resolution with Flutter `AppBar` precedence: unresolved toolbar height now remains `56` (`kToolbarHeight`) for both M3 and M2 unless overridden by widget/theme appBarTheme, with focused `MaterialScaffoldTests` coverage.
- Aligned app-bar default background/foreground fallback to Flutter Material mode semantics: unresolved colors now resolve to `CanvasColor`/`OnSurfaceColor` in M3 and `PrimaryColor`/`OnPrimaryColor` in M2, with focused `MaterialScaffoldTests` coverage for both mode paths.
- Added `ThemeData.Brightness` (`Light` default) and aligned M2 dark-path app-bar defaults with Flutter behavior: when `UseMaterial3` is false and brightness is dark, unresolved app-bar defaults now resolve to `CanvasColor`/`OnSurfaceColor`.
- Added `ThemeData.OnSurfaceVariantColor` and aligned app-bar actions icon default fallback to Flutter Material mode semantics: unresolved actions icon color now resolves to `OnSurfaceVariantColor` in M3 (with size fallback `24`) and keeps existing foreground fallback path in M2, while explicit `actionsIconTheme`/`iconTheme` overrides retain precedence.
- Aligned app-bar leading `iconTheme` default fallback to Flutter Material mode semantics: when no explicit leading icon theme is provided, M3 now defaults to foreground + `size: 24`, while M2 keeps foreground fallback without forcing size.
- Continued Material button/theme parity hardening from Flutter M3 defaults:
  - `ThemeData.Light` default tokens now follow Flutter M3 light scheme for key surfaces/foregrounds used by sample controls (`primary`, `onSurface`, `secondaryContainer`, `onSecondaryContainer`, `surface/canvas`),
  - `MaterialTextTheme` now includes `LabelLarge` and updated `TitleLarge` defaults to 2021 token metrics (`22/1.27/0.0`), while default body font resolution now follows platform-specific family fallback (Android `Roboto`, iOS/macOS system font, Windows `Segoe UI`, Linux `Noto Sans`),
  - `TextButton`/`ElevatedButton`/`OutlinedButton`/`FilledButton` defaults now resolve `ButtonStyle.textStyle` from `ThemeData.TextTheme.LabelLarge`,
  - `ElevatedButton`/`OutlinedButton`/`FilledButton` default padding now matches Flutter M3 generated defaults (`horizontal: 24`, `vertical: 0`),
  - `OutlinedButton` focused border now uses primary color token (matching Flutter focused-state side behavior),
  - `MaterialButtonCore` now preserves foreground-color precedence over `ButtonStyle.textStyle.color` (Flutter `ButtonStyleButton` semantics), with added regression coverage for default label typography and foreground-vs-textStyle color precedence,
  - tap-target parity hardening now mirrors Flutter `_InputPadding` behavior more closely: child layout uses incoming constraints (preserving wide-button material bounds) and padded-area hit-tests redirect to child center.
- Continued Material elevated-depth parity hardening:
  - framework `BoxDecoration`/`RenderDecoratedBox` paint path now supports optional `BoxShadows`,
  - `ButtonStyle` now includes state-aware `ShadowColor` + `Elevation`,
  - `ElevatedButton` default states now resolve elevation (`disabled=0`, `pressed=1`, `hovered=3`, `focused=1`, enabled baseline `1`) and map it to rendered shadows in `MaterialButtonCore`,
  - `ElevatedButton.styleFrom(elevation)` now follows Flutter state deltas (`disabled=0`, `pressed=+6`, `hovered/focused=+2`, base for default),
  - theme now exposes `ThemeData.ShadowColor` (default black) used as default elevated shadow token source,
  - Android font-family fallback uses explicit `Roboto` to match Flutter Material typography resolution on Android,
  - `MaterialButtonCore` baseline label style merge now starts from `ThemeData.TextTheme.LabelLarge` (no hardcoded fallback metrics), and current default `LabelLarge` weight is `Medium` (Flutter token parity).
- Continued Material button icon-theme parity hardening:
  - `ButtonStyle` now includes state-aware `IconColor` and `IconSize` properties with merge/composition support.
  - `TextButton`/`ElevatedButton`/`OutlinedButton`/`FilledButton` default styles now expose Flutter-like icon defaults (`iconSize: 18` plus state-aware icon color tied to foreground/disabled tokens), and all `styleFrom(...)` builders now accept `iconColor`, `disabledIconColor`, and `iconSize`.
  - `MaterialButtonCore` now wraps button content with `IconTheme` resolved from composed button style layers (`iconColor` with fallback to `foregroundColor`, plus `iconSize`), so icon-bearing button children inherit style parity defaults.
  - Added focused `MaterialButtonsTests` coverage for icon-theme defaults and `styleFrom(...)` icon overrides in enabled and disabled states.
- Continued Material button tap-target-size parity hardening:
  - `ButtonStyle` now includes `TapTargetSize` and propagates it through style merge/composition layers.
  - `ThemeData` now exposes `MaterialTapTargetSize` (`Padded` default), and Material button default styles now inherit this ambient theme policy.
  - `TextButton`/`ElevatedButton`/`OutlinedButton`/`FilledButton` `styleFrom(...)` builders now accept `tapTargetSize` overrides.
  - `MaterialButtonCore` now resolves tap-target wrapper size from the composed style (`Padded` -> `48x48`, `ShrinkWrap` -> `0x0`) before applying `ButtonTapTargetPadding`, matching Flutter `_InputPadding` mode behavior.
  - Added focused `MaterialButtonsTests` coverage for theme-default tap-target mode, theme shrink-wrap behavior, and `styleFrom` precedence over theme.
- Continued Material button surface-tint parity hardening:
  - `ButtonStyle` now includes state-aware `SurfaceTintColor`, and style composition/merge now includes this layer.
  - all button `styleFrom(...)` builders now accept `surfaceTintColor`.
  - `MaterialButtonCore` now applies Flutter-like elevation-dependent surface-tint overlay to resolved background color using the M3 opacity table (`0/1/3/6/8/12` elevations).
  - Added focused `MaterialButtonsTests` coverage for style-level and theme-level `surfaceTintColor` tinting on elevated buttons.
- Continued Material button `styleFrom` API parity expansion for non-elevated types:
  - `TextButton`/`OutlinedButton`/`FilledButton` `styleFrom(...)` builders now also accept `shadowColor` and `elevation`.
  - explicit `styleFrom(shadowColor: ..., elevation: ...)` on these button types now flows through composed `ButtonStyle` into existing `MaterialButtonCore` shadow/elevation rendering path.
  - Added focused `MaterialButtonsTests` coverage for shadow rendering on Text/Outlined/Filled buttons via style overrides.
- Continued Material shadow fallback parity hardening for non-elevated `styleFrom(elevation)` paths:
  - `MaterialButtonCore` now falls back to `ThemeData.ShadowColor` when effective elevation is positive and no explicit shadow color resolves from style layers.
  - Added focused `MaterialButtonsTests` coverage for Text/Outlined/Filled style-level elevation overrides without explicit `shadowColor`.
- Continued `styleFrom` disabled-color mapping parity hardening for text/outlined buttons:
  - `TextButton.styleFrom(backgroundColor: x)` now keeps `x` for disabled state when `disabledBackgroundColor` is not provided.
  - `TextButton.styleFrom(iconColor: x)` now keeps `x` for disabled state when `disabledIconColor` is not provided.
  - `OutlinedButton.styleFrom(backgroundColor: x)` now keeps `x` for disabled state when `disabledBackgroundColor` is not provided.
  - Added focused `MaterialButtonsTests` coverage for these disabled-state mapping scenarios.
- Continued surface-tint Material-mode parity hardening:
  - `MaterialButtonCore` now applies surface tint only when `ThemeData.UseMaterial3` is `true`, matching Flutter `Material` tint behavior.
  - Added focused `MaterialButtonsTests` coverage for `UseMaterial3=false` to verify style/theme surface tint overrides do not tint elevated button backgrounds.

Initial scope:

- Introduce framework-level theming primitives (`ThemeData`, `Theme`, baseline color/text style propagation).
- Introduce shell/layout primitives for Material app structure (`Scaffold`, `AppBar`, and supporting slots).
- Introduce first Material control set (`TextButton`, `ElevatedButton`, `OutlinedButton`) on top of framework render/widget layers.
- Keep architecture boundaries explicit: behavior in framework libraries (`src/Flutter`, `src/Flutter.Material`), host integration in sample hosts only.

Exit criteria:

- Material theming is available through inherited framework state and can drive common control defaults.
- Material shell primitives are sufficient to host route pages without custom sample-only wrappers.
- Initial Material control set supports core states and API shape needed for straightforward Dart-to-C# rewrites.
- Regression coverage exists for widget-to-render wiring and theming resolution behavior.

### M5. Cross-Host Sample Parity and Stability

Status: `planned`

Scheduling note (2026-03-12):

- Moved after Material rewrite as a final stabilization milestone. Current blockers are local toolchain/environment alignment (Android API 36 SDK platform missing; iOS workload/Xcode version mismatch).

Exit criteria:

- Desktop, browser, Android, and iOS sample hosts build successfully from the solution.
- Framework-driven app flow remains identical across hosts.
- `src/Sample/Flutter.Net` and `dart_sample` stay in feature/route/module parity.

## Backlog Candidates (After M1-M5)

- Text editing/IME primitives and richer text input workflows.
- Overlay/portal-like primitives and advanced route transitions.
- Performance instrumentation and frame diagnostics tooling.
- Expanded documentation for migration recipes from Flutter (Dart) widgets to C#.

## Update Protocol (For Humans and AI Agents)

- Always update this file when milestone status changes (`done`, `in_progress`, `planned`, `blocked`).
- Always record shipped outcomes in `CHANGELOG.md`.
- For Dart-to-C# control/widget ports, follow mandatory parity-first workflow in `docs/ai/PORTING_MODE.md` (strict `1:1` default; documented divergences only).
- For every meaningful feature change, update both:
  - semantic status (this document),
  - historical record (`CHANGELOG.md`).
- Keep architecture boundaries explicit: framework behavior in framework libraries (`src/Flutter`, `src/Flutter.Material`), host adaptation only in sample hosts.
