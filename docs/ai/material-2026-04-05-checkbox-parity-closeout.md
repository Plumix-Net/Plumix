# Feature: material-checkbox-parity-closeout

## Goal

- Close the remaining M4 checkbox parity deltas after baseline delivery by adding dedicated checkbox theming surface, error-state token behavior, adaptive constructor API surface, splash-radius propagation, and animated indicator transitions.

## Non-Goals

- Full Cupertino checkbox rendering parity (adaptive path currently falls back to Material rendering because Cupertino primitives are not implemented in framework scope).
- Mouse-cursor/visual-density semantics parity beyond currently available framework primitives.

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Checkbox.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/checkbox.dart`
- Expansion trigger:
  - Add/update framework primitives required to close checkbox parity in one request (`CheckboxTheme`, error tokens, splash radius propagation through shared ink stack).

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Checkbox`
- Completion checklist (must be closed in this iteration unless explicitly blocked):
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework behavior remains in `src/Flutter` / `src/Flutter.Material` (no host-side checkbox behavior).
  - Shared Material primitives (`MaterialButtonCore`, `InkSplash`) remain reusable for other controls.
  - Dart source remains source-of-truth for checkbox state precedence and defaults within available framework scope.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/checkbox.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/toggleable.dart`
  - `dart_sample/lib/checkbox_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/Checkbox.cs`: adaptive constructor currently uses Material rendering path on iOS/macOS because Cupertino checkbox primitives are not yet available. Expected delta: visual adaptation is not platform-native on Cupertino targets. Follow-up condition: add Cupertino checkbox primitives and route adaptive branch accordingly.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Checkbox.cs`
  - `src/Flutter.Material/CheckboxTheme.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - Material files: close checkbox theming/API/state parity and propagate splash-radius support through shared button/ink path.
  - Test file: add focused regression coverage for the new parity surface.
  - docs/changelog: update M4 status and remove obsolete checkbox pending-gap notes.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
- New tests to add:
  - theme precedence, error-state visuals, adaptive guard, splash-radius propagation, transition animation coverage in `src/Flutter.Tests/MaterialCheckboxTests.cs`
- Parity-risk scenarios covered:
  - precedence order for fill/check/overlay/side tokens,
  - error-state colors and borders,
  - `false -> true -> null` indicator transition visuals,
  - adaptive API guard behavior,
  - splash radius from checkbox theme/widget into render layer.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps (if any) are documented with blocker + next action
