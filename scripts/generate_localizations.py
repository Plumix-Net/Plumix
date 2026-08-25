#!/usr/bin/env python3
"""Generate the per-locale localization bundles from Flutter's own generated Dart.

Flutter's `gen_localizations.dart` turns the `.arb` files into one Dart library per library
(`generated_cupertino_localizations.dart`, `generated_widgets_localizations.dart`): a class per
locale, the sublocale inheritance chain, the supported-language set and the lookup switch. Plumix
transliterates that output instead of re-deriving it from the `.arb` files, so the class hierarchy,
the locale fallbacks and every translated string match Flutter's exactly.

Outputs:
    src/Plumix/Widgets/GlobalWidgetsLocalizations.g.cs
    src/Plumix.Cupertino/GlobalCupertinoLocalizations.g.cs

Usage:
    scripts/generate_localizations.py            # rewrite both generated files
    scripts/generate_localizations.py --check    # exit 1 when either file is out of date
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent

CUPERTINO_DART = REPO / "cupertino-ui-src/lib/src/l10n/generated_cupertino_localizations.dart"
WIDGETS_DART = REPO / "flutter-src/packages/flutter_localizations/lib/src/l10n/generated_widgets_localizations.dart"
CUPERTINO_OUT = REPO / "src/Plumix.Cupertino/GlobalCupertinoLocalizations.g.cs"
WIDGETS_OUT = REPO / "src/Plumix/Widgets/GlobalWidgetsLocalizations.g.cs"

# Members `GlobalCupertinoLocalizations` declares protected (Dart's `@protected`) and nullable.
PLURAL_SUFFIXES = ["Zero", "One", "Two", "Few", "Many", "Other"]
PLURAL_BASES = [
    "datePickerHourSemanticsLabel", "datePickerMinuteSemanticsLabel",
    "timerPickerHourLabel", "timerPickerMinuteLabel", "timerPickerSecondLabel",
]
PROTECTED_NULLABLE = {base + suffix for base in PLURAL_BASES for suffix in PLURAL_SUFFIXES}
PROTECTED_REQUIRED = {"datePickerDateOrderString", "datePickerDateTimeOrderString",
                      "tabSemanticsLabelRaw"}

CUPERTINO_FORMATS = [
    ("DateFormat", "fullYearFormat"),
    ("DateFormat", "dayFormat"),
    ("DateFormat", "weekdayFormat"),
    ("DateFormat", "mediumDateFormat"),
    ("DateFormat", "singleDigitHourFormat"),
    ("DateFormat", "singleDigitMinuteFormat"),
    ("DateFormat", "doubleDigitMinuteFormat"),
    ("DateFormat", "singleDigitSecondFormat"),
    ("NumberFormat", "decimalFormat"),
]


class DartClass:
    def __init__(self, name: str, parent: str, doc: str, locale_name: str | None,
                 text_direction: str | None, getters: list[tuple[str, str, bool]]):
        self.name = name
        self.parent = parent
        self.doc = doc
        self.locale_name = locale_name
        self.text_direction = text_direction
        self.getters = getters


def dart_string(literal: str) -> str:
    """Decode a Dart string literal (raw or escaped, single- or double-quoted)."""
    raw = literal.startswith("r")
    body = literal[1:] if raw else literal
    quote = body[0]
    if body[-1] != quote:
        sys.exit(f"unbalanced string literal {literal!r}")
    body = body[1:-1]
    if raw:
        return body

    def replace(match: re.Match[str]) -> str:
        text = match.group(0)
        if text.startswith("\\u{"):
            return chr(int(text[3:-1], 16))
        if text.startswith("\\u"):
            return chr(int(text[2:], 16))
        return {"\\n": "\n", "\\t": "\t", "\\r": "\r", "\\b": "\b", "\\f": "\f",
                "\\'": "'", '\\"': '"', "\\\\": "\\", "\\$": "$"}.get(text, text[1:])

    return re.sub(r"\\u\{[0-9a-fA-F]+\}|\\u[0-9a-fA-F]{4}|\\.", replace, body)


def parse_classes(source: str) -> list[DartClass]:
    classes: list[DartClass] = []
    matches = list(re.finditer(r"^/// (The translations for .*?)\nclass (\w+) extends (\w+) \{",
                               source, re.M | re.S))
    for index, match in enumerate(matches):
        doc, name, parent = match.group(1), match.group(2), match.group(3)
        end = matches[index + 1].start() if index + 1 < len(matches) else len(source)
        body = source[match.end():end]

        locale_name = None
        locale_match = re.search(r"super\.localeName = '([\w]+)'", body)
        if locale_match:
            locale_name = locale_match.group(1)

        text_direction = None
        direction_match = re.search(r"super\(TextDirection\.(\w+)\)", body)
        if direction_match:
            text_direction = direction_match.group(1)

        getters: list[tuple[str, str, bool]] = []
        for getter in re.finditer(
                r"@override\n  String(\??)\s+get\s+(\w+)\s*=>\s*(null|r?'(?:[^'\\]|\\.)*'"
                r"|r?\"(?:[^\"\\]|\\.)*\");", body):
            nullable = getter.group(1) == "?"
            value = getter.group(3)
            getters.append((getter.group(2), value, nullable))
        classes.append(DartClass(name, parent, doc.strip(), locale_name, text_direction, getters))
    return classes


def parse_supported_languages(source: str, variable: str) -> list[str]:
    match = re.search(variable + r"\s*=\s*HashSet<String>\.from\(const <String>\[(.*?)\]\)",
                      source, re.S)
    if not match:
        sys.exit(f"{variable} not found")
    return re.findall(r"'([\w]+)'", match.group(1))


def parse_switch(source: str, function: str, constructor_args: str) -> list[str]:
    """Transliterate `getXTranslation`'s switch statement into C#."""
    start = source.index(function + "(")
    body = source[source.index("switch (locale.languageCode) {", start):source.index(
        "assert(false", start)]
    lines: list[str] = []
    indent = 2
    for raw in body.splitlines():
        line = raw.strip()
        if not line:
            continue
        if line.startswith("switch (locale."):
            field = {"languageCode": "LanguageCode", "scriptCode": "ScriptCode",
                     "countryCode": "CountryCode"}[re.search(r"locale\.(\w+)", line).group(1)]
            lines.append("    " * indent + f"switch (locale.{field})")
            lines.append("    " * indent + "{")
            indent += 1
        elif line.startswith("case "):
            code = re.match(r"case '([\w\d]+)':", line).group(1)
            lines.append("    " * indent + f'case "{code}":')
            if line.endswith("{"):
                lines.append("    " * indent + "{")
                indent += 1
        elif line.startswith("return "):
            name = re.match(r"return (?:const )?(\w+)\(", line).group(1)
            body_indent = "    " * (indent + (0 if lines[-1].endswith(":") else 0))
            prefix = "    " * (indent + 1) if lines[-1].endswith(":") else "    " * indent
            lines.append(f"{prefix}return new {name}({constructor_args});")
        elif line == "}":
            indent -= 1
            lines.append("    " * indent + "}")
        else:
            sys.exit(f"unexpected line in {function}: {line!r}")
    return lines


