# Feature: widgets-2026-03-10-stack-positioned-baseline

## Goal

- Add M3 overlay layout baseline via `Stack`/`Positioned` so common Dart multi-child overlay compositions can be ported directly.

## Non-Goals

- No advanced stack clipping controls in this iteration.
- No `IndexedStack` or animated positioned variants yet.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter/Rendering/Object.cs`
  - `src/Flutter/Rendering/Flex.RenderFlex.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Expand only if container parent-data wiring or sample parity route wiring requires additional context.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework behavior remains in `src/Flutter` (`Widget -> Element -> RenderObject`).
  - Sample route/module parity remains synchronized between C# and Dart samples.

## Planned Changes

- Files to edit:
  - `src/Flutter/Rendering/Stack.RenderStack.cs`
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Tests/StackTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/StackDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/general/stack_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `Stack.RenderStack.cs`: add render-layer stack algorithm and positioned parent data.
  - `Basic.cs`: expose `Stack` and `Positioned` widgets.
  - `StackTests.cs`: verify layout offsets/insets and parent-data update flow.
  - sample files: add parity demo route/page for runtime validation.
  - docs/changelog: register M3 progress and coverage.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (full)
- New tests to add:
  - `src/Flutter.Tests/StackTests.cs`

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
