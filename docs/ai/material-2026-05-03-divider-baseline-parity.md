# Feature: material-2026-05-03-divider-baseline-parity

## Goal

- Add framework Material `Divider`/`VerticalDivider` parity baseline in one iteration (`API/defaults/theme precedence/layout/paint`) with focused tests and C#/Dart sample runtime probes.

## Non-Goals

- No `ListTile.divideTiles` or `DataTable` divider integration in this pass.
- No advanced shape-border geometry beyond current framework `BorderRadius` primitive.

## Context Budget Plan

- Budget: max 14 files in initial read.
- Entry files:
  - `src/Plumix.Material/ThemeData.cs`
  - `src/Plumix.Material/Scaffold.cs`
  - `src/Plumix.Tests/MaterialScaffoldTests.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/divider.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/divider_theme.dart`
- Expansion trigger:
  - Expand only if divider render/layout behavior requires extra core rendering primitives to close parity in this request.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Divider` / `VerticalDivider`
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
  - Theme precedence chain remains `widget -> local theme -> ThemeData -> mode defaults`.
  - Material M2/M3 mode-aware defaults must stay explicit and test-covered.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/divider.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/divider_theme.dart`
  - `dart_sample/lib/demos/material/divider_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - none

## Planned Changes

- Files to edit:
  - `src/Plumix.Material/Divider.cs`
  - `src/Plumix.Material/DividerTheme.cs`
  - `src/Plumix.Material/ThemeData.cs`
  - `src/Plumix.Tests/MaterialDividerTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/DividerDemoPage.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/material/divider_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - add new Material divider primitives, wire theme surfaces, add focused tests, and keep C#/Dart sample parity.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialDividerTests.cs`
- New tests to add:
  - `src/Plumix.Tests/MaterialDividerTests.cs`
- Parity-risk scenarios covered:
  - M2 vs M3 default color/thickness fallback.
  - Theme and widget override precedence for color/space/thickness/indents.
  - Vertical-divider width/indent/end-indent behavior.

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
