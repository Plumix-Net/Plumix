# Feature: material-2026-04-05-button-stylefrom-iconcolor-shadow-parity

## Goal

- Close remaining Flutter parity gaps in Material button `styleFrom(...)` behavior for disabled icon color mapping and M3/M2 shadow fallback semantics.

## Non-Goals

- No text-scale interpolation changes for icon spacing/padding in icon factories.
- No changes to sample routes/pages.
- No `MediaQuery` text-scaler surface expansion in this iteration.

## Context Budget Plan

- Budget: max 20 files in initial read (recommended: 12-20 for full-control parity work).
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/INVARIANTS.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/filled_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
- Expansion trigger:
  - Expand only if additional Flutter token/default references are required to resolve disabled-state or shadow fallback behavior.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Material button family (`TextButton`, `ElevatedButton`, `OutlinedButton`, `FilledButton`) for `styleFrom` state resolution parity.
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
  - Material behavior remains framework-owned in `src/Flutter.Material`.
  - Default Flutter parity source-of-truth workflow remains unchanged.
  - No host-level behavior moved into framework controls.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/text_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/filled_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/outlined_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - Text-scale-dependent icon spacing interpolation remains intentionally out of scope for this iteration.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `Buttons.cs`: align `styleFrom(iconColor)` disabled mapping for elevated/filled/outlined and align default M3 shadow token behavior for text/outlined.
  - `MaterialButtonsTests.cs`: add disabled-icon mapping coverage for elevated/outlined/filled and split shadow-fallback assertions by M3 vs M2 for text/outlined.
  - docs/changelog: record parity delta and coverage updates.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
- New tests to add:
  - `ElevatedButton_StyleFrom_IconColorWithoutDisabledIcon_UsesIconColorWhenDisabled`
  - `OutlinedButton_StyleFrom_IconColorWithoutDisabledIcon_UsesIconColorWhenDisabled`
  - `FilledButton_StyleFrom_IconColorWithoutDisabledIcon_UsesIconColorWhenDisabled`
  - `TextButton_StyleFrom_ElevationWithoutShadowColor_DoesNotApplyShadowInMaterial3`
  - `TextButton_StyleFrom_ElevationWithoutShadowColor_UsesThemeShadowColorFallbackInMaterial2`
  - `OutlinedButton_StyleFrom_ElevationWithoutShadowColor_DoesNotApplyShadowInMaterial3`
  - `OutlinedButton_StyleFrom_ElevationWithoutShadowColor_UsesThemeShadowColorFallbackInMaterial2`
- Parity-risk scenarios covered:
  - `styleFrom(iconColor: x)` keeps icon color in disabled state when `disabledIconColor` is omitted (elevated/outlined/filled parity with Flutter `defaultColor` semantics).
  - M3 text/outlined defaults keep `shadowColor` transparent, so style-level elevation without explicit shadow color does not paint shadow.
  - M2 text/outlined path still falls back to themed shadow color when style-level elevation is provided.

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
