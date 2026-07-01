# Feature: widgets-2026-03-11-text-rendering-parity-hardening

## Goal

- Reduce visible C#/Dart sample mismatch in text rendering by aligning baseline widget/render behavior for horizontal alignment and unconstrained paragraph layout.

## Non-Goals

- No full typography parity pass (font family fallback matrices, advanced shaping parity, locale-specific typography behavior).
- No host toolchain stabilization work for Android/iOS builds in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/Widgets/Text.cs`
  - `src/Flutter/RenderParagraph.cs`
  - `src/Sample/Flutter.Net/CounterWidgets.cs`
  - `src/Sample/Flutter.Net/Demos/General/UnconstrainedLimitedBoxDemoPage.cs`
  - `dart_sample/lib/counter_widgets.dart`
  - `dart_sample/lib/demos/general/unconstrained_limited_box_demo_page.dart`
- Expansion trigger:
  - Expand only if additional text-layout regression coverage is needed outside widget/render text scope.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework behavior remains in `src/Flutter` (`Text`/`RenderParagraph`), not in host controls.
  - C#/Dart sample parity remains synchronized when sample-facing text behavior is adjusted.

## Planned Changes

- Files to edit:
  - `src/Flutter/UI/Text.cs`
  - `src/Flutter/RenderParagraph.cs`
  - `src/Flutter/Widgets/Text.cs`
  - `src/Flutter.Tests/TextWidgetTests.cs`
  - `src/Sample/Flutter.Net/CounterWidgets.cs`
  - `src/Sample/Flutter.Net/Demos/General/UnconstrainedLimitedBoxDemoPage.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `src/Flutter/UI/Text.cs`: add Flutter-like text enums used by widget/render API.
  - `src/Flutter/RenderParagraph.cs`: add text layout options and remove synthetic unbounded width cap.
  - `src/Flutter/Widgets/Text.cs`: expose text layout options and wire to render object.
  - `src/Flutter.Tests/TextWidgetTests.cs`: cover widget wiring updates and unbounded paragraph layout behavior.
  - `src/Sample/Flutter.Net/*`: apply `textAlign` parity where Dart sample already centers text.
  - Tracking docs/changelog: record M3 closure + text parity hardening.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj`
- New tests to add:
  - `src/Flutter.Tests/TextWidgetTests.cs`

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
