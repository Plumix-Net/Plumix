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
- **Samples**: add new demos to the gallery's Cupertino tab and mirror them in `dart_sample`.

## Foundation (port these first — most controls below depend on them)

| Dart file (`cupertino_ui/lib/src/`) | Lines | Public types missing in C# | Status | Size | Notes / dependencies |
| --- | --- | --- | --- | --- | --- |
| `global_cupertino_localizations.dart` + `l10n/` | 569 + arb | `GlobalCupertinoLocalizations` | open — align first | L | No `GlobalMaterialLocalizations` exists either; needs a shared localization-loading design (arb → C#) before either side ports. |

## Open controls

None — every `cupertino_ui/lib/src/` control has at least a first-pass port; what remains is the
*Foundation* row above and the *Partial ports to tighten* table below.

## Partial ports to tighten (existing file, missing members or a qualified marker)

| Dart file | Lines (Dart / C#) | Gap found | Size |
| --- | --- | --- | --- |
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
