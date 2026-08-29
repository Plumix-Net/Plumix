# Backlog — open work not tied to a single control row

The one place for work that is known to be open but does not fit `docs/CUPERTINO_TODO.md` (a
Cupertino file), `docs/ai/DIVERGENCES.md` (an intentional, documented divergence) or a qualified
marker in `docs/ai/PORT_MAP.md` (an admitted approximate port). Created 2026-08-16 from an audit of
the retired per-iteration notes; everything else in those notes was verified closed or already tracked.

Rules:

- One row per item: **what is open**, **where** (C# file + Dart source), **next step**. No history, no
  rationale beyond one clause.
- Remove the row in the same change that closes it (and add the `CHANGELOG.md` line).
- An iteration that ends blocked adds a row here (`PORT_PLAYBOOK.md` Step 8) instead of a note file.
- `PORT_PLAYBOOK.md` Step 0 reaches this file only after the Cupertino TODO, the divergence registry
  and the qualified-marker list are empty — but a row here may be picked any time it unblocks a control.

## Control-level gaps found outside their rows

| Item | Where | Next step |
| --- | --- | --- |
| `PipelineOwner` is a single owner: `PipelineManifold`, the child-owner tree (`adoptChild`/`dropChild`/`visitChildren`/`attach`/`detach`), the deprecated `ensureSemantics`/`SemanticsHandle` pair, the nullable/disposable `SemanticsOwner` lifecycle and `Dispose` are all absent. | `src/Plumix/PipelineOwner.cs` vs `rendering/object.dart` (953–2003) | Port `PipelineManifold` over `IListenable`, make `SemanticsOwner` nullable and created on demand, then add the owner tree and `Dispose`; needed before multi-view. |
| `RelayoutWhenSystemFontsChangeMixin` is not ported, and neither primitive it needs exists: a one-shot transient frame callback on `Scheduler` and a `PaintingBinding.systemFonts` listenable. | `src/Plumix/Scheduler.cs`, `src/Plumix/Painting/*` vs `rendering/object.dart` (4693–4753) | Add `Scheduler.ScheduleFrameCallback` and a system-fonts `ChangeNotifier` fed by the hosts, then port the mixin onto `RenderParagraph` and friends. |
| `RenderObjectWithLayoutCallbackMixin`'s `_needsRebuild` state is not ported: `IRenderObjectWithLayoutCallback` exists and `PipelineOwner` admits such nodes into the dirty list, but `LayoutBuilder`/`SliverLayoutBuilder`/`OverlayPortal` still call `MarkNeedsLayout` ad hoc, so a callback in a subtree an ancestor declines to lay out never runs. | `src/Plumix/Widgets/LayoutBuilder.cs`, `SliverLayoutBuilder.cs`, `src/Plumix/Rendering/Overlay.cs` vs `rendering/object.dart` (4291–4336) | Move `_needsRebuild`/`RunLayoutCallback`/`ScheduleLayoutCallback` onto `RenderObject` behind `IRenderObjectWithLayoutCallback` and have the three consumers use them. |
| `SemanticsConfiguration.absorb` copies action handlers with `TryAdd` (first wins); Dart's `_actions.addAll(child._actions)` overwrites (last wins), so a merged node can keep an outer handler where Flutter would take the absorbed one. | `src/Plumix/Rendering/Semantics.cs` (`Absorb`, `CopyActionHandlersTo`) vs `semantics/semantics.dart:6817` | Switch both copies to indexer assignment and add coverage from `semantics_test.dart`'s absorb cases. |
| Timeline/profiling and the `debugPrint*` layout hooks are absent: `debugPrintMarkNeedsLayoutStacks`, `debugPrintMarkNeedsPaintStacks`, `debugPrintLayouts`, `debugProfileLayoutsEnabled`, `debugProfilePaintsEnabled`, `debugRepaintRainbowEnabled`, `debugPaintLayerBordersEnabled`, `debugOnProfilePaint`. | `src/Plumix/Rendering` (no `Debug.cs`) vs `rendering/debug.dart`, `object.dart` | Add `rendering/debug.dart`'s flag surface, then wire the hooks into `MarkNeedsLayout`/`MarkNeedsPaint`/`Layout`/`_paintWithContext`/`StopRecordingIfNeeded`. |
| `Scheduler` has only `CurrentFrameTimeStamp`; Dart separates `currentFrameTimeStamp` (time-dilated) from `currentSystemFrameTimeStamp` (raw), and `MultitouchDragStrategy.AverageBoundaryPointers` buckets its per-frame deltas on the raw one. | `src/Plumix/Scheduler.cs` vs `scheduler/binding.dart` | Track the raw timestamp alongside the dilated one and read it from `DragGestureRecognizer.ResolveLocalDeltaForMultitouch`. |
| `RenderObject.DebugCreator` is settable but nothing sets it: Dart's `RenderObjectElement.mount` assigns `DebugCreator(this)`, whose `toString` is `Element.debugGetCreatorChain(12)`. | `src/Plumix/Widgets/Framework.RenderObject.cs`, `Framework.Element.cs` vs `widgets/framework.dart` | Port `DebugCreator` and `Element.debugGetCreatorChain`, then assign the creator in `RenderObjectElement.Mount`; the diagnostics wrapper `DiagnosticsDebugCreator` already exists. |
| `TextStyle` has no `debugFillProperties`, so Dart's `InlineSpan.debugFillProperties` line `style?.debugFillProperties(properties)` has no counterpart and a span dump shows no style properties. | `src/Plumix/Painting/TextSpan.cs`, `Plumix/UI/Text.cs` (`TextStyle`) vs `painting/text_style.dart` | Port `TextStyle.debugFillProperties`/`getDiagnosticsProperties`, then call it from `InlineSpan.DebugFillProperties`. |
| `RenderParagraph` dumps no `devicePixelRatio` and `RenderEditable` no `textScaler`/`locale`/`offset`: the properties Dart's `debugFillProperties` reads do not exist on the C# render objects. | `src/Plumix/RenderParagraph.cs`, `src/Plumix/Rendering/Editable.cs` vs `rendering/paragraph.dart`, `rendering/editable.dart` | Add the missing properties with the upstream 3.47 delta (`rendering/paragraph.dart` row below), then extend the two `DebugFillProperties` overrides. |
| `new BoxConstraints()` bypasses the primary-constructor defaults, yielding a tight 0x0 constraint where Dart's `const BoxConstraints()` is unbounded | `src/Plumix/Rendering/Box.cs` (`rendering/box.dart`); four call sites: `Plumix.Material/Badge.cs:286`, `Chips.cs:920`, `MergeableMaterial.cs:697`, `Plumix/Rendering/ListBody.cs:174` | Check each site against its Dart source, fix, and add a `BoxConstraints.Unbounded` static (or an explicit parameterless constructor) so the trap cannot recur. |
| `ButtonThemeData` has no `colorScheme` field, so `ThemeData`'s `buttonTheme` default cannot pass one as Dart does | `src/Plumix.Material/ButtonTheme.cs` (`material_ui/lib/src/button_theme.dart`); consumed by `ThemeData` | Port `ButtonThemeData.colorScheme` (and the `getTextColor`/`getFillColor` paths that read it), then pass `colorScheme` from `ThemeData`'s `buttonTheme` default. |
| `SelectionListener` / `SelectionListenerNotifier` / `SelectionDetails` (the observer surface of `widgets/selectable_region.dart`) are not ported. | `src/Plumix/Widgets/Selection.cs`, `SelectableRegion.cs` vs `flutter-src/.../widgets/selectable_region.dart` | Port the three types 1:1 on top of the existing `SelectionContainer` protocol; tests from `selectable_region_test.dart` ("SelectionListener"). |
| Host back button outside `Router` mode goes through a C#-only handler stack (`NavigatorBackButtonDispatcher`, registered by `NavigatorState`), and `WidgetsApp.DidPopRoute` returns `false`; Flutter's `WidgetsApp.didPopRoute` calls `navigator.maybePop()` and nested navigators win through `PopScope`/`NavigationNotification`. | `src/Plumix/Widgets/Navigation.cs`, `Navigation.NavigatorState.cs`, `src/Plumix/Widgets/App.cs` (`DidPopRoute`) vs `widgets/app.dart`, `widgets/navigator.dart` | Breaking: drop the dispatcher stack, make `DidPopRoute` call `NavigatorState.MaybePop`, keep nested-navigator precedence via `NavigationNotification`; add a `docs/ai/DIVERGENCES.md` row only for what cannot close. |
| `Container` lacks `clipBehavior` and `transformAlignment` (and `alignment` is `Alignment?` not `AlignmentGeometry?`); `FittedBox` lacks `clipBehavior`. | `src/Plumix/Widgets/Basic.cs` (`Container`, `FittedBox`) vs `widgets/container.dart`, `widgets/basic.dart` | Add the parameters with Dart defaults (`Clip.none`) and composition (`ClipPath` with `ShapeBorderClipper` for `Container`); give `Container` its own `container.dart` marker. |
| Two debug-only pieces of the `Hero` port are missing: Dart's `NavigatorState._updateHeroController` reports a `FlutterError` from a post-frame callback when one `HeroController` ends up owned by two navigators, and `Hero`/`HeroMode` implement `debugFillProperties` (`tag`, `mode`). | `src/Plumix/Widgets/Navigation.NavigatorState.cs` (`UpdateHeroController`), `src/Plumix/Widgets/Hero.cs` vs `flutter/.../widgets/navigator.dart`, `heroes.dart` | Port the ownership check over `FlutterError.ReportError` (now available); add the two `DebugFillProperties` overrides. Neither affects runtime behavior. |

## Host-level gaps (platform adapters, `src/Plumix/FlutterHost.cs` and per-host projects)

| Item | Where | Next step |
| --- | --- | --- |
| `MediaQueryData.GestureSettings` exists (recognizers read it through `MediaQuery.MaybeGestureSettingsOf`) but no host ever sets it, so every recognizer falls back to the hard-coded `GestureConstants.TouchSlop`. Dart's `MediaQuery.fromView` fills it from `view.gestureSettings` (Android's `ViewConfiguration` touch slop). | `src/Plumix/FlutterHost.cs`, `src/Plumix/Widgets/MediaQuery.cs` vs `widgets/media_query.dart`, `dart:ui` `FlutterView.gestureSettings` | Expose a per-view touch slop from the host (Android `ViewConfiguration.ScaledTouchSlop`, default elsewhere) and pass it into the `MediaQueryData` the host builds. |
| No native accessibility bridge on any host. `FlutterHost` exposes `SemanticsRoot`, `SemanticsUpdated` and `PerformSemanticsAction(nodeId, action)`, but nothing consumes them: no Avalonia automation peers (desktop), no ARIA overlay tree (browser), no `AccessibilityNodeProvider` (Android), no accessibility elements (iOS). | `src/Plumix/FlutterHost.cs`, `src/Sample/Plumix.{Desktop,Browser,Android,iOS}` | Per host: consume `PlumixHost.SemanticsUpdateProduced` (already one batch of changed nodes per flushed frame), map `Id`/`Rect`/`Label`/`Flags`/`Actions`/`IsHidden`/children to the platform tree, and route every platform action back through `SemanticsOwner.PerformAction` (never call framework callbacks directly); keep focused node aligned with `FocusManager.PrimaryFocus`. Start with desktop (Avalonia `AutomationPeer`). |
| No host feeds `WidgetsBinding.HandleAccessibilityFeaturesChanged`: the framework carries `AccessibilityFeatures` (`ReduceMotion`, `DisableAnimations`) and `CupertinoMenuAnchor` consumes it, but the value stays at its default on every platform. | `src/Sample/Plumix.{Desktop,Browser,Android,iOS}`, `src/Plumix/FlutterHost.cs` | Read the platform reduce-motion / disable-animations settings per host (macOS `NSWorkspace`/Windows animation setting via Avalonia, browser `prefers-reduced-motion`, Android/iOS accessibility managers) and call `HandleAccessibilityFeaturesChanged` on change. |
| `SystemChrome.SetSystemUIOverlayStyle` per-bar colours are applied only on Android, through reflection over Avalonia internals (`TrySetInsetsManagerSystemBarTheme`, private `_activity`/`Window`); iOS and desktop get no per-bar styling. | `src/Plumix/FlutterHost.cs` (system-bar section) | Replace the reflection path with a public Avalonia insets API when one exists; add the iOS status-bar style path. Low priority. |

