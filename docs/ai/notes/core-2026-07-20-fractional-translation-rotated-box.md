# Feature: FractionalTranslation + RotatedBox

## Goal

- Close the paired Flutter transform-widget pass for fractional paint translation and layout-time quarter turns.

## Non-Goals

- Porting the framework-wide intrinsic-dimension and dry-layout query pipeline.

## Context Plan

- Entry files:
  - `src/Plumix/Widgets/Basic.cs`
  - `src/Plumix/Rendering/Proxy.RenderBox.cs`
  - `src/Plumix.Tests/BasicWidgetProxyTests.cs`
- Expansion trigger:
  - A direct `RenderRotatedBox` source mapping requires the currently absent shared intrinsic/dry-layout protocols.

## Delivery Scope (Required for Control Parity Work)

- Target controls:
  - `FractionalTranslation`
  - `RotatedBox`
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Runtime constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for both controls

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Touched invariants:
  - `Widget -> Element -> RenderObject` architecture boundary
  - Dart source as parity source of truth
  - mirrored C#/Dart sample behavior

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/basic.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/rendering/proxy_box.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/rendering/rotated_box.dart`
  - `dart_sample/lib/demos/general/proxy_widgets_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Runtime constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergences:
  - `docs/ai/DIVERGENCES.md` records the absent framework-wide intrinsic/dry-layout query protocol.

## Planned Changes

- `src/Plumix/Widgets/Basic.cs`: expose both source-shaped widgets.
- `src/Plumix/Rendering/Proxy.RenderBox.cs`: port paint, hit-test, layout, baseline, and semantics behavior.
- `src/Plumix.Tests/BasicWidgetProxyTests.cs`: cover defaults, updates, constraints, transforms, layers, and hits.
- Mirrored sample and tracking files: add interactive probes and record coverage.

## Test Plan

- Existing tests to run/update:
  - `src/Plumix.Tests/BasicWidgetProxyTests.cs`
- Parity-risk scenarios covered:
  - transformed versus untransformed hit testing
  - hits in translated overflow
  - odd-turn constraint/size transposition
  - negative turn values
  - paint-layer and semantics transforms
  - rotated child baseline exclusion

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] Both controls are closed for all executable framework contracts
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Runtime parity constraints satisfied
- [x] The shared intrinsic/dry-layout blocker and close action are documented
