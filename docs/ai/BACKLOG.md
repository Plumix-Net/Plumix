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
| `GlobalObjectKey<T>.ToString()` recurses without bound when the key's value is a `Diagnosticable` that renders the widget tree: `PrintMembers` → `Diagnosticable.ToString` → `ToStringDeep` → `Widget.ToStringShort` → the key again. Formatting a failed assertion over widgets that carry one (any `FocusableActionDetector`) overflows the stack and kills the test host. | `src/Plumix/Widgets/Framework.cs` (`GlobalObjectKey<T>`) vs `flutter/.../widgets/framework.dart` (`GlobalObjectKey.toString`) | Port Dart's `describeIdentity`-based `toString` (type + short hash of the value, never the value's own `toString`); add a regression test that stringifies a widget holding a `GlobalObjectKey` over a `Diagnosticable`. |
| `ToggleablePainter` is missing Dart's `activeColor`, `inactiveColor`, `inactiveReactionColor`, `downPosition`, `isFocused`, `isHovered` and `isActive` properties, so every subclass (`CupertinoRadioPainter`, `CupertinoCheckboxPainter`, `CupertinoSwitchPainter`, Material `RadioPainter`, `CheckboxPainter`) re-declares them as private fields. | `src/Plumix/Widgets/Toggleable.cs` vs `flutter-src/.../widgets/toggleable.dart` (`ToggleablePainter`) | Add the seven properties to the base class with Dart's names and change-notifying setters, then delete the duplicated fields from the five painters in one mechanical pass. No behavior change. |
| `Slider.semanticFormatterCallback` is mapped onto the semantics *label*; Dart sets `config.value`/`increasedValue`/`decreasedValue` and `onIncrease`/`onDecrease` actions. | `src/Plumix.Material/Slider.cs` (semantics section) vs `material-ui-src/lib/src/slider.dart` `_RenderSlider.describeSemanticsConfiguration` | Port the semantics configuration 1:1 (`Widgets/Semantics.cs` already exposes `Value`/`IncreasedValue`/`OnIncrease`), add the increase/decrease tests from `slider_test.dart`. |
| `RangeSlider` uses one `FocusNode` (plus a non-Dart public `focusNode` parameter) and one `Semantics(label:)`; Dart has `startFocusNode`/`endFocusNode` and per-thumb start/end semantics nodes with increase/decrease actions. | `src/Plumix.Material/RangeSlider.cs` vs `material-ui-src/lib/src/range_slider.dart` (`_RenderRangeSlider` semantics, `_RangeSliderState` focus nodes) | Breaking: replace `focusNode` with Dart's dual-thumb focus model, port `describeSemanticsConfiguration`/`assembleSemanticsNode`, add the `range_slider_test.dart` semantics assertions. |

| `Scaffold` keeps its `(reference)` marker: no `restorationId`/`RestorationMixin` on the drawer flags, one `_persistentBottomSheet` instead of Dart's `_dismissedBottomSheets` stack (so a sheet cannot be replaced while the previous one is still animating out), `didUpdateWidget`'s three-way `bottomSheet` branch is a `SyncStaticBottomSheetAnimation` call, and the root is `Container(color:)` rather than `Material(color:)`. | `src/Plumix.Material/Scaffold.cs` vs `material_ui/lib/src/scaffold.dart` | Port `_buildBottomSheet`/`_maybeBuildPersistentBottomSheet`/`_closeCurrentBottomSheet` with the dismissed-sheet stack and the `bottomSheet` `didUpdateWidget` asserts, adopt `RestorableBool` for both drawer flags, swap the root for `Material`, then drop the marker qualifier; tests from `scaffold_test.dart` ("can rebuild and remove bottomSheet at the same time", "Scaffold background color defaults to ColorScheme.surface") and `persistent_bottom_sheet_test.dart`. |
| `SelectionListener` / `SelectionListenerNotifier` / `SelectionDetails` (the observer surface of `widgets/selectable_region.dart`) are not ported. | `src/Plumix/Widgets/Selection.cs`, `SelectableRegion.cs` vs `flutter-src/.../widgets/selectable_region.dart` | Port the three types 1:1 on top of the existing `SelectionContainer` protocol; tests from `selectable_region_test.dart` ("SelectionListener"). |
| Host back button outside `Router` mode goes through a C#-only handler stack (`NavigatorBackButtonDispatcher`, registered by `NavigatorState`), and `WidgetsApp.DidPopRoute` returns `false`; Flutter's `WidgetsApp.didPopRoute` calls `navigator.maybePop()` and nested navigators win through `PopScope`/`NavigationNotification`. | `src/Plumix/Widgets/Navigation.cs`, `Navigation.NavigatorState.cs`, `src/Plumix/Widgets/App.cs` (`DidPopRoute`) vs `widgets/app.dart`, `widgets/navigator.dart` | Breaking: drop the dispatcher stack, make `DidPopRoute` call `NavigatorState.MaybePop`, keep nested-navigator precedence via `NavigationNotification`; add a `docs/ai/DIVERGENCES.md` row only for what cannot close. |
| `ScrollableState` gates dragging imperatively (`GlobalObjectKey` + `RawGestureDetectorState.SetDragEnabled`) instead of Flutter's `setCanDrag` → `replaceGestureRecognizers` over the recognizer-factory map (the map exists). | `src/Plumix/Widgets/Scroll.cs`, `Gestures.cs` vs `widgets/scrollable.dart`, `widgets/gesture_detector.dart` | Port `RawGestureDetectorState.replaceGestureRecognizers`/`replaceSemanticsActions`, then rewrite `SetCanDrag` on it and delete `SetDragEnabled`. Structural, small. |
| `Container` lacks `clipBehavior` and `transformAlignment` (and `alignment` is `Alignment?` not `AlignmentGeometry?`); `FittedBox` lacks `clipBehavior`. | `src/Plumix/Widgets/Basic.cs` (`Container`, `FittedBox`) vs `widgets/container.dart`, `widgets/basic.dart` | Add the parameters with Dart defaults (`Clip.none`) and composition (`ClipPath` with `ShapeBorderClipper` for `Container`); give `Container` its own `container.dart` marker. |

## Host-level gaps (platform adapters, `src/Plumix/FlutterHost.cs` and per-host projects)

| Item | Where | Next step |
| --- | --- | --- |
| `MediaQueryData.GestureSettings` exists (recognizers read it through `MediaQuery.MaybeGestureSettingsOf`) but no host ever sets it, so every recognizer falls back to the hard-coded `GestureConstants.TouchSlop`. Dart's `MediaQuery.fromView` fills it from `view.gestureSettings` (Android's `ViewConfiguration` touch slop). | `src/Plumix/FlutterHost.cs`, `src/Plumix/Widgets/MediaQuery.cs` vs `widgets/media_query.dart`, `dart:ui` `FlutterView.gestureSettings` | Expose a per-view touch slop from the host (Android `ViewConfiguration.ScaledTouchSlop`, default elsewhere) and pass it into the `MediaQueryData` the host builds. |
| No native accessibility bridge on any host. `FlutterHost` exposes `SemanticsRoot`, `SemanticsUpdated` and `PerformSemanticsAction(nodeId, action)`, but nothing consumes them: no Avalonia automation peers (desktop), no ARIA overlay tree (browser), no `AccessibilityNodeProvider` (Android), no accessibility elements (iOS). | `src/Plumix/FlutterHost.cs`, `src/Sample/Plumix.{Desktop,Browser,Android,iOS}` | Per host: read the tree only after `PipelineOwner.FlushSemantics` (one publish per flushed frame, diff by node id), map `Id`/`Rect`/`Label`/`Flags`/`Actions`/`IsHidden`/children to the platform tree, and route every platform action back through `SemanticsOwner.PerformAction` (never call framework callbacks directly); keep focused node aligned with `FocusManager.PrimaryFocus`. Start with desktop (Avalonia `AutomationPeer`). |
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
| `rendering/binding.dart` | `getRectOfSemanticsNodeInViewCoordinates(viewId, nodeId)`. | open |
| `rendering/box.dart` | `globalToLocal` early-out `Offset.zero` when local view direction z==0 (was NaN). | verify |
| `rendering/object.dart` | `debugNeeds*` false in release; system-fonts relayout tolerates mid-frame notification; `_SemanticsParentData` compares `accessibilityFocusBlockType`, blocked nodes `isFocused = null`; merging boundary with siblings builds inner node + synthetic boundary. | verify (semantics compiler re-ported 08-15; `AccessibilityFocusBlockType` absent) |
| `rendering/paragraph.dart` | `devicePixelRatio` property; selection highlight under text, handles above; semantics configs merged in encounter order with first-tag placeholder ownership; selection endpoints honour affinity; empty-rect drag paths call `_setSelectionPosition`. `widgets/basic.dart` `RichText` passes DPR. | verify (selection re-ported 08-14; DPR property absent) |
| `rendering/table.dart` | `findRowIndex` bound fix (`_rows - 1`). | verify (`FindRowIndex` exists) |
| `semantics/semantics.dart` | Custom-action dispatch on merged nodes finds owning descendant by id; custom-action map changes dirty the node; `SemanticsOwner.getSemanticsNode(id)`; traversal grafting via dirty pipeline, orphaned grafted children filtered from hit-test order. | open (`GetSemanticsNode` absent) |
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
| `widgets/tap_region.dart` | `RenderTapRegionSurface` handles semantics tap/longPress (synthetic pointer down at node centre); shared `_classifyRegions`. | verify (re-ported 08-14) |
