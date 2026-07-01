# Feature: textinput-2026-03-10-paragraph-shortcuts-baseline

## Goal

- Add paragraph-level caret shortcuts for multiline `EditableText`.
- Keep behavior in framework text-editing pipeline (controller + editable key handling).

## Non-Goals

- No clipboard/action menu parity in this iteration.
- No paragraph-level delete shortcuts in this iteration.

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
  - Expand only if host key modifier normalization requires host integration changes.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Editing behavior remains framework-owned in `src/Flutter`.
  - Host key events remain bridge-only and do not own editing semantics.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `TextInput.cs`: add paragraph boundary movement APIs and wire `Ctrl/Alt + ArrowUp/ArrowDown` in multiline key handling.
  - `TextInputTests.cs`: add controller-level and focused key-path regression coverage for paragraph shortcuts.
  - Tracking docs: mark paragraph shortcuts delivered and narrow remaining text-editing gaps.

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
  - No sample route/module delta required for this core text-editing shortcut baseline.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
