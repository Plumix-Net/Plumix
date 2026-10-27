# Framework Master Plan

This is the single source of truth for framework status, direction, and implementation priorities.

## AI Semantic Snapshot

Use this block as the fastest machine-readable status summary.

```yaml
framework_plan_version: 2
last_updated: 2026-10-27
north_star: "Flutter-like widget/rendering framework in C# with Avalonia as host infrastructure."
current_phase: "RC preparation: every blocker met (2026-10-27); next are the release gates on the tagged commit."
latest_release: "v0.4.0-beta.3 (2026-10-24); next: v1.0.0-rc.1, criteria in the Release Candidate section"
flutter_pin: "3.47.0 (4cf24164269); material_ui 1.0.0; cupertino_ui 1.0.0 — see AGENTS.md"
open_work_outside_controls: "docs/ai/BACKLOG.md (control-level gaps, host-level gaps; the 3.44->3.47 re-port table is down to subsystem-blocked rows)"
status:
  widget_element_state_lifecycle: done
  render_pipeline_layout_paint_compositing_semantics: done
  ui_geometry_companions_and_rounded_shape_contracts: done
  native_path_geometry_boolean_operations_and_metrics: done
  native_paragraph_metrics_variable_fonts_and_paints: done
  scheduler_ticker_frame_flow: done
  gesture_arena_and_recognizers: done
  navigation_stack_and_observers: done
  scroll_sliver_list_grid_pipeline: done
  desktop_widget_host_app_flow: done
  material_library_rewrite: done
  cupertino_library_port: done
  browser_android_ios_sample_hosts: done
  docs_alignment_and_tracking: done
milestones:
  - { id: M1, title: "Core parity hardening", status: done }
  - { id: M2, title: "Input/focus/accessibility completion", status: done }
  - { id: M3, title: "Port-first widget set expansion", status: done }
  - { id: M4, title: "Material library rewrite", status: done }
  - { id: M5, title: "Cross-host sample parity and stability", status: done }
  - { id: M6, title: "Cupertino library port", status: done }
  - { id: RC, title: "v1.0.0-rc.1 API freeze (criteria: Release Candidate section)", status: planned }
```

## Confirmed Done (Repository Baseline)

- [x] Flutter-like core abstractions exist and are wired: `Widget -> Element -> RenderObject`.
- [x] Stateful lifecycle and build scheduling include per-owner focus managers and per-root build scopes.
- [x] Inherited dependency model is implemented (`InheritedWidget`, `InheritedModel`, `InheritedNotifier`).
- [x] Render pipeline is implemented (`PipelineOwner`, layout/compositing/paint/semantics phases).
- [x] Layer tree primitives are implemented (offset/opacity/transform/clip/picture layers), built into retained
  dart:ui scenes (`SceneBuilder`, `addRetained`) that hosts rasterize.
- [x] Gesture system is implemented (pointer router, arena, tap/drag/long-press recognizers).
- [x] Navigation stack is implemented (`Navigator`, routes, named routes, observers, back handling).
- [x] Scroll/sliver stack is implemented (`Scrollable`, `Viewport`, sliver lists/grids, keep-alive, notifications).
- [x] Binding-owned `RunApp`/`RunWidget` bootstrap drives the desktop `WidgetHost` implicit view.
- [x] Sample gallery demonstrates navigation, scrolling, and editable text/focus demos through framework widgets.
- [x] Automated test project exists and covers lifecycle, rendering, layers, semantics, gestures, navigation, and scrolling.
- [x] Hot reload is supported via .NET Hot Reload + Flutter's reassemble flow (`HotReloadManager` → `BindingBase.ReassembleApplication`, `Element.Reassemble`), preserving `State` across code patches.
- [x] Every widget family in `material_ui/lib/src/` is ported and the Material theming foundation is closed in both Material 2 and Material 3 (M4, closed 2026-08-15).

## Milestones

### M1–M4

