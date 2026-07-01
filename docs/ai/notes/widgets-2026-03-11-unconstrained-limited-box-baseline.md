# Feature: widgets-2026-03-11-unconstrained-limited-box-baseline

## Goal

- Extend M3 widget parity with `UnconstrainedBox` and `LimitedBox`, including render/widget behavior parity and a mirrored C#/Dart sample demo route.

## Non-Goals

- No `OverflowBox`, `SizedOverflowBox`, or intrinsic-size widget ports in this iteration.
- No host pipeline changes outside sample route wiring.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Tests/FittedBoxTests.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
- Expansion trigger:
  - Expand only for sample parity route wiring and docs synchronization.

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
  - `src/Flutter.Tests/UnconstrainedLimitedBoxTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/UnconstrainedLimitedBoxDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/general/unconstrained_limited_box_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - framework files: add `RenderUnconstrainedBox`/`RenderLimitedBox` and widget wrappers.
  - tests: verify unconstrained-axis behavior, `LimitedBox` bounded-vs-unbounded semantics, and widget update wiring.
  - sample files: add parity demo route/page for interactive axis/max-limit checks.
  - docs/changelog: register M3 progression and updated parity/coverage records.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter UnconstrainedLimitedBoxTests`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug`
  - `dotnet build src/Sample/Flutter.Net.Desktop/Flutter.Net.Desktop.csproj -c Debug`
  - `dotnet build src/Sample/Flutter.Net.Browser/Flutter.Net.Browser.csproj -c Debug`
  - `dart analyze` in `dart_sample`
- New tests to add:
  - `src/Flutter.Tests/UnconstrainedLimitedBoxTests.cs`

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
