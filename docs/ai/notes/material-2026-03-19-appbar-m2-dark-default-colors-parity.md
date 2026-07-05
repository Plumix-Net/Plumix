# Feature: material-2026-03-19-appbar-m2-dark-default-colors-parity

## Goal

- Align M2 dark-theme `AppBar` default background/foreground fallback with Flutter behavior.

## Non-Goals

- No changes to M3 default color paths.
- No sample route/module updates.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `AGENTS.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Expansion trigger:
  - Open extra files only if compile/test output indicates unresolved behavior dependencies.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Theme-driven default behavior stays explicit in framework layers.
  - Default resolution remains deterministic and test-covered across mode/brightness branches.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - Framework uses simplified theme tokens (`CanvasColor`/`OnSurfaceColor`) to represent M2 dark surface/onSurface behavior.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `src/Flutter.Material/ThemeData.cs`: add `Brightness` to theme model with default `Light`.
  - `src/Flutter.Material/Scaffold.cs`: branch M2 app-bar default colors by brightness.
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`: add focused M2 dark fallback tests and brightness default test.
  - docs files: capture parity and coverage updates.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- New tests to add:
  - `ThemeData_DefaultsBrightnessToLight`
  - `Scaffold_WithAppBar_UsesThemeCanvasColorForAppBarBackground_WhenUseMaterial3IsDisabledAndBrightnessDark`
  - `AppBar_DefaultTitle_UsesThemeOnSurfaceColor_WhenUseMaterial3IsDisabledAndBrightnessDark`
- Parity-risk scenarios covered:
  - M2 dark fallback should use surface-like colors instead of primary/onPrimary defaults.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` update not required (no sample route/module change in this iteration)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
