# Feature: material-2026-04-10-bottom-navigation-bar-baseline-parity

## Goal

- Add baseline framework Material `BottomNavigationBar` support and use it to structure the sample gallery menu into tabs (`Material`, `Cupertino`, `General`) with C#/Dart parity.

## Non-Goals

- No `BottomNavigationBarType.shifting` animation/background behavior in this iteration.
- No dedicated bottom-navigation theme data objects (`BottomNavigationBarThemeData`) in this iteration.
- No full label-visibility/semantics/tooltip parity surface in this iteration.

## Context Budget Plan

- Budget: max 16 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_navigation_bar.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/bottom_navigation_bar_item.dart`
- Expansion trigger:
  - Expand only for concrete parity closure needs: focused tests, C#/Dart sample menu restructuring, and tracking-doc updates.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `BottomNavigationBar`
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
  - Material behavior remains framework-owned in `src/Flutter.Material` (no host-control behavior leakage).
  - Sample parity remains synchronized between `src/Sample/Flutter.Net` and `dart_sample` in the same iteration.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_navigation_bar.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/bottom_navigation_bar_item.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/BottomNavigationBar.cs`: current scope intentionally implements fixed-layout baseline only (no shifting mode animation/background).
  - `src/Flutter.Material/BottomNavigationBar.cs`: no dedicated `BottomNavigationBarThemeData` plumbing in this iteration; defaults resolve directly from existing `ThemeData`.
  - `src/Flutter.Material/BottomNavigationBar.cs`: label-visibility toggles are supported via constructor flags, but full Flutter semantics/tooltip surface is deferred.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/BottomNavigationBar.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/PARITY_MATRIX.md`
- Brief intent per file:
  - `BottomNavigationBar.cs`: add framework control and item primitives with fixed-layout baseline.
  - sample gallery files: split demo menu into bottom-tab sections while preserving route map behavior.
  - test file: add focused bottom-navigation coverage.
  - docs/changelog: record scope, status, and remaining divergence.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialBottomNavigationBarTests`
- New tests to add:
  - `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`
- Parity-risk scenarios covered:
  - constructor guard behavior (`items` count, `currentIndex` range),
  - theme-default color mapping for selected/unselected labels,
  - selected `activeIcon` rendering,
  - tap callback index dispatch.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

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
