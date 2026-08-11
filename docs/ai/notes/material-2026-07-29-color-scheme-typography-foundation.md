# Feature: Material ColorScheme and Typography foundation

## Goal

- Replace ad hoc theme constants with a source-shaped Material 3 color-role and typography foundation that can
  drive existing controls through `ThemeData`.

## Non-Goals

- Directly rewrite every existing component default class from legacy `ThemeData` compatibility fields to
  `ThemeData.ColorScheme` in this foundation iteration.

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
  - [x] Material 2014/2018 scales and platform color/font themes
  - [x] Locale script-category geometry and localized `Theme.of` resolution
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
  - [x] Material 2014/2018 and locale script geometry mapped

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
  - `ElevatedButton` (direct M2/M3 primary/on-primary/surface-container/on-surface/shadow roles, generated M3
    tint/icon defaults, source animation/cursor/density metadata, callbacks/state/semantics, and theme capture).
  - `OutlinedButton` (direct M2/M3 primary/on-surface/outline roles, exact executable overlay opacities, generated
    M3 tint/icon defaults, source animation/cursor/density metadata, callbacks/state/semantics, and theme capture).
  - `FilledButton`/tonal (direct primary/on-primary and secondary-container/on-secondary-container roles, disabled
    on-surface and shadow roles, generated M3 icon/elevation defaults, callbacks/state/layer builders, and inherited
    theme capture).
  - `DrawerHeader`/`UserAccountsDrawerHeader` (direct primary role, generic decoration and directional-inset API,
    source picture/details layout, reversible arrow motion, localized semantics, and exact divider composition).
  - `MaterialBanner` (direct M2/M3 surface and outline-variant roles, inherited theme capture, exact threshold/vector
    transition composition, theme copy/lerp, and widget/theme/default precedence).
  - `ExpansionPanel`/`ExpansionPanelList` (source card/divider/icon and interaction-color precedence, directional
    header geometry, exact header/body composition, independent transition timing, and radio callback ownership).
  - `ExpansionTile` (direct M2/M3 text/icon roles, source directional/theme/state API, exact `Expansible` and
    `ListTileTheme` composition, per-side/custom shape paint, controller lookup, `PageStorage`, and semantics).
  - `LinearProgressIndicator`/`CircularProgressIndicator` (direct M2/M3 primary/background/secondary-container
    roles, source theme API/copy/lerp/capture, controller precedence, 2023/2024 geometry, range semantics, adaptive
    Cupertino routing, circular padding, and mirrored runtime probes).
  - `ExpandIcon` (exact M2/M3 enabled/disabled colors, directional padding, source half-turn animation,
    action-specific semantic hints, callback/state behavior, and mirrored enabled/disabled runtime probes).
  - `Switch` (direct M2/M3 roles, adaptive Cupertino defaults, state colors, source sizing and transition geometry,
    thumb-image/cursor/drag/padding APIs, theme copy/lerp, and mirrored runtime M2/M3 probes).
  - `Scrollbar` (direct on-surface roles without an M2/M3 split, source `WidgetStateProperty` theme copy/lerp/capture,
    public painter/state extension contracts, fade/hover/drag/track geometry, and adaptive Cupertino behavior).
  - `Radio`/`RadioListTile` (direct M2/M3 roles, state/theme precedence, source painter and toggleable timing,
    background/side/inner-radius APIs, density targets, adaptive registry behavior, and merged tile semantics).
  - Chips (`Chip`/`ActionChip`/`ChoiceChip`/`FilterChip`/`InputChip`) with direct M3 roles, exact M2 derived-color
    alpha behavior, theme copy/lerp/capture, state precedence, and source render/animation composition.
  - Material action buttons (`BackButton`/`CloseButton`/`DrawerButton`/`EndDrawerButton`) with source `IconButton`
    inheritance/composition, standard-component keys, direct M3 `onSurfaceVariant` and M2 legacy icon colors,
    default-platform Android labels, and action-icon theme copy/precedence behavior.
  - `Stepper` (direct light/dark M2/M3 circle, connector, and control roles; source `WidgetStateProperty`,
    directional inset, `BoxBorder`, and gradient APIs; exact connector geometry and old/new icon transitions;
    focused coverage and mirrored runtime probes).
  - `ToggleButtons` (direct `ColorScheme.primary`/`onSurface`/`surface` roles, state-resolving fill API, inherited
    theme copy/lerp/capture, source checked/TextButton composition, adjacent selected-border ownership, intrinsic and
    baseline layout, cross-axis tap targets, RTL/vertical border paint, and elliptical per-corner clipping).
- Completed `Typography.Material2014`/`Material2018`, exact platform black/white themes, dense/tall geometry, and
  locale script-category selection.
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
