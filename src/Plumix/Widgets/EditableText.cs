using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/editable_text.dart

namespace Plumix.Widgets;

// The parts of `EditableTextState` that live around the field's own `Scrollable`: the scroll
// wrapper, caret-on-screen scheduling, the input connection's per-frame geometry and its
// `_CompositionCallback`, the floating cursor, the autocorrection prompt rect, and the context menu
// hiding on scroll start and re-showing on scroll end.
public sealed partial class EditableText
{
    public sealed partial class EditableTextState
    {
        // --------------------------------------------------------------- scrolling

        /// Dart's `_scrollController`: the widget's controller, or one created on first use and never
        /// replaced (the `Scrollable` swaps positions itself when the widget's controller changes).
        private ScrollController EffectiveScrollController =>
            Widget.ScrollController ?? (_internalScrollController ??= new ScrollController());

        /// Dart's `_isMultiline`: `maxLines: null` is multiline too.
        private bool IsMultiline => Widget.MaxLines != 1;

        private Widget BuildScrollable(BuildContext context, Func<ViewportOffset, Widget> viewportBuilder)
        {
            ScrollPhysics? physics = Widget.ScrollPhysics
                                     ?? (!IsMultiline && PlatformDefaults.TargetPlatform == TargetPlatform.IOS
                                         ? new NeverUserScrollableScrollPhysics()
                                         : null);
            return new NotificationListener<ScrollNotification>(
                onNotification: notification =>
                {
                    HandleContextMenuOnScroll(notification);
                    _scribbleCacheKey = null;
                    return false;
                },
                child: new Scrollable(
                    key: _scrollableKey,
                    excludeFromSemantics: true,
                    axisDirection: IsMultiline ? AxisDirection.Down : AxisDirection.Right,
                    controller: EffectiveScrollController,
                    physics: physics,
                    dragStartBehavior: Widget.DragStartBehavior,
                    restorationId: Widget.RestorationId,
                    scrollBehavior: Widget.ScrollBehavior
                                    ?? ScrollConfiguration.Of(context)
                                        .CopyWith(scrollbars: IsMultiline, overscroll: false),
                    viewportBuilder: (_, offset) => viewportBuilder(offset)));
        }

        // --------------------------------------------------------- caret on screen

        /// Dart's `_scheduleShowCaretOnScreen`: after the next frame, scrolls the field so the caret
        /// (or the moving end of a selection) is visible, then asks the enclosing viewports to reveal
        /// it padded by <see cref="EditableText.ScrollPadding"/> and the selection handle.
        private void ScheduleShowCaretOnScreen(bool withAnimation)
        {
            if (_showCaretOnScreenScheduled)
            {
                return;
            }

            _showCaretOnScreenScheduled = true;
            Scheduler.AddPostFrameCallback(_ =>
            {
                _showCaretOnScreenScheduled = false;
                // Dart only checks for a render object; Plumix's scheduler is process-wide, so the
                // callback can also outlive the state or run before the field's first layout.
                RenderEditable? renderEditable = Mounted ? RenderEditable : null;
                if (renderEditable is not { HasSize: true, Attached: true }
                    || !(renderEditable.Selection?.IsValid ?? false)
                    || !EffectiveScrollController.HasClients)
                {
                    return;
                }

                double lineHeight = renderEditable.PreferredLineHeight;
                double bottomSpacing = Widget.ScrollPadding.Bottom;
                if (_selectionOverlay?.SelectionOverlay.SelectionControls is TextSelectionControls controls)
                {
                    double handleHeight = controls.GetHandleSize(lineHeight).Height;
                    double interactiveHandleHeight = Math.Max(
                        handleHeight,
                        WidgetConstants.MinInteractiveDimension);
                    Point anchor = controls.GetHandleAnchor(TextSelectionHandleType.Collapsed, lineHeight);
                    double handleCenter = handleHeight / 2 - anchor.Y;
                    bottomSpacing = Math.Max(handleCenter + interactiveHandleHeight / 2, bottomSpacing);
                }

                Thickness scrollPadding = Widget.ScrollPadding;
                var caretPadding = new Thickness(
                    scrollPadding.Left,
                    scrollPadding.Top,
                    scrollPadding.Right,
                    bottomSpacing);
                Rect caretRect = renderEditable.GetLocalRectForCaret(renderEditable.Selection!.Value.Extent);
                RevealedOffset targetOffset = GetOffsetToRevealCaret(caretRect);

                TextSelection selection = TextEditingValue.Selection;
                Rect rectToReveal;
                if (selection.IsCollapsed)
                {
                    rectToReveal = targetOffset.Rect;
                }
                else
                {
                    IReadOnlyList<TextBox> boxes = renderEditable.GetBoxesForSelection(selection);
                    rectToReveal = boxes.Count == 0
                        ? targetOffset.Rect
                        : selection.BaseOffset < selection.ExtentOffset
                            ? boxes[^1].ToRect()
                            : boxes[0].ToRect();
                }

                if (withAnimation)
                {
                    EffectiveScrollController.AnimateTo(
                        targetOffset.Offset,
                        CaretAnimationDuration,
                        CaretAnimationCurve);
                    renderEditable.ShowOnScreen(
                        rect: caretPadding.InflateRect(rectToReveal),
                        duration: CaretAnimationDuration,
                        curve: CaretAnimationCurve);
                }
                else
                {
                    EffectiveScrollController.JumpTo(targetOffset.Offset);
                    renderEditable.ShowOnScreen(rect: caretPadding.InflateRect(rectToReveal));
                }
            }, "EditableText.showCaret");
        }

