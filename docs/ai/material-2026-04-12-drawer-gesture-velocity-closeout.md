# Feature: material-2026-04-12-drawer-gesture-velocity-closeout

## Goal

- Close the remaining framework-scope `Scaffold` drawer gesture-controller gaps: drag-cancel settle behavior and true px/s drag-release velocity.

## Non-Goals

- Dedicated `DrawerTheme` object parity.
- Full Flutter `DrawerController` route/animation internals beyond current framework `Scaffold` composition.

## Context Budget Plan

- Budget: max 12 files in initial read.
- Entry files:
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter/Gestures/DragGestureRecognizer.cs`
  - `src/Flutter/Widgets/Gestures.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Flutter.Tests/GesturePipelineTests.cs`
- Expansion trigger:
  - Update button splash clipping only if full-suite validation exposes an existing focused regression.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Scaffold` drawer gesture-controller behavior.
- Completion checklist:
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
  - Gesture behavior remains in shared framework gesture primitives.
  - Drawer behavior remains in framework Material `Scaffold`, not in host adapters.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/drawer.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/gestures/monodrag.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log:
  - Dedicated drawer theming parity remains pending outside this gesture-controller close-out.

## Planned Changes

- Files edited:
  - `src/Flutter/Gestures/DragGestureRecognizer.cs`
  - `src/Flutter/Widgets/Gestures.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/GesturePipelineTests.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- Brief intent per file:
  - `DragGestureRecognizer.cs`: report drag-end primary velocity from timestamped pointer samples and expose cancel callback plumbing.
  - `Gestures.cs`: surface `onHorizontalDragCancel` / `onVerticalDragCancel` through framework gesture widgets.
  - `Scaffold.cs`: use px/s release velocity directly and settle drawer cancel by half-progress threshold.
  - `Buttons.cs`: fix existing rounded ripple clipping regression surfaced by validation.
  - Test files: add focused px/s velocity and drawer cancel settle coverage.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~GesturePipelineTests|FullyQualifiedName~MaterialScaffoldTests|FullyQualifiedName~TextButton_UsesRoundedClipForInkSplash"`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~TextButton_KeyboardActivation_IgnoresModifiedSpaceChord`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~Navigator_TryHandleBackButton_PopsWhenPossible`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~Radio_KeyboardActivation_Unselected_CallsOnChangedWithValue`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~Scaffold_OpenDrawer_AnimatesScrimOpacity_ToFullValue`
- New tests added:
  - `GestureBinding_HorizontalDragRecognizer_ReportsPrimaryVelocityInPixelsPerSecond`
  - `Scaffold_DragCancel_SettlesDrawerClosed_BelowHalfThreshold`
  - `Scaffold_DragCancel_SettlesDrawerOpen_AboveHalfThreshold`
- Parity-risk scenarios covered:
  - Drawer cancel below and above half-progress threshold.
  - Fling velocity computed as px/s instead of synthetic per-frame delta.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` update not needed because sample route structure did not change

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and focused tests passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps documented with blocker + next action
