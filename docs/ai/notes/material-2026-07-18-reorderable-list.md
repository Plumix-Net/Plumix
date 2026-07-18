# Feature: ReorderableList + ReorderableListView

## Goal

- Port Flutter's keyed core reorderable-list behavior and its Material list-view composition as one paired delivery.

## Non-Goals

- Implement a general root overlay, custom semantics-action service, or prototype-extent sliver inside the control.

## Context Plan

- Entry files: Flutter `widgets/reorderable_list.dart`, Flutter `material/reorderable_list.dart`, and core scroll tests.
- Expansion trigger: add missing drag ownership, variable-extent, cursor, and sample primitives needed by both controls.

## Delivery Scope

- Target controls: `ReorderableList`, `SliverReorderableList`, and `ReorderableListView`.
- [x] API/default values
- [x] Widget composition order within available overlay/sliver primitives
- [x] State transitions/interaction states
- [x] Constraint/layout behavior for natural, fixed, and variable extents
- [x] Paint/visual semantics for gap and pickup proxy
- [x] Focused tests

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Preserves `Widget -> Element -> RenderObject` and keeps Material as a thin composition over core drag/sliver behavior.

## Dart Reference Mapping

- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/reorderable_list.dart`
- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/reorderable_list.dart`
- `dart_sample/lib/demos/material/reorderable_list_demo_page.dart`
- [x] API/default values mapped
- [x] Widget composition order mapped where framework primitives exist
- [x] State transitions/interaction states mapped
- [x] Constraint/layout behavior mapped
- [x] Paint/visual semantics mapped where framework primitives exist
- Divergence: registered in `docs/ai/DIVERGENCES.md` for root overlay/drop motion, prototype extent, continuous
  edge auto-scroll, and custom semantics actions.

## Planned Changes

- Core reorder list and listener APIs, Material wrapper/default handles, focused tests, mirrored samples, and tracking docs.

## Test Plan

- `src/Plumix.Tests/MaterialReorderableListTests.cs`
- Cover guards, callback normalization, real pointer drag, platform handles, split padding, and variable child extents.

## Sample Parity Plan

- [x] C# sample updated
- [x] Dart sample updated
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] Both requested controls are delivered through shared core behavior
- [x] Tests pass
- [x] Sample parity is mirrored
- [x] Remaining primitive-bound gaps have a concrete close condition
