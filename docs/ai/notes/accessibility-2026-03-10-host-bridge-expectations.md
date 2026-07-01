# Feature: accessibility-2026-03-10-host-bridge-expectations

## Goal

- Document the host-side accessibility bridge contract for desktop/web/mobile hosts.
- Define how framework semantics data should be exposed to platform accessibility APIs and how actions route back.

## Non-Goals

- No platform-specific bridge implementation code in this iteration.
- No assistive-technology runtime validation in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `src/Flutter/PipelineOwner.cs`
  - `src/Flutter/Rendering/Semantics.cs`
  - `src/Flutter/Rendering/Object.RenderObjectSemantics.cs`
  - `src/Flutter/FlutterHost.cs`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Expand only when a concrete host implementation starts (desktop/web/mobile adapter code).

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Semantics tree ownership remains in framework pipeline (`PipelineOwner` + `SemanticsOwner`).
  - Host remains an adapter that consumes semantics snapshots and routes actions back.

## Bridge Contract (Baseline)

1. Snapshot source:
   - Host reads semantics tree only after `PipelineOwner.FlushSemantics()` completes.
   - Authoritative root is `SemanticsOwner.RootNode`.
2. Node payload expected by hosts:
   - Stable `Id`
   - Geometry (`Rect`, already transformed/clipped by framework semantics pipeline)
   - `Label`, `Flags`, `Actions`, visibility (`IsHidden`)
   - Child hierarchy
3. Action routing:
   - Host action on node id must call `SemanticsOwner.PerformAction(nodeId, action)`.
   - Host does not execute framework callbacks directly; routing stays through `SemanticsOwner`.
4. Update cadence:
   - Host bridge should coalesce updates to frame boundaries (one semantics publish per flushed frame).
   - Host should diff by node id to avoid full tree reallocation when possible.
5. Focus/accessibility sync:
   - Focused semantics state should stay aligned with framework focus (`FocusManager.PrimaryFocus`).
   - Text input semantics should be consistent with `EditableText` state surfaced through focus/text pipeline.

## Per-Host Expectations

- Desktop (Avalonia):
  - Expose semantics tree through an accessibility adapter rooted at `FlutterHost`.
  - Map semantics nodes to virtual automation nodes/peers and raise structure/property changed events on diffs.
  - Route invoke/expand/scroll/value patterns back via `SemanticsOwner.PerformAction`.
- Browser (WASM host):
  - Maintain a synchronized, hidden/overlay DOM accessibility tree with ARIA roles/properties derived from semantics.
  - Keep DOM node ids mapped to semantics ids for deterministic action dispatch.
  - Route click/keyboard accessibility actions into semantics action dispatch.
- Android:
  - Bridge semantics ids/rects/actions to platform accessibility node provider semantics.
  - Route accessibility actions from `AccessibilityNodeProvider` to `SemanticsOwner.PerformAction`.
- iOS:
  - Expose semantics hierarchy via accessibility elements/containers using semantics ids and transformed bounds.
  - Route VoiceOver actions back into framework semantics action dispatch.

## Runtime Baseline Status

- Runtime host bridge surface is now available in `FlutterHost`:
  - `SemanticsRoot`
  - `SemanticsUpdated` event
  - `PerformSemanticsAction(nodeId, action)`
- Framework tests validate host-level semantics publication and action dispatch path (`src/Flutter.Tests/FlutterHostSemanticsTests.cs`).

## Planned Changes

- Files to edit:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
  - `docs/ai/accessibility-2026-03-10-host-bridge-expectations.md`
- Brief intent per file:
  - Mark documentation baseline delivered and clarify remaining runtime-validation gaps.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/SemanticsTreeTests.cs` (existing safety coverage retained)
- New tests to add:
  - None in this docs-only iteration.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample route/module delta for documentation-only accessibility bridge baseline.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Host bridge expectations documented per target host
- [x] Framework/host boundary clarified
- [x] Tracking docs aligned
