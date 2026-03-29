# Feature: material-2026-03-29-appbar-primary-safearea-parity

## Goal

- Align framework `AppBar` with Flutter status-bar inset behavior so toolbar content does not overlap the system status bar in edge-to-edge hosts.

## Non-Goals

- No `systemOverlayStyle` implementation in this iteration.
- No sample route/module structure changes.
- No sliver app-bar behavior changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `AGENTS.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter/Widgets/MediaQuery.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Expansion trigger:
  - Expand only when parity behavior could not be validated from app-bar/material host + tests.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework behavior remains in `src/Flutter.Material` and framework widget primitives.
  - Dart `AppBar` behavior is source of truth for `primary`/status-bar inset defaults.
  - Existing app-bar theming/layout precedence remains intact.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - none

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `src/Flutter.Material/Scaffold.cs`: add Flutter-like `AppBar.primary` (default `true`) and apply top safe-area inset when ambient `MediaQuery` exists.
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`: add focused coverage for `primary=true` inset application and `primary=false` opt-out.
  - docs/changelog: track shipped parity behavior and test coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialScaffoldTests`
- New tests to add:
  - `AppBar_PrimaryTrue_AppliesMediaQueryTopPadding`
  - `AppBar_PrimaryFalse_DoesNotApplyMediaQueryTopPadding`
- Parity-risk scenarios covered:
  - App-bar toolbar no longer overlaps status-bar area when `MediaQuery` top padding is present.
  - `primary: false` preserves opt-out parity and does not inject top inset.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` update not required (no sample route/module change)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
