# Feature: material-2026-05-07-circular-progress-indicator-baseline-parity

## Goal

- Add framework Material `CircularProgressIndicator` baseline parity in one iteration: API/defaults, determinate/indeterminate behavior, layout/paint, focused tests, and C#/Dart sample route parity.

## Non-Goals

- Full Flutter circular progress-indicator parity matrix (`strokeAlign`, `strokeCap`, full `constraints` surface, `trackGap`, `year2023`, external `controller`, and adaptive Cupertino branch).
- Host-level accessibility role/value mapping beyond current framework `Semantics` label surface.

## Context Budget Plan

- Budget: max 16 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Plumix.Material/ThemeData.cs`
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Material/ProgressIndicatorTheme.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
- Expansion trigger:
  - Add dedicated circular theme fields and render-primitive support once baseline control behavior is implemented.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `CircularProgressIndicator`
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
  - Keep Material defaults/theme precedence in framework layer, not in sample host adapters.
  - Keep parity-first behavior wiring (`value`/indeterminate state machine and render-object-owned paint).

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Plumix.Material/ProgressIndicator.cs`: advanced Flutter circular API surface intentionally deferred in this baseline (`strokeAlign`, `strokeCap`, full constraints matrix, `trackGap`, `year2023`, external `controller`, adaptive Cupertino branch). Expected delta: fewer styling/control knobs, while core determinate/indeterminate behavior and theming precedence match current scope.

## Planned Changes

- Files to edit:
  - `src/Plumix/Rendering/Object.PaintingContext.cs`
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Material/ProgressIndicatorTheme.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `PaintingContext.cs`: add reusable arc paint primitive for circular render paths.
  - `ProgressIndicator.cs`: implement circular control API + state lifecycle + render-object paint logic.
  - `ProgressIndicatorTheme.cs`: add circular theme surface.
  - `MaterialCircularProgressIndicatorTests.cs`: add focused parity coverage.
  - sample files (C# + Dart): add runtime parity demo and route wiring.
  - docs/changelog files: update roadmap, parity matrix, and shipped history.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/Plumix.Tests.csproj`
- New tests to add:
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
- Parity-risk scenarios covered:
  - M2 vs M3 defaults and theme/widget precedence.
  - Determinate value clamping.
  - Indeterminate arc animation progression across scheduler ticks.
  - Semantics label + computed percentage fallback.

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
