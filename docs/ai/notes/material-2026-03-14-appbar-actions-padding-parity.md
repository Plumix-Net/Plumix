# Feature: material-2026-03-14-appbar-actions-padding-parity

## Goal

- Add Flutter-like `actionsPadding` support to framework `AppBar` and `AppBarThemeData` with precedence `widget -> appBarTheme -> default zero`.
- Add focused regression tests for theme fallback and widget override.

## Non-Goals

- No sliver app-bar or scroll-under behavior.
- No sample route/module changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/ai/TEST_MATRIX.md`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Expansion trigger:
  - Open additional framework files only if action-row composition causes layout ambiguity.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework behavior stays inside `src/Flutter.Material`.
  - AppBar defaults and precedence are aligned to Dart source as source of truth.

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
  - `ThemeData.cs`: add `AppBarThemeData.ActionsPadding`.
  - `Scaffold.cs`: add `AppBar.actionsPadding`, resolve effective value by precedence, and apply it around the actions row.
  - `MaterialScaffoldTests.cs`: add tests for theme fallback and widget override.
  - docs/changelog: record shipped parity increment and coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialScaffoldTests`
- New tests to add:
  - `AppBar_ActionsPadding_DefaultsFromThemeAppBarTheme`
  - `AppBar_ActionsPadding_WidgetValue_OverridesThemeAppBarTheme`
- Parity-risk scenarios covered:
  - `actionsPadding` fallback order and override semantics.
  - Actions row composition remains stable while adding padding wrapper.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` update not required (no route/module behavior changes)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
