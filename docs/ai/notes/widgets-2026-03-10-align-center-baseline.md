# Feature: widgets-2026-03-10-align-center-baseline

## Goal

- Add M3 single-child alignment baseline (`Align`/`Center`) so common Dart layout patterns can be ported to widget-level C# APIs.

## Non-Goals

- No multi-child positioned layout (`Stack`/`Positioned`) in this iteration.
- No directional alignment variants (`AlignmentDirectional`) yet.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter/Rendering/Box.cs`
  - `src/Flutter.Tests/BasicWidgetProxyTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
- Expansion trigger:
  - Expand only if alignment math or sample parity wiring needs additional host/sample files.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Architecture boundary remains `Widget -> Element -> RenderObject`.
  - Framework behavior stays in `src/Flutter` (samples only consume public widget APIs).

## Planned Changes

- Files to edit:
  - `src/Flutter/Rendering/Alignment.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Tests/AlignTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/AlignDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/general/align_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `Alignment.cs`: add Flutter-like alignment primitive with canonical constants and offset mapping helper.
  - `Proxy.RenderBox.cs`: add `RenderAlign` with alignment + width/height factor behavior.
  - `Basic.cs`: expose `Align` and `Center` widgets.
  - `AlignTests.cs`: cover render-layout behavior + widget update wiring.
  - sample files: add parity route/demo for interactive validation.
  - docs/changelog: register M3 progress and coverage.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (full)
- New tests to add:
  - `src/Flutter.Tests/AlignTests.cs`

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
