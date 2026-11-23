# Versioning

Updated: 2026-11-23. Applies to every NuGet package built from `Plumix` and `Plumix.Packages`.

## Rule

A package carries the version of the upstream it ports. The version says which upstream release the port
targets; the prerelease label says whether it has reached it yet.

| Stage | Version | Meaning |
| --- | --- | --- |
| Catching up | `X.Y.Z-preview.N` | Port of upstream `X.Y.Z`, not yet at full parity; `N` counts our releases |
| Parity | `X.Y.Z` | Full parity with upstream `X.Y.Z` |
| Our fixes after parity | `X.Y.Z.N` | Plumix-only fixes; never `X.Y.(Z+1)`, which belongs to upstream's next release |

The pin does not move until the port reaches parity with it: a new upstream release is ported only after
the current one ships as a bare `X.Y.Z`. Precedent: `Plumix.NImpeller` (`3.50.0.5`) and the
Xamarin.AndroidX bindings use the upstream version plus a fourth segment.

## Package lines

| Package | Upstream | Pin | Line in | Tag |
| --- | --- | --- | --- | --- |
| `Plumix`, `Plumix.Avalonia`, `Plumix.Android`, `Plumix.iOS`, `Plumix.Impeller`, `Plumix.FSharp`, `Plumix.Elmish`, `Plumix.Templates` | Flutter SDK | 3.47.0 | `Plumix/src/Versions.props` `PlumixVersion` | `v<version>` |
| `Plumix.Cupertino` | [`cupertino_ui`](https://pub.dev/packages/cupertino_ui) | 1.1.7 | `PlumixCupertinoVersion` | `cupertino-v<version>` |
| `Plumix.Material` | [`material_ui`](https://pub.dev/packages/material_ui) | 1.11.1 | `PlumixMaterialVersion` | `material-v<version>` |
| `Plumix.Provider` | [`provider`](https://pub.dev/packages/provider) | 6.1.5 | `Plumix.Packages/src/Versions.props` `PlumixProviderVersion` | `provider-v<version>` |
| `Plumix.Bloc` | [`flutter_bloc`](https://pub.dev/packages/flutter_bloc) | 9.1.1 | `PlumixBlocVersion` | `bloc-v<version>` |
| `Plumix.Bloc.Core` | [`bloc`](https://pub.dev/packages/bloc) | 9.2.1 | `PlumixBlocCoreVersion` | `bloc-core-v<version>` |
| `Plumix.Slang` | [`slang_flutter`](https://pub.dev/packages/slang_flutter) | 4.19.0 | `PlumixSlangVersion` | `slang-v<version>` |

Hosts, the F# DSL and the template have no upstream of their own and follow the Flutter line.
Dart's build suffix (`provider 6.1.5+1`) is dropped: NuGet ignores `+metadata`.

The Material and Cupertino ports were validated against `material_ui` 1.0.0 and `cupertino_ui` 1.0.0; the
reference pins moved to 1.11.1 / 1.1.7 on 2026-11-23 (`AGENTS.md`, Local Reference Paths). The `-preview` label
stays until the 1.0.0 → 1.11.1 / 1.1.7 re-port table in `docs/ai/BACKLOG.md` is closed.

## Dependencies between packages

Packages reference each other with `ProjectReference`, so a nuspec floor is the referenced package's line in
`Versions.props` (for example `Plumix.Material 1.11.1-preview.1` → `Plumix >= 3.47.0-preview.1`,
`Plumix.Cupertino >= 1.1.7-preview.1`). `Plumix.Packages` depends on Plumix through `PlumixVersion` in its
`Directory.Packages.props`: the lowest Plumix it is tested against, independent of the Plumix line.

Never pass `-p:Version` to `dotnet pack`: as a global property it restamps every ProjectReference floor.

## Releasing

1. Bump the line(s) in `src/Versions.props` and collapse `CHANGELOG.md`; commit.
2. Tag the commit once per bumped line and push the tags together:
   `git push --atomic origin main v3.47.0-preview.2 material-v1.11.1-preview.2`. GitHub creates no events when
   more than three tags are pushed at once; then no run starts — delete one of the tags on origin and push it
   again on its own (`git push origin :refs/tags/<tag>` then `git push origin <tag>`): that single run sees all
   the tags and releases every line.
3. CI (`scripts/release_plan.sh`) checks that the tag matches `Versions.props` and that every line whose version
   is not on nuget.org has its tag on origin, then pushes every package with `--skip-duplicate`: lines already
   published are skipped, and lines that depend on each other (Material on core, the F# DSL and the template on
   Material) ship in one run.
4. Downstream: bump `NetBench` (`Directory.Packages.props`) and anything that pins these versions.

## Breaking changes

The major version belongs to upstream, so it cannot signal our breaking changes. While a line is in
`-preview`, breaking changes are allowed and are called out in `CHANGELOG.md` with a `Breaking:` prefix.
After parity, a fourth-segment release must not break the public API; a break waits for the next upstream pin.
