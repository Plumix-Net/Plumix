# Feature: material-2026-04-05-icon-widget-parity

## Goal

- Port core Flutter-like `Icon` support into framework widgets (`IconData` + `Icon`) and remove `DemoIconGlyph` sample placeholder by switching to real `Icon(Icons.*)` usage.

## Non-Goals

- Full generated Material icon catalog parity (`Icons` contains only the subset used by current sample/tests).
- Full `IconThemeData` surface parity (`opacity`, variable-font axes, shadows, semantic wrappers) while corresponding framework primitives are still absent.

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter/Widgets/Text.cs`
  - `src/Flutter/Widgets/IconTheme.cs`
  - `src/Flutter.Material/IconButton.cs`
  - `src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`
  - `dart_sample/lib/material_buttons_demo_page.dart`
  - `src/Flutter.Tests/TextWidgetTests.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/icon.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/icon_data.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/icons.dart`
- Expansion trigger:
  - Extend or adjust core widget/render primitives only if needed to close `Icon` composition/layout behavior in the same iteration.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Icon` (`IconData` + minimal `Icons` provider for sample parity)
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
  - Framework behavior stays in framework widgets/material layers (`src/Flutter`, `src/Flutter.Material`) without shifting rendering logic into Avalonia controls.
  - Sample parity between `src/Sample/Flutter.Net` and `dart_sample` remains aligned in the same iteration.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/icon.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/icon_data.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/icons.dart`
  - `dart_sample/lib/material_buttons_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - `src/Flutter/Widgets/Icon.cs`: semantic label is accepted on API surface but not yet wired to semantics widgets, because framework currently lacks `Semantics`/`ExcludeSemantics` widget wrappers.
  - `src/Flutter.Material/Icons.cs`: only a sample-driven constant subset (`ArrowBack`, `Menu`, `Close`, `Add`, `InfoOutline`, `Star`, `StarOutline`) is shipped instead of the full generated Flutter `Icons` table.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Flutter.Material.csproj`
  - `src/Flutter.Material/Assets/Fonts/MaterialIcons-Regular.otf`
  - `src/Flutter.Material/Assets/Fonts/MaterialIcons_LICENSE.txt`
  - `src/Flutter/Widgets/Icon.cs`
  - `src/Flutter.Material/Icons.cs`
  - `src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`
  - `src/Sample/Flutter.Net/AppBarIconThemeDemoPage.cs`
  - `src/Sample/Flutter.Net/AppBarLeadingWidthDemoPage.cs`
  - `src/Sample/Flutter.Net/AppBarActionsPaddingDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/app_bar_icon_theme_demo_page.dart`
  - `dart_sample/lib/app_bar_leading_width_demo_page.dart`
  - `dart_sample/lib/app_bar_actions_padding_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `src/Flutter.Tests/TextWidgetTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-05-icon-widget-parity.md`
- Brief intent per file:
  - `Flutter.Material.csproj` + `Assets/Fonts/*`: bundle Material icon font as Material-layer Avalonia resource for runtime icon glyph rendering.
  - `Icon.cs`: add `IconData` and `Icon` widget composition with `IconTheme` defaults and optional RTL mirroring.
  - `Icons.cs`: add sample-required Material icon constants used by sample/tests.
  - sample pages: replace placeholder/text badge visuals with real `Icon(Icons.*)` in icon-focused app-bar demos and demo-shell back action.
  - tests: add focused icon behavior coverage.
  - docs/changelog files: mark parity status and coverage updates.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/TextWidgetTests.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
- New tests to add:
  - icon-focused scenarios in `TextWidgetTests.cs`
- Parity-risk scenarios covered:
  - `IconTheme` fallback for size/color.
  - Explicit size/color override precedence.
  - Null-icon square layout behavior.
  - `matchTextDirection` RTL mirroring via transform.

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
