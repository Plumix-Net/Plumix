# Feature: material-2026-05-03-drawer-runtime-demo-parity

## Goal

- Add a dedicated C#/Dart sample parity route for framework Material `Drawer` so runtime verification of start/end drawer choreography and theme/widget precedence is available in the Material tab.

## Non-Goals

- No framework `Drawer`/`Scaffold` behavior changes in this pass.
- No new drawer physics or gesture recognizer changes in this pass.

## Context Budget Plan

- Budget: max 12 files in initial read.
- Entry files:
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/DividerDemoPage.cs`
  - `dart_sample/lib/sample_routes.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/demos/material/divider_demo_page.dart`
  - `src/Plumix.Material/Scaffold.cs`
- Expansion trigger:
  - Expand only to docs/tracking files once C#/Dart route parity is wired and builds cleanly.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Drawer` sample runtime parity route (`/drawer`).
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
  - Sample parity changes stay mirrored between C# and Dart in the same iteration.
  - Framework behavior remains in framework libraries; this pass only adds runtime probe pages.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `dart_sample/lib/demos/material/drawer_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
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
  - `src/Sample/Plumix.Sample/Demos/Material/DrawerDemoPage.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/material/drawer_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
  - `docs/ai/material-2026-05-03-drawer-runtime-demo-parity.md`
- Brief intent per file:
  - add mirrored runtime probe pages and route wiring in both samples, then record the shipped parity delta.

## Test Plan

- Existing tests to run/update:
  - `dotnet build src/Plumix.sln -c Debug` (expected iOS workload/Xcode mismatch outside this scope)
  - `dotnet build src/Sample/Plumix.Sample/Plumix.Sample.csproj -c Debug`
- New tests to add:
  - none (sample parity route only; framework drawer tests already exist in `src/Plumix.Tests/MaterialScaffoldTests.cs`).
- Parity-risk scenarios covered:
  - start/end drawer open/close choreography via `ScaffoldState` methods;
  - `DrawerTheme` vs widget override precedence for color/elevation/width;
  - scrim override precedence (`DrawerTheme.scrimColor` vs widget `drawerScrimColor`).

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added) - not required in this pass (no new coverage area).

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps (if any) are documented with blocker + next action
