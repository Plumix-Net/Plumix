# AGENTS.md

This file defines expectations for coding agents working in this repository.

## Project Snapshot

- Platform: .NET 10
- UI stack: Avalonia
- Purpose: Flutter-like widget/rendering layer implemented in C#
- Main library: `src/Flutter`
- Example hosts: `src/Sample/*`
- Main solution: `src/Flutter.Net.sln`

## Project Vision

- Build a Flutter-like framework in C# where `Widget`/`Element`/`RenderObject` concepts stay close to Flutter.
- Keep render object behavior and APIs close enough to Flutter to simplify rewriting controls from Dart to C#.
- Reuse Avalonia mainly as platform infrastructure: windowing/app host, lifecycle, input plumbing, and drawing backend abstractions.
- Keep layout/paint logic in the new framework, not in Avalonia control implementations (except thin host adapters).

## Expected End State (Definition of Done)

1. Applications are composed through Flutter-like widgets/state/lifecycle and rendered by framework-owned render objects.
2. Core rendering behavior lives in `src/Flutter/Rendering` and related framework layers, with minimal Avalonia-specific UI logic.
3. Desktop sample runs a widget app through `WidgetHost` (or an equivalent framework host), not only a render demo window.
4. Core primitives (box, flex, text, animation tick flow) are stable enough for straightforward Dart-to-C# control rewrites.
5. Project docs stay aligned with architecture boundaries and migration goals.

## Repository Map

- `src/Flutter`: core framework (`Foundation`, `Widgets`, `Rendering`, `UI`, scheduler/ticker pipeline).
- `src/Sample/Flutter.Net`: shared sample app/widgets.
- `dart_sample`: reference sample app on real Flutter (Dart), kept in lockstep with `src/Sample/Flutter.Net`.
- `src/Sample/Flutter.Net.Desktop`: desktop entry point.
- `src/Sample/Flutter.Net.Browser`: WebAssembly host.
- `src/Sample/Flutter.Net.Android`: Android host.
- `src/Sample/Flutter.Net.iOS`: iOS host.

## Progress Source of Truth

- Historical shipped changes: `CHANGELOG.md`
- Current status + global roadmap: `docs/FRAMEWORK_PLAN.md`
- Module entry points by task: `docs/ai/MODULE_INDEX.md`
- Non-negotiable behavior rules: `docs/ai/INVARIANTS.md`
- Mandatory Dart-to-C# porting workflow: `docs/ai/PORTING_MODE.md`
- Sample parity tracker: `docs/ai/PARITY_MATRIX.md`
- Feature-to-tests map: `docs/ai/TEST_MATRIX.md`
- Iteration planning template: `docs/ai/FEATURE_TEMPLATE.md`
- When task scope changes framework behavior, update tracking docs so agents can infer:
  - what is already done,
  - what remains,
  - what direction has priority now.

## Context Budget Protocol (For AI Agents)

1. Start with read order: `AGENTS.md` -> `docs/FRAMEWORK_PLAN.md` -> `docs/ai/MODULE_INDEX.md` -> targeted tests -> targeted implementation files.
2. Default scope for Dart-to-C# parity requests: close one control end-to-end in one request (`API/defaults/composition/states/layout/paint/tests`), not a sequence of micro-fixes.
3. Initial context budget: up to 20 files per task (recommended `12-20` for full-control parity work).
4. Do not open large hotspot files (`Widgets/Scroll.cs`, `Rendering/Sliver.cs`, `Widgets/Navigation.cs`, `Widgets/Framework.Element.cs`, `SemanticsTreeTests.cs`) unless the task explicitly requires them.
5. Expand context proactively when needed to finish the current control in the same request; do not stop at partial parity unless blocked by a concrete missing primitive.
6. For every non-trivial feature or control-parity pass, create/update a task note based on `docs/ai/FEATURE_TEMPLATE.md`.
7. If sample behavior changes, update both `src/Sample/Flutter.Net` and `dart_sample` in the same iteration and reflect status in `docs/ai/PARITY_MATRIX.md`.
8. Before finishing, update docs with minimal deltas only (`CHANGELOG.md`, `docs/FRAMEWORK_PLAN.md`, and relevant `docs/ai/*` files).

## Environment Requirements

- .NET SDK 10 preview (projects target `net10.0` and platform-specific TFMs).
- Avalonia tooling/workloads for browser/mobile targets where applicable.

## Local Reference Paths

- Flutter source: `/Users/egorozh/Documents/flutter/flutter`
- Avalonia source: `../Avalonia` (resolved: `/Users/egorozh/Flutter.Net.Local/Avalonia`)

## Common Commands

Run from repository root:

```bash
dotnet restore src/Flutter.Net.sln
dotnet build src/Flutter.Net.sln -c Debug
dotnet run --project src/Sample/Flutter.Net.Desktop/Flutter.Net.Desktop.csproj
dotnet run --project src/Sample/Flutter.Net.Browser/Flutter.Net.Browser.csproj
```

Platform-specific builds:

```bash
dotnet build src/Sample/Flutter.Net.Android/Flutter.Net.Android.csproj -c Debug
dotnet build src/Sample/Flutter.Net.iOS/Flutter.Net.iOS.csproj -c Debug
```

## Change Guidelines

1. Keep core API and behavior changes focused in `src/Flutter` unless sample host updates are required.
2. Respect architecture boundaries: `Widget` -> `Element` -> `RenderObject` -> platform adapter.
3. Keep render-object semantics and naming close to Flutter unless there is a clear, documented reason to diverge.
4. Use Avalonia primarily for host/platform integration and low-level drawing backend; avoid moving framework behavior into Avalonia controls.
5. Preserve lifecycle contracts (`CreateElement`, mount/update/rebuild flow, render object attachment).
6. Keep nullability correctness (`Nullable` is enabled) and avoid introducing nullable warnings.
7. Avoid broad dependency/framework upgrades unless explicitly requested.
8. Any behavior/structure update in `src/Sample/Flutter.Net` must be adapted 100% in `dart_sample` in the same change (feature parity, route parity, and page/module structure parity).

## Porting Workflow (Mandatory)

1. For control/widget ports, treat Flutter Dart source as source of truth and follow `docs/ai/PORTING_MODE.md`.
2. Default mode is strict `1:1` structure/behavior port, not approximation.
3. Default delivery unit for parity work is one complete control per request; avoid splitting one control into many token-level follow-ups (for example geometry/colors/overlay in separate requests) unless explicitly requested or blocked by missing primitives.
4. If a required primitive is missing in C#, add/fix the primitive first, then continue and close the control parity pass in the same iteration whenever feasible.
5. Any unavoidable divergence must be documented in docs/changelog in the same iteration.

## Validation Checklist

1. Build the full solution: `dotnet build src/Flutter.Net.sln -c Debug`.
2. For UI behavior changes, run desktop sample and verify startup/rendering through the framework widget host path.
3. For rendering changes, verify that layout/paint behavior is executed by framework render objects.
4. For browser/mobile changes, build the affected sample project(s).
5. For sample changes, validate both C# sample (`src/Sample/Flutter.Net`) and Dart sample (`dart_sample`) are kept in parity.
6. Automated tests live in `src/Flutter.Tests`; add focused coverage when introducing non-trivial logic.
7. For control parity tasks, verify parity-critical coverage (`API/defaults/states/layout/paint`) for that control before closing the request.
