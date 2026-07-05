# Feature: material-2026-03-19-flex-main-axis-size-widget-wiring

## Goal

- Expose `mainAxisSize` at the framework widget layer (`Flex`/`Row`/`Column`) and apply `MainAxisSize.Min` in `AppBar` actions row for closer Flutter toolbar layout parity.

## Non-Goals

- No change to render-level `RenderFlex` behavior.
- No sample route/module changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `AGENTS.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Expansion trigger:
  - Open additional files only if compile/test feedback requires it.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Widget-to-render property wiring stays explicit (`Widget -> RenderObject`).
  - Material behavior remains in framework libraries, not host glue.

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
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `src/Flutter/Widgets/Basic.cs`: add widget-level `mainAxisSize` property on `Flex` and pass-through in `Row`/`Column`.
  - `src/Flutter.Material/Scaffold.cs`: set app-bar actions row to `mainAxisSize: MainAxisSize.Min`.
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`: verify actions-row render flex uses `MainAxisSize.Min`.
  - docs files: track delivered parity delta.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- New tests to add:
  - none (extend `AppBar_ActionsRow_DoesNotApplyExtraSpacing` assertion)
- Parity-risk scenarios covered:
  - App-bar actions container should not claim unnecessary main-axis width.

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
