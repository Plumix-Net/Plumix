# Feature: material-2026-03-14-appbar-actions-padding-runtime-demo-parity

## Goal

- Add a dedicated sample parity route/page that visually validates `AppBar` actions-row padding precedence at runtime:
  - theme fallback via `ThemeData.AppBarTheme.ActionsPadding`,
  - widget override via `AppBar.actionsPadding`.
- Keep C# and Dart sample route/module structure in sync.

## Non-Goals

- No framework API changes in this iteration.
- No additional framework tests (runtime sample probe only).

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `src/Sample/Flutter.Net/Demos/Material/AppBarLeadingWidthDemoPage.cs`
  - `dart_sample/lib/demos/material/app_bar_leading_width_demo_page.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
- Expansion trigger:
  - Open shared counter/button sample helpers only if control styling needs alignment.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Sample parity updates are mirrored in C# and Dart in one iteration.
  - Framework behavior remains in framework libraries; sample page is runtime validation only.

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
  - `src/Sample/Flutter.Net/Demos/Material/AppBarActionsPaddingDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/material/app_bar_actions_padding_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
- Brief intent per file:
  - add mirrored runtime probe page for actions-padding theme fallback and widget override;
  - wire route in menu on both sides;
  - update parity/docs tracking.

## Test Plan

- Existing tests to run/update:
  - `dotnet build src/Sample/Flutter.Net/Flutter.Net.csproj -c Debug`
  - `dart analyze dart_sample`
- New tests to add:
  - `<none>` (sample runtime probe page)
- Parity-risk scenarios covered:
  - theme actions-padding changes are visually observable;
  - widget override visibly wins over theme value;
  - default reference app bar stays available for comparison.

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
