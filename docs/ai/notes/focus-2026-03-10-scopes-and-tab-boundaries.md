# Feature: focus-2026-03-10-scopes-and-traversal-boundaries

## Goal

- Add first-class focus scope support so keyboard traversal stays local to the active focus scope.
- Add directional key traversal (`Left/Right/Up/Down`) with geometry-aware candidate selection when bounds are available.
- Extend M2 keyboard/focus baseline with test-covered scope-aware behavior.

## Non-Goals

- No transform-aware directional traversal policy implementation in this iteration.
- No editable text/IME focus handoff in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter.Tests/FocusTests.cs`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Expand only if focus scope wiring requires lifecycle host changes outside widget/focus modules.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework input/focus behavior remains in `src/Flutter`.
  - Input handling remains deterministic and regression-testable through framework-level abstractions.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter.Tests/FocusTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `src/Flutter/Widgets/Focus.cs`: add `FocusScopeNode` and `FocusScope`, track node-scope membership, and scope-aware traversal (Tab + directional keys) with geometry-aware directional selection + sequential fallback.
  - `src/Flutter.Tests/FocusTests.cs`: add regression coverage for manager-level and widget-level scope traversal boundaries, plus directional key traversal behavior and geometry-based candidate selection.
  - `docs/FRAMEWORK_PLAN.md`: record M2 progress and remaining focus/accessibility gaps.
  - `docs/ai/TEST_MATRIX.md`: refresh focus row with scope traversal coverage.
  - `CHANGELOG.md`: record shipped scope-focused M2 increment.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj`
- New tests to add:
  - `src/Flutter.Tests/FocusTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample route/module change in this feature, so parity matrix delta is not required.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
