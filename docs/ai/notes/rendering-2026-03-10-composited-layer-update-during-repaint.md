# Feature: rendering-2026-03-10-composited-layer-update-during-repaint

## Goal

- Ensure composited layer property updates (`Opacity`/`Transform`/`ClipRect`) are not dropped when the same repaint boundary is also dirty for paint in that frame.

## Non-Goals

- No redesign of layer architecture.
- No changes to host integration APIs.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Rendering/Object.RenderObject.cs`
  - `src/Flutter/PipelineOwner.cs`
  - `src/Flutter.Tests/CompositingLayerTests.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Expand only if a failing test indicates dependency on additional paint-path files.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Pipeline ordering remains `layout -> compositing bits -> paint -> semantics`.
  - Repaint boundary layer updates must remain deterministic and frame-consistent.

## Planned Changes

- Files to edit:
  - `src/Flutter/Rendering/Object.RenderObject.cs`
  - `src/Flutter/PipelineOwner.cs`
  - `src/Flutter.Tests/CompositingLayerTests.cs`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `Object.RenderObject.cs`: keep composited-layer-update dirty bit even if paint is already dirty.
  - `PipelineOwner.cs`: apply pending composited-layer updates before repainting repaint boundaries.
  - `CompositingLayerTests.cs`: add regression test for combined repaint + layer update in one frame.
  - `TEST_MATRIX.md`: mention new covered scenario.
  - `CHANGELOG.md`: record the fix.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/CompositingLayerTests.cs`
  - `src/Flutter.Tests/Flutter.Tests.csproj` (full)
- New tests to add:
  - Added test in `src/Flutter.Tests/CompositingLayerTests.cs`.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
