# Feature: material-2026-04-13-hero-duplicate-tag-guard

## Goal

- Add Flutter-like runtime protection against duplicate `Hero(tag)` registrations in one route subtree.

## Non-Goals

- Cross-route duplicate tag restrictions.
- Nested navigator hero-controller orchestration.
- Hero diversion lifecycle behavior.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Update docs/changelog after duplicate-tag guard and test are validated.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Hero registration guard for duplicate tags in one route subtree
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
  - Hero registry state remains framework-owned and deterministic.
  - Invalid composition patterns fail fast instead of silently overwriting registrations.

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
  - Uses `InvalidOperationException` for duplicate-tag failures in C# framework runtime.

## Planned Changes

- Files edited:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Hero.cs`: throw on conflicting active registration for identical `tag` in one route subtree.
  - `HeroNavigatorTests.cs`: add regression test that duplicate tags throw.
  - docs/changelog: record delivered parity hardening.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~HeroNavigatorTests|FullyQualifiedName~MaterialFloatingActionButtonTests"`
- New tests added:
  - `Navigator_InitialRoute_WithDuplicateHeroTagsInRouteSubtree_ThrowsInvalidOperationException` (`HeroNavigatorTests`)
- Parity-risk scenarios covered:
  - Duplicate `Hero(tag)` inside one route subtree fails fast with explicit runtime error.

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
