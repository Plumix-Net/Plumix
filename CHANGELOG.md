# Changelog

All notable framework changes are documented in this file.

This project follows the spirit of [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Planned

- Continue `M4` Material library rewrite with advanced Material control refinements (hover/ripple/style-system expansion) after shipping baseline theming + shell + first button set plus initial interaction polish.
- Run cross-host parity/stability validation in final `M5` phase after Material rewrite sequencing completes.
- Improve architecture docs and migration guidance for Dart-to-C# rewrites.

### Changed

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
  - hero flight shuttle composition now supports destination-priority `Hero.flightShuttleBuilder` (with source fallback and destination-child default) in `src/Flutter/Widgets/Hero.cs` and `src/Flutter/Widgets/Navigation.cs`.
- Added focused hero regression coverage:
  - new `src/Flutter.Tests/HeroNavigatorTests.cs` verifies shared-tag push/pop hero transitions keep both routes during flight and settle to a single destination route after completion;
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now also verifies destination `Hero.createRectTween` precedence and custom tween evaluation during flight;
  - `src/Flutter.Tests/HeroNavigatorTests.cs` now verifies destination `Hero.flightShuttleBuilder` precedence (over source) and source-builder fallback when destination builder is absent;
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs` now verifies FAB composition is wrapped by `Hero` when `heroTag` is set.
- Synced tracking docs for this parity pass:
  - `docs/FRAMEWORK_PLAN.md`,
  - `docs/ai/MODULE_INDEX.md`,
  - `docs/ai/TEST_MATRIX.md`,
  - `docs/ai/material-2026-04-12-fab-hero-transition-closeout.md`,
  - `docs/ai/material-2026-04-12-hero-create-rect-tween.md`,
  - `docs/ai/material-2026-04-13-hero-flight-shuttle-builder.md`.
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

## [2026-04-11] - M4 BottomNavigationBar semantics + shifting/tooltip parity hardening

### Changed

- Hardened framework `BottomNavigationBar` accessibility semantics in `src/Flutter.Material/BottomNavigationBar.cs`:
  - each tile is now wrapped with explicit semantics flags (`IsButton`, `IsEnabled` when tappable, and `IsSelected` for the active tab) and tap-action routing;
  - tabs now expose index-label semantics (`Tab {index} of {count}`), and hidden unselected labels now keep label semantics through fallback wrappers when visual labels are suppressed.
- Completed stateful shifting behavior in `src/Flutter.Material/BottomNavigationBar.cs`:
  - animated selected/unselected icon color+size transitions,
  - animated label visibility transitions,
  - animated tile-width (`Expanded.flex`) transitions in shifting mode,
  - radial selected-item background flood transition for shifting color changes.
- Added framework Material `Tooltip` primitive in `src/Flutter.Material/Tooltip.cs` and wired `BottomNavigationBarItem.tooltip` wrapping for bottom-nav tiles.
- Added focused semantics regression coverage in `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`:
  - verifies button/enabled/selected/tap semantics and index-label exposure,
  - verifies disabled bars omit enabled/tap semantics.
- Added focused behavior regression coverage in `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`:
  - verifies tooltip show/hide behavior on hover enter/exit transitions,
  - verifies shifting selection changes animate tile widths between source/target tabs.
- Updated tracking docs for this parity pass (`docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`, and `docs/ai/material-2026-04-11-bottom-navigation-bar-semantics-parity.md`).
- Remaining documented divergence:
  - semantics index labels currently use fixed English (`"Tab {i} of {n}"`) until Material localization primitives are added.

## [2026-04-11] - M4 BottomNavigationBar parity expansion

### Changed

- Expanded framework `BottomNavigationBar` parity surface in `src/Flutter.Material/BottomNavigationBar.cs`:
  - added `BottomNavigationBarType` (`fixed`/`shifting`) with Flutter-like effective-type precedence (`widget -> theme -> item-count default`);
  - added shifting background resolution from selected-item `BottomNavigationBarItem.backgroundColor`;
  - added nullable label-visibility flags (`showSelectedLabels` / `showUnselectedLabels`) with type-aware defaults (`fixed`: unselected labels shown, `shifting`: hidden);
  - added label-style + icon-theme parity surface (`selected/unselectedLabelStyle`, `selected/unselectedIconTheme`) with precedence and validation guards (icon themes must be provided as a pair);
  - added optional `elevation` handling and expanded item API with `key` and `tooltip`.
- Added dedicated bottom-navigation theming primitives:
  - new `BottomNavigationBarThemeData` and inherited `BottomNavigationBarTheme` in `src/Flutter.Material/BottomNavigationBarTheme.cs`;
  - new `ThemeData.BottomNavigationBarTheme` integration in `src/Flutter.Material/ThemeData.cs`.
- Expanded focused regression coverage in `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs` for:
  - theme-default precedence,
  - widget-over-theme overrides,
  - auto/type-driven shifting defaults (background + label visibility),
  - label-style color precedence,
  - icon-theme pair guard behavior.
- Updated tracking docs for this pass (`docs/FRAMEWORK_PLAN.md`, `docs/ai/MODULE_INDEX.md`, `docs/ai/TEST_MATRIX.md`, and `docs/ai/material-2026-04-11-bottom-navigation-bar-parity-closeout.md`).
- Remaining documented divergence:
  - full Flutter shifting animation choreography (radial splash + animated tile flex/label transitions) and dedicated tooltip/semantics wrappers are still pending due missing framework primitives.

## [2026-04-10] - M4 BottomNavigationBar baseline + tabbed sample menu

### Changed

- Added framework Material `BottomNavigationBar` + `BottomNavigationBarItem` in `src/Flutter.Material/BottomNavigationBar.cs` with fixed-layout baseline behavior for current scope:
  - item-count/index/font-size guards,
  - default theme color resolution (`canvas` background, `primary` selected item, `onSurfaceVariant` unselected item for M3),
  - selected `activeIcon` rendering path and tap callback index dispatch.
- Added focused regression coverage in `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs` for:
  - constructor guards,
  - theme-default color mapping,
  - active-icon selection,
  - pointer tap callback index behavior.
- Reorganized sample gallery menu structure around bottom tabs in both C# and Dart samples:
  - `Material` tab: Material-focused demos,
  - `Cupertino` tab: adaptive Cupertino behavior demos (`Checkbox`, `Switch`, `Radio`),
  - `General` tab: core framework/control demos.
  - Updated files: `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`.
- Updated tracking docs for this pass (`docs/FRAMEWORK_PLAN.md`, `docs/ai/MODULE_INDEX.md`, `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md`, and `docs/ai/material-2026-04-10-bottom-navigation-bar-baseline-parity.md`).
- Remaining documented divergence:
  - current framework `BottomNavigationBar` scope is fixed-layout baseline only; shifting mode animation/background behavior, dedicated bottom-navigation theming objects, and full label-visibility/semantics parity remain follow-up work.

## [2026-04-10] - M4 FloatingActionButton parity baseline

### Changed

- Added framework Material `FloatingActionButton` in `src/Flutter.Material/FloatingActionButton.cs` with Flutter-like baseline behavior for current framework scope:
  - `regular`, `small`, `large`, and `extended` variants;
  - variant-aware defaults for constraints/shape/icon-size (`56/40/96/extended` sizing paths and M3 rounded-corner defaults);
  - state-aware elevation mapping (`default`/`focused`/`hovered`/`pressed`/`disabled`) through shared `MaterialButtonCore`;
  - extended FAB composition with icon+label spacing/padding and `isExtended` open/collapsed behavior.
- Added dedicated FAB theming primitives:
  - `FloatingActionButtonThemeData` + inherited `FloatingActionButtonTheme` (`src/Flutter.Material/FloatingActionButtonTheme.cs`);
  - new `ThemeData.FloatingActionButtonTheme` integration in `src/Flutter.Material/ThemeData.cs`.
- Expanded `ThemeData` token surface with `PrimaryContainerColor` and `OnPrimaryContainerColor` to support M3 FAB default token mapping.
- Added focused FAB regression coverage in `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`:
  - variant size/shape defaults,
  - M3 foreground/background token defaults,
  - extended directional padding behavior,
  - theme-vs-widget precedence for colors/constraints,
  - hover/pressed/disabled elevation-state transitions.
- Added sample parity route/page in both C# and Dart samples:
  - `src/Sample/Flutter.Net/FloatingActionButtonDemoPage.cs`
  - `dart_sample/lib/floating_action_button_demo_page.dart`
  - route/menu wiring in `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, and `dart_sample/lib/sample_routes.dart`.
- Follow-up layout fix for FAB demo route:
  - wrapped demo content with `SingleChildScrollView`, replaced fragile `Row + Expanded` FAB probe layout with stacked probe cards, and bounded FAB slots via fixed-height `SizedBox` in both `src/Sample/Flutter.Net/FloatingActionButtonDemoPage.cs` and `dart_sample/lib/floating_action_button_demo_page.dart` to remove bottom overflow and keep FAB probes visible inside the shared `SampleDemoPage` viewport.
- Updated tracking artifacts for this parity pass (`docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md`, and `docs/ai/material-2026-04-10-floating-action-button-baseline-parity.md`).
- Remaining documented divergence:
  - `heroTag`/`Tooltip` wrappers, cursor/feedback toggles, and clip-behavior parity are still out of current framework scope due missing primitives.

## [2026-04-05] - M4 Radio adaptive Cupertino parity close-out

### Changed

- Closed the documented adaptive-radio gap by adding `Radio<T>.Adaptive(...)` in `src/Flutter.Material/Radio.cs` with Flutter-like platform split (`ThemeData.Platform`: `iOS`/`macOS` -> Cupertino path; others -> existing Material path).
- Added framework `CupertinoRadio<T>` in `src/Flutter.Cupertino/CupertinoRadio.cs` with Cupertino-like defaults and interactions for current framework scope:
  - `18x18` visual geometry,
  - brightness-aware outer/inner/border color defaults,
  - dark-mode gradient fill branch,
  - pressed and focus visuals,
  - keyboard activation and `toggleable` selected->null behavior.
- Added adaptive API parity surface in `Radio<T>` for Cupertino checkmark mode (`useCupertinoCheckmarkStyle`) and wired it to adaptive Cupertino indicator rendering.
- Expanded `src/Flutter.Tests/MaterialRadioTests.cs` with focused adaptive coverage:
  - iOS adaptive default visuals,
  - adaptive `fillColor` ignore behavior,
  - checkmark-style indicator branch,
  - macOS adaptive visual width (`18x18`).
- Extended C#/Dart sample parity runtime probe in `RadioDemoPage`:
  - added adaptive-platform toggle cycle (`iOS`/`macOS`/`Android`) and adaptive checkmark-style toggle in both sample implementations;
  - added dedicated adaptive probe rows (`Radio.adaptive`) to validate Cupertino path behavior and platform fallback behavior at runtime without route-structure changes.
- Updated tracking artifacts for this close-out step (`docs/ai/material-2026-04-05-radio-adaptive-cupertino-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md`).
- Remaining documented divergence:
  - deeper Cupertino radio fidelity items are still out of current framework scope (`haptics`, accessibility labels, advanced motion nuances).

## [2026-04-05] - M4 Radio parity baseline

### Changed

- Added framework Material `Radio<T>` in `src/Flutter.Material/Radio.cs` with Flutter-like baseline behavior for current framework scope:
  - controlled group-selection API (`value`, `groupValue`, `onChanged`) plus `toggleable` deselect behavior (`selected -> null`),
  - mode-aware M3/M2 default fill/overlay resolution for selected/unselected/disabled states,
  - state-aware visual composition on top of `MaterialButtonCore` (focus/hover/pressed overlays, keyboard activation, and tap-target policy via `ThemeData.MaterialTapTargetSize`),
  - customizable visual surface (`activeColor`, `fillColor`, `overlayColor`, `backgroundColor`, `side`, `innerRadius`, `splashRadius`).
- Added dedicated radio theming primitives:
  - new `RadioThemeData` and inherited `RadioTheme` (`src/Flutter.Material/RadioTheme.cs`),
  - new `ThemeData.RadioTheme` integration in `src/Flutter.Material/ThemeData.cs`.
- Added focused radio regression coverage in `src/Flutter.Tests/MaterialRadioTests.cs`:
  - M3 selected/unselected/disabled defaults,
  - widget-vs-theme precedence (`fillColor`),
  - keyboard activation and toggleable null-transition behavior,
  - tap-target behavior (`padded` vs `shrinkWrap`).
- Added sample parity route/page in both C# and Dart samples:
  - `src/Sample/Flutter.Net/RadioDemoPage.cs`
  - `dart_sample/lib/radio_demo_page.dart`
  - route/menu wiring in `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, and `dart_sample/lib/sample_routes.dart`.
- Added iteration tracking artifacts for this parity pass (`docs/ai/material-2026-04-05-radio-baseline-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md`).
- Remaining documented divergence:
  - `Radio.adaptive` Cupertino path is not yet implemented in current framework scope.

## [2026-04-05] - M4 Switch adaptive Cupertino interaction close-out

### Changed

- Closed the documented adaptive-switch interaction gap in `src/Flutter.Material/Switch.cs`:
  - adaptive iOS/macOS path no longer uses `MaterialButtonCore`; it now uses dedicated Cupertino-style composition (`Focus` + pointer listeners + drag/tap gesture handling),
  - adaptive drag choreography now follows Flutter Cupertino thresholds (`commit=0.7`, `reverse=0.2`) instead of Material-style midpoint commit,
  - adaptive thumb now applies Cupertino-style pressed/drag extension (`+7`) and default thumb shadows (`0,3,8` + `0,3,1`) while keeping existing Cupertino geometry/tokens.
- Expanded adaptive switch regression coverage in `src/Flutter.Tests/MaterialSwitchTests.cs`:
  - drag below commit threshold does not toggle,
  - drag beyond commit threshold toggles on,
  - drag reverse beyond reverse threshold cancels pending toggle.
- Updated tracking docs for this close-out step (`docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`, `docs/ai/material-2026-04-05-switch-adaptive-cupertino-interaction-closeout.md`).
- Remaining documented divergence:
  - deeper Cupertino fidelity items are still out of current scope (`HapticFeedback.lightImpact`, on/off accessibility labels, image-thumb pipeline).

## [2026-04-05] - M4 Switch adaptive Cupertino parity hardening

### Changed

- Hardened `Switch.Adaptive(...)` parity on Cupertino targets in `src/Flutter.Material/Switch.cs`:
  - adaptive path now branches by `ThemeData.Platform` (`IOS`/`MacOS`) instead of always following Material defaults;
  - adaptive iOS/macOS now uses Cupertino-like geometry/defaults (`59x39` shell, `51x31` track, `28` thumb), zero fallback padding, and zero fallback splash radius;
  - adaptive `activeColor` mapping now follows Flutter semantics by platform (`iOS/macOS -> track`, other platforms -> thumb fallback when explicit track/thumb colors are absent);
  - disabled adaptive iOS/macOS now applies switch-level opacity `0.5`.
- Expanded switch regression coverage in `src/Flutter.Tests/MaterialSwitchTests.cs` for:
  - adaptive iOS `activeColor` track mapping (and non-iOS thumb mapping),
  - adaptive iOS disabled opacity behavior,
  - adaptive iOS Cupertino geometry defaults (`track 51x31`).
- Updated iteration tracking artifacts for this parity-hardening pass (`docs/ai/material-2026-04-05-switch-adaptive-cupertino-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).
- Remaining documented divergence:
  - adaptive switch now has Cupertino-like defaults in framework scope, but still uses shared `MaterialButtonCore` composition (no dedicated Cupertino painter/drag-threshold choreography yet).

