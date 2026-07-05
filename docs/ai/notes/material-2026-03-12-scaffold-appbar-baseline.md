# Feature: material-2026-03-12-scaffold-appbar-baseline

## Goal

- Deliver M4 shell baseline by adding framework `Scaffold` and `AppBar` primitives in `Flutter.Material` and integrating them into sample gallery page shells with C#/Dart parity.

## Non-Goals

- No Material button family (`TextButton`/`ElevatedButton`/`OutlinedButton`) in this iteration.
- No advanced app-bar behavior (sliver app bars, scroll under effects, dynamic elevation, etc.).
- No host-toolchain remediation for Android/iOS build blockers.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Tests/TextWidgetTests.cs`
  - `src/Flutter.Tests/ContainerTests.cs`
- Expansion trigger:
  - Expand only if slot composition or render-tree assertions required additional container/flex internals.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework behavior stays inside framework libraries (`src/Flutter`, `src/Flutter.Material`), not in host controls.
  - Material shell is implemented as widget composition (`Widget -> Element -> RenderObject`) over existing framework primitives.
  - Sample parity remains synchronized between C# and Dart gallery structure.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Scaffold.cs`: add baseline `Scaffold`/`AppBar` Material shell widgets.
  - `ThemeData.cs`: extend theme defaults for app-bar foreground usage (`onPrimaryColor`).
  - `SampleGalleryScreen.cs` + `sample_gallery_screen.dart`: consume shell widgets in menu/demo page wrappers.
  - `MaterialScaffoldTests.cs`: verify scaffold/app-bar theme resolution behavior.
  - docs/changelog: reflect shipped M4 progress and test mapping.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter "TextWidgetTests|MaterialScaffoldTests"`
  - `dotnet build src/Sample/Flutter.Net/Flutter.Net.csproj -c Debug`
  - `dotnet build src/Sample/Flutter.Net.Desktop/Flutter.Net.Desktop.csproj -c Debug`
- New tests to add:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`

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
