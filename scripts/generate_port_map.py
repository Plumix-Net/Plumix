#!/usr/bin/env python3
"""Generate docs/ai/PORT_MAP.md from the `// Dart parity source:` markers already in the code.

Every framework file carries a comment naming the Flutter file it was ported from. This turns
those markers into a reverse index (dart file -> C# files + tests + demos) so an agent can jump
straight to the spec for a control instead of searching for it, and reports the gaps:

  * C# files with no marker,
  * markers pointing at Flutter files that no longer exist in the pinned checkout
    (i.e. the port drifted from the Flutter version recorded in AGENTS.md).

Usage:
    scripts/generate_port_map.py              # rewrite docs/ai/PORT_MAP.md
    scripts/generate_port_map.py --check      # exit 1 if the file is out of date (CI/hook)

The Flutter checkout is resolved from $FLUTTER_SRC, else the gitignored ./flutter-src symlink.
"""

from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from collections import defaultdict
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
OUT = REPO / "docs" / "ai" / "PORT_MAP.md"

FRAMEWORK_DIRS = ["src/Plumix", "src/Plumix.Cupertino", "src/Plumix.Material"]
TESTS_DIR = REPO / "src" / "Plumix.Tests"
CS_DEMOS = REPO / "src" / "Sample" / "Plumix.Sample"
DART_DEMOS = REPO / "dart_sample" / "lib"

# Matches both `// Dart parity source: flutter/packages/...` and the `(reference)` / plural forms,
# plus bare continuation lines listing a second dart path. Material/Cupertino ports reference the
# extracted pub packages (`material_ui/lib/src/...`, `cupertino_ui/lib/src/...`) instead of the
# framework checkout.
MARKER = re.compile(
    r"(?:^|\s)((?:flutter/)?packages/flutter/lib/src/[\w/]+\.dart"
    r"|(?:material_ui|cupertino_ui)/lib/src/[\w/]+\.dart)"
)


def flutter_root() -> Path | None:
    env = os.environ.get("FLUTTER_SRC")
    candidates = [Path(env)] if env else []
    candidates.append(REPO / "flutter-src")
    for c in candidates:
        if (c / "packages/flutter/lib/src").is_dir():
            return c
    return None


def package_root(package: str) -> Path | None:
    """Root of an extracted design-library pub package (material_ui / cupertino_ui)."""
    env = os.environ.get(package.upper() + "_SRC")
    candidates = [Path(env)] if env else []
    candidates.append(REPO / (package.replace("_", "-") + "-src"))
    for c in candidates:
        if (c / "lib/src").is_dir():
            return c
    return None


def package_version(root: Path) -> str:
    """The `version:` field of the package's pubspec.yaml, or '?' when unreadable."""
    try:
        for line in (root / "pubspec.yaml").read_text(encoding="utf-8").splitlines():
            if line.startswith("version:"):
                return line.split(":", 1)[1].strip()
    except OSError:
        pass
    return "?"


def resolve_dart(dart: str, froot: Path | None, proots: dict[str, Path | None]) -> bool | None:
    """True/False if the marker path exists under its root; None when the root is unavailable."""
    for package in proots:
        prefix = package + "/"
        if dart.startswith(prefix):
            root = proots[package]
            return None if root is None else (root / dart.removeprefix(prefix)).is_file()
    return None if froot is None else (froot / dart).is_file()


def snake(name: str) -> str:
    return re.sub(r"(?<!^)(?=[A-Z])", "_", name).lower()


def scan_markers() -> tuple[dict[str, list[str]], list[str]]:
    """Return (dart path -> [C# files]) and the list of C# files carrying no marker."""
    index: dict[str, list[str]] = defaultdict(list)
    unmarked: list[str] = []

    for d in FRAMEWORK_DIRS:
        for cs in sorted((REPO / d).rglob("*.cs")):
            if any(p in cs.parts for p in ("obj", "bin")) or cs.name.endswith(".g.cs"):
                continue
            # The marker sits in the file header — sometimes above, sometimes below `namespace`.
            with cs.open(encoding="utf-8-sig") as fh:
                head = "".join(line for _, line in zip(range(80), fh))
            found = {m.group(1).removeprefix("flutter/") for m in MARKER.finditer(head)}
            rel = cs.relative_to(REPO).as_posix()
            if not found:
                unmarked.append(rel)
                continue
            for dart in found:
                index[dart].append(rel)
    return index, unmarked


