#!/usr/bin/env python3
"""Generate src/Plumix/UI/KeyboardKey.g.cs from Flutter's keyboard_key.g.dart.

Flutter generates `keyboard_key.g.dart` from `dev/tools/gen_keycodes`; Plumix mirrors that by
generating the C# key tables from Flutter's generated Dart instead of hand-transcribing 700+
constants. Run this after moving the Flutter pin (see AGENTS.md).

Usage: python3 scripts/generate_keyboard_keys.py
"""

from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
DART = ROOT / "flutter-src/packages/flutter/lib/src/services/keyboard_key.g.dart"
OUT = ROOT / "src/Plumix/UI/KeyboardKey.g.cs"

CONST_RE = re.compile(
    r"^  static const (LogicalKeyboardKey|PhysicalKeyboardKey) (\w+) = "
    r"(?:LogicalKeyboardKey|PhysicalKeyboardKey)\((0x[0-9a-fA-F]+)\);$"
)
ENTRY_RE = re.compile(r"^\s+(0x[0-9a-fA-F]+): (?:'(.*)'|(\w+)),$")
DOC_RE = re.compile(r"^  /// ?(.*)$")

# C# reserved words that collide with generated member names.
RESERVED = {"lock", "new", "base", "in", "out", "is", "as", "for", "do", "if"}


def pascal(name: str) -> str:
    name = name[0].upper() + name[1:]
    return f"@{name}" if name in RESERVED else name


def read_section(lines: list[str], start_marker: str) -> list[str]:
    """Return the raw lines of a map literal that begins with `start_marker`."""
    out: list[str] = []
    inside = False
    for line in lines:
        if not inside:
            if start_marker in line:
                inside = True
            continue
        if line.rstrip() == "  };":
            break
        out.append(line)
    return out


def parse_entries(section: list[str]) -> list[tuple[str, str]]:
    entries: list[tuple[str, str]] = []
    for line in section:
        match = ENTRY_RE.match(line.rstrip("\n"))
        if match:
            value = match.group(2) if match.group(2) is not None else match.group(3)
            entries.append((match.group(1), value))
    return entries


def collect_constants(lines: list[str]) -> dict[str, list[tuple[str, str, str]]]:
    """Map class name -> [(csharp name, hex value, doc summary)] in source order."""
    result: dict[str, list[tuple[str, str, str]]] = {
        "LogicalKeyboardKey": [],
        "PhysicalKeyboardKey": [],
    }
    doc: list[str] = []
    for line in lines:
        stripped = line.rstrip("\n")
        doc_match = DOC_RE.match(stripped)
        if doc_match:
            doc.append(doc_match.group(1))
            continue
        match = CONST_RE.match(stripped)
        if match:
            summary = doc[0].strip() if doc else ""
            result[match.group(1)].append((pascal(match.group(2)), match.group(3), summary))
        if stripped.strip():
            doc = []
    return result


def escape(text: str) -> str:
    return text.replace("\\", "\\\\").replace('"', '\\"')


def xml_escape(text: str) -> str:
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def emit_constants(out: list[str], class_name: str, ctor_arg: str,
                   constants: list[tuple[str, str, str]]) -> None:
    out.append(f"public sealed partial class {class_name}")
    out.append("{")
    for index, (name, value, summary) in enumerate(constants):
        if index:
            out.append("")
        if summary:
            out.append(f"    /// <summary>{xml_escape(summary)}</summary>")
        out.append(f"    public static readonly {class_name} {name} = new({value});")
    out.append("}")
    out.append("")
    _ = ctor_arg


def emit_name_lookup(out: list[str], class_name: str,
                     constants: list[tuple[str, str, str]]) -> None:
    """Emit a source-name lookup so host adapters can bridge W3C `code` names without reflection."""
    out.append(f"public sealed partial class {class_name}")
    out.append("{")
    out.append(f"    private static readonly Dictionary<string, {class_name}> KeysByName =")
    out.append("        new(StringComparer.Ordinal)")
    out.append("        {")
    for name, _, _ in constants:
        out.append(f'            ["{name}"] = {name},')
    out.append("        };")
    out.append("")
    out.append("    /// <summary>")
    out.append("    /// Looks a key up by its generated member name. Avalonia's <c>PhysicalKey</c> enum uses the same")
    out.append("    /// W3C <c>code</c> names as Flutter's key constants, so the host adapter (and the test key")
    out.append("    /// simulator) bridge the two by name instead of carrying a second hand-maintained table.")
    out.append("    /// </summary>")
    out.append(f"    internal static {class_name}? FindKeyByGeneratedName(string name) =>")
    out.append("        KeysByName.GetValueOrDefault(name);")
    out.append("}")
    out.append("")


