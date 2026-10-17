#!/usr/bin/env python3
"""Generate src/Plumix/UI/KeyboardMaps.g.cs from Flutter's keyboard_maps.g.dart.

Flutter generates `keyboard_maps.g.dart` (platform key code -> LogicalKeyboardKey/PhysicalKeyboardKey
tables) from `dev/tools/gen_keycodes`; Plumix mirrors that by generating the C# tables from
Flutter's generated Dart instead of hand-transcribing ~3000 entries. Run this after moving the
Flutter pin (see AGENTS.md), after `generate_keyboard_keys.py` (the maps reference its constants).

Usage: python3 scripts/generate_keyboard_maps.py
"""

from __future__ import annotations

import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from generate_keyboard_keys import pascal  # noqa: E402  (shared member-naming rules)

ROOT = pathlib.Path(__file__).resolve().parent.parent
DART = ROOT / "flutter-src/packages/flutter/lib/src/services/keyboard_maps.g.dart"
KEYS_CS = ROOT / "src/Plumix/UI/KeyboardKey.g.cs"
OUT = ROOT / "src/Plumix/UI/KeyboardMaps.g.cs"

MAX_LINE = 120
INT_MAX = 2**31 - 1

MAP_RE = re.compile(r"^const Map<(int|String), (.+?)> (\w+) = <(?:int|String), .+?>\{$")
DOC_RE = re.compile(r"^/// ?(.*)$")
STRING = r"'((?:[^'\\]|\\.)*)'"
KEY = rf"(0x[0-9a-fA-F]+|\d+|{STRING})"
SCALAR_ENTRY_RE = re.compile(rf"^  {KEY}: (LogicalKeyboardKey|PhysicalKeyboardKey)\.(\w+),$")
LIST_ENTRY_RE = re.compile(rf"{KEY}:\s*<LogicalKeyboardKey\?>\[(.*?)\],", re.DOTALL)
LIST_ITEM_RE = re.compile(r"^(?:null|LogicalKeyboardKey\.(\w+))$")
MEMBER_RE = re.compile(r"public static readonly (LogicalKeyboardKey|PhysicalKeyboardKey) (@?\w+) = ")

SCALAR_VALUE_TYPES = {"LogicalKeyboardKey", "PhysicalKeyboardKey"}
LIST_VALUE_TYPE = "List<LogicalKeyboardKey?>"


class ParseError(Exception):
    pass


def dart_string(raw: str) -> str:
    """Decode the body of a single-quoted Dart string literal (only simple escapes occur)."""
    out: list[str] = []
    index = 0
    while index < len(raw):
        char = raw[index]
        if char == "\\":
            index += 1
            escaped = raw[index]
            if escaped not in "\\'\"$":
                raise ParseError(f"unsupported Dart escape \\{escaped} in '{raw}'")
            out.append(escaped)
        elif char == "$":
            raise ParseError(f"string interpolation in key '{raw}'")
        else:
            out.append(char)
        index += 1
    return "".join(out)


def cs_string(text: str) -> str:
    escaped = text.replace("\\", "\\\\").replace('"', '\\"')
    return f'"{escaped}"'


def xml_escape(text: str) -> str:
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def doc_line(text: str) -> str:
    """Dart doc markup -> C# XML doc markup for the bits these comments use."""
    text = xml_escape(text)
    text = re.sub(r"\[(LogicalKeyboardKey|PhysicalKeyboardKey)\]", r'<see cref="\1"/>', text)
    return re.sub(r"`([^`]+)`", r"<c>\1</c>", text)


class DartMap:
    def __init__(self, name: str, key_type: str, value_type: str, doc: list[str]) -> None:
        self.name = name
        self.key_type = key_type
        self.value_type = value_type
        self.doc = doc
        # (key literal as written in Dart, value) — value is a member name or a list of names/None.
        self.entries: list[tuple[str, object]] = []

    def cs_key_type(self) -> str:
        if self.key_type == "String":
            return "string"
        largest = max((int(key, 0) for key, _ in self.entries), default=0)
        return "long" if largest > INT_MAX else "int"

    def cs_value_type(self) -> str:
        if self.value_type == LIST_VALUE_TYPE:
            return "IReadOnlyList<LogicalKeyboardKey?>"
        return self.value_type


def cs_key(dart_key: str) -> str:
    if dart_key.startswith("'"):
        return cs_string(dart_string(dart_key[1:-1]))
    return dart_key


def parse(lines: list[str]) -> list[DartMap]:
    maps: list[DartMap] = []
    doc: list[str] = []
    index = 0
    while index < len(lines):
        line = lines[index]
        doc_match = DOC_RE.match(line)
        if doc_match:
            doc.append(doc_match.group(1))
            index += 1
            continue
        map_match = MAP_RE.match(line)
        if map_match is None:
            if line.startswith("const "):
                raise ParseError(f"unrecognised top-level const: {line}")
            doc = []
            index += 1
            continue
        key_type, value_type, name = map_match.groups()
        if value_type not in SCALAR_VALUE_TYPES and value_type != LIST_VALUE_TYPE:
            raise ParseError(f"{name}: unsupported value type {value_type}")
        dart_map = DartMap(name, key_type, value_type, doc)
        doc = []
        body: list[str] = []
        index += 1
        while lines[index] != "};":
            body.append(lines[index])
            index += 1
        index += 1
        if value_type == LIST_VALUE_TYPE:
            parse_list_body(dart_map, "\n".join(body))
        else:
            parse_scalar_body(dart_map, body)
        if not dart_map.entries:
            raise ParseError(f"{name}: parsed no entries")
        maps.append(dart_map)
    return maps


