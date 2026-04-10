# Feature: material-2026-04-10-appbar-implied-leading-parity

## Goal

- Close `AppBar` implied-leading parity so title-only app bars on non-root routes show the default back affordance like Flutter.

## Non-Goals

- No drawer/end-drawer implied-leading behavior in this iteration.
- No `BackButton`/`CloseButton` dedicated widget parity surface in this iteration.
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
  - `src/Flutter.Material/Scaffold.cs`: implied leading currently maps to back icon path only (`Navigator.CanPop` + `Icons.ArrowBack`), while drawer/close-button branches remain follow-up work.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/PARITY_MATRIX.md`
- Brief intent per file:
  - `Scaffold.cs`: add `AppBar.automaticallyImplyLeading` and default back-leading resolution on non-root routes.
  - `MaterialScaffoldTests.cs`: add focused regression coverage for implied-leading on/off behavior.
  - sample gallery files: remove custom demo-page back button from app bar, keep title-only app bars.
  - docs/changelog: record shipped parity delta and updated sample parity notes.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialScaffoldTests`
- New tests to add:
  - `src/Flutter.Tests/MaterialScaffoldTests.cs` implied-leading coverage.
- Parity-risk scenarios covered:
  - non-root route app-bar shows default back icon with title-only config,
  - explicit opt-out (`automaticallyImplyLeading: false`) suppresses implied back icon.

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
