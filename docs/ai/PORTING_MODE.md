# Porting Mode: Parity-First (Strict 1:1 Default)

Purpose: define a single mandatory workflow for Dart-to-C# control/widget ports.

## Source of Truth

- For framework controls/widgets that exist in Flutter, Dart implementation is the source of truth.
- Reference priority:
  1. Flutter framework Dart source (API/behavior/state/constraints defaults).
  2. `dart_sample` app structure and usage patterns.
  3. Existing C# implementation details (only when already parity-aligned).

## Default Porting Rule

- Default approach is structural `1:1` port, not behavioral approximation.
- Keep parity in:
  - public API shape and default values,
  - widget composition order,
  - state transitions and interaction states,
  - layout/constraints behavior,
  - paint/visual semantics.

## Delivery Unit (Default)

- Default delivery unit for parity work is one complete control per request/iteration.
- "Complete" means parity is closed for:
  - API/default values,
  - composition order,
  - interaction/state transitions,
  - layout/constraints behavior,
  - paint/visual semantics,
  - focused control tests.
- Avoid micro-iterations that split one control into many token-level passes (for example, geometry first, then colors, then overlay) unless there is a hard blocker.
- Split only when:
  - a missing primitive must be landed first,
  - risk requires an isolated foundational step,
  - user explicitly asks for phased delivery.
- If split is unavoidable, the feature note must document what remains and the immediate next step to close parity for the same control.

## When Framework Primitives Are Missing

- Do not introduce control-local workaround logic if it changes structure/behavior from Flutter.
- First add or fix the missing framework primitive in `src/Flutter` / `src/Flutter.Material`.
- Then continue the control port with the same structure as Dart and close the control in the same request whenever feasible.

## Allowed Divergence

- Divergence is allowed only when platform/runtime constraints require it.
- Every divergence must be documented in the same iteration:
  - feature note (`docs/ai/*`),
  - `CHANGELOG.md` (short note),
  - inline code comment only when needed for future maintainers.
- Divergence note must include:
  - exact reason,
  - expected behavior delta,
  - follow-up condition for removing divergence.

## Required Validation for Ports

- Add/update focused tests in `src/Flutter.Tests` for:
  - default values,
  - interaction states,
  - known high-risk layout/constraints scenarios,
  - parity-critical paint/visual behavior for the target control.
- Keep sample parity in the same iteration:
  - update both `src/Sample/Flutter.Net` and `dart_sample`,
  - update `docs/ai/PARITY_MATRIX.md` when route/page behavior changes.

## Definition of Done for Port Iterations

- One control (or explicitly scoped feature) is closed end-to-end for parity in this iteration.
- Dart references are explicitly listed.
- C# structure follows Dart model without ad hoc substitutions.
- Divergences (if any) are documented with justification.
- Tests cover the parity-critical behavior introduced or changed.
