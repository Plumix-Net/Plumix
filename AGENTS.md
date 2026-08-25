# AGENTS.md

This file defines expectations for coding agents working in this repository. It is the single
instruction source for every agent — Claude Code (`CLAUDE.md` only imports this file), Codex, and
anything else. Tool-specific wiring lives in `.claude/` and is optional convenience; nothing
normative may live only there.

## Project Snapshot

- Platform: .NET 10
- UI stack: Avalonia
- Purpose: Flutter-like widget/rendering layer implemented in C#
- Main library: `src/Plumix`
- Example hosts: `src/Sample/*`
- Main solution: `src/Plumix.sln`

## Project Vision

- Build a Flutter-like framework in C# where `Widget`/`Element`/`RenderObject` concepts stay close to Flutter.
- Keep render object behavior and APIs close enough to Flutter to simplify rewriting controls from Dart to C#.
- Reuse Avalonia mainly as platform infrastructure: windowing/app host, lifecycle, input plumbing, and drawing backend abstractions.
- Keep layout/paint logic in the new framework, not in Avalonia control implementations (except thin host adapters).

## Expected End State (Definition of Done)

1. Applications are composed through Flutter-like widgets/state/lifecycle and rendered by framework-owned render objects.
2. Core rendering behavior lives in `src/Plumix/Rendering` and related framework layers, with minimal Avalonia-specific UI logic.
3. Desktop sample runs a widget app through `WidgetHost` (or an equivalent framework host), not only a render demo window.
4. Core primitives (box, flex, text, animation tick flow) are stable enough for straightforward Dart-to-C# control rewrites.
5. Project docs stay aligned with architecture boundaries and migration goals.

## Repository Map

- `src/Plumix`: core framework (`Foundation`, `Widgets`, `Rendering`, `UI`, scheduler/ticker pipeline).
- `src/Sample/Plumix.Sample`: shared sample app/widgets.
- `dart_sample`: reference sample app on real Flutter (Dart), kept in lockstep with `src/Sample/Plumix.Sample`.
- `src/Sample/Plumix.Desktop`: desktop entry point.
- `src/Sample/Plumix.Browser`: WebAssembly host.
- `src/Sample/Plumix.Android`: Android host.
- `src/Sample/Plumix.iOS`: iOS host.

## Progress Source of Truth

- Historical shipped changes (one line per change; detail is in `git log`): `CHANGELOG.md`
- Current status + global roadmap: `docs/FRAMEWORK_PLAN.md`
- Current milestone work list (Cupertino port, per-file status): `docs/CUPERTINO_TODO.md`
- Open work not tied to one control (upstream re-port deltas, host gaps, blocked iterations): `docs/ai/BACKLOG.md`
- Module entry points by task: `docs/ai/MODULE_INDEX.md`
- Flutter file -> C# files/tests/demos (generated; also lists ports whose marker says `(reference)`/`(approximate)`): `docs/ai/PORT_MAP.md`
- Non-negotiable behavior rules (architecture, package boundaries, versioning): `docs/ai/INVARIANTS.md`
- Mandatory Dart-to-C# porting workflow: `docs/ai/PORTING_MODE.md`
- Step-by-step execution of that workflow: `docs/ai/PORT_PLAYBOOK.md`
- Reading large Dart sources without exhausting context: `docs/ai/DART_SPEC_PROTOCOL.md`
- Intentional divergences from Flutter: `docs/ai/DIVERGENCES.md`
- Sample parity tracker: `docs/ai/PARITY_MATRIX.md`
- Feature-to-tests map: `docs/ai/TEST_MATRIX.md`
- There is no per-iteration journal. History lives in git; anything still open lives in the trackers above. Do not create `docs/ai/notes/`-style files.
- When task scope changes framework behavior, update tracking docs so agents can infer:
  - what is already done,
  - what remains,
  - what direction has priority now.
