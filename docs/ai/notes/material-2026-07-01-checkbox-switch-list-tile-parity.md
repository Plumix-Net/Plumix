# Feature: material-2026-07-01-checkbox-switch-list-tile-parity

## Goal

- Port `CheckboxListTile` and `SwitchListTile` as a paired Material control parity pass, including API/defaults, Flutter-like composition, interaction, layout, semantics, tests, and mirrored C#/Dart runtime demos.

## Non-Goals

- Image-thumb APIs that are not yet available on the framework `Switch` primitive.
- `WidgetStatesController`, `VisualDensity`, and `ListTileTitleAlignment` until their shared framework primitives exist.

## Context Budget Plan

- Budget: max 20 files in the initial read.
- Entry files:
  - `src/Plumix.Material/ListTile.cs`
  - `src/Plumix.Material/Checkbox.cs`
  - `src/Plumix.Material/Switch.cs`
  - `src/Plumix.Material/ListTileTheme.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/checkbox_list_tile.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/switch_list_tile.dart`
- Expansion trigger:
  - Shared focus/semantics primitives and sample/test files may be opened when required to preserve Flutter composition and close both controls in one pass.

## Delivery Scope (Required for Control Parity Work)

- Target controls:
  - `CheckboxListTile`
  - `SwitchListTile`
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for both controls

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Framework behavior stays in `src/Plumix*`; host adapters remain unchanged.
- Missing shared semantics/focus behavior is implemented as framework primitives before composing the controls.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/checkbox_list_tile.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/switch_list_tile.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/list_tile.dart`
  - `dart_sample/lib/demos/material/list_tile_controls_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log:
  - Switch thumb images, `WidgetStatesController`, `VisualDensity`, and `ListTileTitleAlignment` are omitted because the corresponding shared primitives do not exist yet; add them to both base controls and list-tile variants when those primitives land.

## Planned Changes

- Add both controls and control-affinity theme resolution in `src/Plumix.Material`.
- Add shared `MergeSemantics`/`ExcludeFocus` support needed by Flutter's composition.
- Add focused tests and one paired C#/Dart sample page.
- Update roadmap, matrices, and changelog with minimal deltas.

## Test Plan

- Constructor guards and defaults.
- Leading/trailing affinity, including inherited theme affinity.
- Whole-tile toggle/tristate behavior and explicit disabled behavior.
- Selected-color precedence and shrink-wrap embedded control geometry.
- Merged checked/enabled/tap semantics.
- Adaptive constructor routing for both controls.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] Both controls are closed end-to-end for the supported shared framework surface.
- [x] Behavior implemented and tests passing.
- [x] No invariant violations introduced.
- [x] Remaining shared-primitive gaps are documented with a concrete follow-up condition.
