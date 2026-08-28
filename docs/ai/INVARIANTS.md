# Invariants

These rules are non-negotiable unless explicitly changed via documented architecture decision.

## Architecture Boundaries

- Framework behavior must stay in framework libraries under `src/` (`src/Plumix`, `src/Plumix.Material`, `src/Plumix.Cupertino`).
- Avalonia is host/platform infrastructure, not business logic for framework widgets.
- Core direction remains `Widget -> Element -> RenderObject -> platform adapter`.

## Package Boundaries

Allowed dependency direction (mirrors Flutter's `widgets <- cupertino <- material` layering):

- `Plumix` (core) depends on no other Plumix package.
- `Plumix.Cupertino` depends only on `Plumix`.
- `Plumix.Material` depends on `Plumix` and `Plumix.Cupertino` (adaptive controls).
- `Plumix.Sample` and platform hosts may depend on any of the above.
- Never introduce a reverse edge (core referencing Material, Cupertino referencing Material). If core needs a Material concept, port the underlying primitive into `Plumix` instead.

## Public API and Versioning

`Plumix`, `Plumix.Material`, and `Plumix.Cupertino` are published NuGet packages; their public API is a contract.

- Versioning follows SemVer: breaking public API/behavior changes require a major bump; new API is minor; fixes are patch.
- Breaking changes must be called out explicitly in `CHANGELOG.md` (a `Breaking:` prefix on the entry) — never shipped silently inside a parity pass.
- Parity fixes that change existing public defaults/behavior count as breaking for consumers even when they move closer to Flutter; call them out the same way.
- CI gate: `dotnet test src/Plumix.Tests/Plumix.Tests.csproj` must be green before any change is considered done; releases are tag-driven (`v*.*.*`) via `.github/workflows/ci.yml`.

## Dart Porting Invariants

- Dart implementation is the source of truth for matching controls/widgets; ports must follow strict `1:1` structure/behavior by default (see `docs/ai/PORTING_MODE.md`).
- Missing primitives must be implemented in framework layers first; do not hide parity gaps with control-local workarounds.
- Any intentional divergence from Dart behavior must be recorded in `docs/ai/DIVERGENCES.md` in the same iteration.

## Widget and Element Lifecycle

- `StatefulWidget` identity is preserved only when reconciliation keys/type allow it.
- `GlobalKey` reparenting must not dispose state when reinserted in same frame lifecycle.
- `BuildOwner` is the owner of dirty build scheduling; build work runs in frame flow.
- Inherited dependencies must notify only registered dependents per contract (`InheritedWidget/Model/Notifier`).

## Rendering Pipeline

- Pipeline phase order is stable: layout -> compositing bits -> paint -> semantics.
- Layout must not run with non-normalized constraints.
- Repaint boundaries own isolated layers and avoid unnecessary child repaint.
- Semantics updates are part of pipeline flush and must reflect render tree state.

## Input, Gestures, and Hit Testing

- Pointer events are routed through `GestureBinding` and gesture arena resolution.
- Hit testing must apply transform/clip semantics before dispatch.
- Recognizer conflict resolution should remain deterministic for covered scenarios.

## Navigation

- `Navigator` route stack must always keep a valid top route.
- Observer callbacks (`didPush/didPop/didReplace/didRemove`) must match stack mutations.
- Back-button handling should route through navigator APIs, not host-only ad hoc logic.

## Scroll and Slivers

- `ScrollPosition` and physics must clamp/advance within computed scroll extents.
- Viewport/sliver contracts define child creation, eviction, keep-alive reuse, and cache behavior.
- High-level widgets (`ListView`, `GridView`, `Scrollbar`) should map to sliver pipeline primitives.

## Sample Parity

- Feature/route/module parity between `src/Sample/Plumix.Sample` and `dart_sample` is required for sample-level changes.
- Scope: parity covers demo features, routes, and page/module structure. Host glue (`App.axaml`, csproj/platform bootstrap, Avalonia wiring) is exempt — purely host-side edits do not require a Dart-side change.
- Both samples must be updated in the same iteration, with status reflected in `docs/ai/PARITY_MATRIX.md`.

## Code Style

- Use **explicit types** for primitive/built-in value types and `string`: `double`, `int`, `long`, `float`, `decimal`, `bool`, `char`, `byte`, `short`, and their unsigned/`string` counterparts. Do not use `var` for these.
  - Applies to locals, `?? ` fallback chains, `Math.*` results, cast/conversion results, and pattern-binding intermediates whose inferred type is a primitive.
  - Example: `double effectiveWidth = width ?? theme?.Thickness ?? 0.0;` (not `var`).
- Keep `var` for complex/reference types where the type is evident from the right-hand side: constructor calls (`new Size(...)`), factory/`Of` accessors (`Theme.Of(context)`), LINQ results, and other non-primitive expressions.
- This is a non-negotiable convention for all new and ported code; agents must emit it correctly on first pass rather than relying on a follow-up refactor.
- **Max line length: 120 characters** (see `.editorconfig`). Wrap longer lines: one argument per line for long parameter/argument lists, break chained calls before `.`, split long conditions before `&&`/`||`. Applies to all new and edited lines; do not reformat untouched legacy lines just to satisfy the limit.

## Fast Safety Checks

- Lifecycle: `src/Plumix.Tests/ElementLifecycleTests.cs`
- Inherited: `src/Plumix.Tests/InheritedWidgetTests.cs`, `src/Plumix.Tests/InheritedModelTests.cs`, `src/Plumix.Tests/InheritedNotifierTests.cs`
- Pipeline: `src/Plumix.Tests/FramePipelineTests.cs`, `src/Plumix.Tests/RenderingParityTests.cs`
- Layers: `src/Plumix.Tests/CompositingLayerTests.cs`, `src/Plumix.Tests/LayerV2Tests.cs`
- Gestures: `src/Plumix.Tests/GesturePipelineTests.cs`
- Navigation: `src/Plumix.Tests/NavigationTests.cs`
- Scroll: `src/Plumix.Tests/ScrollPipelineTests.cs`, `src/Plumix.Tests/ScrollInfrastructureTests.cs`
- Semantics: `src/Plumix.Tests/SemanticsTreeTests.cs`, `src/Plumix.Tests/SemanticsDirtyPipelineTests.cs`
