# Dart Spec Extraction Protocol

Purpose: let one request close a large control end-to-end without the Dart source eating the
whole context window.

## Why this exists

Flutter's Material sources are big: `input_decorator.dart` is 6107 lines, `menu_anchor.dart` 4265,
`scaffold.dart` 3521, `chip.dart` 2569. Reading one of those, plus the existing C# side, plus tests,
plus the tracking docs, does not fit in a single context. The result is what
`docs/ai/PORTING_MODE.md` forbids: a control split into many token-level follow-ups.

The fix is to read the Dart **once, in a separate context**, and carry forward a dense spec instead
of the source. A 6000-line Dart file compresses to roughly 300–600 lines of spec with no loss of
port-relevant information, because most of a Flutter source file is doc comments, `debugFillProperties`,
and asserts.

This protocol is tool-agnostic on purpose — see *Running it* below.

## Inputs

For control `X`, the extractor reads, in this order:

1. `<root>/<x>.dart` — the control. For Material/Cupertino, `<root>` is `material-ui-src/lib/src`
   or `cupertino-ui-src/lib/src` (the extracted pub packages — see `AGENTS.md` > Local Reference
   Paths); for every other library it is `flutter-src/packages/flutter/lib/src/<library>`.
2. `<root>/<x>_theme.dart` — its theme pair, when Flutter has one.
3. The matching Flutter tests: `material-ui-src/test/<x>_test.dart` / `cupertino-ui-src/test/...`
   for the packages, `flutter-src/packages/flutter/test/<library>/<x>_test.dart` otherwise.
   **Do not skip
   this.** It is the most reliable source of exact default values and edge-case behavior, and it tells
   you which behaviors Flutter itself considers contractual.
4. Any `_<X>DefaultsM3` / `_<X>DefaultsM2` token classes, wherever they live.

`docs/ai/PORT_MAP.md` resolves the Flutter paths for an already-ported control; `docs/MATERIAL_TODO.md`
carries them for controls not yet ported.

## Output contract

The extractor returns **only** the sections below — no prose preamble, no summary of what it did, no
recommendations. Dart identifiers stay in Dart casing so they can be grepped later.

```
## Source
<dart files read, with line counts, and the Flutter revision from AGENTS.md>

## Public API
For every public constructor and named constructor: parameter list with exact types, exact default
values, required/optional, and assert conditions. Quote defaults literally (`8.0`, `Clip.none`,
`Duration(milliseconds: 200)`) — never paraphrase as "the usual" or "the theme value".

## Theme resolution
The precedence chain for every themable property, in the order Flutter evaluates it, e.g.
  widget.color ?? CardTheme.of(context).color ?? Theme.of(context).cardTheme.color ?? _Defaults.color
Note where useMaterial3 splits the chain, and give both the M2 and M3 values.

## Composition
The build() tree as an indented outline: widget names and the arguments that affect geometry or
semantics. Include Semantics/MergeSemantics/ExcludeSemantics wrappers, and the exact nesting order —
order is behavior, not style.

## States and transitions
Interaction states (hover/focus/press/disabled/selected/error), what triggers each, what changes,
which controllers/animations run, their durations and curves, and the dispose/reassign rules.

## Layout
The layout algorithm as ordered steps: constraints in, intrinsic sizing behavior, child layout order,
parent-data writes, size out. For RenderObject subclasses, cover performLayout, computeDryLayout,
the four intrinsic methods, and hit-test overrides.

## Paint
Paint order, layer usage (save layer, clip, opacity, transform), shape/border resolution, elevation
and shadow/surface-tint handling, and text-direction dependent mirroring.

## Behaviors asserted by Flutter's own tests
One line per contractual behavior, phrased as an assertion, with the Flutter test name.
This is the checklist the C# tests must reproduce.

## Primitives required
Framework types/methods the port needs. Mark each present-in-Plumix or missing. Missing primitives
must be landed in src/Plumix first (PORTING_MODE.md), so this list decides whether the control can
close in one pass.
```

## Rules for the extractor

- Report what the Dart **does**, not what it should do. No design opinions, no C# suggestions.
- Never round or generalize a numeric default.
- If a behavior depends on `useMaterial3`, give both branches; Plumix targets both.
- Deprecated members: list them and mark deprecated. Do not silently drop them — parity includes them
  until Flutter removes them.
- If the file is too large to read in one pass, read it in ranges and merge; do not sample.

## Running it

- **Claude Code**: `.claude/agents/dart-spec.md` runs this in a subagent whose context is discarded
  afterwards, so only the spec reaches the main conversation. `/port` invokes it automatically.
- **Codex or any other agent**: the same protocol works inline — read this file, produce the spec,
  and only then open the C# side. If the tool supports subtasks, run it as one; if not, produce the
  spec as the first output of the request and keep it as the reference for the rest of the work.
  Nothing in this protocol is Claude-specific.
