# Feature: rendering-2026-03-10-layout-exception-surfacing

## Goal

- Surface `RenderObject.Layout` failures instead of silently swallowing them.
- Keep render/layout behavior deterministic when Avalonia text services are unavailable in test-hosted environments.

## Non-Goals

- No changes to widget APIs.
- No new sample routes or sample behavior changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Tests/RenderingParityTests.cs`
  - `src/Flutter/Rendering/Object.RenderObject.cs`
  - `src/Flutter/RenderParagraph.cs`
  - `src/Flutter/RenderButton.cs`
- Expansion trigger:
  - Open additional files only if layout exception propagation caused failing framework tests that required render-text fallback handling.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Pipeline phase/layout integrity (layout failures must be observable and not leave hidden inconsistent state).
  - Framework behavior ownership in `src/Flutter` without pushing logic into hosts.

## Planned Changes

- Files to edit:
  - `src/Flutter/Rendering/Object.RenderObject.cs`
  - `src/Flutter.Tests/RenderingParityTests.cs`
  - `src/Flutter/RenderParagraph.cs`
  - `src/Flutter/RenderButton.cs`
  - `src/Flutter/UI/TextLayoutFallback.cs`
- Brief intent per file:
  - `src/Flutter/Rendering/Object.RenderObject.cs`: remove exception swallowing in `Layout`.
  - `src/Flutter.Tests/RenderingParityTests.cs`: add regression for exception propagation + dirty-state preservation after failed layout.
  - `src/Flutter/RenderParagraph.cs`: add constrained fallback sizing when font manager is unavailable.
  - `src/Flutter/RenderButton.cs`: add constrained fallback sizing when font manager is unavailable.
  - `src/Flutter/UI/TextLayoutFallback.cs`: centralize fallback detection and approximate text measurement.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/RenderingParityTests.cs`
  - `src/Flutter.Tests/SemanticsTreeTests.cs`
- New tests to add:
  - `src/Flutter.Tests/RenderingParityTests.cs` (`Layout_PropagatesPerformLayoutException_AndStaysDirty`)

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample feature/route/module behavior changed; parity matrix update is not required for this iteration.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [ ] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (coverage note refined for layout-failure behavior)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