## Upstream re-port backlog — Flutter 3.44.0 → 3.47.0

From the pin-move audit of 2026-08-13 (97 upstream files changed, 66 intersect ported files, 40
behaviour-bearing deltas). Existing C# ports were validated against 3.44.0 and must absorb these when
the control is next touched, or as a dedicated pass. Status spot-checked 2026-08-16 by grepping the
named API: **open** = the API/behaviour is absent in C#; **verify** = the API exists or the file was
re-ported after the pin move (selection stack 08-14, semantics compiler 08-15, tap region 08-14) — read
the Dart diff and confirm before removing the row. Doc-only upstream changes are not listed.

| Dart file | Delta | Status |
| --- | --- | --- |
| `animation/animation_style.dart` | New `AnimationStyle.merge()`; `lerp` interpolates Durations (µs, null→0) and Curves (`_LerpedCurve`, null→linear) instead of a t<0.5 snap. | open |
| `painting/borders.dart` | `ShapeBorder.lerp`/`OutlinedBorder.lerp` gain reversed-timeline fallback `b?.lerpTo(a,1-t) ?? a?.lerpFrom(b,1-t)` before the snap. | open |
| `painting/gradient.dart` | Virtual `Gradient.fromColor(Color)` with covariant overrides on Linear/Radial/Sweep. | verify (`FromColor` exists) |
| `painting/shape_decoration.dart` | `lerp` colour↔gradient converts via `Gradient.fromColor` and lerps as gradients; result nulls `color`. | verify |
| `painting/image_stream.dart` | `ImageInfo.isCloneOf` scale fix; `ImageStreamListener.reportErrors` (default true); completer suppresses error reporting once a non-reporting listener attached. `widgets/image.dart` passes `reportErrors: errorBuilder == null`. | open |
| `painting/text_painter.dart` | Strut line-height path: empty `getBoxesForRange` falls back to `preferredLineHeight` instead of throwing. | verify |
| `gestures/recognizer.dart` | Recognizer records first event's `buttons` per pointer; `getButtonsForPointer(int)`. | verify (`GetButtonsForPointer` exists) |
| `gestures/binding.dart`, `gestures/hit_test.dart` | Engine hit-test query hook (`platformDispatcher.onHitTest`, `NativeHitTestTarget`) — only if the host exposes a native hit-test query. | open (host-gated) |
| `rendering/box.dart` | `globalToLocal` early-out `Offset.zero` when local view direction z==0 (was NaN). | verify |
| `rendering/object.dart` | `debugNeeds*` false in release; system-fonts relayout tolerates mid-frame notification; merging boundary with siblings builds inner node + synthetic boundary. | verify (semantics compiler re-ported 08-15; `AccessibilityFocusBlockType` shipped 08-28) |
| `rendering/paragraph.dart` | `devicePixelRatio` property; selection highlight under text, handles above; semantics configs merged in encounter order with first-tag placeholder ownership; selection endpoints honour affinity; empty-rect drag paths call `_setSelectionPosition`. `widgets/basic.dart` `RichText` passes DPR. | verify (selection re-ported 08-14; DPR property absent) |
| `rendering/table.dart` | `findRowIndex` bound fix (`_rows - 1`). | verify (`FindRowIndex` exists) |
| `widgets/basic.dart`, `widgets/indexed_stack.dart` | `IndexedStack` children wrapped in `_VisibilityScope` + `ExcludeFocus` (no intermediate render objects; non-selected children focus-excluded). | verify (`VisibilityScope` exists) |
| `widgets/actions.dart` | `Actions.find/maybeFind<Intent>` without intent legal; wrong-type → error/null; `Action.overridable` resolves by runtime intent type. | open (`Overridable` absent) |
| `widgets/animated_cross_fade.dart` | New `clipBehavior` (default `Clip.hardEdge`). | open |
| `widgets/animated_scroll_view.dart` | Separated-list `removeItem` last-item check uses `_itemsCount - _outgoingItemsCount`. | verify |
| `widgets/autocomplete.dart` | Options overlay wrapped in `ExcludeFocus`. | verify |
| `widgets/context_menu_controller.dart` | `show` while shown updates the entry in place (`markNeedsBuild`); `InheritedTheme.capture` inside the builder. | verify |
| `widgets/image_icon.dart` | New `useOriginalColors`. | open |
| `widgets/navigator.dart` | `NavigationNotification.canHandlePop` computed at dispatch; bubbling rewrite respects root `PopScope(canPop:false)`; debug assert pop result type matches `T`. | verify (`CanHandlePop` exists) |
| `widgets/overlay.dart` | `OverlayPortal` deferred child add/remove moved to layout-surrogate attach/detach (GlobalKey reparenting fix). | verify |
| `widgets/ticker_provider.dart` | `TickerMode.getValuesNotifier` returns fallback when `!context.mounted`. | verify (`GetValuesNotifier` exists) |
| `widgets/view.dart`, `widgets/focus_traversal.dart` | `FocusTraversalGroup.parentNode`; each view's `FocusScope` attaches to `FocusManager.rootScope`. | open (`ParentNode` absent) |
| `widgets/focus_manager.dart` | Suspended node restored on resume only if nothing else requested focus. | verify |
| `widgets/scrollable.dart` | Semantics `hasImplicitScrolling = allowImplicitScrolling` unconditionally. | verify (`HasImplicitScrolling` exists) |
| `widgets/scrollable_helpers.dart` | `EdgeDraggingAutoScroller` stops when `resolvedPhysics.shouldAcceptUserOffset` is false. | open |
| `widgets/overscroll_indicator.dart` | Stretch indicator resets acceptance on ScrollStart; `scrollEnd` only when accepted. | verify |
| `widgets/sliver_resizing_header.dart` | Child wrapped in `Semantics(container, explicitChildNodes)`; partially collapsed header tags children `excludeFromScrolling`. | verify |
| `widgets/editable_text.dart` | Spell check disabled for password inputs, re-inferred in `didUpdateWidget`; `contextMenuBuilder` identity change no longer recreates the overlay (only null↔non-null); toolbar-on-screen uses `scheduleFrameCallback` when idle. | open |
| `widgets/selectable_region.dart`, `widgets/text.dart` | Escape hides toolbar; handles before toolbar; select-word/paragraph outside selectables clamps to nearest; multi-selectable edge-init sweep rewritten. | verify (re-ported 08-14) |
| `widgets/text_selection.dart` | `showToolbar` asserts not in persistentCallbacks; handle dy null on degenerate metrics; toolbar inserted above the end handle. | verify (re-ported 08-14) |
