#!/usr/bin/env bash
# Enforce the 120-character limit from .editorconfig on *added or modified* lines only.
#
# The repo has ~1000 pre-existing long lines and AGENTS.md forbids mass-reformatting
# untouched code, so a whole-tree gate is wrong here. This checks the diff instead.
#
# Usage:
#   scripts/check_line_length.sh                        # working tree vs HEAD
#   scripts/check_line_length.sh --base origin/main     # branch vs a base ref (CI)
#   scripts/check_line_length.sh src/Plumix/Foo.cs      # limit to given paths (editor hook)
#
# Exits 1 and prints offending lines when any added line exceeds the limit.

set -uo pipefail

LIMIT=120
BASE=""
PATHS=()

while [[ $# -gt 0 ]]; do
    case "$1" in
        --base) BASE="${2:-}"; shift 2 ;;
        *) PATHS+=("$1"); shift ;;
    esac
done

cd "$(git rev-parse --show-toplevel)" || exit 1

if [[ ${#PATHS[@]} -eq 0 ]]; then
    PATHS=('*.cs')
fi

if [[ -n "$BASE" ]]; then
    DIFF=$(git diff --unified=0 "$BASE"...HEAD -- "${PATHS[@]}")
else
    DIFF=$(git diff --unified=0 HEAD -- "${PATHS[@]}")
fi

violations=$(printf '%s\n' "$DIFF" | awk -v limit="$LIMIT" '
    /^\+\+\+ b\// { file = substr($0, 7); next }
    /^@@/ {
        match($0, /\+[0-9]+/)
        line = substr($0, RSTART + 1, RLENGTH - 1) + 0
        next
    }
    /^\+/ {
        text = substr($0, 2)
        if (length(text) > limit && file ~ /\.cs$/ && file !~ /\.g\.cs$/) {
            printf "%s:%d: %d chars (limit %d)\n", file, line, length(text), limit
        }
        line++
    }
')

if [[ -n "$violations" ]]; then
    echo "Line length over ${LIMIT} on new/edited lines (.editorconfig, docs/ai/INVARIANTS.md):" >&2
    printf '%s\n' "$violations" >&2
    echo "Wrap them: one argument per line, break chains before '.', split conditions before && / ||." >&2
    exit 1
fi

exit 0
