# Feature: focus-2026-03-10-keyboard-baseline

## Goal

- Establish first-class framework keyboard/focus baseline for M2.
- Provide a minimal but test-covered path: host key event -> focus manager -> focused node callback / tab traversal.

## Non-Goals

- No full focus scope policy tree yet.
- No IME/text editing integration in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter/UI/PointerEvents.cs`
  - `src/Flutter/Widgets/Gestures.cs`
  - `src/Flutter.Tests/InheritedWidgetTests.cs` (root element harness pattern)
- Expansion trigger:
  - Open additional files only if focus widget lifecycle or host key dispatch requires framework-layer adjustments.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework behavior remains in `src/Flutter` with host-level key input bridged into framework flow.
  - Input handling remains deterministic and testable through framework abstractions.

## Planned Changes

- Files to edit:
  - `src/Flutter/UI/KeyboardEvents.cs`
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Tests/FocusTests.cs`
- Brief intent per file:
  - `src/Flutter/UI/KeyboardEvents.cs`: introduce framework-level keyboard event model.
  - `src/Flutter/Widgets/Focus.cs`: implement `FocusNode`, `FocusManager`, and `Focus` widget baseline behavior.
  - `src/Flutter/FlutterHost.cs`: route key events to framework focus manager before navigator back handling.
  - `src/Flutter.Tests/FocusTests.cs`: add parity regression coverage for manager/widget-level focus behavior.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj`
- New tests to add:
  - `src/Flutter.Tests/FocusTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - No sample route/module changes; parity matrix update not required.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (M2 already in progress)
- [x] `docs/ai/TEST_MATRIX.md` updated (new focus/keyboard row)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
