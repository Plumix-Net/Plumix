# Migration: Flutter pin 3.44.0 → 3.47.0 (2026-08-13)

## What was done

- `flutter-src` checkout moved from tag `3.44.0` (`559ffa3f75e`) to tag `3.47.0` (`4cf24164269`,
  `flutter-3.47-candidate.0`); pin line in `AGENTS.md` updated.
- The material/cupertino extraction landed as **pub packages** (`material_ui` / `cupertino_ui`,
  developed in `flutter/packages`), not as a path change inside the SDK repo — the SDK still carries
  frozen copies under `packages/flutter/lib/src/material|cupertino/`. At the pinned versions
  (`material_ui` 1.0.0, `cupertino_ui` 1.0.0) the package sources are code-identical to the SDK
  copies (only doc comments and constructor-style modernization differ; verified by stripped diff of
  10 controls). The packages are a superset: they add `l10n/`, `global_*_localizations.dart`, and
  `migration_utility.dart`, and they ship Flutter's own `test/` directory.
- Source of truth for Material/Cupertino ports switched to the packages: `dart_sample` migrated to
  `package:material_ui`/`package:cupertino_ui` imports (`dart fix --code=migrate_design_widgets`),
  all material/cupertino parity markers rewritten to `material_ui/lib/src/...` /
  `cupertino_ui/lib/src/...`, `generate_port_map.py` taught to resolve them against the new
  gitignored `material-ui-src`/`cupertino-ui-src` symlinks (pointing into the pub cache), and the
  porting docs (`AGENTS.md`, `DART_SPEC_PROTOCOL.md`, `PORT_PLAYBOOK.md`, `dart-spec` agent) updated.
- Fixed two pre-existing `dart_sample` analyzer errors surfaced by the SDK update (present with the
  old imports too): `WidgetStateProperty<T>.resolveWith(...)` → `WidgetStateProperty.resolveWith<T>(...)`
  in the scrollbar demo, and the search demo's `_TermSearchDelegate` now extends
  `SearchDelegate<String?>` so `close(context, null)` typechecks. Remaining analyzer output is
  21 deprecation infos (`useMaterial3`, `ButtonBar`, `year2023` flags) — intentional demo choices,
  revisit together with the corresponding C# defaults.
- `docs/ai/PORT_MAP.md` regenerated; all parity markers now resolve. Six stale markers fixed:
  - `widgets/visibility.dart` → `widgets/indexed_stack.dart` (file renamed upstream in 3.47,
    `Visibility`/`SliverVisibility` moved verbatim, `IndexedStack` moved in from `basic.dart`).
  - Five markers that were wrong since before the pin move: `rendering/relative_rect.dart` →
    `rendering/stack.dart`; `rendering/semantics.dart` → `semantics/semantics.dart` (4 files);
    `scheduler/scheduler.dart` dropped (kept `scheduler/binding.dart`); `services/semantics.dart` →
    `semantics/semantics_service.dart`; `widgets/widgets_localizations.dart` dropped (kept
    `widgets/localizations.dart`).
- `dotnet build src/Plumix.Ci.slnf` and the full test suite (2684 tests) green after the change.

## Upstream 3.44.0..3.47.0 audit

97 Dart files changed under `packages/flutter/lib/src/`; 66 of them intersect the ported set in
`PORT_MAP.md`. Each intersecting diff was reviewed and classified. 26 are doc-only or internal
refactors (no action). The 40 behavior-bearing changes below are the **re-port backlog**: existing
C# ports were validated against 3.44.0 and must absorb these deltas before the affected control is
next touched (or as dedicated parity iterations). Ordered roughly by subsystem.

### Painting / animation

- `animation/animation_style.dart` — new `AnimationStyle.merge()`; `lerp` now truly interpolates
  Durations (microseconds, null→0) and Curves (`_LerpedCurve`, null→linear) instead of t<0.5 snap.
- `painting/borders.dart` — `ShapeBorder.lerp`/`OutlinedBorder.lerp` gain reversed-timeline fallback
  `b?.lerpTo(a,1-t) ?? a?.lerpFrom(b,1-t)` before the snap; smoother cross-class border lerp.
