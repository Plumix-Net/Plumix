# Feature: material-2026-04-04-button-surface-tint-parity

## Goal

- Align framework Material button behavior with Flutter `ButtonStyleButton` surface-tint semantics by adding `surfaceTintColor` to style APIs and applying elevation-based surface tint blending to button backgrounds.

## Non-Goals

- No new controls/routes or sample-UI changes.
- No cursor/feedback/visual-density parity in this iteration.
- No host-level system-bar or app-bar changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevation_overlay.dart`
- Expansion trigger:
  - Expand only if elevation-to-opacity interpolation or style precedence cannot be validated from these files.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains in framework libraries (`src/Flutter.Material`).
  - Button style layering remains parity-first (`default -> theme -> widget -> legacy`).
  - No sample route/module divergence introduced.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/button_style_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevation_overlay.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/elevated_button.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - Surface tint is applied in current button paint pipeline via explicit background blending (instead of Flutter `Material` widget internals), while preserving equivalent elevation-based opacity behavior for covered scenarios.

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/ButtonStyle.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `ButtonStyle.cs`: add state-aware `SurfaceTintColor` to style model.
  - `Buttons.cs`: extend `styleFrom(...)`, style composition, and background resolution with surface tint application.
  - `MaterialButtonsTests.cs`: validate style-level and theme-level surface-tint behavior.
  - tracking docs/changelog: record delivered parity increment.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialButtonsTests`
  - `dotnet build src/Flutter.Net.sln -c Debug`
- New tests to add:
  - `ElevatedButton_StyleFrom_SurfaceTintColor_TintsBackgroundByElevation`
  - `ElevatedButton_ThemeStyleSurfaceTintColor_TintsDefaultBackground`
- Parity-risk scenarios covered:
  - Elevation-based tinting with `styleFrom(surfaceTintColor: ...)`.
  - Theme-style tint fallback on default elevated button background.

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
