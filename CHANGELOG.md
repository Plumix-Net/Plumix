# Changelog

Newest first. One line per change (≤120 chars): what shipped, the Dart source file in parentheses for
ports, and a `Breaking:` prefix when a public API or default changed — even when the change moves
*toward* Flutter (`docs/ai/INVARIANTS.md` > Versioning). No member lists, no test inventory, no
rationale — the commit message and `git log -p` carry the detail. When a release is tagged, collapse
`[Unreleased]` into a few bullets under the version heading, keeping every `Breaking:` item by name.
Detailed per-change history before 2026-08-16 lives in git history (`git log`).

## [Unreleased]
- Plumix.iOS ships `buildTransitive/Plumix.iOS.targets`: Native AOT apps no longer link the private-data frameworks
  (CoreBluetooth, Contacts, CoreLocation, Photos, HealthKit…) the SDK adds, which drew App Store ITMS-90683.

## v3.47.0-preview.2 (Plumix.Material 1.11.1-preview.2, Plumix.Cupertino 1.1.7-preview.2) — 2026-11-23
- Trim/Native AOT clean: `IsAotCompatible` on every package; service-extension, navigation-event and inspector
  JSON go through the Dart-shaped `Json.Encode` instead of reflection-based `JsonSerializer`.
- Pins moved to `material_ui` 1.11.1 / `cupertino_ui` 1.1.7 (`dart_sample`, `AGENTS.md`); the open delta is the
  re-port table in `docs/ai/BACKLOG.md`. The ink-sparkle shader source moved to `material_ui/lib/shaders/`.
- Breaking: Cupertino deltas absorbed — tinted button light opacity 0.12 → 0.18 (`constants.dart`), disabled
  `CupertinoSlider` at 0.5 opacity (`slider.dart`), magnifier focal point follows the in/out animation
  (`magnifier.dart`), localized action-sheet label (`dialog.dart`), `NoDefaultCupertinoThemeData` equality
  includes `selectionHandleColor` (`theme.dart`); `CupertinoTabBar` accepts one item (`bottom_tab_bar.dart`).
- Breaking: Material deltas absorbed — weighted carousel max scroll extent (`carousel.dart`), snack bar overflow
  spacer and scaled action width (`snack_bar.dart`), header/button/selected/container semantics (`drawer_header`,
  `popup_menu`, `autocomplete`, `expansion_tile`), indicator repaints its `Material` (`navigation_bar.dart`),
  bottom sheet closes within tolerance and disposes drag-end curves (`bottom_sheet.dart`), contrast-level
  tolerance (`color_scheme.dart`), disabled selected switch thumb uses `surface` (`switch.dart`).

## v3.47.0-preview.1 (Plumix.Material 1.11.1-preview.1, Plumix.Cupertino 1.1.7-preview.1) — 2026-11-23
- Breaking: versions follow the ported upstream (`docs/VERSIONING.md`): Plumix and hosts `3.47.0-preview.N` (Flutter),
  `Plumix.Material` `1.11.1-preview.N` (`material_ui`), `Plumix.Cupertino` `1.1.7-preview.N` (`cupertino_ui`).
- Release: numbers live in `src/Versions.props`; tags `v*`, `material-v*`, `cupertino-v*` (`scripts/release_plan.sh`);
  the template stamps `Plumix.Material` at its own version.
- Breaking: `Plumix.Avalonia.Android` (`PlumixMainActivity`) and `AndroidActivityDelegate` are removed; Android apps
  use the native host `Plumix.Android` (`PlumixActivity`). The Avalonia Android sample is gone too.
- Samples: the Avalonia iOS head and `NetBench.Impeller.Android` are removed; the native heads are
  `Plumix.Sample.Android` and `Plumix.Sample.iOS` (were `Plumix.Sample.Impeller.*`).
- `Plumix.Avalonia` drops its Avalonia-on-Android paths: `TopLevel.BackRequested`, the touch slop, bar colours by
  reflection, the `android` key message, Android node classes and `IActivityApplicationLifetime` in `Run`.

## v1.0.0-preview.9 — 2026-11-23
- New template package `Plumix.Templates`: `dotnet new plumix-app` creates shared widgets with Desktop (Avalonia),
  Android and iOS (native hosts) projects; `--desktop/--android/--ios false` drop a head, `--application-id` sets
  the id.
- `Plumix.iOS` reads pixels back: `Scene.toImage` draws into a stand-in drawable over a shared texture, the way the
  engine's `FlutterMetalLayer` drawables learn when Impeller's work completes (`FlutterMetalDrawable`).

## v1.0.0-preview.8 — 2026-11-23
- New native iOS host `Plumix.iOS` (`PlumixViewController`, `PlumixSceneDelegate`) on Impeller/Metal, without
  Avalonia: raster thread, ports of the engine's platform plugin, text input, keyboard, VoiceOver and state restoration.
