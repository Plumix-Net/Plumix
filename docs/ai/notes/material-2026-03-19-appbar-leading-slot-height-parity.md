# Feature: material-2026-03-19-appbar-leading-slot-height-parity

## Goal

- Align `AppBar` leading-slot geometry with Flutter toolbar behavior by constraining leading slot height to effective toolbar height (in addition to leading width).

## Non-Goals

- No implied leading/action behavior changes.
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
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/navigation_toolbar.dart`
- Expansion trigger:
  - Open additional files only if compile/test feedback requires it.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material layout behavior remains in `src/Flutter.Material`.
  - Parity-sensitive toolbar geometry is validated via framework tests.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/navigation_toolbar.dart`
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
  - `src/Flutter.Material/Scaffold.cs`: constrain leading slot with both effective `leadingWidth` and effective `toolbarHeight`.
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`: add focused assertion for tight leading slot width+height constraints.
  - docs files: record delivered geometry-parity update.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- New tests to add:
  - `AppBar_LeadingSlot_IsConstrainedByLeadingWidthAndToolbarHeight`
- Parity-risk scenarios covered:
  - Leading slot should size consistently with toolbar height in app-bar layouts.

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
