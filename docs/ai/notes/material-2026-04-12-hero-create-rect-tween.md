# Feature: material-2026-04-12-hero-create-rect-tween

## Goal

- Close the next Hero parity slice by supporting destination-priority `Hero.createRectTween` during navigator hero flights.

## Non-Goals

- Hero shuttle builders.
- Nested-navigator hero orchestration.
- Gesture-driven hero flight diversion.

## Context Budget Plan

- Budget: max 12 files in initial read.
- Entry files:
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter/AnimationController.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Expansion trigger:
  - Add/update test and docs only if API + runtime flight wiring is validated.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Hero flight rect interpolation (`Hero.createRectTween`)
- Completion checklist (must be closed in this iteration unless explicitly blocked):
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Keep route transitions and widget/render behavior in framework layers, not host adapters.
  - Keep Flutter-like precedence semantics where destination Hero controls flight rect tweening.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/heroes.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log (only if needed):
  - None for this scoped feature.

## Planned Changes

- Files to edit:
  - `src/Flutter/AnimationController.cs`
  - `src/Flutter/Widgets/Hero.cs`
  - `src/Flutter/Widgets/Navigation.cs`
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `src/Flutter/AnimationController.cs`: add reusable linear `RectTween`.
  - `src/Flutter/Widgets/Hero.cs`: expose `createRectTween` API and carry tween into flight manifest.
  - `src/Flutter/Widgets/Navigation.cs`: evaluate flight bounds via manifest tween.
  - `src/Flutter.Tests/HeroNavigatorTests.cs`: verify destination-priority tween factory and tween evaluation on push flight.
  - docs/changelog files: sync shipped status and residual gap wording.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/HeroNavigatorTests.cs`
  - `src/Flutter.Tests/NavigationTests.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
- New tests to add:
  - Destination-priority `createRectTween` regression in `src/Flutter.Tests/HeroNavigatorTests.cs`.
- Parity-risk scenarios covered:
  - Destination Hero tween precedence over source Hero tween callback.
  - Runtime flight overlay uses custom tween evaluation path.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [ ] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps (if any) are documented with blocker + next action
