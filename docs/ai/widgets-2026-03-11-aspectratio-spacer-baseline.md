# Feature: widgets-2026-03-11-aspectratio-spacer-baseline

## Goal

- Extend M3 widget parity with `AspectRatio` and `Spacer`, including framework-owned layout behavior in render/widget layers and sample parity coverage.

## Non-Goals

- No `FittedBox`, `FractionallySizedBox`, or intrinsic-size widgets in this iteration.
- No host-platform changes outside existing sample route wiring.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter.Tests/AlignTests.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
- Expansion trigger:
  - Expand only for sample parity routing and tracking docs synchronization.

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
  - `src/Sample/Flutter.Net/Demos/General/AspectRatioDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/general/aspect_ratio_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - rendering/widgets: add `RenderAspectRatio`, `AspectRatio`, and `Spacer`.
  - tests: validate ratio layout behavior, widget update wiring, and `Spacer` flex parent-data contract.
  - sample files: add parity demo route/page for interactive ratio + spacer checks in both runtimes.
  - docs/changelog: register M3 progression and updated coverage/parity records.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter AspectRatioTests`
  - `dotnet build src/Sample/Flutter.Net.Desktop/Flutter.Net.Desktop.csproj -c Debug`
  - `dotnet build src/Flutter.Tests/Flutter.Tests.csproj -c Debug`
  - `dart analyze` in `dart_sample`
- New tests to add:
  - `src/Flutter.Tests/AspectRatioTests.cs`

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
