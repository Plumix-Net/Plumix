# Feature: textinput-2026-03-10-clipboard-actions-baseline

## Goal

- Add copy/cut/paste keyboard action shortcuts to `EditableText`.
- Keep clipboard editing behavior framework-owned while allowing host clipboard synchronization.

## Non-Goals

- No context-menu/action-sheet UI in this iteration.
- No rich clipboard payloads beyond plain text.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter/Widgets/TextClipboard.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Expand only if platform clipboard API constraints require host-specific adapters.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Text-editing behavior remains in framework layer.
  - Host remains bridge/adaptor for platform clipboard integration.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/TextClipboard.cs`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - Add framework clipboard cache, editable key shortcuts, and host clipboard sync hooks.
  - Add focused regression coverage for copy/cut/paste flows.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (`TextInputTests` filter)
- New tests to add:
  - `src/Flutter.Tests/TextInputTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample route/module delta required for this framework-level shortcut baseline.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