- `CHANGELOG.md` entries are one line each (rules at the top of that file). When a release is tagged, collapse `[Unreleased]` into a few bullets under the version heading; never split the changelog into rotation files.

## Context Budget Protocol (For AI Agents)

1. Start with read order: `AGENTS.md` -> `docs/FRAMEWORK_PLAN.md` -> `docs/ai/MODULE_INDEX.md` -> targeted tests -> targeted implementation files. For a port, `docs/ai/PORT_PLAYBOOK.md` replaces steps 2-7 of this protocol.
2. Default scope for Dart-to-C# parity requests: close one control end-to-end in one request (`API/defaults/composition/states/layout/paint/tests`), not a sequence of micro-fixes.
3. Prefer entering unfamiliar subsystems through their tests (`docs/ai/TEST_MATRIX.md`); open implementation hotspot files (`Widgets/Scroll.cs`, `Rendering/Sliver.cs`, `Widgets/Navigation.cs`, `Widgets/Framework.Element.cs`, `SemanticsTreeTests.cs`) only when the task explicitly requires them.
3a. `docs/ai/PORT_MAP.md`, `docs/ai/PARITY_MATRIX.md`, `docs/ai/TEST_MATRIX.md` and `docs/ai/DIVERGENCES.md` are lookup tables — grep them for the control/subsystem you are touching, never read them end-to-end.
3b. Flutter Dart sources over ~800 lines must go through `docs/ai/DART_SPEC_PROTOCOL.md` (separate context, dense spec back) instead of being read into the working context. `input_decorator.dart` alone is 6107 lines.
4. Expand context proactively when needed to finish the current control in the same request; do not stop at partial parity unless blocked by a concrete missing primitive.
5. If an iteration ends blocked (unclosed parity with a concrete blocker), add a row to `docs/ai/BACKLOG.md` (what remains, next step, blocker); a divergence gets a row in `docs/ai/DIVERGENCES.md`. Routine closed iterations need only `CHANGELOG.md`, the `docs/CUPERTINO_TODO.md` row and matrix updates.
6. If sample behavior changes, update both `src/Sample/Plumix.Sample` and `dart_sample` in the same iteration and reflect status in `docs/ai/PARITY_MATRIX.md` (scope per `docs/ai/INVARIANTS.md` Sample Parity).
7. Before finishing, update docs with minimal deltas only (`CHANGELOG.md`, `docs/FRAMEWORK_PLAN.md`, and relevant `docs/ai/*` files) and keep `dotnet test src/Plumix.Tests/Plumix.Tests.csproj` green.

## Environment Requirements

- .NET SDK 10 preview (projects target `net10.0` and platform-specific TFMs).
- Avalonia tooling/workloads for browser/mobile targets where applicable.
- Python 3 for `scripts/generate_port_map.py` and `scripts/generate_keyboard_keys.py`.

## Agent Tooling

Everything normative is in this file and `docs/ai/*`, so any agent can follow it. `.claude/` adds
convenience for Claude Code only:

- `/port [control]` — runs `docs/ai/PORT_PLAYBOOK.md` end-to-end, picking the control if none given.
- `/finish-port` — runs the four gates and the tracking-doc updates for whatever is in the tree.
- `dart-spec` subagent — runs `docs/ai/DART_SPEC_PROTOCOL.md` in a throwaway context.
- A `PostToolUse` hook checks line length on every edited `.cs` file.

Codex and other agents get the same behavior by naming the doc: "follow `docs/ai/PORT_PLAYBOOK.md`"
does what `/port` does, and the spec protocol works inline when subagents are unavailable.

## Local Reference Paths

All are gitignored symlinks in the repository root, so every reference in docs and code can use a
stable relative path. Create them once after cloning:

```bash
ln -s /path/to/your/flutter flutter-src     # Flutter checkout (see pin below)
ln -s ~/.pub-cache/hosted/pub.dev/material_ui-1.0.0 material-ui-src    # Material pub package
ln -s ~/.pub-cache/hosted/pub.dev/cupertino_ui-1.0.0 cupertino-ui-src  # Cupertino pub package
ln -s /path/to/your/Avalonia avalonia-src   # optional, for host/backend questions
```

