# Feature: rendering-2026-03-10-invalidation-and-compositing-noops

## Goal

- Eliminate redundant layout/paint work caused by no-op property assignments in core rendering paths.
- Strengthen compositing parity for root-layer replacement no-op scenarios.

## Non-Goals

- No functional behavior changes for real property updates.
- No sample route/module changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/Rendering/Flex.RenderFlex.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter/RenderView.cs`
  - `src/Flutter.Tests/RenderingParityTests.cs`
  - `src/Flutter.Tests/CompositingLayerTests.cs`
- Expansion trigger:
  - Open additional files only if no-op invalidation fixes trigger regressions outside rendering/compositing tests.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Layout/paint invalidation should be predictable and proportional to actual state changes.
  - Layer updates should avoid unnecessary subtree repaint work.

## Planned Changes

- Files to edit:
  - `src/Flutter/Rendering/Flex.RenderFlex.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter/RenderView.cs`
  - `src/Flutter.Tests/RenderingParityTests.cs`
  - `src/Flutter.Tests/CompositingLayerTests.cs`
- Brief intent per file:
  - `src/Flutter/Rendering/Flex.RenderFlex.cs`: add no-op guards for unchanged layout-affecting enum properties.
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`: add no-op guard for unchanged `RenderColoredBox.Color`.
  - `src/Flutter/RenderView.cs`: skip repaint when replacing root layer with the same instance.
  - `src/Flutter.Tests/RenderingParityTests.cs`: add regression coverage for no-op `RenderFlex` updates.
  - `src/Flutter.Tests/CompositingLayerTests.cs`: add regression coverage for no-op colored box update and same-layer root replacement.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/RenderingParityTests.cs`
  - `src/Flutter.Tests/CompositingLayerTests.cs`
- New tests to add:
  - `src/Flutter.Tests/RenderingParityTests.cs` (`RenderFlex_IgnoresNoOpDirectionUpdate`)
  - `src/Flutter.Tests/RenderingParityTests.cs` (`RenderFlex_IgnoresNoOpMainAxisSizeUpdate`)
  - `src/Flutter.Tests/RenderingParityTests.cs` (`RenderFlex_IgnoresNoOpMainAxisAlignmentUpdate`)
  - `src/Flutter.Tests/RenderingParityTests.cs` (`RenderFlex_IgnoresNoOpCrossAxisAlignmentUpdate`)
  - `src/Flutter.Tests/CompositingLayerTests.cs` (`RenderColoredBox_NoOpColorUpdate_DoesNotRepaintChild`)
  - `src/Flutter.Tests/CompositingLayerTests.cs` (`ReplaceRootLayer_WithSameLayer_DoesNotRepaintTree`)

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample-level behavior changes; parity matrix update is not required.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (M1 closure and M2 activation)
- [x] `docs/ai/TEST_MATRIX.md` updated (render/compositing invalidation coverage expanded)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
