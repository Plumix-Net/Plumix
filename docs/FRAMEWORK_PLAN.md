# Framework Master Plan

This is the single source of truth for framework status, direction, and implementation priorities.

## AI Semantic Snapshot

Use this block as the fastest machine-readable status summary.

```yaml
framework_plan_version: 2
last_updated: 2026-08-16
north_star: "Flutter-like widget/rendering framework in C# with Avalonia as host infrastructure."
current_phase: "M6 Cupertino library port (docs/CUPERTINO_TODO.md); M5 cross-host stability continues in parallel."
flutter_pin: "3.47.0 (4cf24164269); material_ui 1.0.0; cupertino_ui 1.0.0 — see AGENTS.md"
open_work_outside_controls: "docs/ai/BACKLOG.md (3.44->3.47 re-port deltas, host-level gaps)"
status:
  widget_element_state_lifecycle: done
  render_pipeline_layout_paint_compositing_semantics: done
  scheduler_ticker_frame_flow: done
  gesture_arena_and_recognizers: done
  navigation_stack_and_observers: done
  scroll_sliver_list_grid_pipeline: done
  desktop_widget_host_app_flow: done
  material_library_rewrite: done
  cupertino_library_port: in_progress
  browser_android_ios_sample_hosts: in_progress
  docs_alignment_and_tracking: done
milestones:
  - { id: M1, title: "Core parity hardening", status: done }
  - { id: M2, title: "Input/focus/accessibility completion", status: done }
  - { id: M3, title: "Port-first widget set expansion", status: done }
  - { id: M4, title: "Material library rewrite", status: done }
  - { id: M5, title: "Cross-host sample parity and stability", status: in_progress }
  - { id: M6, title: "Cupertino library port", status: in_progress }
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
- [x] Every widget family in `material_ui/lib/src/` is ported and the Material theming foundation is closed in both Material 2 and Material 3 (M4, closed 2026-08-15).

## Milestones

### M1–M4

`done` (M1 2026-03-10, M2 2026-03-11, M3 2026-03-12, M4 2026-08-15). Their bodies were retired on
2026-08-16; the shipped result is the repository itself plus the summary in `CHANGELOG.md`. What
remains from those passes is tracked as rows in `docs/ai/DIVERGENCES.md`, qualified markers in
`docs/ai/PORT_MAP.md` (*Ports with a qualified marker*), and `docs/ai/BACKLOG.md`.

### M5. Cross-Host Sample Parity and Stability

Status: `in_progress` (runs in parallel with M6; blockers are local toolchain/environment alignment —
Android API 36 SDK platform, iOS workload/Xcode version).

Exit criteria:

- Desktop, browser, Android, and iOS sample hosts build successfully from the solution.
- Framework-driven app flow remains identical across hosts.
- `src/Sample/Plumix.Sample` and `dart_sample` stay in feature/route/module parity.

### M6. Cupertino Library Port

Status: `in_progress` (opened 2026-08-16). Work list and per-file status: `docs/CUPERTINO_TODO.md`.

Order of work: foundation first (`colors`/`theme`/`text_theme` done, then `localizations`, `route`,
`page_scaffold`, `app`), then controls smallest-first, then the "partial ports to tighten" table
(now empty — only the `global_cupertino_localizations` foundation row is left).
`Plumix.Cupertino` may depend only on `Plumix`; anything Cupertino that currently lives in
`Plumix.Material` moves down, never the other way (`docs/ai/INVARIANTS.md` > Package Boundaries).

Exit criteria:

- Every file in `cupertino_ui/lib/src/` (except the *Not listed* section of `docs/CUPERTINO_TODO.md`)
  has a strict C# port with a `// Dart parity source: cupertino_ui/lib/src/<file>.dart` marker and no
  `(reference)`/`(adapted)` qualifier.
- Every Material `.Adaptive` factory composes the Cupertino widget the way Flutter does.
- `CupertinoApp` runs the sample gallery's Cupertino tab through the framework host on desktop.
- Focused tests per control (`src/Plumix.Tests/Cupertino<Control>Tests.cs`) and mirrored demos in
  both samples.

## Backlog Candidates (After M5–M6)

- Shared localization loading (`GlobalMaterialLocalizations`/`GlobalCupertinoLocalizations`, arb → C#).
- Native accessibility bridges per host (see `docs/ai/BACKLOG.md`).
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
