# Feature: Text selection handle overlay

## Goal

- Port Flutter's draggable selection-handle overlay (`widgets/text_selection.dart` +
  `material/text_selection.dart`) so any editing surface can show source-exact Material handles.

## Non-Goals

- `TextSelectionGestureDetectorBuilder` (translates raw text-field gestures; needs `RenderEditable`).
- Spell-check-service integration (`SpellCheckService`/`DefaultSpellCheckService`), a separate subsystem.

## Context Plan

- Entry files:
  - `flutter-src/packages/flutter/lib/src/widgets/text_selection.dart` (4019 lines, read through the
    `dart-spec` subagent per `docs/ai/DART_SPEC_PROTOCOL.md`)
  - `flutter-src/packages/flutter/lib/src/material/text_selection.dart`
  - `src/Plumix/Widgets/TextInput.cs`, `src/Plumix/Widgets/Magnifier.cs`, `src/Plumix/Widgets/Overlay.cs`
- Expansion trigger:
  - Binding handles to real text endpoints requires entering `rendering/editable.dart`, which is the
    blocker below.

## Delivery Scope

- Target feature:
  - `TextSelectionHandleType`, `TextSelectionPoint`, `TextSelectionControls`,
    `EmptyTextSelectionControls`, `ITextSelectionHandleControls`, `ITextSelectionDelegate`,
    `ClipboardStatus`/`ClipboardStatusNotifier`, `SelectionOverlay`, and the Material controls.
- Completion checklist:
  - [x] Primitives (`PanGestureRecognizer`, `DeviceGestureSettings`, richer drag details,
        `RawGestureDetector` pan callbacks, `WidgetConstants.MinInteractiveDimension`)
  - [x] Core `SelectionOverlay` composition, states, layout, lifecycle
  - [x] Material handles, painter, anchors, legacy toolbar
  - [x] Focused tests
  - [x] Mirrored samples
  - [ ] `TextSelectionOverlay` and automatic in-field magnifier — blocked, see below

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Package direction stays `Plumix.Material -> Plumix`; the handle painter and controls live in Material,
  the overlay and the control contract live in core.

## Dart Reference Mapping

- Flutter/Dart source files used as source of truth:
  - `packages/flutter/lib/src/widgets/text_selection.dart`
  - `packages/flutter/lib/src/material/text_selection.dart`
  - `packages/flutter/lib/src/rendering/selection.dart` (`TextSelectionHandleType`)
  - `packages/flutter/lib/src/widgets/constants.dart` (`kMinInteractiveDimension`)
- Parity mapping checklist:
  - [x] Controls API, defaults, anchors, sizes, rotations
  - [x] Overlay composition, fades, drag state machine, magnifier flow
  - [x] Material legacy toolbar anchors, item order, clipboard gating
  - [ ] `TextSelectionOverlay` endpoint/line-height/viewport wiring

## Blocker

`TextSelectionOverlay` is defined entirely in terms of `RenderEditable`. This repository has no such
render object: `EditableText` (marked `(adapted)`) composes a plain `Text` widget and hit-tests through a
throwaway `TextLayout` built from `FocusNode.ResolveTraversalRect()`. The overlay needs
`getEndpointsForSelection`, `getRectForComposingRange`, `getLineAtOffset`, `getLocalRectForCaret`,
`preferredLineHeight`, `plainText`, `lastSecondaryTapDownPosition`, and the
`selectionStartInViewport`/`selectionEndInViewport` notifiers, plus leader layers painted at the
endpoints. None of that can be approximated without silently diverging.

Concrete next step: port `flutter/packages/flutter/lib/src/rendering/editable.dart` into
`src/Plumix/Rendering/Editable.cs` (spec it through `dart-spec`; it is ~3000 lines), rebuild
`EditableText` on it, then port `TextSelectionOverlay` and `TextSelectionGestureDetectorBuilder` and wire
`TextField`/`SelectableText` to them. The automatic touch magnifier falls out of that same wiring, since
`SelectionOverlay.ShowMagnifier`/`UpdateMagnifier`/`HideMagnifier` already exist here.

## Test Plan

- New tests:
  - `src/Plumix.Tests/SelectionOverlayTests.cs`
  - `src/Plumix.Tests/MaterialTextSelectionControlsTests.cs`
- Covered risks:
  - handle/toolbar/magnifier lifecycle, collapsed-handle visibility rules, live rebuilds on
    type/line-height change, touch-versus-pointer drag gating, Android endpoint haptics, interactive
    geometry and follower offsets, Material anchors/sizes/rotation/colour, single-path handle painting,
    `canSelectAll` rules, and legacy toolbar anchors/order/clipboard gating.

## Sample Parity Plan

- [x] C# probe added (`SelectionHandlesDemoPage`)
- [x] Dart sample probe mirrored
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/MATERIAL_TODO.md` row narrowed
- [x] `docs/ai/TEST_MATRIX.md` updated
- [x] `docs/ai/DIVERGENCES.md` updated (three rows)

## Done Criteria

- [x] `SelectionOverlay` and the Material controls are closed and tested
- [x] Divergences recorded
- [ ] Text fields show draggable handles — blocked on `RenderEditable`
