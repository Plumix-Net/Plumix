# Feature: material-2026-04-13-hero-default-placeholder-parity

## Goal

- Align default hidden-hero placeholder behavior with Flutter push/pop semantics when `Hero.placeholderBuilder` is not provided.

## Non-Goals

- Full Flutter ticker/placeholder lifecycle parity for diverted flights.
- Nested navigator hero-controller orchestration.
- Gesture-driven hero diversion semantics.

## Context Budget Plan

- Budget: max 10 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Update docs/changelog only after push/pop regression coverage passes.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Hero default placeholder composition (without custom placeholder builder)
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
  - Hero transition behavior remains in framework layers (`src/Flutter`).
  - Navigator push/pop route-host choreography remains unchanged while hidden-hero placeholders become behavior-aware.

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
  - TickerMode-specific parity details remain out of current scope; offstage + fixed-size placeholder semantics are aligned for push/pop behavior.

## Planned Changes

- Files edited:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Hero.cs`: store placeholder include-child semantics and build default placeholders as fixed-size offstage/empty boxes by transition role.
  - `Navigation.cs`: pass push/pop transition direction into hero placeholder activation.
  - `HeroNavigatorTests.cs`: add focused push/pop regressions for default placeholder offstage behavior.
  - docs/changelog: record delivered parity slice and updated coverage.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~HeroNavigatorTests|FullyQualifiedName~MaterialFloatingActionButtonTests"`
- New tests added:
  - `Navigator_Push_DefaultHeroPlaceholder_UsesOffstageChildForSourceHero` (`HeroNavigatorTests`)
  - `Navigator_Pop_DefaultHeroPlaceholder_DoesNotUseOffstageChild` (`HeroNavigatorTests`)
- Parity-risk scenarios covered:
  - Push flight source hero default placeholder preserves hidden child subtree via offstage wrapper.
  - Pop flight default placeholders avoid offstage child inclusion.

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
