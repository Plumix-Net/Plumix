# Feature: material-2026-03-13-appbar-title-layout-parity

## Goal

- Extend `Flutter.Material.AppBar` parity for title layout, center-title defaults, and theme-driven title/toolbar text styling by adding:
  - `centerTitle` + platform/theme fallback (`widget -> theme -> platform`),
  - `titleSpacing` fallback (`widget -> theme -> default`),
  - `titleTextStyle` / `toolbarTextStyle` fallback (`widget -> theme -> defaults`),
  with focused regression coverage.

## Non-Goals

- No sliver app-bar implementation (`SliverAppBar`, scroll-under, dynamic elevation).
- No sample route/module restructuring.

## Context Budget Plan

- Budget: max 7 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- Expansion trigger:
  - Open Flutter Dart source only if default/title-layout mapping is ambiguous.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework behavior remains in framework libraries (`src/Flutter.Material`), not host controls.
  - Dart implementation stays source of truth for API/default/layout behavior where feasible.
  - Any known Dart divergence is documented in the same iteration.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/theme_data.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar_theme.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - `<none>`

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `ThemeData.cs`: add `AppBarThemeData` fields needed for app-bar fallback parity (`centerTitle`, `titleSpacing`, `toolbarTextStyle`, `titleTextStyle`) plus platform defaults.
  - `Scaffold.cs`: add app-bar precedence wiring for center-title/title-spacing/text-styles and apply text defaults through nested `DefaultTextStyle`.
  - `MaterialScaffoldTests.cs`: add regression tests for center-title precedence (`widget/theme/platform`), title-spacing precedence (`widget/theme/default`), and title/toolbar text-style precedence (`widget/theme/default`).
  - docs/changelog: record shipped parity scope and coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter "MaterialScaffoldTests"`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter "MaterialScaffoldTests|MaterialButtonsTests|FlutterHostInputTests|FocusTests"`
- New tests to add:
  - `AppBar_CenterTitleTrue_WrapsTitleInCenterAlign`
  - `AppBar_CenterTitle_DefaultsFromThemeAppBarTheme`
  - `AppBar_CenterTitle_ExplicitValue_OverridesThemeAppBarTheme`
  - `AppBar_CenterTitle_DefaultsFromPlatform_MacOS_WhenActionsCountLessThanTwo`
  - `AppBar_CenterTitle_DefaultsFromPlatform_MacOS_WithTwoActions_IsNotCentered`
  - `AppBar_TitleSpacing_AppliesHorizontalPaddingToTitle`
  - `AppBar_TitleSpacing_DefaultsFromThemeAppBarTheme`
  - `AppBar_TitleSpacing_WidgetValue_OverridesThemeAppBarTheme`
  - `AppBar_TitleTextStyle_DefaultsFromThemeAppBarTheme`
  - `AppBar_TitleTextStyle_DefaultsFromTextThemeTitleLarge`
  - `AppBar_TitleTextStyle_WidgetValue_OverridesThemeAppBarTheme`
  - `AppBar_ToolbarTextStyle_DefaultsFromThemeAppBarTheme_ForActionsText`
  - `AppBar_ToolbarTextStyle_WidgetValue_OverridesThemeAppBarTheme_ForActionsText`
  - `AppBar_NegativeTitleSpacing_Throws`
- Parity-risk scenarios covered:
  - `centerTitle` resolves by Flutter order: widget override first, then theme app-bar override, then platform fallback.
  - Platform fallback behavior matches Flutter baseline (`iOS/macOS` center only when actions count is below two).
  - `titleSpacing` resolves by Flutter order: widget override first, then theme app-bar override, then default spacing.
  - `titleTextStyle` / `toolbarTextStyle` resolve by Flutter order and are applied through inherited text style to title/actions.
  - Centered title remains centered when leading widget exists and actions are absent.
  - Title spacing is applied as horizontal insets around title content.
  - Invalid `titleSpacing` inputs fail fast.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` update not required (no sample route/module behavior change)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
