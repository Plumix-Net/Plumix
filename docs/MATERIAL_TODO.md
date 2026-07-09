# Material Port — Up for Grabs

Widgets and subsystems from Flutter's Material library (`packages/flutter/lib/src/material`) that are **not yet ported** to `src/Plumix.Material`. Pick one, claim it, and submit a PR.

Last verified against Flutter source and `src/Plumix.Material`: **2026-07-10**.

## How to claim and deliver

1. **Claim it**: open a GitHub issue titled `Claim: <Widget>` (or comment on an existing one) so two contributors don't port the same control.
2. **Read the workflow**: [`CONTRIBUTING.md`](../CONTRIBUTING.md) — contributions must be produced with a frontier coding agent (Claude Opus 4.8 / GPT-5.5 or newer). The agent must follow [`docs/ai/PORTING_MODE.md`](ai/PORTING_MODE.md): Dart source is the spec, controls are closed **end-to-end in one PR** (API, defaults, composition, states, layout, paint, theme, tests) — partial ports are sent back.
3. **Scope**: a port includes the widget, its `*Theme`/`*ThemeData` pair (when Flutter has one), tests mapped in [`ai/TEST_MATRIX.md`](ai/TEST_MATRIX.md), and mirrored demo probes in `src/Sample/Plumix.Sample` + `dart_sample` with a [`ai/PARITY_MATRIX.md`](ai/PARITY_MATRIX.md) entry.
4. **Done**: when merged, delete the row from this file in the same PR.

Size legend: **S** — one focused widget, few states. **M** — widget family or nontrivial interaction/paint. **L** — subsystem with core-framework dependencies; open a design issue and align with the maintainer before starting.

## Open controls

| Widget / family | Flutter source (`lib/src/material/`) | Size | Notes / dependencies |
| --- | --- | --- | --- |
| `MenuAnchor`, `MenuBar`, `SubmenuButton`, `MenuItemButton` + `MenuStyle`, `MenuTheme`, `MenuBarTheme`, `MenuButtonTheme` | `menu_anchor.dart`, `menu_style.dart`, `menu_theme.dart`, `menu_bar_theme.dart`, `menu_button_theme.dart` | L | Desktop-critical menu system. Overlay/anchor infra partially exists (see `DropdownMenu`, `SearchAnchor` ports for prior art). |
| `CarouselView` + `CarouselTheme` | `carousel.dart`, `carousel_theme.dart` | M | Scrollable/sliver infra already in core (`Widgets/Scroll.cs`, `Rendering/Sliver.cs`). |
| `ReorderableListView` | `reorderable_list.dart` | L | Requires core `SliverReorderableList`/drag infra first (`widgets/reorderable_list.dart`) — not yet in `src/Plumix`. |
| `SelectableText` | `selectable_text.dart` | M | Builds on `EditableText` (already ported); needs selection gestures without editing. |
| `SelectionArea` | `selection_area.dart` | L | Requires core `SelectableRegion` (`widgets/selectable_region.dart`) — not yet in `src/Plumix`. |
| Text-selection toolbar family: `TextSelectionToolbar`, `AdaptiveTextSelectionToolbar`, desktop variants + `TextSelectionTheme` | `text_selection.dart`, `text_selection_toolbar.dart`, `adaptive_text_selection_toolbar.dart`, `desktop_text_selection*.dart`, `text_selection_theme.dart` | L | Copy/paste context menus for `TextField`/`SelectableText`. Coordinate scope split in the claim issue. |
| `Magnifier` (Material text magnifier) | `magnifier.dart` | M | Depends on selection overlay infra; claim after toolbar family lands. |
| Spell-check suggestions toolbar | `spell_check_suggestions_toolbar.dart` + layout delegate | M | Low priority until text-selection toolbars exist. |
| `AnimatedIcon` + `AnimatedIcons` catalog | `animated_icons.dart`, `animated_icons/` | M | Vector interpolation data; port a subset of the icon catalog first (e.g. `menu_arrow`). |
| `SearchDelegate` / `showSearch` | `search.dart` | M | Legacy full-screen search route; `SearchAnchor`/`SearchBar` already ported. |
| `Ink` (ink decoration) | `ink_decoration.dart` | S | `InkResponse`/`InkWell` already ported (`InkWell.cs`); this adds the decoration-on-Material widget. |
| `InkRipple`, `InkSparkle` splash factories | `ink_ripple.dart`, `ink_sparkle.dart` | M | Only the base `InkSplash` behavior exists today; needs pluggable `InteractiveInkFeatureFactory`. `InkSparkle` is shader-based — a paint-approximation divergence is acceptable if documented in [`ai/DIVERGENCES.md`](ai/DIVERGENCES.md). |

## Open infrastructure (align with maintainer first)

| Subsystem | Flutter source | Size | Notes |
| --- | --- | --- | --- |
| `MaterialApp` (+ core `WidgetsApp`) | `app.dart`, `widgets/app.dart` | L | Today samples compose `Theme` + `ScaffoldMessenger` + navigator manually. Open a design issue before starting. |
| `ColorScheme` + `Typography` | `color_scheme.dart`, `typography.dart`, `text_theme.dart` | L | `ThemeData` currently exposes flat color/text fields; migrating to token-based `ColorScheme`/`Typography` touches every ported control. Maintainer coordination required. |
| `PageTransitionsTheme` + Material page transitions | `page.dart`, `page_transitions_theme.dart`, `predictive_back_page_transitions_builder.dart` | L | Needs route transition hooks in the core navigator. |
| `ElevationOverlay` | `elevation_overlay.dart` | S | M2 overlay tinting utility; mostly used by already-ported surfaces. |

## Not listed / out of scope

- `debug.dart`, `constants.dart`, `shadows.dart`, `curves.dart`, `motion.dart`, `arc.dart`, `shaders/` — utility files ported piecemeal as controls need them; don't port standalone.
- Everything else in `lib/src/material` is already ported. If you find a gap this list misses (or a listed item that's actually done), a PR fixing **this file** is welcome too.