- `Plumix.Impeller` finds HarfBuzz as an embedded framework on iOS, tvOS and Mac Catalyst.
- Avalonia 12.1.6 compatibility: its new `Avalonia.Media.FontVariation` no longer clashes with Plumix's.
- The Avalonia iOS sample moves to `src/Sample/Plumix.Sample.iOS`; the name `Plumix.iOS` is the native host's.

## v1.0.0-preview.7 — 2026-11-23
- `Plumix.Android` supports Android 7.0 (API 24): the engine's version checks and fallbacks for API 26–29 calls.
- `Plumix.Android` before Android 11: draws behind the status bar and reports bars and keyboard as FlutterView does.

## v1.0.0-preview.6 — 2026-11-23
- Migration notes for this release: `docs/MIGRATION.md`.
- Breaking: the Avalonia Android host package is `Plumix.Avalonia.Android` (namespace `Plumix.Avalonia.Android`).
- Breaking: `Plumix` no longer depends on Avalonia, SkiaSharp or HarfBuzzSharp; they come with `Plumix.Avalonia`.
- Breaking: `RawMagnifier` is a `StatelessWidget`; with backdrop filters it composes as Flutter does (magnifier.dart).
- New native Android host `Plumix.Android` (`PlumixActivity`) on the new `Plumix.Impeller` backend, without Avalonia:
  ports of the engine embedding's input, keyboard, IME, autofill, accessibility, platform channels and image codecs.
- Drawing, text, images, the UI thread and scene rasterization go through internal host seams; `Picture`, `Path`,
  `Shader` and filters are framework data, and `Path` ports Skia's geometry and PathOps to managed code.
- Fixes: opacity/image-filter render objects start as non-repaint-boundaries (object.dart); relative asset keys
  resolve on Android; Material/Cupertino painters and icon fonts no longer need a host.

## v1.0.0-preview.5 — 2026-11-21
- Migration notes for this release: `docs/MIGRATION.md`.
- Breaking: Avalonia host moves to the new `Plumix.Avalonia` package; reference it for `PlumixApplication`/`PlumixHost`.
- Breaking: `PlumixExtensions.Run(widget, lifetime)` is `PlumixApplication.Run`; `RunApp`/`RunWidget` stay in core.
- Breaking: `Offset`, `Size`, `Rect` are framework structs with Dart members and `Dx`/`Dy` (geometry.dart).
- Breaking: `Rect` keeps four edges; `Contains` excludes right/bottom; `Intersect`/`Deflate` unclamped (geometry.dart).
- Breaking: `DartGeometry` and geometry extension helpers removed; factories are members, `Offset & Size` builds a Rect.
- Breaking: geometry and `Radius` `==` follow Dart (NaN unequal to itself); `Equals`/`GetHashCode` stay reflexive.
- Breaking: `FontWeight`/`FontStyle` are dart:ui types; `Medium`/`SemiBold`-style names are `W500`/`W600` (text.dart).
- Breaking: font families are strings in text/strut/paragraph styles, `ThemeData`, `TextTheme` (text_style.dart).
- `FontLoader` and `PlatformFonts.LoadFontFromList` register font bytes under a family at run time (font_loader.dart).
- Breaking: `Icon` finds its font via `fontPackage`; `IconFontRegistry`, `Icon.ResolveFontFamily` removed (icon.dart).
- `PlatformAssetBundle` resolves `packages/<assembly>/<path>` keys; `AssetBundle.LoadBytesAsync` reads bytes.
- Breaking: `RasterImage` (dart:ui `Image`) replaces `IImage`/`Bitmap` in image, canvas and layer APIs (painting.dart).
- Breaking: image `ColorBlendMode` and `ColorFilter.Mode` take `BlendMode`; `ColorFilter.Mode.FlutterBlendMode` removed.
- Breaking: `Shader` replaces `IBrush` in `Gradient.CreateShader`, `Paint.Shader`, `ShaderCallback` and shader masks.
- Breaking: `Canvas` brush/pen/geometry overloads, `ClipGeometryLayer`, `PushClipGeometry`, `ToPen` are internal.
- Breaking: `NotchedShape.GetOuterPath` returns `Path` (notched_shapes.dart); `GradientTransform` gives `Matrix4`.
- Breaking: `paintZigZag` takes a `Paint`; `Color`/`EdgeInsets` lose Avalonia conversions (see `AvaloniaInterop`).
- Breaking: `EngineTexture.Paint` and `Picture.Playback(DrawingContext)` are internal; only hosts add texture kinds.
- Public API of `Plumix`, `Plumix.Material`, `Plumix.Cupertino` names no Avalonia type (`PublicApiAvaloniaFreeTests`).

## v1.0.0-preview.4 — 2026-11-20
- Predictive back pops the route: the user gesture stops only when its settle animation ends (routes.dart).

