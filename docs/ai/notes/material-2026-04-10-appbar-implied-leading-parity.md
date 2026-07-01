# Feature: material-2026-04-10-appbar-implied-leading-parity

## Goal

- Close `AppBar` implied-leading parity so non-root routes get Flutter-like default dismiss affordances:
  - standard routes use back icon (`Icons.ArrowBack`),
  - fullscreen dialog routes use close icon (`Icons.Close`).

## Non-Goals

- No drawer/end-drawer implied-leading behavior in this iteration.
- No sliver app-bar or scroll-under behavior changes.

## Context Budget Plan

- Budget: max 14 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Expansion trigger:
  - Expand only if needed to close parity with focused tests and sample C#/Dart sync in the same iteration.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `AppBar`
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
  - Framework behavior remains in `src/Flutter.Material` + core widget/navigation layers, without host-control leakage.
  - Sample parity is updated in both C# and Dart samples in one iteration.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/Scaffold.cs`: drawer/end-drawer implied-leading branches remain follow-up work because scaffold drawer primitives are not implemented yet in framework scope.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `Navigation.cs`: add `PageRoute.FullscreenDialog` parity flag needed by app-bar implied-leading decision.
  - `Scaffold.cs`: resolve implied-leading icon by route mode (`fullscreenDialog` -> close, otherwise back).
  - `MaterialScaffoldTests.cs`: add focused regression coverage for fullscreen-dialog close-icon path.
  - docs/changelog: record shipped parity delta.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialScaffoldTests`
- New tests to add:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs` implied-leading coverage.
- Parity-risk scenarios covered:
  - non-root route app-bar shows default back icon with title-only config,
  - fullscreen dialog route app-bar shows default close icon,
  - explicit opt-out (`automaticallyImplyLeading: false`) suppresses implied back icon.

## Sample Parity Plan

- [x] No C#/Dart sample structure changes required in this follow-up (runtime behavior change is in framework app-bar implied-leading resolution only).

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
