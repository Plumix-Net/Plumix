# Feature: textinput-2026-03-10-word-shortcuts-baseline

## Goal

- Add baseline word-level caret navigation and deletion shortcuts for `EditableText`.
- Keep shortcut handling in framework text-editing layer (controller + editable key pipeline).

## Non-Goals

- No paragraph-level shortcuts in this iteration.
- No clipboard/action-menu parity work in this iteration.

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
  - Expand only if host key-modifier normalization details require touching host-level key mapping code.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Text-editing behavior remains framework-owned in `src/Flutter`.
  - Host integration remains an event bridge without owning edit semantics.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `TextInput.cs`: add controller word-boundary navigation/deletion methods and route `Ctrl/Alt` shortcuts in editable key handling.
  - `TextInputTests.cs`: add regression coverage for controller word APIs and focused key event behavior.
  - Tracking docs: reflect delivered shortcut coverage and narrow remaining gaps.

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
  - No sample route/module delta required for this core shortcut baseline.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
