# Feature: widgets-2026-03-11-paragraph-alignment-intrinsic-width-fix

## Goal

- Fix visible C#/Dart mismatch where centered intrinsic-width labels in the `Counter` page (`Keyed List`) were painted with a right-shift in C#.
- Remove remaining mismatch from Flutter overflow debug zones by preventing bottom overflow on shorter viewports in both samples.

## Non-Goals

- No full typography/theming rewrite.
- No framework-wide redesign of page scrolling composition beyond `CounterScreen`.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/RenderParagraph.cs`
  - `src/Flutter/Widgets/Text.cs`
  - `src/Sample/Flutter.Net/CounterScreen.cs`
  - `src/Sample/Flutter.Net/CounterWidgets.cs`
  - `dart_sample/lib/counter_screen.dart`
  - `dart_sample/lib/counter_widgets.dart`
- Expansion trigger:
  - Expand only if issue is not isolated to paragraph alignment under loose width constraints.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Rendering/layout behavior remains in framework render layer (`src/Flutter`).
  - Sample parity maintained without diverging route/page structure.

## Planned Changes

- Files to edit:
  - `src/Flutter/RenderParagraph.cs`
  - `src/Sample/Flutter.Net/CounterScreen.cs`
  - `dart_sample/lib/counter_screen.dart`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
- Brief intent per file:
  - `RenderParagraph`: normalize aligned loose-width layout width when host text layout applies internal positive glyph offset.
  - Counter sample screens: wrap main content with `SingleChildScrollView` to avoid bottom overflow debug zones at smaller heights.
  - docs/changelog: capture parity rationale and runtime verification target.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter TextWidgetTests`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug`
- New tests to add:
  - None (headless test environment frequently uses font-manager fallback, making glyph-offset assertions unstable).

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [ ] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
