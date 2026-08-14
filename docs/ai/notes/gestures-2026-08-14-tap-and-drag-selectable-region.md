# Feature: tap-and-drag recognizers, default text-editing shortcuts, SelectableRegion overlay

## Goal

- Close the `SelectableRegion` gesture/shortcut/overlay divergence end-to-end: the region drives
  Flutter's real `TapAndPan`/`TapAndHorizontalDrag` recognizers, resolves its intents from the ambient
  `DefaultTextEditingShortcuts`, and shows a real `SelectionOverlay` (handles, toolbar, magnifier).

## Non-Goals

- `ProcessTextService`, the browser context-menu bridge, `Action.overridable`, and moving the context
  menu off its route onto an overlay entry. All four stay in `docs/ai/DIVERGENCES.md`.

## Context Plan

- Entry files:
  - `src/Plumix/Widgets/Selection.cs`
  - `src/Plumix/Gestures/GestureRecognizer.cs`
  - `src/Plumix/Widgets/Shortcuts.cs`
- Expansion trigger:
  - The divergence's close condition names three unported Dart files, so the primitives had to land
    before the control could be rewired.

## Delivery Scope

- Target control:
  - `SelectableRegion` (plus the `tap_and_drag.dart` and `default_text_editing_shortcuts.dart`
    primitives it depends on).
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Invariants touched:
  - Architecture boundaries: every primitive landed in `src/Plumix`, none in Material.
  - Versioning: the arena ordering fix, the `TapGestureRecognizer` button gate and three intent base
    classes are behavior changes, so `CHANGELOG.md` carries a `Breaking:` prefix.

## Dart Reference Mapping

- Flutter sources used as source of truth:
  - `flutter/packages/flutter/lib/src/gestures/tap_and_drag.dart` (+ `constants.dart`, `recognizer.dart`)
  - `flutter/packages/flutter/lib/src/widgets/default_text_editing_shortcuts.dart`
  - `flutter/packages/flutter/lib/src/widgets/selectable_region.dart`
  - `flutter/packages/flutter/lib/src/widgets/scrollable_helpers.dart`
  - `dart_sample/lib/demos/material/selection_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergences: one new row in `docs/ai/DIVERGENCES.md` for the gesture layer — Plumix's
  `PointerEvent` carries no per-event transform (so the delta transform is the identity), there is no
  `GestureArenaTeam` or arena hold/release, Dart's private `_TapStatusTrackerMixin` is folded into
  `BaseTapAndDragGestureRecognizer`, and `Timer.isActive`/`FakeAsync` become the swappable
  `GestureTimer`. The `SelectableRegion` row was rewritten down to what is still missing.

## Planned Changes

- `src/Plumix/Gestures/Constants.cs`, `Events.cs`, `GestureTimer.cs`, `TapAndDrag.cs`: new primitives.
- `src/Plumix/Gestures/GestureRecognizer.cs`: `OneSequenceGestureRecognizer`, `OffsetPair`, and the
  `debugOwner`/`allowedButtonsFilter`/`getKindForPointer`/`invokeCallback` surface.
- `src/Plumix/Gestures/GestureArena.cs`: reject losers before accepting the winner.
- `src/Plumix/Widgets/DefaultTextEditingShortcuts.cs`, `GestureRecognizerFactory.cs`: new widgets.
- `src/Plumix/Widgets/Selection.cs`: the rewired region.

## Test Plan

- Existing tests updated: `MaterialSelectionTests.cs`, `MaterialAboutTests.cs` (a pre-existing race
  in the license-loading assertion surfaced under the heavier suite and is now gated deterministically).
- New tests: `TapAndDragGestureTests.cs`, `DefaultTextEditingShortcutsTests.cs`.
- Parity-risk scenarios covered: consecutive-tap counting and its three reset rules, the
  past-hit-slop-but-not-drag-distance drag, eager vs deferred arena victory, the press deadline's
  consecutive-tap-only resolution, and all seven platform shortcut maps.

## Sample Parity Plan

- [x] C# sample impact checked (`SelectionDemoPage` description)
- [x] Dart sample parity checked (`selection_demo_page.dart`)
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [ ] `docs/FRAMEWORK_PLAN.md` — no milestone state changed
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] The control is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing (3135)
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps documented with close conditions in `docs/ai/DIVERGENCES.md`
