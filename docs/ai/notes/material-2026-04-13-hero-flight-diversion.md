# Feature: material-2026-04-13-hero-flight-diversion

## Goal

- Close Hero lifecycle parity for interrupted transitions: when pop interrupts an active push hero flight between the same routes, reuse and reverse the active flight instead of creating a new pop flight.

## Non-Goals

- Nested hero-controller orchestration across nested navigators.
- Hero animation curve customization beyond current tween/controller behavior.
- Additional Hero API surface expansion.

## Context Budget Plan

- Budget: max 10 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Add/update docs + feature note after behavior + test pass.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Hero transition lifecycle diversion (`push -> interrupted by pop`)
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
  - Active hero flight state must remain coherent when route direction flips mid-transition.
  - Hidden-hero placeholder ownership must switch from push semantics to pop semantics without creating duplicate flights.

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
  - Uses existing framework navigator/session abstractions (`HeroTransitionSession`) and controller reversal instead of Flutter internals; observable behavior is aligned for this scope.

## Planned Changes

- Files edited:
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Navigation.cs`: implement diversion detection and reverse active flight when pop interrupts push; avoid eager cancel before pop.
  - `Hero.cs`: expose placeholder refresh for already-active flights when transition direction changes.
  - `HeroNavigatorTests.cs`: add regression proving diverted flight reuses active tween and avoids new pop tween creation.
  - docs/changelog: reflect closed parity slice and updated residual gaps.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~HeroNavigatorTests|FullyQualifiedName~MaterialFloatingActionButtonTests"`
- New tests added:
  - `Navigator_PushFlight_InterruptedByPop_DivertsActiveHeroFlight` (`HeroNavigatorTests`)
- Parity-risk scenarios covered:
  - Push flight starts, then pop is triggered before completion.
  - No secondary pop `createRectTween` call is created.
  - Active tween keeps evaluating while controller reverses.

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
