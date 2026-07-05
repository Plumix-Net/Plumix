# Feature: material-checkbox-adaptive-cupertino-parity

## Goal

- Close the documented `Checkbox.Adaptive` iOS/macOS fallback gap by introducing a Cupertino-like adaptive path in framework scope with parity-critical platform mapping/defaults/tests.

## Non-Goals

- Pixel-perfect anti-aliasing parity across all host GPU backends.

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/Checkbox.cs`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/checkbox.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/cupertino/checkbox.dart`
- Expansion trigger:
  - update docs/changelog + feature tracking after adaptive behavior/tests land.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Checkbox` (`Checkbox.Adaptive` iOS/macOS path)
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
  - Framework control behavior remains in framework libraries (`src/Flutter.Material` + `src/Flutter.Cupertino`), with no host-side checkbox adaptation logic.
  - Adaptive platform behavior follows Flutter Dart source as source-of-truth for platform mapping and ignored-parameter semantics.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/checkbox.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/cupertino/checkbox.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - No behavior-level divergence is currently documented for adaptive checkbox defaults in framework scope.

## Planned Changes

- Files to edit:
  - `src/Flutter.Cupertino/Flutter.Cupertino.csproj`
  - `src/Flutter.Cupertino/CupertinoCheckbox.cs`
  - `src/Flutter.Material/Checkbox.cs`
  - `src/Flutter.Material/Flutter.Material.csproj`
  - `src/Flutter.Tests/Flutter.Tests.csproj`
  - `src/Flutter.Net.sln`
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/material-2026-04-05-checkbox-adaptive-cupertino-parity.md`
- Brief intent per file:
  - `Flutter.Cupertino`: introduce framework Cupertino checkbox primitive.
  - `Checkbox.cs`: adaptive iOS/macOS route to `CupertinoCheckbox` + ignored-parameter behavior through API mapping.
  - `src/Flutter/Rendering/Object.PaintingContext.cs`: add line-drawing primitive used by adaptive vector glyph rendering.
  - `src/Flutter/Rendering/Decoration.cs` + `src/Flutter/Rendering/Proxy.RenderBox.cs`: add brush-backed decoration fill for dark gradient parity.
  - `src/Flutter/Widgets/StrokeGlyph.cs` + `src/Flutter/RenderStrokeGlyph.cs`: reusable vector stroke primitive for check/dash painter parity.
  - `MaterialCheckboxTests.cs`: focused adaptive behavior coverage.
  - docs/changelog: record shipped status and remaining divergence.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/MaterialCheckboxTests.cs`
- New tests to add:
  - adaptive iOS defaults,
  - adaptive ignored parameter behavior (`fillColor`, `materialTapTargetSize`),
  - adaptive macOS shrink-wrap behavior and visual `14x14` geometry.
- Parity-risk scenarios covered:
  - platform adaptive branch mapping (`IOS`/`MacOS`),
  - Cupertino-like token resolution defaults,
  - Flutter-documented ignored parameters for adaptive Cupertino branch.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

Notes:
- No sample route/module structure changes were required in this iteration.

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