def csharp_string(value: str) -> str:
    out = []
    for char in value:
        if char == '"':
            out.append('\\"')
        elif char == "\\":
            out.append("\\\\")
        elif char == "\n":
            out.append("\\n")
        elif char == "\r":
            out.append("\\r")
        elif char == "\t":
            out.append("\\t")
        elif ord(char) < 0x20 or ord(char) == 0x7F:
            out.append(f"\\u{ord(char):04x}")
        else:
            out.append(char)
    return '"' + "".join(out) + '"'


def subject(doc: str) -> str:
    """`The translations for Afrikaans (`af`).` -> `Afrikaans (`af`).`"""
    return doc.removeprefix("The translations for ")


def member(name: str) -> str:
    return name[0].upper() + name[1:]


def render_cupertino() -> str:
    source = CUPERTINO_DART.read_text(encoding="utf-8")
    classes = parse_classes(source)
    languages = parse_supported_languages(source, "kCupertinoSupportedLanguages")
    arguments = ", ".join(name for _, name in CUPERTINO_FORMATS)

    lines = [
        "// Generated by scripts/generate_localizations.py from",
        "// cupertino_ui/lib/src/l10n/generated_cupertino_localizations.dart. Do not edit by hand.",
        "",
        "#nullable enable",
        "",
        "using Plumix.Foundation.Intl;",
        "using Plumix.Widgets;",
        "",
        "namespace Plumix.Cupertino;",
        "",
    ]
    for entry in classes:
        parameters = [f"{kind} {name}," for kind, name in CUPERTINO_FORMATS]
        base_arguments = ", ".join(["localeName"] + [name for _, name in CUPERTINO_FORMATS])
        lines += [
            f"/// {entry.doc}",
            f"public class {entry.name} : {entry.parent}",
            "{",
            f"    /// Creates an instance of the translation bundle for {subject(entry.doc)}",
            f"    public {entry.name}(",
        ]
        lines += [f"        {parameter}" for parameter in parameters]
        lines.append(f"        string localeName = {csharp_string(entry.locale_name)})")
        if entry.parent == "GlobalCupertinoLocalizations":
            lines.append(f"        : base({base_arguments})")
        else:
            lines.append(f"        : base({', '.join(name for _, name in CUPERTINO_FORMATS)}, "
                         "localeName)")
        lines += ["    {", "    }", ""]
        for name, value, nullable in entry.getters:
            declaration = "protected override" if name in PROTECTED_NULLABLE | PROTECTED_REQUIRED \
                else "public override"
            kind = "string?" if name in PROTECTED_NULLABLE else "string"
            text = "null" if value == "null" else csharp_string(dart_string(value))
            lines.append(f"    {declaration} {kind} {member(name)} => {text};")
            lines.append("")
        if lines[-1] == "":
            lines.pop()
        lines += ["}", ""]

    lines += [
        "public abstract partial class GlobalCupertinoLocalizations",
        "{",
        "    /// The languages `GlobalCupertinoLocalizations.Delegate` supports.",
        "    public static IReadOnlySet<string> CupertinoSupportedLanguages { get; } =",
        "        new HashSet<string>(StringComparer.Ordinal)",
        "        {",
    ]
    lines += [f"            {csharp_string(language)}," for language in languages]
    lines += [
        "        };",
        "",
        "    /// <summary>",
        "    /// The translation bundle for <paramref name=\"locale\"/>, or null when it has none.",
        "    /// </summary>",
        "    public static GlobalCupertinoLocalizations? GetCupertinoTranslation(",
        "        Locale locale,",
    ]
    lines += [f"        {kind} {name}," for kind, name in CUPERTINO_FORMATS[:-1]]
    lines += [
        f"        {CUPERTINO_FORMATS[-1][0]} {CUPERTINO_FORMATS[-1][1]})",
        "    {",
    ]
    lines += parse_switch(source, "GlobalCupertinoLocalizations? getCupertinoTranslation",
                          arguments)
    lines += [
        "",
        "        return null;",
        "    }",
        "}",
        "",
    ]
    return "\n".join(lines)


