# Feature: material-2026-04-13-hero-placeholder-builder

## Goal

- Close the next Hero parity slice by supporting `Hero.placeholderBuilder` during navigator hero flights with size-aware placeholder metadata.

## Non-Goals

- Full Flutter placeholder lifecycle parity (`keepPlaceholder` behavior during diverted/cancelled flights).
- Nested navigator hero-controller orchestration.
- Gesture-diverted hero flight behavior.

## Context Budget Plan

- Budget: max 12 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Update docs/changelog after API + regression tests pass.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Hero placeholder composition (`Hero.placeholderBuilder`)
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
  - Hero behavior remains framework-owned (`src/Flutter`) with no host-level workaround.
  - Navigator push/pop flight choreography stays intact while hero visibility is substituted with route-local placeholders.

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
  - Placeholder builder callback shape is aligned to Flutter essentials (`context`, `size`, `child`), but advanced flight-diversion placeholder retention semantics remain out of current scope.

## Planned Changes

- Files edited:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Hero.cs`: add placeholder-builder API and route/tag placeholder state resolution in the transition controller.
  - `HeroNavigatorTests.cs`: verify source-placeholder behavior on push and destination-placeholder behavior on pop, including size metadata.
  - docs/changelog: record shipped parity and narrow residual hero gaps.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~HeroNavigatorTests|FullyQualifiedName~MaterialFloatingActionButtonTests"`
- New tests added:
  - `Navigator_Push_UsesSourceHeroPlaceholderBuilder_DuringFlight` (`HeroNavigatorTests`)
  - `Navigator_Pop_UsesDestinationHeroPlaceholderBuilder_DuringFlight` (`HeroNavigatorTests`)
- Parity-risk scenarios covered:
  - Push flight applies source-route placeholder builder while hero is hidden.
  - Pop flight applies destination-route placeholder builder while hero is hidden.

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
