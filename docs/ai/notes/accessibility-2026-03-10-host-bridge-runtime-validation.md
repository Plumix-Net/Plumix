# Feature: accessibility-2026-03-10-host-bridge-runtime-validation

## Goal

- Add runtime host semantics bridge surface in `FlutterHost`.
- Validate host-level semantics update and action routing flow with automated tests.

## Non-Goals

- No full native platform adapter implementation in this iteration.
- No assistive-tech end-to-end UI automation in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/accessibility-2026-03-10-host-bridge-expectations.md`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter/PipelineOwner.cs`
  - `src/Flutter/Rendering/Semantics.cs`
  - `src/Flutter.Tests/FlutterHostSemanticsTests.cs`
- Expansion trigger:
  - Expand only when wiring concrete platform accessibility adapters.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Semantics ownership stays in framework pipeline.
  - Host APIs consume semantics snapshots and route actions back through `SemanticsOwner`.

## Planned Changes

- Files to edit:
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Tests/FlutterHostSemanticsTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - Expose host-facing semantics root/action APIs and update-notification event.
  - Add regression tests for semantics publication and action dispatch.
  - Align tracking docs for delivered runtime host bridge baseline.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (full run)
- New tests to add:
  - `src/Flutter.Tests/FlutterHostSemanticsTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample route/module delta for this host runtime bridge baseline.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
