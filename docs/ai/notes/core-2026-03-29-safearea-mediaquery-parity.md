# Feature: core-2026-03-29-safearea-mediaquery-parity

## Goal

- Add Flutter-like `MediaQuery`/`SafeArea` primitives in `src/Flutter` so `SafeArea` behavior matches Dart semantics, including `maintainBottomViewPadding`.
- Make `MediaQuery` available by default at the app root in `WidgetHost` and source data from host metrics/insets.

## Non-Goals

- Do not implement full Flutter `MediaQueryData` surface (only parity-critical fields/methods used by `SafeArea` in this iteration).
- Do not add a dedicated sample parity route for `SafeArea` in this iteration.
- Do not support legacy Android non-edge-to-edge behavior.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `AGENTS.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter/WidgetHost.cs`
  - `src/Flutter/Widgets/Basic.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/safe_area.dart`
- Expansion trigger:
  - Open additional files only for test wiring and required tracking docs updates.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework behavior remains in framework layers (`src/Flutter`) without host-control workarounds.
  - Flutter Dart widget semantics are source of truth for API/defaults/composition.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/safe_area.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/media_query.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - `src/Flutter/FlutterHost.cs`: Android non-edge-to-edge path is intentionally unsupported in this iteration; host sets `DisplayEdgeToEdgePreference = true` when an insets manager is available.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/MediaQuery.cs`
  - `src/Flutter/Widgets/SafeArea.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter/WidgetHost.cs`
  - `src/Flutter.Tests/SafeAreaTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `src/Flutter/Widgets/MediaQuery.cs`: add `MediaQueryData` + `MediaQuery` with parity-critical insets helpers and remove APIs.
  - `src/Flutter/Widgets/SafeArea.cs`: add Flutter-like `SafeArea` behavior including `maintainBottomViewPadding`.
  - `src/Flutter/FlutterHost.cs`: surface host metrics/insets for `MediaQueryData`, subscribe to insets/input-pane changes, force edge-to-edge preference when supported.
  - `src/Flutter/WidgetHost.cs`: wrap root with `MediaQuery` and refresh on metrics changes.
  - `src/Flutter.Tests/SafeAreaTests.cs`: add focused parity regression coverage.
  - docs files: track shipped behavior and coverage updates.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/FlutterHostInputTests.cs`
  - `src/Flutter.Tests/InheritedWidgetTests.cs`
- New tests to add:
  - `src/Flutter.Tests/SafeAreaTests.cs`
- Parity-risk scenarios covered:
  - `SafeArea` default padding and descendant `MediaQuery.removePadding` behavior.
  - `maintainBottomViewPadding` when keyboard-consumed bottom padding is `0`.
  - side-selective safe-area removal and `minimum` overrides.
  - root-level ambient `MediaQuery` availability in `WidgetHost`.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` update not required (no new/changed sample route/module)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
