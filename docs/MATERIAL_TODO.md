# Material Port — Up for Grabs

Widgets and subsystems from Flutter's Material library (the `material_ui` pub package, `material-ui-src/lib/src/`) that are **not yet ported** to `src/Plumix.Material`. Pick one, claim it, and submit a PR.

Last verified against Flutter source and `src/Plumix.Material`: **2026-08-15**.

## How to claim and deliver

1. **Claim it**: open a GitHub issue titled `Claim: <Widget>` (or comment on an existing one) so two contributors don't port the same control.
2. **Read the workflow**: [`CONTRIBUTING.md`](../CONTRIBUTING.md) — contributions must be produced with a frontier coding agent (Claude Opus 4.8 / GPT-5.5 or newer). The agent must follow [`docs/ai/PORTING_MODE.md`](ai/PORTING_MODE.md): Dart source is the spec, controls are closed **end-to-end in one PR** (API, defaults, composition, states, layout, paint, theme, tests) — partial ports are sent back.
3. **Scope**: a port includes the widget, its `*Theme`/`*ThemeData` pair (when Flutter has one), tests mapped in [`ai/TEST_MATRIX.md`](ai/TEST_MATRIX.md), and mirrored demo probes in `src/Sample/Plumix.Sample` + `dart_sample` with a [`ai/PARITY_MATRIX.md`](ai/PARITY_MATRIX.md) entry.
4. **Done**: when merged, delete the row from this file in the same PR.

Size legend: **S** — one focused widget, few states. **M** — widget family or nontrivial interaction/paint. **L** — subsystem with core-framework dependencies; open a design issue and align with the maintainer before starting.

## Open controls

| Widget / family | Flutter source (`lib/src/material/`) | Size | Notes / dependencies |
| --- | --- | --- | --- |

## Open infrastructure (align with maintainer first)

| Subsystem | Flutter source | Size | Notes |
| --- | --- | --- | --- |
| `ColorScheme` + `Typography` closeout | `color_scheme.dart`, `typography.dart`, `text_theme.dart`, component defaults | L | The Material 3 role model, HCT seed generation, 2014/2018/2021 type scales, exact platform themes, locale-script geometry, and `ThemeData` projection now exist; navigation surfaces, standard `AppBar`, bottom/FAB surfaces, `BottomSheet`, progress indicators, `MaterialBanner`, `Scrollbar`, `ReorderableListView`, `Slider`/`RangeSlider`, `IconButton`, action buttons, `ButtonBar`, `Tooltip`, `ExpandIcon`, `ExpansionTile`, `ListTile`, `PopupMenu`, `DataTable`, `Card`, `Divider`, `Badge`, `RefreshIndicator`, `CircleAvatar`, `DrawerHeader`, `Checkbox`, `Switch`, `Radio`/`RadioListTile`, `ToggleButtons`, `SegmentedButton`, `FlexibleSpaceBar`, `Autocomplete`, Chips, `DatePicker`/range pickers, `TimePicker`, the Tabs family (`TabBar`/`TabBar.secondary`/`TabBarView`/`TabPageSelector`/`TabController`/`UnderlineTabIndicator`), `SearchBar`/`SearchAnchor`, `InputDecorator`/`InputDecoration`/`InputDecorationTheme`, and the `TextButton`/`ElevatedButton`/`OutlinedButton`/`FilledButton` families have direct-token or strict-default/theme closeouts. The menus family (`MenuAnchor`/`MenuBar`/`SubmenuButton`/`MenuItemButton` with `MenuStyle` and the `MenuTheme`/`MenuBarTheme`/`MenuButtonTheme` trio) is closed too, as are the `DropdownMenu` family (`DropdownMenu`/`DropdownMenuEntry`/`DropdownMenuTheme`/`DropdownMenuFormField`) and the `CarouselView` family (`CarouselView`/`CarouselController`/`CarouselScrollPhysics`/`CarouselViewTheme`). Remaining work is the other component families (legacy `DropdownButton`, `SnackBar`, `TextSelectionTheme`). See `docs/ai/notes/material-2026-07-29-color-scheme-typography-foundation.md`. |

## Not listed / out of scope

- `debug.dart`, `constants.dart`, `shadows.dart`, `curves.dart`, `motion.dart`, `arc.dart`, `shaders/` — utility files ported piecemeal as controls need them; don't port standalone.
- Everything else in `lib/src/material` is already ported. If you find a gap this list misses (or a listed item that's actually done), a PR fixing **this file** is welcome too.