## v1.0.0-preview.3 — 2026-11-20
- Android: `setSystemUIOverlayStyle` styles the window, so bars are transparent below API 35 (PlatformPlugin.java).
- Host has no theme focus adorner: Fluent's focus-visible frame no longer outlines the whole view on Android 14.
- `ImageFilter.matrix` layers draw children through the matrix: stretch overscroll no longer blurs text (layer.dart).
- Rounded clips scale oversized radii uniformly like SkRRect: `circular(100)` on a thin bar is a stadium (rrect).

## v1.0.0-preview.2 — 2026-11-20
- Paragraph glyphs paint with grayscale anti-aliasing like SkParagraph, never LCD subpixel text (SkParagraph TextStyle).
- Host: the dispatcher clock workaround recognizes the upstream fix in Avalonia 12.1.6 and leaves it alone.
- Breaking: all packages target .NET 11 (`net11.0`, `net11.0-android`); consumers need the .NET 11 SDK.
- Breaking: minimum Android is API 24 and the iOS sample's minimum is 15.0, as the .NET 11 workloads require.
- iOS sample declares a `UIApplicationSceneManifest`, which apps built with the iOS 27 SDK must have.
- Android: a text field node carries "label, hint" as hint text, not a nameless EditText (BaseRoleConfigurator.java).
- Host keeps the shown frame when a scene only re-adds retained layers: an idle iOS caret no longer redraws at 60 Hz.

