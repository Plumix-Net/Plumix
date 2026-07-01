# Feature: textinput-2026-03-10-host-preedit-bridge-baseline

## Goal

- Add host-level IME preedit bridge so Avalonia composition updates flow into framework editable text.
- Keep composition ownership in framework layers (`FocusManager` + `EditableText` + `TextEditingController`).

## Non-Goals

- No full surrounding-text/caret-geometry integration with host IME yet.
- No multiline editing or advanced clipboard shortcut expansion in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Expand only if Avalonia text-input client APIs are unclear and require source lookup.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework editing behavior remains in `src/Flutter`.
  - Host stays an adapter layer that routes native input into framework primitives.

## Planned Changes

- Files to edit:
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `src/Flutter/FlutterHost.cs`: expose text-input method client and route preedit updates to focus manager composition update path.
  - `src/Flutter.Tests/TextInputTests.cs`: verify host client provisioning and preedit routing to focused editable controller.
  - Tracking docs: mark host preedit bridge baseline as delivered and narrow remaining IME gaps.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (focused `TextInputTests` first, then full test project)
- New tests to add:
  - `src/Flutter.Tests/TextInputTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample route/module change required.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