def render_widgets() -> str:
    source = WIDGETS_DART.read_text(encoding="utf-8")
    classes = parse_classes(source)
    languages = parse_supported_languages(source, "kWidgetsSupportedLanguages")

    lines = [
        "// Generated by scripts/generate_localizations.py from",
        "// flutter_localizations/lib/src/l10n/generated_widgets_localizations.dart."
        " Do not edit by hand.",
        "",
        "#nullable enable",
        "",
        "using Plumix.UI;",
        "",
        "namespace Plumix.Widgets;",
        "",
    ]
    for entry in classes:
        lines += [
            f"/// {entry.doc}",
            f"public class {entry.name} : {entry.parent}",
            "{",
            f"    /// Creates an instance of the translation bundle for {subject(entry.doc)}",
        ]
        if entry.text_direction:
            direction = member(entry.text_direction)
            lines.append(f"    public {entry.name}()")
            lines.append(f"        : base(TextDirection.{direction})")
            lines += ["    {", "    }", ""]
        else:
            lines += [f"    public {entry.name}()", "    {", "    }", ""]
        for name, value, _ in entry.getters:
            text = "null" if value == "null" else csharp_string(dart_string(value))
            lines.append(f"    public override string {member(name)} => {text};")
            lines.append("")
        if lines[-1] == "":
            lines.pop()
        lines += ["}", ""]

    lines += [
        "public abstract partial class GlobalWidgetsLocalizations",
        "{",
        "    /// The languages `GlobalWidgetsLocalizations.Delegate` supports.",
        "    public static IReadOnlySet<string> WidgetsSupportedLanguages { get; } =",
        "        new HashSet<string>(StringComparer.Ordinal)",
        "        {",
    ]
    lines += [f"            {csharp_string(language)}," for language in languages]
    lines += [
        "        };",
        "",
        "    /// <summary>",
        "    /// The translation bundle for <paramref name=\"locale\"/>, or null when it has none.",
        "    /// </summary>",
        "    public static GlobalWidgetsLocalizations? GetWidgetsTranslation(Locale locale)",
        "    {",
    ]
    lines += parse_switch(source, "GlobalWidgetsLocalizations? getWidgetsTranslation", "")
    lines += [
        "",
        "        return null;",
        "    }",
        "}",
        "",
    ]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    for path, rendered in ((CUPERTINO_OUT, render_cupertino()), (WIDGETS_OUT, render_widgets())):
        if args.check:
            current = path.read_text(encoding="utf-8") if path.is_file() else ""
            if current != rendered:
                print(f"{path} is out of date; run scripts/generate_localizations.py",
                      file=sys.stderr)
                return 1
        else:
            path.write_text(rendered, encoding="utf-8")
            print(f"wrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
