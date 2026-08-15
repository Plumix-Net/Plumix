# Feature: Menus token/theme closeout

## Goal

- Close the menus family in the `ColorScheme + Typography` closeout: replace the collapsed
  `MenuStyleDefaults` helper with Flutter's real `_MenuBarDefaultsM3`/`_MenuDefaultsM3` `MenuStyle`
  subclasses reading `theme.colorScheme`, and port the `MenuStyle`/`MenuThemeData`/`MenuBarThemeData`/
  `MenuButtonThemeData` surface those subclasses require.

## Non-Goals

- The `_MenuLayout` positioning algorithm, the anchor/overlay tree and the accelerator stack (already
  ported and untouched here).
- `debugFillProperties` on any of the four value classes.
- `MenuItemButton.styleFrom` / `SubmenuButton.styleFrom` (thin `TextButton.styleFrom` forwarders).

## Context Plan

- `menu_anchor.dart` is 4314 lines, so the default tables and the resolution chains came through
  `docs/ai/DART_SPEC_PROTOCOL.md` (dart-spec subagent) and the source never entered the working
  context. `menu_style.dart` (417), `menu_theme.dart` (142), `menu_bar_theme.dart` (106) and
  `menu_button_theme.dart` (141) are under the threshold and were read directly.
- Entry files: `src/Plumix.Material/MenuAnchor.cs`, `MenuTheme.cs`, `MenuThemes.cs`,
  `DropdownMenuTheme.cs`, `ThemeDataLerp.cs`.

## Delivery Scope

- Completion checklist:
  - [x] `MenuStyle` as a subclassable class in its own file: 13 virtual fields in Dart's declaration
        order, `Shape` retyped to `OutlinedBorder?`, `CopyWith`/`Merge`/`Lerp`, `runtimeType` equality
  - [x] `MenuBarDefaultsM3`/`MenuDefaultsM3` as `MenuStyle` subclasses reading `ColorScheme`
  - [x] `_MenuButtonDefaultsM3`'s four-branch `foregroundColor`/`iconColor` resolvers and the shared
        `WidgetStateMouseCursor.AdaptiveClickable`
  - [x] `MenuBarThemeData : MenuThemeData`; all three theme datas as classes with `Lerp` and Dart's
        equality; all three `*Theme`s as `InheritedTheme`s with `Wrap`
  - [x] Panel composition: `MaterialType.Canvas`, unconditional side-into-shape fold, fixed size
        clamped by the min/max window
  - [x] Focused tests, mirrored samples

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed — `EdgeInsetsGeometry.DirectionalSymmetric` and
      `WidgetStatePropertyAll` equality land in core (`Plumix`); `WidgetStateMouseCursor.AdaptiveClickable`
      stays in `Plumix.Material` beside the existing type. Direction is unchanged.
- [x] `docs/ai/PORTING_MODE.md` reviewed.

## Dart Reference Mapping

- `material_ui/lib/src/menu_style.dart`, `menu_theme.dart`, `menu_bar_theme.dart`,
  `menu_button_theme.dart`, and `menu_anchor.dart`'s generated token block
  (`_MenuBarDefaultsM3`, `_MenuButtonDefaultsM3`, `_MenuDefaultsM3`, `_scaledPadding`) plus the
  `_MenuPanel`/`_Submenu`/`_SubmenuButtonState` resolution chains.
- `material_ui/lib/src/theme_data.dart` (`menuTheme`/`menuBarTheme`/`menuButtonTheme` storage and lerp).
- The divergence introduced is registered in `docs/ai/DIVERGENCES.md`. Do not restate it here.

## Test Plan

- `src/Plumix.Tests/MaterialMenuAnchorTests.cs` — `MenuStyle` copy/merge semantics, `runtimeType`
  equality, lerp special cases and the discrete cursor/density switches, the three theme datas'
  defaults and lerp special cases, and `MenuBarThemeData`'s inherited-but-unequal contract.
- `src/Plumix.Tests/MaterialDropdownTests.cs` — the M3 token table for both orientations, the
  directional padding/alignment split, the side-into-shape fold, the clamped fixed size and
  `InheritedTheme.Wrap` on all three menu themes.

## Sample Parity Plan

- [x] C# probe added (`DropdownDemoPage`, "MenuStyle surface tokens")
- [x] Dart probe mirrored (`dropdown_demo_page.dart`)
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/MATERIAL_TODO.md` closeout row narrowed to the remaining families
- [x] `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md`, `docs/ai/DIVERGENCES.md` updated

## Done Criteria

- [x] Control family closed end-to-end; four gates green
- [x] Divergence registered in the same iteration
- [x] Remaining `ColorScheme` closeout families named in `docs/MATERIAL_TODO.md`
