# Feature: Material ColorScheme and Typography foundation

## Goal

- Replace ad hoc theme constants with a source-shaped Material 3 color-role and typography foundation that can
  drive existing controls through `ThemeData`.

## Non-Goals

- Directly rewrite every existing component default class from legacy `ThemeData` compatibility fields to
  `ThemeData.ColorScheme` in this foundation iteration.
- Port the legacy Material 2014/2018 and locale-script-specific typography tables.

## Context Plan

- Entry files:
  - `src/Plumix.Material/ThemeData.cs`
  - Flutter `color_scheme.dart`
  - Flutter `typography.dart`
- Expansion trigger:
  - The component closeout must enter each control's Dart defaults and focused tests before changing its token
    precedence.

## Delivery Scope

- Target feature:
  - Material 3 `ColorScheme`, `TextTheme`, `Typography.material2021`, and `ThemeData` integration.
- Completion checklist:
  - [x] Color API/default values
  - [x] HCT seed variants and contrast levels
  - [x] Complete Material 2021 type scale
  - [x] ThemeData projection and interpolation
  - [x] Focused tests

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Package direction remains `Plumix.Material -> Plumix`; the color algorithm is a platform-neutral Material
  dependency and introduces no host behavior.

## Dart Reference Mapping

- Flutter/Dart source files used as source of truth:
  - `packages/flutter/lib/src/material/color_scheme.dart`
  - `packages/flutter/lib/src/material/typography.dart`
  - `packages/flutter/lib/src/material/text_theme.dart`
  - `packages/flutter/lib/src/material/theme_data.dart`
  - `dart_sample/lib/demos/material/navigation_surfaces_demo_page.dart`
- Parity mapping checklist:
  - [x] Material 3 role API/default values mapped
  - [x] Seed generation and override composition mapped
  - [x] Material 2021 typography mapped
  - [x] ThemeData construction/interpolation mapped
  - [ ] Every component default reads `ColorScheme` directly
  - [ ] Material 2014/2018 and locale script geometry mapped

## Planned Closeout

- Migrate one related component family at a time from flat compatibility colors to the exact Dart
  `theme.colorScheme` roles, retaining legacy M2 paths where Dart does.
- Completed component families:
  - `NavigationBar` (direct M2/M3 roles, exact M2 overlay background, M3 stadium indicator, theme copy/lerp).
  - `NavigationRail` (direct M2/M3 roles, source M2 icon opacity/ink policy, M3 stadium indicator, theme copy/lerp).
  - `NavigationDrawer` (direct M3 surface/destination roles, disabled states, stadium indicator, theme copy/lerp).
  - Legacy `BottomNavigationBar` (direct fixed/shifting and light/dark roles, source body typography, icon opacity,
    default elevation/shadow, theme copy/lerp).
  - `BottomAppBar` (direct M2/M3 surface/tint/shadow roles, physical surface composition, configured FAB geometry,
    notch hit testing, inherited theme capture, theme copy/lerp).
  - `FloatingActionButton` (direct M2/M3 foreground/background/state roles, source shapes and adaptive cursors,
    default/null hero-tag policy, extended overflow layout, merged semantics, inherited theme copy/lerp).
  - `IconButton` (exact M2 legacy versus M3 styled composition, direct scheme roles, all variants, density/cursor/
    tooltip/state-controller surfaces, inherited theme copy/lerp, and Material `Theme` icon inheritance).
  - `Card` (exact M2/M3 elevated/filled/outlined roles, source Material/semantics composition, tint/shadow/shape/
    clip/border-order behavior, local theme copy/lerp).
  - `Divider`/`VerticalDivider` (direct M2/M3 roles, source container/border composition, hairlines, directional
    indents/radii, inherited theme copy/lerp).
  - `Badge` (direct M3 error/on-error roles, generated-default precedence, negative narrow-child alignment space,
    large-label stadium geometry, and inherited theme copy/lerp).
  - `RefreshIndicator`/`RefreshProgressIndicator` (direct primary role, source circular Material surface and
    two-controller transition composition, leading overscroll-chrome suppression, and runtime color updates).
  - `CircleAvatar` (direct M3 primary-container/on-primary-container roles, exact M2 brightness fallback,
    circular image layering, and implicit color/diameter transitions).
  - `TextButton` (direct M2/M3 primary/on-surface roles, executable overlay opacities, generated M3 icon defaults,
    inherited theme capture, and source callback/state/semantic plumbing).
- Add `Typography.Material2014`/`Material2018`, exact platform black/white themes, dense/tall geometry, and locale
  script-category selection.
- Remove the narrowed `ColorScheme + Typography closeout` row from `docs/MATERIAL_TODO.md` only after those checks
  are complete.

## Test Plan

- Existing tests:
  - `src/Plumix.Tests/MaterialThemeAnimationTests.cs`
  - all component-focused Material test files
- New tests:
  - `src/Plumix.Tests/MaterialColorSchemeTests.cs`
- Covered risks:
  - exact Flutter seed outputs, all variants, light/dark roles, contrast guards, copy/lerp, theme projection,
    type-scale metrics, platform fonts, and theme animation.

## Sample Parity Plan

- [x] C# palette/typography probe added
- [x] Dart sample probe mirrored
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] Material 3 foundation scope is closed and tested
- [x] No invariant violations introduced
- [x] Remaining closeout scope has a concrete next action
