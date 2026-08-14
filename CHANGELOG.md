# Changelog

- Breaking: closed the live-`ScaffoldGeometry`/FAB-motion divergence (`material_ui/scaffold.dart`,
  `floating_action_button_location.dart`). `Scaffold` now lays its slots out through the ported
  `_ScaffoldLayout` (`CustomMultiChildLayout` + `ScaffoldSlot`) instead of a Column/Stack, so
  `ScaffoldPrelayoutGeometry` carries measured snack-bar, bottom-sheet and material-banner sizes. New public
  `ScaffoldGeometry` + `Scaffold.GeometryOf` (paint-phase only, backed by the ported
  `_ScaffoldGeometryNotifier`), and the ported `_FloatingActionButtonTransition` drives entrance/exit/move
  scale, rotation and cross-fade; a `floatingActionButtonLocation` change now animates over the 400 ms segue
  and restarts from `GetAnimationRestart` when interrupted, relayouting through the delegate's `relayout`
  listenable without rebuilding. `BottomAppBar` tracks the moving FAB through that listenable. New `Scaffold`
  options: `ExtendBody`, `ExtendBodyBehindAppBar`, `ResizeToAvoidBottomInset`. Core gained
  `CompoundAnimation`/`AnimationMin`/`AnimationMax`/`AnimationMean`, `TrainHoppingAnimation`,
  `Animatable.Chain`, `Animation<double>.Drive` and `PipelineOwner.DebugDoingPaint`. **Breaking:**
  `FloatingActionButtonAnimator.GetScale`/`GetRotation` became `GetScaleAnimation`/`GetRotationAnimation`
  (`Animation<double>`); `FloatingActionButtonLocation.MiniButtonOffsetAdjustment` is Flutter's `4.0`, not
  `8.0`; a fixed `SnackBar` and a zero-elevation `MaterialBanner` now follow Flutter's overlay/`contentTop`
  placement instead of being Column children; and `ScaffoldState.FloatingActionButtonSize` is gone (the
  layout measures the button, so the estimate had no callers). Remaining deltas are tracked in
  `DIVERGENCES.md`.

- Breaking: closed the `Navigator` divergence end-to-end (`widgets/navigator.dart`). The navigator now runs
  Flutter's staged route lifecycle: `_RouteLifecycle`/`_RouteEntry` (as internal `RouteLifecycle`/`RouteEntry`),
  `_flushHistoryUpdates` with its observer queues (`didPush`/`didReplace` drain LIFO before `didPop`/`didRemove`
  drain FIFO), `_flushRouteAnnouncement`, deferred subtree-aware disposal, and the new `NavigatorObserver`
  `DidChangeTop`. Declarative routing landed: `Page`, `Navigator.Pages`/`OnDidRemovePage`, the full
  `_updatePages` page-diff, `RouteTransitionRecord`, `TransitionDelegate`/`DefaultTransitionDelegate`. Restoration
  landed: `Route.RestorationScopeId`, `_HistoryProperty`, named/anonymous `_RestorationInformation`, and the
  `RestorablePush*`/`RestorableReplace*` family; `NavigatorState` is now a `RestorationState` and its build
  composes `FocusTraversalGroup > Focus > UnmanagedRestorationScope > Overlay`. `_ModalScopeStatus` became an
  `InheritedModel` with the seven `ModalRouteAspect`s, adding `ModalRoute.CanPopOf`/`SettingsOf`/`IsActiveOf`/
  `IsFirstOf`/`PopDispositionOf`; `_ModalScopeState.build` now wraps the scope in a `RestorationScope` and uses
  `FocusScope.WithExternalFocusNode`. Core gained `FocusScopeNode.SetFirstFocus`, and `Focus(autofocus: true)`
  now defers to a scope that already has a focused child. **Breaking:** `Route.DidPop(Route? previousRoute)` is
  now `bool DidPop(object? result)`; `ModalRoute.RequestFocus` moved to `Route` as a non-nullable getter fed by a
  `requestFocus` constructor argument; `RouteSettings` is no longer `sealed`; initial routes are *added* rather
  than pushed, so their transition starts completed; and `didRemove`'s `previousRoute` now skips routes that are
  themselves leaving. New `Navigator` options: `TransitionDelegate`, `RequestFocus`, `ClipBehavior`,
  `RestorationScopeId`, `RouteTraversalEdgeBehavior`, `RouteDirectionalTraversalEdgeBehavior`,
  `ReportsRouteUpdateToEngine`. Remaining deltas are tracked in `DIVERGENCES.md`.

- Breaking: ported the state-restoration subsystem end-to-end (`services/restoration.dart`,
  `widgets/restoration.dart`, `widgets/restoration_properties.dart`). New `RestorationManager`/
  `RestorationBucket` in `Plumix.UI` (claim/adopt/rename/drop, duplicate-id detection, post-frame
  serialization, `isReplacing`, `flushData`), real `RestorationScope`/`UnmanagedRestorationScope`/
  `RootRestorationScope`, `RestorableProperty<T>`, the `RestorationMixin` equivalent
  `RestorationState : State`, and the full `Restorable*` property family (num/double/int/string/bool
  and their nullable forms, `DateTime`, `Enum`, `Listenable`, `ChangeNotifier`,
  `TextEditingController`). `FormField.restorationId` now persists `error_text` and
  `has_interacted_by_user`. Core gained `Scheduler.ScheduleMicrotask` and an
  `AddPostFrameCallback(..., scheduleFrame: false)` overload. **Breaking:** the placeholder
  `RootRestorationScope` is gone — `MaybeRestorationIdOf` was removed, the constructor takes
  `(restorationId, child)`, and `FormFieldState` now derives from `RestorationState`. Host transport,
  codec and first-frame-deferral deltas are tracked in `DIVERGENCES.md`.

- Breaking: closed the `Dialog` family divergence end-to-end. Material `Dialog`/`AlertDialog`/`SimpleDialog` now
  render on a real `Material(type: card)` surface with `AnimatedPadding`, `EdgeInsetsGeometry` slot paddings,
  `AlignmentGeometry` alignment, `Curves.Decelerate` inset animation, host-platform (`defaultTargetPlatform`) route
  labels, and icon-driven `TextAlign.Center` titles; `AlertDialog.Adaptive`/`ShowAdaptiveDialog` route to the new
  Cupertino dialog on iOS/macOS. `DialogRoute<T>` is rebuilt on the new core `RawDialogRoute<T>` (`PopupRoute` +
  `DisplayFeatureSubScreen` + scopesRoute semantics + `ShowGeneralDialog`): captured inherited themes, safe area,
  opaque-surface semantics, the source 150ms easeOut fade (`AnimationStyle`-overridable), and the shared barrier
  pipeline — its future now completes on pop, not after the exit fade. Ported Cupertino `dialog.dart` at the 3.47
  shape: `CupertinoAlertDialog` (`_PriorityColumn`/`_AlertDialogActionsLayout` `RenderFlex` subclasses, overscroll
  backgrounds, 270/310 widths, exact styles/colors), `CupertinoDialogAction`, blur+saturation `CupertinoPopupSurface`,
  sliding-tap press/slide/confirm targets, and `CupertinoDialogRoute`/`ShowCupertinoDialog` with the critically-damped
  spring (scale 1.3 fade-in, fade-only exit) via the new `TransitionRoute.CreateSimulation` hook. Core gained
  `TraversalEdgeBehavior` (Tab wraps in a closed loop per scope by default — **Breaking** for edge-stop assumptions),
  route `RequestFocus`/traversal-edge wiring in `ModalScope`, directional-edge handling, `RenderStack` intrinsics,
  reversed `AnimationController.AnimateWith`, and Cupertino gained elevation-aware `CupertinoDynamicColor`,
  `CupertinoUserInterfaceLevel`, `SystemRed`/`Separator`/`Label` colors. **Breaking:** `DialogThemeData.Alignment` is
  `AlignmentGeometry?`, `ActionsPadding` is `EdgeInsetsGeometry?`, `DialogTheme` is an `InheritedTheme`, and
  `MaterialDialogs.ShowDialog` replaced `transitionDuration` with `animationStyle` and gained
  `anchorPoint`/`traversalEdgeBehavior`/`requestFocus`. Remaining deltas (slide-vs-scroll arena, superellipse clip,
  high-contrast colors, legacy `DialogTheme` shims) are tracked in `DIVERGENCES.md`.

- Breaking: completed the strict `SearchAnchor`/`SearchBar` closeout. The search view now uses the source
  `PopupRoute` with the 600ms `easeInOutCubicEmphasized` grow/fade choreography from the anchor rect (navigator-
  relative geometry, LTR/RTL clamping, fullscreen top-padding lerp, interval-staggered icon/divider/list fades),
  `CapturedThemes` for local inherited themes, docked-close-on-resize, and the exact `_SearchBarDefaultsM3`/
  `_SearchViewDefaultsM3` tables on `Material` surfaces. `SuggestionsBuilder` is now async
  (`ValueTask<IReadOnlyList<Widget>>`, Dart `FutureOr`) with source dedupe/coalescing; `SearchViewTheme` is an
  `InheritedTheme` and both theme records use source types (`OutlinedBorder` shapes, `EdgeInsetsGeometry`
  paddings, the upstream `headerHintStyle` lerp quirk). Core gained `TextCapitalization`/`SmartDashesType`/
  `SmartQuotesType` in `TextInputConfiguration`/`EditableText` (moved from `Plumix.Material`), plus
  `scrollPadding`; `TextField` gained `textCapitalization`, `smartDashesType`/`smartQuotesType`, `onTapOutside`,
  `onTapAlwaysCalled`, and `scrollPadding`. **Breaking:** `SearchViewBuilder` was renamed `ViewBuilder`,
  `SearchController.CloseView` takes a required argument and `IsOpen`/`OpenView`/`CloseView` throw when detached,
  non-source constructor validation was removed, and `SearchAnchor.Bar` forwards `scrollPadding`/
  `contextMenuBuilder`.

