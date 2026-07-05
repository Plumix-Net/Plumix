# Feature: rendering-2026-03-10-tighten-clamp

## Goal

- Bring `BoxConstraints.Tighten` behavior in line with Flutter by clamping requested width/height into the current constraint range.
- Lock this behavior with a regression test in render parity coverage.

## Non-Goals

- No widget API changes.
- No sample route/module updates.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Tests/RenderingParityTests.cs`
  - `src/Flutter/Rendering/Box.cs`
- Expansion trigger:
  - Open additional files only if `Tighten` fix causes broader rendering regressions.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Layout constraints must remain normalized and predictable across render object APIs.
  - Core framework behavior remains in `src/Flutter`.

## Planned Changes

- Files to edit:
  - `src/Flutter/Rendering/Box.cs`
  - `src/Flutter.Tests/RenderingParityTests.cs`
- Brief intent per file:
  - `src/Flutter/Rendering/Box.cs`: clamp `Tighten(width/height)` values to existing min/max bounds.
  - `src/Flutter.Tests/RenderingParityTests.cs`: add parity regression for out-of-range tighten requests.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/RenderingParityTests.cs`
- New tests to add:
  - `src/Flutter.Tests/RenderingParityTests.cs` (`BoxConstraints_Tighten_ClampsRequestedSizeToCurrentRange`)

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample behavior changes; parity matrix update not required.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [ ] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (render constraints parity note expanded)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