def emit_lookup(out: list[str], class_name: str, field: str, value_type: str,
                entries: list[tuple[str, str]], quote: bool) -> None:
    out.append(f"public sealed partial class {class_name}")
    out.append("{")
    out.append(f"    private static readonly Dictionary<long, {value_type}> {field} = new()")
    out.append("    {")
    for key, value in entries:
        rendered = f'"{escape(value)}"' if quote else pascal(value)
        out.append(f"        [{key}] = {rendered},")
    out.append("    };")
    out.append("}")
    out.append("")


def main() -> int:
    if not DART.exists():
        print(f"missing {DART}; create the flutter-src symlink (see AGENTS.md)", file=sys.stderr)
        return 1

    lines = DART.read_text(encoding="utf-8").splitlines(keepends=True)
    constants = collect_constants(lines)
    known_logical = parse_entries(read_section(lines, "_knownLogicalKeys = <int, LogicalKeyboardKey>{"))
    key_labels = parse_entries(read_section(lines, "_keyLabels = <int, String>{"))
    known_physical = parse_entries(read_section(lines, "_knownPhysicalKeys = <int, PhysicalKeyboardKey>{"))
    debug_names = parse_entries(read_section(lines, "      : <int, String>{"))

    for name, collection in (
        ("logical constants", constants["LogicalKeyboardKey"]),
        ("physical constants", constants["PhysicalKeyboardKey"]),
        ("known logical keys", known_logical),
        ("key labels", key_labels),
        ("known physical keys", known_physical),
        ("physical debug names", debug_names),
    ):
        if not collection:
            print(f"parsed no {name} — the Dart layout changed", file=sys.stderr)
            return 1

    out: list[str] = [
        "// Dart parity source: flutter/packages/flutter/lib/src/services/keyboard_key.g.dart",
        "//",
        "// DO NOT EDIT -- DO NOT EDIT -- DO NOT EDIT",
        "// Generated by scripts/generate_keyboard_keys.py from Flutter's own generated",
        "// keyboard_key.g.dart. Regenerate after moving the Flutter pin; edit the script, not this.",
        "",
        "#nullable enable",
        "",
        "namespace Plumix.UI;",
        "",
    ]
    emit_constants(out, "LogicalKeyboardKey", "keyId", constants["LogicalKeyboardKey"])
    emit_lookup(out, "LogicalKeyboardKey", "KnownLogicalKeysById", "LogicalKeyboardKey",
                known_logical, quote=False)
    emit_lookup(out, "LogicalKeyboardKey", "KeyLabelsById", "string", key_labels, quote=True)
    emit_name_lookup(out, "LogicalKeyboardKey", constants["LogicalKeyboardKey"])
    emit_constants(out, "PhysicalKeyboardKey", "usbHidUsage", constants["PhysicalKeyboardKey"])
    emit_lookup(out, "PhysicalKeyboardKey", "KnownPhysicalKeysByCode", "PhysicalKeyboardKey",
                known_physical, quote=False)
    emit_name_lookup(out, "PhysicalKeyboardKey", constants["PhysicalKeyboardKey"])
    emit_lookup(out, "PhysicalKeyboardKey", "DebugNamesByCode", "string", debug_names, quote=True)

    while out and out[-1] == "":
        out.pop()
    OUT.write_text("\n".join(out) + "\n", encoding="utf-8")

    print(
        f"wrote {OUT.relative_to(ROOT)}: "
        f"{len(constants['LogicalKeyboardKey'])} logical + {len(constants['PhysicalKeyboardKey'])} "
        f"physical constants, {len(known_logical)}/{len(known_physical)} lookup entries, "
        f"{len(key_labels)} labels, {len(debug_names)} debug names"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