        /// Dart's `_getOffsetToRevealCaret`: the scroll offset of the field's own scrollable that
        /// reveals <paramref name="rect"/>, and the rect moved to where it will be at that offset.
        private RevealedOffset GetOffsetToRevealCaret(Rect rect)
        {
            ScrollController scrollController = EffectiveScrollController;
            if (!scrollController.Position.AllowImplicitScrolling)
            {
                return new RevealedOffset(scrollController.Offset, rect);
            }

            RenderEditable renderEditable = RenderEditable!;
            Size editableSize = renderEditable.Size;
            double additionalOffset;
            Vector unitOffset;
            if (!IsMultiline)
            {
                additionalOffset = rect.Width >= editableSize.Width
                    ? editableSize.Width / 2 - rect.Center.X
                    : ClampDouble(0.0, rect.Right - editableSize.Width, rect.Left);
                unitOffset = new Vector(1, 0);
            }
            else
            {
                double expandedHeight = Math.Max(rect.Height, renderEditable.PreferredLineHeight);
                Rect expandedRect = new(
                    rect.Center.X - rect.Width / 2,
                    rect.Center.Y - expandedHeight / 2,
                    rect.Width,
                    expandedHeight);
                additionalOffset = expandedRect.Height >= editableSize.Height
                    ? editableSize.Height / 2 - expandedRect.Center.Y
                    : ClampDouble(0.0, expandedRect.Bottom - editableSize.Height, expandedRect.Top);
                unitOffset = new Vector(0, 1);
            }

            double targetOffset = ClampDouble(
                additionalOffset + scrollController.Offset,
                scrollController.Position.MinScrollExtent,
                scrollController.Position.MaxScrollExtent);
            double offsetDelta = scrollController.Offset - targetOffset;
            return new RevealedOffset(targetOffset, rect.Translate(unitOffset * offsetDelta));
        }

        /// Dart's `clampDouble`: <paramref name="value"/> clamped into [min, max], asserting nothing
        /// when min exceeds max the way `Math.Clamp` would throw.
        private static double ClampDouble(double value, double min, double max) =>
            value < min ? min : value > max ? max : value;

        /// <inheritdoc/>
        /// <remarks>Dart's <c>EditableTextState.bringIntoView</c>: jumps the field's scrollable, then
        /// reveals the caret in every enclosing viewport, with no padding and no animation.</remarks>
        public void BringIntoView(TextPosition position)
        {
            RenderEditable renderEditable = RenderEditable!;
            Rect localRect = renderEditable.GetLocalRectForCaret(position);
            RevealedOffset targetOffset = GetOffsetToRevealCaret(localRect);
            EffectiveScrollController.JumpTo(targetOffset.Offset);
            renderEditable.ShowOnScreen(rect: targetOffset.Rect);
        }

