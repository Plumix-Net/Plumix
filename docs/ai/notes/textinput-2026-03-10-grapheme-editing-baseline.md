# Feature: textinput-2026-03-10-grapheme-editing-baseline

## Goal

- Make caret motion and delete operations grapheme-aware in `TextEditingController`.
- Prevent surrogate/ZWJ/combining-sequence corruption when editing by keyboard.

## Non-Goals

- No visual bidi caret policy changes in this iteration.
- No language-specific word-boundary heuristics beyond current baseline.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Expansion trigger:
  - Expand only if bidi visual traversal policy work is pulled into scope.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Editing mutation rules remain framework-owned.
  - Selection/caret behavior stays deterministic under keyboard input.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - Replace UTF-16 step-based caret/delete boundaries with grapheme text-element boundaries.
  - Add regression coverage for emoji ZWJ and combining-mark sequences.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (`TextInputTests` filter)
- New tests to add:
  - `src/Flutter.Tests/TextInputTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample route/module delta required for this controller-level baseline.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
