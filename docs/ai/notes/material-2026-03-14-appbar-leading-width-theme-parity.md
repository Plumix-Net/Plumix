# Feature: material-2026-03-14-appbar-leading-width-theme-parity

## Goal

- Align `Flutter.Material.AppBar` leading-slot width fallback with Flutter semantics by wiring `AppBarThemeData.LeadingWidth` resolution (`widget -> appBarTheme -> 56`) and validating resolved value.
- Add focused regression coverage for theme fallback, widget override, and invalid themed leading width.

## Non-Goals

- No changes to sliver app bars or advanced Material top-app-bar variants.
- No sample route or module changes.

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
  - Inspect Flutter Dart source when precedence defaults are ambiguous.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework behavior stays in framework libraries (`src/Flutter.Material`).
  - Dart defaults/precedence remain source of truth.
  - Non-trivial behavior changes are covered by focused tests.

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
  - `ThemeData.cs`: add `AppBarThemeData.LeadingWidth`.
  - `Scaffold.cs`: resolve effective leading width by Flutter precedence and validate resolved value.
  - `MaterialScaffoldTests.cs`: add precedence/guard tests for leading width.
  - docs/changelog: record shipped parity increment and coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialScaffoldTests`
- New tests to add:
  - `AppBar_LeadingWidth_DefaultsFromThemeAppBarTheme`
  - `AppBar_LeadingWidth_WidgetValue_OverridesThemeAppBarTheme`
  - `AppBar_NonPositiveThemeLeadingWidth_Throws`
- Parity-risk scenarios covered:
  - Leading slot width fallback and override order follows Flutter.
  - Invalid themed leading width fails fast.

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
