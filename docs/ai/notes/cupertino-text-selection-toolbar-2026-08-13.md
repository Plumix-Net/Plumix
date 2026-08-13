# Cupertino text-selection toolbar closeout — 2026-08-13

The Cupertino mobile/desktop/adaptive/spell-check toolbar family and Android default spell-check host service are
closed behaviorally against Flutter 3.44.0. Material adaptive routing now selects Cupertino controls on iOS/macOS.

One rendering-only divergence remains: Plumix has no rounded-superellipse path, arbitrary-path shadow, or retained
clip-layer handle. The mobile arrow therefore does not contribute to the light shadow, the desktop toolbar uses the
existing continuous rounded rectangle, and mobile clip layers are recreated. The exact close step is tracked in
`docs/ai/DIVERGENCES.md`; it requires shared rendering primitives rather than control-local workarounds.
