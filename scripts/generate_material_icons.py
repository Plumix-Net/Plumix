#!/usr/bin/env python3
"""Generate Icons.g.cs and vendor the matching MaterialIcons font asset.

material_ui's icons.dart declares more than 8,800 IconData constants plus the 20 platform-adaptive
getters of PlatformAdaptiveIcons. Plumix parses that pinned Dart source instead of hand-transcribing
the catalog, and copies the font/license from the pinned Flutter checkout's material_fonts artifact
(the same file Flutter itself ships).

The catalog is emitted as packed code-point/direction arrays plus one accessor per icon, so the
generated type keeps a single IconData instance per icon without an 8,800-entry static constructor.

Run this after moving the material_ui or Flutter pin (see AGENTS.md).

Usage: python3 scripts/generate_material_icons.py
"""

from __future__ import annotations

import hashlib
import pathlib
import re
import shutil
import struct
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
DART = ROOT / "material-ui-src/lib/src/icons.dart"
FONT_SOURCE_DIR = ROOT / "flutter-src/bin/cache/artifacts/material_fonts"
OUT = ROOT / "src/Plumix.Material/Icons.g.cs"
ASSET_DIR = ROOT / "src/Plumix.Material/Assets/Fonts"

MAX_LINE = 120
INDENT = "    "

ICON_RE = re.compile(
    r"^  static const IconData (\w+)\s*=\s*IconData\(\s*"
    r"(0x[0-9a-fA-F]+)\s*,(.*?)\);$",
    re.MULTILINE | re.DOTALL,
)
ADAPTIVE_RE = re.compile(
    r"^  IconData get (\w+) =>\s*!_isCupertino\(\) \? Icons\.(\w+) : Icons\.(\w+);$",
    re.MULTILINE | re.DOTALL,
)
FONT_FAMILY_RE = re.compile(r"fontFamily: '([^']+)'")

# Members the generated type declares itself; an icon whose C# name collides would not compile.
RESERVED = {
    "Adaptive",
    "BuildCatalog",
    "Catalog",
    "CatalogCodePoints",
    "DirectionalIndexes",
    "IconCount",
    "IconFont",
    "ManifestSha256",
}


def pascal(name: str) -> str:
    return "".join(part[0].upper() + part[1:] for part in name.split("_") if part)


def wrapped_property(name: str, index: int) -> list[str]:
    declaration = f"{INDENT}public static IconData {name} => Catalog[{index}];"
    if len(declaration) <= MAX_LINE:
        return [declaration]
    return [f"{INDENT}public static IconData {name} =>", f"{INDENT}{INDENT}Catalog[{index}];"]


def packed_array(name: str, values: list[str], per_line: int) -> list[str]:
    lines = [f"{INDENT}private static readonly int[] {name} =", f"{INDENT}["]
    for start in range(0, len(values), per_line):
        chunk = ", ".join(values[start : start + per_line])
        lines.append(f"{INDENT}{INDENT}{chunk},")
    lines.append(f"{INDENT}];")
    for line in lines:
        if len(line) > MAX_LINE:
            raise SystemExit(f"generate_material_icons: packed array line exceeds {MAX_LINE} chars")
    return lines


def adaptive_property(name: str, material: str, cupertino: str) -> list[str]:
    declaration = f"{INDENT}public IconData {name} => !IsCupertino() ? Icons.{material} : Icons.{cupertino};"
    if len(declaration) <= MAX_LINE:
        return [declaration]
    return [
        f"{INDENT}public IconData {name} =>",
        f"{INDENT}{INDENT}!IsCupertino() ? Icons.{material} : Icons.{cupertino};",
    ]


def parse_icons(source: str) -> list[tuple[str, str, bool]]:
    icons = [
        (name, code_point.lower(), "matchTextDirection: true" in arguments)
        for name, code_point, arguments in ICON_RE.findall(source)
    ]
    if not icons:
        raise SystemExit("generate_material_icons: parsed no IconData declarations")

    families = set(FONT_FAMILY_RE.findall(source))
    if families != {"MaterialIcons"}:
        raise SystemExit(f"generate_material_icons: unexpected icon font families {sorted(families)}")

    for name, code_point, arguments in ICON_RE.findall(source):
        residue = arguments.replace("fontFamily: 'MaterialIcons'", "").replace("matchTextDirection: true", "")
        if residue.replace(",", "").strip():
            raise SystemExit(f"generate_material_icons: unsupported IconData arguments on '{name}'")

    if len({name for name, _, _ in icons}) != len(icons):
        raise SystemExit("generate_material_icons: parsed duplicate Dart member names")
    if len({pascal(name) for name, _, _ in icons}) != len(icons):
        raise SystemExit("generate_material_icons: Dart names collide after C# casing conversion")
    collisions = RESERVED & {pascal(name) for name, _, _ in icons}
    if collisions:
        raise SystemExit(f"generate_material_icons: icon names collide with generated members {sorted(collisions)}")
    return icons


