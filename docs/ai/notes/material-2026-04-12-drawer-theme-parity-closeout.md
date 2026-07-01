# Feature: material-2026-04-12-drawer-theme-parity-closeout

## Goal

- Close dedicated `DrawerTheme` parity for framework-scope Material drawer behavior (`Drawer` visuals + `Scaffold` scrim/drag choreography integration).

## Non-Goals

- Full Flutter `DrawerController` route/animation internals beyond current `Scaffold` composition.
- New sample gallery routes/pages (no sample structure change in this iteration).

## Context Budget Plan

- Budget: max 14 files in initial read.
- Entry files:
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/DrawerTheme.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - If drag-progress or scrim precedence paths diverge from visual `Drawer` theme resolution, expand to shared helpers and add focused regression coverage in the same pass.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Material `Drawer` + `Scaffold` drawer theming surface.
- Completion checklist:
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
  - Material drawer behavior remains framework-owned (`src/Flutter.Material`) and does not move into host-specific adapters.
  - Theme precedence remains explicit and testable (`widget -> inherited/local theme -> default`).

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/drawer.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/drawer_theme.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/scaffold.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log:
  - Deeper `DrawerController` route-animation internals remain outside current framework-scope composition and are tracked separately.

## Planned Changes

- Files edited:
  - `src/Flutter.Material/DrawerTheme.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
- Brief intent per file:
  - `DrawerTheme.cs`: add inherited/local drawer theme object + fallback contract to `ThemeData`.
  - `ThemeData.cs`: add `DrawerTheme` data surface on global Material theme.
  - `Scaffold.cs`: resolve `Drawer` background/elevation/shadow/width and scrim by `widget -> drawerTheme -> defaults`; keep drag progress math width-aware for themed drawers.
  - `MaterialScaffoldTests.cs`: add focused precedence + validation + themed-width drag-threshold + scrim tests.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~MaterialScaffoldTests`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore`
- New tests added:
  - `Drawer_UsesDrawerThemeDefaults_WhenWidgetValuesAreNull`
  - `Drawer_WidgetValues_OverrideDrawerThemeDefaults`
  - `Drawer_InvalidThemedWidth_ThrowsArgumentOutOfRange`
  - `Drawer_InvalidThemedElevation_ThrowsArgumentOutOfRange`
  - `Scaffold_DragCancel_UsesThemedDrawerWidth_ForProgressThreshold`
  - `Scaffold_OpenDrawer_UsesDrawerThemeScrimColor_WhenWidgetScrimColorIsNull`
  - `Scaffold_OpenDrawer_WidgetScrimColor_OverridesDrawerThemeScrimColor`
- Parity-risk scenarios covered:
  - Width precedence affects both visual layout and drag progress thresholds.
  - Scrim precedence remains deterministic when both scaffold and drawer theme define colors.
  - Invalid themed width/elevation are guarded.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` update not needed (sample structure/behavior for this pass unchanged)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps documented with blocker + next action
