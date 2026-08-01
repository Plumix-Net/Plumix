# Feature: MenuAnchor overlay closeout

## Goal

- Move `MenuAnchor`, `MenuBar`, and `SubmenuButton` from in-tree panel paint to Flutter-shaped raw-menu overlay
  ownership with safe placement, dismissal, animation, semantics, tests, and mirrored sample coverage.

## Non-Goals

- Rebuild the framework-wide focus traversal policy or directional edge-inset hierarchy inside a Material control.

## Context Plan

- Entry files:
  - `src/Plumix/Widgets/RawMenuAnchor.cs`
  - `src/Plumix.Material/MenuAnchor.cs`
  - `src/Plumix.Tests/MaterialMenuAnchorTests.cs`
- Expansion trigger:
  - The Flutter spec exposed missing raw-menu overlay, directional focus, display-feature, and inset primitives.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - `MenuAnchor` / `MenuBar` / `SubmenuButton`
- Completion checklist:
  - [x] API/default values for the changed controller/animation/overlay surface
  - [x] Overlay composition order and grouped outside-tap ownership
  - [x] Controller, sibling-close, hover-delay, panel-animation, and closing interaction states
  - [x] Reserved-padding, keyboard-inset, display-feature, explicit-position, flip, clamp, and RTL layout
  - [x] Material surface, shape-side, clip, opacity, and semantics behavior for covered states
  - [x] Focused tests for the delivered surface
  - [ ] Stateful shared `MenuController` raw anchor/group tree and intercepted close lifecycle
  - [ ] Ancestor-scroll closure with internal-menu-scroll exemption
  - [ ] Complete source shortcut/directional-focus action map
  - [ ] Per-item fade stagger and completion-gated scrollbar
  - [ ] `EdgeInsetsGeometry` menu padding/resolution

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed
- Framework behavior stays in `Plumix`/`Plumix.Material`; the sample remains a consumer.
- The C# and Dart demo changes remain mirrored.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `flutter-src/packages/flutter/lib/src/material/menu_anchor.dart`
  - `flutter-src/packages/flutter/lib/src/widgets/raw_menu_anchor.dart`
  - `flutter-src/packages/flutter/test/material/menu_anchor_test.dart`
  - `dart_sample/lib/demos/material/dropdown_demo_page.dart`
- Parity mapping checklist:
  - [x] Public defaults and theme chains mapped
  - [x] Overlay composition and panel surface mapped for the delivered path
  - [x] Controller, dismissal, hover-delay, semantics, and panel transitions mapped
  - [x] Safe-area, keyboard, display-feature, explicit-position, flip, clamp, and RTL layout mapped
  - [x] Material paint/clip/shape-side semantics mapped
- Divergences:
  - The remaining focus/inset/item-stagger gap is updated in `docs/ai/DIVERGENCES.md`.

## Planned Changes

- `src/Plumix/Widgets/RawMenuAnchor.cs`: shared overlay portal and grouped tap ownership.
- `src/Plumix/Widgets/MediaQuery.cs`: display-feature geometry exposed to overlay layout.
- `src/Plumix.Material/MenuAnchor.cs`: raw overlay, placement, animation, controller, and panel composition.
- `src/Plumix.Tests/MaterialMenuAnchorTests.cs`: focused defaults, curve, and geometry assertions.
- Mirrored sample and tracking files: runtime probe and status updates.

## Test Plan

- Existing tests:
  - `src/Plumix.Tests/MaterialDropdownTests.cs`
  - `src/Plumix.Tests/MaterialMenuAcceleratorTests.cs`
- New tests:
  - `src/Plumix.Tests/MaterialMenuAnchorTests.cs`
- Parity-risk scenarios:
  - Unattached controller behavior, nearest/root portal ownership, safe reserved padding, keyboard insets,
    display-feature sub-screens, explicit positions, vertical and cascading flips, LTR/RTL, and curve checkpoints.

## Sample Parity Plan

- [x] C# sample uses the animated consumed-outside-tap path.
- [x] Dart sample mirrors the same probe.
- [x] `docs/ai/PARITY_MATRIX.md` updated.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` checked; milestone state did not change
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [ ] Full control parity is closed end-to-end.
- [x] Overlay and panel behavior implemented and tested.
- [x] Architecture and sample-parity invariants preserved.
- [x] Remaining parity gap has a concrete next action: land directional focus actions plus `EdgeInsetsGeometry`,
  move shared `MenuController` attachment/group/intercepted-close ownership into core, then port the ancestor-scroll
  hook, shortcut map, item intervals, and scrollbar completion branch directly.
