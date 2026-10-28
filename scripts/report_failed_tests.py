#!/usr/bin/env python3
"""Print the failed tests of one or more TRX files, so a CI log ends with what actually failed.

`dotnet test` reports a failure where it happens, thousands of lines above the summary, and the
summary itself names no test. This reads the TRX logs the test steps write and prints every
failed test with its message and stack trace. On GitHub Actions it also emits an `::error`
annotation per test (visible on the run page) and a table in the step summary.

Usage:
    scripts/report_failed_tests.py TestResults/            # every *.trx under the directory
    scripts/report_failed_tests.py a.trx b.trx

Always exits 0: the test step has already failed the job, this step only reports.
"""

from __future__ import annotations

import os
import sys
import xml.etree.ElementTree as ElementTree
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
ANNOTATION_LIMIT = 4000


def trx_files(arguments: list[str]) -> list[Path]:
    files: list[Path] = []
    for argument in arguments:
        path = Path(argument)
        if path.is_dir():
            files.extend(sorted(path.rglob("*.trx")))
        elif path.is_file():
            files.append(path)
    return files


def failed_results(trx: Path) -> list[tuple[str, str, str]]:
    root = ElementTree.parse(trx).getroot()
    failures: list[tuple[str, str, str]] = []
    for result in root.iterfind(".//t:Results/t:UnitTestResult", NS):
        if result.get("outcome") != "Failed":
            continue
        message = result.findtext("t:Output/t:ErrorInfo/t:Message", default="", namespaces=NS)
        stack = result.findtext("t:Output/t:ErrorInfo/t:StackTrace", default="", namespaces=NS)
        failures.append((result.get("testName", "?"), message.strip(), stack.rstrip()))
    return failures


def escape_command(value: str) -> str:
    """Workflow-command data encoding: `%`, CR and LF are the characters a message must escape."""
    return value.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")


def main() -> int:
    files = trx_files(sys.argv[1:] or ["TestResults"])
    if not files:
        print("No TRX files found; the test run did not get far enough to write one.")
        return 0

    failures = [failure for trx in files for failure in failed_results(trx)]
    on_actions = os.environ.get("GITHUB_ACTIONS") == "true"
    rule = "=" * 100
    print(rule)
    print(f"Failed tests: {len(failures)}")
    print(rule)
    for name, message, stack in failures:
        print(f"\n--- {name}\n{message}")
        if stack:
            print(stack)
        if on_actions:
            # Properties additionally escape `:` and `,`, which delimit them.
            title = escape_command(name).replace(":", "%3A").replace(",", "%2C")
            print(f"::error title={title}::{escape_command(message[:ANNOTATION_LIMIT])}")

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary and failures:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(f"### Failed tests ({len(failures)})\n\n")
            for name, message, _ in failures:
                first_line = message.splitlines()[0] if message else ""
                handle.write(f"- `{name}`: {first_line}\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
