# Feature: material-2026-03-14-appbar-icon-theme-parity

## Goal

- Add minimal icon-theme infrastructure and wire Flutter-like app-bar icon-theme precedence:
  - `iconTheme`: `widget -> appBarTheme -> foreground-color fallback`
  - `actionsIconTheme`: `widget -> appBarTheme -> iconTheme -> foreground-color fallback`
- Add focused regression coverage for leading/actions icon-theme resolution.

## Non-Goals

- No icon glyph widget implementation (`Icon`, icon fonts) in this iteration.
- No sliver app-bar/scroll-under behavior.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Flutter/Widgets/DefaultTextStyle.cs`
  - `CHANGELOG.md`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Expansion trigger:
  - Add widget-layer primitive only if app-bar icon-theme propagation cannot be represented by existing inherited infrastructure.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - New shared behavior placed in framework widgets layer (`src/Flutter/Widgets`).
  - App-bar defaults/precedence mirror Flutter source ordering.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar_theme.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/icon_theme.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - `IconThemeData` is intentionally minimal for now (`Color`, `Size`) to support app-bar parity without introducing full icon-rendering stack; extended fields can be added when icon controls are ported.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/IconTheme.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `IconTheme.cs`: add inherited `IconTheme` + `IconThemeData` baseline.
  - `ThemeData.cs`: extend `AppBarThemeData` with `IconTheme` and `ActionsIconTheme`.
  - `Scaffold.cs`: add `AppBar.iconTheme/actionsIconTheme` and apply resolved icon themes to leading/actions slots.
  - tests/docs: lock precedence behavior and document shipped increment.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialScaffoldTests`
- New tests to add:
  - `AppBar_IconTheme_DefaultsFromThemeAppBarTheme_ForLeading`
  - `AppBar_IconTheme_WidgetValue_OverridesThemeAppBarTheme_ForLeading`
  - `AppBar_ActionsIconTheme_DefaultsFromThemeAppBarTheme_ForActions`
  - `AppBar_ActionsIconTheme_WidgetValue_OverridesThemeAppBarTheme_ForActions`
  - `AppBar_ActionsIconTheme_FallsBackToAppBarIconTheme_WhenActionsThemeMissing`
- Parity-risk scenarios covered:
  - Icon-theme precedence and fallback chains for leading/actions slots.
  - Actions icon theme fallback to app-bar icon theme when dedicated actions theme is absent.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` update not required (no route/module behavior change)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
