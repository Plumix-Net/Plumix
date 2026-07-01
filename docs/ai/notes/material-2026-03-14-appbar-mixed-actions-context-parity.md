# Feature: material-2026-03-14-appbar-mixed-actions-context-parity

## Goal

- Harden app-bar parity with focused regression tests for mixed actions context where both text style and icon theme inheritance must be correct simultaneously.
- Validate icon-theme fallback chain edges:
  - widget `iconTheme` fallback for actions when `actionsIconTheme` is missing,
  - foreground-color fallback when icon theme is provided without explicit color.

## Non-Goals

- No new app-bar API fields in this iteration.
- No sample route/module changes.

## Context Budget Plan

- Budget: max 6 files in initial read.
- Entry files:
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter/Widgets/IconTheme.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Open additional files only if inherited context lookup does not match render tree composition.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Parity-first behavior remains anchored in Flutter precedence rules.
  - New assertions focus on framework-level inherited context behavior.

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
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `MaterialScaffoldTests.cs`: add fallback and mixed-context coverage for actions slot text/icon inherited state.
  - docs/changelog: record parity-hardening coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialScaffoldTests`
- New tests to add:
  - `AppBar_IconTheme_WithNullColor_FallsBackToForeground_ForLeading`
  - `AppBar_ActionsIconTheme_FallsBackToWidgetIconTheme_WhenActionsThemeMissing`
  - `AppBar_ActionsIconTheme_WithNullColor_FallsBackToForeground_ForActions`
  - `AppBar_Actions_ReceiveToolbarTextStyle_AndActionsIconTheme`
- Parity-risk scenarios covered:
  - Correct fallback order under partial icon-theme configuration.
  - Simultaneous inherited `DefaultTextStyle` and `IconTheme` behavior in actions subtree.

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