## v1.0.0-preview.1 — 2026-10-28
- Breaking: a Dart-shaped `EdgeInsets` replaces Avalonia `Thickness` in every signature (edge_insets.dart).
- Breaking: `EdgeInsetsGeometry.Resolve` returns `EdgeInsets`; `RRect` edge helpers move to it (edge_insets.dart).
- Breaking: scroll-view, chip, app-bar, card, FAB, nav and `StyleFrom` paddings are `EdgeInsetsGeometry` (material_ui).
- Breaking: `FlutterView` padding, insets and gesture insets are `ViewPadding` (ui/platform_dispatcher.dart).
- Breaking: `TextSelectionToolbarTextButton.GetPadding(index, total)` is directional (text_selection_toolbar_text_button.dart).
- Insets deflate rects without clamping at zero, like Dart's `deflateRect` (edge_insets.dart).
- Breaking: `RenderParagraph` drops flat style accessors, `PlainText` and the string ctor; only `Text` (paragraph.dart).
- Breaking: `SelectableRegion` drops the C#-only `onTap`/`mouseCursor` options (selectable_region.dart).
- Breaking: `ReorderableList` takes Dart's param order, `EdgeInsetsGeometry?` padding, asserts (reorderable_list.dart).
- Breaking: `scrollController`, bool `StartItemDragReorder` and `Delayed` removed (reorderable_list.dart).
- Breaking: `ReorderableListView` validates through Dart's asserts; no anchor range guard (reorderable_list.dart).
- Breaking: `EditableText` matches Dart's constructor: required args, defaults, no C#-only members (editable_text.dart).
- Breaking: `EditableText` drops its `Container`/`Focus` wrappers; cursor radius/offset nullable (editable_text.dart).
- Breaking: `TextField.OnKeyEvent` removed; the field sets its node's `canRequestFocus` (text_field.dart).
- `CupertinoTextField` is its field's `AutofillClient`; the placeholder is the autofill `hintText` (text_field.dart).
- Search page closes on Escape through its own `FocusNode.onKeyEvent` (search.dart).
- Breaking: `MaterialLocalizations.Of` throws without a delegate, no default fallback (material_localizations.dart).
- Material widgets assert `debugCheckHasMaterialLocalizations` at all 40 Dart call sites (material_ui/lib/src).
- `Theme.Of` falls back to englishLike typography without `MaterialLocalizations` (theme.dart).
- Breaking: `RenderSliverMultiBoxAdaptor` takes a required get-only `ChildManager` (sliver_multi_box_adaptor.dart).
- `GetRectOfSemanticsNodeInViewCoordinates` asserts like Dart; stale semantics actions dropped (rendering/binding.dart).
- Focus: `debugFocusChanges` and `_focusDebug` logging at every call site, tree dump on change (focus_manager.dart).
- `BindingBase.CheckInstance`/`DebugBindingType`, `WidgetsBinding.DebugCheckZone` (foundation/binding.dart).
- `RunApp`/`RunWidget` assert `DebugCheckZone`; first frame sets the default `UserTag` (widgets/binding.dart).
- `UserTag` and `DeveloperService.GetCurrentTag` (dart:developer profiler.dart).
- `PlatformDispatcher.OnPlatformConfigurationChanged`, before each specific callback (ui/platform_dispatcher.dart).
- Image codecs complete `GetNextFrame` on a later event-loop turn; tests turn it via `RunAsync` (multi_frame_codec.cc).
- Breaking: `IPreferredSizeWidget.PreferredSize` is `PreferredSizeValue`, no `PreferredAppBarSize` (preferred_size.dart).
- Sample: `ImageDemoPage` decodes its raw image like the Dart sample; captures reset caches per route; `/image` matches.
- Text: an empty line's leading follows the strut's half-leading flag, not the style's (SkParagraph `computeEmptyMetrics`).
- Headless paragraphs: hard-break and placeholder-only line metrics and the half-spacing shift follow SkParagraph `TextWrapper`.
- Headless paragraphs shape FlutterTest marks like HarfBuzz (per-glyph spacing, composition, mark widths per shaper).
- `SelectableText` merges `DefaultTextStyle` and passes an unforced `StrutStyle()` (selectable_text.dart).
- Sample capture parity: every route matches the Dart capture or is a `DIVERGENCES.md` row (RC blocker met).
- Headless paragraphs fake bold (w600+)/italic FlutterTest glyphs and paint backgrounds, shadows, decorations (SkParagraph).
- Headless paragraphs keep the half letter-spacing lead-in unless an alignment shift applies (SkParagraph `TextLine`).
- `Badge` composes `Container(clipBehavior, ShapeDecoration(StadiumBorder))` like badge.dart (badge.dart).
- M3 `Chip` delete icon takes `_ChipDefaultsM3.deleteIconColor` (chip.dart).
- Sample: Router/NavigatorPages buttons and TextField form buttons follow the Dart sample; 160 of 164 capture routes match.
- Breaking: `PlatformDispatcher.TimerRun` removed; scheduler tasks and warm-up frames use `Timer.Run` (scheduler/binding.dart).
- `FrameworkDartTester` fails on pending timers, runs warm-up frames at once, disables shadows (flutter_test binding.dart).
- Service-extension handlers start on the framework thread; the intermittent banner-extension hang is gone (C#-only).
- Breaking: C#-only `MaterialLocalizationsScope` removed; `MaterialLocalizations.Of` reads `Localizations` (C#-only).
- Switch/CupertinoSwitch thumbs and the magnifier decoration paint shadows via `BoxShadow.toPaint` (switch.dart, magnifier.dart).
- Breaking: `BoxShadowExtensions` (`ToAvalonia`) removed (C#-only).
- Breaking: strict `SnapshotWidget` with `SnapshotPainter`/`painter`; `pixelRatio` and `SnapshotOffsetLayer` removed (snapshot_widget.dart).
- Breaking: strict single-thread `ImageCache`; `ImageCacheStatus` ctor internal, `UntrackedStatus` removed (image_cache.dart).
- `ImageProvider.Evict`/`ObtainCacheStatus` resume on the caller's context (image_provider.dart).
- Breaking: `FocusNode.ToString` is Dart's diagnostics form; focus-tree diagnostics and `debugDumpFocusTree` (focus_manager.dart).
- `RenderParagraph`/`RenderEditable` relayout, `Banner` and Cupertino pickers refresh on system font changes (object.dart).
- Windows host forwards `WM_FONTCHANGE` as the `fontsChange` system message (C#-only).
- Hosts answer `SystemNavigator.pop` per platform; Android no longer maps Escape/BrowserBack to `popRoute` (C#-only).
- Hosts play system sounds/haptics natively as the engine plugins do; `PlumixHost` handler hooks (C#-only).
- Android/iOS hosts resolve the platform locale; Android/browser report accessibility features (C#-only).
- iOS AOT Bloc counter crash closed; `HostBinaryCompatibilityTests` binds sample packages to Plumix (C#-only).
- Breaking: one dart:ui `Brightness` in `Plumix.UI` replaces `PlatformBrightness` and Material's enum (window.dart).
- Breaking: `Plumix.Timer` (dart:async `Timer`) on the event loop with a fake test clock; `GestureTimer` removed.
- Snack bar, tooltip, menu hover, search, magnifier, expansion-tile and gesture timers run on `Timer` (scaffold.dart).
- Breaking: a second `AnimationController.Dispose` throws a `FlutterError` (animation_controller.dart).
- Breaking: `AnimationController` duration/dispose/`repeat` checks throw Dart's errors (animation_controller.dart).
- Breaking: Color/TextStyle/MouseCursor state-property slots are `IWidgetStateProperty`, as in Dart (widget_state.dart).
- Breaking: `WidgetStateMouseCursor` resolves a non-null `MouseCursor`, as in Dart (widget_state.dart).
- Breaking: `WidgetStateProperty<T>.Lerp` and `MaterialThemeLerp` lerps take `IWidgetStateProperty` (C#-only).
- Breaking: `PipelineManifold` is an interface; `HostPipelineManifold` extends `ChangeNotifier` (rendering/object.dart).
- Breaking: `BuildPage` moves from `Route` to `ModalRoute`, as Dart's `buildPage` (widgets/routes.dart).
- Breaking: `ButtonThemeData.Get*Color(button)` drop `ThemeData` and read the new `ColorScheme` (button_theme.dart).
- `ButtonTheme.Of` fills a missing `colorScheme`; `ThemeData.buttonTheme` defaults carry one (button_theme.dart).
- `Switch.adaptive` reads `cupertinoOverrideTheme.primaryColor` unresolved, as Dart does (switch.dart).
- `TextField`/`SelectableText` gain `selectionHeightStyle`/`selectionWidthStyle` (text_field.dart).
- `ScrollableDetails` gains the deprecated `clipBehavior` parameter and Dart's `toString` (scrollable_helpers.dart).
- `debugCheckHasTable`, asserted by `TableRowInkWell` (widgets/debug.dart, data_table.dart).
- `PlatformDispatcher.FrameData` carries the vsync's frame number; `OnFrameDataChanged` added (hooks.dart).
- Breaking: `MediaQueryData.FromView` uses `SystemTextScaler` over `ScaleFontSize` (media_query.dart).
- Android 14+ reports its nonlinear font scaling curve to `ScaleFontSize` (platform_dispatcher.dart).
- Breaking: `DefaultSelectionWidthStyle` is `Max` on web Safari via `BrowserDetection` (editable_text.dart).
- Hosts report the OS's ordered preferred languages: Android, iOS, macOS, Windows, Linux (C#-only).
- Android reports `ViewConfiguration` touch slop as the view's `GestureSettings` (gesture_settings.dart).
- Browser host ports the web engine's typography observer: text scale and text spacing overrides (C#-only).

## v0.4.0-beta.4 — 2026-10-25
- Hosts point Avalonia's dispatcher at its platform clock; Android timers busy-spun and stalled rendering (C#-only).
- `MediaQuery.textScaler` follows the system font size (`PlatformDispatcher.TextScaleFactor`, platform_dispatcher.dart).
- Automation peers report a selected node without a tap action; tappable selectable nodes stay invocable (C#-only).
- A `TextField` with a URL/email/phone keyboard is one semantics node again: no input-type conflict (semantics.dart).

## v0.4.0-beta.3 — 2026-10-24
- Android: the Activity's back callback steps aside while the soft keyboard is shown, so back hides it first (C#-only).

## v0.4.0-beta.2 — 2026-10-24
- Variable-font outlines fill with the non-zero rule, as Skia fills glyphs; overlaps were punched out (C#-only).
- Hosts answer `Clipboard.hasStrings` from the clipboard's formats; reading it showed iOS's paste alert (C#-only).
- Hosts take `viewInsets.bottom` from the view area the input pane covers; hidden iOS keyboards left it (C#-only).
- iOS Backspace runs the plugin's `deleteBackward`; hosts no longer type a key's control characters (C#-only).

## v0.4.0-beta.1 — 2026-10-24 (collapses v0.2.0-alpha.2 … v0.3.0-alpha.4, 2026-08-20 … 2026-10-24)
- Cupertino (M6): every `cupertino_ui/lib/src/` file is a strict port — theme/colours/text theme, localizations
  (116 locale bundles), routes and sheets, `CupertinoApp`, page/tab scaffolds, navigation bars, pickers, context
  menu, menu anchor, text field and selection, segmented controls, refresh control, list sections/tiles, icons.
- Hosts are pure engine embedders: retained dart:ui `Scene`s through `SceneBuilder`/`EngineLayer` and
  `FlutterView.Render`, `PlatformDispatcher` metrics/locales/accessibility/key data, physical `PointerDataPacket`s,
  `flutter/textinput`/`keyevent`/`platform`/`mousecursor` channels, the windowing API, `FrameTiming` reports.
- dart:ui surface: `Canvas`/`Picture`/`Paint` (mask filters, blend modes, isolated `saveLayer`), framework `Color`,
  native `Path` geometry and metrics, `Paragraph` over Avalonia with SkParagraph metrics plus a headless FlutterTest
  engine, `drawShadow`, gradients, animated image codecs, Skia-exact anti-aliasing for RRect/DRRect/path draws.
- Strict framework ports: `framework.dart` (elements, build scopes, keys, notifications), the rendering
  object/box/sliver/proxy/shifted/flex/viewport protocols, layers and the `PipelineOwner` tree, every binding, the
  focus tree and traversal, the scrolling stack (2D viewports, nested scroll), gestures (tap, drag, long-press,
  scale, force press, multi-drag, trackpad pan/zoom), mouse tracking, the semantics fragment model, text
  input/editing/selection, curves/tweens/implicit animations, `Hero`, `Navigator`/`Router`, state restoration.
- Material: strict re-ports of buttons and `ButtonStyle`, `Switch`, sliders, ink features, `Material`, `Scaffold`,
  `AppBar`, date/time pickers, `TextField`/`InputDecorator`, `ThemeData`, `BottomNavigationBar`, `Stepper`,
  `PaginatedDataTable`, list tiles; the 8,825-icon `Icons` catalog; `GlobalMaterialLocalizations` (119 locales).
- Platform bridges: accessibility (UI Automation, macOS, AT-SPI, Android, VoiceOver, browser ARIA), platform views
  (framework side), `PlatformMenuBar`, system context menu, `SystemChrome`, clipboard, Scribe/Live Text, ProcessText.
- Hosts: the `Plumix.Android` package (`PlumixMainActivity` over `AndroidActivityDelegate`), Android system back,
  Windows/Linux key messages; CI also runs the suite on the newest Avalonia of the major.
- Tooling: `FrameworkDartTester` with flutter_test's finders, matchers and `TestTextInput`; Flutter's test files
  ported per subsystem; timeline events, service extensions and the widget inspector; a `Profile` configuration;
  tests green in Debug/Profile/Release; opt-in screen capture of every sample route in both samples.
- Breaking (hosts): Avalonia 12.1.4 minimum; `PlumixHost` never lays out or paints, it rasterizes
  `FlutterView.RenderRequested` scenes (`PipelineOwner.CompositeFrame(DrawingContext)` gone); render-only hosts
  register views; one process-wide channel set across windows; Android `Run` sets `MainViewFactory`; the Android
  sample is `Plumix.Sample.Android`; back reaches `WidgetsApp.DidPopRoute` via `TopLevel.BackRequested`
  (`TryHandleBackButton` gone, `Key.Back` is no back button); hosts send `flutter/keyevent` (`HostRawKeyEventData`
  gone) and speak `flutter/textinput` through an instance `HostTextInputPlugin`; host clicks get distinct gesture IDs;
  hot reload runs `reassembleApplication` (host hooks gone); the framework thread, microtasks and async continuations
  live on `EventLoop`, vsync and clock on `FrameClock`.
- Breaking (painting): framework `Color`/`ColorSpace` replace Avalonia's `Color` in every API, with dart:ui's
  `lerp`/`alphaBlend`/`withOpacity` and `toString`; `ColorSwatch`/`MaterialColor`/`CupertinoDynamicColor`/
  `WidgetStateColor` derive from `Color`; `Matrix4` replaces the affine pipeline; `Canvas` is unsealed,
  `saveLayer(Rect?, Paint)` isolates, `drawImage*` take a `Paint`, clips push no edge mode; `TextStyle`,
  `TextPainter`, `StrutStyle`-aware `Paragraph`, `LineMetrics`/`TextBox`, `WordBoundary` (to `Plumix.Painting`),
  `TextOverflow.Visible`/vertical `Fade`, `TextHeightBehavior.LeadingDistribution` follow Dart
  (`TextLayoutFallback`, `Canvas.DrawTextLayout*` gone); headless text uses FlutterTest metrics and glyph boxes;
  `BoxDecoration.BorderRadius` is `BorderRadiusGeometry?` (`EffectiveBorderRadius` gone), decorations paint through
  Dart's painters with `IsComplex` as in Dart; `ShapeBorder.PaintInterior` takes a `Paint` and `Lerp` tries the
  reversed timeline; `paintImage`/`DecorationImage`/`lerp`, `TableBorder`, gradients, `BoxShadow` (Avalonia shadow
  structs gone), `Path`, rounded geometry, `Alignment`, `FittedSizes`, image streams, memory-allocation events and
  `PaintingBinding`'s cache are strict.
- Breaking (rendering): `RenderView` is a bare `RenderObject` with a DPR `TransformLayer` root; `RenderSliver` is a
  `RenderObject`; `SliverGeometry`, sliver parent data (`SliverLogicalContainerParentData`, nullable
  `SliverMultiBoxAdaptorParentData.Index`/`LayoutOffset`, `IKeepAliveParentData`), `IRenderSliverBoxChildManager`
  (`ChildCount`/`EstimatedChildCount`), fixed/varied extent lists (variable-extent types gone, `SetItemExtent`),
  `RenderSliverGrid` (RTL, `SliverGridGeometry` class), persistent headers (abstract extents, no `ChildBuilder`,
  `SliverFloatingHeader`/`SliverResizingHeader`/`PinnedHeaderSliver`), `RenderSliverOpacity` follow Dart;
  proxy_box is split and unsealed (`RenderProxyBox` concrete; `RenderOpacity`, `RenderLimitedBox`,
  `RenderAspectRatio`, `RenderConstrainedBox`, `RenderDecoratedBox.Decoration`, `RenderBackdropFilter`,
  `RenderTransform`, `RenderPhysicalModel`, clip hit tests and `Clip.AntiAlias` defaults, `RenderPointerListener`,
  `RenderIgnorePointer`/`RenderAbsorbPointer`, `RenderSemanticsGestureHandler`, `RenderAnnotatedRegion`);
  `RenderAlign`/`RenderFractionallySizedBox` become `RenderPositionedBox`/`RenderFractionallySizedOverflowBox` over
  `AlignmentGeometry` + `TextDirection`, `RenderPadding.Padding` is `EdgeInsetsGeometry`, `RenderBaseline`,
  `RenderAnimatedSize`, `RenderIndexedStack`, `RenderFlex` (Directionality, unbounded errors, enum order),
  `RenderEditable`, `RenderParagraph`, `RenderErrorBox` are strict; `RenderBox` hit tests are half-open;
  `PaintingContext` (a `ClipContext`, `Push*` return layers), `ClipRRectLayer`/`ClipPathLayer`, 8-bit `OpacityLayer`,
  `Layer.AddToScene`/`ContainerLayer.BuildScene`, nullable `ShaderMaskLayer` fields; clips/transforms are no repaint
  boundaries; `PipelineOwner` dirty queues, child owners and nullable `SemanticsOwner`; `MarkNeedsPaint`,
  `SizedByParent`/`PerformResize`, `DropChild`/`Attach`/`Detach`, `GetTransformTo`, layer release on unmount;
  `MouseCursor` is a class, `MouseCursorManager` per tracker, `SystemMouseCursors` complete, `MouseRegion` a
  render-object widget; hit tests carry transforms (`HitTestEntry.TransformEvent` gone).
- Breaking (framework): `BuildContext` is an interface `Element` implements; `ComponentElement`, abstract
  `ProxyElement`, `InheritedWidget` as a `ProxyWidget`, `...WidgetOfExactType` lookups, `PerformRebuild`, `Mounted`,
  `UnmountRoot`, `_InactiveElements`, `RootWidget`/`RootElement` (`IRootRenderObjectHost` gone),
  `View`/`RawView`/`ViewCollection`, `BuildScope` and Dart's build errors, public lifecycle and render-object hooks;
  `State` (`base.Dispose()`, `SetState` rules, `Context`, `Mounted`; `InvokeSetState` gone); keys (`ObjectKey`,
  `GlobalObjectKey`, `UniqueKey` by identity, `ValueKey` NaN), `IndexedSlot` equality; a throwing `build()` becomes
  `ErrorWidget`; `BuildOwner` owns its `FocusManager` (`ScheduleBuild`/`IsBuilding`/`RegisterElement` gone);
  `RunApp`/`RunWidget` keep one root.
- Breaking (widgets): `Directionality` is required (`Stack`/`IndexedStack` default to `TopStart`); `Text` (no flat
  style parameters, `Locale?`), `RichText.locale`, `DefaultTextStyle.Of`, `Icon`, `Container`, `DecoratedBox`,
  `Padding.Insets`, `SafeArea.minimum`, `ColoredBox`, `Positioned`, `IndexedStack`, `AnimatedCrossFade.ClipBehavior`;
  `MediaQuery` aspects, `PlatformBrightnessOf`, `WithClampedTextScaling`, `RemoveDisplayFeatures`, `fromView`
  accessibility; `SharedAppData`, `PageStorage`, `ChangeNotifier`, `Title`; localizations (`WidgetsLocalizations`
  abstract, async batches, `LocalizationsResolver`); `Semantics` takes one `SemanticsProperties` (`flags`/
  `mergeDescendants` gone); focus (`FocusNode` tree, `Unfocus(disposition)`, void `RequestFocus`, microtask changes,
  `FocusTraversalPolicy`, `ExcludeFocus`; `OnTextInput*`, `HandleText*`, `FocusScopeMarker` gone; no focus on
  pointer down); `WidgetsApp` shortcuts/actions, overlays, inspector, back handling; `Actions.Invoke`;
  `ContextMenuController.Show`; `Overlay`/`OverlayPortal`/`RenderOverlayTheater.SkipCount`; `Navigator` (async pops,
  `Task<bool>` route handlers, post-frame `NavigationNotification`); strict `Hero` (`HeroControllerScope`,
  `HeroFlightShuttleBuilder`, `CreateRectTween`, `TransitionRoute.Suspend` gone); `AutomaticKeepAlive`, `KeepAlive`;
  strict `scroll_delegate.dart` (`NullableIndexedWidgetBuilder`, nullable `itemCount`, abstract `ShouldRebuild`);
  `EditableText` (no `maxLength`/`semanticsLabel`, Actions keys, own `Scrollable`, async clipboard, cursor timer,
  toolbar, selection boxes, non-null `Composing`), `TextEditingValue`, `UndoHistory`, `TextSelectionOverlay`,
  selection gesture builder, `SelectableRegion`, `WidgetStateTextStyle`, `WidgetStateBorderSide`/
  `WidgetStateMouseCursor` and `BottomNavigationBarItem` moved to core, shortcut diagnostics.
- Breaking (scrolling): strict `Scrollable` (requires a `View`, `viewportBuilder`/`axisDirection`, scoped `Of`,
  `SetCanDrag`), `ScrollView`/`BoxScrollView` (`cacheExtent`/`scrollCacheExtent`), `ScrollPosition` split into
  `ScrollPositionWithSingleContext` (`PointerScroll`, activities on `AnimationController`, `PageStorage` keys),
  `ScrollContext`, `IScrollMetrics` (`ScrollMetricsSnapshot` gone), `NestedScrollView`, viewports, list wheel,
  `PageController`, show-on-screen, `EdgeDraggingAutoScroller`, `SingleChildScrollView`, `RawScrollbar` (C#-only
  `Widgets.Scrollbar` gone), glow on Android/Fuchsia.
- Breaking (gestures, keyboard): strict pointer events (trackpad kinds rejected), `GestureBinding` queueing and
  resampling, `SamplingClock` (`DartStopwatch`), arena, tap (`BaseTap`, `TapDownDetails`/`TapUpDetails`), drag
  (accumulated slop, nullable `PrimaryDelta`), long press, `GestureDetector`/`RawGestureDetector` (`gestures:` only,
  nullable `Behavior`), `InvokeCallback` reporting; `KeyEventManager`, `HardwareKeyboard.SyncKeyboardState`, the
  keyboard identity stack.
- Breaking (semantics, services): all 33 `SemanticsRole`s; `SemanticsFlags` is `long` (`IsInvalid`, `IsDialog`
  gone); `SemanticsData`, `SemanticsOwner` dirty set, custom actions, `Absorb`, empty-string text defaults;
  `AccessibilityFeatures` class on `SemanticsBinding`; `SemanticsService.SendAnnouncement`; `FormState.validate`
  announces; strict `SystemChrome`/`SystemUiOverlayStyle` (`SystemUiIconBrightness` gone); platform channels,
  restoration, autofill, text input layer; `FlutterError` replaces `TextInput.OnError`/`ServicesDebug.OnError`;
  diagnostics layer (`Widget`/`RenderObject`/`Element` are `DiagnosticableTree`s).
- Breaking (scheduler, animation): `SchedulerBinding.Instance`, `Priority` class, `lockEvents`, warm-up frame,
  `AddPostFrameCallback`, read-only `FramesEnabled`, `CurrentFrameTimeStamp`, `MidFrameMicrotasks`, reported frame
  errors; `Ticker`/`AnimationController`; `Curve` class hierarchy (`Curves.*` factories gone, `EaseOutCubic`);
  `Tween` (`Transform`, `Lerp(t)`, nullable `ColorTween`/`RectTween`/`SizeTween`, `CurveTween`, `TweenSequence`);
  `CurvedAnimation`; `Animation.ToString`; `AnimationStyle`; `ImplicitlyAnimatedWidget`-based `Animated*`.
- Breaking (Material): `WidgetState` replaces `[Flags] MaterialState`; `Material` (physical model,
  `MaterialInkController` interface, `MaterialEdges.KMaterialEdges`); ink as Dart's `InkFeature`s (`InkResponse`
  stateless, intent activation, stateful `Ink`, native `InkSparkle`); `ThemeData` (per-platform density,
  `VisualDensity`, derived `Brightness`, colour mirrors and `*ButtonStyle` slots gone, eager component themes);
  `Theme` (no `DefaultTextStyle`, `MaterialBasedCupertinoThemeData`); `ButtonStyleButton` and the four buttons
  (shortcuts gone, `ButtonStyle` field order/`Padding`/`Lerp`/`styleFrom`, `splashColor` gone), `IconButton`,
  `RawMaterialButton`, FAB, `ToggleButtons`, `ToggleablePainter`; `Switch` (`semanticLabel` and `SwitchConfig`
  gone); slider family (`SliderThemeData`, `RangeValues`/`RangeLabels`, semantics, keys); time and date pickers
  (`DatePickerThemeData`, `DateTimeRange<T>`, `SelectableDayPredicate`, C#-only calendar types gone); `TextField`,
  `InputDecoration`/`InputDecorationThemeData`, `DesktopTextSelectionToolbarButton`; `AppBar` (`titleText`/`padding`
  gone), `AppBarThemeData`; `Scaffold`/`ScaffoldMessenger`/`PersistentBottomSheetController`; list tiles, chips,
  navigation bar/drawer/rail, `BottomNavigationBar`, `Stepper`, `showDialog` via `showRawDialog`, `DialogTheme`,
  menus and `RawMenuAnchor`, dropdowns, `CarouselView`, `SnackBar`, `TextSelectionTheme`, M2 scheme derivation,
  `Icons` (code points, `MaterialIcons` family), `MaterialLocalizations`, `About`/`LicensePage`, `MaterialApp.Router`.
- Breaking (Cupertino): `CupertinoButton`, `CupertinoCheckbox`, `CupertinoRadio`, `CupertinoSwitch`,
  `CupertinoSlider`, `CupertinoActivityIndicator`, `CupertinoScrollbar`, `CupertinoMagnifier`, `CupertinoTextField`,
  `CupertinoTabScaffold`, `CupertinoTabBar`, `CupertinoMenuAnchor`/`CupertinoMenuItem`, `CupertinoActionSheet`,
  text-selection toolbar and controls re-ported strictly (no `isDark`); theme, colours, icon themes and localizations
  closed with dynamic colours resolved at each consumer.

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
