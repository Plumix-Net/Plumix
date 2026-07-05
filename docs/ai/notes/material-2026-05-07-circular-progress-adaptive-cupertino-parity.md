# Feature: material-2026-05-07-circular-progress-adaptive-cupertino-parity

## Goal

- Close `CircularProgressIndicator.adaptive` parity for iOS/macOS by routing to Cupertino spinner behavior while keeping Material behavior on non-Apple platforms.

## Non-Goals

- Full `Animation<Color?>` parity for `valueColor` (framework still uses `ValueNotifier<Color?>`).
- Broader Cupertino progress-indicator family beyond the adaptive circular path.

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
- Expansion trigger:
  - Add missing Cupertino primitive if adaptive path requires dedicated non-Material rendering behavior.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `CircularProgressIndicator.adaptive`
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
  - Keep platform-adaptive behavior in framework widgets (not host control logic).
  - Preserve precedence and fallback rules for Material path on non-adaptive platforms.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/progress_indicator.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/cupertino/activity_indicator.dart`
  - `dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Plumix.Cupertino/CupertinoActivityIndicator.cs`: default Cupertino tick color uses explicit light/dark constants via `isDark` bridge because Plumix Cupertino layer does not have Flutter dynamic color resolution context.

## Planned Changes

- Files to edit:
  - `src/Plumix.Cupertino/CupertinoActivityIndicator.cs`
  - `src/Plumix.Material/ProgressIndicator.cs`
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
- Brief intent per file:
  - `src/Plumix.Cupertino/CupertinoActivityIndicator.cs`: add reusable Cupertino spinner render primitive with animated and partially revealed modes.
  - `src/Plumix.Material/ProgressIndicator.cs`: add `CircularProgressIndicator.Adaptive(...)` and iOS/macOS platform routing to Cupertino spinner.
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`: add adaptive coverage (iOS indeterminate/determinate + Android material fallback).

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialCircularProgressIndicatorTests.cs`
- New tests to add:
  - `CircularProgressIndicator_AdaptiveIOS_Indeterminate_UsesCupertinoActivityIndicator`
  - `CircularProgressIndicator_AdaptiveIOS_Determinate_UsesPartiallyRevealedProgress`
  - `CircularProgressIndicator_AdaptiveAndroid_FallsBackToMaterialIndicator`
- Parity-risk scenarios covered:
  - iOS/macOS adaptive branch ignores Material-only progress styling parameters and uses Cupertino renderer.
  - Non-Apple adaptive branch remains on Material renderer.

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