def parse_adaptive(source: str, known: set[str]) -> list[tuple[str, str, str]]:
    adaptive = ADAPTIVE_RE.findall(source)
    declared = len(re.findall(r"^  IconData get \w+ =>", source, re.MULTILINE))
    if len(adaptive) != declared:
        raise SystemExit(
            f"generate_material_icons: parsed {len(adaptive)} of {declared} PlatformAdaptiveIcons getters"
        )
    for name, material, cupertino in adaptive:
        if material not in known or cupertino not in known:
            raise SystemExit(f"generate_material_icons: adaptive getter '{name}' references an unknown icon")
    return adaptive


def font_code_points(font: pathlib.Path) -> set[int]:
    """Every code point the font's cmap maps, read from its format 4 and 12 subtables."""
    data = font.read_bytes()
    table_count = struct.unpack(">H", data[4:6])[0]
    tables = {}
    for index in range(table_count):
        offset = 12 + index * 16
        tag = data[offset : offset + 4].decode("latin1")
        tables[tag] = struct.unpack(">II", data[offset + 8 : offset + 16])
    if "cmap" not in tables:
        raise SystemExit(f"generate_material_icons: {font.name} has no cmap table")

    cmap = tables["cmap"][0]
    codes: set[int] = set()
    for index in range(struct.unpack(">H", data[cmap + 2 : cmap + 4])[0]):
        subtable = cmap + struct.unpack(">HHI", data[cmap + 4 + index * 8 : cmap + 12 + index * 8])[2]
        if struct.unpack(">H", data[subtable : subtable + 2])[0] != 12:
            continue
        for group in range(struct.unpack(">I", data[subtable + 12 : subtable + 16])[0]):
            start, end, _ = struct.unpack(">III", data[subtable + 16 + group * 12 : subtable + 28 + group * 12])
            codes.update(range(start, end + 1))
    if not codes:
        raise SystemExit(f"generate_material_icons: {font.name} has no format 12 cmap subtable")
    return codes