- Moved the pinned Flutter parity revision from 3.44.0 to 3.47.0 (`4cf24164269`) and switched the
  Material/Cupertino source of truth to the extracted `material_ui`/`cupertino_ui` pub packages
  (pinned 1.0.0, code-identical to the SDK's frozen copies at these pins). `dart_sample` now imports
  `package:material_ui`/`package:cupertino_ui` (via `dart fix --code=migrate_design_widgets`); all
  material/cupertino parity markers were rewritten to `material_ui/lib/src/...` /
  `cupertino_ui/lib/src/...` and `generate_port_map.py` resolves them against the new
  `material-ui-src`/`cupertino-ui-src` symlinks. Six stale markers were fixed (`visibility.dart` →
  `indexed_stack.dart` rename plus five pre-existing wrong paths) and `PORT_MAP.md` regenerated
  clean. All 66 ported files that changed upstream were audited; the 40 behavior-bearing deltas are
  recorded as the re-port backlog in `docs/ai/notes/migration-2026-08-13-flutter-3.47-pin.md`.
  Two pre-existing `dart_sample` analyzer errors surfaced by the SDK update were fixed
  (`WidgetStateProperty.resolveWith` static-call form; `SearchDelegate<String?>` nullable result).

- Breaking: completed strict Material `MergeableMaterial` parity. The constructor now follows Flutter's field
  order and accepts arbitrary source-shaped gap/elevation values; keyed gap/chunk reconciliation, 200ms extent,
  corner and divider transitions, transparent slice materials, directional list-body layout, and one render-owned
  card shadow per connected slice group now match the pinned implementation. Focused tests and the mirrored Card
  demo cover live merge/separate choreography.

- Completed Cupertino text-selection toolbar parity: mobile/desktop surfaces, buttons, adaptive routing, overflow,
  spell-check suggestions, Cupertino theme/color primitives, and mirrored gallery probes are now available. Material
  adaptive toolbars select Cupertino controls on iOS/macOS, and the Android host registers the native default
  sentence spell checker through `DefaultSpellCheckService`. Rounded-superellipse/path-shadow/retained-clip backend
  limits remain documented in `DIVERGENCES.md`.

- Breaking: completed strict Material `ReorderableListView` parity. The public wrapper now preserves nullable
  padding and auto-scroll defaults, forwards anchor/drag/keyboard/restoration/clip contracts, resolves desktop
  cursors from dragged state, animates the default proxy elevation, and follows horizontal RTL axis direction.
  Shared scrolling now supports anchored viewport geometry and restoration-ID-keyed page-storage offsets; process
  restoration still awaits the framework restoration manager tracked in `DIVERGENCES.md`.

- Breaking: closed deferred-loading scroll parity. The widget root now exposes the raw platform view through
  `View.Of`/`View.MaybeOf`; the default physics threshold ignores nested `MediaQuery` overrides, and `JumpTo` plus
  pointer scrolling contribute their forced displacement as implied velocity until the next frame. The direct
  widget property is named `ViewHandle` because C# forbids a member named `View` on the `View` class.

- Breaking: completed the Material `BottomSheet`/`BottomSheetThemeData` direct-token and theme closeout. M3 sheets
  now read `surfaceContainerLow`/`onSurfaceVariant` from `ColorScheme` directly; drag-handle colors use the
  source-shaped `WidgetStateColor` contract, and bottom-sheet themes now provide exact copy/lerp, diagnostics, and
  inherited-theme capture. The mirrored demo exercises captured theme overrides and hover-state handle colors.

- Breaking: completed the Material date/range-picker direct-token and theme closeout. `DatePickerThemeData` now
  uses Flutter's `OutlinedBorder` state-shape contract, locale/copy/lerp surface, inherited-theme capture, and exact
  M2/M3 `ColorScheme`/`TextTheme` defaults; picker dialogs honor source theme precedence and range overlays, and
  `showDatePicker`/`showDateRangePicker` apply explicit or themed locale overrides.

- Breaking: completed strict Material `DataTable`/`DataTableThemeData` parity. Tables now resolve local and global
  theme fields in Flutter order, use direct `ColorScheme` row roles and source divider defaults, accept arbitrary
  decorations, compose through clipped transparent `Material`, merge ambient text styles, expose column-header
  semantics, and animate sort arrows over 150ms. Focused tests and mirrored demos cover M2/M3 roles, row states,
  theme fallback, layout, clipping, semantics, and sort transitions.

- Breaking: completed strict Material `SegmentedButton<T>`/`SegmentedButtonThemeData` parity. Expanded insets now
  use `EdgeInsets`, `styleFrom` accepts any `OutlinedBorder`, segment state controllers survive updates, and the
  source `Material`/`TextButtonTheme`/`TextButton` composition carries selected and enabled semantics. A dedicated
  render object now equalizes intrinsic sizes, honors 48px tap targets, mirrors RTL placement, clips segment shapes,
  and paints source dividers and mixed-state borders; focused tests cover defaults, style/theme precedence,
  lifecycle, selection content, semantics, layout and paint.

- Breaking: completed strict Material `ButtonBar`/`ButtonBarThemeData` parity. Button padding now remains
  directional through the legacy `ButtonThemeData`/`MaterialButton`/`RawMaterialButton` path; the bar uses
  Flutter's `RenderFlex`-based unconstrained probe, constrained row retry, vertical overflow and dry layout; and its
  theme now has source copy/lerp/diagnostics and validation. Focused tests and mirrored C#/Dart probes cover theme
  precedence, logical padding, LTR/RTL overflow alignment, spacing, direction, and constrained/padded sizing.

- Breaking: completed strict Material `Tooltip`/`TooltipThemeData` parity. Tooltip padding and margin now use
  directional `EdgeInsetsGeometry`, decoration accepts any `Decoration`, rich-message overlays remain interactive by
  default, cursor/text-direction/style composition matches Flutter, local tooltip themes participate in inherited
  theme capture, and theme copy/lerp follows the pinned source fields exactly. Core `Container` now resolves
  directional padding/margin, tooltip presentation emits `TooltipSemanticEvent`, and the framework has reusable
  diagnostic-property nodes for source-shaped `DebugFillProperties` output. Focused tests and the mirrored demo cover
  plain/rich pointer policy, arbitrary shape decoration, directional insets, semantic events, diagnostics, and theme
  copy/lerp behavior. Advanced `Plumix` and `Plumix.Material` to `0.2.0-alpha.1`.

- Breaking: closed `MenuAcceleratorLabel` parity by replacing its global deepest/latest Alt dispatcher with
  per-label `CharacterActivator` entries in the nearest `ShortcutRegistry`. Accelerator callbacks now participate
  in normal focus-local `Shortcuts` precedence, entries follow Alt/dependency/submenu/disposal lifecycle, and labels
  without a registrar remain display-only. The default builder now matches Flutter's direct `RichText`/ambient-style
  span composition. Focused coverage mirrors Flutter's marker table, submenu replacement, Apple policy, and zero-area
  layout; the paired dropdown demos include a focus-local Alt+N override probe.

- Breaking: closed the nonlinear text-scaling divergence. `MediaQueryData` now owns the exact `TextScaler` strategy,
  adds scaler-aware `CopyWith`, and keeps `TextScaleFactor` as a derived compatibility surface; `MediaQuery` adds
  scaler accessors and aspect-scoped dependencies, while its no-scaling/clamped wrappers preserve strategy behavior.
  `Text`/`RichText` retain their legacy scale-factor inputs with Flutter's mutual-exclusion rules, and `TabBar` now
  passes widget/theme/ambient scalers unchanged, including custom and clamped nonlinear implementations.

- Breaking: closed the `MenuAnchor` divergence by landing its three missing primitives. Core gained
  `IMenuSerializableShortcut`/`ShortcutSerialization` (`SingleActivator`/`CharacterActivator` now serialize for
  menus) and `MouseRegion.OnHover`; `MaterialLocalizations` gained the 47 `KeyboardKey*` strings; and
  `_LocalizedShortcutLabeler` is ported, so `MenuItemButton`/`CheckboxMenuButton`/`RadioMenuButton` take a
  display-only `shortcut` whose label renders between the trailing icon and the submenu arrow with the source
  per-platform modifier order, separator and Apple ⌃⌥⇧⌘ symbols. `MenuItemButton` and `SubmenuButton` now read hover
  from `MouseRegion.OnHover` (edge-detected) instead of `onEnter`/`TextButton.onHover`, matching Flutter's
  scroll-under focus behavior, and `MenuItemButton` invalidates the traversal scope after taking focus.
  **Breaking:** `ButtonStyle.Alignment` and every `styleFrom` `alignment` parameter widen from `Alignment?` to
  `AlignmentGeometry?`, and `_MenuButtonDefaultsM3.alignment` is now the source `AlignmentDirectional.CenterStart`,
  so a menu button's content aligns to the text-direction start and mirrors under RTL.

- Breaking: ported Flutter's `tabs.dart`, `tab_bar_theme.dart`, `tab_controller.dart` and `tab_indicator.dart`
  strictly. `TabBar` now composes `_TabStyle` + `_TabLabelBar` (a `RenderFlex` subclass reporting tab offsets) under a
  `CustomPaint` driven by the ported `_IndicatorPainter`, replacing the bespoke `RenderTabBar`; the M2/M3 primary and
  secondary default tables, `_ChangeAnimation`/`_DragAnimation`, `_TabBarScrollController`/`_TabBarScrollPosition`
  initial-offset correction, the elastic/linear indicator math, the scrollable M3 divider/`Align` wrapper and
  `_warpToNonAdjacentTab` staging are all source-shaped. New `UnderlineTabIndicator`, `TabBarScrollController`,
  `TabBar.Secondary`, `TabValueChanged<T>`, `TabBar.SplashFactory`/`TextScaler`/`TabHasTextAndIcon` and
  `TabBarThemeData.SplashFactory`/`TextScaler`. **Breaking:** `TabBar.Indicator` and `TabBarThemeData.Indicator`
  widen from `BoxDecoration?` to `Decoration?`; `LabelColor` becomes `WidgetStateColor?`; `Padding`/`LabelPadding`/
  `IndicatorPadding`/`Tab.IconMargin` become `EdgeInsetsGeometry`; `MouseCursor` is a plain `MouseCursor?`;
  `OnHover`/`OnFocusChange` take `TabValueChanged<bool>`; `TabController.AnimationValue` is replaced by
  `TabController.Animation` (null after dispose); `TabBar.ScrollController` is a `TabBarScrollController`; and M2/M3
  label colors, indicator weights (M3 primary label indicators are 3px and rounded), divider defaults and scrollable
  `startOffset`/`start` alignment now follow the Dart tables. Core gained `AnimationWithParentMixin<T>` and
  `BuildContext.Size`, `AnimationController.AnimateTo` no longer clamps unbounded controllers to `[0, 1]`, and
  `PageView` now dispatches `ScrollUpdateNotification`/`ScrollEndNotification` so `TabBarView` syncs its controller
  through Flutter's `_handleScrollNotification` instead of a page-controller listener.

- Breaking: ported Flutter's `_MenuLayout` and rebased the menu buttons on `TextButton`. Menu overlays are now placed
  by `MenuStyle.Alignment` resolved within the anchor rect (`MenuStyle.Alignment` widened from `Alignment?` to
  `AlignmentGeometry?`; the panel defaults are the source `AlignmentDirectional.BottomStart`/`TopEnd` instead of
  `Alignment.BottomLeft`/`TopRight`), with directional `alignmentOffset` mirroring, cascade-flip vs screen-clamp chosen
  by parent orientation, `MediaQuery` padding/view-insets deflation and `DisplayFeatureSubScreen` sub-screens.
  `MenuItemButton` is now stateful (own focus node, so `requestFocusOnHover` works without an external one) and both
  it and `SubmenuButton` build a `TextButton` over `_MenuButtonDefaultsM3` + `_MenuItemLabel`: square shape, 64x48
  minimum, 24 icon size, `onSurface`/`onSurfaceVariant` roles, the 0.08/0.1 overlay ladder and the density- and
  text-scale-driven `_scaledPadding`. **Breaking:** a `SubmenuButton` in a `MenuBar` no longer paints a submenu arrow
  (Flutter shows it only inside a vertical menu), the default arrow is `Icons.ArrowRight`, and menu button metrics,
  colors and label spacing all change. Core gained `Alignment.WithinRect`/`AlongSize`/`Inscribe`, public
  `AlignmentGeometry.IsDirectional` (with Flutter's mixed-lerp semantics, so `Alignment.Center` and
  `AlignmentDirectional.Center` are no longer equal) and public `DisplayFeatureSubScreen.SubScreensInBounds`.

- Breaking: closed the `InputBorder`/`InputDecorator` divergence and landed the two core primitives it needed.
  `RenderObject.ApplyPaintTransform` is now Flutter's protocol: `GetTransformTo`/`LocalToGlobal`/the new
  `GlobalToLocal` compose the parent chain instead of the semantics walk, so they also resolve inside subtrees
  hidden from semantics; `RenderBox`, `RenderTransform`, `RenderFittedBox`, `RenderFractionalTranslation`,
  `RenderRotatedBox`, `RenderFlow` and `RenderFollowerLayer` implement it, and `GetPaintOffsetToRoot` is no longer
  a translations-only approximation. Core semantics gained `SemanticsTag`, `SemanticsConfiguration.AddTagForChildren`/
  `TagsChildrenWith`, `SemanticsNode.Tags` and `ChildSemanticsConfigurationsResultBuilder`; `Semantics` takes
  `tagForChildren`. `UI/Path` gained `Combine(PathOperation, …)`. **Breaking:** `InputBorder` is now a `ShapeBorder`
  record (so `Dimensions` is an `EdgeInsetsGeometry`, `LerpFrom`/`LerpTo` take `ShapeBorder?`, and the removed
  `InputBorder.Lerp` is `ShapeBorder.Lerp` — which switches at the midpoint between unlike borders instead of scaling
  through it, and no longer short-circuits t=0/1); `UnderlineInputBorder` paints its rounded branch through
  `BoxBorder.PaintNonUniformBorder`; new `ShapedInputBorder` wraps an arbitrary `ShapeBorder`. `_RenderDecoration`
  records the label transform in its own coordinate space and overrides `ApplyPaintTransform`, so the floating label's
  global rect is now the painted one; the decorator ports Flutter's semantics visit order and the affix
  `childConfigurationsDelegate`, so prefix/suffix/prefixIcon/suffixIcon form sibling nodes with per-decorator ordinal
  sort keys (0/1/2) instead of merging into one label.

- Breaking: rebased `Navigator` on the ported `Overlay`. `Route` gained `OverlayEntries`/`ChangedInternalState`/
  `ChangedExternalState`/`HasActiveRouteBelow`, the new `OverlayRoute` owns `CreateOverlayEntries`/`FinishedWhenPopped`,
  and `ModalRoute` now installs Flutter's `[barrier, scope]` entry pair with `Filter` (`BackdropFilter` on the barrier),
  `MaintainState`, `Offstage`, `CanPop` and the `_ModalScope` composition (`Offstage`/`PageStorage`/`Actions`+
  `DismissModalAction`/`PrimaryScrollController`/`FocusScope`/`RepaintBoundary`/transitions over a cached page).
  `TransitionRoute` drives `overlayEntries.first.opaque` from its animation status; `NavigatorState` exposes `Overlay`
  and `UserGestureInProgressNotifier`, rearranges entries to history order, and defers route disposal until the entries
  unmount. **Breaking:** routes below an opaque route now stay mounted (`maintainState` defaults to `true`) instead of
  being dropped from the tree, and they are no longer rebuilt by a push; `PageRoute`/`PageRouteBuilder`/`PopupRoute`
  take `maintainState`/`filter`; `ImpliesAppBarDismissal` is per-route (`HasActiveRouteBelow`) rather than
  `Navigator.CanPop`. Fixes: `OverlayEntry.Opaque` is settable before insertion and theater children carry the entry
  identity at both levels; focus traversal descends into nested scopes; `BuildOwner` no longer keeps dirty elements in
  a depth-ordered set (reparenting could corrupt it and crash a flush).

- Breaking: ported `material/time_picker.dart` and `material/time_picker_theme.dart` strictly. The dialog is now
  Flutter's widget tree — `_TimePickerModel` (aspect-based `InheritedModel`), `_DialTimePickerHeader`,
  `_DialTimeSelectorControl`/`_DialHourControl`/`_DialMinuteControl`, `_TimeSelectorSeparator`, `_DayPeriodControl`
  with `_AmPmButton` and `_RenderInputPadding`, `_TimePickerInput`/`_HourMinuteTextField`, and `_Dial`/`_DialPainter`
  with the source theta/radius math, shortest-path animation, inner/outer 24-hour ring and selector-dot label clip.
  `_TimePickerDefaultsM2`/`_TimePickerDefaultsM3` carry the exact M2/M3 tables (including the entry-mode-dependent
  `hourMinuteTextStyle` and both `inputDecorationTheme`s), and the dialog reproduces Flutter's size tables, minimum
  sizes, text-scale clamp, tap-target offset and `AnimatedContainer` resize.
  **Breaking:** `TimePickerThemeData` now matches Dart's field types — `DayPeriodColor`/`DayPeriodTextColor`/
  `DialTextColor`/`HourMinuteColor`/`HourMinuteTextColor` are `WidgetStateColor?` (a plain `Color` is auto-wrapped to
  selected-only, as in Dart), `DayPeriodShape` is `OutlinedBorder?`, and `Padding` is `EdgeInsetsGeometry?`;
  `TimePickerDialog.SwitchTo*EntryModeIcon` are `Icon?`; `TimePickerTheme` is an `InheritedTheme`.
  New primitives: `TextScaler.Clamp`, `WidgetStateColor.IsConstantColor`, `HapticFeedback.Vibrate`,
  `RenderObject.GetPaintOffsetToRoot`, `Semantics`/`RenderSemanticsAnnotations` increase/decrease values and actions,
  `TextFormField` `keyboardType`/`textInputAction`, and the `timePickerHourModeAnnouncement`/
  `timePickerMinuteModeAnnouncement` localizations.

- Breaking: ported Flutter's rich-text span model and rebased the paragraph stack on it —
  `painting/inline_span.dart`, `text_span.dart`, `placeholder_span.dart`, `text_scaler.dart`,
  `widgets/widget_span.dart`, `RichText` from `widgets/basic.dart`, and `Text`/`Text.rich` from
  `widgets/text.dart`. `InlineSpan`/`TextSpan`/`PlaceholderSpan`/`WidgetSpan` carry the source
  traversal, `ToPlainText`, `CodeUnitAt`, affinity-based `GetSpanForPosition`, semantics information
  with spell-out/locale attributes, and `CompareTo`. `RenderParagraph` is now a multi-child render
  object driven by an `InlineSpan`: the `RenderComparison` setter switch, styled runs and inline
  placeholders through one Avalonia `TextLayout`, the six `PlaceholderAlignment` rules, span hit
  testing that adds the hit `TextSpan` as the hit-test entry, and `AssembleSemanticsNode` with
  per-run `OrdinalSortKey` nodes plus tap/long-press actions.
  **Breaking:** `RenderParagraph.Text` is an `InlineSpan` (the flattened string moved to `PlainText`),
  `Text` is a `StatelessWidget` that builds `RichText`, and text scaling now lives on
  `RenderParagraph.TextScaler` instead of being folded into `FontSize`.
  Closes the `Tooltip.richMessage` divergence (Material `Tooltip` takes `message` or `richMessage`
  with the source mutual-exclusion guard) and the `RichText` half of the `MenuAccelerator` one (the
  default label is one paragraph whose accelerator run carries the underline).
  New primitives: `RenderComparison`, `TextStyle.CompareTo`, `TextScaler`, `PlaceholderDimensions`,
  `PlaceholderAlignment`, `TextParentData`, `RenderInlineChildrenContainerDefaults`, `IHitTestTarget`
  (now the `HitTestEntry` target type) and `IMouseTrackerAnnotation`.

- Breaking: moved Flutter's stateful menu-anchor tree into core and closed the `RawMenuAnchor`/`MenuAnchor`
  divergence. `widgets/raw_menu_anchor.dart` is now a strict port: `MenuController` (subclassable, with
  `Open(position)`/`Close`/`CloseChildren`/`IsOpen`/`MaybeOf`/`MaybeIsOpenOf`), the shared anchor/group state
  with parent/child registration, sibling exclusivity, root-anchor ancestor-scroll and view-size closure, the
  intercepted `onOpenRequested`/`onCloseRequested` protocol, `DismissMenuAction`, and the six-entry traversal
  shortcut map. Material `MenuAnchor` now sits on top of it: `_MenuAnchorScope`, the `_Submenu`/`_MenuPanel`
  composition (menu `FocusScope`, `Actions`+`Shortcuts`, `ScrollConfiguration`/`PrimaryScrollController`),
  the source eight-entry shortcut map, `_MenuDirectionalFocusAction`'s open/close/traverse behavior on
  `SubmenuButton`, the staggered per-item fades, the completion-gated panel `Scrollbar`, and `MenuBar` built
  on `RawMenuAnchorGroup`.
  **Breaking:** `MenuController` moved from `Plumix.Material` to `Plumix.Widgets` and no longer derives from
  `ChangeNotifier`; `Open`/`CloseChildren` throw when detached (`Close` stays silent). `RawMenuAnchor` is a
  `StatefulWidget` taking a `MenuController` (`RawMenuAnchorController` is gone), `RawMenuAnchorGroup` requires
  a controller, `MenuStyle.Padding` moved from `Thickness` to `EdgeInsetsGeometry`, `MenuAnchor.ReservedPadding`
  likewise, `MenuAnchor.AnchorTapClosesMenu` was dropped, and `MenuItemButton.OnPressed` now runs in a
  post-frame callback (Flutter restores focus first).
  New primitives: `TraversalDirection`, `DirectionalFocusIntent`/`Action`, `NextFocusIntent`/`Action`,
  `PreviousFocusIntent`/`Action`, `RequestFocusIntent`/`Action`, `FocusTraversalPolicy.InDirection`/
  `FindFirstFocus`/`FindLastFocus`/`InvalidateScopeData`, `FocusNode.NextFocus`/`PreviousFocus`/
  `FocusInDirection`/`HasPrimaryFocus`, `FocusScopeNode.HasFocusInScope`, `Scheduler.Phase` with
  `SchedulerPhase`, `Curves.TweenCurve`, and `EdgeInsetsGeometry.Clamp`/`Infinity`.

- Breaking: ported the `painting` border hierarchy strictly — `borders.dart`, `box_border.dart`,
  `rounded_rectangle_border.dart`, `stadium_border.dart`, `circle_border.dart`, `oval_border.dart`,
  `beveled_rectangle_border.dart`, `continuous_rectangle_border.dart`, `linear_border.dart`,
  `star_border.dart` and `shape_decoration.dart`. `ShapeBorder` is now the abstract Flutter class
  (`Dimensions`, `Add`/`operator +`, `Scale`, `LerpFrom`/`LerpTo`/`Lerp`, `GetOuterPath`/`GetInnerPath`,
  `PreferPaintInterior`/`PaintInterior`, `Paint`) with `CompoundBorder`, `OutlinedBorder` and the concrete
  shapes, including the private stadium/rounded-rect-to-circle interpolators and `StarBorder`'s conic path
  generator. `BoxBorder`/`Border`/`BorderDirectional` replace the old four-side record, and `ShapeDecoration`
  is Flutter's decoration (padding, `FromBoxDecoration`, hit testing, clip path, shape-driven paint).
  **Breaking:** `ShapeBorder.RoundedRectangle`/`Circle`/`Stadium`/`Border` factories are gone in favor of the
  real types; `BoxDecoration.Border` is a `BoxBorder?` and `BorderSides` was removed; `ButtonStyle.Shape`,
  `MenuStyle`, `SegmentedButton` and the list-tile controls take `OutlinedBorder`; `BorderSide.None` is black
  (was transparent) and `BorderSide.Scale` no longer carries `strokeAlign`. `Material` now paints through
  `ShapeDecoration` and clips with `ShapeBorderClipper`, so arbitrary shapes reach clips, hit tests and the
  bottom-app-bar notch.
  New primitives: `Path.AddRRect`/`AddPolygon`/`ConicTo`/`Reset`/`Transform`/`GetBounds`, `RRect.ShortestSide`/
  `InflateEdges`/`DeflateEdges`, `EdgeInsetsGeometry.Add`, `BorderRadiusGeometry * double`, `BorderSide.Merge`/
  `CanMerge`/`ToPen`, and `PaintingContext.DrawRRect`/`DrawDRRect`/`DrawOval`/`DrawPath`.

- Breaking: ported `material/input_decorator.dart` and `material/input_border.dart` strictly. The decoration is
  now laid out by `RenderDecoration` (`RenderInputDecoration.cs`), a slotted render object carrying Flutter's
  `_layout`/`performLayout` verbatim — baseline-driven slot placement, `_interpolateThree` outline alignment,
  container/subtext split, intrinsics, dry layout and dry baselines. `InputBorder`/`UnderlineInputBorder`/
  `OutlineInputBorder` moved to `InputBorder.cs` with the real paint math (`_gapBorderPath` arcs, `strokeOffset`
  inflation, bottom-radius clamping, `lerpFrom`/`lerpTo`/`scale`/equality). `_BorderContainer`, `_HelperError`,
  the affix opacity fades, the shaking label and the M2/M3 defaults (fill, indicator/outline sides, label/hint/
  helper/error styles, the full `contentPadding` table, the M3 input gap) are ported too.
  **Breaking:** `InputDecoration` is a record with `init` properties, `ContentPadding` moved from `Thickness` to
  `EdgeInsetsGeometry`, `InputBorder.CopyWith` takes a nullable side, per-state border slots (`disabledBorder`,
  `errorBorder`, …) are used verbatim instead of being state-resolved (only `border` resolves an
  `IStateInputBorder`), and `Hovered` is masked while disabled. `InputDecorationThemeData` gained
  `ActiveIndicatorBorder`, `OutlineBorder`, `VisualDensity`, `HintFadeDuration`, `AlignLabelWithHint`, the icon
  constraints and `Merge`; `InputDecorator`/`TextField` gained `textAlignVertical`, and `TextField` no longer
  forces `expands` for multiline.
  New core primitives: `TextAlignVertical`, `TextStyle.Merge`, `Listenable.Merge`, `RRect` (with `ScaleRadii`/
  `Inflate`/`ToPath`), `Path.AddArc`/`ArcTo`/`AddPath` plus open-contour stroking, `BorderSide.StrokeAlign`/
  `StrokeInset`/`StrokeOutset`/`StrokeOffset`/`Lerp`/`Scale`/`CopyWith`, `Radius.Clamp`, `BorderRadius * double`
  and `BorderRadius.ToRRect`.

- Closed the `RenderTable` semantics divergence: the shared pipeline gained Flutter's
  `RenderObject.AssembleSemanticsNode` hook (called for every semantic boundary, default annotates the node and
  adds the children), `SemanticsNode.UpdateWith` with public `Rect`/`IndexInParent` mutation, and
  `RenderObject.ClearSemantics` (recursive) plus the non-recursive `ClearOwnSemantics` used by `Detach`.
  `RenderTable` now synthesizes one semantics node per non-empty row (`SemanticsRole.Row`, `IndexInParent`, row
  box geometry) and wraps a cell in a `SemanticsRole.Cell` node when it produced several nodes or a node whose
  role is neither `Cell` nor `ColumnHeader`; cells narrower than their column edge are skipped, children are
  bucketed by geometry with an id-to-index map, and row/cell nodes are reused across passes and released on
  detach. Remaining pipeline-level gap (no per-node transform, no `showOnScreen` node callback): see
  `docs/ai/DIVERGENCES.md`.

- Breaking: closed the Material `BottomSheet`/`Scaffold` divergence against `DraggableScrollableSheet`, and moved
  modal-barrier ownership into `ModalRoute`. `BottomSheet` and the scaffold's `_StandardBottomSheet` now listen for
  `DraggableScrollableNotification`, so a draggable-scrollable child closes the sheet at its minimum extent
  (unless `shouldCloseOnMinExtent` is false), drives the new `Scaffold.bottomSheetScrimBuilder` body scrim
  (`max(0.1, 0.6 - extentRemaining * 3)` black by default) and shrinks the floating action button through
  Flutter's `extentRemaining * 3` visibility curve. A `Scaffold.bottomSheet` is wrapped in a
  `DraggableScrollableActuator` and registers a `LocalHistoryEntry` once dragged past its initial extent, so back
  resets the sheet instead of closing it.
  **Breaking:** `ModalRoute` now owns the barrier: `BarrierColor`/`BarrierDismissible`/`BarrierLabel`/`BarrierCurve`/
  `SemanticsDismissible` and an overridable `BuildModalBarrier()` build a barrier painted below the page, outside the
  route's transition, wrapped in `IgnorePointer` while the route animates out and sorted after the page
  (`OrdinalSortKey(1.0)` versus `0.0`). Every `ModalRoute` therefore contributes a barrier, and modal routes block
  the semantics of the routes below them. `ModalBottomSheetRoute`, `DialogRoute`, `PopupMenuRoute` and
  `DropdownRoute` stopped composing their own barriers in `BuildPage`; `DialogRoute.BarrierColor` is now `Color?`.
  Core semantics gained `SemanticsHitTestBehavior` (`Defer`/`Opaque`/`Transparent`) on `Semantics`,
  `RenderSemanticsAnnotations`, `SemanticsConfiguration` (with Flutter's absorb/compatibility rules) and
  `SemanticsNode`; the modal bottom sheet marks its page opaque so taps inside it never reach the barrier.
  `Scaffold` builds its overlay stack unconditionally and keys the snackbar/scrim/sheet/banner slots, so a slot
  appearing no longer rebuilds the body's elements (which re-registered its heroes and detached sheet controllers).
  `DraggableScrollableSheetTests` joined the serial scheduler collection; it drives the process-wide frame clock and
  could be rewound by another class mid-animation.

- Breaking: ported `widgets/draggable_scrollable_sheet.dart` — `DraggableScrollableSheet`,
  `DraggableScrollableController`, `DraggableScrollableNotification` and `DraggableScrollableActuator`, with
  Flutter's extent math, drag-versus-list hand-off, constant-velocity snapping (including `snapAnimationDuration`),
  implied min/max snap sizes, ballistic hand-off velocity boost, and the `hasDragged`/`hasChanged` rules that decide
  when a new `initialChildSize` moves the sheet.
  Supporting core primitives: `ScrollPosition.Absorb` plus virtual `ApplyUserOffset`/`GoBallistic`/`GoIdle`/
  `BeginActivity`/`Drag`, `ScrollPosition.NotificationContext`, virtual `ScrollController.Attach`/`Detach`,
  `AnimationController.Unbounded`/`AnimateWith`/`Velocity`, and `ChangeNotifier.HasListeners`.
  **Breaking:** a `Scrollable` replacing its `ScrollPosition` (physics or controller change) now absorbs the old
  position's pixels, extents, activity and in-flight drag instead of resetting to the stored offset.
  Test classes no longer run in parallel: the frame clock is process-wide, so concurrent classes could rewind each
  other's tickers mid-animation.

- Fixed `ModalBarrier` resolving its target platform from the host OS instead of `PlatformDefaults.TargetPlatform`,
  so barrier semantics (label, tap/dismiss actions, `SemanticsClipper`) ignored
  `PlatformDefaults.DebugTargetPlatformOverride` and varied by the machine running the tests. The private
  `ModalBarrierTargetPlatform` enum is gone; `PlatformSupportsDismissingBarrier` now takes `TargetPlatform`.
  Modal barrier/dialog/bottom-sheet semantics tests pin the platform instead of branching on `OperatingSystem`.

- Breaking: closed the `Table`/`RenderTable` divergence with a strict port of `rendering/table.dart`,
  `rendering/table_border.dart` and `widgets/table.dart`. The full `TableColumnWidth` algebra is available
  (`FlexColumnWidth`, `FractionColumnWidth`, `MaxColumnWidth`, `MinColumnWidth` join the existing fixed/intrinsic
  modes) and column sizing now runs Flutter's exact flex-grow/deficit-shrink algorithm instead of an approximation.
  `RenderTable` stores its cells as a flat row-major grid with `SetFlatChildren`/`SetChildren`/`AddRow`/`SetChild`/
  `Column`/`Row`, supports null cells, implements every intrinsic, dry-layout and dry-baseline path, paints arbitrary
  row `Decoration`s through cached `BoxPainter`s, and declares the `SemanticsRole.Table` boundary.
  `Table` now uses Flutter's `TableElement`, reconciling one `TableRow` at a time so keyed rows keep their state, and
  rejects irregular/empty rows and duplicate row or cell keys. **Breaking:** `Table.defaultColumnWidth` defaults to
  `FlexColumnWidth()` (was `IntrinsicColumnWidth()`), `TableRow.decoration` takes `Decoration` (was `BoxDecoration`),
  and `TableBorder` sides are non-nullable `BorderSide.None`-defaulted with `TableBorder.All(color:, width:, style:,
  borderRadius:)`/`Symmetric`/`Scale`/`Lerp` replacing `All(BorderSide)`.
  `PaginatedDataTable` restores its page index from `PageStorage`, and `ScrollView`/`SingleChildScrollView` only
  insert `PrimaryScrollController.None` when a primary controller was actually resolved.

- Breaking: completed the strict Material `BottomSheet`/`showModalBottomSheet`/`showBottomSheet` closeout. The sheet
  surface is now a real `Material` (elevation, surface tint, shadow, shape, clip) and the M3 default shape is
  top-only 28px corners instead of a uniform radius; drag handles resolve their color through hovered/dragged
  `WidgetState`s and are ordered before the content in the stack; drag release uses Flutter's fling/threshold math and
  ignores drags while the sheet is closing. `ModalBottomSheetRoute<T>` is now a `PopupRoute` driven by the route's own
  transition controller (`transitionDuration`/`reverseTransitionDuration`, caller-supplied controllers are never
  disposed), composes `AnimatedModalBarrier` with the localized `scrimLabel`/`scrimOnTapHint` and barrier-semantics
  clipping, animates through `ProxyAnimation`/`CurvedAnimation` with `Split` on drag release, and accepts
  `anchorPoint`. Scaffold-hosted sheets grow with `Align.heightFactor` on `fastOutSlowIn` rather than translating.
  New core primitives: `DisplayFeatureSubScreen` + `MediaQueryData.RemoveDisplayFeatures`, `PopupRoute`,
  `TransitionRoute.WillDisposeAnimationController`, `Curves.Split`/`EaseOutCubic`/`LegacyDecelerate`, and
  `MouseRegion.Opaque` (Flutter's `true` default, so mouse regions now hit-test themselves).
  `BottomSheet.CreateAnimationController` takes a ticker provider first and sets `ReverseDuration`;
  `MaterialLocalizations` gained `ScrimLabel`/`BottomSheetLabel`/`ScrimOnTapHint`.

- Breaking: closed the `ScrollPhysics` gesture-tuning divergence. `AlwaysScrollableScrollPhysics` and
  `NeverScrollableScrollPhysics` are ported, `ScrollPhysics.RecommendDeferredLoading` plus
  `ScrollPosition.RecommendDeferredLoading`/`Scrollable.RecommendDeferredLoadingForContext` are available, and
  `ShouldAcceptUserOffset` now registers or removes the scrollable's drag recognizers (and gates wheel scrolling)
  instead of being ignored. Drags run through a ported `ScrollDragController`/`HoldScrollActivity` pair, so iOS
  carried momentum and the 3.5px drag-start motion threshold apply; `DragGestureRecognizer` gained
  `OnDown`/`MinFlingDistance`/`MinFlingVelocity`/`MaxFlingVelocity` with Flutter's `considerFling` gate, so a release
  under the physics' fling floor now reports zero velocity and the reported fling is axis-projected and clamped.
  A pointer that never becomes a drag now reports one cancel, and `MediaQueryData.PhysicalSize` was added.

- Fixed `RenderTransform.EffectiveTransform` composing the alignment anchor in Flutter's column-vector order while
  Avalonia matrices are row-vector based, so every aligned `Transform` (`ScaleTransition`, `RotationTransition`,
  `MatrixTransition`, `RefreshProgressIndicator`) rotated/scaled around a mirrored anchor instead of the alignment
  point. Most visibly, the pull-to-refresh arrow flew outside its indicator circle while rotating.

- Breaking: completed the strict `BouncingScrollPhysics` (iOS rubber-band scrolling) closeout. A new
  `Plumix.Physics` library ports `Simulation`/`Tolerance`/`FrictionSimulation`/`SpringDescription`/`SpringSimulation`/
  `ScrollSpringSimulation`/`ClampedSimulation` with Flutter's exact math, plus `BouncingScrollSimulation` and
  `ClampingScrollSimulation`. `ScrollPhysics` gained the full source surface (`ApplyTo`/`Spring`/`ToleranceFor`/
  fling limits/`CarriedMomentum`/`AdjustPositionForNewDimensions`), `RangeMaintainingScrollPhysics` is now the real
  algorithm, `ScrollPosition.SetPixels` returns overscroll instead of clamping, ballistic activities follow the
  simulation and re-settle through `GoBallistic`/`ApplyNewDimensions`, and the viewports keep out-of-range offsets so
  the overscroll is visible. Pointer (wheel) scrolling follows the source rule and clamps its target into range, so
  only drags and flings rubber-band. `Simulation`/`FrictionSimulation` moved from `Plumix.Rendering` to `Plumix.Physics` and
  `FrictionSimulation`'s `drag` now has Flutter's meaning; `CarouselScrollPhysics` moved to `ScrollSpringSimulation`.

- Breaking: completed the strict Material `AppBar` closeout. The standard app bar now uses direct M2/M3
  `ColorScheme` roles, state-resolving scrolled-under surfaces/elevation, source `Material`/`NavigationToolbar`
  composition, visual configuration fields, system-overlay policy, and semantic ordering. Shared widget-state color
  and ordinal semantics-sort primitives, focused coverage, and the mirrored scroll-under demo probe were added.

- Breaking: completed the strict Material `Drawer` ColorScheme/theme closeout. The control now uses direct M3
  `surfaceContainerLow`, exact M2/M3 surface/shadow/tint/elevation defaults, direction-aware inner-edge shapes,
  source-shaped theme copy/lerp/capture, host-platform route semantics, and zero-width-compatible constraints.
  The source-ordered constructor, focused Flutter-test coverage, and mirrored runtime probe were updated.

- Breaking: completed the strict Material `PopupMenu` closeout. The family now uses source-shaped inherited-theme
  capture, directional padding, direct M2/M3 surface and label roles, navigator-owned route transitions, display-
  feature-aware placement, selected-item scrolling, stateful cursors, and zero-area-safe checked entries. Focused
  Flutter-test coverage and the mirrored M2/M3/directional-theme demo probes expanded.

- Breaking: completed the strict Material `Slider`/`RangeSlider` shape closeout. The family now exposes and
  executes Flutter-shaped track, thumb, overlay, tick, and value-indicator contracts; `SliderThemeData` carries
  the source fields/copy/lerp behavior, range selection and separation are pluggable, and `Slider.adaptive` routes
  Apple platforms through the new `CupertinoSlider`. Focused coverage and the mirrored custom-thumb probe expanded.

- Breaking: completed the strict Material `Autocomplete` closeout. The wrapper now matches Flutter's field/options
  composition, sizing, scrolling, selection, overlay, and option semantics; shared M2/M3 canvas, focus, and shadow
  defaults are source-shaped. Focused Flutter-test coverage and the mirrored live M2/M3 demo probe were expanded.

- Breaking: completed the strict Material `FlexibleSpaceBar` closeout. The control now uses the source stateful/
  layout-builder composition, logical title padding and scaled-width constraint, exact collapse and M2/M3 title
  rules, all zoom/blur/fade stretch modes, repaint-aware background opacity, and strict settings extents. Focused
  coverage and the mirrored SliverAppBar stretch-mode demo were expanded.

- Breaking: completed the strict Material `ToggleButtons` closeout. Direct `ColorScheme` defaults, state-resolving
  fills, exact checked/theme/TextButton composition, axis-aware tap targets, adjacent border ownership, intrinsic/
  baseline layout, RTL/vertical paint, and elliptical corner clipping now match Flutter; focused coverage and the
  mirrored state-fill probe were expanded.

- Breaking: closed the `RadioGroup` traversal divergence. Shared focus traversal groups now apply Flutter's stable
  geometry/bidi reading order and nested policies; radios use source shortcut-manager composition, selected-only Tab
  entry, enabled-only wrapping arrows, Space toggling, and non-radio shortcut fall-through with focused coverage.

- Breaking: completed the strict Material `Stepper` ColorScheme/API closeout. The control now uses direct M2/M3
  roles, `WidgetStateProperty`, directional inset APIs, `BoxBorder`, framework linear gradients, exact icon/error
  transitions and connector geometry, with expanded focused coverage and mirrored runtime probes.

- Breaking: closed the shared intrinsic/dry-layout divergence. `RenderBox` now caches and invalidates intrinsic,
  dry-layout, and nullable baseline queries with relayout-boundary propagation; flex, rotated box, flow, image,
  custom layout, fill/header slivers, intrinsic widgets, and extended-FAB overflow now use direct source algorithms.

- Breaking: completed the strict Material action-button closeout. Back, close, drawer, and end-drawer buttons now
  use the source `IconButton` inheritance/composition, standard-component keys, default-platform Android labels,
  direct M3 `onSurfaceVariant` and legacy M2 icon colors, plus source-shaped action-icon theme copying. Focused
  coverage and the mirrored M2/M3 scheme probe were expanded.

- Breaking: completed the Material chips ColorScheme/theme closeout. Exact M2 derived-color alpha behavior,
  source-shaped `ChipThemeData.copyWith`/lerp null-endpoint rules, shared icon-theme interpolation, and inherited-theme
  capture now match Flutter; focused coverage and the mirrored local-theme demo were updated.

- Breaking: completed the strict Material `ExpansionTile` closeout. The control and theme now expose source-shaped
  directional geometry, `ShapeBorder`, shared `AnimationStyle`, state-controller, and semantics APIs; direct M2/M3
  roles, exact `Expansible`/`ListTileTheme` composition, per-side border paint, controller lookup, `PageStorage`
  restoration, disabled/programmatic behavior, and mirrored live scheme probes now match the pinned Flutter source.

- Breaking: completed the strict Material `Radio`/`RadioListTile` closeout. The family now uses the shared
  `RawRadio` toggleable path, direct M2/M3 scheme roles, exact state/theme precedence, source painter geometry and
  timing, density-adjusted targets, expanded theme/list-tile APIs, adaptive registry behavior, and merged semantics.
  Added focused parity coverage and a mirrored live M2/M3 plus disabled-state demo probe.

- Breaking: closed the Material chips render divergence. `RawChip` now uses Flutter's three-slot intrinsic/dry
  layout, mirrored avatar/label/delete geometry and hit routing, painted checkmarks/scrims, enabled-state fading,
  minimum delete semantics bounds, and independent forward/reverse selection/avatar/delete/enable animation styles.
  Stateful chip sides/shapes and `ChipThemeData.fromDefaults` are source-shaped, with focused parity coverage.

- Breaking: completed the strict Material `ListTile` closeout. The widget and theme now use source-shaped M2/M3
  defaults, state resolution, ink/semantics/SafeArea composition, directional padding, and a dedicated slotted
  render object with intrinsic/dry layout. Added focused coverage and a mirrored M2/M3 demo probe.

- Breaking: completed the strict Material `Scrollbar` closeout. The theme now uses Flutter's
  `WidgetStateProperty` API and direct `ColorScheme.onSurface` roles; public painter/state extension contracts,
  controller validation, fade/hover/track motion, exact margin geometry, and adaptive Cupertino dark/resize/haptic
  behavior are covered, with a mirrored state-theme demo probe.

- Breaking: completed the strict Material `Switch` closeout. M2/M3 and adaptive defaults now read the exact direct
  roles, state colors, geometry, and 140/200/300 ms motion paths; thumb images, cursor, drag-start, padding, adaptive
  theme policy, theme copy/lerp, and source precedence are covered, with a mirrored M2/M3 demo probe.

- Breaking: completed the strict `ExpandIcon` closeout. Directional padding, half-turn transition composition,
  M2/M3 enabled and disabled colors, callback/state behavior, and action-specific semantic hints now match Flutter;
  shared IconButton state fallback and opacity rounding were corrected, with focused tests and a mirrored demo probe.

- Breaking: completed the `LinearProgressIndicator`/`CircularProgressIndicator` ColorScheme and API closeout.
  Defaults now read direct M2/M3 roles; the shared theme, controller precedence, circular padding, adaptive path,
  constructor contracts, and progress semantics are source-shaped. Added focused coverage and mirrored padding probes.

- Breaking: closed the shared Material ink-ownership divergence. `Material` now owns ordered descendant ink
  features, `Ink` decorations and responses paint beneath Material children, rapid splashes fade independently,
  pressed/hover/focus highlights use source timing, nested responses coordinate press ownership, and circular
  materials use oval clipping. Added focused source-test coverage and a mirrored timed-hover/rapid-tap demo probe.

- Breaking: completed the strict Material `Checkbox` closeout. The control now uses shared Flutter-shaped toggleable
  state and custom-paint geometry, direct M2/M3 `ColorScheme` defaults, mixed semantics, stateful sides, outlined
  shapes, cursor/density/theme copy/lerp APIs, exact tap-target and transition timing, focused source-test coverage,
  and a mirrored M2/M3 plus local-theme demo probe.

- Breaking: closed the remaining Apple page-transition divergence. Cupertino routes now use leading-edge drag
  ownership, linear finger tracking, exact velocity/position settle rules and timing, directional parallax and edge
  shadow paint, balanced navigator gesture callbacks, and LTR/RTL focused coverage.

- Breaking: completed the pinned Material page-transition closeout. Android now defaults to
  `PredictiveBackPageTransitionsBuilder`; shared/fullscreen predictive peek, cancel, commit, display-corner radii,
  exact fade/zoom timing, delegated transitions, retained subtree snapshots, and route snapshot permissions are
  framework-owned. Android 14+ now forwards native predictive-back progress, with focused source-test coverage and
  the mirrored nested-navigation demo updated to use Material routes.

- Breaking: completed the strict `ExpansionPanel`/`ExpansionPanelList` closeout. Public constructor ordering,
  salted keys, directional header geometry, exact `InkWell`/`IgnorePointer`/`ExpandIcon` composition, independent
  header/body/gap animations, radio ownership, callback ordering, colors, and focused source-test coverage now match.

- Completed the strict `GridTile` Dart closeout. The constructor now enforces the source non-null child contract,
  while focused coverage locks the exact direct-child and ordered fill/header/footer `Stack` composition.

- Breaking: completed the legacy and locale-aware Material typography foundation. `Typography.material2014`/
  `material2018`, exact platform color/font themes, dense/tall script geometry, localized `Theme.of` merging, M2/M3
  `ThemeData` selection, and the expanded Flutter-shaped `TextStyle` metadata now match the pinned source.

- Breaking: completed the strict `SearchDelegate` closeout. Its transition contract is now the source-shaped
  `Animation<double>` proxy backed by the shared 300 ms page-route fade; search fields forward keyboard type,
  action, correction, and suggestion configuration through editable/platform input, and search-input semantics,
  keyed body cross-fades, theme defaults, focused coverage, and the mirrored demo now match Flutter.

- Breaking: completed the `MaterialBanner` ColorScheme/theme closeout. M2/M3 surfaces and M3 divider now read
  direct scheme roles, local banner themes participate in inherited-theme capture, and entrance/exit composition
  uses shared Flutter-shaped threshold/vector animation primitives. Added focused coverage and a mirrored M2/M3
  direct-scheme demo probe.

- Breaking: completed the strict `DrawerHeader`/`UserAccountsDrawerHeader` closeout. Account surfaces now read
  `ColorScheme.primary`, directional insets and generic decorations match Flutter, and pictures/details use the
  source stack/custom-layout/ink/animation composition. Core icon labels and semantics container/merge behavior
  now follow Flutter; focused source-test coverage and the mirrored default-scheme demo were expanded.

- Breaking: completed the `FilledButton`/tonal ColorScheme and API closeout. Defaults now read the exact primary,
  secondary-container, on-surface, and shadow roles; callbacks, state controllers, clipping, cursor/density/timing,
  layer builders, inherited-theme capture, focused coverage, and the mirrored direct-scheme probe match Flutter.

- Breaking: completed the `OutlinedButton` ColorScheme/theme closeout. M2/M3 foreground, disabled, overlay,
  outline, tint, icon, cursor, density, and timing defaults now match Flutter; constructor callbacks/state/semantics
  and inherited-theme capture are source-shaped. Added focused coverage and a mirrored direct-scheme probe.

- Breaking: completed the `ElevatedButton` ColorScheme/theme closeout. M2/M3 enabled, disabled, overlay,
  shadow, tint, and icon defaults now read the exact Flutter roles; constructor callbacks/state/semantics, style
  metadata, and inherited-theme capture match the source. Added focused coverage and a mirrored scheme probe.

- Breaking: completed the strict `GridTileBar` Dart closeout. The control now uses source-shaped directional
  padding, inherited row/column/text direction, and `IconTheme.Merge`; shared `Padding`, `Flex`, and `Text`
  primitives now resolve omitted direction from `Directionality`. Added constructor, RTL, zero-area, layout,
  typography, icon, background, and overlay coverage against the pinned Flutter tests.

- Breaking: closed core scroll input-policy parity. `ScrollBehavior` now selects Flutter's base/iOS/macOS
  velocity trackers per pointer, drag recognizers honor custom tracker builders, and mouse-wheel axes flip for the
  configured logical modifiers while trackpads remain unchanged. Pointer-scroll responses now report accepted versus
  rejected platform-default handling, with focused estimator, behavior, modifier, and gesture integration coverage.

- Breaking: closed `MaterialBanner` presentation parity. Banner animation and inset APIs now accept generic
  `Animation<double>` and directional `EdgeInsetsGeometry`; `ScaffoldMessenger` owns the source FIFO queue,
  close reasons, accessible dismissal, and root-Scaffold presentation, while `Scaffold` pushes or overlays its body
  according to banner elevation. Added focused queue/layout/semantics coverage and a mirrored messenger demo probe.

- Breaking: completed the `TextButton` ColorScheme/theme closeout. M2 and M3 foreground, disabled, icon, and
  overlay defaults now read `ColorScheme.primary`/`onSurface` directly; M2 follows the pinned executable 0.10
  pressed/focused opacity. Added source callback/state/semantic plumbing, inherited-theme capture, focused tests,
  and a mirrored direct-scheme runtime probe.

- Breaking: completed the `CircleAvatar` ColorScheme closeout. Material 3 foreground/background defaults now read
  `onPrimaryContainer`/`primaryContainer` directly, with focused precedence and Material 2 brightness coverage plus
  a mirrored local-scheme demo probe.

- Breaking: completed the `RefreshIndicator`/`RefreshProgressIndicator` ColorScheme and composition closeout.
  Default value colors now read `ColorScheme.primary` directly, refresh surfaces use circular `Material`, pull and
  dismissal use the source two-controller transition tree, and active pulls suppress leading glow/stretch chrome.
  Added arbitrary-target `AnimationController.AnimateTo`, focused parity coverage, and a mirrored scheme-color probe.

- Breaking: closed Material `Stepper` animation and scrolling parity. Vertical headers now animate into view before
  callbacks, panels/icons/text use the shared 200 ms implicit-animation primitives, horizontal content preserves state
  through `Visibility`, and the retained Flutter `margin` metadata no longer adds non-source layout padding.

- Breaking: moved `MenuAnchor` panels onto the shared raw-menu/`OverlayPortal` pipeline. Menus now escape ancestor
  clips, honor nearest/root overlay selection, grouped outside-tap consumption, reserved padding, keyboard insets,
  display-feature sub-screens, explicit controller positions, and Flutter's panel fade/height timing. The animation
  callback now reports `AnimationStatus`, and unattached `MenuController.Open` is a no-op; mirrored demos and focused
  layout/default/lifecycle tests cover the new behavior. The remaining raw-controller-tree, ancestor-scroll,
  item-stagger, directional inset/focus, and scrollbar gaps stay tracked in `docs/ai/DIVERGENCES.md`.

- Breaking: completed the Material `Badge` ColorScheme/layout closeout. M3 defaults now read `error` and `onError`
  roles directly, narrow decorated children preserve Flutter's negative alignment space, and focused tests plus the
  mirrored runtime probe cover generated-token precedence and large-label stadium geometry.

- Breaking: closed `TickerMode` parity. The widget now composes a state-owned effective inherited mode with nested
  enabled/force-frame AND/OR semantics, merge/value/notifier APIs, reparent-safe ticker providers, and scheduler-level
  muting that preserves elapsed time without requesting hidden-subtree frames. Framework animation controllers now
  register with their owning state, with focused Flutter-test coverage and a mirrored maintained-visibility demo.

- Breaking: completed the strict `Divider`/`VerticalDivider` Dart closeout. Both controls now use direct M2/M3
  color roles and the source `SizedBox -> Center -> Container` composition, resolve directional indents and
  physical/directional per-corner radii, preserve hairline paint, and accept Flutter's non-negative numeric domain.
  Added per-side box borders, source null-child `Container` expansion, `DividerThemeData.CopyWith`/`Lerp`, inherited
  theme capture, focused Flutter-test parity coverage, and an expanded mirrored runtime probe.

- Breaking: closed `ReorderableList`/`ReorderableListView` overlay parity. Dragged items now use a theme-captured
  overlay proxy with source 250 ms pickup/drop choreography, constraints preservation, continuously ticking edge
  auto-scroll, and localized custom reorder semantics. Reorder callbacks now complete after the drop animation;
  deprecated `cacheExtent` is nullable and the controls expose `ScrollCacheExtent` plus sliver child-index lookup.
  Internal item keys now include their source index, preventing sliver child-list corruption when callbacks mutate a
  keyed backing list.

- Breaking: closed legacy dropdown and cross-fade directional-alignment parity. `DropdownMenuItem`,
  `DropdownButton`, `DropdownButtonFormField`, `Stack`, `IndexedStack`, `AnimatedSize`, and `AnimatedCrossFade`
  now accept `AlignmentGeometry`, retain Flutter's logical defaults, and resolve mixed physical/logical values from
  ambient text direction. Focused LTR/RTL tests and mirrored sample probes cover the new path.

- Breaking: closed magnifier overlay-order parity. `MagnifierController.OverlayEntry` and `Show(... below:)` now
  use core `OverlayEntry` instead of a navigator route, capture inherited themes into the root overlay, preserve
  source animation lifecycle, and let `SelectionOverlay` keep selection handles above lenses that exclude handles.

- Closed cross-host app lifecycle delivery parity. Browser focus/visibility, Android activity/window-focus, and iOS
  foreground/background notifications now feed the Flutter-shaped lifecycle synthesizer, including hidden-state
  transitions, duplicate suppression, and focused Android channel coverage.

- Breaking: closed `RawAutocomplete<T>`/Material `Autocomplete<T>` overlay parity. Suggestions now use the
  source `OverlayPortal` + grouped `TextFieldTapRegion` composition instead of pushing a route, follow live field
  transforms and safe insets, preserve inherited state, announce localized availability changes, and use the exact
  elevated Material surface plus keyed highlight scrolling. `WidgetsApp` now supplies the root overlay required by
  portal-backed framework controls; focused tests and the mirrored runtime probe cover the new path.

- Breaking: closed Material theme interpolation parity. Every component `*ThemeData` now exposes its source-shaped
  `Lerp` contract and participates in `ThemeData.Lerp`; theme extensions interpolate with Flutter's union semantics,
  and non-interpolable policy fields switch at the exact midpoint without endpoint identity shortcuts.

- Breaking: closed Material text-field selection handles end to end. Core now renders editable text through
  `RenderEditable`, drives `TextSelectionOverlay` from retained caret/line/viewport geometry, and supports in-field
  handle drags, adaptive touch magnifiers, and explicit/default spell-check services. `SelectableText` now uses the
  source read-only `EditableText` composition; Material supplies handle controls, misspelling defaults, suggestion
  replacement actions, focused tests, and a mirrored runtime probe.

- Ported the text selection handle overlay: core gains `TextSelectionControls`/`EmptyTextSelectionControls`,
  `TextSelectionHandleType`, `TextSelectionPoint`, `ITextSelectionDelegate`, `ClipboardStatusNotifier`, and
  `SelectionOverlay` with the source handle/toolbar overlay entries, 150 ms linear fades, `kMinInteractiveDimension`
  hit padding, and the touch-gated drag state machine. Material gains `MaterialTextSelectionControls`,
  `MaterialTextSelectionHandleControls`, the exact 22 px single-path handle painter, source anchors, and the legacy
  Cut/Copy/Paste/Select all toolbar. Landed the supporting primitives (`PanGestureRecognizer`,
  `DeviceGestureSettings`, drag details carrying pointer kind/local position/timestamp, `RawGestureDetector` pan
  callbacks) and made `EditableTextState` a `ITextSelectionDelegate`. `TextSelectionOverlay` and the automatic
  in-field magnifier remain blocked on a `RenderEditable` render object
  (`docs/ai/notes/widgets-2026-08-01-selection-handle-overlay.md`).

- Agent/contributor tooling: the code-style contract is now machine-checked instead of review-only.
  `EnforceCodeStyleInBuild` makes IDE0008 (explicit types for built-ins) a build error, nullable warnings
  are errors, and `scripts/check_line_length.sh` enforces the 120-char rule on new/edited lines
  (Claude Code hook + PR gate). CI now builds `src/Plumix.Ci.slnf`, so a public API change can no longer
  break `Plumix.FSharp`/`Plumix.Elmish` unnoticed until pack time.

- Agent/contributor tooling: added `docs/ai/PORT_PLAYBOOK.md` (executable port sequence, including target
  selection), `docs/ai/DART_SPEC_PROTOCOL.md` (reading large Dart sources without exhausting context) and
  generated `docs/ai/PORT_MAP.md` (Flutter file -> C# files/tests/demos, from the existing parity markers).
  Pinned the Flutter parity revision to 3.44.0 in `AGENTS.md` and moved the reference checkouts behind the
  `flutter-src`/`avalonia-src` symlinks. Rotated closed milestones out of `docs/FRAMEWORK_PLAN.md`
  (156 KB -> 6 KB) into `docs/FRAMEWORK_PLAN-archive.md`.

- Breaking: completed the `Card` Dart closeout: elevated/filled/outlined variants now use direct M2/M3
  `ColorScheme` roles and the source `Semantics -> Padding -> Material(type: card) -> Semantics` composition,
  including exact tint, shadow, shape, clipping, border paint order, and theme precedence. Added
  `CardThemeData.CopyWith`/`Lerp`, the source-compatible local `CardTheme`, `ThemeData.Lerp` integration, focused
  tests, and an expanded mirrored runtime probe; advanced `Plumix.Material` to `0.20.0-alpha.1`.

- Breaking: completed the `IconButton` Dart closeout: all four constructors now expose the source API, Material 2
  uses the legacy `InkResponse` composition, and Material 3 uses direct `ColorScheme` roles, stadium geometry,
  standard density, external state controllers, tooltips, adaptive cursors, and source style precedence. Added
  `IconButtonThemeData.CopyWith`/`Lerp`, `ButtonStyle.Lerp`, Material `Theme` icon inheritance, focused tests, and a
  mirrored M2/M3 runtime probe; advanced `Plumix.Material` to `0.19.0-alpha.1`.

- Breaking: completed the `FloatingActionButton` ColorScheme/theme/layout closeout: M2/M3 defaults now read exact
  source roles and state colors, all variants use source shapes and adaptive cursors, omitted/default versus explicit
  null hero tags match Flutter, extended content uses the source overflow layout, and output merges semantics. Added
  `FloatingActionButtonThemeData.CopyWith`/`Lerp`, inherited-theme capture, and an M2/M3 runtime probe whose
  secondary FABs explicitly disable hero registration; advanced `Plumix.Material` to `0.18.0-alpha.1`.

- Breaking: completed the `BottomAppBar` ColorScheme/theme/geometry closeout: M2/M3 defaults now read exact source
  roles, surface tint and elevation overlays follow Flutter, full-strength physical shadows and transparent
  `Material` composition are restored, and notches track the configured FAB rectangle while excluding the cutout
  from hit testing. Added `BottomAppBarThemeData.CopyWith`/`Lerp`, inherited-theme capture, and a mirrored
  center-docked runtime probe; advanced `Plumix.Material` to `0.17.0-alpha.1`.

- Breaking: completed the legacy `BottomNavigationBar` ColorScheme/theme closeout: fixed and shifting defaults now
  read source roles directly, dark fixed selection uses `secondary`, shifting content uses `surface`, icon-theme
  opacity is preserved, and the default body typography/elevation/shadow paths match Flutter. Added
  `BottomNavigationBarThemeData.CopyWith`/`Lerp` and `ThemeData.Lerp` integration; advanced `Plumix.Material` to
  `0.16.0-alpha.1`.

- Breaking: completed the `NavigationDrawer` ColorScheme/theme closeout: M3 drawer surfaces and destination
  defaults now read source roles directly, selected/disabled colors and stadium indicator geometry match Flutter,
  and `NavigationDrawerThemeData.CopyWith`/state-aware `Lerp` now participates in `ThemeData.Lerp`. Advanced
  `Plumix.Material` to `0.15.0-alpha.1`.

- Breaking: completed the `NavigationRail` ColorScheme/theme closeout: M2/M3 defaults and disabled/ink states now
  read source roles directly, M2 preserves the source unselected-icon opacity contract, and M3 uses a stadium
  indicator. Added `NavigationRailThemeData.CopyWith`/`Lerp`, shared component-theme lerp helpers, and
  `ThemeData.Lerp` integration; advanced `Plumix.Material` to `0.14.0-alpha.1`.

- Breaking: completed the `NavigationBar` ColorScheme/theme closeout: M2/M3 defaults now read source roles directly,
  the M2 surface uses Flutter's elevation-overlay formula, the M3 indicator uses a stadium shape, and
  `NavigationBarThemeData` now supports `CopyWith`/state-aware `Lerp` through `ThemeData.Lerp`. Advanced
  `Plumix.Material` to `0.13.0-alpha.1`.

- Breaking: added a Flutter-shaped Material theme foundation with `ColorScheme`, `TextTheme`, and
  `Typography`. Added all Material 3 color roles, exact HCT seed generation for every dynamic scheme variant and
  contrast level, complete 2021 type-scale composition, scheme-driven `ThemeData` defaults/interpolation, focused
  coverage, and a mirrored palette/typography runtime probe. Advanced `Plumix.Material` to `0.12.0-alpha.1`.

