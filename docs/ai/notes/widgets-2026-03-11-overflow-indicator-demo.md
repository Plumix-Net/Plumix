# Feature: widgets-2026-03-11-overflow-indicator-demo

## Goal

- Add Flutter-style overflow debug indicator rendering to framework `RenderFlex` and provide a dedicated demo route/page in both C# and Dart samples.

## Non-Goals

- No full `DebugOverflowIndicatorMixin` parity for every render object type in this iteration.
- No release-mode toggle surface for overflow indicator visibility yet.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/Rendering/Flex.RenderFlex.cs`
  - `src/Flutter.Tests/RenderingParityTests.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `src/Sample/Flutter.Net/Demos/General/OverflowIndicatorDemoPage.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/demos/general/overflow_indicator_demo_page.dart`
- Expansion trigger:
  - Expand only if indicator rendering requires new paint-context primitives beyond existing rectangle/text/clip/transform APIs.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework rendering behavior remains implemented in `src/Flutter/Rendering`.
  - Sample parity maintained by updating both C# and Dart routes/pages in the same change.

## Planned Changes

- Files to edit:
  - `src/Flutter/Rendering/Flex.RenderFlex.cs`
  - `src/Flutter.Tests/RenderingParityTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/OverflowIndicatorDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/general/overflow_indicator_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `RenderFlex`: paint overflow stripes + edge label when `_hasOverflow`.
  - `RenderingParityTests`: assert overflow scenario adds indicator paint commands.
  - sample files: add dedicated overflow-indicator route/page in both codebases.
  - docs/changelog: capture parity progress and validation scope.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter RenderingParityTests`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug`
  - `dotnet build src/Sample/Flutter.Net.Desktop/Flutter.Net.Desktop.csproj -c Debug`
  - `flutter analyze dart_sample`
- New tests to add:
  - Extend `src/Flutter.Tests/RenderingParityTests.cs` with overflow-indicator paint regression.

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