def related(cs_files: list[str]) -> tuple[list[str], list[str]]:
    """Best-effort test and demo files for a control, matched on the C# type name."""
    stems = {Path(f).stem for f in cs_files}
    stems = {s for s in stems if not s.endswith("Theme") and not s.endswith("ThemeData")}

    tests, demos = set(), set()
    for stem in stems:
        for t in TESTS_DIR.glob(f"*{stem}*Tests.cs"):
            tests.add(t.relative_to(REPO).as_posix())
        for demo in CS_DEMOS.rglob(f"*{stem}*.cs"):
            demos.add(demo.relative_to(REPO).as_posix())
        if DART_DEMOS.is_dir():
            for demo in DART_DEMOS.rglob(f"*{snake(stem)}*.dart"):
                demos.add(demo.relative_to(REPO).as_posix())
    return sorted(tests), sorted(demos)


def render(
    index: dict[str, list[str]],
    unmarked: list[str],
    froot: Path | None,
    proots: dict[str, Path | None],
) -> str:
    lines = [
        "# Port Map (Dart -> C#)",
        "",
        "Generated by `scripts/generate_port_map.py` from the `// Dart parity source:` markers in the",
        "framework sources. **Do not edit by hand** — fix the marker in the C# file and regenerate.",
        "",
        "Use it to go straight from a Flutter file to everything on the C# side (and back) without",
        "searching. Paths under `packages/flutter/...` resolve inside the pinned checkout; paths under",
        "`material_ui/...` / `cupertino_ui/...` resolve inside the pinned pub packages — see",
        "`AGENTS.md` > Local Reference Paths for the pins and the symlinks.",
        "",
        "**This is a lookup table — grep it for your control, do not read it end-to-end.**",
        "",
        f"Flutter checkout used for validation: `{froot}`" if froot else
        "Flutter checkout not found — existence of `packages/flutter/...` paths was NOT validated.",
        *(
            f"`{p}` package used for validation: version {package_version(r)} at `{r}`" if r else
            f"`{p}` package root not found — existence of `{p}/...` paths was NOT validated."
            for p, r in proots.items()
        ),
        "",
        "## Index",
        "",
        "| Flutter source | C# implementation | Tests | Demos (C# / Dart) |",
        "| --- | --- | --- | --- |",
    ]

    missing_dart: list[str] = []
    for dart in sorted(index):
        cs_files = sorted(index[dart])
        tests, demos = related(cs_files)
        resolved = resolve_dart(dart, froot, proots)
        exists = resolved is not False
        if not exists:
            missing_dart.append(dart)
        short = dart.removeprefix("packages/flutter/lib/src/").replace("/lib/src/", "/")
        mark = "" if exists else " ⚠️"
        lines.append(
            f"| `{short}`{mark} | {'<br>'.join(f'`{c}`' for c in cs_files)} "
            f"| {'<br>'.join(f'`{t}`' for t in tests) or '—'} "
            f"| {'<br>'.join(f'`{d}`' for d in demos) or '—'} |"
        )

    lines += ["", "## Gaps", ""]
    if missing_dart:
        lines += [
            "### Markers pointing at files absent from the pinned Flutter checkout",
            "",
            "The port targeted a different Flutter revision, or the file was renamed upstream.",
            "Re-check parity against the current source before touching these.",
            "",
        ]
        lines += [f"- `{d}`" for d in missing_dart]
        lines.append("")
    if unmarked:
        lines += [
            "### Framework files without a `Dart parity source` marker",
            "",
            "Either C#-only infrastructure (fine — say so in a header comment) or an undocumented port.",
            "",
        ]
        lines += [f"- `{f}`" for f in unmarked]
        lines.append("")
    if not missing_dart and not unmarked:
        lines += ["None.", ""]

    lines += [
        "## Summary",
        "",
        f"- Flutter files mapped: {len(index)}",
        f"- C# files carrying a marker: {sum(len(v) for v in index.values())}",
        f"- C# files without a marker: {len(unmarked)}",
        f"- Markers not resolvable in the pinned checkout: {len(missing_dart)}",
        "",
    ]
    return "\n".join(lines) + "\n"


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="fail if PORT_MAP.md is stale")
    args = ap.parse_args()

    froot = flutter_root()
    proots = {p: package_root(p) for p in ("material_ui", "cupertino_ui")}
    index, unmarked = scan_markers()
    content = render(index, unmarked, froot, proots)

    if args.check:
        current = OUT.read_text(encoding="utf-8") if OUT.exists() else ""
        if current != content:
            print("docs/ai/PORT_MAP.md is stale — run scripts/generate_port_map.py", file=sys.stderr)
            return 1
        return 0

    OUT.write_text(content, encoding="utf-8")
    print(f"wrote {OUT.relative_to(REPO)}: {len(index)} dart files, {len(unmarked)} unmarked C# files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