- `painting/gradient.dart` — new virtual `Gradient.fromColor(Color)` (uniform-color copy, geometry
  preserved) with covariant overrides on Linear/Radial/SweepGradient.
- `painting/shape_decoration.dart` — `lerp` color↔gradient converts the color side via
  `Gradient.fromColor` and lerps as gradients; result nulls `color` when a gradient is produced.
- `painting/image_stream.dart` — `ImageInfo.isCloneOf` scale-compare bug fixed; new
  `ImageStreamListener.reportErrors` (default true); completer suppresses error reporting once a
  non-reporting listener was attached.
- `painting/text_painter.dart` — strut line-height path: empty `getBoxesForRange` falls back to
  `preferredLineHeight` instead of throwing.

### Gestures / rendering / semantics

- `gestures/recognizer.dart` — recognizer records first event's `buttons` per pointer; new
  `getButtonsForPointer(int pointer)`.
- `gestures/binding.dart` + `gestures/hit_test.dart` — engine hit-test query hook
  (`platformDispatcher.onHitTest`, marker mixin `NativeHitTestTarget`); only relevant if the host
  exposes a native hit-test query.
- `rendering/binding.dart` — new `getRectOfSemanticsNodeInViewCoordinates(viewId, nodeId)`.
- `rendering/box.dart` — `globalToLocal` early-out `Offset.zero` when local view direction z==0
  (was NaN via div-by-zero).
- `rendering/object.dart` — `debugNeeds*` return false in release instead of throwing;
  system-fonts relayout tolerates mid-frame notification; `_SemanticsParentData` compares
  `accessibilityFocusBlockType` and blocked nodes set `isFocused = null`; merging boundary with
  sibling nodes now builds inner node + synthetic merging boundary `[innerNode, ...siblings]`.
- `rendering/paragraph.dart` — new `devicePixelRatio` property (web repaint); selection highlight
  painted under text, handles above; semantics configs merged in encounter order with
  first-tag placeholder ownership; selection endpoints honor affinity; empty-rect drag paths call
  `_setSelectionPosition` before returning.
- `rendering/table.dart` — `findRowIndex` bound fix (`_rows - 1`): bottom-edge points no longer
  resolve to a nonexistent row.
- `semantics/semantics.dart` — custom-action dispatch on merged nodes finds the owning descendant
  by action id; custom-action map changes dirty the node; new `SemanticsOwner.getSemanticsNode(id)`;
  traversal grafting goes through the dirty pipeline and orphaned grafted children are filtered from
  hit-test order.

### Core widgets

- `widgets/basic.dart` + `widgets/indexed_stack.dart` — `IndexedStack` children now wrapped in
  `_VisibilityScope` + `ExcludeFocus` instead of full `Visibility(maintain*: true)`: no intermediate
  render objects (so `Positioned`/ParentDataWidgets finally work under IndexedStack), non-selected
  children are focus-excluded, hiding is solely `RenderIndexedStack`'s job. `Visibility` itself is
  unchanged. `RichText` now passes devicePixelRatio (MediaQuery ?? View ?? 1.0) to RenderParagraph.
- `widgets/actions.dart` — `Actions.find/maybeFind<Intent>` without intent arg legal; wrong-type
  match → descriptive error (debug) / null (release); `Action.overridable` resolves the override by
  runtime intent type in invoke/isEnabled/consumesKey.
- `widgets/animated_cross_fade.dart` — new `clipBehavior` (default `Clip.hardEdge`).
- `widgets/animated_scroll_view.dart` — separated-list `removeItem` uses
  `_itemsCount - _outgoingItemsCount` for the last-item check (correct separator removal during
  concurrent removals).
