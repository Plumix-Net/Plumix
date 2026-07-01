# Divergence Registry

Single source of truth for intentional behavior/structure divergences from Flutter (Dart).

Rules:

- Every intentional divergence from Flutter behavior must have exactly one row here, added in the same iteration that introduces it (see `docs/ai/PORTING_MODE.md`).
- A row is removed only when the divergence is closed; record the closure in `CHANGELOG.md`.
- Divergences recorded before 2026-07-01 live in archived feature notes under `docs/ai/notes/`. Migration rule: when you touch a control/subsystem, check its notes for still-active divergences, move them into this table, and do not add new divergence text to notes.

Row format:

- **Area/File**: framework file(s) the divergence lives in.
- **Divergence**: what differs from Flutter and why (platform/runtime constraint).
- **Expected delta**: user-visible or behavioral difference.
- **Close condition**: what must land to remove the divergence.

## Active Divergences

| Area/File | Divergence | Expected delta | Close condition |
| --- | --- | --- | --- |
| _(migrate entries from `docs/ai/notes/` as controls are touched)_ | | | |
