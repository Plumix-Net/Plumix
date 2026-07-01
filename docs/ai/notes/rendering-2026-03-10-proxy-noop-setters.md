# Feature: rendering-2026-03-10-proxy-noop-setters

## Goal

- Prevent redundant layout invalidations in proxy render objects when setter values are unchanged.
- Add regression coverage for no-op updates in `RenderConstrainedBox` and `RenderPadding`.

## Non-Goals

- No behavioral changes for actual value updates.
- No sample route/module changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Tests/RenderingParityTests.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
- Expansion trigger:
  - Open additional files only if no-op invalidation fix causes broad render pipeline regressions.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Layout invalidation should be predictable and limited to actual state changes.
  - Framework behavior remains within `src/Flutter`.

## Planned Changes

- Files to edit:
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter.Tests/RenderingParityTests.cs`
- Brief intent per file:
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`: avoid `MarkNeedsLayout` on no-op setter updates.
  - `src/Flutter.Tests/RenderingParityTests.cs`: verify no extra relayout on no-op `AdditionalConstraints`/`Padding` updates.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/RenderingParityTests.cs`
- New tests to add:
  - `src/Flutter.Tests/RenderingParityTests.cs` (`RenderConstrainedBox_IgnoresNoOpAdditionalConstraintsUpdate`)
  - `src/Flutter.Tests/RenderingParityTests.cs` (`RenderPadding_IgnoresNoOpPaddingUpdate`)

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample behavior change; parity matrix update not required.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [ ] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (render invalidation coverage note expanded)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
