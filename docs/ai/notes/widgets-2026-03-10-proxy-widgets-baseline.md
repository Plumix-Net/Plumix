# Feature: widgets-2026-03-10-proxy-widgets-baseline

## Goal

- Add M3 baseline widget wrappers over existing proxy render objects so Dart-to-C# ports can use widget-level APIs for opacity/transform/clip behavior.

## Non-Goals

- No new render-layer algorithms.
- No advanced clipping API (custom clippers/rounded clips).

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter/Rendering/Proxy.RenderBox.cs`
  - `src/Flutter.Tests/CompositingLayerTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Open additional files only if widget/element mount-update behavior needs dedicated test harness patterns.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Architecture boundaries remain `Widget -> Element -> RenderObject`.
  - Framework behavior remains in `src/Flutter` (no host-specific widget behavior).

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/Basic.cs`
  - `src/Flutter.Tests/BasicWidgetProxyTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/ProxyWidgetsDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/general/proxy_widgets_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `src/Flutter/Widgets/Basic.cs`: add `Opacity`, `Transform`, and `ClipRect` widgets.
  - `src/Flutter.Tests/BasicWidgetProxyTests.cs`: verify widget-to-render wiring and rebuild-time property updates.
  - `src/Sample/Flutter.Net/Demos/General/ProxyWidgetsDemoPage.cs`: add interactive proxy-widget sample page for framework runtime checks.
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`: wire route and menu entry for the new proxy demo page.
  - `dart_sample/lib/demos/general/proxy_widgets_demo_page.dart`: mirror the same demo page behavior in Dart sample.
  - `dart_sample/lib/sample_gallery_screen.dart`: mirror menu wiring in Dart sample.
  - `dart_sample/lib/sample_routes.dart`: add route constant for parity.
  - `docs/FRAMEWORK_PLAN.md`: mark M3 in-progress with first delivered baseline.
  - `docs/ai/TEST_MATRIX.md`: register new coverage row for proxy widgets.
  - `docs/ai/PARITY_MATRIX.md`: register new sample parity row for proxy widgets demo.
  - `CHANGELOG.md`: capture shipped M3 baseline in `Unreleased`.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj` (full pass)
- New tests to add:
  - `src/Flutter.Tests/BasicWidgetProxyTests.cs`

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