`done` (M1 2026-03-10, M2 2026-03-11, M3 2026-03-12, M4 2026-08-15). Their bodies were retired on
2026-08-16; the shipped result is the repository itself plus the summary in `CHANGELOG.md`. What
remains from those passes is tracked as rows in `docs/ai/DIVERGENCES.md`, qualified markers in
`docs/ai/PORT_MAP.md` (*Ports with a qualified marker*), and `docs/ai/BACKLOG.md`.

### M5. Cross-Host Sample Parity and Stability

`done` (2026-10-27): the Android, iOS and Browser hosts build, and the iOS sample runs AOT-only (Release,
simulator) through the Bloc counter and its restored named route. The earlier `ExecutionEngineException`
came from a prebuilt `Plumix.Bloc` bound to renamed framework members; `HostBinaryCompatibilityTests`
now binds the sample's packages against the in-tree framework.

Exit criteria:

- Desktop, browser, Android, and iOS sample hosts build successfully from the solution.
- Framework-driven app flow remains identical across hosts.
- `src/Sample/Plumix.Sample` and `dart_sample` stay in feature/route/module parity.

### M6. Cupertino Library Port

`done` (2026-10-27): every `cupertino_ui/lib/src/` file is a strict port, every Material `.Adaptive`
factory composes its Cupertino widget as Flutter does, and `SampleCupertinoTabTests` drives the gallery's
Cupertino tab and `CupertinoApp` shell.

### Release Candidate (`v1.0.0-rc.1`)

Status: `planned`. The RC freezes the public API of the published packages: after it only fixes land
until `1.0.0`, and any `Breaking:` entry means a new RC.

Blockers (all must close):

- M5 and M6 are `done` (met 2026-10-27).
- Every sample route in `SampleScreenCaptureTests` matches the Dart capture, or its residual
  difference is a `docs/ai/DIVERGENCES.md` row. (met 2026-10-27: 163 of 164 within Skia AA noise;
  `/desktop-text-selection-toolbar` is a `DIVERGENCES.md` row).
- No qualified marker in `docs/ai/PORT_MAP.md` (met 2026-10-27).
- Control-level gaps (`docs/ai/BACKLOG.md`) that change an existing signature, type or default are
  closed (met 2026-10-27: state-property slots on `IWidgetStateProperty`, `AnimationController` errors,
  `PipelineManifold`, `Route.BuildPage`, core `Brightness`, `Timer`). Gaps that only add API may land
  in a later minor.

Release gates (on the commit that gets the tag):

- `dotnet build src/Plumix.Ci.slnf -c Debug`; the test suite green in Debug, Profile and Release, and
  on the newest Avalonia of the major (`AvaloniaTestVersion`); `scripts/check_line_length.sh` clean.
- `src/Plumix.sln` builds locally, Browser/Android/iOS hosts included.
- Every gallery route opens without errors on desktop, browser, Android and iOS. Interactive
  comparisons (`verify-runtime` in `docs/ai/PARITY_MATRIX.md`, *Pending runtime verification* in
  `docs/ai/BACKLOG.md`) may stay open as known limitations.
- `python3 scripts/generate_port_map.py` reports no unresolvable markers.
- `CHANGELOG.md`: `[Unreleased]` collapses under the RC heading, every `Breaking:` item kept by name.

Known limitations (listed in the RC release notes, not blockers):

- `docs/ai/BACKLOG.md` > Host-level gaps: platform views, `flutter/menu`,
  the rest of `SystemChrome.*`, accessibility features and semantics focus on desktop/Android,
  `GestureSettings`, text spacing overrides, windowing-owner gaps, the iOS system context menu.
- The upstream re-port row (platform-view hit-test), blocked on platform views.
- Additive control-level gaps (service extensions, missing `EditableText` parameters, the Avalonia
  FORWARD_DEL report).
- Every row of `docs/ai/DIVERGENCES.md`.

## Backlog Candidates (After M5–M6)

- Native accessibility bridges: shipped for every host (Avalonia automation, iOS, browser); gaps in `docs/ai/BACKLOG.md`.
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
  When a milestone closes, replace its body with a one-line status; do not archive the body anywhere
  (git history keeps it).