## [2026-04-05] - M4 Checkbox adaptive Cupertino parity hardening

### Changed

- Hardened `Checkbox.Adaptive(...)` parity on Cupertino targets in `src/Flutter.Material/Checkbox.cs`:
  - added a dedicated `src/Flutter.Cupertino` project and framework `CupertinoCheckbox` control (`src/Flutter.Cupertino/CupertinoCheckbox.cs`);
  - adaptive path now branches by `ThemeData.Platform` (`IOS`/`MacOS`) and delegates to `CupertinoCheckbox` instead of following Material defaults/composition;
  - adaptive iOS/macOS now uses Cupertino-like checkbox defaults for visual geometry and tokens (body size `14x14`, Cupertino-like fill/check/border defaults, and mode-aware dark-path token handling);
  - Flutter-documented adaptive exclusions are now honored in framework scope for Cupertino targets (`fillColor`, `overlayColor`, `hoverColor`, `materialTapTargetSize`, `splashRadius`, and `isError` are ignored);
  - adaptive tap-target policy now follows platform split in framework scope (`IOS` uses `44x44`; `MacOS` uses `14x14`).
- Expanded checkbox regression coverage in `src/Flutter.Tests/MaterialCheckboxTests.cs` for:
  - adaptive iOS default token behavior,
  - adaptive iOS ignored-parameter behavior (`fillColor`, `materialTapTargetSize`),
  - adaptive macOS shrink-wrap hit-target behavior and `14x14` visual geometry.
