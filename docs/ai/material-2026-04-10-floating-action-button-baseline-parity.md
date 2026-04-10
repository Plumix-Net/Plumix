# Feature: material-2026-04-10-floating-action-button-baseline-parity

## Goal

- Add baseline Flutter-like `FloatingActionButton` support to `Flutter.Material` (variants, defaults, interaction states, theming) and close one full control parity pass with focused tests and C#/Dart sample-route parity.

## Non-Goals

- No hero/tooltip port in this iteration (`heroTag`, `Tooltip`) because framework primitives are still missing.
- No mouse-cursor or feedback behavior parity for FAB in this iteration.
- No clip-behavior parity port in this iteration.

## Context Budget Plan

- Budget: max 16 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/IconButton.cs`
  - `src/Flutter.Material/ButtonThemes.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/floating_action_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/floating_action_button_theme.dart`
- Expansion trigger:
  - Expand only for concrete parity closure needs: FAB theme-surface wiring, focused tests, and sample route parity in both C# and Dart.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `FloatingActionButton`
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
  - Material behavior remains framework-owned in `src/Flutter.Material` with no Avalonia-control logic leakage.
  - Sample parity between `src/Sample/Flutter.Net` and `dart_sample` remains updated in the same iteration.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/floating_action_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/floating_action_button_theme.dart`
  - `dart_sample/lib/floating_action_button_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/FloatingActionButton.cs`: `heroTag` and tooltip wrapping are omitted because framework `Hero`/`Tooltip` primitives are not yet present.
  - `src/Flutter.Material/FloatingActionButton.cs`: cursor/feedback/clip behavior knobs are omitted in this baseline for the same primitive-gap reason.
  - `src/Flutter.Material/FloatingActionButton.cs`: M2 focus/hover/splash defaults are approximated from FAB foreground token because `ThemeData.focusColor`/`hoverColor`/`splashColor` are not currently exposed in framework theme data.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/FloatingActionButton.cs`
  - `src/Flutter.Material/FloatingActionButtonTheme.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
  - `src/Sample/Flutter.Net/FloatingActionButtonDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/floating_action_button_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/PARITY_MATRIX.md`
- Brief intent per file:
  - `FloatingActionButton.cs`: add framework FAB control with variant defaults and state/elevation behavior.
  - `FloatingActionButtonTheme.cs` + `ThemeData.cs`: add FAB theme surface and token wiring.
  - `MaterialFloatingActionButtonTests.cs`: add focused parity tests for defaults/states/layout/theme precedence.
  - sample files: add C#/Dart runtime probe route for FAB variants and theme override behavior.
  - docs/changelog files: record shipped control parity pass and remaining documented divergence.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialFloatingActionButtonTests`
- New tests to add:
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
- Parity-risk scenarios covered:
  - Variant-specific sizing and shape defaults (`regular/small/large/extended`).
  - M3 token defaults (`primaryContainer` / `onPrimaryContainer`) and theme override precedence.
  - Extended icon/label composition with directional padding.
  - Elevation-state transitions (`default`/`hovered`/`pressed`/`disabled`).

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Follow-up Fixes

- 2026-04-10: eliminated runtime bottom-overflow and out-of-view FAB probes on the demo route by wrapping page content with `SingleChildScrollView`, replacing `Row + Expanded` probe composition with stacked probe cards, and bounding FAB slots via fixed-height `SizedBox` in both C# and Dart sample implementations (`src/Sample/Flutter.Net/FloatingActionButtonDemoPage.cs`, `dart_sample/lib/floating_action_button_demo_page.dart`).

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps (if any) are documented with blocker + next action
