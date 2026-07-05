# Feature: material-2026-04-11-bottom-navigation-bar-parity-closeout

## Goal

- Close the remaining high-priority `BottomNavigationBar` parity gaps from the baseline pass by adding type/theming/default-precedence behavior and focused regression coverage.

## Non-Goals

- Full Flutter shifting animation choreography (`_Circle` radial splash + per-tile animated flex/label transitions).
- Dedicated tooltip widget rendering and full semantics-wrapper parity (requires missing framework primitives in current scope).

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/BottomNavigationBar.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_navigation_bar.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_navigation_bar_theme.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/bottom_navigation_bar_item.dart`
- Expansion trigger:
  - Expand only to update tracking docs and verify cross-module theme wiring.

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
  - Material behavior stays framework-owned (`src/Flutter.Material`) and does not leak into host adapters.
  - Theme precedence is explicit and follows framework convention (`widget -> inherited theme -> defaults`).
  - Sample route/module parity remains unchanged between C# and Dart samples.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_navigation_bar.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_navigation_bar_theme.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/bottom_navigation_bar_item.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/BottomNavigationBar.cs`: full Flutter shifting animation choreography is not implemented yet (no radial splash/flex/label animation pipeline).
  - `src/Flutter.Material/BottomNavigationBar.cs`: `tooltip` field exists on `BottomNavigationBarItem`, but dedicated tooltip/semantics wrappers remain pending until corresponding framework primitives land.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/BottomNavigationBar.cs`
  - `src/Flutter.Material/BottomNavigationBarTheme.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-11-bottom-navigation-bar-parity-closeout.md`
- Brief intent per file:
  - `BottomNavigationBar.cs`: add type/theming/style/default precedence and shifting background behavior.
  - `BottomNavigationBarTheme.cs`: introduce inherited theme primitive for bar defaults.
  - `ThemeData.cs`: wire `BottomNavigationBarTheme` into global Material theme surface.
  - `MaterialBottomNavigationBarTests.cs`: add focused regression checks for new precedence and type behavior.
  - docs/changelog: record shipped behavior and remaining documented divergence.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialBottomNavigationBarTests`
  - `dotnet build src/Flutter.Net.sln -c Debug`
- New tests to add:
  - `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`
- Parity-risk scenarios covered:
  - theme defaults and widget override precedence,
  - auto `fixed`/`shifting` type behavior and shifting background resolution,
  - label visibility defaults by type and explicit overrides,
  - label-style color precedence and icon-theme input guards.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed; not required in this pass because sample route/module structure did not change)

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