        /// Dart's `_bringIntoViewBySelectionState`: a long press or drag (Apple) or a drag (others)
        /// reveals the end of the selection that moved.
        private void BringIntoViewBySelectionState(
            TextSelection oldSelection,
            TextSelection newSelection,
            SelectionChangedCause? cause)
        {
            if (RenderEditable is not { HasSize: true } || !EffectiveScrollController.HasClients)
            {
                return;
            }

            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.IOS:
                case TargetPlatform.MacOS:
                    if (cause is SelectionChangedCause.LongPress or SelectionChangedCause.Drag)
                    {
                        BringIntoView(newSelection.Extent);
                    }

                    break;
                default:
                    if (cause == SelectionChangedCause.Drag)
                    {
                        if (oldSelection.BaseOffset != newSelection.BaseOffset)
                        {
                            BringIntoView(newSelection.Base);
                        }
                        else if (oldSelection.ExtentOffset != newSelection.ExtentOffset)
                        {
                            BringIntoView(newSelection.Extent);
                        }
                    }

                    break;
            }
        }

        /// The first statement of Dart's `userUpdateTextEditingValue`: a read-only field shows the
        /// caret when the selection changes, an editable one when anything changes. It compares the
        /// proposed value, before the input formatters run.
        private void ScheduleShowCaretForUserUpdate(TextEditingValue oldValue, TextEditingValue value)
        {
            bool shouldShowCaret = Widget.ReadOnly
                ? !oldValue.Selection.Equals(value.Selection)
                : !oldValue.Equals(value);
            if (shouldShowCaret)
            {
                ScheduleShowCaretOnScreen(withAnimation: true);
            }
        }

