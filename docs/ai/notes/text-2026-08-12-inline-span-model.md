# Feature: rich-text span model (`InlineSpan` / `RichText` / `Text.rich`)

## Goal

- One paragraph can carry many styles, per-span gesture recognizers and inline widgets, so the
  divergence rows that named "no `InlineSpan`/`TextSpan` tree" as their blocker can be closed.

## Non-Goals

- `_SelectableFragment` splitting, `StrutStyle`, `AttributedString`, `SemanticsTag`, and the
  `MediaQuery` accessibility overrides. Each is tracked as its own divergence row.

## Context Plan

- Entry files:
  - `src/Plumix/RenderParagraph.cs`
  - `src/Plumix/Widgets/Text.cs`
  - Flutter `painting/inline_span.dart`, `text_span.dart`, `placeholder_span.dart`,
    `widgets/widget_span.dart`
- Expansion trigger:
  - `rendering/paragraph.dart` (3625 lines) and `widgets/text.dart` (1546 lines) went through
    `docs/ai/DART_SPEC_PROTOCOL.md`; the four span files were read directly.

## Delivery Scope

- Target control:
  - `RichText` + `Text`/`Text.rich` + `RenderParagraph`.
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions (the `RenderComparison` setter switch)
  - [x] Constraint/layout behavior (inline placeholder children)
  - [x] Paint/visual semantics (glyphs then inline children; per-run semantics nodes)
  - [x] Focused tests (`src/Plumix.Tests/InlineSpanTests.cs`)

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Package direction is unchanged (`Plumix.Material -> Plumix`); the span model is core-only.
- Versioning: this is a `Breaking:` change even though it moves toward Flutter —
  `RenderParagraph.Text` changed type, `Text` became a `StatelessWidget`, and text scaling moved off
  `FontSize` onto `TextScaler`.

## Dart Reference Mapping

- Flutter/Dart source files used as source of truth:
  - `packages/flutter/lib/src/painting/inline_span.dart`, `text_span.dart`, `placeholder_span.dart`,
    `text_scaler.dart`
  - `packages/flutter/lib/src/widgets/widget_span.dart`, `text.dart`, `basic.dart` (`RichText`)
  - `packages/flutter/lib/src/rendering/paragraph.dart`
  - `dart_sample/lib/demos/general/rich_text_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergences: four rows added to `docs/ai/DIVERGENCES.md` (glyph-info hit testing and selection-box
  direction; placeholder semantics tags and `AttributedString`; the `Text`/`RichText` fields core
  cannot supply yet). Two rows were closed (`Tooltip.richMessage`, the `RichText` half of
  `MenuAccelerator`) and the selection row was narrowed.

## Follow-ups (in priority order)

1. Backend glyph-info API with grapheme-cluster bounds plus direction-carrying selection boxes — this
   is what keeps span hit testing and `AssembleSemanticsNode` from being exact.
2. `SemanticsTag` + `AttributedString`, which unblock tagged placeholder semantics nodes and the
   spell-out/locale attributes the span model already records.
3. `_SelectableFragment` splitting on `￼`, which is what the remaining selection row needs.
