# Feature: Material AppBar closeout

## Goal

- Close standard `AppBar` API, defaults, composition, scroll-under state, layout, visual, and semantics parity
  against the pinned Flutter revision.

## Non-Goals

- `SliverAppBar` ballistic snap and viewport-generated stretch, which remain tracked separately.
- Replacing the framework's Avalonia-backed public `Color` and `Size` value contracts.

## Context Plan

- Entry files:
  - `src/Plumix.Material/Scaffold.cs`
  - `src/Plumix.Material/AppBarTheme.cs`
  - `src/Plumix.Tests/MaterialScaffoldTests.cs`
- Expansion trigger:
  - Add shared state-color and semantics-sort-key primitives required by the source control.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Material `AppBar`
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
  - Widget to element to render-object ownership remains framework-owned.
  - Material component defaults resolve directly from `ColorScheme` roles.
  - Sample behavior changes remain mirrored between C# and Dart.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `flutter-src/packages/flutter/lib/src/material/app_bar.dart`
  - `flutter-src/packages/flutter/test/material/app_bar_test.dart`
  - `dart_sample/lib/demos/material/app_bar_actions_padding_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergences:
  - `docs/ai/DIVERGENCES.md` records the sealed Avalonia `Color`/`Size` contract limits, their expected API
    delta, and the framework-owned value hierarchy required to close them.

## Planned Changes

- Files to edit:
  - `src/Plumix.Material/Scaffold.cs`, `AppBarTheme.cs`, and `ThemeData.cs`
  - `src/Plumix/Widgets/RawRadio.cs`, `Semantics.cs`, and rendering semantics files
  - `src/Plumix.Tests/MaterialScaffoldTests.cs` and `SemanticsTreeTests.cs`
  - Mirrored AppBar demo and tracking files
- Brief intent per file:
  - Material files: port the standard app bar's source state, defaults, composition, and visual API.
  - Core files: add reusable state-color resolution and ordinal semantics ordering.
  - Tests/samples/docs: cover and expose the port, then record the bounded CLR contract differences.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/MaterialScaffoldTests.cs`
  - `src/Plumix.Tests/SemanticsTreeTests.cs`
- New tests to add:
  - Stateful scroll-under background/elevation and visual/semantic configuration cases.
- Parity-risk scenarios covered:
  - M2/M3 direct defaults, theme/widget precedence, vertical-only scroll state, source composition, and
    ordinal semantic child ordering.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed; no milestone changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps (if any) are documented with blocker + next action
