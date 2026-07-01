# Feature: material-2026-07-01-radio-list-tile-expansion-tile-parity

## Goal

- Port `RadioListTile<T>` and `ExpansionTile` as a paired Material parity pass with Flutter-like APIs, composition, state transitions, animation, layout, semantics, tests, and mirrored C#/Dart demos.

## Non-Goals

- `PageStorage` restoration until the shared page-storage primitive exists.
- `WidgetStatesController`, `VisualDensity`, and `ListTileTitleAlignment` until their shared framework primitives exist.
- Platform accessibility announcements beyond the current framework semantics tree.
- Ordered arrow-key navigation inside modern `RadioGroup` until the shared radio-client focus-order registry exists.

## Context Budget Plan

- Budget: max 20 files in the initial read.
- Entry files:
  - `src/Plumix.Material/Radio.cs`
  - `src/Plumix.Material/ListTile.cs`
  - `src/Plumix.Material/ListTileControls.cs`
  - `src/Plumix/AnimationController.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/radio_list_tile.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/expansion_tile.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/expansible.dart`
- Expansion trigger:
  - Shared expansion, clipping, theming, semantics, test, and sample files may be opened to close both controls in one pass.

## Delivery Scope (Required for Control Parity Work)

- Target controls:
  - `RadioListTile<T>`
  - `ExpansionTile`
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
- Missing reusable expansion/clipping behavior is implemented in framework primitives before Material composition.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/radio_list_tile.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/expansion_tile.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/expansible.dart`
  - `dart_sample/lib/demos/material/radio_expansion_tile_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log:
  - `PageStorage` restoration is omitted because the shared primitive does not exist; expansion state remains stable for the mounted element and external controller lifecycle.
  - Shape animation is limited to the framework's rounded-rectangle/uniform-side shape surface; broaden it when general `ShapeBorder` interpolation lands.
  - Platform announcement/hint strings are represented by expanded-state semantics flags because the current semantics model does not expose hint/live-region fields.
  - `RadioGroup<T>` supplies inherited value/callback coordination; Flutter's ordered arrow-key client traversal remains pending a shared focus-order radio registry.

## Planned Changes

- Add reusable `ExpansibleController`/`Expansible` and self-bounds `ClipRect` support.
- Add `RadioListTile<T>`, `RadioGroup<T>`, `ExpansionTile`, and expansion-tile theming.
- Add focused tests and one paired C#/Dart runtime page.
- Update roadmap, matrices, and changelog.

## Test Plan

- Radio group/legacy callback selection, toggleable behavior, enabled override, affinity, adaptive path, selected-color precedence, scale transform, and merged semantics.
- Expansion initial/controller/tap transitions, callback timing, maintain-state behavior, height/icon/color animation, theme/widget precedence, affinity, disabled behavior, and expanded-state semantics.

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