- Updated project graph and iteration tracking artifacts for this parity-hardening pass (`src/Flutter.Net.sln`, `src/Flutter.Material/Flutter.Material.csproj`, `src/Flutter.Tests/Flutter.Tests.csproj`, `docs/ai/material-2026-04-05-checkbox-adaptive-cupertino-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).
- Remaining documented divergence:
  - none for adaptive checkbox painter defaults in current framework scope.

### Additional parity close-out (same iteration)

- Completed painter-level Cupertino parity for adaptive checkbox visuals:
  - `src/Flutter.Cupertino/CupertinoCheckbox.cs` now renders dark-mode gradient fill behavior matching Flutter Cupertino logic (enabled and disabled gradient opacity profiles).
  - check and dash indicators now use vector stroke glyph rendering (`0.22/0.54 -> 0.40/0.75 -> 0.78/0.25` check path and centered half-width dash) instead of font glyph text.
- Added shared rendering primitives needed for this parity pass:
  - `PaintingContext.DrawLine(...)` in `src/Flutter/Rendering/Object.PaintingContext.cs`,
  - brush-backed decoration support (`BoxDecoration.Brush`) in `src/Flutter/Rendering/Decoration.cs` + `RenderDecoratedBox`.
  - reusable stroke glyph widget/render object (`src/Flutter/Widgets/StrokeGlyph.cs`, `src/Flutter/RenderStrokeGlyph.cs`).
- Expanded adaptive checkbox tests in `src/Flutter.Tests/MaterialCheckboxTests.cs`:
  - adaptive iOS uses vector indicator path (no `RenderParagraph` checkmark text),
  - adaptive dark unchecked path uses gradient brush fill,
  - adaptive dark checked+enabled path keeps solid fill (no dark gradient branch).

## [2026-04-05] - M4 Switch parity baseline

### Changed

- Added framework Material `Switch` in `src/Flutter.Material/Switch.cs` with Flutter-like baseline behavior for current framework scope:
  - controlled `value` (`bool`) with callback-driven state updates (`onChanged`),
  - tap and horizontal-drag interaction flow with animated thumb-position transitions,
  - keyboard activation through focus path (`focusNode`/`autofocus`) and `MaterialButtonCore`,
  - mode-aware defaults for thumb/track/outline/overlay and tap-target policy wiring to `ThemeData.MaterialTapTargetSize`,
  - customizable thumb/track/outline/icon surface (`active/inactive` colors, state properties, `thumbIcon`, `overlayColor`, `padding`, `splashRadius`).
- Added dedicated switch theming primitives:
  - new `SwitchThemeData` and inherited `SwitchTheme` (`src/Flutter.Material/SwitchTheme.cs`),
  - new `ThemeData.SwitchTheme` integration in `src/Flutter.Material/ThemeData.cs`.
- Added focused switch regression coverage in `src/Flutter.Tests/MaterialSwitchTests.cs`:
  - M3 default selected/unselected/disabled visual checks,
  - widget-vs-theme precedence checks for thumb/track/outline,
  - thumb icon rendering path, tap-target behavior, and keyboard toggle activation.
- Added sample parity route/page in both C# and Dart samples:
  - `src/Sample/Flutter.Net/SwitchDemoPage.cs`
  - `dart_sample/lib/switch_demo_page.dart`
  - route wiring in `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, and `dart_sample/lib/sample_routes.dart`.
- Added iteration tracking artifacts for this parity pass (`docs/ai/material-2026-04-05-switch-baseline-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md`).
- Remaining documented divergence:
  - adaptive switch currently renders through Material path on iOS/macOS because Cupertino switch primitives are not yet implemented in framework scope.

## [2026-04-05] - M4 Checkbox parity close-out (theme + animation)

### Changed

- Expanded framework Material `Checkbox` (`src/Flutter.Material/Checkbox.cs`) from baseline parity to close-out scope:
  - converted control to stateful transition model with animated indicator state changes across `false/true/null` values (check/dash crossfade),
  - added API surface and behavior for `Checkbox.Adaptive(...)`, `isError`, `splashRadius`, and `semanticLabel` field support,
  - added Flutter-like resolution order for checkbox visuals and interaction tokens (`widget -> checkboxTheme -> defaults`) including fill/check/overlay/side/shape/tap-target/splash-radius.
- Added dedicated checkbox theme primitives:
  - new `CheckboxThemeData` and inherited `CheckboxTheme` (`src/Flutter.Material/CheckboxTheme.cs`),
  - new `ThemeData.CheckboxTheme` integration in `src/Flutter.Material/ThemeData.cs`.
- Extended Material state/theme tokens for checkbox parity:
  - added `MaterialState.Error` in `src/Flutter.Material/ButtonStyle.cs`,
  - added `ThemeData.ErrorColor` / `ThemeData.OnErrorColor` defaults for M3 checkbox error-state visuals.
- Extended shared ink/ripple plumbing to support checkbox splash-radius parity:
  - added optional splash-radius support to `InkSplash` / `RenderInkSplash` (`src/Flutter/Widgets/Basic.cs`, `src/Flutter/Rendering/Proxy.RenderBox.cs`),
  - threaded optional `splashRadius` through `MaterialButtonCore` (`src/Flutter.Material/Buttons.cs`).
- Expanded checkbox regression coverage (`src/Flutter.Tests/MaterialCheckboxTests.cs`) with:
  - checkbox-theme precedence and widget-over-theme override checks,
  - M3 error-state visual token checks,
  - adaptive constructor guard check,
  - splash-radius propagation to `RenderInkSplash`,
  - indicator transition animation behavior check.
- Remaining documented divergence:
  - adaptive checkbox currently renders through Material path on iOS/macOS because Cupertino checkbox primitives are not yet implemented in framework scope.

## [2026-04-05] - M4 Checkbox parity baseline

### Changed

- Added framework Material `Checkbox` in `src/Flutter.Material/Checkbox.cs` with Flutter-like baseline behavior for current framework scope:
  - controlled `value` (`bool?`) + `tristate` cycle,
  - mode-aware defaults for selected/unselected/disabled states,
  - focus/hover/pressed state overlays and keyboard activation through shared `MaterialButtonCore`,
  - tap-target policy wiring through `ThemeData.MaterialTapTargetSize` (`padded` vs `shrinkWrap`),
  - customizable color/border surface (`activeColor`, `fillColor`, `checkColor`, `overlayColor`, `side`, `shape`, focus/hover overrides).
- Extended Material icons set with `Icons.Check` in `src/Flutter.Material/Icons.cs` for checkbox indicator rendering.
- Added focused checkbox regression coverage in `src/Flutter.Tests/MaterialCheckboxTests.cs`:
  - constructor guards (`tristate` + null value),
  - M3 default visual states (checked/unchecked/disabled),
  - check/dash indicator rendering,
  - tap-target behavior for theme `MaterialTapTargetSize`,
  - keyboard-driven value transitions including tristate cycling.
- Added sample parity route/page in both C# and Dart samples:
  - `src/Sample/Flutter.Net/CheckboxDemoPage.cs`
  - `dart_sample/lib/checkbox_demo_page.dart`
  - route wiring in `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, and `dart_sample/lib/sample_routes.dart`.
- Added iteration tracking artifacts for this parity pass (`docs/ai/material-2026-04-05-checkbox-baseline-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md`).

## [2026-04-05] - M4 core Icon widget parity baseline

### Changed

- Added core icon primitives in framework widgets (`src/Flutter/Widgets/Icon.cs`):
  - introduced `IconData` and `Icon`,
  - wired `Icon` defaults to ambient `IconTheme` (`size`/`color`) with explicit-parameter precedence,
  - added null-icon square layout behavior and RTL mirroring for `matchTextDirection`.
- Added bundled Material icon font wiring for framework icons:
  - imported `MaterialIcons-Regular.otf` + license into `src/Flutter.Material/Assets/Fonts`,
  - enabled `AvaloniaResource` packaging for Material assets in `src/Flutter.Material/Flutter.Material.csproj`,
  - routed Material icon constants to embedded Material font family (`avares://Flutter.Material/...#Material Icons`) while keeping `Flutter.Widgets.Icon` core font resolution generic.
- Expanded Material icon constants set in `src/Flutter.Material/Icons.cs` for current parity/demo usage:
  - `arrow_back` (with `matchTextDirection=true`),
  - `menu`, `close`,
  - `add`, `info_outline`, `star`, `star_outline`
  (all code points aligned to Flutter source).
- Updated C# Material buttons demo to use real framework icons (`Icon(Icons.*)`) instead of temporary glyph probe widget (`src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`), matching existing Dart sample composition.
- Updated C#/Dart AppBar-focused runtime demos to use real icons in leading/actions slots where icon rendering is the probe target:
  - `AppBar icon themes`: `menu` + `close` + `info_outline`,
  - `AppBar leadingWidth`: `menu` leading probe and `close` action probe,
  - `AppBar actionsPadding`: `close`/`menu` action badges.
  (`src/Sample/Flutter.Net/AppBarIconThemeDemoPage.cs`, `src/Sample/Flutter.Net/AppBarLeadingWidthDemoPage.cs`, `src/Sample/Flutter.Net/AppBarActionsPaddingDemoPage.cs`, `dart_sample/lib/app_bar_icon_theme_demo_page.dart`, `dart_sample/lib/app_bar_leading_width_demo_page.dart`, `dart_sample/lib/app_bar_actions_padding_demo_page.dart`)
- Updated C#/Dart sample demo-shell back button to icon+label composition (`arrow_back` + `Back`) for visual parity with new icon set (`src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`).
- Added focused `Icon` behavior coverage in `src/Flutter.Tests/TextWidgetTests.cs`:
  - icon-theme default resolution,
  - explicit size/color override precedence,
  - null-icon size box behavior,
  - RTL `matchTextDirection` transform behavior,
  - Material `arrow_back` constant mapping (`codePoint` + `matchTextDirection` + font family).
- Added iteration tracking artifacts for this parity pass (`docs/ai/material-2026-04-05-icon-widget-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md`).

## [2026-04-05] - M4 IconButton parity baseline

### Changed

- Added framework Material `IconButton` in `src/Flutter.Material/IconButton.cs` with Flutter-like API surface for current framework scope:
  - baseline constructor + M3 variant factories (`Filled`, `FilledTonal`, `Outlined`),
  - `styleFrom(...)` helper,
  - selected-state behavior (`isSelected`, `selectedIcon`) and variant-specific default tokens.
- Extended Material style/theming primitives for icon-button parity:
  - added `MaterialState.Selected`,
  - added `IconButtonThemeData` + inherited `IconButtonTheme`,
  - added `ThemeData.IconButtonStyle` and `ThemeData.IconButtonTheme`,
  - added M3 tokens used by icon-button defaults (`SurfaceContainerHighestColor`, `InverseSurfaceColor`, `OnInverseSurfaceColor`).
- Extended shared `MaterialButtonCore` to support selected-state propagation and optional hover/long-press callbacks, enabling icon-button interactions without duplicating button-state infrastructure.
- Added focused `MaterialButtonsTests` coverage for icon-button parity-critical behavior:
  - default icon color/size and mode-aware min-size defaults,
  - `styleFrom` icon/foreground overrides,
  - `IconButtonTheme` precedence over ambient `IconTheme` with widget-style override precedence,
  - selected-icon rendering and outlined selected-border behavior,
  - tap-target-size style override precedence over theme defaults.
- Updated Material buttons runtime demo parity in both samples (`src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`, `dart_sample/lib/material_buttons_demo_page.dart`) with `IconButton` probes (standard selected toggle + filled + outlined).
- Added iteration tracking artifacts for this parity pass (`docs/ai/material-2026-04-05-icon-button-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md`).

## [2026-04-05] - M4 material button parity close-out (padding scale + theme icon-alignment precedence)

### Changed

- Aligned Material button default paddings with Flutter `scaledPadding` interpolation semantics across `TextButton`, `ElevatedButton`, `OutlinedButton`, and `FilledButton`:
  - piecewise interpolation now follows Flutter behavior for text-scale multipliers (`<=1`, `1..2`, `2..3`, `>=3`),
  - default and icon-variant padding tables now scale with ambient `MediaQuery.TextScaleFactor` and baseline `labelLarge` font size.
  (`src/Flutter.Material/Buttons.cs`).
- Added direction-aware start/end default icon paddings for button variants that map to Flutter `EdgeInsetsDirectional` defaults, so RTL swaps start/end insets correctly (`src/Flutter.Material/Buttons.cs`).
- Aligned icon-factory icon-alignment precedence with Flutter order:
  - explicit icon-factory `iconAlignment` argument,
  - local button-theme style `iconAlignment`,
  - button `style` `iconAlignment`,
  - fallback `start`.
  (`src/Flutter.Material/Buttons.cs`).
- Expanded `MaterialButtonsTests` with focused coverage for:
  - text-scale-driven default/icon padding interpolation,
  - RTL directional start/end padding mapping for icon variants,
  - theme-level icon-alignment precedence on text/elevated/outlined/filled icon buttons
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this close-out step (`docs/ai/material-2026-04-05-button-padding-scale-theme-iconalignment-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 button icon-factory text-scale spacing parity hardening

### Changed

- Extended framework ambient media metrics with `MediaQueryData.TextScaleFactor` (`1.0` default) and added `MediaQuery.TextScaleFactorOf(...)` / `MaybeTextScaleFactorOf(...)` helpers for context-driven text scaling lookup (`src/Flutter/Widgets/MediaQuery.cs`).
- Aligned Material `.Icon(...)` button child spacing with Flutter icon-factory behavior:
  - `MaterialButtonIconFactory` now computes spacing using Flutter-style interpolation `lerp(8, 4, clamp(effectiveTextScale, 1, 2) - 1)`.
  - Effective text scale now uses style text-size baseline (`buttonStyle.textStyle.fontSize` fallback `14`) multiplied by ambient `MediaQuery` text scale factor.
  (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for icon-factory spacing behavior:
  - default spacing at baseline scale (`8`),
  - interpolated spacing at scale `1.5` (`6`),
  - clamped minimum spacing at scale `>= 2` (`4`),
  - style text-size driven scaling (`fontSize: 28` -> `4`)
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-button-icon-spacing-textscale-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 button `styleFrom` icon-color + M3 shadow parity hardening

### Changed

- Aligned `styleFrom(iconColor: ...)` disabled-state mapping with Flutter `defaultColor` semantics for `ElevatedButton`, `OutlinedButton`, and `FilledButton`: when `disabledIconColor` is omitted, disabled icon color now stays on the provided `iconColor` instead of falling back to default disabled foreground (`src/Flutter.Material/Buttons.cs`).
- Aligned non-elevated M3 shadow behavior with Flutter defaults:
  - `TextButton` and `OutlinedButton` default shadow token now resolves to transparent when `UseMaterial3=true`, so style-level elevation without explicit `shadowColor` no longer paints fallback shadows.
  - M2 behavior remains unchanged (`UseMaterial3=false` keeps theme-shadow fallback via default shadow token)
  (`src/Flutter.Material/Buttons.cs`).
- Expanded `MaterialButtonsTests` coverage with:
  - disabled icon-color mapping checks for `ElevatedButton`/`OutlinedButton`/`FilledButton` `styleFrom(iconColor: ...)`,
  - M3-vs-M2 shadow-fallback split checks for `TextButton`/`OutlinedButton` style-level elevation without explicit shadow color
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-button-stylefrom-iconcolor-shadow-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 button icon-alignment directionality parity hardening

### Changed

- Added core `Directionality` inherited widget in framework layer with ambient `TextDirection` lookup (`Of`/`MaybeOf`) to support context-driven start/end behavior (`src/Flutter/Widgets/Directionality.cs`).
- Aligned Material icon-factory icon-order behavior with Flutter directionality semantics:
  - `IconAlignment.Start`/`End` now resolves against ambient text direction (`LTR`/`RTL`) instead of fixed visual left/right ordering.
  - `MaterialButtonIconFactory` now builds icon/label row order from `Directionality.Of(context)` while preserving existing spacing/flex composition (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for RTL icon-order resolution:
  - `IconAlignment.Start` under RTL places label before icon,
  - `IconAlignment.End` under RTL places icon before label
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-button-icon-alignment-directionality-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 button icon-alignment parity hardening

### Changed

- Added `IconAlignment` parity primitives to Material button styling:
  - introduced `IconAlignment` enum (`Start`, `End`),
  - added `ButtonStyle.IconAlignment` with merge support,
  - wired icon-alignment precedence into composed style layers in `MaterialButtonCore.ComposeStyles(...)`
  (`src/Flutter.Material/ButtonStyle.cs`, `src/Flutter.Material/Buttons.cs`).
- Expanded `styleFrom(...)` APIs with `iconAlignment` for `TextButton`, `ElevatedButton`, `OutlinedButton`, and `FilledButton`, matching Flutter button-style surface for icon-bearing button variants (`src/Flutter.Material/Buttons.cs`).
- Expanded icon-factory APIs with explicit `iconAlignment` arguments for:
  - `TextButton.Icon(...)`
  - `ElevatedButton.Icon(...)`
  - `OutlinedButton.Icon(...)`
  - `FilledButton.Icon(...)`
  - `FilledButton.TonalIcon(...)`
  and wired precedence `iconAlignment arg -> style.iconAlignment -> start` for icon-row composition (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for icon-alignment behavior:
  - `IconAlignment.End` icon-row order for text/filled-tonal icon factories,
  - `styleFrom(iconAlignment: ...)` propagation to icon factory layout,
  - explicit icon-factory `iconAlignment` override precedence over `styleFrom(...)`
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-button-icon-alignment-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 button icon-factory default padding parity hardening

### Changed

- Added Flutter-like icon factory constructors for Material buttons:
  - `TextButton.Icon(...)`
  - `ElevatedButton.Icon(...)`
  - `OutlinedButton.Icon(...)`
  - `FilledButton.Icon(...)`
  - `FilledButton.TonalIcon(...)`
  (`src/Flutter.Material/Buttons.cs`).
- Added shared icon+label composition helper for icon factories (`Row(mainAxisSize: min, spacing: 8, children: [icon, Flexible(label)])`) to align default button child structure with Flutter icon-button composition (`src/Flutter.Material/Buttons.cs`).
- Aligned icon-factory default padding to Flutter mode-aware defaults:
  - `TextButton.Icon`: M3 `12/8/16/8`, M2 `all(8)`
  - `ElevatedButton.Icon`: M3 `16/0/24/0`, M2 `12/0/16/0`
  - `OutlinedButton.Icon`: M3 `16/0/24/0`, M2 `16/0` (same as non-icon default)
  - `FilledButton.Icon` and `FilledButton.TonalIcon`: M3 `16/0/24/0`, M2 `12/0/16/0`
  (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for icon-factory default padding across M3/M2 paths for text/elevated/outlined/filled/tonal filled buttons (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-button-icon-factory-padding-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 filled-button default padding/elevation parity hardening

### Changed

- Aligned `FilledButton` and `FilledButton.Tonal` default horizontal padding with Flutter mode-aware scaling:
  - `UseMaterial3=true` keeps `horizontal: 24`,
  - `UseMaterial3=false` now resolves to `horizontal: 16`
  (`src/Flutter.Material/Buttons.cs`).
- Aligned `FilledButton` default elevation progression with Flutter defaults by adding hovered elevation state (`default=0`, `hovered=1`) while keeping non-hover states at `0` (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for:
  - M2 default padding on filled and tonal variants,
  - default filled hovered-elevation behavior (shadow appears only on hover)
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-filled-button-default-padding-elevation-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 button M2 overlay-opacity default parity hardening

### Changed

- Extended `MaterialButtonCore.CreateDefaultOverlayResolver(...)` with configurable pressed/focused opacity while preserving hover opacity (`0.08`) and existing default behavior (`0.10`) for current call sites (`src/Flutter.Material/Buttons.cs`).
- Aligned mode-aware default overlay behavior for `TextButton`, `ElevatedButton`, and `OutlinedButton` with Flutter M2 semantics:
  - when `UseMaterial3=false`, focused/pressed overlay alpha now resolves to `0.12`,
  - when `UseMaterial3=true`, existing focused/pressed overlay alpha `0.10` is preserved
  (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for M2 overlay defaults across text/elevated/outlined buttons, including elevated `onPrimary` overlay blending over primary background (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-button-m2-overlay-opacity-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 button M2 default color/elevation parity hardening

### Changed

- Aligned `ElevatedButton` mode-aware default color tokens with Flutter behavior:
  - `UseMaterial3=true`: keep existing M3 defaults (`surfaceContainerLow` background, `primary` foreground),
  - `UseMaterial3=false`: now use M2 defaults (`primary` background, `onPrimary` foreground)
  (`src/Flutter.Material/Buttons.cs`).
- Aligned `ElevatedButton` default elevation resolver with Flutter M2 state map when `UseMaterial3=false`:
  - `disabled=0`, `default=2`, `hovered/focused=4`, `pressed=8`
  while preserving existing M3 behavior (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for:
  - M2 elevated default color pair,
  - M2 elevated default elevation-state mapping,
  - baseline M2 foreground-default behavior for `TextButton` and `OutlinedButton`
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-button-m2-default-colors-elevation-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 button M2 default geometry parity hardening

### Changed

- Aligned mode-aware default geometry for `TextButton`, `ElevatedButton`, and `OutlinedButton` with Flutter M2 behavior under `UseMaterial3=false` (`minimumSize` height `36`, M2 padding tokens, and rounded shape radius `4`) while preserving current M3 defaults (`src/Flutter.Material/Buttons.cs`).
- Updated these button constructors to treat omitted `minHeight` as mode-resolved default (`M3=40`, `M2=36`) and keep explicit `minHeight` values as overrides (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for M2 default `minimumSize`, `padding`, and clip-shape behavior across text/elevated/outlined buttons (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-button-m2-default-geometry-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 outlined-button M2 border-focus parity hardening

### Changed

- Aligned `OutlinedButton` default border-side state mapping with Flutter mode-aware behavior:
  - `UseMaterial3=true`: focused border uses primary color, other enabled states use outline color.
  - `UseMaterial3=false`: focused and unfocused enabled border both use `onSurface(0.12)` (no primary focus border accent)
  (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for M2 outlined-button border behavior:
  - default enabled border uses `onSurface` opacity path,
  - focused border remains on the same `onSurface` opacity path in M2 mode
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-outlined-button-m2-border-focus-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 outlined/filled `styleFrom` background mapping parity correction

### Changed

- Corrected `styleFrom(...)` background mapping split to match Flutter source:
  - `OutlinedButton.styleFrom(backgroundColor: ...)` without `disabledBackgroundColor` now resolves as all-state background color (disabled state included),
  - `FilledButton.styleFrom(backgroundColor: ...)` without `disabledBackgroundColor` now keeps default disabled fallback behavior (no all-state special-case)
  (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for:
  - outlined-button background-only style override winning over themed disabled background,
  - filled-button background-only style preserving default disabled background fallback
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-outlined-filled-stylefrom-background-mapping-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-04] - M4 material button icon-theme parity hardening

### Changed

- Extended Material `ButtonStyle` with state-aware icon fields (`IconColor`, `IconSize`) and wired them into style merge/state resolution in framework button infrastructure (`src/Flutter.Material/ButtonStyle.cs`).
- Extended `TextButton.StyleFrom(...)`, `ElevatedButton.StyleFrom(...)`, `OutlinedButton.StyleFrom(...)`, and `FilledButton.StyleFrom(...)` with Flutter-like icon arguments (`iconColor`, `disabledIconColor`, `iconSize`) and mapped them to style-state resolvers (`src/Flutter.Material/Buttons.cs`).
- Aligned default button icon tokens with Flutter M3 button defaults by setting icon-size baseline `18` and state-aware icon color defaults tied to existing foreground/disabled token paths across Material button types (`src/Flutter.Material/Buttons.cs`).
- `MaterialButtonCore` now wraps button child content with resolved `IconTheme` so icon-bearing button subtrees inherit composed button style icon settings (with foreground fallback when icon color is not explicitly provided), matching Flutter `ButtonStyleButton` icon-theme behavior (`src/Flutter.Material/Buttons.cs`).
- Added focused regression coverage in `MaterialButtonsTests` for button icon-theme defaults and `styleFrom(...)` icon overrides (enabled and disabled paths) (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-04-button-icon-theme-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-04] - M4 material button tap-target-size parity hardening

### Changed

- Extended `ButtonStyle` with Flutter-like `TapTargetSize` support and wired it into style merge/composition resolution (`src/Flutter.Material/ButtonStyle.cs`, `src/Flutter.Material/Buttons.cs`).
- Added `ThemeData.MaterialTapTargetSize` (default `Padded`) and propagated it into default Material button styles so ambient theme controls the default tap-target policy (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Buttons.cs`).
- Extended `TextButton.StyleFrom(...)`, `ElevatedButton.StyleFrom(...)`, `OutlinedButton.StyleFrom(...)`, and `FilledButton.StyleFrom(...)` with `tapTargetSize` override support (`src/Flutter.Material/Buttons.cs`).
- `MaterialButtonCore` now resolves tap-target min size from composed button style (`Padded` -> `48x48`, `ShrinkWrap` -> `0x0`) before applying `ButtonTapTargetPadding`, matching Flutter `_InputPadding` tap-target mode behavior (`src/Flutter.Material/Buttons.cs`).
- Expanded `MaterialButtonsTests` with coverage for:
  - `ThemeData.Light` default tap-target mode (`Padded`),
  - theme-level `MaterialTapTargetSize.ShrinkWrap` behavior,
  - `styleFrom(tapTargetSize: ...)` precedence over theme defaults
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-04-button-tap-target-size-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-04] - M4 material button surface-tint parity hardening

### Changed

- Extended `ButtonStyle` with state-aware `SurfaceTintColor` and wired it into style merge/composition/state-resolution pipeline for Material buttons (`src/Flutter.Material/ButtonStyle.cs`, `src/Flutter.Material/Buttons.cs`).
- Extended `TextButton.StyleFrom(...)`, `ElevatedButton.StyleFrom(...)`, `OutlinedButton.StyleFrom(...)`, and `FilledButton.StyleFrom(...)` with `surfaceTintColor` support (`src/Flutter.Material/Buttons.cs`).
- `MaterialButtonCore` now applies Flutter-like surface tint blending to resolved button background color using elevation-based opacity interpolation (token table `0 -> 0.00`, `1 -> 0.05`, `3 -> 0.08`, `6 -> 0.11`, `8 -> 0.12`, `12 -> 0.14`) before state overlay tinting (`src/Flutter.Material/Buttons.cs`).
- Added focused regression coverage in `MaterialButtonsTests` for:
  - `ElevatedButton.styleFrom(surfaceTintColor: ...)` background tinting by elevation,
  - theme-level `ElevatedButtonTheme` `surfaceTintColor` tinting on default elevated background
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-04-button-surface-tint-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-04] - M4 material button styleFrom shadow/elevation parity expansion

### Changed

- Expanded `TextButton.StyleFrom(...)`, `OutlinedButton.StyleFrom(...)`, and `FilledButton.StyleFrom(...)` with `shadowColor` and `elevation` arguments to align API shape with Flutter `styleFrom` helpers (`src/Flutter.Material/Buttons.cs`).
- Wired new style arguments into `ButtonStyle.ShadowColor` and `ButtonStyle.Elevation` so non-elevated button types can opt into explicit shadow/elevation visuals via style overrides in the existing `MaterialButtonCore` paint path (`src/Flutter.Material/Buttons.cs`).
- Added focused regression coverage in `MaterialButtonsTests` validating shadow rendering for `TextButton`, `OutlinedButton`, and `FilledButton` when `styleFrom(shadowColor: ..., elevation: ...)` is provided (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-04-button-stylefrom-shadow-elevation-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-04] - M4 material shadow-color fallback parity for styleFrom elevation overrides

