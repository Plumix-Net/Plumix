# Cupertino Port — Up for Grabs

Widgets and subsystems from Flutter's Cupertino library (the `cupertino_ui` pub package,
`cupertino-ui-src/lib/src/`) that are **not yet ported, or only partially ported**, to
`src/Plumix.Cupertino`. This is the entry point for the current milestone (`docs/FRAMEWORK_PLAN.md`,
M6) and the first place `docs/ai/PORT_PLAYBOOK.md` Step 0 looks for a target.

Last verified against `cupertino_ui` 1.0.0 and `src/Plumix.Cupertino`: **2026-08-16** (by comparing
the public top-level types of every Dart file with the C# sources; the *Status* column is what that
comparison found — it is not a claim that a "partial" file is otherwise correct).

## How to claim and deliver

1. **Claim it**: open a GitHub issue titled `Claim: <Widget>` (or comment on an existing one) so two
   contributors don't port the same control.
2. **Read the workflow**: [`CONTRIBUTING.md`](../CONTRIBUTING.md) — contributions must be produced with a
   frontier coding agent. The agent must follow [`docs/ai/PORT_PLAYBOOK.md`](ai/PORT_PLAYBOOK.md):
   Dart source is the spec, controls are closed **end-to-end in one PR** (API, defaults, composition,
   states, layout, paint, tests, samples) — partial ports are sent back.
3. **Scope**: a port includes the widget, tests mapped in [`ai/TEST_MATRIX.md`](ai/TEST_MATRIX.md)
   (`src/Plumix.Tests/Cupertino<Control>Tests.cs`), and mirrored demo pages in
   `src/Sample/Plumix.Sample/Demos/Cupertino` + `dart_sample/lib/demos/cupertino` with a
   [`ai/PARITY_MATRIX.md`](ai/PARITY_MATRIX.md) row.
4. **Done**: when merged, delete (or downgrade) the row in this file in the same PR.

Size legend: **S** — one focused widget, few states (Dart file under ~400 lines). **M** — widget
family or nontrivial interaction/paint. **L** — over ~1500 lines or with core-framework
dependencies; the Dart source goes through `docs/ai/DART_SPEC_PROTOCOL.md`, and if the row says
"align first", open a design issue before starting.

## Cupertino-specific rules

- **Package direction** (`docs/ai/INVARIANTS.md`): `Plumix.Cupertino` depends only on `Plumix`.
  Never reference `Plumix.Material` from Cupertino code. When a Cupertino port needs a piece that
  currently lives in `Plumix.Material`, move it down into
  `Plumix.Cupertino` and make Material reference it — do not duplicate it.
- **Material `.Adaptive` factories**: Flutter's `Switch.adaptive`, `Slider.adaptive`,
  `RefreshIndicator.adaptive`, `CircularProgressIndicator.adaptive`, `Checkbox.adaptive`,
  `Radio.adaptive`, `AlertDialog.adaptive`, `showAdaptiveDialog` compose the Cupertino widget. Some
  Plumix Material files inline Cupertino behaviour instead because the Cupertino widget did not exist
  (`Switch.cs` carries private `Cupertino*` constants). When the Cupertino control lands, rewire the
  Material factory to compose it, exactly like Flutter, in the same PR.
- **Existing "partial" files** were written to support Material adaptive controls and toolbars, not
  as strict ports. Before extending one, diff it against the Dart file top to bottom; treat missing
  members as gaps to close, and keep the `// Dart parity source:` marker (drop its
  `(reference)`/`(adapted)` qualifier only when the file is a strict port).
- **Samples**: the gallery already has a Cupertino tab with five demos on both sides
  (`Checkbox`, `Radio`, `Switch`, `Theme + dynamic colors`, `Routes + modal popup`). Add new demos
  there and mirror them in `dart_sample`.

## Foundation (port these first — most controls below depend on them)

