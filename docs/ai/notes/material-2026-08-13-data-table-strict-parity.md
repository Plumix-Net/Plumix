# Feature: Material DataTable strict parity

## Goal

- Close the `DataTable` component-family slice of the Material color/typography closeout against Flutter 3.44.

## Non-Goals

- Change `PaginatedDataTable` pagination behavior or close unrelated component families.

## Context Plan

- Entry files: `src/Plumix.Material/DataTable.cs`, `DataTableTheme.cs`, and
  `src/Plumix.Tests/MaterialDataTableTests.cs`.
- Expansion trigger: reuse core table, Material, animation, semantics, and arbitrary-decoration primitives already
  present in Plumix.

## Delivery Scope

- Target control: `DataTable` + `DataTableThemeData`.
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition and documented semantics-boundary exception
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Public decoration types now follow Flutter's general `Decoration` contract; Material remains above core.

## Dart Reference Mapping

- Flutter revision: `559ffa3f75e7` (3.44.0 candidate pin).
- Sources: `material/data_table.dart`, `material/data_table_theme.dart`,
  `test/material/data_table_test.dart`, and `test/material/data_table_theme_test.dart`.
- [x] API/defaults, theme resolution, composition, states, layout, paint, and asserted behaviors mapped.
- Divergence: the existing fragment-semantics row in `docs/ai/DIVERGENCES.md` now includes column headers. Plumix
  places the column-header boundary outside `InkWell` so `RenderTable` retains the role; Flutter keeps the
  annotation inside and merges it upward with the action.

## Planned Changes

- `DataTable.cs`/`DataTableTheme.cs`: source theme chains, roles, composition, row borders, and sort animation.
- `MaterialDataTableTests.cs`: direct-token, fallback, state, semantics, clipping, style, and animation coverage.
- Mirrored samples: expose the direct primary-role and animated-sort probes.

## Test Plan

- Focused `MaterialDataTableTests`, then the repository's four required port gates.
- Risks covered: M2/M3 role split, local/global fallback, selected/disabled states, arbitrary decoration, clipping,
  column-header roles, ambient style merge, and interrupted/redundant sort rebuilds.

## Sample Parity Plan

- [x] C# sample updated
- [x] Dart sample mirrored
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md`, `docs/MATERIAL_TODO.md`, `TEST_MATRIX.md`, and the foundation closeout updated
- [x] `docs/FRAMEWORK_PLAN.md` unchanged because M4 remains in progress

## Done Criteria

- [x] DataTable family closed end-to-end with the unavoidable semantics-fragment exception documented
- [x] Behavior, tests, samples, and tracking updated