- `material-ui-src` / `cupertino-ui-src` — source of truth for Material/Cupertino controls. Flutter
  extracted these libraries into the `material_ui`/`cupertino_ui` pub packages (developed in
  `flutter/packages`); the copies inside the Flutter SDK are frozen leftovers. Controls:
  `<pkg>-src/lib/src/`; Flutter's own tests ship with the package: `<pkg>-src/test/` — the most
  reliable record of exact defaults and contractual behavior; read them during ports.
- `flutter-src` — source of truth for everything else (`widgets`, `rendering`, `painting`,
  `gestures`, `semantics`, ...): `flutter-src/packages/flutter/lib/src/<library>/`, tests under
  `flutter-src/packages/flutter/test/<library>/`.
- `avalonia-src` — Avalonia source, host/platform questions only.

**Pinned Flutter revision: 3.47.0 (`4cf24164269`, `flutter-3.47-candidate.0`). Pinned packages:
`material_ui` 1.0.0, `cupertino_ui` 1.0.0, `intl` 0.20.3** (what `dart_sample` resolves; at these pins the package
sources are code-identical to the SDK's frozen `src/material`/`src/cupertino` copies, modulo doc
comments and constructor-style modernization). Parity is defined against these pins. Material
defaults change between releases, so a port validated against a different checkout is not validated.
When a pin moves, update this line, re-point the symlink(s), and re-run
`python3 scripts/generate_port_map.py` (it flags markers whose Dart file no longer exists) and
`python3 scripts/generate_keyboard_keys.py` (it regenerates the logical/physical key tables from
Flutter's own generated `keyboard_key.g.dart`) and `python3 scripts/generate_material_colors.py`
(it regenerates the Material palette from `colors.dart`) and
`python3 scripts/generate_material_icons.py` (it regenerates the Material icon catalog from `icons.dart`
and re-vendors the matching `material_fonts` artifact) and `python3 scripts/generate_intl_data.py`
(it re-reads the CLDR snapshot from `flutter_localizations` and `~/.pub-cache/.../intl-<pin>`) and
`python3 scripts/generate_localizations.py` (it re-transliterates Flutter's generated locale bundles).

## Common Commands

Run from repository root:

```bash
dotnet restore src/Plumix.sln
dotnet build src/Plumix.Ci.slnf -c Debug          # what CI builds; includes the F# DSL projects
dotnet test src/Plumix.Tests/Plumix.Tests.csproj  # ~25 s for 4791 tests — always run it
scripts/check_line_length.sh                      # 120-char rule on new/edited lines
python3 scripts/generate_port_map.py              # regenerate docs/ai/PORT_MAP.md
python3 scripts/generate_keyboard_keys.py         # regenerate src/Plumix/UI/KeyboardKey.g.cs
python3 scripts/generate_material_colors.py       # regenerate src/Plumix.Material/Colors.g.cs
python3 scripts/generate_cupertino_icons.py       # regenerate Cupertino icon catalog + font asset
python3 scripts/generate_material_icons.py        # regenerate Material icon catalog + font asset
python3 scripts/generate_intl_data.py             # regenerate the CLDR snapshot the intl subset uses
python3 scripts/generate_localizations.py         # regenerate the Cupertino/widgets locale bundles
dotnet run --project src/Sample/Plumix.Desktop/Plumix.Desktop.csproj
dotnet run --project src/Sample/Plumix.Browser/Plumix.Browser.csproj
```

`src/Plumix.Ci.slnf` excludes the Browser/Android/iOS hosts (they need workloads CI does not have);
build `src/Plumix.sln` locally when you touch those hosts.

Platform-specific builds:

```bash
dotnet build src/Sample/Plumix.Android/Plumix.Android.csproj -c Debug
dotnet build src/Sample/Plumix.iOS/Plumix.iOS.csproj -c Debug
```

## Change Guidelines

1. Keep core API and behavior changes focused in `src/Plumix` unless sample host updates are required.
2. Respect architecture boundaries: `Widget` -> `Element` -> `RenderObject` -> platform adapter.
3. Keep render-object semantics and naming close to Flutter unless there is a clear, documented reason to diverge.
4. Use Avalonia primarily for host/platform integration and low-level drawing backend; avoid moving framework behavior into Avalonia controls.
5. Preserve lifecycle contracts (`CreateElement`, mount/update/rebuild flow, render object attachment).
6. Keep nullability correctness (`Nullable` is enabled). Nullable warnings are promoted to errors in `src/Directory.Build.props`, so they fail the build.
7. Code style: use explicit types for primitives and `string` (`double`, `int`, `bool`, `string`, `char`, `byte`, `long`, `float`, `decimal`, ...); keep `var` only for complex/reference types whose type is obvious from the right-hand side. See `docs/ai/INVARIANTS.md` (Code Style). Emit this correctly on first pass — `EnforceCodeStyleInBuild` makes IDE0008 a **build error**, so a violation breaks the build rather than surfacing in review.
8. Max line length is 120 characters (`.editorconfig` `max_line_length`), checked by `scripts/check_line_length.sh` on new/edited lines only; do not mass-reformat untouched code. Wrap long argument lists, chained calls, and conditions instead of exceeding it.
8a. Every framework file carries a `// Dart parity source:` header marker naming its Dart origin: `material_ui/lib/src/<file>.dart` or `cupertino_ui/lib/src/<file>.dart` for the extracted design packages, `flutter/packages/flutter/lib/src/<library>/<file>.dart` (or `flutter/packages/flutter_localizations/lib/src/<file>.dart`) for everything else. Keep it on new files — `docs/ai/PORT_MAP.md` is generated from these markers, and a missing one drops the file out of the map. C#-only infrastructure states that in a header comment instead.
9. Avoid broad dependency/framework upgrades unless explicitly requested.
10. Demo feature/route/page-structure updates in `src/Sample/Plumix.Sample` must be mirrored in `dart_sample` in the same change; host glue is exempt (see `docs/ai/INVARIANTS.md`, Sample Parity).

## Porting Workflow (Mandatory)

0. Execution order for a port is `docs/ai/PORT_PLAYBOOK.md` — including how to pick the control when the request does not name one. In Claude Code it is wrapped as `/port`; in other agents, follow the file directly.
1. For control/widget ports, treat Flutter Dart source as source of truth and follow `docs/ai/PORTING_MODE.md`.
2. Default mode is strict `1:1` structure/behavior port, not approximation.
3. Default delivery unit for parity work is one complete control per request; avoid splitting one control into many token-level follow-ups (for example geometry/colors/overlay in separate requests) unless explicitly requested or blocked by missing primitives.
4. If a required primitive is missing in C#, add/fix the primitive first, then continue and close the control parity pass in the same iteration whenever feasible.
5. Any unavoidable divergence must be documented in docs/changelog in the same iteration.

## Validation Checklist

1. Build what CI builds: `dotnet build src/Plumix.Ci.slnf -c Debug`. `Plumix.Tests` does not reference `Plumix.FSharp`/`Plumix.Elmish`, so a green test run alone does not prove the F# DSL still compiles against a changed public API.
2. For UI behavior changes, run desktop sample and verify startup/rendering through the framework widget host path.
3. For rendering changes, verify that layout/paint behavior is executed by framework render objects.
4. For browser/mobile changes, build the affected sample project(s).
5. For sample changes, validate both C# sample (`src/Sample/Plumix.Sample`) and Dart sample (`dart_sample`) are kept in parity.
6. Automated tests live in `src/Plumix.Tests`; add focused coverage when introducing non-trivial logic.
7. For control parity tasks, verify parity-critical coverage (`API/defaults/states/layout/paint`) for that control before closing the request.
