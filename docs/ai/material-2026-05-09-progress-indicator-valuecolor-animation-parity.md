# Feature: material-2026-05-09-progress-indicator-valuecolor-animation-parity

## Goal

- Close the remaining `valueColor` API divergence for framework Material progress indicators by moving from `ValueNotifier<Color?>`-only input to a Flutter-like listenable value contract, including `AlwaysStoppedAnimation<Color?>` usage parity.

## Non-Goals

- Full Flutter generic animation framework (`Animation<T>` hierarchy, tween chains, and curve-driven color animation objects beyond current progress-indicator scope).
- Any paint-geometry changes for linear/circular indicators.

## Context Budget Plan

- Budget: max 12 files in initial read.
- Entry files:
  - `src/Plumix/Foundation/Listenable.cs`
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
- Expansion trigger:
  - Expand only if sample parity wiring or docs tracking requires updates.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `LinearProgressIndicator` + `CircularProgressIndicator` (`valueColor` input contract)
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
  - Dart-source API parity for Material controls remains primary.
  - Behavior stays inside framework widget/material layers (no host-side workarounds).

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
  - `dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - Framework now accepts a value-listenable contract (`IValueListenable<Color?>`) plus `AlwaysStoppedAnimation<Color?>`; this closes the previous `ValueNotifier`-only divergence while keeping framework animation architecture intentionally lightweight.

## Planned Changes

- Files to edit:
  - `src/Plumix/Foundation/Listenable.cs`
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/LinearProgressIndicatorDemoPage.cs`
  - `src/Sample/Plumix.Sample/Demos/Material/CircularProgressIndicatorDemoPage.cs`
- Brief intent per file:
  - `src/Plumix/Foundation/Listenable.cs`: add reusable `IValueListenable<T>` and `AlwaysStoppedAnimation<T>`.
  - `src/Plumix.Material/ProgressIndicator.cs`: switch `valueColor` API and listener plumbing to `IValueListenable<Color?>`.
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`: add explicit `AlwaysStoppedAnimation` value-color precedence regression.
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`: add explicit `AlwaysStoppedAnimation` value-color precedence regression.
  - sample demo pages: align C# sample `valueColor` toggle usage with Dart `AlwaysStoppedAnimation` pattern.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialLinearProgressIndicatorTests.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
- New tests to add:
  - `LinearProgressIndicator_ValueColor_UsesAlwaysStoppedAnimationValue`
  - `CircularProgressIndicator_ValueColor_UsesAlwaysStoppedAnimationValue`
- Parity-risk scenarios covered:
  - constant animation-like value source precedence over `color` and theme fallback;
  - retained live updates from mutable listenable (`ValueNotifier`) through existing listener wiring.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [ ] `docs/ai/PARITY_MATRIX.md` updated (if needed)

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
