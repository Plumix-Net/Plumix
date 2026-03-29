# Changelog

All notable framework changes are documented in this file.

This project follows the spirit of [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Planned

- Continue `M4` Material library rewrite with advanced Material control refinements (hover/ripple/style-system expansion) after shipping baseline theming + shell + first button set plus initial interaction polish.
- Run cross-host parity/stability validation in final `M5` phase after Material rewrite sequencing completes.
- Improve architecture docs and migration guidance for Dart-to-C# rewrites.

### Changed

- Documentation policy update: Dart-to-C# control/widget work now uses mandatory parity-first porting mode (`docs/ai/PORTING_MODE.md`) with strict `1:1` default behavior, required divergence logging, and explicit parity-validation workflow references in `AGENTS.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/INVARIANTS.md`, `docs/ai/MODULE_INDEX.md`, `docs/ai/FEATURE_TEMPLATE.md`, `docs/ai/TEST_MATRIX.md`, and `docs/ai/PARITY_MATRIX.md`.

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