        /// Dart's post-frame `bringIntoView` after a toolbar cut or paste.
        private void BringSelectionIntoViewAfterFrame()
        {
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (Mounted && RenderEditable is { HasSize: true } && EffectiveScrollController.HasClients)
                {
                    BringIntoView(TextEditingValue.Selection.Extent);
                }
            }, "EditableText.bringSelectionIntoView");
        }

        // ------------------------------------------------------------ view metrics

        private void StartObservingMetrics()
        {
            if (!_observingMetrics)
            {
                WidgetsBinding.Instance.AddObserver(this);
                _observingMetrics = true;
            }

            // Dart reads `View.of(context)`; a host-less harness may mount the field without a view.
            _lastBottomViewInset = View.MaybeOf(Context)?.ViewInsets.Bottom ?? 0.0;
        }

        private void StopObservingMetrics()
        {
            if (_observingMetrics)
            {
                WidgetsBinding.Instance.RemoveObserver(this);
                _observingMetrics = false;
            }
        }

        /// <inheritdoc/>
        /// <remarks>Dart's <c>EditableTextState.didChangeMetrics</c>: the observer is registered
        /// only while focused; a keyboard that grows reveals the caret without animation.</remarks>
        public void DidChangeMetrics()
        {
            if (!Mounted)
            {
                return;
            }

            if (View.MaybeOf(Context) is not FlutterView view)
            {
                return;
            }

            if (_lastBottomViewInset != view.ViewInsets.Bottom)
            {
                Scheduler.AddPostFrameCallback(
                    _ => _selectionOverlay?.UpdateForScroll(),
                    "EditableText.updateForScroll");
                if (_lastBottomViewInset < view.ViewInsets.Bottom)
                {
                    ScheduleShowCaretOnScreen(withAnimation: false);
                }
            }

            _lastBottomViewInset = view.ViewInsets.Bottom;
        }

        // ------------------------------------------- input connection geometry

        /// Dart's `_hasInputConnection`.
        private bool HasInputConnection => _textInputConnection?.Attached ?? false;

        /// Dart's `_compositeCallback`: after every composite that includes the field, the input
        /// connection learns the editable's size and transform.
        private void CompositeCallback(Layer layer)
        {
            // The callback can be invoked when the layer is detached, and the platform can close the
            // input connection without this widget rebuilding.
            if (RenderEditable is not { Attached: true } || !HasInputConnection)
            {
                return;
            }

            Debug.Assert(Mounted);
            UpdateSizeAndTransform();
        }

        /// Dart's `_updateSizeAndTransform`. Must be called after layout.
        private void UpdateSizeAndTransform()
        {
            RenderEditable renderEditable = RenderEditable!;
            Size size = renderEditable.Size;
            Matrix4 transform = renderEditable.GetTransformTo(null);
            _textInputConnection!.SetEditableSizeAndTransform(size, transform);
        }

        /// Dart's `_schedulePeriodicPostFrameCallbacks`: sends the composing and caret rects after
        /// every frame while the input connection is open.
        private void SchedulePeriodicPostFrameCallbacks(TimeSpan duration = default)
        {
            if (!HasInputConnection)
            {
                return;
            }

            UpdateSelectionRects();
            UpdateComposingRectIfNeeded();
            UpdateCaretRectIfNeeded();
            Scheduler.AddPostFrameCallback(
                SchedulePeriodicPostFrameCallbacks,
                "EditableText.postFrameCallbacks");
        }

        /// Dart's `_updateComposingRectIfNeeded`.
        private void UpdateComposingRectIfNeeded()
        {
            if (RenderEditable is not { HasSize: true } renderEditable)
            {
                return;
            }

            TextRange composingRange = TextEditingValue.Composing ?? new TextRange(-1, -1);
            Rect? composingRect = renderEditable.GetRectForComposingRange(composingRange);
            if (composingRect is null)
            {
                int offset = composingRange.IsValid ? composingRange.Start : 0;
                composingRect = renderEditable.GetLocalRectForCaret(new TextPosition(offset));
            }

            _textInputConnection!.SetComposingRect(composingRect.Value);
        }

        /// Dart's `_updateCaretRectIfNeeded`: the caret at the selection's start.
        private void UpdateCaretRectIfNeeded()
        {
            if (RenderEditable is not { HasSize: true } renderEditable
                || renderEditable.Selection is not { IsValid: true } selection)
            {
                return;
            }

            Rect caretRect = renderEditable.GetLocalRectForCaret(new TextPosition(selection.Start));
            _textInputConnection!.SetCaretRect(caretRect);
        }

        // ---------------------------------------------------------- floating cursor

        /// Dart's `_floatingCursorOffset`: the floating cursor is positioned by its vertical center.
        private Point FloatingCursorOffset => new(0, RenderEditable!.PreferredLineHeight / 2);

        /// <inheritdoc/>
        /// <remarks>Dart's <c>EditableTextState.updateFloatingCursor</c>.</remarks>
        public void UpdateFloatingCursor(RawFloatingCursorPoint point)
        {
            if (_floatingCursorResetController is null)
            {
                _floatingCursorResetController = new AnimationController(vsync: this);
                _floatingCursorResetController.AddListener(OnFloatingCursorResetTick);
            }

            RenderEditable renderEditable = RenderEditable!;
            switch (point.State)
            {
                case FloatingCursorDragState.Start:
                {
                    if (_floatingCursorResetController.IsAnimating)
                    {
                        _floatingCursorResetController.Stop();
                        OnFloatingCursorResetTick();
                    }

                    StopCursorBlink(resetCharTicks: false);
                    CursorBlinkOpacityController.SetValue(1.0);
                    _pointOffsetOrigin = point.Offset;
                    Point startCaretCenter;
                    TextPosition currentTextPosition;
                    bool shouldResetOrigin;
                    if (point.StartLocation is { } startLocation)
                    {
                        shouldResetOrigin = false;
                        (startCaretCenter, currentTextPosition) = startLocation;
                    }
                    else
                    {
                        shouldResetOrigin = true;
                        TextSelection selection = renderEditable.Selection!.Value;
                        currentTextPosition = new TextPosition(selection.BaseOffset, selection.Affinity);
                        startCaretCenter = renderEditable.GetLocalRectForCaret(currentTextPosition).Center;
                    }

                    _startCaretCenter = startCaretCenter;
                    _lastBoundedOffset = renderEditable.CalculateBoundedFloatingCursorOffset(
                        _startCaretCenter.Value - FloatingCursorOffset,
                        shouldResetOrigin: shouldResetOrigin);
                    _lastTextPosition = currentTextPosition;
                    renderEditable.SetFloatingCursor(point.State, _lastBoundedOffset.Value, _lastTextPosition.Value);
                    break;
                }
                case FloatingCursorDragState.Update:
                {
                    Point centeredPoint = point.Offset!.Value - _pointOffsetOrigin!.Value;
                    Point rawCursorOffset = _startCaretCenter!.Value + centeredPoint - FloatingCursorOffset;
                    _lastBoundedOffset = renderEditable.CalculateBoundedFloatingCursorOffset(rawCursorOffset);
                    _lastTextPosition = renderEditable.GetPositionForPoint(
                        renderEditable.LocalToGlobal(_lastBoundedOffset.Value + FloatingCursorOffset));
                    renderEditable.SetFloatingCursor(point.State, _lastBoundedOffset.Value, _lastTextPosition.Value);
                    break;
                }
                case FloatingCursorDragState.End:
                    // Resume cursor blinking.
                    if (_focusNode?.HasFocus == true)
                    {
                        StartCursorBlink();
                    }

                    if (_lastTextPosition is not null && _lastBoundedOffset is not null)
                    {
                        _floatingCursorResetController.SetValue(0.0);
                        _floatingCursorResetController.AnimateTo(
                            1.0,
                            duration: FloatingCursorResetTime,
                            curve: Curves.Decelerate);
                    }

                    break;
            }
        }

        /// Dart's `_onFloatingCursorResetTick`: animates the floating cursor back onto the caret and,
        /// when the animation completes, commits a one-finger move as a force-press selection.
        private void OnFloatingCursorResetTick()
        {
            RenderEditable renderEditable = RenderEditable!;
            Rect caretRect = renderEditable.GetLocalRectForCaret(_lastTextPosition!.Value);
            Point finalPosition = new Point(caretRect.Left, caretRect.Center.Y) - FloatingCursorOffset;
            if (_floatingCursorResetController!.Status.IsCompleted())
            {
                renderEditable.SetFloatingCursor(FloatingCursorDragState.End, finalPosition, _lastTextPosition.Value);
                // A one-finger move collapses the selection; a two-finger selection keeps what the
                // engine selected.
                if (renderEditable.Selection!.Value.IsCollapsed)
                {
                    HandleSelectionChanged(
                        TextSelection.FromPosition(_lastTextPosition.Value),
                        SelectionChangedCause.ForcePress);
                }

                _startCaretCenter = null;
                _lastTextPosition = null;
                _pointOffsetOrigin = null;
                _lastBoundedOffset = null;
            }
            else
            {
                double lerpValue = _floatingCursorResetController.Value;
                Point lastBoundedOffset = _lastBoundedOffset!.Value;
                double lerpX = lastBoundedOffset.X + (finalPosition.X - lastBoundedOffset.X) * lerpValue;
                double lerpY = lastBoundedOffset.Y + (finalPosition.Y - lastBoundedOffset.Y) * lerpValue;
                renderEditable.SetFloatingCursor(
                    FloatingCursorDragState.Update,
                    new Point(lerpX, lerpY),
                    _lastTextPosition.Value,
                    resetLerpValue: lerpValue);
            }
        }

        // -------------------------------------------------- autocorrection prompt

        /// <inheritdoc/>
        /// <remarks>Dart's <c>EditableTextState.showAutocorrectionPromptRect</c>: the range is painted
        /// in <see cref="EditableText.AutocorrectionTextRectColor"/> until the text or composing region
        /// changes or the field loses focus.</remarks>
        public void ShowAutocorrectionPromptRect(int start, int end)
        {
            SetState(() => _currentPromptRectRange = new TextRange(start, end));
        }

        // ------------------------------------------- context menu on scroll

        /// Dart's `_disposeScrollNotificationObserver`.
        private void DisposeScrollNotificationObserver()
        {
            _listeningToScrollNotificationObserver = false;
            if (_scrollNotificationObserver is not null)
            {
                _scrollNotificationObserver.RemoveListener(HandleContextMenuOnParentScroll);
                _scrollNotificationObserver = null;
            }
        }

        /// The tail of Dart's `showToolbar`: on platforms whose menu fades on scroll, listen to the
        /// nearest <see cref="ScrollNotificationObserver"/> for ancestor scrolls.
        private void ListenToParentScrollsIfNeeded()
        {
            if (!_platformSupportsFadeOnScroll)
            {
                return;
            }

            _listeningToScrollNotificationObserver = true;
            _scrollNotificationObserver?.RemoveListener(HandleContextMenuOnParentScroll);
            _scrollNotificationObserver = ScrollNotificationObserver.MaybeOf(Context);
            _scrollNotificationObserver?.AddListener(HandleContextMenuOnParentScroll);
        }

        /// The mobile tail of Dart's `didChangeDependencies`: an orientation change hides the menu
        /// (keeping the handles on iOS), and a live parent-scroll subscription follows a moved
        /// observer.
        private void DidChangeDependenciesForContextMenu()
        {
            TargetPlatform platform = PlatformDefaults.TargetPlatform;
            if (platform is not (TargetPlatform.IOS or TargetPlatform.Android))
            {
                return;
            }

            Orientation? orientation = MediaQuery.MaybeOrientationOf(Context);
            if (_lastOrientation is null)
            {
                _lastOrientation = orientation;
                return;
            }

            if (orientation != _lastOrientation)
            {
                _lastOrientation = orientation;
                if (platform == TargetPlatform.IOS)
                {
                    HideToolbar(hideHandles: false);
                }
                else
                {
                    HideToolbar();
                }
            }

            if (_listeningToScrollNotificationObserver)
            {
                _scrollNotificationObserver?.RemoveListener(HandleContextMenuOnParentScroll);
                _scrollNotificationObserver = ScrollNotificationObserver.MaybeOf(Context);
                _scrollNotificationObserver?.AddListener(HandleContextMenuOnParentScroll);
            }
        }

        /// Dart's `_handleContextMenuOnParentScroll`.
        private void HandleContextMenuOnParentScroll(ScrollNotification notification)
        {
            switch (notification)
            {
                case ScrollStartNotification when _dataWhenToolbarShowScheduled is not null:
                case ScrollEndNotification when _dataWhenToolbarShowScheduled is null:
                    break;
                case ScrollEndNotification when !_dataWhenToolbarShowScheduled!.Value.Value.Equals(
                    TextEditingValue):
                    _dataWhenToolbarShowScheduled = null;
                    DisposeScrollNotificationObserver();
                    break;
                case ScrollStartNotification or ScrollEndNotification
                    when !IsInternalScrollableNotification(notification.Context)
                         && ScrollableNotificationIsFromSameSubtree(notification.Context):
                    HandleContextMenuOnScroll(notification);
                    break;
            }
        }

        /// Dart's `_isInternalScrollableNotification`: the field's own scrollable also notifies the
        /// ancestor observer.
        private bool IsInternalScrollableNotification(BuildContext? notificationContext)
        {
            ScrollableState? scrollableState = notificationContext?.FindAncestorStateOfType<ScrollableState>();
            return ReferenceEquals(_scrollableKey.CurrentContext, scrollableState?.Context);
        }

        /// Dart's `_scrollableNotificationIsFromSameSubtree`: whether the notifying scrollable is an
        /// ancestor of this field.
        private bool ScrollableNotificationIsFromSameSubtree(BuildContext? notificationContext)
        {
            if (notificationContext is null)
            {
                return false;
            }

            // The notification context is the Scrollable's gesture detector.
            ScrollableState? notificationScrollableState =
                notificationContext.FindAncestorStateOfType<ScrollableState>();
            if (notificationScrollableState is null)
            {
                return false;
            }

            BuildContext? currentContext = Context;
            while (currentContext is not null)
            {
                ScrollableState? scrollableState = currentContext.FindAncestorStateOfType<ScrollableState>();
                if (ReferenceEquals(scrollableState, notificationScrollableState))
                {
                    return true;
                }

                currentContext = scrollableState?.Context;
            }

            return false;
        }

        /// Dart's `_handleContextMenuOnScroll`: on Android and iOS the menu hides when a scroll starts
        /// and comes back when the scroll ends with the selection still visible and the value
        /// unchanged; elsewhere the overlay only follows the scroll.
        private void HandleContextMenuOnScroll(ScrollNotification notification)
        {
            if (WebContextMenuEnabled)
            {
                return;
            }

            if (!_platformSupportsFadeOnScroll)
            {
                _selectionOverlay?.UpdateForScroll();
                return;
            }

            if (notification is ScrollStartNotification)
            {
                if (_dataWhenToolbarShowScheduled is not null)
                {
                    return;
                }

                bool toolbarIsVisible = _selectionOverlay is { ToolbarIsVisible: true };
                if (!toolbarIsVisible || RenderEditable is not { HasSize: true } renderEditable)
                {
                    return;
                }

                TextEditingValue value = TextEditingValue;
                IReadOnlyList<TextBox> selectionBoxes = renderEditable.GetBoxesForSelection(value.Selection);
                Rect selectionBounds = value.Selection.IsCollapsed || selectionBoxes.Count == 0
                    ? renderEditable.GetLocalRectForCaret(value.Selection.Extent)
                    : selectionBoxes
                        .Select(box => box.ToRect())
                        .Aggregate(ExpandToInclude);
                _dataWhenToolbarShowScheduled = (value, selectionBounds);
                _selectionOverlay!.HideToolbar();
            }
            else if (notification is ScrollEndNotification)
            {
                if (_dataWhenToolbarShowScheduled is null)
                {
                    return;
                }

                if (!_dataWhenToolbarShowScheduled.Value.Value.Equals(TextEditingValue))
                {
                    _dataWhenToolbarShowScheduled = null;
                    DisposeScrollNotificationObserver();
                    return;
                }

                if (_showToolbarOnScreenScheduled)
                {
                    return;
                }

                _showToolbarOnScreenScheduled = true;
                switch (Scheduler.Phase)
                {
                    case SchedulerPhase.Idle:
                    case SchedulerPhase.PostFrameCallbacks:
                        Scheduler.ScheduleFrameCallback(ScheduleToolbar);
                        break;
                    case SchedulerPhase.TransientCallbacks:
                    case SchedulerPhase.MidFrameMicrotasks:
                    case SchedulerPhase.PersistentCallbacks:
                        Scheduler.AddPostFrameCallback(ScheduleToolbar, "EditableText.scheduleToolbar");
                        break;
                }
            }
        }

        /// The `scheduleToolbar` closure of Dart's `_handleContextMenuOnScroll`.
        private void ScheduleToolbar(TimeSpan timeStamp)
        {
            _showToolbarOnScreenScheduled = false;
            if (!Mounted || _dataWhenToolbarShowScheduled is not { } data)
            {
                return;
            }

            if (!data.Value.Equals(TextEditingValue))
            {
                _dataWhenToolbarShowScheduled = null;
                DisposeScrollNotificationObserver();
                return;
            }

            if (RenderEditable is not { HasSize: true, Attached: true } renderEditable)
            {
                return;
            }

            Rect deviceRect = CalculateDeviceRect();
            bool selectionVisibleInEditable = renderEditable.SelectionStartInViewport.Value
                                              || renderEditable.SelectionEndInViewport.Value;
            Rect selectionBounds = MatrixUtils.TransformRect(
                renderEditable.GetTransformTo(null),
                data.SelectionBounds);
            bool selectionOverlapsWithDeviceRect = !HasNaN(selectionBounds)
                                                   && Overlaps(deviceRect, selectionBounds);
            if (selectionVisibleInEditable
                && selectionOverlapsWithDeviceRect
                && SelectionInViewport(renderEditable, data.SelectionBounds))
            {
                ShowToolbar();
                _dataWhenToolbarShowScheduled = null;
            }
        }

        /// Dart's `_calculateDeviceRect`: the screen minus the view's padding and bottom inset.
        private Rect CalculateDeviceRect()
        {
            Size screenSize = MediaQuery.SizeOf(Context);
            FlutterView view = View.Of(Context);
            double devicePixelRatio = view.DevicePixelRatio;
            double obscuredVertical =
                (view.Padding.Top + view.Padding.Bottom + view.ViewInsets.Bottom) / devicePixelRatio;
            double obscuredHorizontal = (view.Padding.Left + view.Padding.Right) / devicePixelRatio;
            return new Rect(
                view.Padding.Left / devicePixelRatio,
                view.Padding.Top / devicePixelRatio,
                screenSize.Width - obscuredHorizontal,
                screenSize.Height - obscuredVertical);
        }

        /// Dart's `_selectionInViewport`: the selection overlaps the paint bounds of every enclosing
        /// viewport.
        private static bool SelectionInViewport(RenderEditable renderEditable, Rect selectionBounds)
        {
            IRenderAbstractViewport? closest = RenderAbstractViewport.MaybeOf(renderEditable);
            while (closest is RenderObject closestObject)
            {
                Rect local = MatrixUtils.TransformRect(renderEditable.GetTransformTo(closestObject), selectionBounds);
                if (HasNaN(local)
                    || HasNaN(closestObject.PaintBounds)
                    || !Overlaps(closestObject.PaintBounds, local))
                {
                    return false;
                }

                closest = RenderAbstractViewport.MaybeOf(closestObject.Parent);
            }

            return true;
        }

        private static bool HasNaN(Rect rect) =>
            double.IsNaN(rect.X) || double.IsNaN(rect.Y) || double.IsNaN(rect.Width) || double.IsNaN(rect.Height);

        /// Dart's `Rect.overlaps`: edges that only touch do not overlap.
        private static bool Overlaps(Rect a, Rect b) =>
            a.Right > b.Left && b.Right > a.Left && a.Bottom > b.Top && b.Bottom > a.Top;

        /// Dart's `Rect.expandToInclude`.
        private static Rect ExpandToInclude(Rect a, Rect b)
        {
            double left = Math.Min(a.Left, b.Left);
            double top = Math.Min(a.Top, b.Top);
            return new Rect(left, top, Math.Max(a.Right, b.Right) - left, Math.Max(a.Bottom, b.Bottom) - top);
        }
    }
}

