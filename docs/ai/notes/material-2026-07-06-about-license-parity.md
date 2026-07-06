# Feature: AboutDialog + LicensePage parity

## Goal

- Port Flutter's about/license controls with real registry, parsing, modal, package-list, and detail navigation behavior.

## Dart References

- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/about.dart`
- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/foundation/licenses.dart`
- `dart_sample/lib/demos/material/about_demo_page.dart`

## Completion

- [x] `AboutDialog`, `LicensePage`, and source-coupled `AboutListTile` APIs.
- [x] Metadata/icon/legalese/custom children and M2/M3 localized actions.
- [x] Lazy registry, paragraph parsing, package grouping/sorting, plural labels, and detail routes.
- [x] Focused tests and mirrored C#/Dart demo route.
- [x] Tracking docs and divergence registry updated.

## Remaining Divergence

- Cupertino adaptive actions and Flutter's wide master-detail license shell await shared primitives; tracked in `docs/ai/DIVERGENCES.md`.
