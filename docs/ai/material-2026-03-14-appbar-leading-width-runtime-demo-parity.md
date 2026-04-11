# Feature: material-2026-03-14-appbar-leading-width-runtime-demo-parity

## Goal

- Add a dedicated sample parity route/page that visually validates `AppBar` leading-width precedence at runtime:
  - theme fallback via `ThemeData.AppBarTheme.LeadingWidth`,
  - widget override via `AppBar.leadingWidth`.
- Keep the route/module structure mirrored between C# and Dart samples.

## Non-Goals

- No framework API changes in this iteration.
- No new framework tests; this is sample runtime validation coverage.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `src/Sample/Flutter.Net/Demos/Material/MaterialButtonsDemoPage.cs`
  - `dart_sample/lib/demos/material/material_buttons_demo_page.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
- Expansion trigger:
  - Open additional sample files only if parity route wiring or shared button controls are unclear.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - C# and Dart sample route/module parity is updated in the same iteration.
  - Framework behavior stays in framework layers; sample only adds runtime probe UI.

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
  - `src/Sample/Flutter.Net/Demos/Material/AppBarLeadingWidthDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/material/app_bar_leading_width_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
- Brief intent per file:
  - add mirrored runtime probe page for leading-width theme fallback and widget override;
  - wire menu route on both samples;
  - update parity and shipped-change tracking docs.

## Test Plan

- Existing tests to run/update:
  - `dotnet build src/Sample/Flutter.Net/Flutter.Net.csproj -c Debug`
- New tests to add:
  - `<none>` (sample runtime probe page)
- Parity-risk scenarios covered:
  - theme leading-width changes are visually observable;
  - widget override visibly wins over theme value;
  - default reference app bar remains available for side-by-side comparison.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (new route row)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
