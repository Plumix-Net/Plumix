# Feature: material-2026-04-11-bottom-navigation-bar-semantics-parity

## Goal

- Close the remaining `BottomNavigationBar` parity gap in one pass: semantics wrappers, framework tooltip wiring, and shifting selection animations (tile flex, icon/label transitions, radial background flood).

## Non-Goals

- No localization system work in this iteration (index semantics labels remain fixed English).
- No new sample route/module restructuring.

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/BottomNavigationBar.cs`
  - `src/Flutter.Material/Tooltip.cs`
  - `src/Flutter/Widgets/Semantics.cs`
  - `src/Flutter/Widgets/Gestures.cs`
  - `src/Flutter/Gestures/GestureBinding.cs`
  - `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_navigation_bar.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/tooltip.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/bottom_navigation_bar_item.dart`

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
  - Material behavior stays framework-owned in `src/Flutter.Material`.
  - Accessibility annotations remain framework-owned (`src/Flutter` + widget/render layers) without host control leakage.
  - Theme/default precedence stays unchanged (`widget -> inherited theme -> defaults`).

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_navigation_bar.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/tooltip.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/bottom_navigation_bar_item.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/BottomNavigationBar.cs`: index-label semantics string is currently framework-localized as fixed English (`"Tab {i} of {n}"`) because Material localization primitives are not yet implemented.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/BottomNavigationBar.cs`
  - `src/Flutter.Material/Tooltip.cs`
  - `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-11-bottom-navigation-bar-semantics-parity.md`
- Brief intent per file:
  - `BottomNavigationBar.cs`: migrate to stateful animation flow and add shifting transition choreography + tooltip wrappers + semantics wiring.
  - `Tooltip.cs`: add a baseline framework tooltip primitive for hover/long-press show and fade-out hide.
  - `MaterialBottomNavigationBarTests.cs`: add semantics coverage + tooltip hover + shifting width-animation regression checks.
  - docs/changelog: record shipped parity closure and remaining localized-label divergence.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialBottomNavigationBarTests`
- New tests added:
  - `BottomNavigationBar_Semantics_ExposeButtonSelectionAndIndexLabel`
  - `BottomNavigationBar_DisabledSemantics_OmitEnabledFlagAndTapAction`
  - `BottomNavigationBar_Tooltip_ShowsOnPointerEnter_AndHidesOnPointerExit`
  - `BottomNavigationBar_ShiftingSelectionChange_AnimatesTileWidths`
- Parity-risk scenarios covered:
  - hidden unselected labels still expose accessible labels,
  - tooltip message appears/disappears on hover transitions,
  - shifting selection transfer animates per-tile width ownership from source to target.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (not required; route/module structure unchanged)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps (if any) are documented with blocker + next action
