# Feature: material-2026-05-07-linear-progress-indicator-baseline-parity

## Goal

- Add framework Material `LinearProgressIndicator` baseline parity in one iteration: API/defaults, determinate/indeterminate behavior, layout/paint, focused tests, and C#/Dart sample route parity.

## Non-Goals

- Full Flutter progress-indicator parity matrix (`valueColor`, external `controller`, `year2023`, `stopIndicatorColor`, `stopIndicatorRadius`, `trackGap`, and circular progress indicators).
- Host-level accessibility role/value mapping beyond current framework `Semantics` label surface.

## Context Budget Plan

- Budget: max 16 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Plumix.Material/ThemeData.cs`
  - `src/Plumix.Material/Divider.cs`
  - `src/Plumix.Tests/MaterialDividerTests.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
- Expansion trigger:
  - Add a dedicated theme surface and focused test harness coverage once baseline control behavior is implemented.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `LinearProgressIndicator`
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
  - Keep parity-first behavior wiring (`value`/indeterminate state machine, RTL flow, and render-object-owned paint).

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
  - `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Plumix.Material/ProgressIndicator.cs`: advanced Flutter API surface intentionally deferred in this baseline (`valueColor`, external `controller`, `year2023`, stop-indicator/track-gap customization). Expected delta: fewer styling/control knobs, while core determinate/indeterminate behavior and theming precedence match current scope. Follow-up condition: add dedicated `ProgressIndicatorThemeData` extended fields and API params when circular/advanced progress parity is scheduled.

## Planned Changes

- Files to edit:
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Material/ProgressIndicatorTheme.cs`
  - `src/Plumix.Material/ThemeData.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/LinearProgressIndicatorDemoPage.cs`
  - `src/Sample/Plumix.Sample/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `ProgressIndicator.cs`: implement control API + state lifecycle + render-object paint logic.
  - `ProgressIndicatorTheme.cs` + `ThemeData.cs`: add theme surface and precedence integration.
  - `MaterialLinearProgressIndicatorTests.cs`: add focused parity coverage.
  - sample files (C# + Dart): add runtime parity demo and route wiring.
  - docs/changelog files: update roadmap, parity matrix, and shipped history.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/Plumix.Tests.csproj`
- New tests to add:
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
- Parity-risk scenarios covered:
  - M2 vs M3 defaults and theme/widget precedence.
  - Determinate value clamping.
  - Indeterminate animation progression across scheduler ticks.
  - RTL direction wiring.
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
