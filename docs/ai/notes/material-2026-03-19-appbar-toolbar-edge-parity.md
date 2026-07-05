# Feature: material-2026-03-19-appbar-toolbar-edge-parity

## Goal

- Align framework `AppBar` toolbar-edge geometry with Flutter defaults by removing framework-only extra spacing around actions and at toolbar outer edges.

## Non-Goals

- Do not implement `automaticallyImplyLeading`/`automaticallyImplyActions`.
- Do not introduce new Material controls in this iteration.
- Do not change sample route structure.

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
  - Open additional files only if required to satisfy compilation/test failures or docs tracking updates.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework behavior remains in `src/Flutter.Material` without host-side workaround logic.
  - Dart parity defaults for existing Flutter widgets are treated as source of truth.

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
  - `src/Flutter.Material/Scaffold.cs`: remove framework-only extra app-bar spacing defaults (`actions` row spacing and outer toolbar padding default).
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`: add focused regression coverage for zero outer padding default and zero actions-row spacing.
  - `docs/FRAMEWORK_PLAN.md`: record M4 progress update.
  - `docs/ai/TEST_MATRIX.md`: track new `AppBar` geometry coverage.
  - `CHANGELOG.md`: record shipped parity hardening.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- New tests to add:
  - `AppBar_DefaultOuterPadding_IsZero`
  - `AppBar_ActionsRow_DoesNotApplyExtraSpacing`
- Parity-risk scenarios covered:
  - Framework app-bar should not inject extra trailing-actions gap.
  - Framework app-bar should not add non-Flutter horizontal outer toolbar padding by default.

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
