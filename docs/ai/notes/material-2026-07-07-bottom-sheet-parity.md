# Feature: BottomSheet + ModalBottomSheet

## Goal

- Port Flutter's persistent and modal Material bottom-sheet surfaces end-to-end.

## Non-Goals

- Foldable display-feature placement and new shared per-corner radius/mouse-region primitives.

## Context Plan

- Entry files: Flutter `bottom_sheet.dart`/`bottom_sheet_theme.dart`, `Scaffold.cs`, `Dialog.cs`, and focused Material route tests.
- Expansion trigger: persistent presentation required Scaffold LocalHistory/controller integration.

## Delivery Scope

- Target controls: `BottomSheet` and `ModalBottomSheetRoute<T>`.
- [x] API/default values
- [x] Widget composition order
- [x] State transitions/interaction states
- [x] Constraint/layout behavior
- [x] Paint/visual semantics
- [x] Focused tests

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Preserved `Widget -> Element -> RenderObject` ownership and Material-to-core dependency direction.

## Dart Reference Mapping

- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_sheet.dart`
- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/bottom_sheet_theme.dart`
- `dart_sample/lib/demos/material/bottom_sheet_demo_page.dart`
- [x] API/defaults, composition, states, layout, paint, and semantics mapped
- Divergence: registered in `docs/ai/DIVERGENCES.md` for per-corner M3 shape, foldable anchoring/barrier semantics clipping, and handle hover state.

## Test and Sample Plan

- [x] Added `src/Plumix.Tests/MaterialBottomSheetTests.cs`.
- [x] Covered defaults/precedence, drag close, 9/16 sizing, modal results/barrier, and persistent LocalHistory/controller lifecycle.
- [x] Added mirrored C#/Dart `/bottom-sheet` demos and updated `docs/ai/PARITY_MATRIX.md`.

## Docs and Tracking

- [x] Updated changelog, framework plan, module/test/parity matrices, and divergence registry.

## Done Criteria

- [x] Behavior implemented and focused/full tests passing.
- [x] Remaining primitive gaps have explicit close conditions.
