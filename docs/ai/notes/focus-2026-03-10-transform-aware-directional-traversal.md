# Feature: focus-2026-03-10-transform-aware-directional-traversal

## Goal

- Make directional focus traversal geometry aware of render-object transform chains.
- Keep traversal candidate ranking deterministic while using transformed bounds.

## Non-Goals

- No new focus policy mode in this iteration.
- No host-level key mapping changes in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter.Tests/FocusTests.cs`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Expansion trigger:
  - Expand only if additional render-object transform types need traversal integration.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Focus traversal remains framework behavior (`src/Flutter/Widgets/Focus.cs`).
  - Directional ranking remains deterministic for covered geometry.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter.Tests/FocusTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `Focus.cs`: compute traversal rects via render-object offset + transform chain to root.
  - `FocusTests.cs`: add regression covering transformed directional traversal outcomes.
  - Tracking docs: mark transform-aware directional baseline delivered and narrow focus gaps.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (`FocusTests` filter)
- New tests to add:
  - `src/Flutter.Tests/FocusTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample route/module delta required for this focus traversal baseline.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