/// Dart's private `_NeverUserScrollableScrollPhysics`: a single-line field on iOS cannot be dragged,
/// but still scrolls implicitly to reveal the caret.
internal sealed class NeverUserScrollableScrollPhysics : ScrollPhysics
{
    public NeverUserScrollableScrollPhysics(ScrollPhysics? parent = null) : base(parent)
    {
    }

    public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor) =>
        new NeverUserScrollableScrollPhysics(BuildParent(ancestor));

    public override bool AllowUserScrolling => false;
}

/// Dart's private `_CompositionCallback`: registers a composition callback on the layer the field
/// paints into while <see cref="Enabled"/>, so the input connection hears about size and transform
/// changes that happen at composition time.
internal sealed class EditableTextCompositionCallback : SingleChildRenderObjectWidget
{
    public EditableTextCompositionCallback(
        CompositionCallback compositeCallback,
        bool enabled,
        Widget? child = null)
        : base(child)
    {
        CompositeCallback = compositeCallback;
        Enabled = enabled;
    }

    public CompositionCallback CompositeCallback { get; }

    public bool Enabled { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderEditableTextCompositionCallback(CompositeCallback, Enabled);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        base.UpdateRenderObject(context, renderObject);
        var render = (RenderEditableTextCompositionCallback)renderObject;
        // EditableTextState always uses the same callback.
        Debug.Assert(render.CompositeCallback == CompositeCallback);
        render.Enabled = Enabled;
    }
}

/// Dart's private `_RenderCompositionCallback`.
internal sealed class RenderEditableTextCompositionCallback(CompositionCallback compositeCallback, bool enabled)
    : RenderProxyBox
{
    private Action? _cancelCallback;
    private bool _enabled = enabled;

    public CompositionCallback CompositeCallback { get; } = compositeCallback;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!value)
            {
                _cancelCallback?.Invoke();
                _cancelCallback = null;
            }
            else if (_cancelCallback is null)
            {
                MarkNeedsPaint();
            }
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Enabled)
        {
            _cancelCallback ??= context.AddCompositionCallback(CompositeCallback);
        }

        base.Paint(context, offset);
    }
}
