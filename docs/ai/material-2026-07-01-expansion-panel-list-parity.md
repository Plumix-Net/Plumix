# Feature: material-2026-07-01-expansion-panel-list-parity

## Goal

- Port `ExpansionPanel`, `ExpansionPanelRadio`, and `ExpansionPanelList` (including the radio constructor) with Flutter-matching controlled/radio state behavior, composition, animation, layout, semantics, tests, and mirrored C#/Dart demos.

## Non-Goals

- Exhaustive standalone `MergeableMaterial` reconciliation for arbitrary simultaneous slice insertion/removal/reordering beyond the keyed gap transitions required by `ExpansionPanelList`.
- State restoration beyond the mounted `ExpansionPanelList` state.

## Context Budget Plan

- Budget: max 20 files in the initial read.
- Entry files:
  - `src/Plumix.Material/ExpansionTile.cs`
  - `src/Plumix/Widgets/Expansible.cs`
  - `src/Plumix.Material/Card.cs`
  - `src/Plumix.Material/Divider.cs`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/expansion_panel.dart`
- Expansion trigger:
  - Shared input, semantics, test, and paired sample files may be opened to close both normal and radio behavior in one pass.

## Delivery Scope (Required for Control Parity Work)

- Target controls:
  - `ExpansionPanel`
  - `ExpansionPanelList`
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for both normal and radio modes

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Framework behavior stays in `src/Plumix*`; host adapters remain unchanged.
- Expansion callbacks and radio exclusivity follow the Dart state machine.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/expansion_panel.dart`
  - `dart_sample/lib/demos/material/expansion_panel_demo_page.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log:
  - The framework currently exposes only uniform `BorderRadius` and uniform `BorderSide`; merged groups therefore use uniform outer card radii and a 0.5px layout divider instead of Flutter's independently animated edge radii and paint-only top/bottom borders. Remove this delta when per-corner radii and per-edge decoration borders land.
  - Body transition uses the shared `Expansible` clipped-height animation plus the same delayed opacity interval instead of a standalone `AnimatedCrossFade`; visible height/opacity/state lifecycle matches, while cross-fade widget structure differs until that shared primitive lands.

## Planned Changes

- Add panel descriptors and a stateful panel list with normal/radio behavior.
- Compose existing framework-owned expansion, Material button, card, divider, and semantics primitives.
- Add focused tests and one mirrored C#/Dart runtime page.
- Update roadmap, matrices, and changelog.

## Test Plan

- Constructor defaults and validation, controlled callbacks, header/icon tap gating, radio callback ordering and exclusivity, initial-open value, animated body/header/icon, gap/divider/background/elevation, and expanded-state semantics.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] Both controls are closed end-to-end for the supported shared framework surface.
- [x] Behavior implemented and tests passing.
- [x] No invariant violations introduced.
- [x] Remaining parity gaps are documented with a concrete follow-up condition.

## Validation Results

- `dotnet test src/Plumix.Tests/Plumix.Tests.csproj -c Debug --no-build`: 772/772 passed.
- Focused `MaterialExpansionPanelTests`: 7/7 passed.
- C# sample project builds with no warnings; Dart demo/gallery/routes analyze with no issues.
- Desktop host starts and remains live through the framework widget-host path.
- Full solution builds Desktop, Android, Browser, libraries, and tests; iOS remains blocked by the pre-existing local toolchain mismatch (`.NET iOS 26.4` requires Xcode `26.4`, installed Xcode is `26.6`).
