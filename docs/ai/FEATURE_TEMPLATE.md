# Feature Template

Use this template for every non-trivial feature or control-parity iteration.

```md
# Feature: <short-name>

## Goal

- What user-visible or framework-level outcome should exist after this change?

## Non-Goals

- What is explicitly out of scope for this iteration?

## Context Budget Plan

- Budget: max <N> files in initial read (recommended: 12-20 for full-control parity work).
- Entry files:
  - <file 1>
  - <file 2>
  - <file 3>
- Expansion trigger:
  - Which concrete need is required to close the target control in this same request?

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - <control name>
- Completion checklist (must be closed in this iteration unless explicitly blocked):
  - [ ] API/default values
  - [ ] Widget composition order
  - [ ] State transitions/interaction states
  - [ ] Constraint/layout behavior
  - [ ] Paint/visual semantics
  - [ ] Focused tests for this control

## Invariants Impacted

- [ ] `docs/ai/INVARIANTS.md` reviewed
- [ ] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - <invariant A>
  - <invariant B>

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - <flutter dart file path>
  - <dart_sample file path>
- Parity mapping checklist:
  - [ ] API/default values mapped
  - [ ] Widget composition order mapped
  - [ ] State transitions/interaction states mapped
  - [ ] Constraint/layout behavior mapped
  - [ ] Paint/visual semantics mapped
- Divergence log (only if needed):
  - <none> or <file + reason + expected delta + follow-up condition>

## Planned Changes

- Files to edit:
  - <file path>
  - <file path>
- Brief intent per file:
  - <path>: <intent>

## Test Plan

- Existing tests to run/update:
  - <test file path>
- New tests to add:
  - <test file path>
- Parity-risk scenarios covered:
  - <scenario tied to Dart behavior>

## Sample Parity Plan

- [ ] C# sample impact checked
- [ ] Dart sample parity checked
- [ ] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [ ] `CHANGELOG.md` updated
- [ ] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [ ] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [ ] One full control (or explicitly scoped feature) is closed end-to-end
- [ ] Behavior implemented
- [ ] Tests updated and passing
- [ ] No invariant violations introduced
- [ ] Parity constraints satisfied
- [ ] Remaining parity gaps (if any) are documented with blocker + next action
```

## Optional Naming Convention

- Suggested branch prefix: `codex/<area>-<feature>`
- Suggested feature id format: `<area>-<yyyy-mm-dd>-<slug>`
