# Feature: material-2026-03-29-button-elevation-shadow-typography-parity

## Goal

- Close visible C# vs Dart parity gaps in Material button rendering by restoring `ElevatedButton` shadows/elevation and tightening button label typography weight so sample visuals match Flutter reference more closely.

## Non-Goals

- No new routes or sample module additions.
- No changes to Dart sample logic/structure (framework-side parity fix only).
- No mobile host SDK/toolchain environment fixes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter/Rendering/Decoration.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter/Rendering/Object.PaintingContext.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
- Expansion trigger:
  - Expand only if needed to validate Flutter source token/state defaults or sample shell constraints.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned (`src/Flutter` + `src/Flutter.Material`), without host-control fallback.
  - Button defaults/state styling remain parity-first against Flutter behavior.
  - Sample parity remains route/module equivalent between C# and Dart.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `flutter/packages/flutter/lib/src/material/elevated_button.dart`
  - `flutter/packages/flutter/lib/src/material/theme_data.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/demos/material/material_buttons_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - None. Final parity path keeps Flutter baseline (`LabelLarge = Medium`, Android font family `Roboto`).

## Planned Changes

- Files to edit:
  - `src/Flutter/Rendering/Decoration.cs`
  - `src/Flutter/Rendering/Object.PaintingContext.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - rendering files: add `BoxShadows` support to framework decorations and paint path.
  - material files: add state-aware `ShadowColor`/`Elevation` to `ButtonStyle`, wire default elevated states + styleFrom state deltas, and map elevation -> rendered box shadows.
  - `ThemeData.cs`: expose `ShadowColor` token source for default elevated shadows and keep Android `Roboto` fallback for Flutter typography parity.
  - theme/tests: tighten `labelLarge` weight and extend regression coverage for elevated shadow + typography expectations.
  - docs/changelog: track shipped parity hardening.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug`
- New tests to add:
  - `ElevatedButton_DefaultShadow_IsAppliedWhenEnabled`
  - `ElevatedButton_DisabledState_DoesNotApplyShadow`
- Parity-risk scenarios covered:
  - Elevated default-state shadow visibility and disabled-state shadow suppression.
  - Label-large fallback weight consistency when widget text-style resolver returns partial overrides.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