- `widgets/autocomplete.dart` — options overlay wrapped in `ExcludeFocus`.
- `widgets/context_menu_controller.dart` — `show` while shown updates the existing overlay entry in
  place (`markNeedsBuild`, no remove/re-insert; superseded instance's `onRemove` not fired);
  `InheritedTheme.capture` moved inside the builder.
- `widgets/image.dart` — passes `reportErrors: errorBuilder == null` to its stream listener.
- `widgets/image_icon.dart` — new `useOriginalColors` (true disables the srcIn tint).
- `widgets/navigator.dart` — `NavigationNotification.canHandlePop` computed at dispatch time;
  bubbling-notification rewrite respects `PopScope(canPop: false)` on the root route; debug assert
  that pop result type matches route `T`.
- `widgets/overlay.dart` — `OverlayPortal` deferred-child add/remove moved to layout-surrogate
  attach/detach; fixes GlobalKey-reparenting crashes; no public API change.
- `widgets/ticker_provider.dart` — `TickerMode.getValuesNotifier` returns the fallback listenable
  when `!context.mounted` (no crash when first touched in `dispose`).
- `widgets/view.dart` — each view's `FocusScope` attaches to `FocusManager.rootScope` via new
  `FocusTraversalGroup.parentNode`; ancestor views no longer steal native focus.
- `widgets/focus_manager.dart` — suspended node restored on resume only if nothing else requested
  focus meanwhile.
- `widgets/focus_traversal.dart` — `FocusTraversalGroup` gains `FocusNode? parentNode`.
- `widgets/focus_scope.dart` — debug-only `debugPaintFocusBoxes` painting.

### Scrolling / text stack

- `widgets/scrollable.dart` — semantics `hasImplicitScrolling = allowImplicitScrolling` set
  unconditionally (was gated on `haveDimensions`).
- `widgets/scrollable_helpers.dart` — `EdgeDraggingAutoScroller` refuses/stops auto-scroll when
  `resolvedPhysics.shouldAcceptUserOffset` is false.
- `widgets/overscroll_indicator.dart` — stretch indicator resets acceptance/overscroll on
  ScrollStart (rejection no longer sticky); `scrollEnd` only when accepted.
- `widgets/sliver_resizing_header.dart` — child wrapped in `Semantics(container: true,
  explicitChildNodes: true)`; partially collapsed header tags children `excludeFromScrolling`.
- `widgets/editable_text.dart` — spell check force-disabled for password inputs and re-inferred in
  `didUpdateWidget`; `contextMenuBuilder` identity change no longer recreates the selection overlay
  (only null↔non-null); toolbar-on-screen scheduling uses `scheduleFrameCallback` when idle.
- `widgets/selectable_region.dart` — Escape hides toolbar; handles shown before toolbar;
  select-word/paragraph outside all selectables clamps to the nearest selectable; multi-selectable
  edge-init sweep rewritten (starts at opposite edge's index, bidirectional, reversal → `end`).
  `widgets/text.dart` dropped its private `_initSelection` and now inherits this sweep.
- `widgets/text_selection.dart` — `showToolbar` asserts not-in-persistentCallbacks; handle dy
  computation returns null on degenerate metrics (drag handlers early-return); contextMenuBuilder
  toolbar inserted just above the end handle instead of overlay top.
- `widgets/tap_region.dart` — `RenderTapRegionSurface` handles semantics tap/longPress actions
  (synthetic PointerDownEvent at node-rect center); pointer path refactored onto shared
  `_classifyRegions`; outside-set materialized for safe mutation during iteration.
- `widgets/raw_menu_anchor.dart` — `onCloseRequested` fires on every `MenuController.close` even
  when already closed; root anchor detaches its scroll listener on dispose.

### Doc-only / internal (no action)

`animation/animation_controller.dart`, `animation/animations.dart`, `foundation/key.dart`,
`foundation/licenses.dart`, `material/about.dart`, `material/date_picker.dart`,
`material/scaffold.dart`, `painting/image_provider.dart`, `scheduler/ticker.dart`,
`services/binding.dart`, `widgets/form.dart`, `widgets/framework.dart`,
`widgets/layout_builder.dart`, `widgets/magnifier.dart`, `widgets/media_query.dart`,
`widgets/page_view.dart`, `widgets/routes.dart`, `widgets/scroll_metrics.dart`,
`widgets/scroll_physics.dart`, `widgets/scroll_position.dart`, `widgets/scroll_view.dart`,
`widgets/shortcuts.dart`, `widgets/sliver.dart`, `widgets/dual_transition_builder.dart`,
`widgets/draggable_scrollable_sheet.dart`, `widgets/text.dart` (behavior tracked under
selectable_region).
