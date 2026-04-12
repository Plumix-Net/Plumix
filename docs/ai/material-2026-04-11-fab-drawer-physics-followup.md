# Feature: material-2026-04-11-fab-drawer-physics-followup

## Goal

- Close the next Material parity gaps for `Scaffold` drawer interaction and `FloatingActionButton` API surface in one iteration.

## Non-Goals

- Full Flutter `Hero` transition runtime.
- Host-level mouse cursor application.
- Haptic/audio feedback dispatch.
- Dedicated drawer theming parity.

## Context Budget Plan

- Budget: max 16 files in initial read.
- Entry files:
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Material/FloatingActionButton.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
- Expansion trigger:
  - Add minimal shared primitives if required to keep the control closeout in this same pass (`Clip`, `MouseCursor` placeholders).

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Scaffold` (`DrawerController`-adjacent behavior in framework scope) and `FloatingActionButton`.
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
  - Material control parity should follow Flutter source defaults and precedence order.
  - Framework behavior should remain in framework layers (`src/Flutter*`) without host-specific control logic.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/drawer.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/floating_action_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `FloatingActionButton`: runtime `Hero` animation pipeline is not implemented yet; `heroTag` is API-level parity only for now.
  - `FloatingActionButton`: `mouseCursor` and `enableFeedback` are currently stored but not wired to host runtime cursor/haptics.
  - `Scaffold` drawer: drag-cancel parity and true px/s velocity estimation were closed in the follow-up note `docs/ai/material-2026-04-12-drawer-gesture-velocity-closeout.md`.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Material/FloatingActionButton.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter/UI/Clip.cs`
  - `src/Flutter/Widgets/MouseCursor.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
- Brief intent per file:
  - `Scaffold.cs`: align drawer edge width and settle constants/decision flow with Flutter defaults.
  - `FloatingActionButton.cs`: extend API parity (`heroTag`, `mouseCursor`, `enableFeedback`, `clipBehavior`) and wire clip behavior.
  - `Buttons.cs`: allow per-control clip behavior in `MaterialButtonCore`.
  - `Clip.cs` and `MouseCursor.cs`: minimal parity primitives for API compatibility.
  - test files: add focused coverage for new behavior paths.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
  - `src/Flutter.Tests/MaterialBottomNavigationBarTests.cs`
- New tests to add:
  - `Scaffold_EdgeDrag_UsesMediaPaddingForStartDrawerActivationWidth`
  - `FloatingActionButton_DefaultClipBehavior_DoesNotInsertClipRRect`
  - `FloatingActionButton_ClipBehaviorHardEdge_InsertsClipRRect`
  - `FloatingActionButton_StoresHeroTagMouseCursorAndEnableFeedback`
- Parity-risk scenarios covered:
  - Drawer open gesture from safe-area-extended edge zone.
  - FAB clipping behavior parity with `clipBehavior` default vs explicit clip.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [ ] `docs/ai/PARITY_MATRIX.md` updated (if needed)

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
