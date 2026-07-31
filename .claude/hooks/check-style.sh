#!/usr/bin/env bash
# PostToolUse hook: after an Edit/Write to a C# file, check the 120-char rule on the lines
# just written and feed violations straight back to the agent (exit 2 = blocking feedback).
#
# The other half of the style contract — explicit types for built-ins — is IDE0008, which
# EnforceCodeStyleInBuild reports as a compile error, so it needs no hook.

set -uo pipefail

payload=$(cat)
file=$(printf '%s' "$payload" | python3 -c \
    'import json,sys; print(json.load(sys.stdin).get("tool_input", {}).get("file_path", ""))' 2>/dev/null)

[[ "$file" == *.cs ]] || exit 0
[[ "$file" == *.g.cs ]] && exit 0

cd "${CLAUDE_PROJECT_DIR:-$(dirname "$0")/../..}" || exit 0

# Only files tracked by this repo; edits elsewhere are not ours to police.
case "$file" in
    "$PWD"/*) rel="${file#"$PWD"/}" ;;
    /*) exit 0 ;;
    *) rel="$file" ;;
esac

output=$(scripts/check_line_length.sh "$rel" 2>&1)
status=$?

if [[ $status -ne 0 ]]; then
    printf '%s\n' "$output" >&2
    exit 2
fi

exit 0
