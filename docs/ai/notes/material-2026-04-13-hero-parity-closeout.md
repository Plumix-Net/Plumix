# Feature: material-2026-04-13-hero-parity-closeout

## Goal

- Close the remaining Hero parity gaps by implementing nested-navigator hero orchestration and `transitionOnUserGestures` behavior for user-gesture route pops.

## Non-Goals

- New hero animation curve families beyond current rect-tween support.
- Host-driven back-swipe gesture implementation details outside framework route APIs.

## Context Budget Plan

- Budget: max 10 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Update tracking docs and feature note once behavior and focused tests are complete.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Hero transition orchestration (`transitionOnUserGestures` + nested navigator candidate resolution)
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
  - User-gesture hero transitions must only run when both source and destination heroes opt in.
  - Nested navigator heroes must only contribute to ancestor navigator flights when they belong to the nested navigator's active route.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/heroes.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - Uses framework registration model (multi-scope hero registration) rather than Flutter's route-subtree scan helper, with equivalent observable candidate selection for covered nested/current-route behavior.

## Planned Changes

- Files edited:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-13-hero-parity-closeout.md`
- Brief intent per file:
  - `Hero.cs`: add `transitionOnUserGestures` API, support multi-scope registration for nested navigators, and resolve placeholders/snapshots by active route registration set.
  - `Navigation.cs`: carry user-gesture flag through hero transition sessions and pass it into flight creation.
  - `HeroNavigatorTests.cs`: add nested navigator and user-gesture hero transition regressions.
  - docs/changelog: reflect closeout and remove remaining hero parity gap notes.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~HeroNavigatorTests|FullyQualifiedName~MaterialFloatingActionButtonTests"`
- New tests added:
  - `Navigator_Push_WithNestedNavigatorHeroes_ShowsBothRoutesDuringFlight` (`HeroNavigatorTests`)
  - `Navigator_Pop_FromUserGesture_SkipsHeroFlight_WhenTransitionOnUserGesturesDisabled` (`HeroNavigatorTests`)
  - `Navigator_Pop_FromUserGesture_UsesHeroFlight_WhenBothHeroesAllowGestureTransition` (`HeroNavigatorTests`)
- Parity-risk scenarios covered:
  - Shared-tag heroes inside nested navigators now animate during outer navigator transitions.
  - User-gesture pop transitions skip hero flights by default.
  - User-gesture pop transitions animate heroes when both sides opt in.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` update not needed (no sample route/module change)

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
