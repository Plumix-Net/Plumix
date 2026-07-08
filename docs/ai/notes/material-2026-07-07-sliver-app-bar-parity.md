# Feature: SliverAppBar + FlexibleSpaceBar parity

## Goal

- Port the connected Flutter `SliverAppBar` and `FlexibleSpaceBar` controls with framework-owned persistent-header layout and mirrored sample coverage.

## Non-Goals

- Rebuilding the complete scroll-activity/ballistic physics protocol in this control iteration.

## Context Plan

- Entry files: Flutter `app_bar.dart`/`flexible_space_bar.dart`, Plumix scroll/sliver rendering, Material AppBar/theme, and their focused tests.
- Expansion trigger: add source-required persistent-header and aligned-transform primitives before closing control composition.

## Delivery Scope

- Target controls: `SliverAppBar` and `FlexibleSpaceBar`.
- Completed: API/defaults, composition, collapse/floating/pinned states, layout/paint, theme precedence, focused tests, and mirrored sample.

## Invariants Impacted

- `INVARIANTS.md` and `PORTING_MODE.md` reviewed; behavior stays in Widget/Element/RenderObject layers and sample changes remain paired.

## Dart Reference Mapping

- Sources: `flutter/packages/flutter/lib/src/material/app_bar.dart` and `flexible_space_bar.dart`.
- Divergence: `docs/ai/DIVERGENCES.md` records missing ballistic snap settling, overscroll stretch, and same-layout delegate rebuilding because the shared viewport clamps negative offsets and exposes no header snap/activity or render-layout callback protocol.

## Test Plan

- `MaterialSliverAppBarTests.cs`: contracts/defaults, persistent-header geometry, floating reveal, flexible-space composition, scroll-under theme precedence, custom-scroll collapse, and aligned transforms.
- Full `Plumix.Tests` suite plus C#/Dart sample builds.

## Done Criteria

- The two connected controls are closed for ordinary collapse/pinned/floating and M3 variant use; the interaction deltas tied to missing shared scroll protocols have an explicit close condition.
