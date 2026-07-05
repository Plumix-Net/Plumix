# Feature: material-2026-03-14-appbar-icon-theme-runtime-demo-parity

## Goal

- Add a dedicated sample parity route/page to visually validate `AppBar` icon-theme precedence at runtime:
  - `iconTheme`: widget override -> theme appBarTheme -> foreground fallback,
  - `actionsIconTheme`: widget override -> theme appBarTheme -> iconTheme fallback -> foreground fallback.
- Keep C# and Dart sample route/module structure fully mirrored.

## Non-Goals

- No framework API changes in this iteration.
- No new framework tests (runtime sample probe only).

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `src/Sample/Flutter.Net/Demos/Material/AppBarActionsPaddingDemoPage.cs`
  - `dart_sample/lib/demos/material/app_bar_actions_padding_demo_page.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
- Expansion trigger:
  - Open additional sample helper files only if probe widgets require shared utility extraction.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Sample parity changes are applied to C# and Dart in the same iteration.
  - Framework behavior stays in framework libraries; sample page is a runtime validation harness.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `dart_sample/lib/counter_widgets.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - `<none>`

## Planned Changes

- Files to edit:
  - `src/Sample/Flutter.Net/Demos/Material/AppBarIconThemeDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/material/app_bar_icon_theme_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
- Brief intent per file:
  - add mirrored runtime probe page for icon-theme precedence with leading/actions context widgets;
  - wire route in both sample menus;
  - update parity/docs tracking.

## Test Plan

- Existing tests to run/update:
  - `dotnet build src/Sample/Flutter.Net/Flutter.Net.csproj -c Debug`
  - `dart analyze dart_sample`
- New tests to add:
  - `<none>` (sample runtime probe page)
- Parity-risk scenarios covered:
  - leading/actions icon theme precedence order is visually observable;
  - actions icon fallback to icon theme and foreground fallback are visible under toggles;
  - default reference app bar remains available for side-by-side comparison.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (new route row)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` update not required (no new framework coverage area)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
