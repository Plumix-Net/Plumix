# Feature: menu shortcut labels, MouseRegion.onHover, directional button alignment

Closes the `src/Plumix.Material/MenuAnchor.cs` divergence row. Written because the iteration
introduces one new divergence row (`ShortcutSerialization`).

## Goal

- `MenuItemButton`/`CheckboxMenuButton`/`RadioMenuButton` accept a display-only `shortcut` and render
  its localized label in the source slot, with the source per-platform modifier order and separator.
- Menu buttons take hover focus from `MouseRegion.onHover` rather than `onEnter`/`TextButton.onHover`.
- `ButtonStyle.Alignment` is an `AlignmentGeometry`, so `_MenuButtonDefaultsM3`'s
  `AlignmentDirectional.centerStart` mirrors under RTL.

## Non-Goals

- `PlatformMenuBar`/`PlatformMenuItem` themselves — only `ShortcutSerialization` was needed.
- A numeric `LogicalKeyboardKey` type; keys stay normalized strings (keyboard-events divergence).
- Registering the shortcut as an actual key binding: Flutter's `shortcut` is display-only.

## Context Plan

- Entry files:
  - `src/Plumix/Widgets/Shortcuts.cs`
  - `src/Plumix/Widgets/MouseCursor.cs`
  - `src/Plumix.Material/MenuAnchor.cs`
- Expansion trigger: `ButtonStyle`/`Buttons.cs` had to widen before the M3 default table could carry
  a directional alignment.

## Delivery Scope

- Target control: `MenuItemButton` family (`_MenuItemLabel` shortcut slot + hover).
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
  - Core never references Material: the labeler stays in `Plumix.Material`, while
    `IMenuSerializableShortcut`/`ShortcutSerialization` live in core beside the activators.
  - Versioning: `ButtonStyle.Alignment` widening is a breaking public-API change, flagged in
    `CHANGELOG.md`.

## Dart Reference Mapping

- Source of truth:
  - `flutter/packages/flutter/lib/src/widgets/platform_menu_bar.dart` (`ShortcutSerialization`)
  - `flutter/packages/flutter/lib/src/widgets/shortcuts.dart` (`MenuSerializableShortcut`)
  - `flutter/packages/flutter/lib/src/widgets/basic.dart` (`MouseRegion.onHover`)
  - `flutter/packages/flutter/lib/src/material/menu_anchor.dart`
    (`_LocalizedShortcutLabeler`, `_MenuItemLabel`, `_MenuButtonDefaultsM3`)
  - `flutter/packages/flutter/lib/src/material/material_localizations.dart` (`keyboardKey*`)
  - `dart_sample/lib/demos/material/dropdown_demo_page.dart`
- Parity mapping checklist: all five boxes mapped.
- Divergences: one row added to `docs/ai/DIVERGENCES.md` for
  `src/Plumix/Widgets/PlatformMenuBar.cs` — Dart's `ShortcutSerialization.character` constructor is
  `ForCharacter` (C# forbids a static factory named like the `Character` property), and `Trigger` /
  the `shortcutTrigger` channel entry carry a normalized key string instead of
  `LogicalKeyboardKey.keyId`. Close condition is shared with the keyboard-events row: add a
  `LogicalKeyboardKey` value type carrying `keyId`/`valueMask`/`planeMask`.

## Planned Changes

- `src/Plumix/Widgets/PlatformMenuBar.cs`: new — `IMenuSerializableShortcut`, `ShortcutSerialization`.
- `src/Plumix/Widgets/Shortcuts.cs`: both activators implement `SerializeForMenu`.
- `src/Plumix/Widgets/MouseCursor.cs`: `MouseRegion.OnHover` forwarded to `Listener.OnPointerHover`.
- `src/Plumix.Material/MaterialLocalizations.cs`: the 47 `KeyboardKey*` strings.
- `src/Plumix.Material/MenuAnchor.cs`: `LocalizedShortcutLabeler`, the `_MenuItemLabel` shortcut slot,
  `Shortcut` on the three item widgets, hover rework, directional default alignment.
- `src/Plumix.Material/ButtonStyle.cs`, `Buttons.cs`, `IconButton.cs`, `SegmentedButton.cs`:
  `AlignmentGeometry` widening, resolved against `Directionality` at the single `Align` site.

## Test Plan

- Updated: `src/Plumix.Tests/MaterialDropdownTests.cs`, `MaterialMenuAnchorTests.cs`.
- Added: `src/Plumix.Tests/MouseRegionTests.cs`.
- Parity-risk scenarios covered: per-platform modifier order/separator/symbols, the graphic →
  localized → Unicode → key-label trigger fallback chain, character activators keeping their case and
  dropping shift, the channel bit masks, shortcut slot ordering and padding, semantics exclusion,
  hover edge detection, and LTR/RTL content alignment.

## Sample Parity Plan

- [x] C# sample impact checked (`src/Sample/Plumix.Sample/Demos/Material/DropdownDemoPage.cs`)
- [x] Dart sample parity checked (`dart_sample/lib/demos/material/dropdown_demo_page.dart`)
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [ ] `docs/FRAMEWORK_PLAN.md` — no milestone status change (M4 still `in_progress`)
- [x] `docs/ai/TEST_MATRIX.md` updated (new mouse-region row + menu-anchor row extended)

## Done Criteria

- [x] Feature closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining gap documented: the key-id serialization, shared with the keyboard-events row

## Implementation Notes

- `_handlePointerHover` requests focus without testing `enabled` in Dart, because a disabled button
  builds no `Focus` and its unregistered node cannot take primary focus. Plumix's `FocusNode` can take
  focus while unattached, so `MenuItemButtonState.HandlePointerHover` excludes the disabled case
  explicitly. Observable behavior matches; no divergence row, but the core `FocusNode` difference is
  worth revisiting if unattached-node focus is ever tightened.
