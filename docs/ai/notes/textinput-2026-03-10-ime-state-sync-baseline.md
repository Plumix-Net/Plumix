# Feature: textinput-2026-03-10-ime-state-sync-baseline

## Goal

- Expose focused editable state to host IME (`SurroundingText`, selection, cursor rectangle).
- Support host-driven selection updates flowing back into framework `TextEditingController`.

## Non-Goals

- No precise glyph-level caret geometry in this iteration (approximate caret rect only).
- No multiline editing expansion in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Expand only if Avalonia IME API contracts require direct source confirmation.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Editing logic remains framework-owned (`src/Flutter`).
  - Host layer only adapts platform IME contracts to framework focus/editing primitives.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `Focus.cs`: add focused text-input state and selection-change callback surface.
  - `TextInput.cs`: provide editable state snapshot + apply host selection changes.
  - `FlutterHost.cs`: feed Avalonia IME client getters/setters from focus text-input state.
  - `TextInputTests.cs`: cover host IME surrounding-text/selection/cursor sync.
  - Tracking docs: record baseline completion and narrow remaining gaps.

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
  - No sample route/module change in this iteration.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
