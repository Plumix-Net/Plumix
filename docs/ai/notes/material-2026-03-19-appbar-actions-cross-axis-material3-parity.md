# Feature: material-2026-03-19-appbar-actions-cross-axis-material3-parity

## Goal

- Align `AppBar` actions-row cross-axis behavior with Flutter `useMaterial3` mode semantics and add the missing `ThemeData.UseMaterial3` switch in framework theming.

## Non-Goals

- No app-bar scroll-under/elevation behavior changes.
- No sample route/module additions.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `AGENTS.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/theme_data.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Expansion trigger:
  - Open extra files only if compile/test feedback indicates a dependency break.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains in framework widgets/rendering layers.
  - Theme-driven widget behavior remains explicit and test-covered.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/theme_data.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - none

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `src/Flutter.Material/ThemeData.cs`: add `UseMaterial3` switch with Flutter default (`true`).
  - `src/Flutter.Material/Scaffold.cs`: branch app-bar actions-row cross-axis alignment by `theme.UseMaterial3`.
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`: assert default `UseMaterial3` and both M3/M2 alignment paths.
  - docs files: capture new parity status and coverage.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- New tests to add:
  - `ThemeData_DefaultsUseMaterial3ToTrue`
  - `AppBar_ActionsRow_UsesStretchCrossAxisAlignment_WhenUseMaterial3IsDisabled`
- Parity-risk scenarios covered:
  - M3 default app-bar actions-row alignment (`center`) and M2 fallback alignment (`stretch`) stay deterministic.

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
