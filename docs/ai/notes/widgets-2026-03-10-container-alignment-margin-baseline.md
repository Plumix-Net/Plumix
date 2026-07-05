# Feature: widgets-2026-03-10-container-alignment-margin-baseline

## Goal

- Extend M3 `Container` parity with baseline `alignment` and `margin` composition behavior to simplify direct Dart-to-C# ports.

## Non-Goals

- No `constraints`, `transform`, or `foregroundDecoration` support in this iteration.
- No directional/inset-aware margin variants beyond existing `Thickness`.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Tests/ContainerTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/ContainerDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/FRAMEWORK_PLAN.md`
- Expansion trigger:
  - Expand only when sample parity wiring and tracking docs need updates.

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
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `Basic.cs`: add `Container.alignment` and `Container.margin` composition wrappers.
  - `ContainerTests.cs`: validate wrapper render-object wiring and composition order.
  - sample files: add parity route/page to validate behavior interactively.
  - docs/changelog: capture M3 progression and coverage/parity updates.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (full)
  - `dart analyze` for `dart_sample`
- New tests to add:
  - `src/Flutter.Tests/ContainerTests.cs`

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
