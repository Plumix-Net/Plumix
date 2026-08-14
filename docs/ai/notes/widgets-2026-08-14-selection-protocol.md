# Selection protocol port (2026-08-14)

## Scope

Replaced Plumix's bespoke text-selection stack with Flutter's real protocol:

- `rendering/selection.dart` → `src/Plumix/Rendering/Selection.cs`
- `widgets/selection_container.dart` → `src/Plumix/Widgets/SelectionContainer.cs`
- `widgets/selectable_region.dart` → `src/Plumix/Widgets/Selection.cs` +
  `src/Plumix/Widgets/SelectionContainerDelegates.cs`
- `_SelectableFragment` half of `rendering/paragraph.dart` → `src/Plumix/RenderParagraph.Selection.cs`
- Supporting primitives: `services/text_boundary.dart` + `services/text_layout_metrics.dart` →
  `src/Plumix/UI/TextBoundary.cs`; `widgets/text_editing_intents.dart` →
  `src/Plumix/Widgets/TextEditingIntents.cs`; text metrics on `RenderParagraph` →
  `src/Plumix/RenderParagraph.Text.cs`.

The old `ITextSelectionRegistrar`/`TextSelectionRegistrar` pair (which drove `RenderParagraph` through
`SetSelection(base, extent)` and owned a caret) is gone. Details and breaking changes: `CHANGELOG.md`.

## Why this iteration produced a note

It narrows one divergence row into two and introduces new ones — see `docs/ai/DIVERGENCES.md`, rows for
`Widgets/Selection.cs` + `Material/SelectionArea.cs` and for `RenderParagraph.Selection.cs` +
`RenderParagraph.Text.cs`.

## Concrete next steps, in dependency order

1. **`gestures/tap_and_drag.dart`** — `BaseTapAndDragGestureRecognizer`,
   `TapAndPanGestureRecognizer`, `TapAndHorizontalDragGestureRecognizer`, the `TapDrag*Details` family
   with `consecutiveTapCount`/`kind`, `eagerVictoryOnDrag` and `onTapTrackStart`/`onTapTrackReset`.
   Until it lands, `SelectableRegionState` counts consecutive taps itself (300 ms / 100 px) and fuses
   them with `RawGestureDetector`'s pan, which is the one place the port is an approximation rather than
   a 1:1 structure port.
1a. **Route-backed context menu** — `ContextMenuController` pushes a route, so showing the menu moves
   focus to its modal scope. `SelectableRegionState` therefore suppresses focus-loss clearing and
   re-entrant `HideToolbar` calls while the menu is showing; without that guard the focus handover inside
   `ModalRoute.DidPush` removed the route before `TransitionRoute.DidPush` reached its controller. Moving
   the menu to an `OverlayEntry` (as Flutter does) removes the need for the guard.
2. **`widgets/tap_region.dart`** — needed for Flutter's `TapRegion(groupId: SelectableRegion,
   onTapOutside: …)` wrapper; today focus loss is the only dismissal path.
3. **`SelectionOverlay` in `SelectableRegionState`** — the handle `LayerLink`s are pushed to the right
   selectable already (`_updateHandleLayersAndOwners` is fully ported), but no overlay is created, so
   generic selection has no drag handles, magnifier or `_handleSelectionStartHandleDragStart` path.
   `SelectionOverlay` itself already exists and is used by `TextSelectionOverlay`.
4. **`widgets/default_text_editing_shortcuts.dart`** — replace the region-local `Shortcuts` map and
   promote the `CallbackAction`s to Flutter's overridable `_NonOverrideAction` shape.
5. **Text backend metrics** — real `BoxHeightStyle`/`BoxWidthStyle` and an ICU word iterator, then drop
   the monospaced estimation fallbacks in `RenderParagraph.Text.cs` (they exist because hosts without a
   font manager, including the test harness, leave `TextLayout` null).
6. **Placeholder-aware paragraph granularity** — port `_isPlaceholder`, `_getOriginParagraph`,
   `_getParagraphContainingPosition` and the four
   `_updateSelection{Start,End}EdgeAtPlaceholderByMultiSelectableTextBoundary` methods so a triple-click
   drag that starts inside a `WidgetSpan`'s nested paragraph absorbs the enclosing paragraph.
7. **`SelectionListener`/`SelectionListenerNotifier`/`SelectionDetails`** — the observer surface of
   `selectable_region.dart`; not ported, and nothing depends on it yet.

## Verified

`dotnet build src/Plumix.Ci.slnf -c Debug`, `dotnet test src/Plumix.Tests/Plumix.Tests.csproj`
(3095 passed), `scripts/check_line_length.sh`, `python3 scripts/generate_port_map.py`.
