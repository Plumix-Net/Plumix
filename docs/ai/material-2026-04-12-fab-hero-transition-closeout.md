# Feature: material-2026-04-12-fab-hero-transition-closeout

## Goal

- Close the remaining framework-scope `FloatingActionButton` parity gap for runtime hero transitions on navigator push/pop when matching `heroTag` values are present.

## Non-Goals

- Full Flutter `Hero` API parity (`createRectTween`, `flightShuttleBuilder`, `placeholderBuilder`, nested-hero-controller orchestration).
- Hero behavior for non-navigator transition primitives outside current framework scope.

## Context Budget Plan

- Budget: max 14 files in initial read.
- Entry files:
  - `src/Flutter.Material/FloatingActionButton.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Add a dedicated hero primitive in `src/Flutter/Widgets` and focused navigator render-harness tests when runtime flights require route-level overlay choreography.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Material `FloatingActionButton` hero transition behavior through framework navigator integration.
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
  - Framework behavior stays in framework libraries (`src/Flutter*`) with no host-specific widget logic.
  - Navigator lifecycle contracts remain explicit (`DidPush`/`DidPop`/observer notifications) while hero flight adds temporary route composition only during animation.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/floating_action_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/heroes.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/navigator.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log:
  - Advanced Hero customization surface (`createRectTween`, shuttle/placeholder builders, nested hero-controller overrides) remains follow-up beyond current framework scope.

## Planned Changes

- Files edited:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Material/FloatingActionButton.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `Hero.cs`: add baseline `Hero` primitive, route/tag registration, bounds snapshotting, flight manifests, and hero visibility suppression during active flights.
  - `Navigation.cs`: add shared-tag hero-flight choreography for push/pop transitions with overlay animation and deferred pop-route disposal.
  - `FloatingActionButton.cs`: wrap built FAB output with `Hero` when `heroTag` is provided.
  - `HeroNavigatorTests.cs`: add render-harness push/pop hero-flight regressions.
  - `MaterialFloatingActionButtonTests.cs`: assert FAB output is hero-wrapped for non-null `heroTag`.
  - tracking docs/changelog: record closure of FAB hero runtime gap and new coverage.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~HeroNavigatorTests|FullyQualifiedName~MaterialFloatingActionButtonTests"`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~NavigationTests|FullyQualifiedName~MaterialScaffoldTests"`
- New tests added:
  - `Navigator_Push_WithSharedHeroTag_ShowsBothRoutesDuringFlight_ThenSettlesToDestination` (`HeroNavigatorTests`)
  - `Navigator_Pop_WithSharedHeroTag_KeepsPoppedRouteDuringFlight_ThenDisposesIt` (`HeroNavigatorTests`)
  - `FloatingActionButton_HeroTag_WrapsBuiltResultWithHero` (`MaterialFloatingActionButtonTests`)
- Parity-risk scenarios covered:
  - Shared-tag hero flight visibility and lifecycle on push/pop transitions.
  - FAB build composition correctly participates in hero transitions when `heroTag` is set.

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
- [x] Remaining parity gaps documented with blocker + next action
