#!/usr/bin/env python3
"""Generate src/Plumix/Foundation/Intl/IntlData.g.cs - the CLDR snapshot the intl subset runs on.

Plumix formats dates and numbers from pinned CLDR data instead of `System.Globalization`, so that a
locale renders identically on every host and matches the data Flutter itself ships:

  * date symbols and patterns are read out of Flutter's
    `flutter_localizations/lib/src/l10n/generated_date_localizations.dart`, which is exactly what
    `loadDateIntlDataIfNotLoaded` installs into intl. It is not identical to the `package:intl` data
    files - `ar`'s native digits are in Flutter's snapshot only - so Flutter's copy is the source.
  * number symbols come from `package:intl`'s `lib/number_symbols_data.dart`, which intl compiles in
    and Flutter does not snapshot.

Usage:
    scripts/generate_intl_data.py            # rewrite the generated file
    scripts/generate_intl_data.py --check    # exit 1 when the file is out of date
"""

from __future__ import annotations

import argparse
import os
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
OUT = REPO / "src" / "Plumix" / "Foundation" / "Intl" / "IntlData.g.cs"
DATE_LOCALIZATIONS = (
    REPO / "flutter-src/packages/flutter_localizations/lib/src/l10n/generated_date_localizations.dart"
)
INTL_VERSION = "0.20.3"

FIELD = "\u0001"
ITEM = "|"

# DateSymbols fields, in the order `DateSymbols.Parse` reads them.
SYMBOL_FIELDS = [
    "NAME", "ERAS", "ERANAMES", "NARROWMONTHS", "STANDALONENARROWMONTHS", "MONTHS",
    "STANDALONEMONTHS", "SHORTMONTHS", "STANDALONESHORTMONTHS", "WEEKDAYS", "STANDALONEWEEKDAYS",
    "SHORTWEEKDAYS", "STANDALONESHORTWEEKDAYS", "NARROWWEEKDAYS", "STANDALONENARROWWEEKDAYS",
    "SHORTQUARTERS", "QUARTERS", "AMPMS", "ZERODIGIT", "FIRSTDAYOFWEEK",
]

# NumberSymbols fields, in the order `NumberSymbols.Parse` reads them.
NUMBER_FIELDS = ["NAME", "DECIMAL_SEP", "GROUP_SEP", "ZERO_DIGIT", "PLUS_SIGN", "MINUS_SIGN",
                 "DECIMAL_PATTERN"]


def intl_root() -> Path:
    env = os.environ.get("INTL_SRC")
    if env:
        return Path(env)
    root = Path.home() / ".pub-cache" / "hosted" / "pub.dev" / ("intl-" + INTL_VERSION)
    if not root.is_dir():
        sys.exit("intl " + INTL_VERSION + " not found at " + str(root) + "; set $INTL_SRC")
    return root


def unescape_dart(text: str) -> str:
    def replace(match: re.Match[str]) -> str:
        body = match.group(0)
        if body.startswith("\\u{"):
            return chr(int(body[3:-1], 16))
        if body.startswith("\\u"):
            return chr(int(body[2:], 16))
        return {"\\n": "\n", "\\t": "\t", "\\r": "\r", "\\'": "'", '\\"': '"',
                "\\\\": "\\", "\\$": "$"}.get(body, body[1:])

    return re.sub(r"\\u\{[0-9a-fA-F]+\}|\\u[0-9a-fA-F]{4}|\\.", replace, text)


def parse_dart_value(source: str, index: int):
    """Parse one Dart literal (string, int or `const <T>[...]`) starting at `index`."""
    while source[index] in " \n":
        index += 1
    if source.startswith("const <", index) or source[index] == "[":
        index = source.index("[", index) + 1
        items = []
        while True:
            while source[index] in " \n,":
                index += 1
            if source[index] == "]":
                return items, index + 1
            value, index = parse_dart_value(source, index)
            items.append(value)
    if source[index] in "'\"":
        quote = source[index]
        index += 1
        start = index
        while True:
            if source[index] == "\\":
                index += 2
                continue
            if source[index] == quote:
                return unescape_dart(source[start:index]), index + 1
            index += 1
    match = re.compile(r"-?\d+").match(source, index)
    if not match:
        sys.exit("unparsable Dart value at " + repr(source[index:index + 40]))
    return int(match.group(0)), match.end()


def parse_map_region(source: str, header: str) -> str:
    start = source.index(header)
    return source[start:source.index("\n};", start)]


def parse_symbols(source: str) -> dict[str, dict]:
    """Parse the `dateSymbols` map: one `intl.DateSymbols(...)` call per locale."""
    region = parse_map_region(source, "final Map<String, intl.DateSymbols> dateSymbols")
    entries: dict[str, dict] = {}
    for match in re.finditer(r"^  '([\w]+)': intl\.DateSymbols\($", region, re.M):
        fields: dict[str, object] = {}
        index = match.end()
        while True:
            key = re.compile(r"\s*(\w+):\s*").match(region, index)
            if not key:
                break
            value, index = parse_dart_value(region, key.end())
            fields[key.group(1)] = value
            index = region.index(",", index - 1) + 1
        entries[match.group(1)] = fields
    return entries


def parse_patterns(source: str) -> dict[str, dict[str, str]]:
    """Parse the `datePatterns` map: one skeleton -> pattern map per locale."""
    region = parse_map_region(source, "const Map<String, Map<String, String>> datePatterns")
    entries: dict[str, dict[str, str]] = {}
    for match in re.finditer(r"^  '([\w]+)': <String, String>\{$", region, re.M):
        fields: dict[str, str] = {}
        index = match.end()
        while True:
            key = re.compile(r"\s*'([\w]+)':\s*").match(region, index)
            if not key:
                break
            value, index = parse_dart_value(region, key.end())
            fields[key.group(1)] = value
            index = region.index(",", index - 1) + 1
        entries[match.group(1)] = fields
    return entries


