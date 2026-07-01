# Feature: textinput-2026-03-10-multiline-caret-geometry-baseline

## Goal

- Add multiline editing baseline to `EditableText`.
- Provide glyph-aware caret geometry for IME cursor rectangle via `TextLayout` when available.

## Non-Goals

- No full word/paragraph editing shortcut set in this iteration.
- No selection-handle UX or clipboard parity beyond existing shortcuts in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Expand only if Avalonia text layout/caret API details are required.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Editing behavior remains in framework layers (`src/Flutter`).
  - Host remains a bridge that consumes focused text-input state without owning editing rules.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `TextInput.cs`: add multiline key workflow and vertical caret movement; use `TextLayout` for caret rectangle with fallback.
  - `TextInputTests.cs`: add regression test for multiline newline insertion and vertical navigation.
  - Tracking docs: mark multiline/caret baseline as delivered and narrow remaining editing gaps.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (`TextInputTests` focused run first)
- New tests to add:
  - `src/Flutter.Tests/TextInputTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - Follow-up parity refresh applied in the same milestone: both C# and Dart `EditableText` demo pages now include multiline Notes flow (`Enter` newline, ArrowUp/ArrowDown caret hints), seeded multiline content action, and escaped value summary output.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