### Changed

- `MaterialButtonCore` now applies Flutter-like shadow fallback semantics: when composed button style resolves a positive `elevation` but no explicit `shadowColor`, rendering falls back to `ThemeData.ShadowColor` instead of dropping the shadow (`src/Flutter.Material/Buttons.cs`).
- Added focused regression coverage in `MaterialButtonsTests` for fallback behavior on `TextButton`, `OutlinedButton`, and `FilledButton` with `styleFrom(elevation: ...)` and no explicit `shadowColor` (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-04-button-shadow-fallback-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-04] - M4 text/outlined styleFrom disabled-color mapping parity hardening

### Changed

- Aligned `TextButton.styleFrom(...)` mapping with Flutter behavior:
  - `backgroundColor` without `disabledBackgroundColor` now resolves as `all(backgroundColor)` (applies to disabled state),
  - `iconColor` without `disabledIconColor` now resolves as `all(iconColor)` (applies to disabled state)
  (`src/Flutter.Material/Buttons.cs`).
- Aligned `OutlinedButton.styleFrom(...)` background mapping with Flutter behavior: `backgroundColor` without `disabledBackgroundColor` now resolves as `all(backgroundColor)` and remains applied in disabled state (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for:
  - disabled `TextButton` icon-color behavior when only `iconColor` is provided,
  - disabled `TextButton` background behavior when only `backgroundColor` is provided,
  - disabled `OutlinedButton` background behavior when only `backgroundColor` is provided
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-04-text-outlined-stylefrom-disabled-color-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-04-05] - M4 material surface-tint gating parity (`useMaterial3`)

### Changed

- Aligned button surface-tint behavior with Flutter Material semantics by gating `surfaceTintColor` application on `ThemeData.UseMaterial3`: tint overlay is now ignored when `UseMaterial3` is `false` (`src/Flutter.Material/Buttons.cs`).
- Added focused `MaterialButtonsTests` coverage for both style-level and theme-level elevated-button surface tint under `UseMaterial3 = false` to ensure background remains untinted (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-04-05-button-surface-tint-usematerial3-gating-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-03-29] - M4 material elevated-button shadow/elevation parity hardening

### Changed

