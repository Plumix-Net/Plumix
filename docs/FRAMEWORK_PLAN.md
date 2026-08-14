# Framework Master Plan

This is the single source of truth for framework status, direction, and implementation priorities.

## AI Semantic Snapshot

Use this block as the fastest machine-readable status summary.

```yaml
framework_plan_version: 1
last_updated: 2026-08-14
north_star: "Flutter-like widget/rendering framework in C# with Avalonia as host infrastructure."
current_phase: "M4 material library rewrite (theme/scaffold/material controls) in progress."
flutter_pin: "3.47.0 (4cf24164269); 3.44->3.47 re-port backlog: docs/ai/notes/migration-2026-08-13-flutter-3.47-pin.md"
status:
  widget_element_state_lifecycle: done
  render_pipeline_layout_paint_compositing_semantics: done
  scheduler_ticker_frame_flow: done
  gesture_arena_and_recognizers: done
  navigation_stack_and_observers: done
  scroll_sliver_list_grid_pipeline: done
  desktop_widget_host_app_flow: done
  material_library_rewrite: in_progress
  browser_android_ios_sample_hosts: planned
  dart_to_csharp_control_porting_readiness: in_progress
  docs_alignment_and_tracking: in_progress
next_milestones:
  - id: M1
    title: "Core parity hardening"
    status: done
  - id: M2
    title: "Input/focus/accessibility completion"
    status: done
  - id: M3
    title: "Port-first widget set expansion"
    status: done
  - id: M4
    title: "Material library rewrite"
    status: in_progress
  - id: M5
    title: "Cross-host sample parity and stability"
    status: planned
```

## Confirmed Done (Repository Baseline)

- [x] Flutter-like core abstractions exist and are wired: `Widget -> Element -> RenderObject`.
- [x] Stateful lifecycle and build scheduling are implemented (`State`, `SetState`, `BuildOwner`).
- [x] Inherited dependency model is implemented (`InheritedWidget`, `InheritedModel`, `InheritedNotifier`).
- [x] Render pipeline is implemented (`PipelineOwner`, layout/compositing/paint/semantics phases).
- [x] Layer tree primitives are implemented (offset/opacity/transform/clip/picture layers).
- [x] Gesture system is implemented (pointer router, arena, tap/drag/long-press recognizers).
- [x] Navigation stack is implemented (`Navigator`, routes, named routes, observers, back handling).
- [x] Scroll/sliver stack is implemented (`Scrollable`, `Viewport`, sliver lists/grids, keep-alive, notifications).
- [x] Widget host path is active on desktop (`FlutterExtensions.Run` + `WidgetHost`).
- [x] Sample gallery demonstrates navigation, scrolling, and editable text/focus demos through framework widgets.
- [x] Automated test project exists and covers lifecycle, rendering, layers, semantics, gestures, navigation, and scrolling.
- [x] Hot reload is supported via .NET Hot Reload + Flutter-style reassemble flow (`HotReloadManager`, `ReassembleApplication`, `Element.Reassemble`), preserving `State` across code patches.

## Global Plan

### M1. Core Parity Hardening

Status: `done` (closed 2026-03-10). Completion notes and exit criteria: `docs/FRAMEWORK_PLAN-archive.md`.

### M2. Input, Focus, and Accessibility Completion

Status: `done` (closed 2026-03-11). Completion notes and exit criteria: `docs/FRAMEWORK_PLAN-archive.md`.

### M3. Port-First Widget Set Expansion

Status: `done` (closed 2026-03-12). Completion notes and exit criteria: `docs/FRAMEWORK_PLAN-archive.md`.

### M4. Material Library Rewrite

Status: `in_progress`

What is left (live list, do not duplicate it here): `docs/MATERIAL_TODO.md`.
Per-control completion notes for passes already closed: `docs/FRAMEWORK_PLAN-archive.md`.
Shipped changes: `CHANGELOG.md`. Active divergences: `docs/ai/DIVERGENCES.md`.

Initial scope:

- Introduce framework-level theming primitives (`ThemeData`, `Theme`, baseline color/text style propagation).
- Introduce shell/layout primitives for Material app structure (`Scaffold`, `AppBar`, and supporting slots).
- Introduce first Material control set (`TextButton`, `ElevatedButton`, `OutlinedButton`) on top of framework render/widget layers.
- Keep architecture boundaries explicit: behavior in framework libraries (`src/Plumix`, `src/Plumix.Material`), host integration in sample hosts only.

Exit criteria:

- Material theming is available through inherited framework state and can drive common control defaults.
- Material shell primitives are sufficient to host route pages without custom sample-only wrappers.
- Initial Material control set supports core states and API shape needed for straightforward Dart-to-C# rewrites.
- Regression coverage exists for widget-to-render wiring and theming resolution behavior.

### M5. Cross-Host Sample Parity and Stability

Status: `planned`

Scheduling note (2026-03-12):

- Moved after Material rewrite as a final stabilization milestone. Current blockers are local toolchain/environment alignment (Android API 36 SDK platform missing; iOS workload/Xcode version mismatch).

Exit criteria:

- Desktop, browser, Android, and iOS sample hosts build successfully from the solution.
- Framework-driven app flow remains identical across hosts.
- `src/Sample/Plumix.Sample` and `dart_sample` stay in feature/route/module parity.

## Backlog Candidates (After M1-M5)

- Text editing/IME primitives and richer text input workflows.
- Overlay/portal-like primitives and advanced route transitions.
- Performance instrumentation and frame diagnostics tooling.
- Expanded documentation for migration recipes from Flutter (Dart) widgets to C#.

## Update Protocol (For Humans and AI Agents)

This document owns exactly one thing: milestone status and roadmap direction.

- Always update this file when milestone status changes (`done`, `in_progress`, `planned`, `blocked`).
- For every meaningful feature change, update both:
  - semantic status (this document),
  - historical record (`CHANGELOG.md`).
- Porting workflow and delivery-unit rules live in `docs/ai/PORTING_MODE.md`; architecture/package/versioning rules live in `docs/ai/INVARIANTS.md`. Do not restate them here.
- **Size budget: this file must stay under 10 KB.** Every agent reads it on every task, so growth here is
  a tax on every port. Do not append per-control completion notes — those belong in `CHANGELOG.md`.
  When a milestone closes, replace its body with a one-line status plus a pointer, and move the body to
  `docs/FRAMEWORK_PLAN-archive.md` (same rotation discipline as `CHANGELOG.md`).
