# Port Playbook

The executable version of `docs/ai/PORTING_MODE.md`: the exact sequence for closing one control
Dart-to-C# in a single request, including picking the control when the request does not name one.

Tool-agnostic. Claude Code wraps it as `/port`; in Codex or any other agent, say
"follow docs/ai/PORT_PLAYBOOK.md" (optionally with a control name) and run the steps in order.

## Step 0 — Pick the target (skip if the request names one)

Choose without asking. Take the first rule that yields a candidate:

1. `docs/CUPERTINO_TODO.md` > *Foundation* rows top to bottom (they unblock everything else), then
   *Open controls* smallest size first (**S** before **M** before **L**), then *Partial ports to
   tighten*. Rows marked "align first" are picked only when nothing else is open, and the request
   opens by stating the design decision being made.
2. If that file has nothing left: `docs/ai/DIVERGENCES.md` > *Active Divergences*, pick a row whose
   close condition no longer needs a missing primitive.
3. If that is empty too: `docs/ai/PORT_MAP.md` > *Ports with a qualified marker*, pick a file marked
   `(approximate)`/`(reference)`/`(adapted)`/subset and tighten it to a strict port.
4. If none of the above: `docs/ai/BACKLOG.md`, then `docs/FRAMEWORK_PLAN.md` > current milestone.

Announce the pick in one line with the reason, then proceed. Do not stop for confirmation — an
unconfirmed pick that follows this order is the expected behavior.

## Step 1 — Budget check

Look at the Dart file size first:

```bash
wc -l cupertino-ui-src/lib/src/<x>.dart   # material-ui-src/lib/src/ for Material;
                                          # flutter-src/packages/flutter/lib/src/<library>/ otherwise
```

Over ~800 lines: extract the spec in a separate context per `docs/ai/DART_SPEC_PROTOCOL.md` before
opening anything else. Under that: read the Dart directly.

Then read only what the task needs: `docs/ai/PORT_MAP.md` row for the control, `docs/ai/INVARIANTS.md`,
and the subsystem entry points from `docs/ai/MODULE_INDEX.md`. Do **not** read `PARITY_MATRIX.md`,
`TEST_MATRIX.md`, `DIVERGENCES.md` or `PORT_MAP.md` end-to-end — grep them for the control name.

If a C# file for the control already exists (Cupertino "partial" rows, qualified markers), diff it
against the Dart file top to bottom before writing anything: existing C# is evidence of what was
done, not a spec, and may be an adapted subset.

## Step 2 — Establish the spec

Produce or receive the spec (API, theme resolution, composition, states, layout, paint, asserted
behaviors, required primitives). Everything downstream is checked against this, not against the Dart
file, so it must be complete before any code is written.

## Step 3 — Land missing primitives first

If the spec's *Primitives required* lists anything missing, implement it in `src/Plumix` (or
`src/Plumix.Cupertino` for Cupertino-only pieces) **before** the control. Never work around a missing
primitive inside the control — that is the single most common way a port silently diverges
(`PORTING_MODE.md`).

Respect the dependency direction from `INVARIANTS.md`: core never references Cupertino or Material,
Cupertino never references Material. If a Cupertino port needs code that currently sits in
`Plumix.Material`, move it down into `Plumix.Cupertino` and make Material reference it.

## Step 4 — Port the control

- Widget + its `*Theme`/`*ThemeData` pair, following the Dart structure 1:1: same class split, same
  member order, same composition nesting.
- Defaults come from the spec verbatim (for Material, both `useMaterial3` branches; for Cupertino,
  both brightness resolutions of every `CupertinoDynamicColor`).
- Keep the `// Dart parity source:` header marker on every new file (`cupertino_ui/lib/src/<x>.dart`,
  `material_ui/lib/src/<x>.dart`, or `flutter/packages/flutter/lib/src/<library>/<x>.dart`) —
  `docs/ai/PORT_MAP.md` is generated from it. Add a `(reference)`/`(adapted)` qualifier only if the
  port is knowingly not strict, and say why in `docs/ai/DIVERGENCES.md`; when you tighten a file to a
  strict port, remove the qualifier.
- Style is enforced by the compiler: explicit types for built-ins (IDE0008 is an error) and
  120-char lines (`scripts/check_line_length.sh`). Emit it right the first time.

## Step 5 — Tests

Add focused tests in `src/Plumix.Tests/Cupertino<Control>Tests.cs` / `Material<Control>Tests.cs`
(or the subsystem's file) covering every line of the spec's *Behaviors asserted by Flutter's own
tests* (`cupertino-ui-src/test/`, `material-ui-src/test/`, `flutter-src/packages/flutter/test/`),
plus defaults, interaction states, and parity-critical layout/paint. A behavior Flutter tests and
Plumix does not is a parity gap, not a test-coverage preference.

## Step 6 — Samples

If the control is user-visible, add or extend a demo page in **both** `src/Sample/Plumix.Sample` and
`dart_sample` in this same iteration (`INVARIANTS.md` > Sample Parity; Cupertino demos go under
`Demos/Cupertino` / `demos/cupertino`). Host glue is exempt.

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

- `CHANGELOG.md` — one line under `[Unreleased]`, per the rules at the top of that file. Prefix
  `Breaking:` if a public default or behavior changed, even when the change moves *toward* Flutter.
- `docs/CUPERTINO_TODO.md` — delete the row you closed (or downgrade a "partial" row to what is left).
- `docs/ai/TEST_MATRIX.md`, `docs/ai/PARITY_MATRIX.md` — update the affected rows.
- `docs/ai/DIVERGENCES.md` — add a row for any unavoidable divergence; remove rows you closed.
- `docs/ai/BACKLOG.md` — remove items you absorbed; add a row if the iteration ends blocked
  (what is left + the concrete next step). This replaces the old per-iteration notes.
- `docs/FRAMEWORK_PLAN.md` — only if milestone status changed. Do not append completion notes; it has
  a 10 KB budget.

## Definition of done

One control, closed end-to-end: API, defaults, composition, states, layout, paint, tests, samples,
docs — with build, tests, line-length and port-map checks green. If a hard blocker prevents closing,
finish everything else, add the `docs/ai/BACKLOG.md` row with the concrete next step, and say plainly
what is left.
