# Feature: material-2026-03-19-appbar-mode-aware-default-colors

## Goal

- Align `AppBar` default background/foreground fallback with Flutter Material mode behavior (M3 vs M2).

## Non-Goals

- No scroll-under color/elevation behavior in this iteration.
- No sample route/module changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `AGENTS.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Expansion trigger:
  - Open extra files only if compile/test output indicates missing dependencies.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Theme-driven app-bar behavior remains framework-owned.
  - Widget/theme/default precedence remains explicit and test-covered.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - Simplified token mapping in framework theme model: M3 app-bar defaults map to `CanvasColor`/`OnSurfaceColor` (closest available surface tokens), while M2 maps to `PrimaryColor`/`OnPrimaryColor`.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `src/Flutter.Material/Scaffold.cs`: resolve default background/foreground via `UseMaterial3` mode-aware helpers.
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`: add mode-specific default color tests and update existing expectation paths.
  - docs files: capture behavior and coverage updates.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- New tests to add:
  - `Scaffold_WithAppBar_UsesThemeCanvasColorForAppBarBackground_WhenUseMaterial3IsEnabled`
  - `Scaffold_WithAppBar_UsesThemePrimaryColorForAppBarBackground_WhenUseMaterial3IsDisabled`
  - `AppBar_DefaultTitle_UsesThemeOnSurfaceColor_WhenUseMaterial3IsEnabled`
  - `AppBar_DefaultTitle_UsesThemeOnPrimaryColor_WhenUseMaterial3IsDisabled`
- Parity-risk scenarios covered:
  - Mode switch should deterministically update app-bar default color path while preserving widget/theme override precedence.

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
