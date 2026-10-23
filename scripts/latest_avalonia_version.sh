#!/usr/bin/env bash
# Print the newest stable Avalonia on nuget.org with the same major version as the AvaloniaVersion
# Plumix compiles against (src/Directory.Packages.props). CI passes it as -p:AvaloniaTestVersion to
# run the test suite on that Avalonia while the Plumix assemblies stay built against the minimum.
#
# Usage:
#   scripts/latest_avalonia_version.sh          # e.g. 12.1.4

set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
minimum="$(sed -n 's:.*<AvaloniaVersion>\(.*\)</AvaloniaVersion>.*:\1:p' "$root/src/Directory.Packages.props")"
if [[ -z "$minimum" ]]; then
  echo "AvaloniaVersion not found in src/Directory.Packages.props" >&2
  exit 1
fi

curl -fsSL https://api.nuget.org/v3-flatcontainer/avalonia/index.json | python3 -c '
import json, sys
major = sys.argv[1].split(".")[0]
stable = [v for v in json.load(sys.stdin)["versions"] if "-" not in v and v.split(".")[0] == major]
print(max(stable, key=lambda v: tuple(int(part) for part in v.split("."))))
' "$minimum"