| Dart file (`cupertino_ui/lib/src/`) | Lines | Public types missing in C# | Status | Size | Notes / dependencies |
| --- | --- | --- | --- | --- | --- |
| `app.dart` | 794 | `CupertinoApp`, `CupertinoScrollBehavior` | open | M | Mirror `src/Plumix.Material/App.cs` (`WidgetsApp` composition, `Router` form). Depends on theme/localizations. |
| `icons.dart` | 9811 | `CupertinoIcons` | open | infra | Do not hand-port. Add `scripts/generate_cupertino_icons.py` producing `src/Plumix.Cupertino/CupertinoIcons.g.cs` (same pattern as `scripts/generate_material_colors.py`), then list it in `AGENTS.md` > Common Commands. |
| `global_cupertino_localizations.dart` + `l10n/` | 569 + arb | `GlobalCupertinoLocalizations` | open — align first | L | No `GlobalMaterialLocalizations` exists either; needs a shared localization-loading design (arb → C#) before either side ports. |

## Open controls

| Dart file (`cupertino_ui/lib/src/`) | Lines | Public types | Size | Notes / dependencies |
| --- | --- | --- | --- | --- |
| `cupertino_focus_halo.dart` | 139 | `CupertinoFocusHalo` | S | Used by buttons/list tiles for focus rings. |
| `bottom_tab_bar.dart` | 312 | `CupertinoTabBar` | S | Icon theme, `CupertinoLocalizations`. |
| `tab_view.dart` | 255 | `CupertinoTabView` | S | Nested `Navigator`; `CupertinoPageRoute` is available. |
| `tab_scaffold.dart` | 556 | `CupertinoTabScaffold`, `CupertinoTabController`, `RestorableCupertinoTabController` | M | After `bottom_tab_bar` + `tab_view`. |
| `expansion_tile.dart` | 265 | `CupertinoExpansionTile`, `ExpansionTileTransitionMode` | S | Core `ExpansionTile` primitives already exist for Material. |
| `list_tile.dart` | 419 | `CupertinoListTile`, `CupertinoListTileChevron` | S | |
| `list_section.dart` | 531 | `CupertinoListSection`, `CupertinoListSectionType` | M | After `list_tile`. |
| `form_row.dart`, `form_section.dart` | 160, 249 | `CupertinoFormRow`, `CupertinoFormSection` | S | After `list_section`. |
| `text_selection.dart`, `desktop_text_selection.dart` | 323, 216 | `CupertinoTextSelectionControls`, `CupertinoTextSelectionHandleControls`, `CupertinoDesktopTextSelectionControls` | S | Handles/toolbar glue over the already-ported toolbars; Material `TextField` selects them per platform. |
| `switch.dart` + `thumb_painter.dart` | 1443, 70 | `CupertinoSwitch`, `CupertinoThumbPainter` | M | Then rewire `Switch.Adaptive` in `src/Plumix.Material/Switch.cs` to compose it (remove the inlined `Cupertino*` constants). |
| `picker.dart` | 638 | `CupertinoPicker`, `CupertinoPickerDefaultSelectionOverlay` | M | `ListWheelScrollView`/`FixedExtentScrollController` are ported in core. |
| `refresh.dart` | 594 | `CupertinoSliverRefreshControl`, `RefreshIndicatorMode` | M | Then rewire `RefreshIndicator.Adaptive`. |
| `search_field.dart` | 603 | `CupertinoSearchTextField` | M | After `text_field`. |
| `segmented_control.dart` | 877 | `CupertinoSegmentedControl` | M | Custom `RenderBox`; compare with Material `SegmentedControlLayout.cs`. |
| `sheet.dart` | 1405 | `CupertinoSheetRoute`, `CupertinoSheetTransition`, `showCupertinoSheet` | M | Route primitives are available. |
| `sliding_segmented_control.dart` | 1539 | `CupertinoSlidingSegmentedControl` | L | Drag/thumb physics; spec via `dart-spec`. |
| `context_menu.dart` + `context_menu_action.dart` | 1576, 140 | `CupertinoContextMenu`, `CupertinoContextMenuAction` | L | Overlay + `Hero`-like flight; core `Hero.cs` is `(baseline subset)` — check what it needs first. |
| `text_field.dart` | 2039 | `CupertinoTextField`, `OverlayVisibilityMode` | L | Composes core `EditableText`; mirror `src/Plumix.Material/TextField.cs` structure. |
| `text_form_field_row.dart` | 412 | `CupertinoTextFormFieldRow` | S | After `text_field` + `form_row`. |
| `date_picker.dart` | 2974 | `CupertinoDatePicker`, `CupertinoDatePickerMode`, `CupertinoTimerPicker`, `CupertinoTimerPickerMode` | L | After `picker` + localizations date formatting. |
| `menu_anchor.dart` | 3056 | `CupertinoMenuAnchor`, `CupertinoMenuItem`, `CupertinoMenuDivider`, `CupertinoMenuEntry` | L | Core `RawMenuAnchor` exists (Material `MenuAnchor.cs`); check `docs/ai/DIVERGENCES.md` menu rows first. |
| `nav_bar.dart` | 3581 | `CupertinoNavigationBar`, `CupertinoSliverNavigationBar`, `CupertinoNavigationBarBackButton`, `NavigationBarBottomMode` | L | Hero transitions between bars; after `page_scaffold.dart`. |

## Partial ports to tighten (existing file, missing members or a qualified marker)

| Dart file | Lines (Dart / C#) | Gap found | Size |
| --- | --- | --- | --- |
| `activity_indicator.dart` | 311 / 366 | `CupertinoLinearActivityIndicator` missing; marker `(reference) (adapted)` | S |
| `button.dart` | 621 / 142 | `CupertinoButtonSize` missing; file is a fraction of the Dart source — re-port strictly | M |
| `checkbox.dart` | 676 / 785 | marker `(reference) (adapted)` — verify haptics/semantics/motion against Dart | S |
| `radio.dart` | 657 / 657 | marker `(reference) (adapted)` — same | S |
| `dialog.dart` | 2751 / 1210 | `CupertinoActionSheet`, `CupertinoActionSheetAction` missing | M |
| `slider.dart`, `scrollbar.dart`, `magnifier.dart`, `text_selection_toolbar*.dart`, `desktop_text_selection_toolbar*.dart`, `spell_check_suggestions_toolbar.dart`, `adaptive_text_selection_toolbar.dart` | — | No missing public types; not re-verified member-by-member in this pass — diff against Dart when you touch them | — |

## Not listed / out of scope

- `constants.dart`, `debug.dart` — utilities, ported piecemeal as controls need them; don't port
  standalone. (`interface_level.dart` shipped with the theme foundation, since
  `CupertinoDynamicColor.resolveFrom` reads it.)
- `migration_utility.dart` (`CupertinoUiCompatibilityBridge`) — pub-package migration shim, no C#
  counterpart needed.
- If you find a gap this list misses (or a listed item that's actually done), a PR fixing **this
  file** is welcome too.
