# Feature: material-2026-03-12-theme-baseline

## Goal

- Start M4 by shipping framework Material theming primitives in a dedicated `Flutter.Material` project (`ThemeData`, `MaterialTextTheme`, `Theme`) and wire baseline text style propagation into existing framework `Text`.

## Non-Goals

- No `Scaffold`/`AppBar` in this iteration.
- No Material button family (`TextButton`/`ElevatedButton`/`OutlinedButton`) in this iteration.
- No host-toolchain fixes for Android/iOS M5 blockers.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/Widgets/DefaultTextStyle.cs`
  - `src/Flutter/Widgets/Text.cs`
  - `src/Sample/Flutter.Net/CounterApp.cs`
  - `src/Flutter.Tests/TextWidgetTests.cs`
  - `src/Sample/Flutter.Net/Flutter.Net.csproj`
  - `src/Flutter.Tests/Flutter.Tests.csproj`
- Expansion trigger:
  - Expand only if solution/project wiring or inherited widget access required additional context.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework behavior remains in framework libraries under `src/` (`src/Flutter`, `src/Flutter.Material`), not in host controls.
  - Inherited dependency contracts remain preserved (`Theme` implemented as `InheritedWidget`).
  - Sample parity between C# and Dart app bootstrap remains aligned.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Flutter.Material.csproj`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Theme.cs`
  - `src/Flutter.Net.sln`
  - `src/Sample/Flutter.Net/Flutter.Net.csproj`
  - `src/Flutter.Tests/Flutter.Tests.csproj`
  - `src/Sample/Flutter.Net/CounterApp.cs`
  - `src/Flutter.Tests/TextWidgetTests.cs`
  - `dart_sample/lib/counter_app.dart`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/INVARIANTS.md`
- Brief intent per file:
  - `src/Flutter.Material/*`: introduce Material theming primitives and API surface.
  - `src/Flutter.Net.sln` + `*.csproj`: wire new project into solution/test/sample graph.
  - `CounterApp.cs`: switch bootstrap from manual `DefaultTextStyle` to `Theme`.
  - `TextWidgetTests.cs`: verify Material theme text propagation.
  - `dart_sample/lib/counter_app.dart`: keep bootstrap text-theme baseline aligned.
  - docs/changelog: reflect M4 progress and coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter TextWidgetTests`
- New tests to add:
  - Extend `src/Flutter.Tests/TextWidgetTests.cs` with Material-theme inheritance coverage.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
