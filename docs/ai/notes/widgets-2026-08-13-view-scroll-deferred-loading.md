# Feature: View-backed scroll deferred loading

## Goal

- Close the deferred-loading divergence by reading the raw platform view and accounting for forced scroll jumps.

## Non-Goals

- Port the full multi-view `RawView`/`ViewAnchor` composition or platform view collection.

## Context Plan

- Entry files:
  - `src/Plumix/Widgets/View.cs`
  - `src/Plumix/Rendering/Scroll.cs`
  - `src/Plumix/Rendering/ScrollPhysics.cs`
- Expansion trigger:
  - The host root must expose stable physical view metrics independently of `MediaQuery` overrides.

## Delivery Scope (Required for Control Parity Work)

- Target feature:
  - `View.Of` plus `ScrollPosition.forcePixels`-backed deferred-loading parity.
- Completion checklist:
  - [x] API/default values
  - [x] Widget dependency behavior
  - [x] State transitions
  - [x] Deferred-loading threshold behavior
  - [x] No paint behavior applies
  - [x] Focused tests

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Architecture remains in core; forced pixel changes preserve the scroll activity contract.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `flutter/packages/flutter/lib/src/widgets/view.dart`
  - `flutter/packages/flutter/lib/src/widgets/scroll_position.dart`
  - `flutter/packages/flutter/lib/src/widgets/scroll_position_with_single_context.dart`
  - `flutter/packages/flutter/lib/src/widgets/scroll_physics.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget lookup/dependency behavior mapped
  - [x] State transitions mapped
  - [x] Threshold behavior mapped
  - [x] Paint behavior does not apply
- Divergence:
  - `View.view` is `View.ViewHandle` because C# forbids a member named `View` in the `View` class; registered in
    `docs/ai/DIVERGENCES.md` with no runtime delta.

## Planned Changes

- `src/Plumix/Widgets/View.cs`: expose stable raw view identity and physical metrics.
- `src/Plumix/WidgetHost.cs`: install and update the raw view above `MediaQuery`.
- `src/Plumix/Rendering/Scroll.cs`: add forced-pixel implied velocity and reset it after the frame.
- `src/Plumix/Rendering/ScrollPhysics.cs`: use `View.Of(context).PhysicalSize` for the terminal heuristic.

## Test Plan

- Existing tests updated:
  - `src/Plumix.Tests/StatefulBuilderLookupBoundaryTests.cs`
  - `src/Plumix.Tests/ScrollPhysicsTests.cs`
  - `src/Plumix.Tests/ScrollPipelineTests.cs`
- Parity-risk scenarios covered:
  - View identity changes, lookup-boundary hiding, nested `MediaQuery` overrides, direct force semantics, jump
    displacement, and the post-frame implied-velocity reset.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] No sample route or page behavior changed; `PARITY_MATRIX.md` needs no delta.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` unchanged because milestone status did not change
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] The selected divergence is closed end-to-end
- [x] Behavior implemented
- [x] Required validation gates pass
- [x] No architecture invariant violations introduced
- [x] The language-shaped property-name divergence is documented
