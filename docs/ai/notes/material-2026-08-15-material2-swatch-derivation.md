# Feature: Material 2 swatch color derivation

Closed 2026-08-15. Note exists because the iteration introduces a divergence
(`ColorSwatch<T>` cannot extend `Color`); the port itself is closed end-to-end.

## Goal

- `ThemeData(useMaterial3: false)` derives its scheme the way Flutter does, from a `MaterialColor`
  swatch, so every Material 2 default that reads the scheme matches Flutter.

## Non-Goals

- `ThemeData.secondaryHeaderColor`, `indicatorColor`, `dialogBackgroundColor` and
  `ButtonThemeData.buttonColor` defaults: Flutter derives them from the swatch too, but Plumix has
  no `SecondaryHeaderColor`/`IndicatorColor`/`DialogBackgroundColor` at all and never reads
  `ButtonColor`, so there is nothing to diverge from yet. Port them together with their consumers.

## Delivery Scope

- Target: `colors.dart` + `ColorScheme.fromSwatch` + the `ThemeData` `primarySwatch` path.
- Closed: API/defaults, composition, states (N/A), layout (N/A), paint (palette values), tests,
  both samples, docs.

## Dart Reference Mapping

- `material_ui/lib/src/colors.dart` (2075 lines — API read directly, palette parsed by
  `scripts/generate_material_colors.py`)
- `flutter/packages/flutter/lib/src/painting/colors.dart` (`ColorSwatch<T>`)
- `material_ui/lib/src/color_scheme.dart` (`fromSwatch` only)
- `material_ui/lib/src/theme_data.dart` (`primarySwatch` derivation order only)
- Spec extracted through `docs/ai/DART_SPEC_PROTOCOL.md`; Flutter's own tests
  (`material_ui/test/colors_test.dart`, `flutter/test/painting/colors_test.dart`,
  `theme_data_test.dart`) drive `src/Plumix.Tests/MaterialColorsTests.cs`. Flutter ships no
  `fromSwatch` unit test, so its roles are asserted against the factory body.

## Divergence

One row in `docs/ai/DIVERGENCES.md`: Avalonia's `Color` is a sealed struct, so `ColorSwatch<T>` is a
class with an implicit conversion instead of a `Color` subclass, and a swatch stored in a `Color`
slot decays to its primary colour. The only Dart behaviours that depend on the swatch surviving in a
`Color` slot are `indicatorColor`/`secondaryHeaderColor`, neither of which is ported (see
Non-Goals). Also generated-vs-hand-written palette, content-based swatch hashing, and the
`Plumix.Material.Colors` / `Avalonia.Media.Colors` name collision.

## Follow-ups

- If `ThemeData.IndicatorColor` is ever ported, its M2 default depends on
  `colorScheme.secondary == primaryColor` being **false** for the default light theme (Dart compares
  a plain `Color` against a `MaterialColor` and the runtime types differ). A C# port must reproduce
  that outcome explicitly rather than comparing the two colours.
