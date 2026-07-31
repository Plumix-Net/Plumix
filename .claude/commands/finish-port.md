---
description: Run the validation gates and close the tracking-doc paperwork for the current change
allowed-tools: Read, Grep, Glob, Edit, Write, Bash
---

Close out the change currently in the working tree.

1. Run all four gates and fix anything they report:

```bash
dotnet build src/Plumix.Ci.slnf -c Debug
dotnet test src/Plumix.Tests/Plumix.Tests.csproj
scripts/check_line_length.sh
python3 scripts/generate_port_map.py
```

2. Read `git diff` and `git status` to see what actually changed, then update the tracking docs per
Step 8 of `docs/ai/PORT_PLAYBOOK.md` — minimal deltas, no restating rules that live elsewhere:

- `CHANGELOG.md` — a few lines; `Breaking:` prefix if a public default or behavior changed.
- `docs/MATERIAL_TODO.md` — delete rows for controls now closed.
- `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md` — the affected rows only.
- `docs/ai/DIVERGENCES.md` — add rows for new divergences, remove rows you closed.
- `docs/FRAMEWORK_PLAN.md` — only on milestone status change (10 KB budget, no completion notes).
- `docs/ai/notes/` — only if the iteration ended blocked or introduced a divergence.

3. Report what changed, which gates passed, and anything left open. Do not commit unless asked.