def parse_number_symbols(source: str) -> dict[str, dict[str, str]]:
    """Read `numberFormatSymbols` out of intl's `lib/number_symbols_data.dart`.

    The file also holds `compactNumberSymbols`; only the first map's region is parsed."""
    region = parse_map_region(source, "Map<String, NumberSymbols> numberFormatSymbols")
    entries: dict[str, dict[str, str]] = {}
    for match in re.finditer(r'^  "([\w]+)": new NumberSymbols\($', region, re.M):
        fields: dict[str, str] = {}
        index = match.end()
        while True:
            key = re.compile(r"\s*(\w+):\s*").match(region, index)
            if not key:
                break
            value, index = parse_dart_value(region, key.end())
            fields[key.group(1)] = value
            index = region.index(",", index - 1) + 1
        entries[match.group(1)] = fields
    return entries


def pack(value: str) -> str:
    if FIELD in value or ITEM in value:
        sys.exit("separator character present in CLDR value " + repr(value))
    return value


def pack_symbols(data: dict) -> str:
    fields = []
    for name in SYMBOL_FIELDS:
        value = data.get(name)
        if name == "ZERODIGIT":
            fields.append(pack(value or ""))
        elif name in ("NAME", "FIRSTDAYOFWEEK"):
            fields.append(pack(str(value)))
        else:
            fields.append(ITEM.join(pack(item) for item in value))
    return FIELD.join(fields)


def pack_patterns(data: dict[str, str]) -> str:
    parts = []
    for skeleton, pattern in data.items():
        parts.append(pack(skeleton))
        parts.append(pack(pattern))
    return FIELD.join(parts)


def literal(value: str) -> str:
    out = []
    for char in value:
        if char == '"':
            out.append('\\"')
        elif char == "\\":
            out.append("\\\\")
        elif ord(char) < 0x20 or ord(char) == 0x7F:
            out.append("\\u%04x" % ord(char))
        else:
            out.append(char)
    return '"' + "".join(out) + '"'


def render() -> str:
    source = DATE_LOCALIZATIONS.read_text(encoding="utf-8") if DATE_LOCALIZATIONS.is_file() \
        else sys.exit(str(DATE_LOCALIZATIONS) + " not found; create the flutter-src symlink")
    symbols = parse_symbols(source)
    patterns = parse_patterns(source)
    if sorted(symbols) != sorted(patterns):
        sys.exit("dateSymbols and datePatterns cover different locales")
    for locale, fields in symbols.items():
        missing = [name for name in SYMBOL_FIELDS
                   if name != "ZERODIGIT" and fields.get(name) is None]
        if missing:
            sys.exit("locale " + locale + " is missing " + ", ".join(missing))

    number_source = (intl_root() / "lib/number_symbols_data.dart").read_text(encoding="utf-8")
    numbers_raw = parse_number_symbols(number_source)
    if len(numbers_raw) < 100:
        sys.exit("number symbol parse looks incomplete: " + str(len(numbers_raw)) + " locales")
    numbers = {
        locale: FIELD.join(pack(entry[name]) for name in NUMBER_FIELDS)
        for locale, entry in numbers_raw.items()
    }

    lines = [
        "// Generated by scripts/generate_intl_data.py. Do not edit by hand.",
        "//",
        "// Date symbols and patterns come from flutter_localizations'",
        "// `l10n/generated_date_localizations.dart` (what Flutter installs into intl); number",
        "// symbols come from `package:intl` " + INTL_VERSION + ".",
        "// Records are packed: fields separated by U+0001, list items by '|'.",
        "",
        "namespace Plumix.Foundation.Intl;",
        "",
        "internal static class IntlData",
        "{",
        "    internal const char FieldSeparator = '\\u0001';",
        "",
        "    internal const char ItemSeparator = '|';",
        "",
        "    internal static readonly IReadOnlyDictionary<string, string> DateSymbols =",
        "        new Dictionary<string, string>(StringComparer.Ordinal)",
        "        {",
    ]
    for locale in symbols:
        lines.append("            [" + literal(locale) + "] = "
                     + literal(pack_symbols(symbols[locale])) + ",")
    lines += [
        "        };",
        "",
        "    internal static readonly IReadOnlyDictionary<string, string> DatePatterns =",
        "        new Dictionary<string, string>(StringComparer.Ordinal)",
        "        {",
    ]
    for locale in patterns:
        lines.append("            [" + literal(locale) + "] = "
                     + literal(pack_patterns(patterns[locale])) + ",")
    lines += [
        "        };",
        "",
        "    internal static readonly IReadOnlyDictionary<string, string> NumberSymbols =",
        "        new Dictionary<string, string>(StringComparer.Ordinal)",
        "        {",
    ]
    for locale in sorted(numbers):
        lines.append("            [" + literal(locale) + "] = " + literal(numbers[locale]) + ",")
    lines += ["        };", "}", ""]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    rendered = render()
    if args.check:
        current = OUT.read_text(encoding="utf-8") if OUT.is_file() else ""
        if current != rendered:
            print(str(OUT) + " is out of date; run scripts/generate_intl_data.py", file=sys.stderr)
            return 1
        return 0
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(rendered, encoding="utf-8")
    print("wrote " + str(OUT))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
