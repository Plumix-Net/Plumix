# Changelog

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

All notable framework changes are documented in this file.

This project follows the spirit of [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Planned

- Continue `M4` Material library rewrite with advanced Material control refinements (hover/ripple/style-system expansion) after shipping baseline theming + shell + first button set plus initial interaction polish.
- Run cross-host parity/stability validation in final `M5` phase after Material rewrite sequencing completes.
- Improve architecture docs and migration guidance for Dart-to-C# rewrites.

### Changed

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
