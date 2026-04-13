# Feature: material-2026-04-13-hero-mode-disable-parity

## Goal

- Add Flutter-like `HeroMode(enabled: ...)` gating so heroes in a disabled subtree do not participate in navigator hero flights.

## Non-Goals

- Nested-navigator hero-controller orchestration changes.
- Hero gesture-transition APIs (`transitionOnUserGestures`) and advanced flight choreography.

## Context Budget Plan

- Budget: max 9 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Add/update docs + feature note once behavior + focused test are in place.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Hero subtree participation gating (`HeroMode.enabled`)
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
  - Heroes disabled by local policy must not be registered as flight candidates.
  - Disabled heroes must remain visible and avoid hidden-placeholder substitution during matching-tag transitions.

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
  - Current scope uses inherited `HeroModeScope` registration gating in C# (instead of Flutter's on-demand subtree traversal), with equivalent observable behavior for disabled-subtree opt-out.

## Planned Changes

- Files edited:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-13-hero-mode-disable-parity.md`
- Brief intent per file:
  - `Hero.cs`: add `HeroMode` widget + inherited scope and gate hero registration/placeholder usage when disabled.
  - `HeroNavigatorTests.cs`: add regression proving push does not enter hero-flight host when destination hero subtree is disabled.
  - docs/changelog: record shipped behavior and focused test coverage.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~HeroNavigatorTests|FullyQualifiedName~MaterialFloatingActionButtonTests"`
- New tests added:
  - `Navigator_Push_WithDisabledDestinationHeroMode_DoesNotStartHeroFlight` (`HeroNavigatorTests`)
- Parity-risk scenarios covered:
  - Matching tags exist on source/destination routes but destination route disables hero participation via `HeroMode(enabled: false)`.
  - Navigator push remains single-route (no hero dual-route transition host and no hero placeholder offstage subtree).

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
