# Feature: material-2026-03-14-appbar-theme-colors-toolbar-height-parity

## Goal

- Extend `Flutter.Material.AppBar` parity by wiring `AppBarThemeData` defaults for color and toolbar sizing with Flutter-like precedence:
  - `backgroundColor`: `widget -> appBarTheme -> theme primary`
  - `foregroundColor`: `widget -> appBarTheme -> theme onPrimary`
  - `toolbarHeight`: `widget -> appBarTheme -> default 56`
- Add focused regression coverage for precedence and invalid theme toolbar-height guard.

## Non-Goals

- No sliver app bar work (`SliverAppBar`, scroll-under, dynamic elevation).
- No sample gallery route/module restructuring.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
- Expansion trigger:
  - Open Flutter Dart sources only if fallback/default precedence is ambiguous.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework behavior remains inside framework libraries (`src/Flutter.Material`).
  - Dart implementation stays source of truth for defaults/precedence.
  - Validation and behavior changes are covered by focused framework tests.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar_theme.dart`
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
  - `ThemeData.cs`: extend `AppBarThemeData` with `BackgroundColor`, `ForegroundColor`, `ToolbarHeight`.
  - `Scaffold.cs`: resolve app-bar background/foreground/toolbarHeight by Flutter-like precedence and validate resolved toolbar height.
  - `MaterialScaffoldTests.cs`: cover widget-vs-theme precedence for the new fields and invalid themed toolbar height guard.
  - docs/changelog: record shipped parity scope and test coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialScaffoldTests`
- New tests to add:
  - `AppBar_BackgroundColor_DefaultsFromThemeAppBarTheme`
  - `AppBar_BackgroundColor_WidgetValue_OverridesThemeAppBarTheme`
  - `AppBar_ForegroundColor_DefaultsFromThemeAppBarTheme`
  - `AppBar_ForegroundColor_WidgetValue_OverridesThemeAppBarTheme`
  - `AppBar_ToolbarHeight_DefaultsFromThemeAppBarTheme`
  - `AppBar_ToolbarHeight_WidgetValue_OverridesThemeAppBarTheme`
  - `AppBar_NonPositiveThemeToolbarHeight_Throws`
- Parity-risk scenarios covered:
  - Theme fallback for app-bar colors remains deterministic and overrideable from widget-level inputs.
  - Toolbar-height fallback and override semantics match Flutter ordering.
  - Invalid theme toolbar heights fail fast instead of creating undefined layout behavior.

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
