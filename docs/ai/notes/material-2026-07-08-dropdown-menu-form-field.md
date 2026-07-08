# Feature: DropdownMenu + DropdownMenuFormField

## Goal

- Port Flutter's modern Material dropdown pair end-to-end for direct Dart-to-C# rewrites.

## Delivery Scope

- Target controls: `DropdownMenu<T>` and `DropdownMenuFormField<T>`.
- [x] API/default values
- [x] Widget composition order
- [x] State transitions/interaction states
- [x] Constraint/layout behavior
- [x] Paint/visual semantics
- [x] Focused tests

## Non-Goals

- Building the still-missing general cascading menu, restoration, and platform text-input configuration subsystems inside this control iteration.

## Context Plan

- Entry files: Flutter `dropdown_menu.dart`, `dropdown_menu_form_field.dart`, `dropdown_menu_theme.dart`, and Plumix `Dropdown.cs`, `TextField.cs`, `Form.cs`, `MaterialDropdownTests.cs`.
- Expansion trigger: add only source-required editable key interception and reusable route capabilities needed to close both controls.

## Invariants

- Reviewed `docs/ai/INVARIANTS.md` and `docs/ai/PORTING_MODE.md`; behavior remains in `Plumix`/`Plumix.Material` with `Widget -> Element -> RenderObject` ownership and no host-control logic.

## Dart Reference Mapping

- Sources: Flutter `dropdown_menu.dart`, `dropdown_menu_form_field.dart`, `dropdown_menu_theme.dart`, `menu_anchor.dart`, and the mirrored dropdown sample.
- Remaining divergence: the repository lacks cascading `MenuAnchor`, restoration, text-input configuration/formatters, and the complete directional/outlined style hierarchy. The active row in `docs/ai/DIVERGENCES.md` records observable deltas and the close condition.

## Planned Changes

- Add the modern entry/controller/theme/menu/form-field surfaces, extend the existing framework-owned route, add focused tests, and mirror the demo in C# and Dart.

## Validation

- Focused coverage: `src/Plumix.Tests/MaterialDropdownTests.cs`.
- Sample parity: `src/Sample/Plumix.Sample/Demos/Material/DropdownDemoPage.cs` and `dart_sample/lib/demos/material/dropdown_demo_page.dart`.

## Docs and Done Criteria

- [x] `CHANGELOG.md`, framework plan, module/test/parity matrices, and divergences updated
- [x] Behavior implemented and focused/full tests passing
- [x] C#/Dart sample structure synchronized
- [x] Remaining shared blockers documented with close conditions
