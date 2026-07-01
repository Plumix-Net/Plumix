# Feature: widgets-2026-03-11-text-typography-parity-hardening

## Goal

- Continue post-M3 control parity by reducing visible C#/Dart text-style mismatches through framework-level typography controls on `Text` and matching render/layout behavior in `RenderParagraph`.

## Non-Goals

- No full pixel-perfect typography parity audit for every host/font backend in this iteration.
- No broad redesign of sample page content or route/module structure.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/Widgets/Text.cs`
  - `src/Flutter/RenderParagraph.cs`
  - `src/Flutter/UI/TextLayoutFallback.cs`
  - `src/Flutter/RenderButton.cs`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextWidgetTests.cs`
- Expansion trigger:
  - Expand only if additional text regression gaps are uncovered outside the paragraph/widget text scope.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework behavior remains in `src/Flutter` render/widget layers.
  - Sample parity constraints remain respected when text behavior changes.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/Text.cs`
  - `src/Flutter/RenderParagraph.cs`
  - `src/Flutter/UI/TextLayoutFallback.cs`
  - `src/Flutter/RenderButton.cs`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextWidgetTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `Text`/`RenderParagraph`: expose and wire `fontWeight`, `fontStyle`, `height`, `letterSpacing`.
  - `RenderParagraph`/fallback: ensure layout + host-less sizing path both honor new typography options.
  - `RenderButton`/`TextInput`: align default typeface selection to host default font family.
  - Tests/docs/changelog: capture behavior and tracking updates.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj`
- New tests to add:
  - No new file; extend `src/Flutter.Tests/TextWidgetTests.cs`.

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
