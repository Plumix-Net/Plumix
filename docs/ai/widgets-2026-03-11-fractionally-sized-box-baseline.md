# Feature: widgets-2026-03-11-fractionally-sized-box-baseline

## Goal

- Extend M3 widget parity with `FractionallySizedBox` and harden `Spacer` behavior validation with a proportional-flex regression test.

## Non-Goals

- No port of `FittedBox`, `IntrinsicWidth`, or `IntrinsicHeight` in this iteration.
- No host pipeline changes outside sample route wiring.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Tests/AspectRatioTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/AspectRatioDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
- Expansion trigger:
  - Expand only for parity route wiring and tracking docs synchronization.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework behavior remains in `src/Flutter`.
  - Widget/render layering remains explicit (`Widget -> Element -> RenderObject`).
  - C# and Dart sample route parity is kept in lockstep.

## Planned Changes

- Files to edit:
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Tests/AspectRatioTests.cs`
  - `src/Flutter.Tests/FractionallySizedBoxTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/AspectRatioDemoPage.cs`
  - `src/Sample/Flutter.Net/Demos/General/FractionallySizedBoxDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/general/aspect_ratio_demo_page.dart`
  - `dart_sample/lib/demos/general/fractionally_sized_box_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - framework files: add `RenderFractionallySizedBox` and `FractionallySizedBox` widget API.
  - tests: add render/widget tests for fractional sizing and a two-spacer flex-distribution regression.
  - sample files: make `Spacer` flex changes visually observable and add a new parity demo route/page for fractional sizing controls.
  - docs/changelog: register M3 progression and updated parity/coverage records.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter "AspectRatioTests|FractionallySizedBoxTests"`
  - `dotnet build src/Sample/Flutter.Net.Desktop/Flutter.Net.Desktop.csproj -c Debug`
  - `dart analyze` in `dart_sample`
- New tests to add:
  - `src/Flutter.Tests/FractionallySizedBoxTests.cs`
  - additions in `src/Flutter.Tests/AspectRatioTests.cs`

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
