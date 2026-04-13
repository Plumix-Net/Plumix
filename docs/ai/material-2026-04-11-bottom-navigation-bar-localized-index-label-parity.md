# Feature: material-2026-04-11-bottom-navigation-bar-localized-index-label-parity

## Goal

- Close the remaining `BottomNavigationBar` semantics-index-label divergence by replacing fixed English formatting with a framework Material localization primitive.

## Non-Goals

- No full app-wide localization delegate system in this iteration.
- No sample route/module structure changes.
- No behavior changes for tooltip or shifting animation paths.

## Context Budget Plan

- Budget: max 12 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/BottomNavigationBar.cs`
  - `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/material_localizations.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_navigation_bar.dart`

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `BottomNavigationBar` semantics index-label localization path.
- Completion checklist (must be closed in this iteration unless explicitly blocked):
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned in `src/Flutter.Material`.
  - Semantics annotations remain framework-owned and do not move into host adapters.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/material_localizations.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_navigation_bar.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - none for bottom-navigation index-label localization in current framework scope.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/MaterialLocalizations.cs`
  - `src/Flutter.Material/BottomNavigationBar.cs`
  - `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-11-bottom-navigation-bar-localized-index-label-parity.md`
- Brief intent per file:
  - add framework Material localization primitive with default tab-label formatter;
  - route bottom-navigation index semantics through localization provider;
  - add focused regression test for local localization override;
  - update docs/changelog tracking and remove fixed-English divergence note.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialBottomNavigationBarTests`
- New tests added:
  - `BottomNavigationBar_SemanticsIndexLabel_UsesMaterialLocalizationsOverride`
- Parity-risk scenarios covered:
  - default localizations path preserves current English index labels;
  - local subtree override changes semantics index labels without altering tile selection/enabled behavior.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (not required; sample structure/routes unchanged)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps (if any) are documented with blocker + next action
