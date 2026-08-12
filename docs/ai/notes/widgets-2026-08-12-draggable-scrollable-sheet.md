# Feature: draggable-scrollable-sheet

## Goal

- `widgets/draggable_scrollable_sheet.dart` ported strictly: a sheet that resizes as the user drags
  it, hands the drag over to its child scrollable at the top, snaps on release, and can be observed
  and driven programmatically.

## Non-Goals

- Adopting the new notification in Material `BottomSheet`/`Scaffold` (that closes a different row of
  `docs/ai/DIVERGENCES.md` and is a `BottomSheet` parity pass).
- `ModalRoute.BuildModalBarrier` ownership and semantics `hitTestBehavior`, the other two halves of
  the `BottomSheet` divergence.

## Delivery Scope

- Target control: `DraggableScrollableSheet` (+ `DraggableScrollableController`,
  `DraggableScrollableNotification`, `DraggableScrollableActuator`).
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics (the control paints nothing itself)
  - [x] Focused tests for this control

## Dart Reference Mapping

- Source of truth: `flutter-src/packages/flutter/lib/src/widgets/draggable_scrollable_sheet.dart`
  (1211 lines, read through `docs/ai/DART_SPEC_PROTOCOL.md`) and
  `flutter-src/packages/flutter/test/widgets/draggable_scrollable_sheet_test.dart`.
- Divergence: one row added to `docs/ai/DIVERGENCES.md` for the interrupted-`AnimateTo` task and the
  simulation-only `AnimationController.Velocity`.

## Primitives landed first

- `ScrollPosition.Absorb`, plus virtual `ApplyUserOffset`/`GoBallistic`/`GoIdle`/`BeginActivity`/
  `Drag`; `ScrollActivity.UpdateDelegate` and `ScrollDragController.UpdatePosition` carry an in-flight
  activity and drag across a position replacement.
- `ScrollPosition.NotificationContext` stands in for Flutter's `ScrollContext.notificationContext`;
  Plumix has no `ScrollContext`, so `ScrollableState` hands the position its own context.
- Virtual `ScrollController.Attach`/`Detach` (the sheet's controller needs the detach hook).
- `AnimationController.Unbounded`/`AnimateWith(Simulation)`/`Velocity`, and
  `ChangeNotifier.HasListeners`.

## Notes for the next agent

- The position's activity during a real fling is the **drag** activity, not idle. That matters:
  `IdleScrollActivity.ApplyNewDimensions` calls `GoBallistic(0)`, and the sheet's `BeginActivity`
  override stops its ballistic controllers — so a test that calls `GoBallistic` without first
  beginning a drag has its fling cancelled by the very relayout the resizing sheet causes.
- Test classes now run serially (`src/Plumix.Tests/AssemblyInfo.cs`). `Scheduler` is process-wide, so
  parallel classes rewound each other's tickers. Clock-driven tests must also advance their own
  monotonic clock rather than repeatedly pumping `Scheduler.CurrentSeconds + dt`, which advances
  tickers by real elapsed time instead of `dt`.

## Done Criteria

- [x] One full control closed end-to-end
- [x] Tests updated and passing (`2316` tests green)
- [x] Parity constraints satisfied
- [x] Remaining gaps documented in `docs/ai/DIVERGENCES.md`
