# Feature: navigation-2026-04-13-nested-back-dispatch

## Goal

- Close nested navigator host back-dispatch coverage by validating that back handling prioritizes the innermost active navigator and falls back to outer stacks when inner stacks cannot pop.

## Non-Goals

- Reworking `NavigatorBackButtonDispatcher` architecture.
- Platform-specific back gesture recognizers outside current framework back-dispatch APIs.

## Context Budget Plan

- Budget: max 7 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/NavigationTests.cs`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
- Expansion trigger:
  - Add tracking docs after targeted nested back scenarios pass.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Navigator nested back-dispatch behavior
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
  - Back dispatch must not skip pop-eligible nested navigators.
  - Outer route stacks must remain reachable when inner navigator stacks cannot consume back events.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/navigator.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - Current implementation keeps dispatcher as framework-global stack of handlers; this iteration closes observable nested precedence behavior through focused regression coverage.

## Planned Changes

- Files edited:
  - `src/Flutter.Tests/NavigationTests.cs`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
  - `docs/ai/navigation-2026-04-13-nested-back-dispatch.md`
- Brief intent per file:
  - `NavigationTests.cs`: add explicit nested navigator back-dispatch precedence/fallback tests.
  - tracking docs/changelog: mark nested host back-dispatch gap as covered in current scope.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~NavigationTests|FullyQualifiedName~HeroNavigatorTests|FullyQualifiedName~MaterialFloatingActionButtonTests"`
- New tests added:
  - `Navigator_TryHandleBackButton_PrefersInnermostNavigator` (`NavigationTests`)
  - `Navigator_TryHandleBackButton_FallsBackToOuterNavigatorWhenInnerCannotPop` (`NavigationTests`)
- Parity-risk scenarios covered:
  - Nested navigator can pop: back dispatch is consumed by inner stack.
  - Nested navigator cannot pop: same back dispatch pops outer stack.

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
