# Feature: sample-2026-05-09-bloc-counter-route-parity

## Goal

- Restore C#/Dart sample route parity by adding the missing Dart `Bloc counter` demo (`/bloc-counter`) that already exists in the C# sample.

## Non-Goals

- Reworking framework-side bloc primitives or adding new framework runtime behavior.
- Expanding this iteration into new Material controls.

## Context Budget Plan

- Budget: max 10 files in initial read.
- Entry files:
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `src/Sample/Plumix.Sample/Demos/General/BlocCounterDemoPage.cs`
  - `dart_sample/lib/sample_routes.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `docs/ai/PARITY_MATRIX.md`
- Expansion trigger:
  - Expand only for tracking/doc updates required to record parity closure.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Sample route/page parity (`Bloc counter` demo) across C# and Dart samples.
- Completion checklist (must be closed in this iteration unless explicitly blocked):
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [ ] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Sample feature/route/module parity between C# and Dart samples is required.
  - Dart usage patterns remain source-of-truth for app-level parity structure when mirroring sample routes.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `src/Sample/Plumix.Sample/Demos/General/BlocCounterDemoPage.cs`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - none.

## Planned Changes

- Files to edit:
  - `dart_sample/pubspec.yaml`
  - `dart_sample/lib/sample_routes.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/demos/general/bloc_counter_demo_page.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `pubspec.yaml`: add bloc dependencies for parity demo (`bloc`, `flutter_bloc`, `bloc_concurrency`).
  - Dart route/menu files: wire `/bloc-counter` into route constants and General tab menu.
  - new Dart demo page: mirror C# bloc counter behavior and restartable refresh flow.
  - tracking docs/changelog: record closed parity drift.

## Test Plan

- Existing tests to run/update:
  - none (sample-only parity update).
- New tests to add:
  - none in this iteration.
- Parity-risk scenarios covered:
  - Route-map drift (`/bloc-counter` only present on C# side) is removed.
  - Demo interaction parity for bloc event flow (`increment/decrement/reset` + restartable refresh) is mirrored structurally.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [ ] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [ ] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps (if any) are documented with blocker + next action
