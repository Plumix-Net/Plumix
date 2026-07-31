# Port Playbook

The executable version of `docs/ai/PORTING_MODE.md`: the exact sequence for closing one control
Dart-to-C# in a single request, including picking the control when the request does not name one.

Tool-agnostic. Claude Code wraps it as `/port`; in Codex or any other agent, say
"follow docs/ai/PORT_PLAYBOOK.md" (optionally with a control name) and run the steps in order.

## Step 0 — Pick the target (skip if the request names one)

Choose without asking. Take the first rule that yields a candidate:

1. `docs/MATERIAL_TODO.md` > *Open controls*, smallest size first (**S** before **M** before **L**).
   **L** items say "align with maintainer first" — pick one only if nothing smaller is open, and open
   the request by stating the design decision you are making.
2. If *Open controls* is empty: `docs/ai/DIVERGENCES.md` > *Active Divergences*, pick a row whose
   close condition no longer needs a missing primitive.
3. If that is empty too: `docs/ai/PORT_MAP.md`, pick a control whose markers say `(approximate)` or
   `(reference)` and tighten it to a strict port.
4. If none of the above: `docs/FRAMEWORK_PLAN.md` > current milestone.

Announce the pick in one line with the reason, then proceed. Do not stop for confirmation — an
unconfirmed pick that follows this order is the expected behavior.

## Step 1 — Budget check

Look at the Dart file size first:

```bash
wc -l flutter-src/packages/flutter/lib/src/material/<x>.dart
```

Over ~800 lines: extract the spec in a separate context per `docs/ai/DART_SPEC_PROTOCOL.md` before
opening anything else. Under that: read the Dart directly.

Then read only what the task needs: `docs/ai/PORT_MAP.md` row for the control, `docs/ai/INVARIANTS.md`,
and the subsystem entry points from `docs/ai/MODULE_INDEX.md`. Do **not** read `PARITY_MATRIX.md`,
`TEST_MATRIX.md` or `DIVERGENCES.md` end-to-end — grep them for the control name.

## Step 2 — Establish the spec

Produce or receive the spec (API, theme resolution, composition, states, layout, paint, asserted
behaviors, required primitives). Everything downstream is checked against this, not against the Dart
file, so it must be complete before any code is written.

## Step 3 — Land missing primitives first

If the spec's *Primitives required* lists anything missing, implement it in `src/Plumix` (or
`src/Plumix.Cupertino`) **before** the control. Never work around a missing primitive inside the
control — that is the single most common way a port silently diverges (`PORTING_MODE.md`).

Respect the dependency direction from `INVARIANTS.md`: core never references Material.

## Step 4 — Port the control

- Widget + its `*Theme`/`*ThemeData` pair, following the Dart structure 1:1: same class split, same
  member order, same composition nesting.
- Defaults come from the spec verbatim, including both `useMaterial3` branches.
- Keep the `// Dart parity source: flutter/packages/flutter/lib/src/<library>/<x>.dart` header marker
  on every new file — `docs/ai/PORT_MAP.md` is generated from it.
- Style is enforced by the compiler now: explicit types for built-ins (IDE0008 is an error) and
  120-char lines (`scripts/check_line_length.sh`). Emit it right the first time.

## Step 5 — Tests

Add focused tests in `src/Plumix.Tests/Material<Control>Tests.cs` (or the subsystem's file) covering
every line of the spec's *Behaviors asserted by Flutter's own tests*, plus defaults, interaction
states, and parity-critical layout/paint. A behavior Flutter tests and Plumix does not is a parity
gap, not a test-coverage preference.

## Step 6 — Samples

If the control is user-visible, add or extend a demo page in **both** `src/Sample/Plumix.Sample` and
`dart_sample` in this same iteration (`INVARIANTS.md` > Sample Parity). Host glue is exempt.

## Step 7 — Validate

```bash
dotnet build src/Plumix.Ci.slnf -c Debug     # includes the F# DSL — Plumix.Tests does not
dotnet test src/Plumix.Tests/Plumix.Tests.csproj
scripts/check_line_length.sh
python3 scripts/generate_port_map.py
```

All four must be clean. The test suite runs in ~15 s; there is no reason to skip it.

## Step 8 — Close the paperwork

Minimal deltas only:

- `CHANGELOG.md` — a few lines. Prefix `Breaking:` if a public default or behavior changed, even when
  the change moves *toward* Flutter (`INVARIANTS.md` > Versioning).
- `docs/MATERIAL_TODO.md` — delete the row you closed.
- `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md` — update the affected rows.
- `docs/ai/DIVERGENCES.md` — add a row for any unavoidable divergence; remove rows you closed.
- `docs/FRAMEWORK_PLAN.md` — only if milestone status changed. Do not append completion notes; it has
  a 10 KB budget.
- A note in `docs/ai/notes/` **only** if the iteration ended blocked or introduced a divergence.

## Definition of done

One control, closed end-to-end: API, defaults, composition, states, layout, paint, tests, samples,
docs — with build, tests, line-length and port-map checks green. If a hard blocker prevents closing,
finish everything else, write the note with the concrete next step, and say plainly what is left.
