# Feature: material-2026-04-11-control-semantics-annotations-parity

## Goal

- Close the accessibility-label/state gap for framework Material controls by adding a reusable semantics annotation primitive and wiring `semanticLabel`/checked-state semantics for toggle controls.

## Non-Goals

- No full `Tooltip` popup behavior port in this iteration.
- No `Hero` transition parity work in this iteration.
- No bottom-navigation tooltip rendering parity in this iteration.

## Context Budget Plan

- Budget: max 16 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/Checkbox.cs`
  - `src/Flutter.Material/Switch.cs`
  - `src/Flutter.Material/Radio.cs`
  - `src/Flutter.Cupertino/CupertinoCheckbox.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
- Expansion trigger:
  - Expand only to tracking docs and focused test updates required to close semantics wiring end-to-end.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Material interactive control semantics wiring (`MaterialButtonCore` + checkbox/switch/radio toggle states).
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
  - Accessibility/semantics behavior remains framework-owned in `src/Flutter` and `src/Flutter.Material`.
  - Material/Cupertino adaptive control behavior remains within widget/render layers without host-control leakage.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/basic.dart` (`Semantics`)
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/checkbox.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/switch.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/radio.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `Tooltip` behavior (overlay popup + timing) remains out of scope in this pass; only semantics annotation plumbing is delivered.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/Semantics.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/Checkbox.cs`
  - `src/Flutter.Material/Switch.cs`
  - `src/Flutter.Material/Radio.cs`
  - `src/Flutter.Cupertino/CupertinoCheckbox.cs`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `src/Flutter.Tests/MaterialSwitchTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-11-control-semantics-annotations-parity.md`
- Brief intent per file:
  - semantics primitive + render wiring in core framework;
  - Material control semantics state propagation (enabled/checked/button/label);
  - focused regression tests for checkbox and switch semantic labels/states;
  - docs/tracking updates.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter "FullyQualifiedName~MaterialCheckboxTests|FullyQualifiedName~MaterialSwitchTests"`
- New tests to add:
  - `src/Flutter.Tests/MaterialCheckboxTests.cs` semantic-label coverage
  - `src/Flutter.Tests/MaterialSwitchTests.cs` semantic-label coverage
- Parity-risk scenarios covered:
  - semantic label propagation to semantics tree,
  - checked/unchecked state flags for toggle controls,
  - enabled/tap-action semantics.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed; not required in this pass because sample route/module structure did not change)

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
