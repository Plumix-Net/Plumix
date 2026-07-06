# Feature: DataTable + PaginatedDataTable

## Goal

- Port Flutter's static and paginated Material data tables as one source-coupled delivery unit.

## Non-Goals

- Full general-purpose `TableColumnWidth` algebra, baseline table cells, `PageStorage`, and primary scroll-controller restoration.

## Context Plan

- Entry files: Flutter `data_table.dart`, `data_table_theme.dart`, `data_table_source.dart`, and `paginated_data_table.dart`.
- Expansion trigger: add the missing core `Table` render primitive, cell gesture callbacks, and a finite cross-axis single-child viewport before composing Material controls.

## Delivery Scope (Required for Control Parity Work)

- Target controls: `DataTable`, `PaginatedDataTable`.
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
- Behavior remains in `Plumix`/`Plumix.Material`; both samples remain route/module aligned.

## Dart Reference Mapping (Required for Ports)

- Flutter sources: `packages/flutter/lib/src/material/data_table.dart`, `data_table_theme.dart`, `data_table_source.dart`, `paginated_data_table.dart`; `packages/flutter/lib/src/widgets/table.dart`; `packages/flutter/lib/src/rendering/table.dart`.
- Sample source: `dart_sample/lib/demos/material/data_table_demo_page.dart`.
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence: active shared-Table/PageStorage gap registered in `docs/ai/DIVERGENCES.md`.

## Test Plan

- `src/Plumix.Tests/MaterialDataTableTests.cs`: contracts, layout, theming, sort/select, source cache, paging, and selected headers.
- Full `Plumix.Tests` suite plus solution/sample builds; Dart format/analyze for mirrored sample.

## Sample Parity Plan

- [x] C# sample route/page added
- [x] Dart sample route/page added
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] Both controls are usable end-to-end
- [x] Focused behavior is implemented and tested
- [x] Remaining shared primitive gaps are registered with a close condition