def main() -> int:
    if not DART.exists():
        print(f"missing {DART}; create the material-ui-src symlink (see AGENTS.md)", file=sys.stderr)
        return 1

    source = DART.read_text(encoding="utf-8")
    icons = parse_icons(source)
    adaptive = parse_adaptive(source, {name for name, _, _ in icons})

    manifest = "\n".join(
        f"{pascal(name)}={code_point}:{str(directional).lower()}"
        for name, code_point, directional in sorted(icons, key=lambda icon: pascal(icon[0]))
    )
    manifest_hash = hashlib.sha256(manifest.encode("utf-8")).hexdigest()

    out = [
        "// Dart parity source: material_ui/lib/src/icons.dart",
        "//",
        "// DO NOT EDIT -- DO NOT EDIT -- DO NOT EDIT",
        "// Generated by scripts/generate_material_icons.py from the material_ui package's icons.dart.",
        "// Regenerate after moving the material_ui or Flutter pin; edit the script, not this file.",
        "//",
        "// Dart's PlatformAdaptiveIcons declares `implements Icons` so that it type-checks against the",
        "// icon set; C# has no static class to implement, so the relationship is dropped. No behaviour",
        "// depends on it -- every getter below resolves to the same Icons member Dart resolves to.",
        "",
        "using Plumix.Widgets;",
        "",
        "namespace Plumix.Material;",
        "",
        "/// <summary>A set of platform-adaptive Material Design icons.</summary>",
        "public sealed class PlatformAdaptiveIcons",
        "{",
        f"{INDENT}internal static PlatformAdaptiveIcons Instance {{ get; }} = new();",
        "",
        f"{INDENT}private PlatformAdaptiveIcons()",
        f"{INDENT}{{",
        f"{INDENT}}}",
    ]

    for name, material, cupertino in adaptive:
        out.append("")
        out.extend(adaptive_property(pascal(name), pascal(material), pascal(cupertino)))

    out.extend(
        [
            "",
            f"{INDENT}private static bool IsCupertino()",
            f"{INDENT}{{",
            f"{INDENT}{INDENT}return PlatformDefaults.TargetPlatform switch",
            f"{INDENT}{INDENT}{{",
            f"{INDENT}{INDENT}{INDENT}TargetPlatform.IOS or TargetPlatform.MacOS => true,",
            f"{INDENT}{INDENT}{INDENT}_ => false,",
            f"{INDENT}{INDENT}}};",
            f"{INDENT}}}",
            "}",
            "",
            "/// <summary>Identifiers for the supported Material Design icons.</summary>",
            "public static partial class Icons",
            "{",
            f'{INDENT}public const string IconFont = "MaterialIcons";',
            "",
            f'{INDENT}internal const string ManifestSha256 = "{manifest_hash}";',
            "",
            f"{INDENT}internal const int IconCount = {len(icons)};",
            "",
            f"{INDENT}/// <summary>A set of platform-adaptive Material Design icons.</summary>",
            f"{INDENT}public static PlatformAdaptiveIcons Adaptive => PlatformAdaptiveIcons.Instance;",
        ]
    )

    for index, (name, _, _) in enumerate(icons):
        out.append("")
        out.extend(wrapped_property(pascal(name), index))

    out.append("")
    out.extend(packed_array("CatalogCodePoints", [code_point for _, code_point, _ in icons], 10))
    out.append("")
    out.extend(
        packed_array(
            "DirectionalIndexes",
            [str(index) for index, icon in enumerate(icons) if icon[2]],
            15,
        )
    )
    out.extend(
        [
            "",
            f"{INDENT}private static readonly IconData[] Catalog = BuildCatalog();",
            "",
            f"{INDENT}private static IconData[] BuildCatalog()",
            f"{INDENT}{{",
            f"{INDENT}{INDENT}var directional = new bool[CatalogCodePoints.Length];",
            f"{INDENT}{INDENT}foreach (int index in DirectionalIndexes)",
            f"{INDENT}{INDENT}{{",
            f"{INDENT}{INDENT}{INDENT}directional[index] = true;",
            f"{INDENT}{INDENT}}}",
            "",
            f"{INDENT}{INDENT}var catalog = new IconData[CatalogCodePoints.Length];",
            f"{INDENT}{INDENT}for (int index = 0; index < catalog.Length; index++)",
            f"{INDENT}{INDENT}{{",
            f"{INDENT}{INDENT}{INDENT}catalog[index] = new IconData(",
            f"{INDENT}{INDENT}{INDENT}{INDENT}CatalogCodePoints[index],",
            f"{INDENT}{INDENT}{INDENT}{INDENT}FontFamily: IconFont,",
            f"{INDENT}{INDENT}{INDENT}{INDENT}MatchTextDirection: directional[index]);",
            f"{INDENT}{INDENT}}}",
            "",
            f"{INDENT}{INDENT}return catalog;",
            f"{INDENT}}}",
            "}",
        ]
    )

    long_lines = [line for line in out if len(line) > MAX_LINE]
    if long_lines:
        raise SystemExit(f"generate_material_icons: {len(long_lines)} generated lines exceed {MAX_LINE} chars")

    OUT.write_text("\n".join(out) + "\n", encoding="utf-8")

    font_source = FONT_SOURCE_DIR / "MaterialIcons-Regular.otf"
    license_source = FONT_SOURCE_DIR / "MaterialIcons_LICENSE.txt"
    if not font_source.exists() or not license_source.exists():
        raise SystemExit(
            f"generate_material_icons: missing material_fonts artifact at {FONT_SOURCE_DIR}; "
            "create the flutter-src symlink and run 'flutter precache' (see AGENTS.md)"
        )

    covered = font_code_points(font_source)
    uncovered = sorted({int(code_point, 16) for _, code_point, _ in icons} - covered)
    if uncovered:
        raise SystemExit(
            f"generate_material_icons: {len(uncovered)} catalog code points are missing from "
            f"{font_source.name} (first: {hex(uncovered[0])}); the font and icons.dart pins disagree"
        )

    ASSET_DIR.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(font_source, ASSET_DIR / font_source.name)
    shutil.copyfile(license_source, ASSET_DIR / license_source.name)

    print(
        f"wrote {OUT.relative_to(ROOT)} ({len(icons)} icons, {len(adaptive)} adaptive getters, "
        f"manifest {manifest_hash}); copied {font_source.name} and license"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
