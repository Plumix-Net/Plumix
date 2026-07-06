# Feature: ExpandIcon + Stepper parity

## Goal

- Port Flutter `ExpandIcon` and `Stepper` together with API/default, state, layout, paint, semantics, tests, and mirrored sample coverage.

## Dart Reference Mapping

- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/expand_icon.dart`
- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/stepper.dart`
- `dart_sample/lib/demos/material/stepper_demo_page.dart`

## Completion

- [x] Public API/default values and constructor guards.
- [x] Expand/collapse callback, rotation, colors, localized semantic hints.
- [x] Vertical/horizontal step composition and controlled state transitions.
- [x] Indexed/editing/complete/disabled/error visuals, connectors, custom icons and controls.
- [x] Focused tests and mirrored C#/Dart route.
- [x] Tracking docs and divergence registry updated.

## Remaining Divergence

- Exact vertical `ensureVisible` behavior and implicit title/horizontal-size animations await shared core primitives; tracked in `docs/ai/DIVERGENCES.md`.
