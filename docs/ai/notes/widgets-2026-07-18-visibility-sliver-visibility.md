# Feature: Visibility + SliverVisibility

## Goal

- Port Flutter's box and sliver visibility controls with matching replacement, retained-subtree, layout, paint,
  pointer, semantics, focus, and inherited-visibility behavior.

## Non-Goals

- Redesign the framework ticker ownership model in this control iteration.

## Context Plan

- Entry files:
  - `flutter/packages/flutter/lib/src/widgets/visibility.dart`
  - `flutter/packages/flutter/lib/src/widgets/sliver.dart`
  - `flutter/packages/flutter/lib/src/rendering/proxy_sliver.dart`
- Expansion trigger:
  - Add shared sliver pointer/offstage render proxies required by the Dart composition.

## Delivery Scope

- Target controls:
  - `Visibility`
  - `SliverVisibility`
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for both controls

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Touched invariants:
  - `Widget -> Element -> RenderObject` ownership remains in core.
  - Sliver geometry, hit testing, paint, and semantics remain render-object behavior.
  - C# and Dart sample structure remains mirrored.

## Dart Reference Mapping

- Flutter/Dart sources:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/visibility.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/sliver.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/rendering/proxy_sliver.dart`
  - `dart_sample/lib/demos/general/offstage_demo_page.dart`
- Parity mapping:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence:
  - The existing `TickerMode` ownership row in `docs/ai/DIVERGENCES.md` now includes `Visibility.cs`.
    `maintainAnimation=false` builds the source-matching `TickerMode(enabled: visible)` subtree, but descendant
    framework tickers are not physically muted until tickers are associated with element ancestry.

## Test Plan

- New coverage:
  - `src/Plumix.Tests/VisibilityTests.cs`
- Covered risks:
  - constructor guards and maintained factories;
  - child disposal versus state retention;
  - nested `Visibility.Of` and focus exclusion;
  - maintained box/sliver size and geometry;
  - paint, hit-test, semantics, and offstage suppression.

## Sample Parity Plan

- [x] C# sample updated
- [x] Dart sample updated
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] Both public controls and their required render primitives are implemented.
- [x] Tests pass.
- [x] No invariant violations introduced.
- [ ] Descendant tickers are muted when `maintainAnimation=false`.
- [x] The remaining shared blocker and next action are recorded in `docs/ai/DIVERGENCES.md`.