- Added framework decoration shadow support by extending `BoxDecoration` with optional `BoxShadows` and wiring it through `PaintingContext` + `RenderDecoratedBox` so material components can render elevation shadows (`src/Flutter/Rendering/Decoration.cs`, `src/Flutter/Rendering/Object.PaintingContext.cs`, `src/Flutter/Rendering/Proxy.RenderBox.cs`).
- Extended Material `ButtonStyle` with state-aware `ShadowColor` and `Elevation` properties and included them in style merge/layer composition and state resolution (`src/Flutter.Material/ButtonStyle.cs`, `src/Flutter.Material/Buttons.cs`).
- Aligned `ElevatedButton` defaults with Flutter-like elevation states by adding enabled/hovered/pressed/focused/disabled elevation resolution, and mapped default shadow-color source to theme token (`ThemeData.ShadowColor`, default black) (`src/Flutter.Material/Buttons.cs`, `src/Flutter.Material/ThemeData.cs`).
- Aligned `ElevatedButton.styleFrom(elevation: ...)` with Flutter semantics (`disabled=0`, `pressed=+6`, `hovered/focused=+2`, `default=base`) instead of fixed elevation for all states (`src/Flutter.Material/Buttons.cs`).
- `MaterialButtonCore` now resolves elevation/shadow style states into a Material-like multi-layer shadow recipe, restoring visible elevated depth in sample buttons (including app-bar `Back` action) (`src/Flutter.Material/Buttons.cs`).
- Aligned default button label typography with Flutter M3 tokens by keeping `MaterialTextTheme.DefaultLabelLarge` at `FontWeight.Medium` and aligning `MaterialButtonCore` baseline text-style merge source to `ThemeData.TextTheme.LabelLarge` instead of hardcoded metrics (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Buttons.cs`).
- Kept Android body-font fallback family explicit as `Roboto` to match Flutter Material typography resolution on Android (`src/Flutter.Material/ThemeData.cs`).
- Expanded Material button regression coverage with elevated shadow tests (enabled vs disabled) and updated label-large weight expectations (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-03-29-button-elevation-shadow-typography-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/PARITY_MATRIX.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-03-29] - M4 material button color/typography parity hardening

### Changed

- Aligned framework `ThemeData.Light` defaults with Flutter Material 3 light tokens used by sample controls:
  - `ScaffoldBackgroundColor`/`CanvasColor`: `#FEF7FF`,
  - `PrimaryColor`: `#6750A4`,
  - `OnSurfaceColor`: `#1D1B20`,
  - `OnSecondaryContainerColor`: `#4A4458`
  while preserving existing theme override semantics (`src/Flutter.Material/ThemeData.cs`).
- Expanded `MaterialTextTheme` with `LabelLarge` and updated default typography tokens to match Flutter 2021 baseline more closely:
  - `TitleLarge`: `22 / 1.27 / 0.0` with regular weight,
  - `LabelLarge`: `14 / 1.43 / 0.1` with medium weight
  (`src/Flutter.Material/ThemeData.cs`).
- Updated default body-font resolution in `MaterialTextTheme` to follow platform-oriented fallback families (Android `Roboto`, iOS/macOS system UI font, Windows `Segoe UI`, Linux `Noto Sans`) for closer cross-host typography parity (`src/Flutter.Material/ThemeData.cs`).
- Aligned Material button defaults with Flutter `ButtonStyleButton` behavior:
  - `TextButton`/`ElevatedButton`/`OutlinedButton`/`FilledButton` now provide default `ButtonStyle.TextStyle` from `ThemeData.TextTheme.LabelLarge`,
  - `ElevatedButton`/`OutlinedButton`/`FilledButton` default content padding now follows Flutter M3 generated defaults (`horizontal: 24`, `vertical: 0`),
  - focused `OutlinedButton` border now resolves to primary color,
  - `MaterialButtonCore` now keeps resolved foreground color as source of truth for label paint when `ButtonStyle.TextStyle.Color` is set (matching Flutter precedence),
  - `_InputPadding` parity hardening: tap-target wrapper now lays out its child with incoming constraints (matching Flutter wide-button behavior) and redirects hit-tests in padded area to visual-child center
  (`src/Flutter.Material/Buttons.cs`).
- Added `InternalsVisibleTo("Flutter.Material")` on core `Flutter` assembly and aligned `InheritedWidget.UpdateShouldNotify` overrides in `Flutter.Material` to `protected internal` so framework-owned render-object widgets in Material assembly can override core internal render-widget hooks (`src/Flutter/AssemblyInfo.cs`, `src/Flutter.Material/Theme.cs`, `src/Flutter.Material/ButtonThemes.cs`).
- Extended `MaterialButtonsTests` with focused regressions for:
  - `ThemeData.Light` M3 token defaults,
  - default `TextButton` label typography metrics,
  - foreground-color precedence over `textStyle.color`,
  - default `ElevatedButton`/`OutlinedButton`/`FilledButton` horizontal-only padding tokens,
  - tap-target padded-area hit-test redirection in `TextButton`
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Tightened C# sample literal parity with Dart for menu/buttons demo text presentation:
  - switched subtitle helper text to `black54` equivalent (`#8A000000`),
  - switched status line color to `blueGrey` equivalent (`#607D8B`),
  - normalized `enabled=True/False` output to Dart-like lowercase `enabled=true/false`
  (`src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-03-29-button-color-typography-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/PARITY_MATRIX.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-03-29] - M4 system bars styling parity bridge

### Added

- Added framework `SystemChrome` API surface with `SystemUiOverlayStyle` and icon-brightness model so status/navigation bar styling can be controlled directly from C# code (`src/Flutter/UI/SystemChrome.cs`).

### Changed

- `FlutterHost` now listens to `SystemChrome` overlay-style updates and applies them to platform insets/system bars:
  - shared system-bar color via `IInsetsManager.SystemBarColor`,
  - icon theme via `SystemBarTheme` reflection when available,
  - Android-specific best-effort split color application (`status` and `navigation`) via native window reflection fallback,
  - adaptive edge-to-edge mode: edge-to-edge is now enabled only when both system-bar colors are transparent/unset, and disabled for opaque bar colors so API 33/34 status/navigation bar colors are actually applied (`src/Flutter/FlutterHost.cs`).
- Extended `AppBar` with `systemOverlayStyle` and theme-level `AppBarThemeData.SystemOverlayStyle` support; app-bar build now resolves and pushes effective overlay style into `SystemChrome` using Flutter-like precedence (`widget -> theme appBarTheme -> default`) (`src/Flutter.Material/Scaffold.cs`, `src/Flutter.Material/ThemeData.cs`).
- Added focused `MaterialScaffoldTests` coverage for app-bar overlay-style precedence (`theme` default and `widget` override) (`src/Flutter.Tests/MaterialScaffoldTests.cs`).
- Updated Android host theme defaults to remove gray system-bar backgrounds by default:
  - switched to explicit light appcompat base theme,
  - enabled system-bar background drawing,
  - set transparent status/navigation bars with light-icon flags and contrast-enforcement overrides on API 31+,
  - removed `windowIsTranslucent` from API 31+ main theme (`src/Sample/Flutter.Net.Android/Resources/values/styles.xml`, `src/Sample/Flutter.Net.Android/Resources/values-v31/styles.xml`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-03-29-system-bars-overlay-native-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-03-29] - M4 app-bar primary status-bar inset parity

### Changed

- Added Flutter-like `AppBar.primary` control in `Flutter.Material` (`true` by default) so app-bar status-bar inset behavior can be toggled per widget (`src/Flutter.Material/Scaffold.cs`).
- `AppBar` now applies top safe-area inset from ambient `MediaQuery.padding.top` when available (via framework `SafeArea(bottom: false)`), preventing toolbar overlap with the system status bar in edge-to-edge hosts while preserving `primary: false` opt-out parity (`src/Flutter.Material/Scaffold.cs`).
- Added focused `MaterialScaffoldTests` regression coverage for `AppBar` top inset behavior under `MediaQuery` (`primary=true` applies top padding, `primary=false` does not) (`src/Flutter.Tests/MaterialScaffoldTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-03-29-appbar-primary-safearea-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-03-29] - Core SafeArea and MediaQuery parity (edge-to-edge)

### Added

- Added framework `MediaQueryData` + inherited `MediaQuery` primitives in `src/Flutter` with parity-critical insets APIs:
  - `MediaQuery.of/maybeOf`,
  - `paddingOf/viewPaddingOf/viewInsetsOf`,
  - `removePadding/removeViewInsets/removeViewPadding`,
  - `MediaQueryData` insets transform helpers matching Flutter semantics for `padding`/`viewPadding` interactions (`src/Flutter/Widgets/MediaQuery.cs`).
- Added framework `SafeArea` widget in `src/Flutter` with Flutter-like composition and defaults:
  - side flags (`left/top/right/bottom`),
  - `minimum`,
  - `maintainBottomViewPadding`,
  - child `MediaQuery.removePadding(...)` wrapping behavior (`src/Flutter/Widgets/SafeArea.cs`).
- Added focused regression coverage in `SafeAreaTests` for:
  - safe-area padding application + descendant media-padding consumption,
  - `maintainBottomViewPadding` when bottom padding is consumed,
  - side-flag + minimum precedence behavior,
  - `MediaQueryData.removePadding` view-padding adjustment parity,
  - ambient root `MediaQuery` availability in `WidgetHost`
  (`src/Flutter.Tests/SafeAreaTests.cs`).

### Changed

- `WidgetHost` now wraps the app root widget in ambient `MediaQuery` and refreshes it on host metric changes so `SafeArea`/`MediaQuery` consumers work out of the box (`src/Flutter/WidgetHost.cs`).
- `FlutterHost` now sources media insets from host features (`TopLevel.InsetsManager.SafeAreaPadding` and `TopLevel.InputPane.OccludedRect`) and feeds that into root `MediaQueryData` generation (`src/Flutter/FlutterHost.cs`).
- Android legacy non-edge-to-edge behavior is intentionally out of scope for this parity step: when insets manager is available, host now sets `DisplayEdgeToEdgePreference = true` to align runtime behavior with edge-to-edge assumptions (`src/Flutter/FlutterHost.cs`).
- Added tracking artifacts for this parity step (`docs/ai/core-2026-03-29-safearea-mediaquery-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-03-29] - M4 material-button focus-node parity

### Changed

- Extended framework Material button API parity with Flutter `ButtonStyleButton` focus controls by adding `focusNode` and `autofocus` parameters to `TextButton`, `ElevatedButton`, `FilledButton`, and `OutlinedButton`, and propagating them through `MaterialButtonCore` (`src/Flutter.Material/Buttons.cs`).
- Updated `MaterialButtonCore` focus lifecycle handling to support both owned and externally provided focus nodes without disposal leaks on external nodes, while preserving focused-overlay state updates and existing keyboard activation flow (`src/Flutter.Material/Buttons.cs`).
- Added focused regression coverage in `MaterialButtonsTests` for:
  - external focus-node driven focused overlay updates,
  - autofocus requesting the provided focus node on mount,
  - autofocus transition from `false` to `true` after rebuild
  (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-03-29-button-focus-autofocus-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-03-29] - M4 filled-button parity expansion

### Added

- Extended `Flutter.Material` button control set with `FilledButton` plus tonal variant factory `FilledButton.Tonal(...)`, both wired through existing `MaterialButtonCore` state/render pipeline (`src/Flutter.Material/Buttons.cs`).
- Added Filled-button theming and token surfaces in `ThemeData`:
  - color tokens `SecondaryContainerColor` and `OnSecondaryContainerColor`,
  - style hooks `FilledButtonStyle` and `FilledButtonTheme` (`FilledButtonThemeData`) for global + local theme override parity (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/ButtonThemes.cs`).
- Added focused regression coverage in `MaterialButtonsTests` for:
  - default filled and tonal token resolution,
  - disabled-state fallback tones derived from `OnSurfaceColor`,
  - `ThemeData.FilledButtonTheme` precedence over legacy `FilledButtonStyle`,
  - local `FilledButtonTheme` subtree precedence (`src/Flutter.Tests/MaterialButtonsTests.cs`).

### Changed

- Expanded Material buttons runtime parity demo in both C# and Dart samples with `FilledButton` and `FilledButton.tonal` probes (enabled/disabled toggle flow, dedicated tap counters, and custom color overrides) (`src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`, `dart_sample/lib/material_buttons_demo_page.dart`).
- Updated sample-gallery route subtitle parity to include Filled-button coverage on both sample sides (`src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`).
- Added iteration tracking artifacts for this parity step (`docs/ai/material-2026-03-29-filled-button-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/PARITY_MATRIX.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-03-19] - M4 app-bar toolbar-edge geometry parity

### Changed

- Hardened `AppBar` toolbar geometry defaults in `Flutter.Material` to align closer with Flutter `AppBar`/`NavigationToolbar` behavior:
  - removed framework-only default outer toolbar padding (`0` default instead of implicit horizontal `16`),
  - removed hardcoded actions-row inter-item spacing so actions use their own widget-level sizing/padding (`src/Flutter.Material/Scaffold.cs`).
- Aligned `AppBar` default string-title behavior with Flutter defaults: `titleText` now maps to single-line non-wrapping text with ellipsis trimming (`softWrap: false`, `maxLines: 1`, `overflow: ellipsis`) in framework `Scaffold/AppBar` composition (`src/Flutter.Material/Scaffold.cs`).
- Added widget-level `mainAxisSize` property wiring for `Flex`/`Row`/`Column` and applied `MainAxisSize.Min` for `AppBar` actions row to match Flutter-style shrink-wrapped toolbar actions layout (`src/Flutter/Widgets/Basic.cs`, `src/Flutter.Material/Scaffold.cs`).
- Aligned `AppBar` leading-slot sizing with Flutter toolbar geometry by constraining leading slot with both effective `leadingWidth` and effective `toolbarHeight` (`src/Flutter.Material/Scaffold.cs`).
- Aligned empty-string `AppBar.titleText` parity with Flutter: `titleText: ""` now renders as a default title `Text("")` rather than being treated as absent title (`src/Flutter.Material/Scaffold.cs`).
- Added `ThemeData.UseMaterial3` (default `true`) in `Flutter.Material` to mirror Flutter theme mode switch semantics (`src/Flutter.Material/ThemeData.cs`).
- Aligned `AppBar` actions-row cross-axis layout with Flutter Material mode behavior: actions row now uses `CrossAxisAlignment.Center` when `ThemeData.UseMaterial3` is `true` and `CrossAxisAlignment.Stretch` when `false` (`src/Flutter.Material/Scaffold.cs`).
- Aligned `AppBar` default toolbar-height behavior with Flutter source precedence: unresolved toolbar height now follows `widget -> appBarTheme -> default 56` (`kToolbarHeight`) for both M3 and M2 modes (`src/Flutter.Material/Scaffold.cs`).
- Aligned `AppBar` default background/foreground color fallback with Flutter Material mode behavior: unresolved app-bar colors now use `CanvasColor`/`OnSurfaceColor` in M3 (`UseMaterial3=true`) and `PrimaryColor`/`OnPrimaryColor` in M2 (`UseMaterial3=false`) while preserving existing `widget -> appBarTheme -> default` precedence (`src/Flutter.Material/Scaffold.cs`).
- Added `ThemeData.Brightness` (`Light` default) and aligned M2 app-bar default color behavior with Flutter dark-mode path: when `UseMaterial3=false` and `Brightness=Dark`, unresolved app-bar defaults now use `CanvasColor`/`OnSurfaceColor` instead of `PrimaryColor`/`OnPrimaryColor` (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Scaffold.cs`).
- Added `ThemeData.OnSurfaceVariantColor` token and aligned `AppBar` actions icon default fallback with Flutter Material mode behavior: when no explicit actions/icon theme is provided, actions icons now default to `OnSurfaceVariantColor` in M3 and continue using foreground fallback in M2, while preserving existing precedence for `actionsIconTheme`/`iconTheme` overrides (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Scaffold.cs`).
- Aligned `AppBar` leading icon-theme defaults with Flutter Material mode behavior: when no explicit `iconTheme` is provided, M3 now defaults to foreground color with `size: 24`, while M2 keeps foreground fallback without forcing icon size (`src/Flutter.Material/Scaffold.cs`).
- Added focused regression coverage in `MaterialScaffoldTests` for the updated app-bar geometry behavior:
  - default zero outer toolbar padding,
  - no extra hardcoded spacing in actions row (`src/Flutter.Tests/MaterialScaffoldTests.cs`).
- Added focused regression coverage for default app-bar title overflow behavior (`AppBar_DefaultTitle_UsesSingleLineEllipsisDefaults`) in `src/Flutter.Tests/MaterialScaffoldTests.cs`.
- Added focused regression coverage for tight leading-slot geometry (`AppBar_LeadingSlot_IsConstrainedByLeadingWidthAndToolbarHeight`) in `src/Flutter.Tests/MaterialScaffoldTests.cs`.
- Added focused regression coverage for empty-string title rendering parity (`AppBar_DefaultTitle_EmptyString_IsRenderedAsText`) in `src/Flutter.Tests/MaterialScaffoldTests.cs`.
- Added focused regression coverage for `ThemeData.UseMaterial3` default value and Material-mode-dependent app-bar actions-row alignment behavior in `src/Flutter.Tests/MaterialScaffoldTests.cs`.
- Added focused regression coverage for default app-bar toolbar height precedence and mode-invariant fallback (`AppBar_ToolbarHeight_DefaultsTo56_WhenUseMaterial3IsEnabled`, `AppBar_ToolbarHeight_DefaultsTo56_WhenUseMaterial3IsDisabled`) in `src/Flutter.Tests/MaterialScaffoldTests.cs`.
- Added focused regression coverage for Material-mode-dependent default app-bar colors in `MaterialScaffoldTests` (`Scaffold_WithAppBar_UsesThemeCanvasColorForAppBarBackground_WhenUseMaterial3IsEnabled`, `Scaffold_WithAppBar_UsesThemePrimaryColorForAppBarBackground_WhenUseMaterial3IsDisabled`, `AppBar_DefaultTitle_UsesThemeOnSurfaceColor_WhenUseMaterial3IsEnabled`, `AppBar_DefaultTitle_UsesThemeOnPrimaryColor_WhenUseMaterial3IsDisabled`) and aligned title text-style fallback test expectation with M3 foreground defaults (`src/Flutter.Tests/MaterialScaffoldTests.cs`).
- Added focused regression coverage for Material-mode-dependent default actions icon color path (`AppBar_ActionsIconTheme_DefaultsToOnSurfaceVariant_WhenUseMaterial3IsEnabled`, `AppBar_ActionsIconTheme_DefaultsToOnPrimary_WhenUseMaterial3IsDisabled`) in `src/Flutter.Tests/MaterialScaffoldTests.cs`.
- Added focused regression coverage for leading icon-theme mode defaults (`AppBar_IconTheme_DefaultsToOnSurfaceAndSize24_ForLeading_WhenUseMaterial3IsEnabled`, `AppBar_IconTheme_DefaultsToOnPrimary_ForLeading_WhenUseMaterial3IsDisabled`) in `src/Flutter.Tests/MaterialScaffoldTests.cs`.
- Added focused regression coverage for `ThemeData.Brightness` default and M2 dark-mode app-bar default color path (`ThemeData_DefaultsBrightnessToLight`, `Scaffold_WithAppBar_UsesThemeCanvasColorForAppBarBackground_WhenUseMaterial3IsDisabledAndBrightnessDark`, `AppBar_DefaultTitle_UsesThemeOnSurfaceColor_WhenUseMaterial3IsDisabledAndBrightnessDark`) in `src/Flutter.Tests/MaterialScaffoldTests.cs`.
- Added task notes and tracking updates for this parity-hardening iteration (`docs/ai/material-2026-03-19-appbar-toolbar-edge-parity.md`, `docs/ai/material-2026-03-19-appbar-default-title-ellipsis.md`, `docs/ai/material-2026-03-19-flex-main-axis-size-widget-wiring.md`, `docs/ai/material-2026-03-19-appbar-leading-slot-height-parity.md`, `docs/ai/material-2026-03-19-appbar-empty-titletext-parity.md`, `docs/ai/material-2026-03-19-appbar-actions-cross-axis-stretch-parity.md`, `docs/ai/material-2026-03-19-appbar-actions-cross-axis-material3-parity.md`, `docs/ai/material-2026-03-19-appbar-toolbar-height-material3-defaults.md`, `docs/ai/material-2026-03-19-appbar-toolbar-height-default-parity-correction.md`, `docs/ai/material-2026-03-19-appbar-mode-aware-default-colors.md`, `docs/ai/material-2026-03-19-appbar-actions-icon-mode-aware-defaults.md`, `docs/ai/material-2026-03-19-appbar-leading-icon-theme-mode-aware-defaults.md`, `docs/ai/material-2026-03-19-appbar-m2-dark-default-colors-parity.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).

## [2026-03-14] - M4 app-bar theme colors and toolbar-height precedence

### Changed

- Extended `AppBarThemeData` with Flutter-like app-bar fallback fields: `backgroundColor`, `foregroundColor`, and `toolbarHeight` in `Flutter.Material` (`src/Flutter.Material/ThemeData.cs`).
- Updated framework `AppBar` value resolution to Flutter-like precedence:
  - `backgroundColor`: `widget -> theme appBarTheme -> theme primary`,
  - `foregroundColor`: `widget -> theme appBarTheme -> theme onPrimary`,
  - `toolbarHeight`: `widget -> theme appBarTheme -> default 56` (`src/Flutter.Material/Scaffold.cs`).
- Extended framework `AppBar` leading slot width resolution to Flutter-like precedence (`leadingWidth`: `widget -> theme appBarTheme -> default 56`) via new `AppBarThemeData.LeadingWidth`, and added a resolved-value guard for non-finite/non-positive themed leading width (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Scaffold.cs`).
- Extended app-bar theme/style parity with `actionsPadding`: added `AppBarThemeData.ActionsPadding`, added widget-level `AppBar.actionsPadding`, and wired actions-row padding precedence to Flutter-like order (`widget -> theme appBarTheme -> default zero`) (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Scaffold.cs`).
- Added minimal framework icon-theme primitives (`IconThemeData`, inherited `IconTheme`) and wired app-bar icon-theme precedence for leading/actions slots:
  - `iconTheme`: `widget -> theme appBarTheme -> foreground fallback`,
  - `actionsIconTheme`: `widget -> theme appBarTheme -> iconTheme -> foreground fallback`
  (`src/Flutter/Widgets/IconTheme.cs`, `src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Scaffold.cs`).
- Hardened app-bar icon/theme regression coverage with fallback-chain and mixed-context tests:
  - actions fall back to widget `iconTheme` when `actionsIconTheme` is missing,
  - leading/actions icon themes fall back to `foregroundColor` when icon theme color is unset,
  - actions subtree receives both `toolbarTextStyle` and `actionsIconTheme` inheritance simultaneously
  (`src/Flutter.Tests/MaterialScaffoldTests.cs`).
- Added parity runtime demo route/page for app-bar leading-width precedence in both samples (`AppBar leadingWidth theme`), including controls for theme `LeadingWidth` and widget-level `leadingWidth` override plus themed/default preview comparison (`src/Sample/Flutter.Net/AppBarLeadingWidthDemoPage.cs`, `dart_sample/lib/app_bar_leading_width_demo_page.dart`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- Added parity runtime demo route/page for app-bar actions-padding precedence in both samples (`AppBar actionsPadding theme`), including controls for theme `ActionsPadding` and widget-level `actionsPadding` override plus themed/default preview comparison (`src/Sample/Flutter.Net/AppBarActionsPaddingDemoPage.cs`, `dart_sample/lib/app_bar_actions_padding_demo_page.dart`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- Added parity runtime demo route/page for app-bar icon-theme precedence in both samples (`AppBar icon themes`), including controls for `foreground`, theme/widget `iconTheme`, and theme/widget `actionsIconTheme` overrides with leading/actions context probes and expected-color summary (`src/Sample/Flutter.Net/AppBarIconThemeDemoPage.cs`, `dart_sample/lib/app_bar_icon_theme_demo_page.dart`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- Added parity runtime demo route/page for app-bar text-style precedence in both samples (`AppBar text styles`), including controls for `foreground`, theme/widget `titleTextStyle`, and theme/widget `toolbarTextStyle` overrides with expected-style summary plus themed/default preview comparison (`src/Sample/Flutter.Net/AppBarTextStylesDemoPage.cs`, `dart_sample/lib/app_bar_text_styles_demo_page.dart`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- Added resolved-toolbar-height validation guard in `AppBar` so non-finite/non-positive themed `toolbarHeight` fails fast with `ArgumentOutOfRangeException` instead of producing invalid layout behavior (`src/Flutter.Material/Scaffold.cs`).
- Expanded `MaterialScaffoldTests` with focused precedence coverage for app-bar background/foreground colors, icon theme resolution for leading/actions slots, toolbar-height precedence (`theme` and widget override), leading-width precedence (`theme` and widget override), actions-padding precedence (`theme` and widget override), and non-positive themed width/height guards (`src/Flutter.Tests/MaterialScaffoldTests.cs`).

## [2026-03-13] - M4 app-bar title layout parity

### Changed

- Extended framework `AppBar` API with Flutter-like title-layout controls: `centerTitle` and `titleSpacing` (default `16`) plus input validation that rejects negative/non-finite `titleSpacing` values (`src/Flutter.Material/Scaffold.cs`).
- Updated centered-title composition so when `leading` is present and `actions` are absent, app bar reserves a symmetric trailing slot equal to effective leading width to keep the title visually centered (`src/Flutter.Material/Scaffold.cs`).
- Added `ThemeData.Platform` and `AppBarThemeData` (`appBarTheme.centerTitle`) in `Flutter.Material`, and switched app-bar center-title default resolution to Flutter-like precedence: widget `centerTitle` -> `ThemeData.AppBarTheme.CenterTitle` -> platform fallback (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Scaffold.cs`).
- Added platform fallback parity for center-title defaults (`iOS/macOS`: centered when `actions.Count < 2`; other platforms: not centered) with focused regression coverage for theme/platform precedence (`src/Flutter.Material/Scaffold.cs`, `src/Flutter.Tests/MaterialScaffoldTests.cs`).
- Expanded `AppBarThemeData` with Flutter-like title/text style fields (`titleSpacing`, `toolbarTextStyle`, `titleTextStyle`) and updated app bar resolution precedence:
  - `titleSpacing`: widget -> theme -> default `16`,
  - `titleTextStyle` / `toolbarTextStyle`: widget -> theme -> framework defaults with app-bar foreground color (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Scaffold.cs`).
- Added `MaterialTextTheme.TitleLarge` token and switched default app-bar title fallback to this token (`titleLarge` with app-bar foreground color), closing the previously documented temporary divergence from Flutter token-path fallback (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Scaffold.cs`, `src/Flutter.Tests/MaterialScaffoldTests.cs`).
- `AppBar` now applies toolbar/title text defaults through nested framework `DefaultTextStyle` wrappers, so custom title/action widgets inherit `AppBar` text styling the same way as Flutter toolbar/title composition (`src/Flutter.Material/Scaffold.cs`).
- Added focused regression coverage in `MaterialScaffoldTests` for `titleSpacing` widget-vs-theme precedence and `titleTextStyle`/`toolbarTextStyle` widget-vs-theme precedence on rendered title/action text (`src/Flutter.Tests/MaterialScaffoldTests.cs`).
- Added focused regression coverage for title-layout behavior in `MaterialScaffoldTests`: centered-title alignment wiring, `titleSpacing` horizontal padding application, and negative `titleSpacing` guard (`src/Flutter.Tests/MaterialScaffoldTests.cs`).

## [2026-03-12] - M4 theming baseline and Material project split

### Added

- Introduced dedicated framework Material assembly `src/Flutter.Material/Flutter.Material.csproj` and wired it into `src/Flutter.Net.sln` plus dependent sample/test projects (`src/Sample/Flutter.Net/Flutter.Net.csproj`, `src/Flutter.Tests/Flutter.Tests.csproj`).
- Added initial Material theming primitives in `Flutter.Material`: `ThemeData`, `MaterialTextTheme`, and inherited `Theme` with `Theme.Of(context)` lookup and baseline text-style propagation through framework `DefaultTextStyle` (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Theme.cs`).
- Updated C# sample app bootstrap to use framework Material theming (`Theme(data: ThemeData.Light, child: ...)`) instead of sample-local `DefaultTextStyle` injection (`src/Sample/Flutter.Net/CounterApp.cs`).
- Updated Dart sample bootstrap with explicit `MaterialApp.theme` `TextTheme.bodyMedium` baseline (`14/1.43/0.25`) to keep inherited text defaults aligned with C# sample behavior (`dart_sample/lib/counter_app.dart`).
- Added regression coverage that verifies `ThemeData.TextTheme.BodyMedium` reaches `RenderParagraph` defaults via `Text` (`src/Flutter.Tests/TextWidgetTests.cs`).

## [2026-03-12] - M4 scaffold and app-bar baseline

### Added

- Added Material shell primitives in `Flutter.Material`: `Scaffold` and `AppBar` with baseline slot support (`body`, `appBar`, `floatingActionButton`, `bottomNavigationBar`, `leading`, `actions`, title text/widget, toolbar sizing) and theme-driven color defaults (`scaffoldBackgroundColor`, `primaryColor`, `onPrimaryColor`) (`src/Flutter.Material/Scaffold.cs`, `src/Flutter.Material/ThemeData.cs`).
- Updated C# sample gallery pages to use framework `Scaffold`/`AppBar` structure (menu and demo pages now render through Material shell composition instead of manual top-row title/back layout wrappers) (`src/Sample/Flutter.Net/SampleGalleryScreen.cs`).
- Updated Dart sample gallery pages to mirror the same shell structure with Flutter `Scaffold`/`AppBar` usage, preserving route/module parity (`dart_sample/lib/sample_gallery_screen.dart`).
- Added focused regression coverage for Material shell behavior in framework tests (`src/Flutter.Tests/MaterialScaffoldTests.cs`): scaffold background resolution, app-bar theme color resolution, and app-bar title foreground propagation.

## [2026-03-12] - M4 first Material button set baseline

### Added

- Added first Material button set in `Flutter.Material`: `TextButton`, `ElevatedButton`, and `OutlinedButton` with Flutter-like API shape (`child`, `onPressed`, color/padding/radius overrides), default theme resolution, and disabled-state color treatment for foreground/background/border (`src/Flutter.Material/Buttons.cs`).
- Extended sample gallery route map with a dedicated Material buttons demo page in both C# and Dart samples (`src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`, `dart_sample/lib/material_buttons_demo_page.dart`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`).
- Added focused regression coverage for Material button defaults and disabled styling in framework tests (`src/Flutter.Tests/MaterialButtonsTests.cs`).

## [2026-03-12] - M4 material button interaction polish

### Added

- Added initial interactive-state behavior for framework Material buttons: pointer-pressed visual state, focus visual treatment, and keyboard activation handling for `Enter/Return/Space` through `Focus` integration in `MaterialButtonCore` (`src/Flutter.Material/Buttons.cs`).
- Added focused regression coverage for pressed-state visual transitions (`pointer down`/`pointer up`) in Material buttons (`src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added protected `State.StateWidget` helper in framework core so stateful widgets in external assemblies (for example `src/Flutter.Material`) can read their current widget instance without relying on framework-internal fields (`src/Flutter/Widgets/Framework.Widget.cs`).

### Changed

- Updated C# sample gallery shell controls to use Material buttons instead of sample-local `CounterTapButton` for menu entries and demo-page back action (`src/Sample/Flutter.Net/SampleGalleryScreen.cs`).
- Updated Dart sample gallery shell controls to mirror the same Material-button shell behavior for parity (`dart_sample/lib/sample_gallery_screen.dart`).
- Updated Material buttons demo control-strip actions (`Enabled`/`Reset`) in both C# and Dart samples to use `TextButton` instead of `CounterTapButton` (`src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`, `dart_sample/lib/material_buttons_demo_page.dart`).
- Fixed Material button layout behavior in unbounded vertical constraints by switching internal label centering to shrink-wrapped alignment (`Align` with `widthFactor/heightFactor`), preventing button rows in `Column`/`Row` compositions from expanding to effectively infinite height (`src/Flutter.Material/Buttons.cs`).
- Strict parity pass for Material button defaults/state layer behavior based on Flutter source references (`text_button.dart`, `elevated_button.dart`, `outlined_button.dart`): baseline minimum size is now `64x40`, default shape moved to pill/stadium-like radius, state-layer pressed/focused overlay is normalized to `0.10`, and focus-specific border-thickening heuristics were removed in favor of Flutter-like overlay-driven feedback (`src/Flutter.Material/Buttons.cs`).
- Continued strict parity pass for button theming tokens and defaults:
  - `ThemeData` now includes `onSurfaceColor`, `outlineColor`, and `surfaceContainerLowColor`,
  - `ElevatedButton` default colors now follow Material-like surface-container/primary pairing with disabled colors derived from `onSurface`,
  - `OutlinedButton` default border now resolves from `outlineColor`, while default foreground resolves from `primary`,
  - disabled border/foreground resolution now uses explicit theme tokens instead of ad hoc alpha from the active colors (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/Buttons.cs`).
- Added hover-state infrastructure for framework pointer listeners:
  - introduced `PointerEnterEvent` and `PointerExitEvent`,
  - extended `Listener`/`RenderPointerListener` with `onPointerEnter`/`onPointerExit`,
  - added hover enter/exit transition dispatch in `GestureBinding` by tracking per-pointer hover hit-test paths (`src/Flutter/UI/PointerEvents.cs`, `src/Flutter/Widgets/Gestures.cs`, `src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter/Gestures/GestureBinding.cs`).
- Material buttons now consume framework hover enter/exit events and apply Flutter-like hover state-layer opacity (`0.08`) with regression coverage (`src/Flutter.Material/Buttons.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Fixed Material button pointer-activation focus overlay stickiness: pointer clicks now suppress focus state-layer tint after `PointerUp` (so buttons do not stay visually pressed), while keyboard activation re-enables focus overlay behavior; added regression coverage for pointer-click focus interaction (`src/Flutter.Material/Buttons.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Improved Material ink/ripple visibility on wide buttons: splash alpha now holds through most of the expansion phase (tail fade only), preventing visual "cutoff near text" perception before ripple reaches button bounds; added layout regression coverage ensuring `RenderInkSplash` expands to full tight button width (`src/Flutter.Material/Buttons.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Fixed resize-driven stale ink clip bounds: `RenderClipRect`/`RenderClipRRect` now mark composited-layer updates when implicit clip size changes during layout, so ripple clip area tracks current button size after viewport/screen resize; added regression coverage in `LayerV2Tests` (`src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter.Tests/LayerV2Tests.cs`).
- Added initial `ButtonStyle` infrastructure for Material buttons:
  - introduced `MaterialState`, `MaterialStateProperty<T>`, and `ButtonStyle` (`src/Flutter.Material/ButtonStyle.cs`),
  - `TextButton`/`ElevatedButton`/`OutlinedButton` now accept `style` and resolve foreground/background/overlay/splash/side/padding/shape/min-size via state-aware style resolution in `MaterialButtonCore`,
  - existing constructor color/shape/padding overrides remain supported as legacy compatibility overrides,
  - added regression coverage for style-driven foreground/min-size/side behavior in `MaterialButtonsTests` (`src/Flutter.Material/Buttons.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Extended Material button style API with `StyleFrom(...)` builders on `TextButton`, `ElevatedButton`, and `OutlinedButton`, including disabled-state color overrides, text-style forwarding, and explicit legacy-parameter-vs-style precedence regression coverage (`src/Flutter.Material/Buttons.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Matched Flutter overlay conflict precedence for Material button state layers: `pressed` now wins over `hovered/focused`, and `hovered` wins over `focused`; added regression coverage for combined-state conflicts (`TextButton_PressedOverlayTakesPriorityOverHoverOverlay`, `TextButton_PressedOverlayTakesPriorityOverFocusOverlay`, `TextButton_HoverOverlayTakesPriorityOverFocusOverlay`) (`src/Flutter.Material/Buttons.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Continued `StyleFrom(...)` parity with Flutter defaults:
  - `foregroundColor` in `StyleFrom(...)` now derives default overlay and splash state colors when explicit `overlayColor`/`splashColor` are omitted,
  - explicit `overlayColor` now follows Flutter-like state opacities (`pressed/focused: 0.10`, `hovered: 0.08`) and drives splash fallback when `splashColor` is omitted, including `Colors.Transparent` defeating highlight/splash visuals,
  - splash color now follows the same overlay-state resolution and is captured at splash start (matching InkWell behavior where ripple color does not change after press state transitions),
  - removed default-style `SplashColor` overrides in button defaults and rely on overlay-driven splash fallback, so direct `ButtonStyle.OverlayColor` also controls ripple tint when `SplashColor` is unspecified (matching Flutter InkWell precedence),
  - keyboard activation now applies a transient pressed state layer (`~100ms`) before fallback to focused state, aligning button visuals with Flutter `InkWell.activateOnIntent` behavior instead of showing focus-only overlay during keyboard-triggered tap,
  - activation-key filtering now mirrors Flutter shortcut semantics more closely: `NumPadEnter` is treated as activation, while modified chords (`Ctrl/Alt/Meta/Shift + Space/Enter`) are ignored instead of firing button taps,
  - `ThemeData` now supports button-style overrides (`textButtonStyle`, `elevatedButtonStyle`, `outlinedButtonStyle`) and button style composition now resolves with Flutter-like precedence `default -> theme -> widget -> legacy` (highest),
  - host keyboard dispatch now forwards `KeyUp` events into framework focus handling (`FlutterHost.OnKeyUp` -> `FocusManager.HandleKeyEvent`) so controls can react to full key down/up chains in runtime,
  - aligned `ButtonStyle.Merge(...)` with Flutter semantics (current style keeps non-null fields, argument fills only null fields),
  - moved per-state null-fallback layering for button visuals into internal style composition (`MaterialButtonCore.ComposeStyles(...)`) so default disabled tokens still apply when higher-priority style resolvers return null,
  - fixed overlay application semantics so state-layer tint is applied only for interactive states (`pressed/hovered/focused`) and not at idle, with regression coverage in `TextButton_ButtonStyleOverlayAll_DoesNotTintAtRest_ButAppliesOnHover`,
  - extended cross-button parity regression coverage for style-state behavior to `ElevatedButton`/`OutlinedButton` (overlay opacity/priority, transparent overlay suppression, and per-state resolver-null fallback for foreground/background/side) in addition to `TextButton`,
  - added/updated regression coverage in `ButtonStyle_Merge_FillsNullFields_FromArgument_WithoutOverridingExisting`, `TextButton_ThemeStyleForegroundOverridesDefault`, `TextButton_WidgetStyleForegroundOverridesThemeStyle`, `TextButton_LegacyForeground_OverridesWidgetAndThemeStyle`, `ElevatedButton_ThemeStyleBackgroundOverridesDefault`, `OutlinedButton_ThemeStyleSideOverridesDefault`, `ElevatedButton_ThemeStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay`, `TextButton_StyleFrom_ForegroundColor_DerivesOverlayAndSplash`, `TextButton_StyleFrom_TransparentOverlay_DisablesVisualHighlights`, `TextButton_StyleFrom_OverlayColor_UsesStateOpacitiesAndSplashFallback`, `TextButton_SplashColor_RemainsActivationTint_AfterPointerUp`, `TextButton_StyleFrom_ForegroundOnly_DisabledFallsBackToThemeDisabledForeground`, `ElevatedButton_StyleFrom_BackgroundOnly_DisabledFallsBackToThemeDisabledBackground`, `TextButton_StyleFrom_DisabledForegroundOnly_PreservesEnabledThemeForeground`, `ElevatedButton_StyleFrom_DisabledBackgroundOnly_PreservesEnabledThemeBackground`, `TextButton_ButtonStyleForegroundResolverNullForEnabled_FallsBackToDefaultEnabledColor`, `ElevatedButton_ButtonStyleForegroundResolverNullForEnabled_FallsBackToDefaultEnabledColor`, `OutlinedButton_ButtonStyleForegroundResolverNullForEnabled_FallsBackToDefaultEnabledColor`, `ElevatedButton_ButtonStyleBackgroundResolverNullForDisabled_FallsBackToDefaultDisabledBackground`, `OutlinedButton_ButtonStyleSideResolverNullForEnabled_FallsBackToDefaultEnabledSide`, `OutlinedButton_ButtonStyleSideResolverNullForDisabled_FallsBackToDefaultDisabledSide`, `TextButton_ButtonStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay`, `ElevatedButton_ButtonStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay`, `OutlinedButton_ButtonStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay`, `TextButton_ButtonStyleOverlayWithoutSplash_UsesOverlayForSplash`, `ElevatedButton_ButtonStyleOverlayWithoutSplash_UsesOverlayForSplash`, `OutlinedButton_ButtonStyleOverlayWithoutSplash_UsesOverlayForSplash`, `TextButton_KeyboardActivation_UsesPressedOverlay_AndInvokesOnPressedOnKeyDownOnly`, `TextButton_KeyboardActivation_NumPadEnter_InvokesOnPressed`, `TextButton_KeyboardActivation_IgnoresModifiedSpaceChord`, `FlutterHost_KeyDownAndKeyUp_AreDispatchedToPrimaryFocusNode`, `ElevatedButton_StyleFrom_OverlayColor_UsesHoverOpacityAndPressedPriority`, and `OutlinedButton_StyleFrom_TransparentOverlay_HasNoIdleTint_AndNoSplash` (`src/Flutter.Material/ButtonStyle.cs`, `src/Flutter.Material/Buttons.cs`, `src/Flutter.Material/ThemeData.cs`, `src/Flutter/FlutterHost.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`, `src/Flutter.Tests/FlutterHostInputTests.cs`).
- Added inherited local button theme wrappers in `Flutter.Material` (`TextButtonTheme`, `ElevatedButtonTheme`, `OutlinedButtonTheme` plus `*ThemeData`) and switched button theme-style resolution to `*ButtonTheme.Of(context).Style` for Flutter-like subtree override semantics; local wrappers now override `ThemeData` styles per button type and can intentionally clear inherited `ThemeData` button styles when local `Style` is `null` (`src/Flutter.Material/ButtonThemes.cs`, `src/Flutter.Material/Buttons.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Aligned `ThemeData` with Flutter-style button-theme data objects by adding top-level `textButtonTheme`/`elevatedButtonTheme`/`outlinedButtonTheme` (`*ThemeData`) while preserving compatibility with existing legacy `*ButtonStyle` properties; `*ButtonTheme.Of(context)` now resolves through `ThemeData.*ButtonTheme` and explicit theme-data objects take precedence over legacy style fields when both are provided (`src/Flutter.Material/ThemeData.cs`, `src/Flutter.Material/ButtonThemes.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Expanded `ButtonStyle` size-constraint parity with Flutter by adding `fixedSize` and `maximumSize` (`MaterialStateProperty<Size?>`) support, extending `StyleFrom(...)` builders to accept them, and updating `MaterialButtonCore` constraint resolution to follow Flutter order (`minimumSize`/`maximumSize` base constraints, then `fixedSize` per finite axis) including infinite-axis `fixedSize` behavior (`src/Flutter.Material/ButtonStyle.cs`, `src/Flutter.Material/Buttons.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Aligned `minimumSize` validation semantics with Flutter constraints by allowing `0` for width/height (still rejecting negative/NaN/infinite values), with regression coverage for zero-min-size acceptance and negative-size rejection (`src/Flutter.Material/Buttons.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added `ButtonStyle.alignment` parity support for Material buttons (including `StyleFrom(...)` builders and style-layer composition order), and switched internal label `Align` to resolve from style with default center fallback; added regression coverage for default/theme/widget precedence (`src/Flutter.Material/ButtonStyle.cs`, `src/Flutter.Material/Buttons.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Aligned `ButtonStyle.textStyle` closer to Flutter by making it state-aware (`MaterialStateProperty<TextStyle?>`), updating style composition to resolve per-state with layer fallback, and extending regression coverage for resolver-null fallback from widget layer to theme layer in disabled state (`src/Flutter.Material/ButtonStyle.cs`, `src/Flutter.Material/Buttons.cs`, `src/Flutter.Tests/MaterialButtonsTests.cs`).
- Added ink/ripple baseline for Material buttons:
  - new `RenderInkSplash` paint primitive with animated radial splash progress and pointer-origin support,
  - new widget-level wrapper `InkSplash`,
  - `MaterialButtonCore` now starts animated splash on pointer/keyboard activation (`src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter/Widgets/Basic.cs`, `src/Flutter/Rendering/Object.PaintingContext.cs`, `src/Flutter.Material/Buttons.cs`).
- Closed rounded-clipping follow-up for Material ripple parity:
  - added framework rounded-clip primitives (`ClipRRectLayer`, `ClipRRectOffsetLayer`, `RenderClipRRect`, `ClipRRect`, and `PaintingContext.PushClipRRect`),
  - `MaterialButtonCore` now wraps `InkSplash` in `ClipRRect` using button border radius, and splash internal rectangular clip is disabled,
  - added regression coverage for rounded clip integration in `MaterialButtonsTests`, `LayerV2Tests`, and `BasicWidgetProxyTests`.

## [2026-03-12] - Post-M3 typography and visual parity hardening

### Added

- Post-M3 typography parity hardening: expanded `Text`/`RenderParagraph` support with `fontWeight`, `fontStyle`, `height` (line-height multiplier), and `letterSpacing`; aligned fallback text-size estimation to these options; and switched paragraph/button/editable-text layout defaults to host font family instead of hardcoded `Segoe UI` for closer Dart-sample visual parity across platforms (`src/Flutter/Widgets/Text.cs`, `src/Flutter/RenderParagraph.cs`, `src/Flutter/UI/TextLayoutFallback.cs`, `src/Flutter/RenderButton.cs`, `src/Flutter/Widgets/TextInput.cs`, `src/Flutter.Tests/TextWidgetTests.cs`).
- Text-style inheritance parity hardening: added framework `TextStyle` + `DefaultTextStyle` inheritance and updated `Text` to resolve typography from inherited defaults (`fontFamily`, `fontSize`, `color`, `fontWeight`, `fontStyle`, `height`, `letterSpacing`) before applying local overrides, matching Flutter `TextStyle(inherit: true)` behavior more closely (`src/Flutter/Widgets/DefaultTextStyle.cs`, `src/Flutter/Widgets/Text.cs`, `src/Flutter.Tests/TextWidgetTests.cs`).
- C# sample typography baseline parity: wrapped sample root with a Material-like `DefaultTextStyle` (`fontSize: 14`, `height: 1.43`, `letterSpacing: 0.25`, macOS `.AppleSystemUIFont`) to reduce menu text wrapping/line-height differences against the Dart `MaterialApp` sample (`src/Sample/Flutter.Net/CounterApp.cs`).
- Paragraph alignment parity hardening: `RenderParagraph` now tightens loose-width center/right/end layouts to text content width when Avalonia reports internal positive glyph offset, preventing right-shifted paint in intrinsic-width button/list labels (notably `Counter` -> `Keyed List` rows) while preserving tight-width alignment behavior (`src/Flutter/RenderParagraph.cs`).
- Counter page overflow parity hardening: wrapped `CounterScreen` content in `SingleChildScrollView` on both C# and Dart samples to prevent bottom `RenderFlex` overflow debug stripes on smaller viewport heights while keeping route/module parity intact (`src/Sample/Flutter.Net/CounterScreen.cs`, `dart_sample/lib/counter_screen.dart`).
- Flutter-style overflow debug indicator parity hardening: `RenderFlex` now mirrors Flutter debug overflow geometry more closely by clipping overflowing child paint, drawing 45-degree yellow/black edge markers, and placing rotated/edge-aligned labels (`BOTTOM/RIGHT OVERFLOWED BY ... PIXELS`) with Flutter-like font sizing/weight and value formatting; added regression coverage and a dedicated overflow-indicator demo route/page in both C# and Dart samples (`src/Flutter/Rendering/Flex.RenderFlex.cs`, `src/Flutter.Tests/RenderingParityTests.cs`, `src/Sample/Flutter.Net/OverflowIndicatorDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/overflow_indicator_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`).
- Desktop host sizing parity hardening: `FlutterExtensions.Run` now interprets target startup size as physical pixels and converts to DIP using top-level scale (`RenderScaling` with `DesktopScaling` fallback), keeping C# desktop window width/height closer to Dart macOS sample on high-DPI displays (`src/Flutter/FlutterExtensions.cs`).
- Dart macOS host sizing parity hardening: `MainFlutterWindow` now treats startup target size as physical pixels and converts to Cocoa points via `backingScaleFactor`, matching C# startup-size calculation semantics on Retina displays (`dart_sample/macos/Runner/MainFlutterWindow.swift`).

## [2026-03-12] - Roadmap sequencing update

### Changed

- Roadmap sequencing update (2026-03-12): framework planning now treats Material library rewrite as active milestone `M4` (`in_progress`) with focus on theming + Material shell/controls, while the previous cross-host parity/stability milestone is moved to the final phase as `M5` (`planned`) due current host-toolchain alignment blockers documented in `docs/FRAMEWORK_PLAN.md`; task-entry guidance in `docs/ai/MODULE_INDEX.md` now points to M4-first context.

## [2026-03-11] - M3 completion snapshot

### Added

- Closed milestone M3 (`Port-first widget set expansion`) and moved framework planning focus to post-M3 control parity hardening.
- Text-rendering parity hardening: added widget-level `Text` layout options (`textAlign`, `softWrap`, `maxLines`, `overflow`, `textDirection`) and corresponding `RenderParagraph` support with unbounded-width layout parity (removed synthetic `maxWidth=1000` clamp), plus regression coverage (`src/Flutter/UI/Text.cs`, `src/Flutter/Widgets/Text.cs`, `src/Flutter/RenderParagraph.cs`, `src/Flutter.Tests/TextWidgetTests.cs`).
- Sample parity progression: aligned centered text rendering behavior in C# sample where Dart sample already used `TextAlign.center` (`src/Sample/Flutter.Net/CounterWidgets.cs`, `src/Sample/Flutter.Net/UnconstrainedLimitedBoxDemoPage.cs`, `docs/ai/PARITY_MATRIX.md`).
- M3 `Offstage` baseline: added framework widget/render support (`Offstage`, `RenderOffstage`) with offstage layout semantics (child is laid out while parent takes smallest size, painting/hit-testing/semantics participation suppressed when offstage), plus regression coverage for render behavior and widget update wiring (`src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter/Widgets/Basic.cs`, `src/Flutter.Tests/OffstageTests.cs`).
- M3 sample parity progression: added `Offstage` demo route/page in both C# and Dart galleries for interactive offstage toggle and zero-space row-layout checks (`src/Sample/Flutter.Net/OffstageDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/offstage_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- M3 `OverflowBox` + `SizedOverflowBox` baseline: added framework widget/render support (`OverflowBox`, `SizedOverflowBox`, `RenderConstrainedOverflowBox`, `RenderSizedOverflowBox`, `OverflowBoxFit`) with overflow-alignment behavior and fit modes (`max`, `deferToChild`), plus regression coverage for render sizing/alignment and widget update wiring (`src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter/Widgets/Basic.cs`, `src/Flutter.Tests/OverflowBoxTests.cs`).
- M3 sample parity progression: added `OverflowBox + SizedOverflowBox` demo route/page in both C# and Dart galleries for interactive fit/alignment and constraint-override checks (`src/Sample/Flutter.Net/OverflowBoxDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/overflow_box_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- M3 `UnconstrainedBox` + `LimitedBox` baseline: added framework widget/render support (`UnconstrainedBox`, `LimitedBox`, `RenderUnconstrainedBox`, `RenderLimitedBox`) with axis-specific unconstraining, unbounded-axis max clamping, and regression coverage for render sizing/alignment and widget update wiring (`src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter/Widgets/Basic.cs`, `src/Flutter.Tests/UnconstrainedLimitedBoxTests.cs`).
- M3 sample parity progression: added `UnconstrainedBox + LimitedBox` demo route/page in both C# and Dart galleries for interactive constrained-axis and `LimitedBox` max-constraint checks (`src/Sample/Flutter.Net/UnconstrainedLimitedBoxDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/unconstrained_limited_box_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- M3 `FittedBox` baseline: added `BoxFit` sizing utilities (`BoxFit`, `FittedSizes`, `ApplyBoxFit`), `BoxConstraints` aspect-ratio helpers (`Loosen`, `ConstrainSizeAndAttemptToPreserveAspectRatio`), and framework widget/render support (`FittedBox` + `RenderFittedBox`) with transform-aware paint/hit-test alignment (`src/Flutter/Rendering/BoxFit.cs`, `src/Flutter/Rendering/Box.cs`, `src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter/Widgets/Basic.cs`, `src/Flutter.Tests/FittedBoxTests.cs`).
- M3 sample parity progression: added `FittedBox` demo route/page in both C# and Dart galleries for interactive `BoxFit` and alignment checks (`src/Sample/Flutter.Net/FittedBoxDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/fitted_box_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- M3 `FractionallySizedBox` baseline: added framework widget/render support (`FractionallySizedBox` + `RenderFractionallySizedBox`) with alignment-aware child positioning and fractional tight-constraint application on bounded axes, plus regression coverage for factor sizing and widget update wiring (`src/Flutter/Widgets/Basic.cs`, `src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter.Tests/FractionallySizedBoxTests.cs`).
- M3 sample parity progression: added `FractionallySizedBox` demo route/page in both C# and Dart galleries for interactive width/height factor and alignment checks (`src/Sample/Flutter.Net/FractionallySizedBoxDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/fractionally_sized_box_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- M3 `AspectRatio` + `Spacer` baseline: added `RenderAspectRatio` with bounded-axis ratio layout resolution, widget-level `AspectRatio`/`Spacer` APIs, and regression coverage for render sizing, widget update wiring, and flex parent-data propagation (`src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter/Widgets/Basic.cs`, `src/Flutter.Tests/AspectRatioTests.cs`).
- M3 sample parity progression: added `AspectRatio + Spacer` route/page in both C# and Dart sample galleries for interactive ratio and flex-gap checks (`src/Sample/Flutter.Net/AspectRatioDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/aspect_ratio_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- M3 spacer-visibility regression hardening: updated `AspectRatio + Spacer` parity demo to use two spacers with asymmetric flex (`_spacerFlex` vs fixed `1`) and a middle marker, so flex changes are visually observable; added render-level regression test validating proportional main-axis distribution between two spacer slots (`src/Sample/Flutter.Net/AspectRatioDemoPage.cs`, `dart_sample/lib/aspect_ratio_demo_page.dart`, `src/Flutter.Tests/AspectRatioTests.cs`).
- M3 proxy widget baseline: added framework widget wrappers for `RenderOpacity`, `RenderTransform`, and `RenderClipRect` (`Opacity`, `Transform`, `ClipRect`) with regression coverage validating widget-to-render wiring and render-object property updates through rebuilds (`src/Flutter/Widgets/Basic.cs`, `src/Flutter.Tests/BasicWidgetProxyTests.cs`).
- M3 sample parity progression: added a new Proxy widgets route/page in both C# and Dart samples to validate interactive composition with `Opacity`, `Transform`, and `ClipRect` (`src/Sample/Flutter.Net/ProxyWidgetsDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/proxy_widgets_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- M3 proxy demo UX tuning: increased opacity step contrast, added explicit `Opacity 0`/`Opacity 1` controls, and switched demo layer to high-contrast black-on-white visual so opacity changes are immediately visible in sample interaction (`src/Sample/Flutter.Net/ProxyWidgetsDemoPage.cs`, `dart_sample/lib/proxy_widgets_demo_page.dart`).
- Compositing pipeline fix: preserve and apply repaint-boundary composited-layer property updates even when repaint and layer-property invalidation happen in the same frame, preventing dropped `Opacity/Transform/ClipRect` layer updates under concurrent paint dirtiness; added regression coverage for combined repaint + layer update flow (`src/Flutter/Rendering/Object.RenderObject.cs`, `src/Flutter/PipelineOwner.cs`, `src/Flutter.Tests/CompositingLayerTests.cs`).
- M3 alignment baseline: added `Alignment` + `RenderAlign` and framework widgets `Align`/`Center` with shrink-factor layout support, regression coverage for alignment offset behavior and widget update wiring, plus a parity sample route/page in both C# and Dart galleries (`src/Flutter/Rendering/Alignment.cs`, `src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter/Widgets/Basic.cs`, `src/Flutter.Tests/AlignTests.cs`, `src/Sample/Flutter.Net/AlignDemoPage.cs`, `dart_sample/lib/align_demo_page.dart`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- M3 overlay layout baseline: added `RenderStack`/`StackParentData` with framework widgets `Stack`/`Positioned` (including positioned inset/size resolution), regression coverage for render-layout and widget parent-data updates, plus parity sample route/page in both C# and Dart galleries (`src/Flutter/Rendering/Stack.RenderStack.cs`, `src/Flutter/Widgets/Basic.cs`, `src/Flutter.Tests/StackTests.cs`, `src/Sample/Flutter.Net/StackDemoPage.cs`, `dart_sample/lib/stack_demo_page.dart`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- M3 decoration baseline: added `BoxDecoration`, `BorderSide`, and `BorderRadius` value objects with new `RenderDecoratedBox` + `DecoratedBox` widget (and `Container.decoration` support), regression coverage for render/layout and widget update behavior, plus parity sample route/page in both C# and Dart galleries (`src/Flutter/Rendering/Decoration.cs`, `src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter/Widgets/Basic.cs`, `src/Flutter.Tests/DecoratedBoxTests.cs`, `src/Sample/Flutter.Net/DecoratedBoxDemoPage.cs`, `dart_sample/lib/decorated_box_demo_page.dart`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- M3 `Container` composition baseline: extended `Container` with `alignment` and `margin` composition support (wrapping via `Align` and outer `Padding`) with regression coverage for render-object wiring and wrapper order, plus parity sample route/page in both C# and Dart galleries (`src/Flutter/Widgets/Basic.cs`, `src/Flutter.Tests/ContainerTests.cs`, `src/Sample/Flutter.Net/ContainerDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/container_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- M3 `Container` expansion: added `constraints` and `transform` support with Flutter-like width/height tightening against provided constraints, regression coverage for constrained-box wiring and wrapper order (`Transform` outside `margin`), and upgraded parity demo flow in both C# and Dart galleries (`src/Flutter/Widgets/Basic.cs`, `src/Flutter.Tests/ContainerTests.cs`, `src/Sample/Flutter.Net/ContainerDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/container_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `docs/ai/PARITY_MATRIX.md`).

## [2026-03-10] - M1/M2 completion snapshot

### Added

- Lifecycle parity hardening: added element reconciliation tests for mixed keyed/unkeyed updates, including nested multi-parent reorder scenarios, to verify keyed state retention, stable-tail reuse, and disposal of moved unkeyed states (`src/Flutter.Tests/ElementLifecycleTests.cs`).
- Scroll parity hardening: fixed `RenderSliverPadding` child paint offset to avoid double-applying scroll offset (preventing viewport gaps when padded slivers are scrolled), and added regression coverage for `Scrollable` + `ListView.Separated` viewport continuity during controller jumps (`src/Flutter/Rendering/Sliver.cs`, `src/Flutter.Tests/ScrollPipelineTests.cs`).
- Rendering parity hardening: `RenderObject.Layout` no longer swallows layout exceptions, with regression coverage that verifies exception propagation and dirty-state preservation after failed layout; added text-layout fallback sizing for host-less/font-manager-less environments used by framework tests (`src/Flutter/Rendering/Object.RenderObject.cs`, `src/Flutter.Tests/RenderingParityTests.cs`, `src/Flutter/RenderParagraph.cs`, `src/Flutter/RenderButton.cs`, `src/Flutter/UI/TextLayoutFallback.cs`).
- Constraints parity hardening: `BoxConstraints.Tighten(width/height)` now clamps requested values to the existing min/max range, with regression coverage for out-of-range tighten requests (`src/Flutter/Rendering/Box.cs`, `src/Flutter.Tests/RenderingParityTests.cs`).
- Proxy invalidation parity hardening: `RenderConstrainedBox.AdditionalConstraints` and `RenderPadding.Padding` no longer trigger relayout on no-op assignments, with regression tests validating no extra parent relayout passes on unchanged values (`src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter.Tests/RenderingParityTests.cs`).
- Invalidation and compositing parity hardening: no-op `RenderFlex` property updates (`Direction`, `MainAxisSize`, `MainAxisAlignment`, `CrossAxisAlignment`) and no-op `RenderColoredBox.Color` updates no longer trigger redundant layout/paint work; `RenderView.ReplaceRootLayer` now skips repaint when reusing the same root layer (`src/Flutter/Rendering/Flex.RenderFlex.cs`, `src/Flutter/Rendering/Proxy.RenderBox.cs`, `src/Flutter/RenderView.cs`, `src/Flutter.Tests/RenderingParityTests.cs`, `src/Flutter.Tests/CompositingLayerTests.cs`).
- M2 kickoff: added framework-level keyboard/focus baseline (`KeyEvent`, `FocusNode`, `FocusManager`, `Focus` widget) with host key dispatch integration and regression coverage for focus ownership, autofocus, callback handling, and tab traversal (`src/Flutter/UI/KeyboardEvents.cs`, `src/Flutter/Widgets/Focus.cs`, `src/Flutter/FlutterHost.cs`, `src/Flutter.Tests/FocusTests.cs`).
- M2 focus scope progression: added `FocusScopeNode` and `FocusScope` widget wiring so focus nodes register to nearest scope and traversal remains bounded to the active scope (Tab + directional keys), with regression coverage for scope-local forward/reverse boundaries and directional flow (`src/Flutter/Widgets/Focus.cs`, `src/Flutter.Tests/FocusTests.cs`).
- M2 directional focus policy progression: directional traversal now uses geometry-aware candidate selection when focus nodes expose traversal bounds (explicit `TraversalRect` or attached render-box geometry), with deterministic fallback to sequential traversal when geometry is unavailable (`src/Flutter/Widgets/Focus.cs`, `src/Flutter.Tests/FocusTests.cs`).
- M2 editable text/IME baseline progression: added `TextEditingController` + `EditableText` widget and host text-input dispatch (`FlutterHost.OnTextInput -> FocusManager.HandleTextInput`), with regression coverage for focused text delivery, disabled behavior, backspace editing, and `onChanged` callbacks (`src/Flutter/Widgets/TextInput.cs`, `src/Flutter/Widgets/Focus.cs`, `src/Flutter/FlutterHost.cs`, `src/Flutter.Tests/TextInputTests.cs`).
- M2 text-editing progression: added controller-level `TextSelection`/`TextRange` state and selection-aware editing primitives (caret movement, selection expansion, replace-selection insert, backward/forward delete, and `Ctrl/Meta+A` select-all) with editable-widget key handling and regression coverage (`src/Flutter/Widgets/TextInput.cs`, `src/Flutter.Tests/TextInputTests.cs`).
- M2 composition lifecycle progression: added focus-level text composition callbacks (`OnTextComposition`) and focus-manager composition dispatch (`HandleTextCompositionUpdate`/`HandleTextCompositionCommit`), integrated controller composing primitives (`SetComposing`, `CommitComposing`, `ClearComposing`) into `EditableText`, and added regression coverage for composition update/commit/cancel flows (`src/Flutter/Widgets/Focus.cs`, `src/Flutter/Widgets/TextInput.cs`, `src/Flutter.Tests/TextInputTests.cs`).
- M2 host IME progression: wired Avalonia text-input client requests to `FlutterHost` (`TextInputMethodClientRequestedEvent`) and bridged preedit updates (`TextInputMethodClient.SetPreeditText`) into framework composition routing (`FocusManager.HandleTextCompositionUpdate`), with regression coverage validating host client provisioning and focused editable preedit flow (`src/Flutter/FlutterHost.cs`, `src/Flutter.Tests/TextInputTests.cs`).
- M2 IME state-sync progression: added focused text-input state callbacks on `FocusNode` (`OnTextInputState`, `OnTextSelectionChanged`) and wired `FlutterHost` text-input client to expose `SurroundingText`, selection getter/setter, and cursor rectangle from focused `EditableText` state, with regression coverage for host-side selection sync and cursor geometry availability (`src/Flutter/Widgets/Focus.cs`, `src/Flutter/Widgets/TextInput.cs`, `src/Flutter/FlutterHost.cs`, `src/Flutter.Tests/TextInputTests.cs`).
- M2 multiline/caret progression: added `EditableText.Multiline` workflow (`Enter/Return` newline insertion, `ArrowUp/ArrowDown` vertical caret movement with selection extension), upgraded caret rectangle computation to `TextLayout.HitTestTextPosition`-based geometry (with deterministic host-less fallback), and added regression coverage for multiline navigation behavior (`src/Flutter/Widgets/TextInput.cs`, `src/Flutter.Tests/TextInputTests.cs`).
- M2 text-editing ergonomics progression: added word-level controller and key-handler shortcuts in `EditableText` (`Ctrl/Alt + ArrowLeft/ArrowRight` for word navigation and `Ctrl/Alt + Backspace/Delete` for word deletion), with regression coverage for controller APIs and focused key-path behavior (`src/Flutter/Widgets/TextInput.cs`, `src/Flutter.Tests/TextInputTests.cs`).
- M2 paragraph-navigation progression: added multiline paragraph-level caret shortcuts in `EditableText` (`Ctrl/Alt + ArrowUp/ArrowDown`) backed by controller paragraph boundary movement APIs, with regression coverage for controller behavior and focused key-path selection extension (`src/Flutter/Widgets/TextInput.cs`, `src/Flutter.Tests/TextInputTests.cs`).
- M2 focus traversal progression: directional focus geometry resolution now applies render-object transform chains (including `RenderTransform`) when deriving traversal rects from attached render boxes, with regression coverage for transformed directional navigation outcomes (`src/Flutter/Widgets/Focus.cs`, `src/Flutter.Tests/FocusTests.cs`).
- M2 accessibility documentation progression: documented host accessibility bridge expectations (semantics tree consumption, action routing, and per-host integration targets) for desktop/web/mobile hosts (`docs/ai/accessibility-2026-03-10-host-bridge-expectations.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/TEST_MATRIX.md`).
- M2 clipboard/action progression: added editable keyboard actions for copy/cut/paste (`Ctrl/Meta + C/X/V`) via framework clipboard cache plus host clipboard synchronization hooks in `FlutterHost`, with regression coverage for editable shortcut behavior (`src/Flutter/Widgets/TextClipboard.cs`, `src/Flutter/Widgets/TextInput.cs`, `src/Flutter/FlutterHost.cs`, `src/Flutter.Tests/TextInputTests.cs`).
- M2 grapheme editing progression: caret movement and delete operations in `TextEditingController` now advance by grapheme cluster boundaries (text elements) instead of UTF-16 code unit steps, with regression coverage for emoji ZWJ and combining-mark sequences (`src/Flutter/Widgets/TextInput.cs`, `src/Flutter.Tests/TextInputTests.cs`).
- M2 host semantics runtime progression: added `FlutterHost` semantics bridge surface (`SemanticsRoot`, `SemanticsUpdated`, `PerformSemanticsAction`, and test flush helper) and regression coverage for host-level semantics update notification + action dispatch flow (`src/Flutter/FlutterHost.cs`, `src/Flutter.Tests/FlutterHostSemanticsTests.cs`).
- Sample parity update: added `EditableText` demo route/page in both C# sample and `dart_sample` gallery menus, including baseline input flow (`enable/disable`, `clear`, change summary) to validate framework text-input integration in app context (`src/Sample/Flutter.Net/EditableTextDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/editable_text_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `dart_sample/lib/sample_routes.dart`, `docs/ai/PARITY_MATRIX.md`).
- Sample parity progression: upgraded the `EditableText` demo in both C# and Dart samples to showcase multiline behavior (newline insertion, vertical caret travel hints, seeded multiline content) and escaped live value visualization for IME/manual input checks (`src/Sample/Flutter.Net/EditableTextDemoPage.cs`, `src/Sample/Flutter.Net/SampleGalleryScreen.cs`, `dart_sample/lib/editable_text_demo_page.dart`, `dart_sample/lib/sample_gallery_screen.dart`, `docs/ai/PARITY_MATRIX.md`).
- Dart source traceability: annotated all solution-tracked C# files with `Dart parity source (reference)` comments to speed up Dart-to-C# parity review and future porting iterations.

## [2026-03-08] - Baseline framework snapshot

### Added

- Flutter-like widget framework core:
  - `Widget`, `Element`, `BuildContext`, `State`, `StatelessWidget`, `StatefulWidget`.
  - `BuildOwner` scheduling and rebuild flow with dirty element processing.
  - Inherited primitives: `InheritedWidget`, `InheritedModel`, `InheritedNotifier`.
- Frame and render pipeline:
  - `Scheduler` with begin/draw/post-frame phases and persistent callbacks.
  - `PipelineOwner` with layout, compositing, paint, and semantics flushing.
  - `RenderObject`/`RenderBox` tree with hit testing and parent/child contracts.
  - Layer tree primitives (`OffsetLayer`, `OpacityLayer`, `TransformLayer`, `ClipRectLayer`, `PictureLayer`).
- Framework host integration:
  - `FlutterHost` + `WidgetHost` to mount widget trees into Avalonia host controls.
  - Pointer/key event bridging to framework gesture and navigation layers.
- Widgets and rendering primitives:
  - Basic widgets (`Container`, `Padding`, `SizedBox`, `ColoredBox`, `Row`, `Column`, `Flexible`, `Expanded`, `Text`, `Button`).
  - Proxy render objects (`RenderPadding`, `RenderColoredBox`, `RenderOpacity`, `RenderTransform`, `RenderClipRect`, pointer listener).
  - Flex/layout render implementation and text/button render objects.
- Gestures and input pipeline:
  - Pointer routing, gesture arena, and recognizers (tap, drag, long press).
  - Gesture detector and raw gesture widget wiring.
- Navigation stack:
  - Route model (`Route`, `ModalRoute`, `PageRoute`, builder route).
  - `Navigator` APIs for push/pop/replace/named routes and observer hooks.
  - Route observer support and back-button handling integration.
- Scroll/sliver infrastructure:
  - `ScrollController`, `Scrollable`, `Viewport`, `CustomScrollView`, `SingleChildScrollView`.
  - Sliver adapters and lists/grids (`SliverList`, `SliverGrid`, `SliverFixedExtentList`, padding, delegates).
  - High-level widgets (`ListView`, `GridView`, `Scrollbar`), keep-alive and notifications.
- Semantics:
  - Semantics configuration and node ownership model.
  - Semantics updates integrated into pipeline flush cycle.
- Sample app and host targets:
  - Sample gallery with counter, navigator, list/grid/sliver/scrollbar demos.
  - Host entry points for desktop, browser, Android, and iOS sample projects.
- Test coverage foundation (`src/Flutter.Tests`):
  - Lifecycle/reconciliation, inherited dependencies, frame pipeline, rendering parity.
  - Compositing layers, semantics tree behavior, gesture pipeline, scroll pipeline/infrastructure.
  - Navigation stack and observer semantics.

### Notes

- Current strategic direction and open milestones are tracked in `docs/FRAMEWORK_PLAN.md`.
