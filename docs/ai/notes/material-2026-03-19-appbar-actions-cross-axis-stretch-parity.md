# Feature: material-2026-03-19-appbar-actions-cross-axis-stretch-parity

## Goal

- Align `AppBar` actions-row cross-axis alignment with Flutter toolbar behavior path by using `CrossAxisAlignment.Stretch` in the actions row.

## Non-Goals

- No `useMaterial3` switch introduction in this iteration.
- No sample route/module changes.

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
  - Open additional files only if compile/test feedback requires it.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material toolbar layout behavior remains framework-owned.
  - `Widget -> Element -> RenderObject` property wiring remains explicit.

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
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `src/Flutter.Material/Scaffold.cs`: set actions row `crossAxisAlignment` to `Stretch`.
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`: verify actions row cross-axis alignment value.
  - docs files: record this parity-hardening increment.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- New tests to add:
  - none (extend `AppBar_ActionsRow_DoesNotApplyExtraSpacing` assertion)
- Parity-risk scenarios covered:
  - Actions row cross-axis behavior should match framework-selected Flutter toolbar alignment path.

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
