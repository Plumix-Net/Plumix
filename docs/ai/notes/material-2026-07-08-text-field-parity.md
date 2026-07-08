# Feature: InputDecorator + TextField parity

## Goal

- Port Flutter's connected Material input decoration and editable text-field controls over the framework-owned focus/IME/editing pipeline.

## Delivery Scope

- Target controls: `InputDecorator` and `TextField`.
- Completed: constructor contracts/defaults, M2/M3 underline/outline/filled states, local/global theme precedence, floating labels, hint/helper/error/counter and prefix/suffix/icon slots, focus/hover/disabled/error states, real input/read-only/obscured/multiline/submit/max-length behavior, semantics, focused tests, and paired samples.

## Dart Reference Mapping

- `flutter/packages/flutter/lib/src/material/input_border.dart`
- `flutter/packages/flutter/lib/src/material/input_decorator.dart`
- `flutter/packages/flutter/lib/src/material/text_field.dart`
- `dart_sample/lib/demos/material/text_field_demo_page.dart`

## Invariants and Expansion

- `INVARIANTS.md` and `PORTING_MODE.md` reviewed.
- Source-required changes stay split correctly: editing/IME additions in core `Widgets/TextInput.cs`; Material appearance/composition in `Plumix.Material`.
- Both C# and Dart samples expose the same route and state probes.

## Divergence

- `docs/ai/DIVERGENCES.md` records missing selection overlays/context menus, formatter/autofill/spellcheck/restoration services, directional/per-corner geometry, and Flutter's private baseline-driven decorator renderer.
- Close by adding those shared services/primitives, then replacing Row/Stack decoration composition with the direct `_RenderDecoration` port.

## Validation

- `MaterialTextFieldTests.cs` covers contracts, slots, state/theme borders, input, submit, read-only/obscure, grapheme limits, counters and semantics.
- Full `Plumix.Tests`, C#/Dart sample builds, Dart analyzer, and desktop startup are required before closeout.
