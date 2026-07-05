# Feature: widgets-2026-03-10-container-constraints-transform-baseline

## Goal

- Extend M3 `Container` parity with `constraints` and `transform`, including Flutter-like width/height tightening against explicit constraints.

## Non-Goals

- No `foregroundDecoration`, `clipBehavior`, or transform-alignment support in this iteration.
- No new render objects; composition stays on existing primitives.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Tests/ContainerTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/ContainerDemoPage.cs`
  - `dart_sample/lib/demos/general/container_demo_page.dart`
  - `docs/FRAMEWORK_PLAN.md`
- Expansion trigger:
  - Expand only for sample route metadata sync and tracking docs updates.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework behavior remains in `src/Flutter`.
  - Widget/render separation remains explicit (`Widget -> Element -> RenderObject`).
  - C# and Dart sample route parity is kept in lockstep.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Tests/ContainerTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/ContainerDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/general/container_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `Basic.cs`: add new `Container` composition properties and constraint-tightening behavior.
  - `ContainerTests.cs`: validate constrained-box propagation, tightening logic, and wrapper order.
  - sample files: expose interactive checks for clamp/tighten and transform wrappers in both runtimes.
  - docs/changelog: register M3 progression and updated coverage/parity notes.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (full)
  - `dotnet build src/Sample/Flutter.Net.Desktop/Flutter.Net.Desktop.csproj -c Debug`
  - `dotnet build src/Sample/Flutter.Net.Browser/Flutter.Net.Browser.csproj -c Debug`
  - `dart analyze` in `dart_sample`
- New tests to add:
  - additions in `src/Flutter.Tests/ContainerTests.cs`

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
