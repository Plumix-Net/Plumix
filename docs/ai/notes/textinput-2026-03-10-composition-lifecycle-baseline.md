# Feature: textinput-2026-03-10-composition-lifecycle-baseline

## Goal

- Add framework-level IME composition lifecycle handling for editable text.
- Route composition update/commit through focus and apply composing state in `TextEditingController` + `EditableText`.

## Non-Goals

- No host-native IME preedit integration in this iteration (platform bridge remains pending).
- No caret geometry, multiline editing, or clipboard shortcut expansion in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Expansion trigger:
  - Expand only if host preedit APIs are required for this iteration.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Input editing behavior stays framework-owned in `src/Flutter`.
  - Focus remains the routing gate for keyboard/text/composition events.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `src/Flutter/Widgets/Focus.cs`: add composition callback surface and manager dispatch entry points.
  - `src/Flutter/Widgets/TextInput.cs`: apply/update/commit/clear composing state in editable flow.
  - `src/Flutter.Tests/TextInputTests.cs`: cover manager dispatch and editable/controller composition lifecycle.
  - Tracking docs: mark composition baseline progress and keep remaining gaps explicit.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (`TextInputTests` focus run first)
- New tests to add:
  - `src/Flutter.Tests/TextInputTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample route/module delta for this increment.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
