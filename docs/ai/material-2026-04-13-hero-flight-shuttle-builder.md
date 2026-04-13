# Feature: material-2026-04-13-hero-flight-shuttle-builder

## Goal

- Close the next Hero parity slice by supporting `Hero.flightShuttleBuilder` during navigator hero flights with Flutter-like precedence (`destination -> source -> destination child`).

## Non-Goals

- `Hero.placeholderBuilder` parity.
- Nested navigator hero-controller orchestration.
- Gesture-diverted hero-flight behavior.

## Context Budget Plan

- Budget: max 12 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Update tracking docs/changelog only after runtime API + test behavior are validated.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Hero flight shuttle composition (`Hero.flightShuttleBuilder`)
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
  - Hero transition behavior remains in framework libraries (`src/Flutter*`) rather than host adapters.
  - Navigator push/pop lifecycle ordering remains unchanged while overlay flight content becomes customizable.

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
  - `flightShuttleBuilder` callback shape is simplified for current framework scope (progress + push/pop flag instead of full animation object/context surface); precedence and fallback semantics match Flutter behavior.

## Planned Changes

- Files edited:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Hero.cs`: add `Hero.flightShuttleBuilder` API, resolve builder from hero state, and carry builder/state refs in flight manifest.
  - `Navigation.cs`: resolve in-flight shuttle widget from manifest each frame with push/pop direction.
  - `HeroNavigatorTests.cs`: verify destination builder precedence and source fallback behavior.
  - tracking docs/changelog: record delivered parity and updated residual gap wording.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~HeroNavigatorTests|FullyQualifiedName~MaterialFloatingActionButtonTests"`
- New tests added:
  - `Navigator_Push_UsesDestinationHeroFlightShuttleBuilder_WhenBothHeroesProvideBuilder` (`HeroNavigatorTests`)
  - `Navigator_Push_UsesSourceHeroFlightShuttleBuilder_AsFallbackWhenDestinationBuilderIsMissing` (`HeroNavigatorTests`)
- Parity-risk scenarios covered:
  - Destination hero shuttle builder takes precedence when both routes provide one.
  - Source shuttle builder is used when destination builder is absent.

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
