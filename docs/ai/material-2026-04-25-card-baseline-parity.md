# Feature: Material Card Baseline Parity

## Goal

- Add framework-level Material `Card` support with elevated/filled/outlined variants, theme precedence, default geometry/color/elevation behavior, clipping, and sample parity.

## Non-Goals

- Full Flutter `ShapeBorder` hierarchy parity beyond rounded-rectangle shape + optional side.
- Interactive card patterns (`InkWell` inside cards) beyond composing existing controls inside `Card`.
- Advanced semantic merge edge cases beyond exposing `semanticContainer` through framework semantics annotations.

## Context Budget Plan

- Budget: max 20 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `docs/ai/PORTING_MODE.md`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/ListTile.cs`
  - `src/Flutter/Widgets/Basic.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/card.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/card_theme.dart`
- Expansion trigger:
  - Needed `ShapeBorder` and semantics-container primitives to close the Card build/composition contract in this iteration.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `Card`
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- List invariants that this feature touches:
  - Framework behavior stays in `src/Flutter` / `src/Flutter.Material`.
  - Dart implementation remains the source of truth for Material control defaults.
  - Sample route/module parity is maintained between C# and Dart samples.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/card.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/card_theme.dart`
  - `dart_sample/lib/demos/material/card_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log:
  - `src/Flutter/Rendering/Decoration.cs`: current framework scope represents shape as rounded-rectangle `ShapeBorder` with optional side, not Flutter's full `ShapeBorder` hierarchy. Expected delta is limited to custom non-rounded card shapes; remove when a broader shape system lands.

## Planned Changes

- Files edited:
  - `src/Flutter.Material/Card.cs`
  - `src/Flutter.Material/CardTheme.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter/Rendering/Decoration.cs`
  - `src/Flutter/Widgets/Semantics.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter.Tests/MaterialCardTests.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `src/Sample/Flutter.Net/Demos/Material/CardDemoPage.cs`
  - `dart_sample/lib/sample_routes.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/demos/material/card_demo_page.dart`

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter MaterialCardTests`
- New tests added:
  - `src/Flutter.Tests/MaterialCardTests.cs`
- Parity-risk scenarios covered:
  - M3 elevated/filled/outlined default colors, radius, elevation/shadow, margin, and outlined border.
  - M2 variant fallback to elevated defaults.
  - `widget -> CardTheme -> ThemeData` precedence.
  - Surface tint, clipping, elevation guards, and semantic-container wiring.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] One full control is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps are documented with blocker + next action
