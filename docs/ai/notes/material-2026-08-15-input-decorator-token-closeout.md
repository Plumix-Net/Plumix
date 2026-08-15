# Feature: InputDecorator token/theme closeout

## Goal

- Close the last `ColorScheme + Typography` component family: replace the collapsed
  `InputDecoratorDefaults` record with Flutter's real `_InputDecoratorDefaultsM2`/`_InputDecoratorDefaultsM3`
  `InputDecorationThemeData` subclasses, and port the theme surface those subclasses require.

## Non-Goals

- The `_RenderDecoration` layout algorithm (already ported and untouched here).
- Deprecated Dart members (`maintainHintHeight`, `collapsed`'s deprecated floating-label passthrough,
  `copyWith`'s unused `semanticsService`) and `debugFillProperties`.

## Context Plan

- Dart source is 6174 lines, so the spec came through `docs/ai/DART_SPEC_PROTOCOL.md` (dart-spec subagent)
  and the source never entered the working context.
- Entry files: `src/Plumix.Material/InputDecorator.cs`, `InputDecoratorTheme.cs`, `ButtonStyle.cs`,
  `src/Plumix/Widgets/RawRadio.cs`.

## Delivery Scope

- Completion checklist:
  - [x] `InputDecorationThemeData` as a subclassable class: 37 fields, six non-nullable with source
        defaults, `CopyWith`/`Merge` (31 fields), value equality with Dart's `runtimeType` guard
  - [x] `InputDecorationTheme` as an `InheritedTheme` with the obsolete field-based constructor,
        forwarding getters, `Data`, `CopyWith`, `Merge`, `Wrap`, `Of`
  - [x] `InputDecoratorDefaultsM2`/`InputDecoratorDefaultsM3` verbatim tables
  - [x] `ApplyDefaults` overload taking the theme widget
  - [x] Resolution chain rewritten (`WidgetStateProperty` resolution per slot, `IconButtonTheme`
        affix precedence, `baseStyle` in the floating-label chain)
  - [x] Focused tests, mirrored samples

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed — `WidgetStateTextStyle` and `WidgetState.Error` land in core
      (`Plumix`), the `MaterialState` bridge stays in `Plumix.Material`; direction is unchanged.
- [x] `docs/ai/PORTING_MODE.md` reviewed.

## Dart Reference Mapping

- `material_ui/lib/src/input_decorator.dart` (`InputDecorationThemeData`, `InputDecorationTheme`,
  `InputDecoration`, `_InputDecoratorDefaultsM2`, `_InputDecoratorDefaultsM3`, `_InputDecoratorState`).
- `material_ui/lib/src/theme_data.dart` (`inputDecorationTheme` storage; `ThemeData.lerp` keeps Dart's
  discrete `t < 0.5` switch, which Plumix already had).
- Divergences introduced are registered in `docs/ai/DIVERGENCES.md` (state-value subclassing;
  `applyDefaults(Object)` overloads and the unported deprecated surface). Do not restate them here.

## Test Plan

- `src/Plumix.Tests/MaterialInputDecoratorTests.cs` — 14 added tests covering the M2/M3 tables per
  state, theme copy/merge/equality, the obsolete theme surface, `ApplyDefaults` from the widget, the
  `IconButtonTheme` fallback, state-resolving theme values, and the floating-label `baseStyle` merge.

## Sample Parity Plan

- [x] C# probe added (`TextFieldDemoPage`)
- [x] Dart probe mirrored (`text_field_demo_page.dart`)
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated (rotated past 100 KB in the same change)
- [x] `docs/MATERIAL_TODO.md` closeout row narrowed to the remaining families
- [x] `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md`, `docs/ai/DIVERGENCES.md` updated

## Done Criteria

- [x] Control closed end-to-end; four gates green
- [x] Divergences registered in the same iteration
- [x] Remaining `ColorScheme` closeout families named in `docs/MATERIAL_TODO.md`
