# Feature: material-2026-03-19-appbar-leading-icon-theme-mode-aware-defaults

## Goal

- Align `AppBar` leading `iconTheme` default fallback with Flutter Material mode behavior for M3/M2.

## Non-Goals

- No changes to explicit `iconTheme`/`actionsIconTheme` override precedence.
- No sample route/module updates.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `AGENTS.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Expansion trigger:
  - Open extra files only if compile/test output indicates dependency issues.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - App-bar defaults remain framework-owned and explicit.
  - Mode switch only changes behavior where Flutter source indicates it.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
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
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `src/Flutter.Material/Scaffold.cs`: introduce mode-aware default leading icon theme helper.
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`: add focused tests for M3 and M2 default leading icon-theme behavior.
  - docs files: capture parity and coverage updates.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- New tests to add:
  - `AppBar_IconTheme_DefaultsToOnSurfaceAndSize24_ForLeading_WhenUseMaterial3IsEnabled`
  - `AppBar_IconTheme_DefaultsToOnPrimary_ForLeading_WhenUseMaterial3IsDisabled`
- Parity-risk scenarios covered:
  - M3 should provide default leading icon size `24`; M2 should keep non-forced size fallback.

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
