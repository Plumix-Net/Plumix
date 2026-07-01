# Feature: testing-2026-04-12-navigation-material-order-stability

## Goal

- Remove order-dependent failures in full `Flutter.Tests` runs for navigator back-button and material app-bar/button interaction tests.

## Non-Goals

- Runtime behavior changes in production navigation/material widgets.
- Global test-infrastructure redesign across all test classes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/NavigationTests.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `src/Flutter.Tests/SchedulerTestCollection.cs`
- Expansion trigger:
  - Only expand if additional global dispatcher/state owners are required to eliminate reproducible order flakes.

## Delivery Scope

- Target area:
  - test isolation for navigator/material classes using global static state.
- Completion checklist:
  - [x] Deterministic full-suite execution
  - [x] No production behavior changes
  - [x] Focused and full test validation

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants touched:
  - Framework behavior stays in framework libraries; test-only reset hooks are internal and used only from tests.

## Planned Changes

- Files edited:
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/NavigationTests.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
- Intent:
  - expose internal `NavigatorBackButtonDispatcher.ResetForTests()`;
  - run navigation/material test classes in serial scheduler collection;
  - reset global static test state in test constructors.

## Test Plan

- Targeted checks:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~Navigator_TryHandleBackButton_PopsWhenPossible`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~AppBar_AutomaticallyImplyLeading_UsesCloseIcon_OnFullscreenDialogRoute`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~Scaffold_OpenDrawer_AnimatesScrimOpacity_ToFullValue`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~TextButton_HoverOverlayTakesPriorityOverFocusOverlay`
- Full suite:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore` -> `610 passed`.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] Feature note added

## Done Criteria

- [x] Reproducible full `Flutter.Tests` pass without order-dependent navigation/material flakes
- [x] Changes scoped to test isolation with no runtime behavior drift
