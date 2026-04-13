# Feature: material-2026-04-14-list-tile-baseline-parity

## Goal

- Add framework Material `ListTile` parity baseline in C# with Flutter-like defaults for current scope and keep C#/Dart sample parity with a dedicated runtime probe page.

## Non-Goals

- No `VisualDensity` framework primitive in this iteration.
- No advanced custom render-object parity for Flutter `_RenderListTile` geometry edge cases.
- No `CheckboxListTile`/`RadioListTile`/`SwitchListTile` composition in this pass.

## Context Budget Plan

- Budget: max 16 files in initial read.
- Entry files:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `flutter/packages/flutter/lib/src/material/list_tile.dart`
- Expansion trigger:
  - Add missing theme surfaces (`ListTileThemeData`) and dedicated tests/sample pages needed to close `ListTile` parity in one request.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `ListTile`
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
  - Material state-layer and interaction semantics stay in framework-side control composition.
  - Theme precedence remains `widget -> local theme -> global theme defaults`.
  - C# sample and `dart_sample` route/menu parity stay aligned in same iteration.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `flutter/packages/flutter/lib/src/material/list_tile.dart`
  - `flutter/packages/flutter/lib/src/material/list_tile_theme.dart`
  - `dart_sample/lib/demos/material/list_tile_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter.Material/ListTile.cs`: full Flutter `_RenderListTile`/`VisualDensity` parity is out of scope in this baseline; current implementation uses framework widget composition + `MaterialButtonCore` with Flutter-like default heights/colors/interaction semantics for covered scenarios.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/ListTile.cs`
  - `src/Flutter.Material/ListTileTheme.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialListTileTests.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `src/Sample/Flutter.Net/Demos/Material/ListTileDemoPage.cs`
  - `dart_sample/lib/sample_routes.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/demos/material/list_tile_demo_page.dart`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/MODULE_INDEX.md`
- Brief intent per file:
  - Add framework `ListTile` + theme support, focused coverage, and mirrored sample runtime probe in both platforms.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialListTileTests.cs`
- New tests to add:
  - `ListTile_Throws_WhenIsThreeLineAndSubtitleIsNull`
  - `ListTile_DefaultM3_OneLine_UsesMinHeight56`
  - `ListTile_DenseOneLine_UsesMinHeight48`
  - `ListTile_DefaultM3_TwoLine_UsesMinHeight72`
  - `ListTile_DefaultM3_ThreeLine_UsesMinHeight88`
  - `ListTile_Selected_UsesSelectedColorForTitleAndLeadingIcon`
  - `ListTile_SelectedTileColor_OverridesDefaultBackground`
  - `ListTile_ThemeColors_ApplyWhenWidgetOverridesMissing`
  - `ListTile_OnTap_InvokesCallback`
  - `ListTile_Disabled_SemanticsOmitEnabledAndTapAction`
  - `ListTile_Selected_SemanticsIncludeSelectedFlag`
- Parity-risk scenarios covered:
  - M3 default line-height baselines (one/two/three-line).
  - Selected/disabled color and semantics behavior.
  - Theme precedence and runtime interaction callbacks.

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
