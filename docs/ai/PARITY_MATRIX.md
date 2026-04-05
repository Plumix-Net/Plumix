# Sample Parity Matrix

Scope: structural parity of sample routes/modules between C# and Dart samples.

Policy note: sample parity updates must follow `docs/ai/PORTING_MODE.md` (Dart source of truth, strict `1:1` default, documented divergences).

Last checked: 2026-04-05

Status legend:

- `done`: feature exists in both samples and is wired in route map/menu.
- `verify-runtime`: structural parity exists, but interactive behavior still needs runtime validation.
- `missing`: present only on one side.

## Route and Feature Parity

| Feature | C# sample | Dart sample | Status | Notes |
| --- | --- | --- | --- | --- |
| App bootstrap | `src/Sample/Flutter.Net/CounterApp.cs` | `dart_sample/lib/counter_app.dart` | done | Counter model + scope app root present on both sides; C# root uses framework Material `Theme(data: ThemeData.Light, ...)` (`Flutter.Material`) while Dart root uses `MaterialApp(theme: ThemeData(...))`. C# `ThemeData.Light` defaults are now aligned to Flutter M3 token values for key sample surfaces/buttons (`primary`, `onSurface`, `secondaryContainer`, `onSecondaryContainer`, `scaffold/canvas`) and button typography baseline (`labelLarge`). |
| Route constants + route data | `src/Sample/Flutter.Net/SampleGalleryScreen.cs` | `dart_sample/lib/sample_routes.dart` | done | Same route set including navigator details route. |
| Sample gallery menu | `src/Sample/Flutter.Net/SampleGalleryScreen.cs` | `dart_sample/lib/sample_gallery_screen.dart` | done | Same demo page set and menu flow; both sides now use Material shell structure (`Scaffold` + `AppBar`) for menu and demo page wrappers, and shell actions now use Material buttons (outlined route entries + elevated back button). Demo-page back action now uses icon+label composition (`arrow_back` + `Back`) in both samples. |
| Counter page | `src/Sample/Flutter.Net/CounterScreen.cs` | `dart_sample/lib/counter_screen.dart` | verify-runtime | Structural match; centered label parity hardened for intrinsic-width rows (notably `Keyed List`) via `RenderParagraph` loose-width center/right/end alignment normalization, and both samples now wrap page content with `SingleChildScrollView` to avoid bottom overflow-debug zones on shorter viewports. |
| Navigator demo | `src/Sample/Flutter.Net/NavigatorDemoPage.cs` | `dart_sample/lib/navigator_demo_page.dart` | verify-runtime | Route operations and observer flows are mirrored. |
| ListView separated demo | `src/Sample/Flutter.Net/ListViewSeparatedDemoPage.cs` | `dart_sample/lib/list_view_separated_demo_page.dart` | verify-runtime | |
| ListView fixed extent demo | `src/Sample/Flutter.Net/ListViewFixedExtentDemoPage.cs` | `dart_sample/lib/list_view_fixed_extent_demo_page.dart` | verify-runtime | |
| ListView reverse demo | `src/Sample/Flutter.Net/ListViewReverseDemoPage.cs` | `dart_sample/lib/list_view_reverse_demo_page.dart` | verify-runtime | |
| GridView demo | `src/Sample/Flutter.Net/GridViewDemoPage.cs` | `dart_sample/lib/grid_view_demo_page.dart` | verify-runtime | |
| Custom slivers demo | `src/Sample/Flutter.Net/CustomSliversDemoPage.cs` | `dart_sample/lib/custom_slivers_demo_page.dart` | verify-runtime | |
| Scrollbar demo | `src/Sample/Flutter.Net/ScrollbarDemoPage.cs` | `dart_sample/lib/scrollbar_demo_page.dart` | verify-runtime | |
| Editable text demo | `src/Sample/Flutter.Net/EditableTextDemoPage.cs` | `dart_sample/lib/editable_text_demo_page.dart` | verify-runtime | Includes baseline input flow plus multiline Notes demo (`Enter` newline, Up/Down caret travel), seed action, and escaped live value summary. |
| Material buttons demo | `src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs` | `dart_sample/lib/material_buttons_demo_page.dart` | verify-runtime | Demonstrates Material control set (`TextButton`, `ElevatedButton`, `OutlinedButton`, `FilledButton`, `FilledButton.tonal`, `IconButton`) with enabled/disabled toggles, per-button tap counters, custom color overrides, and icon-button selected/filled/outlined probes; C# demo text colors/string formatting now mirror Dart literals (`black54`, `blueGrey`, `enabled=true/false`), and button defaults now resolve M3 `labelLarge` typography plus Flutter-like foreground precedence over `textStyle.color`; non-text button default content padding is now aligned to Flutter M3 token path (`horizontal: 24`, `vertical: 0`), tap-target behavior now mirrors Flutter `_InputPadding` semantics (constraint-preserving wide bounds + padded-area hit-test redirect), and `ElevatedButton` now restores visible elevation shadow rendering (including app-bar `Back` button depth parity). C# icon probes now use framework `Icon(Icons.*)` with the same set as Dart; framework `Icons` now includes `arrow_back`, `menu`, `close`, `star`, `star_outline`, `add`, `info_outline`. |
| AppBar leadingWidth theme demo | `src/Sample/Flutter.Net/AppBarLeadingWidthDemoPage.cs` | `dart_sample/lib/app_bar_leading_width_demo_page.dart` | verify-runtime | Runtime probe for `AppBar` leading-width precedence (`widget` override vs `ThemeData.AppBarTheme.LeadingWidth` fallback) with side-by-side themed/default previews. Leading/actions probes now render real icons (`menu`/`close`) instead of text badges. |
| AppBar actionsPadding theme demo | `src/Sample/Flutter.Net/AppBarActionsPaddingDemoPage.cs` | `dart_sample/lib/app_bar_actions_padding_demo_page.dart` | verify-runtime | Runtime probe for `AppBar` actions-row padding precedence (`widget` override vs `ThemeData.AppBarTheme.ActionsPadding` fallback) with side-by-side themed/default previews. Action probes now render real icons (`close`/`menu`) inside badge containers. |
| AppBar icon themes demo | `src/Sample/Flutter.Net/AppBarIconThemeDemoPage.cs` | `dart_sample/lib/app_bar_icon_theme_demo_page.dart` | verify-runtime | Runtime probe for `AppBar` icon-theme precedence (`iconTheme` and `actionsIconTheme` with widget/theme/fallback chain) using real `Icon` widgets (`menu`, `close`, `info_outline`) in leading/actions slots. |
| AppBar text styles demo | `src/Sample/Flutter.Net/AppBarTextStylesDemoPage.cs` | `dart_sample/lib/app_bar_text_styles_demo_page.dart` | verify-runtime | Runtime probe for `AppBar` text-style precedence (`titleTextStyle`/`toolbarTextStyle` widget override vs theme fallback vs foreground-color defaults) with themed/default preview comparison. |
| Proxy widgets demo | `src/Sample/Flutter.Net/ProxyWidgetsDemoPage.cs` | `dart_sample/lib/proxy_widgets_demo_page.dart` | verify-runtime | Demonstrates widget-level `Opacity`, `Transform`, and `ClipRect` composition with interactive controls. |
| Align demo | `src/Sample/Flutter.Net/AlignDemoPage.cs` | `dart_sample/lib/align_demo_page.dart` | verify-runtime | Demonstrates widget-level `Align` and `Center` with alignment positions and shrink-wrap factors. |
| Stack demo | `src/Sample/Flutter.Net/StackDemoPage.cs` | `dart_sample/lib/stack_demo_page.dart` | verify-runtime | Demonstrates widget-level `Stack` and `Positioned` overlay behavior with movable/pinned badge positioning. |
| DecoratedBox demo | `src/Sample/Flutter.Net/DecoratedBoxDemoPage.cs` | `dart_sample/lib/decorated_box_demo_page.dart` | verify-runtime | Demonstrates widget-level `DecoratedBox` with `BoxDecoration`, `BorderSide`, and `BorderRadius` controls. |
| Container demo | `src/Sample/Flutter.Net/ContainerDemoPage.cs` | `dart_sample/lib/container_demo_page.dart` | verify-runtime | Demonstrates `Container` composition with `alignment`, `margin`, `constraints`, and `transform` behavior (including clamp/tighten interaction). |
| AspectRatio + Spacer demo | `src/Sample/Flutter.Net/AspectRatioDemoPage.cs` | `dart_sample/lib/aspect_ratio_demo_page.dart` | verify-runtime | Demonstrates widget-level `AspectRatio` tight sizing and `Spacer` flex-gap behavior. |
| FractionallySizedBox demo | `src/Sample/Flutter.Net/FractionallySizedBoxDemoPage.cs` | `dart_sample/lib/fractionally_sized_box_demo_page.dart` | verify-runtime | Demonstrates widget-level fractional width/height constraints with alignment controls and pass-through axis mode. |
| FittedBox demo | `src/Sample/Flutter.Net/FittedBoxDemoPage.cs` | `dart_sample/lib/fitted_box_demo_page.dart` | verify-runtime | Demonstrates widget-level `BoxFit` scaling (`contain/cover/fill/none/scaleDown`) and alignment-controlled placement. |
| UnconstrainedBox + LimitedBox demo | `src/Sample/Flutter.Net/UnconstrainedLimitedBoxDemoPage.cs` | `dart_sample/lib/unconstrained_limited_box_demo_page.dart` | verify-runtime | Demonstrates axis-specific unconstraining and `LimitedBox` max-clamp behavior on unbounded axes, including centered multiline probe label parity. |
| OverflowBox + SizedOverflowBox demo | `src/Sample/Flutter.Net/OverflowBoxDemoPage.cs` | `dart_sample/lib/overflow_box_demo_page.dart` | verify-runtime | Demonstrates optional constraint overrides, `OverflowBoxFit` sizing, and fixed-size overflow behavior with alignment controls. |
| Overflow indicator demo | `src/Sample/Flutter.Net/OverflowIndicatorDemoPage.cs` | `dart_sample/lib/overflow_indicator_demo_page.dart` | verify-runtime | Demonstrates Flutter-style `RenderFlex` overflow debug stripes and overflow labels on intentional vertical/horizontal overflow scenarios. |
| Offstage demo | `src/Sample/Flutter.Net/OffstageDemoPage.cs` | `dart_sample/lib/offstage_demo_page.dart` | verify-runtime | Demonstrates `offstage` toggle behavior: child remains in layout pass while paint/hit-test and parent space participation are suppressed. |
| Shared counter widgets | `src/Sample/Flutter.Net/CounterWidgets.cs` | `dart_sample/lib/counter_widgets.dart` | verify-runtime | Includes keyed/movable/keep-alive helper widgets and `CounterTapButton` centered text alignment parity. |
| Counter state container | `src/Sample/Flutter.Net/CounterAppModel.cs`, `src/Sample/Flutter.Net/CounterScope.cs` | `dart_sample/lib/counter_app_model.dart`, `dart_sample/lib/counter_scope.dart` | done | |

## When Updating Sample Features

1. Update C# sample (`src/Sample/Flutter.Net/*`).
2. Apply equivalent Dart change (`dart_sample/lib/*`) in the same iteration.
3. Update this matrix status/notes.
4. Record shipped parity changes in `CHANGELOG.md`.