def parse_scalar_body(dart_map: DartMap, body: list[str]) -> None:
    for line in body:
        match = SCALAR_ENTRY_RE.match(line)
        if match is None:
            raise ParseError(f"{dart_map.name}: unrecognised entry {line!r}")
        key, _, value_class, member = match.groups()
        if value_class != dart_map.value_type:
            raise ParseError(f"{dart_map.name}: {value_class} value in a {dart_map.value_type} map")
        dart_map.entries.append((key, member))


def parse_list_body(dart_map: DartMap, body: str) -> None:
    consumed = 0
    for match in LIST_ENTRY_RE.finditer(body):
        if body[consumed:match.start()].strip():
            raise ParseError(f"{dart_map.name}: unrecognised text {body[consumed:match.start()]!r}")
        consumed = match.end()
        key, _, items = match.groups()
        values: list[str | None] = []
        for item in (part.strip() for part in items.split(",")):
            if not item:
                continue
            item_match = LIST_ITEM_RE.match(item)
            if item_match is None:
                raise ParseError(f"{dart_map.name}: unrecognised list item {item!r}")
            values.append(item_match.group(1))
        dart_map.entries.append((key, values))
    if body[consumed:].strip():
        raise ParseError(f"{dart_map.name}: unrecognised text {body[consumed:]!r}")


def known_members() -> dict[str, set[str]]:
    result: dict[str, set[str]] = {"LogicalKeyboardKey": set(), "PhysicalKeyboardKey": set()}
    for match in MEMBER_RE.finditer(KEYS_CS.read_text(encoding="utf-8")):
        result[match.group(1)].add(match.group(2))
    return result


def member(class_name: str, dart_member: str, known: dict[str, set[str]], map_name: str) -> str:
    name = pascal(dart_member)
    if name not in known[class_name]:
        raise ParseError(f"{map_name}: {class_name}.{name} is not in {KEYS_CS.name}")
    return f"{class_name}.{name}"


def emit_map(out: list[str], dart_map: DartMap, known: dict[str, set[str]]) -> None:
    cs_name = dart_map.name[0].upper() + dart_map.name[1:]
    key_type = dart_map.cs_key_type()
    value_type = dart_map.cs_value_type()
    dictionary = f"Dictionary<{key_type}, {value_type}>"
    if dart_map.doc:
        out.append("    /// <summary>")
        for text in dart_map.doc:
            out.append(f"    /// {doc_line(text)}".rstrip())
        out.append("    /// </summary>")
    out.append(f"    public static readonly IReadOnlyDictionary<{key_type}, {value_type}> {cs_name} =")
    out.append(f"        new {dictionary}")
    out.append("        {")
    for key, value in dart_map.entries:
        prefix = f"            [{cs_key(key)}] = "
        if isinstance(value, list):
            items = [
                "null" if item is None else member("LogicalKeyboardKey", item, known, dart_map.name)
                for item in value
            ]
            single = f"{prefix}[{', '.join(items)}],"
            if len(single) <= MAX_LINE:
                out.append(single)
            else:
                out.append(f"{prefix}[")
                for item in items:
                    out.append(f"                {item},")
                out.append("            ],")
        else:
            assert isinstance(value, str)
            out.append(f"{prefix}{member(dart_map.value_type, value, known, dart_map.name)},")
    out.append("        }.AsReadOnly();")


def main() -> int:
    for path, hint in ((DART, "create the flutter-src symlink (see AGENTS.md)"),
                       (KEYS_CS, "run scripts/generate_keyboard_keys.py first")):
        if not path.exists():
            print(f"missing {path}; {hint}", file=sys.stderr)
            return 1

    try:
        maps = parse(DART.read_text(encoding="utf-8").splitlines())
        if not maps:
            raise ParseError("parsed no maps — the Dart layout changed")
        known = known_members()
        out: list[str] = [
            "// Dart parity source: flutter/packages/flutter/lib/src/services/keyboard_maps.g.dart",
            "//",
            "// DO NOT EDIT -- DO NOT EDIT -- DO NOT EDIT",
            "// Generated by scripts/generate_keyboard_maps.py from Flutter's own generated",
            "// keyboard_maps.g.dart. Regenerate after moving the Flutter pin; edit the script, not this.",
            "",
            "#nullable enable",
            "",
            "namespace Plumix.UI;",
            "",
            "/// <summary>",
            "/// Platform key code tables mapping each embedding's native key codes to",
            '/// <see cref="LogicalKeyboardKey"/> and <see cref="PhysicalKeyboardKey"/> values.',
            "/// </summary>",
            "public static class KeyboardMaps",
            "{",
        ]
        for index, dart_map in enumerate(maps):
            if index:
                out.append("")
            emit_map(out, dart_map, known)
        out.append("}")
    except ParseError as error:
        print(f"error: {error}", file=sys.stderr)
        return 1

    OUT.write_text("\n".join(out) + "\n", encoding="utf-8")
    total = sum(len(dart_map.entries) for dart_map in maps)
    print(f"wrote {OUT.relative_to(ROOT)}: {len(maps)} maps, {total} entries")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
