# Feature: Material app route and theme foundation

## Goal

- Provide the animation-aware route and theme protocols required before a direct `WidgetsApp`/`MaterialApp` port.

## Non-Goals

- Port the full `WidgetsApp`/`MaterialApp` composition in this iteration.
- Add host predictive-back gestures, route snapshot caching, delegated transitions, or every component-theme lerp.
- Change the mirrored sample bootstrap before `MaterialApp` itself is closed.

## Context Plan

- Entry files:
  - `src/Plumix/Widgets/Navigation.cs`
  - `src/Plumix.Material/Theme.cs`
  - `src/Plumix.Material/ThemeData.cs`
  - `src/Plumix.Material/PageTransitionsTheme.cs`
- Expansion trigger:
  - The next iteration should enter `widgets/app.dart` only after its required route factory, title, localization,
    directionality, selection, messenger, and app-builder composition can be closed together.

## Delivery Scope

- Target feature:
  - Shared route-transition lifecycle plus animated Material theme foundation.
- Completion checklist:
  - [x] Primary and secondary route animations
  - [x] Forward/reverse durations and reverse-exit retention
  - [x] Hero-safe finalization and existing modal-route compatibility
  - [x] `PageRouteBuilder`, `MaterialPageRoute`, and platform transition selection
  - [x] `ThemeData.Lerp`, `ThemeDataTween`, and interrupted `AnimatedTheme`
  - [x] Focused route/theme tests

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Navigation keeps a valid current route while popped routes remain visual-only until reverse completion.
- Core remains independent of Material; Material selects and composes the platform transition builders.

## Dart Reference Mapping

- Flutter/Dart source files used as source of truth:
  - `packages/flutter/lib/src/widgets/routes.dart`
  - `packages/flutter/lib/src/widgets/pages.dart`
  - `packages/flutter/lib/src/material/theme.dart`
  - `packages/flutter/lib/src/material/theme_data.dart`
  - `packages/flutter/lib/src/material/page.dart`
  - `packages/flutter/lib/src/material/page_transitions_theme.dart`
- Parity mapping:
  - [x] Route animation/default APIs
  - [x] Primary/secondary lifecycle and reverse disposal
  - [x] Animated-theme interruption continuity
  - [x] Material platform-builder selection
- Remaining deltas:
  - Snapshot/delegated/predictive transitions and remaining component-theme lerps are recorded in
    `docs/ai/DIVERGENCES.md`.

## Test Plan

- Existing tests:
  - `src/Plumix.Tests/NavigationTests.cs`
  - `src/Plumix.Tests/HeroNavigatorTests.cs`
  - Existing dialog, popup, dropdown, search, bottom-sheet, and pop-scope route suites
- New tests:
  - `src/Plumix.Tests/MaterialThemeAnimationTests.cs`
- High-risk scenarios:
  - Secondary animation forwarding, pop-time route retention, zero-duration compatibility, hero deferral,
    interrupted theme retargeting, endpoint identity, and platform-specific transition durations.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] No sample route/page change; `docs/ai/PARITY_MATRIX.md` does not require a delta

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` updated
- [x] `docs/ai/TEST_MATRIX.md` updated
- [x] `docs/MATERIAL_TODO.md` narrowed to the remaining work

## Next Closure

- Port `WidgetsApp` and `MaterialApp` together, using `MaterialPageRoute` as the default route factory and
  `AnimatedTheme` for theme-mode changes.
