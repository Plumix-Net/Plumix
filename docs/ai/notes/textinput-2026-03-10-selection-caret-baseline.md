# Feature: textinput-2026-03-10-selection-caret-baseline

## Goal

- Extend editable text baseline with controller-level selection/caret primitives.
- Ensure keyboard editing behavior is selection-aware (replace selection, directional caret movement, delete backward/forward, select-all).

## Non-Goals

- No composition lifecycle and IME preedit state integration in this iteration.
- No visual caret geometry or multiline layout/cursor movement logic in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Expand only if selection behavior requires render-level geometry APIs outside editable/focus layers.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Input and editing behavior remains framework-owned in `src/Flutter`.
  - Focus remains the routing gate for keyboard/text editing events.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `src/Flutter/Widgets/TextInput.cs`: add `TextSelection`/`TextRange` and selection-aware editing operations.
  - `src/Flutter.Tests/TextInputTests.cs`: add regression coverage for selection replacement, caret movement, select-all, and delete semantics.
  - Tracking docs: reflect shipped M2 progress and remaining editing gaps.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj`
- New tests to add:
  - `src/Flutter.Tests/TextInputTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample route/module delta for this increment; existing editable demo remains valid.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
