# Contributing to Plumix

Thanks for your interest in Plumix! This project is developed **AI-first**: the codebase is written and maintained by AI coding agents working under maintainer guidance, and the repository is structured around that workflow (`AGENTS.md`, `docs/ai/*`).

## AI-first contribution policy

If you want to contribute **code**, use a frontier coding agent — **Claude Opus 4.8, GPT-5.5, or a newer/stronger model** — driven through an agentic tool (Claude Code, Codex, or similar).

Why this requirement exists:

- The repo ships an agent-oriented context system (`AGENTS.md`, `docs/FRAMEWORK_PLAN.md`, `docs/ai/MODULE_INDEX.md`) that these models are expected to read and follow. It encodes architecture boundaries, porting rules, and doc-update duties that manual contributions tend to miss.
- Plumix ports Flutter semantics to C#. The mandatory workflow (`docs/ai/PORTING_MODE.md`) requires closing a control end-to-end — API, defaults, composition, states, layout, paint, tests — in one iteration. Frontier models handle this scope reliably; weaker models and hand-written partial ports create parity drift that is expensive to review.
- Consistency: the entire codebase follows one style and one architectural idiom. Keeping the same class of model in the loop keeps the code homogeneous.

Issues, bug reports, and discussions are welcome from everyone — no AI required.

## Ways to contribute

### 1. Close a gap (port something that's missing)

- **Start here: [`docs/MATERIAL_TODO.md`](docs/MATERIAL_TODO.md)** — the up-for-grabs list of Material widgets not yet ported, with size estimates and Flutter source pointers. Claim an item via a `Claim: <Widget>` issue before starting.
- Beyond Material: pick an open item from [`docs/FRAMEWORK_PLAN.md`](docs/FRAMEWORK_PLAN.md) (roadmap) or an unported control in [`docs/ai/PARITY_MATRIX.md`](docs/ai/PARITY_MATRIX.md).
- Follow [`docs/ai/PORTING_MODE.md`](docs/ai/PORTING_MODE.md) — close the control end-to-end in one PR, don't submit partial parity.
- Respect [`docs/ai/INVARIANTS.md`](docs/ai/INVARIANTS.md) (architecture and package boundaries are non-negotiable).
- If sample behavior changes, update both `src/Sample/Plumix.Sample` and `dart_sample` in the same PR.

### 2. Fix a bug

- Reference the issue you're fixing (file one first if it doesn't exist).
- Add or extend a test that fails without the fix — see [`docs/ai/TEST_MATRIX.md`](docs/ai/TEST_MATRIX.md) for where tests for each subsystem live.
- When behavior differs from Flutter, Flutter's behavior is the spec unless [`docs/ai/DIVERGENCES.md`](docs/ai/DIVERGENCES.md) documents an intentional divergence.

### 3. Report a bug

Open a GitHub issue with:

- A minimal reproducing widget tree (C# snippet that can be dropped into the sample app).
- Expected behavior — ideally the equivalent Flutter (Dart) snippet and how Flutter renders it.
- Actual behavior (screenshot for visual issues).
- Environment: OS, .NET SDK version, Plumix package versions (or commit hash).

### 4. Propose a feature

Open an issue first and describe the Flutter counterpart (if any). Features that keep parity with Flutter's `Widget`/`Element`/`RenderObject` model are prioritized; features that push framework logic into Avalonia controls will be declined (see `docs/ai/INVARIANTS.md`).

### 5. Improve docs and samples

Docs PRs are welcome. Keep `dart_sample` (real Flutter) and `src/Sample/Plumix.Sample` in lockstep, and reflect sample changes in `docs/ai/PARITY_MATRIX.md`.

## Getting started

Requirements: **.NET SDK 10** (projects target `net10.0`); Avalonia workloads for browser/mobile targets if you build those hosts.

```bash
git clone https://github.com/Plumix-Net/Plumix.git
cd Plumix

dotnet restore src/Plumix.sln
dotnet build src/Plumix.sln -c Debug

# run the desktop sample
dotnet run --project src/Sample/Plumix.Desktop/Plumix.Desktop.csproj

# run the test suite (must be green before every PR)
dotnet test src/Plumix.Tests/Plumix.Tests.csproj
```

You also need a local Flutter checkout at the pinned revision (see `AGENTS.md` → Local Reference Paths) symlinked as `flutter-src` in the repo root — it is the spec for every port:

```bash
ln -s /path/to/your/flutter flutter-src
```

Point your agent at the repository and let it read, in order: `AGENTS.md` → `docs/FRAMEWORK_PLAN.md` → `docs/ai/MODULE_INDEX.md` → the tests for the subsystem you're touching. This is the same protocol the maintainer's agents use.

For a port specifically, one instruction is enough: **"follow `docs/ai/PORT_PLAYBOOK.md`"** (add a control name, or let it pick one). In Claude Code that is `/port`. The playbook covers target selection, reading large Dart sources without exhausting context (`docs/ai/DART_SPEC_PROTOCOL.md`), the port itself, tests, samples, validation, and the tracking-doc updates.

## Pull request checklist

1. Branch from `main`; one logical change per PR.
2. All four gates pass locally (CI runs the same in Release):

```bash
dotnet build src/Plumix.Ci.slnf -c Debug
dotnet test src/Plumix.Tests/Plumix.Tests.csproj
scripts/check_line_length.sh
python3 scripts/generate_port_map.py
```
3. New behavior is covered by tests and mapped in `docs/ai/TEST_MATRIX.md`.
4. `CHANGELOG.md` has a short entry (a few lines, no test-inventory prose).
5. Tracking docs updated where relevant: `docs/FRAMEWORK_PLAN.md`, `docs/ai/PARITY_MATRIX.md`, `docs/ai/DIVERGENCES.md`.
6. Fill in the PR template, including the required **"AI model used"** section (e.g. "Claude Opus 4.8 via Claude Code"), and summarize what was ported/fixed and how it maps to Flutter.

**Enforcement:** the model declaration is mandatory. PRs that leave it empty, or that were written by hand or with a model below the Claude Opus 4.8 / GPT-5.5 tier, are closed without review (docs-only PRs are exempt). PRs that break architecture invariants, submit partial ports, or arrive without tests will be sent back for another agent iteration rather than hand-patched in review.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
