# Feature: widgets-2026-03-10-decorated-box-border-radius-baseline

## Goal

- Add M3 decoration baseline (`DecoratedBox` + border/radius value objects) to support straightforward Dart-to-C# visual box decoration ports.

## Non-Goals

- No gradients/shadows/image decorations in this iteration.
- No directional border radii and per-corner radii yet (uniform radius baseline only).

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter/Rendering/Object.PaintingContext.cs`
  - `src/Flutter.Tests/BasicWidgetProxyTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
- Expansion trigger:
  - Expand only when decoration-specific value types and sample parity route wiring are required.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Framework behavior remains in `src/Flutter`.
  - Widget/render separation remains explicit (`Widget -> Element -> RenderObject`).
  - C# and Dart sample route parity is kept in lockstep.

## Planned Changes

- Files to edit:
  - `src/Flutter/Rendering/Decoration.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Tests/DecoratedBoxTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/DecoratedBoxDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/general/decorated_box_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `Decoration.cs`: define baseline decoration value objects.
  - `Proxy.RenderBox.cs`: add render-level decorated box painting.
  - `Basic.cs`: expose `DecoratedBox` widget and `Container.decoration` forwarding.
  - `DecoratedBoxTests.cs`: validate render/layout and widget update behavior.
  - sample files: add parity route/page for runtime checks.
  - docs/changelog: record M3 progression and coverage updates.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (full)
- New tests to add:
  - `src/Flutter.Tests/DecoratedBoxTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
