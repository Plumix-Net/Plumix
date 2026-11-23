#!/usr/bin/env bash
# Check a release tag against src/Versions.props and list the packages a release pushes
# (policy: docs/VERSIONING.md). Prints NAME=value lines for $GITHUB_ENV.
#
# Package lines and their tags:
#   core       v<PlumixVersion>                     Plumix, hosts, F# DSL, template (Flutter line)
#   cupertino  cupertino-v<PlumixCupertinoVersion>  Plumix.Cupertino (cupertino_ui line)
#   material   material-v<PlumixMaterialVersion>    Plumix.Material (material_ui line)
#
# The lines depend on each other both ways (Material on core, the F# DSL and the template on
# Material), so a release does not publish one line: it pushes every package at its Versions.props
# version with --skip-duplicate, and lines already on nuget.org are skipped. Every line whose version
# is not on nuget.org yet must carry its own tag on origin, so no version ships untagged — push all
# release tags of a commit in one `git push origin <tag> <tag>...`.
#
# Usage:
#   scripts/release_plan.sh v3.47.0-preview.1 >> "$GITHUB_ENV"

set -euo pipefail

tag="${1:?usage: release_plan.sh <tag>}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
versions="$root/src/Versions.props"

read_version() {
  sed -n "s:.*<$1>\(.*\)</$1>.*:\1:p" "$versions"
}

core="$(read_version PlumixVersion)"
cupertino="$(read_version PlumixCupertinoVersion)"
material="$(read_version PlumixMaterialVersion)"

# line | tag prefix | version | packages (the first one probes nuget.org for the line)
lines=(
  "core|v|$core|Plumix Plumix.Avalonia Plumix.FSharp Plumix.Elmish Plumix.Impeller Plumix.Android Plumix.iOS Plumix.Templates"
  "cupertino|cupertino-v|$cupertino|Plumix.Cupertino"
  "material|material-v|$material|Plumix.Material"
)

version_pattern='^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?$'

tag_line=""
for entry in "${lines[@]}"; do
  IFS='|' read -r line prefix version _ <<< "$entry"
  if ! [[ "$version" =~ $version_pattern ]]; then
    echo "::error::src/Versions.props: the $line line has no valid version ('$version')" >&2
    exit 1
  fi
  if [[ "$tag" == "$prefix"* && "${tag#"$prefix"}" =~ ^[0-9] ]]; then
    tag_line="$line"
    if [[ "${tag#"$prefix"}" != "$version" ]]; then
      echo "::error::Tag '$tag' does not match the $line line in src/Versions.props ($version)." \
        "Bump Versions.props in the tagged commit." >&2
      exit 1
    fi
  fi
done
if [[ -z "$tag_line" ]]; then
  echo "::error::Unknown release tag '$tag' (expected v*, cupertino-v* or material-v*)" >&2
  exit 1
fi

packages=()
for entry in "${lines[@]}"; do
  IFS='|' read -r line prefix version ids <<< "$entry"
  probe="${ids%% *}"
  index="https://api.nuget.org/v3-flatcontainer/$(tr '[:upper:]' '[:lower:]' <<< "$probe")/index.json"
  published="$(curl -fsSL "$index" 2>/dev/null || echo '{"versions":[]}')"
  lower_version="$(tr '[:upper:]' '[:lower:]' <<< "$version")"
  if ! grep -q "\"$lower_version\"" <<< "$published"; then
    if ! git -C "$root" ls-remote --exit-code --tags origin "refs/tags/$prefix$version" > /dev/null; then
      echo "::error::The $line line ($version) is not on nuget.org and has no tag $prefix$version on origin." \
        "Tag it on this commit and push the release tags together." >&2
      exit 1
    fi
    echo "::notice::Releasing the $line line: $version" >&2
  fi
  for id in $ids; do
    packages+=("$id.$version")
  done
done

echo "RELEASE_PACKAGES=${packages[*]}"
