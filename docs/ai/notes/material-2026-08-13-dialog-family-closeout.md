# Feature: Dialog family closeout (Material + Cupertino)

## Goal

- Close the `src/Plumix.Material/Dialog.cs` divergence end-to-end: strict `material_ui/dialog.dart` +
  `dialog_theme.dart` parity, the adaptive Apple path, and the Cupertino dialog family it depends on.

## Delivery Scope

- Shipped in one iteration; see `CHANGELOG.md` (top entry) for the full delta. Highlights:
  - Core: `TraversalEdgeBehavior` + `FocusScopeNode`/`ModalScope` wiring, route `RequestFocus`,
    `RawDialogRoute<T>`/`ShowGeneralDialog`, `TransitionRoute.CreateSimulation`, reversed
    `AnimationController.AnimateWith`, `RenderStack` intrinsics.
  - Material: `Dialog`/`AlertDialog(.Adaptive)`/`SimpleDialog` re-port on `Material`,
    `DialogRoute<T>` on `RawDialogRoute`, `ShowDialog`/`ShowAdaptiveDialog`, strict `DialogThemeData`.
  - Cupertino: `CupertinoAlertDialog`, `CupertinoDialogAction`, `CupertinoPopupSurface`,
    `CupertinoDialogRoute`/`ShowCupertinoDialog`, elevation-aware `CupertinoDynamicColor`,
    `CupertinoUserInterfaceLevel`.

## Divergences introduced (registered in `docs/ai/DIVERGENCES.md`)

- `CupertinoDialog.cs`: sliding-tap tracks the raw primary pointer instead of the arena-participating
  `_SlidingTapGestureRecognizer`/`_TargetSelectionGestureRecognizer` pair, so sliding selection does
  not yield to actions-list scrolling. Close by porting the recognizers onto the shared drag pipeline.
- `CupertinoTheme.cs`: high-contrast dynamic-color variants are stored but never selected because the
  host accessibility flag is not surfaced. Close by plumbing the flag through `MediaQuery`.
- `DialogTheme.cs`: Dart's obsolete field-based `DialogTheme` constructor/`copyWith`/`lerp` shims and
  the feature-flagged windowing branch of `showRawDialog` are not ported.
- `CupertinoPopupSurface` joined the existing rounded-superellipse clip divergence row.

## Test / Sample coverage

- `src/Plumix.Tests/MaterialDialogTests.cs` (adaptive switch, traversal edges, `requestFocus`,
  `AnimationStyle`, title centering, pop-completes-future) and the new
  `src/Plumix.Tests/CupertinoDialogTests.cs` (widths, styles, layouts, sliding taps, semantics,
  spring route). Demo probes mirrored in `DialogDemoPage.cs` and `dialog_demo_page.dart`.
