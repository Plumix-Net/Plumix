<!--
  Plumix is developed AI-first. Code PRs MUST be produced with a frontier
  coding agent (Claude Opus 4.8, GPT-5.5, or newer). The "AI model used"
  section below is REQUIRED — PRs that leave it empty, or that were written
  by hand / with a weaker model, will be closed without review.
  See CONTRIBUTING.md for details.
-->

## AI model used (required)

<!-- e.g. "Claude Opus 4.8 via Claude Code" or "GPT-5.5 via Codex".
     Docs-only PRs may write "none (docs-only)". -->

Model/agent:

## What & why

<!-- What was ported/fixed and how it maps to Flutter.
     For ports: link the Flutter counterpart (class/file in flutter/flutter).
     For bug fixes: link the issue. -->

## Checklist

- [ ] Change was produced by Claude Opus 4.8 / GPT-5.5 or a newer model (not required for docs-only PRs)
- [ ] The agent followed the repo protocol: `AGENTS.md` → `docs/FRAMEWORK_PLAN.md` → `docs/ai/MODULE_INDEX.md`
- [ ] `dotnet test src/Plumix.Tests/Plumix.Tests.csproj` passes locally
- [ ] New behavior is covered by tests and mapped in `docs/ai/TEST_MATRIX.md`
- [ ] `CHANGELOG.md` has a short entry
- [ ] Ports are closed end-to-end (API/defaults/composition/states/layout/paint/tests), not partial
- [ ] Sample changes (if any) update both `src/Sample/Plumix.Sample` and `dart_sample`, and `docs/ai/PARITY_MATRIX.md`
- [ ] Architecture invariants respected (`docs/ai/INVARIANTS.md`); intentional divergences documented in `docs/ai/DIVERGENCES.md`
