# Changelog

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
