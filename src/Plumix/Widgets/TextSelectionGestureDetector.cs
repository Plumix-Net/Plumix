using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/text_selection.dart

namespace Plumix.Widgets;

/// <summary>
/// Delegate interface for the <see cref="TextSelectionGestureDetectorBuilder"/>: the text field
/// implements it to expose the <see cref="EditableText"/> it wraps and its selection policy.
/// </summary>
public interface ITextSelectionGestureDetectorBuilderDelegate
{
    /// <summary>The key of the <see cref="EditableText"/> the gestures act on.</summary>
    GlobalKey<EditableText.EditableTextState> EditableTextKey { get; }

    /// <summary>Whether the text field should respond to force presses.</summary>
    bool ForcePressEnabled { get; }

    /// <summary>Whether the user may change the text selection.</summary>
    bool SelectionEnabled { get; }
}

/// <summary>
/// Builds a <see cref="TextSelectionGestureDetector"/> that wraps an <see cref="EditableText"/>
/// and translates taps, long presses, double and triple taps, drags, force presses and secondary
/// taps into the platform's selection behavior. Subclasses override the <c>On*</c> handlers to
/// customize it.
/// </summary>
public class TextSelectionGestureDetectorBuilder
{
    // Shows the magnifier on supported platforms at the given offset, currently only Android and
    // iOS.
    private bool _isShiftPressed;
    private double _dragStartScrollOffset;
    private double _dragStartViewportOffset;
    private TextSelection? _dragStartSelection;

    // For a shift + tap + drag gesture, the TextSelection at the point of the tap. Mac uses this
    // value to reset to the original selection when an inversion of the base and offset happens.
    private bool _longPressStartedWithoutFocus;

    public TextSelectionGestureDetectorBuilder(ITextSelectionGestureDetectorBuilderDelegate @delegate)
    {
        Delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
    }

    /// <summary>The delegate for this builder.</summary>
    protected ITextSelectionGestureDetectorBuilderDelegate Delegate { get; }

    /// <summary>Whether to show the selection toolbar. Set by the gesture handlers from the pointer
    /// kind: touch and stylus show it, a mouse does not.</summary>
    public bool ShouldShowSelectionToolbar { get; private set; } = true;

    /// <summary>Whether to show the selection handles, set the same way as
    /// <see cref="ShouldShowSelectionToolbar"/>.</summary>
    public bool ShouldShowSelectionHandles { get; private set; } = true;

    /// <summary>The <see cref="EditableText.EditableTextState"/> of the delegate's key.</summary>
    protected EditableText.EditableTextState EditableText => Delegate.EditableTextKey.CurrentState!;

    /// <summary>The <see cref="RenderEditable"/> of <see cref="EditableText"/>.</summary>
    protected RenderEditable RenderEditable => EditableText.RenderEditableObject;

    /// <summary>Whether <see cref="OnUserTap"/> runs for every tap in a series, not just the first.
    /// </summary>
    protected virtual bool OnUserTapAlwaysCalled => false;

    private void ShowMagnifierIfSupportedByPlatform(Point positionToShow)
    {
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.IOS:
                EditableText.ShowMagnifier(positionToShow);
                break;
        }
    }

    // Hides the magnifier on supported platforms, currently only Android and iOS.
    private void HideMagnifierIfSupportedByPlatform()
    {
        if (!IsEditableTextMounted)
        {
            return;
        }

        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.IOS:
                EditableText.HideMagnifier();
                break;
        }
    }

    /// Returns true if lastSecondaryTapDownPosition was on selection.
    private bool LastSecondaryTapWasOnSelection
    {
        get
        {
            if (RenderEditable.Selection is not { } selection)
            {
                return false;
            }

            TextPosition textPosition = RenderEditable.GetPositionForPoint(
                RenderEditable.LastSecondaryTapDownPosition!.Value);
            return selection.Start <= textPosition.Offset && selection.End >= textPosition.Offset;
        }
    }

    private bool PositionWasOnSelectionExclusive(TextPosition textPosition) =>
        RenderEditable.Selection is { } selection
        && selection.Start < textPosition.Offset
        && selection.End > textPosition.Offset;

    private bool PositionWasOnSelectionInclusive(TextPosition textPosition) =>
        RenderEditable.Selection is { } selection
        && selection.Start <= textPosition.Offset
        && selection.End >= textPosition.Offset;

    // Expand the selection to the given global position.
    //
    // Either base or extent will be moved to the last tapped position, whichever is closest. The
    // selection will never shrink or pivot, only grow.
    //
    // If fromSelection is given, will expand from that selection instead of the current selection
    // in renderEditable.
    private void ExpandSelection(Point offset, SelectionChangedCause cause, TextSelection? fromSelection = null)
    {
        TextPosition tappedPosition = RenderEditable.GetPositionForPoint(offset);
        TextSelection selection = fromSelection ?? RenderEditable.Selection!.Value;
        bool baseIsCloser = Math.Abs(tappedPosition.Offset - selection.BaseOffset)
                            < Math.Abs(tappedPosition.Offset - selection.ExtentOffset);
        TextSelection nextSelection = selection with
        {
            BaseOffset = baseIsCloser ? selection.ExtentOffset : selection.BaseOffset,
            ExtentOffset = tappedPosition.Offset,
        };

        EditableText.UserUpdateTextEditingValue(
            EditableText.TextEditingValue.CopyWith(selection: nextSelection),
            cause);
    }

    // Extend the selection to the given global position.
    //
    // Holds the base in place and moves the extent.
    private void ExtendSelection(Point offset, SelectionChangedCause cause)
    {
        TextPosition tappedPosition = RenderEditable.GetPositionForPoint(offset);
        TextSelection selection = RenderEditable.Selection!.Value;
        TextSelection nextSelection = selection with { ExtentOffset = tappedPosition.Offset };

        EditableText.UserUpdateTextEditingValue(
            EditableText.TextEditingValue.CopyWith(selection: nextSelection),
            cause);
    }

    private bool IsEditableTextMounted => Delegate.EditableTextKey.CurrentContext?.Mounted ?? false;

    // The current scroll offset of the text field's closest ancestor scrollable, or 0.
    private double ScrollPosition
    {
        get
        {
            BuildContext? context = Delegate.EditableTextKey.CurrentContext;
            ScrollableState? scrollableState = context is null ? null : Scrollable.MaybeOf(context);
            return scrollableState?.Position.Pixels ?? 0.0;
        }
    }

    private AxisDirection? ScrollDirection
    {
        get
        {
            BuildContext? context = Delegate.EditableTextKey.CurrentContext;
            ScrollableState? scrollableState = context is null ? null : Scrollable.MaybeOf(context);
            return scrollableState?.AxisDirection;
        }
    }

    /// <summary>Handler for the start of a tap series: records whether shift is held.</summary>
    protected virtual void OnTapTrackStart()
    {
        _isShiftPressed = HardwareKeyboard.Instance.LogicalKeysPressed
            .Any(key => key.Equals(LogicalKeyboardKey.ShiftLeft) || key.Equals(LogicalKeyboardKey.ShiftRight));
    }

    /// <summary>Handler for the reset of a tap series.</summary>
    protected virtual void OnTapTrackReset() => _isShiftPressed = false;

    /// <summary>Handler for <see cref="TextSelectionGestureDetector.OnTapDown"/>.</summary>
    protected virtual void OnTapDown(TapDragDownDetails details)
    {
        if (!Delegate.SelectionEnabled)
        {
            return;
        }

        // TODO(Renzo-Olivares): Migrate text selection gestures away from saving state in
        // renderEditable. The gesture callbacks can use the details objects directly in callbacks
        // variants, and are only saved in renderEditable to be accessible in the other callbacks.
        RenderEditable.HandleTapDown(new TapDownDetails(globalPosition: details.GlobalPosition));
        // The selection overlay should only be shown when the user is interacting through a touch
        // screen (via either a finger or a stylus). A mouse shouldn't trigger the selection overlay.
        // For backwards-compatibility, we treat a null kind the same as touch.
        PointerDeviceKind? kind = details.Kind;
        // TODO(justinmc): Should a desktop platform show its selection toolbar when receiving a
        // tap event?  Say a Windows device with a touchscreen.
        // https://github.com/flutter/flutter/issues/106586
        ShouldShowSelectionToolbar = kind is null or PointerDeviceKind.Touch or PointerDeviceKind.Stylus;
        ShouldShowSelectionHandles = ShouldShowSelectionToolbar;

        // It is impossible to extend the selection when the shift key is pressed, if the
        // renderEditable.selection is invalid.
        bool isShiftPressedValid = _isShiftPressed && RenderEditable.Selection is not null;
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
                if (EditableText.Widget.StylusHandwritingEnabled)
                {
                    bool stylusEnabled = kind switch
                    {
                        PointerDeviceKind.Stylus or PointerDeviceKind.InvertedStylus =>
                            EditableText.Widget.StylusHandwritingEnabled,
                        _ => false,
                    };
                    if (stylusEnabled)
                    {
                        // Dart attaches no error handler to this future either.
                        Scheduler.RunAsync(async () =>
                        {
                            bool isAvailable = await Scribe.IsFeatureAvailable();
                            if (isAvailable)
                            {
                                RenderEditable.SelectPosition(SelectionChangedCause.StylusHandwriting);
                                _ = Scribe.StartStylusHandwriting();
                            }
                        });
                    }
                }

                // On mobile platforms the selection is set on tap up.
                break;
            case TargetPlatform.Fuchsia:
            case TargetPlatform.IOS:
                // On mobile platforms the selection is set on tap up.
                break;
            case TargetPlatform.MacOS:
                EditableText.HideToolbar();
                // On macOS, a shift-tapped unfocused field expands from 0, not from the previous
                // selection.
                if (isShiftPressedValid)
                {
                    TextSelection? fromSelection = RenderEditable.HasFocus ? null : TextSelection.Collapsed(0);
                    ExpandSelection(details.GlobalPosition, SelectionChangedCause.Tap, fromSelection);
                    return;
                }

                // On macOS, a tap/click places the selection in a precise position.
                RenderEditable.SelectPosition(cause: SelectionChangedCause.Tap);
                break;
            case TargetPlatform.Linux:
            case TargetPlatform.Windows:
                EditableText.HideToolbar();
                if (isShiftPressedValid)
                {
                    ExtendSelection(details.GlobalPosition, SelectionChangedCause.Tap);
                    return;
                }

                RenderEditable.SelectPosition(cause: SelectionChangedCause.Tap);
                break;
        }
    }

    /// <summary>Handler for the start of a force press.</summary>
    protected virtual void OnForcePressStart(ForcePressDetails details)
    {
        ShouldShowSelectionToolbar = true;
        if (!Delegate.SelectionEnabled)
        {
            return;
        }

        RenderEditable.SelectWordsInRange(from: details.GlobalPosition, cause: SelectionChangedCause.ForcePress);
        _ = EditableText.ShowToolbar();
    }

    /// <summary>Handler for the end of a force press.</summary>
    protected virtual void OnForcePressEnd(ForcePressDetails details)
    {
        RenderEditable.SelectWordsInRange(from: details.GlobalPosition, cause: SelectionChangedCause.ForcePress);
        if (ShouldShowSelectionToolbar)
        {
            _ = EditableText.ShowToolbar();
        }
    }

    /// <summary>Handler for a user tap; empty by default.</summary>
    protected virtual void OnUserTap()
    {
    }

    /// <summary>Handler for the tap up of the first tap in a series.</summary>
    protected virtual void OnSingleTapUp(TapDragUpDetails details)
    {
        if (!Delegate.SelectionEnabled)
        {
            EditableText.RequestKeyboard();
            return;
        }

        // It is impossible to extend the selection when the shift key is pressed, if the
        // renderEditable.selection is invalid.
        bool isShiftPressedValid = _isShiftPressed && RenderEditable.Selection is not null;
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Linux:
            case TargetPlatform.MacOS:
            case TargetPlatform.Windows:
                break;
            // On desktop platforms the selection is set on tap down.
            case TargetPlatform.Android:
                EditableText.HideToolbar(hideHandles: false);
                if (isShiftPressedValid)
                {
                    ExtendSelection(details.GlobalPosition, SelectionChangedCause.Tap);
                    return;
                }

                RenderEditable.SelectPosition(cause: SelectionChangedCause.Tap);
                _ = EditableText.ShowSpellCheckSuggestionsToolbar();
                break;
            case TargetPlatform.Fuchsia:
                EditableText.HideToolbar(hideHandles: false);
                if (isShiftPressedValid)
                {
                    ExtendSelection(details.GlobalPosition, SelectionChangedCause.Tap);
                    return;
                }

                RenderEditable.SelectPosition(cause: SelectionChangedCause.Tap);
                break;
            case TargetPlatform.IOS:
                if (isShiftPressedValid)
                {
                    // On iOS, a shift-tapped unfocused field expands from 0, not from the previous
                    // selection.
                    TextSelection? fromSelection = RenderEditable.HasFocus ? null : TextSelection.Collapsed(0);
                    ExpandSelection(details.GlobalPosition, SelectionChangedCause.Tap, fromSelection);
                    return;
                }

                switch (details.Kind)
                {
                    case PointerDeviceKind.Mouse:
                    case PointerDeviceKind.Trackpad:
                    case PointerDeviceKind.Stylus:
                    case PointerDeviceKind.InvertedStylus:
                        // TODO(camsim99): Determine spell check toolbar behavior in these cases:
                        // https://github.com/flutter/flutter/issues/119573.
                        // Precise devices should place the cursor at a precise position if the word
                        // at the text position is not misspelled.
                        RenderEditable.SelectPosition(cause: SelectionChangedCause.Tap);
                        EditableText.HideToolbar();
                        break;
                    case PointerDeviceKind.Touch:
                    case PointerDeviceKind.Unknown:
                        OnSingleTapUpIOSTouch(details);
                        break;
                }

                break;
        }

        EditableText.RequestKeyboard();
    }

    private void OnSingleTapUpIOSTouch(TapDragUpDetails details)
    {
        // If the word that was tapped is misspelled, select the word and show the spell check
        // suggestions toolbar once. If additional taps are made on a misspelled word, toggle the
        // toolbar. If the word is not misspelled, default to the following behavior:
        //
        // Toggle the toolbar when the tap is exclusively within the bounds of a non-collapsed
        // previous selection, or when the tap is within the bounds of a collapsed previous selection
        // and the editable is focused.
        //
        // Toggle the toolbar if the `previousSelection` is collapsed, the tap is on the selection,
        // the TextAffinity remains the same, the editable field is not read only, and the editable
        // is focused. The TextAffinity is important when the cursor is on the boundary of a line
        // wrap, if the affinity is different (i.e. it is downstream), the selection should move to
        // the following line and not toggle the toolbar.
        //
        // Selects the word edge closest to the tap when the editable is not focused, or if the tap
        // was neither exclusively or inclusively on `previousSelection`. If the selection remains
        // the same after selecting the word edge, then we toggle the toolbar, if the editable field
        // is not read only. If the selection changes then we hide the toolbar.
        TextSelection previousSelection = RenderEditable.Selection ?? EditableText.TextEditingValue.Selection;
        TextPosition textPosition = RenderEditable.GetPositionForPoint(details.GlobalPosition);
        bool isAffinityTheSame = textPosition.Affinity == previousSelection.Affinity;
        bool wordAtCursorIndexIsMisspelled =
            EditableText.FindSuggestionSpanAtCursorIndex(textPosition.Offset) is not null;

        if (wordAtCursorIndexIsMisspelled)
        {
            RenderEditable.SelectWord(cause: SelectionChangedCause.Tap);
            if (!previousSelection.Equals(EditableText.TextEditingValue.Selection))
            {
                _ = EditableText.ShowSpellCheckSuggestionsToolbar();
            }
            else
            {
                EditableText.ToggleToolbar(hideHandles: false);
            }
        }
        else if (((PositionWasOnSelectionExclusive(textPosition) && !previousSelection.IsCollapsed)
                  || (PositionWasOnSelectionInclusive(textPosition)
                      && previousSelection.IsCollapsed
                      && isAffinityTheSame
                      && !RenderEditable.ReadOnly))
                 && RenderEditable.HasFocus)
        {
            EditableText.ToggleToolbar(hideHandles: false);
        }
        else
        {
            RenderEditable.SelectWordEdge(cause: SelectionChangedCause.Tap);
            if (previousSelection.Equals(EditableText.TextEditingValue.Selection)
                && RenderEditable.HasFocus
                && !RenderEditable.ReadOnly)
            {
                EditableText.ToggleToolbar(hideHandles: false);
            }
            else
            {
                EditableText.HideToolbar(hideHandles: false);
            }
        }
    }

    /// <summary>Handler for a cancelled single tap; empty by default.</summary>
    protected virtual void OnSingleTapCancel()
    {
    }

    /// <summary>Handler for the start of a long press.</summary>
    protected virtual void OnSingleLongTapStart(LongPressStartDetails details)
    {
        if (!Delegate.SelectionEnabled)
        {
            return;
        }

        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.IOS:
            case TargetPlatform.MacOS:
                if (!RenderEditable.HasFocus)
                {
                    _longPressStartedWithoutFocus = true;
                    RenderEditable.SelectWord(cause: SelectionChangedCause.LongPress);
                }
                else if (RenderEditable.ReadOnly)
                {
                    RenderEditable.SelectWord(cause: SelectionChangedCause.LongPress);
                    if (EditableText.Context.Mounted)
                    {
                        _ = Feedback.ForLongPress(EditableText.Context);
                    }
                }
                else
                {
                    RenderEditable.SelectPositionAt(
                        from: details.GlobalPosition,
                        cause: SelectionChangedCause.LongPress);
                    // Show the floating cursor.
                    TextSelection selection = EditableText.TextEditingValue.Selection;
                    var cursorPoint = new RawFloatingCursorPoint(
                        state: FloatingCursorDragState.Start,
                        startLocation: (
                            RenderEditable.GlobalToLocal(details.GlobalPosition),
                            new TextPosition(selection.BaseOffset, selection.Affinity)),
                        offset: default(Point));
                    EditableText.UpdateFloatingCursor(cursorPoint);
                }

                break;
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.Linux:
            case TargetPlatform.Windows:
                RenderEditable.SelectWord(cause: SelectionChangedCause.LongPress);
                if (EditableText.Context.Mounted)
                {
                    _ = Feedback.ForLongPress(EditableText.Context);
                }

                break;
        }

        ShowMagnifierIfSupportedByPlatform(details.GlobalPosition);

        _dragStartViewportOffset = RenderEditable.Offset.Pixels;
        _dragStartScrollOffset = ScrollPosition;
    }

    private Point EditableOffsetFromDragStart() =>
        RenderEditable.MaxLines == 1
            ? new Point(RenderEditable.Offset.Pixels - _dragStartViewportOffset, 0.0)
            : new Point(0.0, RenderEditable.Offset.Pixels - _dragStartViewportOffset);

    private Point ScrollableOffsetFromDragStart() =>
        BasicTypes.AxisDirectionToAxis(ScrollDirection ?? AxisDirection.Left) switch
        {
            Axis.Horizontal => new Point(ScrollPosition - _dragStartScrollOffset, 0.0),
            _ => new Point(0.0, ScrollPosition - _dragStartScrollOffset),
        };

    /// <summary>Handler for a move during a long press.</summary>
    protected virtual void OnSingleLongTapMoveUpdate(LongPressMoveUpdateDetails details)
    {
        if (!Delegate.SelectionEnabled)
        {
            return;
        }

        // Adjust the drag start offset for possible viewport offset changes.
        Point editableOffset = EditableOffsetFromDragStart();
        Point scrollableOffset = ScrollableOffsetFromDragStart();

        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.IOS:
            case TargetPlatform.MacOS:
                if (_longPressStartedWithoutFocus || RenderEditable.ReadOnly)
                {
                    RenderEditable.SelectWordsInRange(
                        from: details.GlobalPosition - details.OffsetFromOrigin - editableOffset - scrollableOffset,
                        to: details.GlobalPosition,
                        cause: SelectionChangedCause.LongPress);
                }
                else
                {
                    RenderEditable.SelectPositionAt(
                        from: details.GlobalPosition,
                        cause: SelectionChangedCause.LongPress);
                    // Update the floating cursor.
                    EditableText.UpdateFloatingCursor(new RawFloatingCursorPoint(
                        state: FloatingCursorDragState.Update,
                        offset: details.OffsetFromOrigin));
                }

                break;
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.Linux:
            case TargetPlatform.Windows:
                RenderEditable.SelectWordsInRange(
                    from: details.GlobalPosition - details.OffsetFromOrigin - editableOffset - scrollableOffset,
                    to: details.GlobalPosition,
                    cause: SelectionChangedCause.LongPress);
                break;
        }

        ShowMagnifierIfSupportedByPlatform(details.GlobalPosition);
    }

    /// <summary>Handler for the end of a long press.</summary>
    protected virtual void OnSingleLongTapEnd(LongPressEndDetails details)
    {
        OnSingleLongTapEndOrCancel();
        if (ShouldShowSelectionToolbar)
        {
            _ = EditableText.ShowToolbar();
        }
    }

    /// <summary>Handler for a cancelled long press.</summary>
    protected virtual void OnSingleLongTapCancel() => OnSingleLongTapEndOrCancel();

    /// <summary>Handler for a secondary tap.</summary>
    protected virtual void OnSecondaryTap()
    {
        if (!Delegate.SelectionEnabled)
        {
            return;
        }

        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.IOS:
            case TargetPlatform.MacOS:
                if (!LastSecondaryTapWasOnSelection || !RenderEditable.HasFocus)
                {
                    RenderEditable.SelectWord(cause: SelectionChangedCause.Tap);
                }

                if (ShouldShowSelectionToolbar)
                {
                    EditableText.HideToolbar();
                    _ = EditableText.ShowToolbar();
                }

                break;
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.Linux:
            case TargetPlatform.Windows:
                if (!RenderEditable.HasFocus)
                {
                    RenderEditable.SelectPosition(cause: SelectionChangedCause.Tap);
                }

                EditableText.ToggleToolbar();
                break;
        }
    }

    /// <summary>Handler for a secondary tap down.</summary>
    protected virtual void OnSecondaryTapDown(TapDownDetails details)
    {
        // TODO(Renzo-Olivares): Migrate text selection gestures away from saving state in
        // renderEditable. The gesture callbacks can use the details objects directly in callbacks
        // variants, and are only saved in renderEditable to be accessible in the other callbacks.
        RenderEditable.HandleSecondaryTapDown(new TapDownDetails(globalPosition: details.GlobalPosition));
        ShouldShowSelectionToolbar = true;
        ShouldShowSelectionHandles = details.Kind is null or PointerDeviceKind.Touch or PointerDeviceKind.Stylus;
    }

    /// <summary>Handler for the tap down of the second tap in a series.</summary>
    protected virtual void OnDoubleTapDown(TapDragDownDetails details)
    {
        if (Delegate.SelectionEnabled)
        {
            RenderEditable.SelectWord(cause: SelectionChangedCause.DoubleTap);
            if (ShouldShowSelectionToolbar)
            {
                _ = EditableText.ShowToolbar();
            }
        }
    }

    private void OnSingleLongTapEndOrCancel()
    {
        HideMagnifierIfSupportedByPlatform();
        _longPressStartedWithoutFocus = false;
        _dragStartViewportOffset = 0.0;
        _dragStartScrollOffset = 0.0;
        if (IsEditableTextMounted
            && PlatformDefaults.TargetPlatform == TargetPlatform.IOS
            && Delegate.SelectionEnabled
            && EditableText.TextEditingValue.Selection.IsCollapsed)
        {
            // Update the floating cursor.
            EditableText.UpdateFloatingCursor(new RawFloatingCursorPoint(state: FloatingCursorDragState.End));
        }
    }

    // Selects the set of paragraphs in a document that intersect a given range of global
    // positions.
    private void SelectParagraphsInRange(Point from, Point? to = null, SelectionChangedCause? cause = null)
    {
        TextBoundary paragraphBoundary = new ParagraphBoundary(EditableText.TextEditingValue.Text);
        SelectTextBoundariesInRange(boundary: paragraphBoundary, from: from, to: to, cause: cause);
    }

    // Selects the set of lines in a document that intersect a given range of global positions.
    private void SelectLinesInRange(Point from, Point? to = null, SelectionChangedCause? cause = null)
    {
        TextBoundary lineBoundary = new LineBoundary(RenderEditable);
        SelectTextBoundariesInRange(boundary: lineBoundary, from: from, to: to, cause: cause);
    }

    // Returns the location of a text boundary at `extent`. When `extent` is at the end of the text,
    // returns the previous text boundary's location.
    private TextRange MoveToTextBoundary(TextPosition extent, TextBoundary textBoundary)
    {
        // Use the character before the caret when the caret is at the end of the text.
        string text = EditableText.TextEditingValue.Text;
        int start = textBoundary.GetLeadingTextBoundaryAt(
            extent.Offset == text.Length ? extent.Offset - 1 : extent.Offset) ?? 0;
        int end = textBoundary.GetTrailingTextBoundaryAt(extent.Offset) ?? text.Length;
        return new TextRange(start, end);
    }

    // Selects the set of text boundaries in a document that intersect a given range of global
    // positions.
    //
    // The set of text boundaries selected are not strictly bounded by the range of global
    // positions.
    //
    // The first and last endpoints of the selection will always be at the beginning and end of a
    // text boundary respectively.
    private void SelectTextBoundariesInRange(
        TextBoundary boundary,
        Point from,
        Point? to = null,
        SelectionChangedCause? cause = null)
    {
        TextPosition fromPosition = RenderEditable.GetPositionForPoint(from);
        TextRange fromRange = MoveToTextBoundary(fromPosition, boundary);
        TextPosition toPosition = to is null ? fromPosition : RenderEditable.GetPositionForPoint(to.Value);
        TextRange toRange = toPosition.Equals(fromPosition) ? fromRange : MoveToTextBoundary(toPosition, boundary);
        bool isFromBoundaryBeforeToBoundary = fromRange.Start < toRange.End;

        TextSelection newSelection = isFromBoundaryBeforeToBoundary
            ? new TextSelection(fromRange.Start, toRange.End)
            : new TextSelection(fromRange.End, toRange.Start);

        EditableText.UserUpdateTextEditingValue(
            EditableText.TextEditingValue.CopyWith(selection: newSelection),
            cause);
    }

    /// <summary>Handler for the tap down of the third tap in a series.</summary>
    protected virtual void OnTripleTapDown(TapDragDownDetails details)
    {
        if (!Delegate.SelectionEnabled)
        {
            return;
        }

        if (RenderEditable.MaxLines == 1)
        {
            EditableText.SelectAll(SelectionChangedCause.Tap);
        }
        else
        {
            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.IOS:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    SelectParagraphsInRange(from: details.GlobalPosition, cause: SelectionChangedCause.Tap);
                    break;
                case TargetPlatform.Linux:
                    SelectLinesInRange(from: details.GlobalPosition, cause: SelectionChangedCause.Tap);
                    break;
            }
        }

        if (ShouldShowSelectionToolbar)
        {
            _ = EditableText.ShowToolbar();
        }
    }

    /// <summary>Handler for the start of a drag.</summary>
    protected virtual void OnDragSelectionStart(TapDragStartDetails details)
    {
        if (!Delegate.SelectionEnabled)
        {
            return;
        }

        PointerDeviceKind? kind = details.Kind;
        ShouldShowSelectionToolbar = kind is null or PointerDeviceKind.Touch or PointerDeviceKind.Stylus;
        ShouldShowSelectionHandles = ShouldShowSelectionToolbar;

        _dragStartSelection = RenderEditable.Selection;
        _dragStartScrollOffset = ScrollPosition;
        _dragStartViewportOffset = RenderEditable.Offset.Pixels;

        if (TextSelectionGestureDetectorState.GetEffectiveConsecutiveTapCount(details.ConsecutiveTapCount) > 1)
        {
            // Do not set the selection on a consecutive tap and drag.
            return;
        }

        if (_isShiftPressed && RenderEditable.Selection is { IsValid: true })
        {
            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.IOS:
                case TargetPlatform.MacOS:
                    ExpandSelection(details.GlobalPosition, SelectionChangedCause.Drag);
                    break;
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.Linux:
                case TargetPlatform.Windows:
                    ExtendSelection(details.GlobalPosition, SelectionChangedCause.Drag);
                    break;
            }
        }
        else
        {
            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.IOS:
                    switch (details.Kind)
                    {
                        case PointerDeviceKind.Mouse:
                        case PointerDeviceKind.Trackpad:
                            RenderEditable.SelectPositionAt(
                                from: details.GlobalPosition,
                                cause: SelectionChangedCause.Drag);
                            break;
                        default:
                            // For iOS platforms, a touch drag does not initiate unless the editable
                            // has focus and the drag began on the previous selection.
                            break;
                    }

                    break;
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                    switch (details.Kind)
                    {
                        case PointerDeviceKind.Mouse:
                        case PointerDeviceKind.Trackpad:
                            RenderEditable.SelectPositionAt(
                                from: details.GlobalPosition,
                                cause: SelectionChangedCause.Drag);
                            break;
                        case PointerDeviceKind.Stylus:
                        case PointerDeviceKind.InvertedStylus:
                        case PointerDeviceKind.Touch:
                        case PointerDeviceKind.Unknown:
                            // For Android, Fuchsia, and iOS platforms, a touch drag does not
                            // initiate unless the editable has focus.
                            if (RenderEditable.HasFocus)
                            {
                                RenderEditable.SelectPositionAt(
                                    from: details.GlobalPosition,
                                    cause: SelectionChangedCause.Drag);
                                ShowMagnifierIfSupportedByPlatform(details.GlobalPosition);
                            }

                            break;
                    }

                    break;
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    RenderEditable.SelectPositionAt(from: details.GlobalPosition, cause: SelectionChangedCause.Drag);
                    break;
            }
        }
    }

    /// <summary>Handler for a drag update.</summary>
    protected virtual void OnDragSelectionUpdate(TapDragUpdateDetails details)
    {
        if (!Delegate.SelectionEnabled)
        {
            return;
        }

        if (!_isShiftPressed)
        {
            // Adjust the drag start offset for possible viewport offset changes.
            Point editableOffset = EditableOffsetFromDragStart();
            Point scrollableOffset = ScrollableOffsetFromDragStart();
            Point dragStartGlobalPosition = details.GlobalPosition - details.OffsetFromOrigin;
            Point origin = dragStartGlobalPosition - editableOffset - scrollableOffset;
            int effectiveCount =
                TextSelectionGestureDetectorState.GetEffectiveConsecutiveTapCount(details.ConsecutiveTapCount);

            // Select word by word.
            if (effectiveCount == 2)
            {
                RenderEditable.SelectWordsInRange(
                    from: origin,
                    to: details.GlobalPosition,
                    cause: SelectionChangedCause.Drag);

                switch (details.Kind)
                {
                    case PointerDeviceKind.Stylus:
                    case PointerDeviceKind.InvertedStylus:
                    case PointerDeviceKind.Touch:
                    case PointerDeviceKind.Unknown:
                        ShowMagnifierIfSupportedByPlatform(details.GlobalPosition);
                        return;
                    default:
                        return;
                }
            }

            // Select paragraph-by-paragraph.
            if (effectiveCount == 3)
            {
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                    case TargetPlatform.IOS:
                        switch (details.Kind)
                        {
                            case PointerDeviceKind.Mouse:
                            case PointerDeviceKind.Trackpad:
                                SelectParagraphsInRange(
                                    from: origin,
                                    to: details.GlobalPosition,
                                    cause: SelectionChangedCause.Drag);
                                return;
                            default:
                                // Triple tap to drag is not present on these platforms when using
                                // non-precise pointer devices at the moment.
                                break;
                        }

                        return;
                    case TargetPlatform.Linux:
                        SelectLinesInRange(from: origin, to: details.GlobalPosition, cause: SelectionChangedCause.Drag);
                        return;
                    case TargetPlatform.Windows:
                    case TargetPlatform.MacOS:
                        SelectParagraphsInRange(
                            from: origin,
                            to: details.GlobalPosition,
                            cause: SelectionChangedCause.Drag);
                        return;
                }
            }

            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.IOS:
                    // With a mouse device, a drag should select the range from the origin of the
                    // drag to the current position of the drag.
                    //
                    // With a touch device, nothing should happen.
                    switch (details.Kind)
                    {
                        case PointerDeviceKind.Mouse:
                        case PointerDeviceKind.Trackpad:
                            RenderEditable.SelectPositionAt(
                                from: origin,
                                to: details.GlobalPosition,
                                cause: SelectionChangedCause.Drag);
                            return;
                        default:
                            break;
                    }

                    return;
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                    // With a precise pointer device, such as a mouse, trackpad, or stylus, the drag
                    // will select the text spanning the origin of the drag to the end of the drag.
                    // With a touch device, the cursor should move with the drag.
                    switch (details.Kind)
                    {
                        case PointerDeviceKind.Mouse:
                        case PointerDeviceKind.Trackpad:
                        case PointerDeviceKind.Stylus:
                        case PointerDeviceKind.InvertedStylus:
                            RenderEditable.SelectPositionAt(
                                from: origin,
                                to: details.GlobalPosition,
                                cause: SelectionChangedCause.Drag);
                            return;
                        case PointerDeviceKind.Touch:
                        case PointerDeviceKind.Unknown:
                            if (RenderEditable.HasFocus)
                            {
                                RenderEditable.SelectPositionAt(
                                    from: details.GlobalPosition,
                                    cause: SelectionChangedCause.Drag);
                                ShowMagnifierIfSupportedByPlatform(details.GlobalPosition);
                            }

                            return;
                        default:
                            break;
                    }

                    return;
                case TargetPlatform.MacOS:
                case TargetPlatform.Linux:
                case TargetPlatform.Windows:
                    RenderEditable.SelectPositionAt(
                        from: origin,
                        to: details.GlobalPosition,
                        cause: SelectionChangedCause.Drag);
                    return;
            }
        }

        if (_dragStartSelection!.Value.IsCollapsed
            || (PlatformDefaults.TargetPlatform != TargetPlatform.IOS
                && PlatformDefaults.TargetPlatform != TargetPlatform.MacOS))
        {
            ExtendSelection(details.GlobalPosition, SelectionChangedCause.Drag);
            return;
        }

        // If the drag inverts the selection, Mac and iOS revert to the initial selection.
        TextSelection selection = EditableText.TextEditingValue.Selection;
        TextSelection dragStartSelection = _dragStartSelection.Value;
        TextPosition nextExtent = RenderEditable.GetPositionForPoint(details.GlobalPosition);
        bool isShiftTapDragSelectionForward = dragStartSelection.BaseOffset < dragStartSelection.ExtentOffset;
        bool isInverted = isShiftTapDragSelectionForward
            ? nextExtent.Offset < dragStartSelection.BaseOffset
            : nextExtent.Offset > dragStartSelection.BaseOffset;
        if (isInverted && selection.BaseOffset == dragStartSelection.BaseOffset)
        {
            EditableText.UserUpdateTextEditingValue(
                EditableText.TextEditingValue.CopyWith(
                    selection: new TextSelection(dragStartSelection.ExtentOffset, nextExtent.Offset)),
                SelectionChangedCause.Drag);
        }
        else if (!isInverted
                 && nextExtent.Offset != dragStartSelection.BaseOffset
                 && selection.BaseOffset != dragStartSelection.BaseOffset)
        {
            EditableText.UserUpdateTextEditingValue(
                EditableText.TextEditingValue.CopyWith(
                    selection: new TextSelection(dragStartSelection.BaseOffset, nextExtent.Offset)),
                SelectionChangedCause.Drag);
        }
        else
        {
            ExtendSelection(details.GlobalPosition, SelectionChangedCause.Drag);
        }
    }

    /// <summary>Handler for the end of a drag.</summary>
    protected virtual void OnDragSelectionEnd(TapDragEndDetails details)
    {
        if (ShouldShowSelectionToolbar
            && TextSelectionGestureDetectorState.GetEffectiveConsecutiveTapCount(details.ConsecutiveTapCount) == 2)
        {
            _ = EditableText.ShowToolbar();
        }

        if (_isShiftPressed)
        {
            _dragStartSelection = null;
        }

        HideMagnifierIfSupportedByPlatform();
    }

    /// <summary>Returns a <see cref="TextSelectionGestureDetector"/> configured with the handlers
    /// provided by this builder.</summary>
    public Widget BuildGestureDetector(Widget child, HitTestBehavior? behavior = null, Key? key = null)
    {
        return new TextSelectionGestureDetector(
            key: key,
            onTapTrackStart: OnTapTrackStart,
            onTapTrackReset: OnTapTrackReset,
            onTapDown: OnTapDown,
            onForcePressStart: Delegate.ForcePressEnabled ? OnForcePressStart : null,
            onForcePressEnd: Delegate.ForcePressEnabled ? OnForcePressEnd : null,
            onSecondaryTap: OnSecondaryTap,
            onSecondaryTapDown: OnSecondaryTapDown,
            onSingleTapUp: OnSingleTapUp,
            onSingleTapCancel: OnSingleTapCancel,
            onUserTap: OnUserTap,
            onSingleLongTapStart: OnSingleLongTapStart,
            onSingleLongTapMoveUpdate: OnSingleLongTapMoveUpdate,
            onSingleLongTapEnd: OnSingleLongTapEnd,
            onSingleLongTapCancel: OnSingleLongTapCancel,
            onDoubleTapDown: OnDoubleTapDown,
            onTripleTapDown: OnTripleTapDown,
            onDragSelectionStart: OnDragSelectionStart,
            onDragSelectionUpdate: OnDragSelectionUpdate,
            onDragSelectionEnd: OnDragSelectionEnd,
            onUserTapAlwaysCalled: OnUserTapAlwaysCalled,
            behavior: behavior,
            child: child);
    }
}

/// <summary>
/// A gesture detector to respond to non-exclusive event chains for a text field: taps, tap series,
/// long presses, drags, force presses and secondary taps.
/// </summary>
public sealed class TextSelectionGestureDetector : StatefulWidget
{
    public TextSelectionGestureDetector(
        Widget child,
        Action? onTapTrackStart = null,
        Action? onTapTrackReset = null,
        Action<TapDragDownDetails>? onTapDown = null,
        Action<ForcePressDetails>? onForcePressStart = null,
        Action<ForcePressDetails>? onForcePressEnd = null,
        Action? onSecondaryTap = null,
        Action<TapDownDetails>? onSecondaryTapDown = null,
        Action<TapDragUpDetails>? onSingleTapUp = null,
        Action? onSingleTapCancel = null,
        Action? onUserTap = null,
        Action<LongPressStartDetails>? onSingleLongTapStart = null,
        Action<LongPressMoveUpdateDetails>? onSingleLongTapMoveUpdate = null,
        Action<LongPressEndDetails>? onSingleLongTapEnd = null,
        Action? onSingleLongTapCancel = null,
        Action<TapDragDownDetails>? onDoubleTapDown = null,
        Action<TapDragDownDetails>? onTripleTapDown = null,
        Action<TapDragStartDetails>? onDragSelectionStart = null,
        Action<TapDragUpdateDetails>? onDragSelectionUpdate = null,
        Action<TapDragEndDetails>? onDragSelectionEnd = null,
        bool onUserTapAlwaysCalled = false,
        HitTestBehavior? behavior = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        OnTapTrackStart = onTapTrackStart;
        OnTapTrackReset = onTapTrackReset;
        OnTapDown = onTapDown;
        OnForcePressStart = onForcePressStart;
        OnForcePressEnd = onForcePressEnd;
        OnSecondaryTap = onSecondaryTap;
        OnSecondaryTapDown = onSecondaryTapDown;
        OnSingleTapUp = onSingleTapUp;
        OnSingleTapCancel = onSingleTapCancel;
        OnUserTap = onUserTap;
        OnSingleLongTapStart = onSingleLongTapStart;
        OnSingleLongTapMoveUpdate = onSingleLongTapMoveUpdate;
        OnSingleLongTapEnd = onSingleLongTapEnd;
        OnSingleLongTapCancel = onSingleLongTapCancel;
        OnDoubleTapDown = onDoubleTapDown;
        OnTripleTapDown = onTripleTapDown;
        OnDragSelectionStart = onDragSelectionStart;
        OnDragSelectionUpdate = onDragSelectionUpdate;
        OnDragSelectionEnd = onDragSelectionEnd;
        OnUserTapAlwaysCalled = onUserTapAlwaysCalled;
        Behavior = behavior;
    }

    /// <summary>Called when a tap series starts.</summary>
    public Action? OnTapTrackStart { get; }

    /// <summary>Called when a tap series resets.</summary>
    public Action? OnTapTrackReset { get; }

    /// <summary>Called for every tap down, including consecutive ones.</summary>
    public Action<TapDragDownDetails>? OnTapDown { get; }

    /// <summary>Called when a pointer has tapped down and the force exceeds the start pressure.</summary>
    public Action<ForcePressDetails>? OnForcePressStart { get; }

    /// <summary>Called when a pointer that started a force press is released.</summary>
    public Action<ForcePressDetails>? OnForcePressEnd { get; }

    /// <summary>Called for a tap by a secondary button.</summary>
    public Action? OnSecondaryTap { get; }

    /// <summary>Called for a tap down by a secondary button.</summary>
    public Action<TapDownDetails>? OnSecondaryTapDown { get; }

    /// <summary>Called for the first tap up in a series.</summary>
    public Action<TapDragUpDetails>? OnSingleTapUp { get; }

    /// <summary>Called when the first tap in a series is cancelled.</summary>
    public Action? OnSingleTapCancel { get; }

    /// <summary>Called for the first tap in a series, or every tap when
    /// <see cref="OnUserTapAlwaysCalled"/> is set.</summary>
    public Action? OnUserTap { get; }

    /// <summary>Called for a single long tap that's sustained for longer than the long press
    /// timeout.</summary>
    public Action<LongPressStartDetails>? OnSingleLongTapStart { get; }

    /// <summary>Called after a long press when the pointer moves.</summary>
    public Action<LongPressMoveUpdateDetails>? OnSingleLongTapMoveUpdate { get; }

    /// <summary>Called after a long press when the pointer is lifted.</summary>
    public Action<LongPressEndDetails>? OnSingleLongTapEnd { get; }

    /// <summary>Called after a long press when the pointer is cancelled.</summary>
    public Action? OnSingleLongTapCancel { get; }

    /// <summary>Called for the tap down of the second tap in a series.</summary>
    public Action<TapDragDownDetails>? OnDoubleTapDown { get; }

    /// <summary>Called for the tap down of the third tap in a series.</summary>
    public Action<TapDragDownDetails>? OnTripleTapDown { get; }

    /// <summary>Called when a mouse or touch drag starts.</summary>
    public Action<TapDragStartDetails>? OnDragSelectionStart { get; }

    /// <summary>Called repeatedly as a drag moves.</summary>
    public Action<TapDragUpdateDetails>? OnDragSelectionUpdate { get; }

    /// <summary>Called when a drag ends.</summary>
    public Action<TapDragEndDetails>? OnDragSelectionEnd { get; }

    /// <summary>Whether <see cref="OnUserTap"/> runs for every tap in a series.</summary>
    public bool OnUserTapAlwaysCalled { get; }

    /// <summary>How this gesture detector behaves during hit testing.</summary>
    public HitTestBehavior? Behavior { get; }

    /// <summary>The child the gestures are detected on.</summary>
    public Widget Child { get; }

    public override State CreateState() => new TextSelectionGestureDetectorState();
}

internal sealed class TextSelectionGestureDetectorState : State<TextSelectionGestureDetector>
{
    // Converts the details.consecutiveTapCount from a TapAndDrag*GestureRecognizer to a number that
    // can be used by text editing to determine what kind of selection to make.
    internal static int GetEffectiveConsecutiveTapCount(int rawCount)
    {
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.Linux:
                // From observation, these platform's reset their tap count to 0 when the number of
                // consecutive taps exceeds 3. For example on Debian Linux with GTK, when going past a
                // triple click, on the fourth click the selection is moved to the precise click
                // position, on the fifth click the word at the position is selected, and on the
                // sixth click the paragraph at the position is selected.
                return rawCount <= 3 ? rawCount : (rawCount % 3 == 0 ? 3 : rawCount % 3);
            case TargetPlatform.IOS:
            case TargetPlatform.MacOS:
                // From observation, these platform's either hold their tap count at 3. For example on
                // macOS, when going past a triple click, the selection should be retained at the
                // paragraph that was first selected on triple click.
                return Math.Min(rawCount, 3);
            case TargetPlatform.Windows:
                // From observation, this platform's consecutive tap actions alternate between double
                // click and triple click actions. For example, after a triple click has selected a
                // paragraph, on the next click the word at the clicked position will be selected, and
                // on the next click the paragraph at the position is selected.
                return rawCount < 2 ? rawCount : 2 + rawCount % 2;
            default:
                return rawCount;
        }
    }

    private void HandleTapTrackStart() => Widget.OnTapTrackStart?.Invoke();

    private void HandleTapTrackReset() => Widget.OnTapTrackReset?.Invoke();

    // The down handler is force-run on success of a single tap and optimistically run before a
    // long press success.
    private void HandleTapDown(TapDragDownDetails details)
    {
        Widget.OnTapDown?.Invoke(details);
        // This isn't detected as a double tap gesture in the gesture recognizer because it's not a
        // sequential double tap sequence. It's a series of tap down events leading to a tap up
        // event.
        switch (GetEffectiveConsecutiveTapCount(details.ConsecutiveTapCount))
        {
            case 2:
                Widget.OnDoubleTapDown?.Invoke(details);
                break;
            case 3:
                Widget.OnTripleTapDown?.Invoke(details);
                break;
        }
    }

    private void HandleTapUp(TapDragUpDetails details)
    {
        if (GetEffectiveConsecutiveTapCount(details.ConsecutiveTapCount) == 1)
        {
            Widget.OnSingleTapUp?.Invoke(details);
            Widget.OnUserTap?.Invoke();
        }
        else if (Widget.OnUserTapAlwaysCalled)
        {
            Widget.OnUserTap?.Invoke();
        }
    }

    private void HandleTapCancel() => Widget.OnSingleTapCancel?.Invoke();

    private void HandleDragStart(TapDragStartDetails details) => Widget.OnDragSelectionStart?.Invoke(details);

    private void HandleDragUpdate(TapDragUpdateDetails details) => Widget.OnDragSelectionUpdate?.Invoke(details);

    private void HandleDragEnd(TapDragEndDetails details) => Widget.OnDragSelectionEnd?.Invoke(details);

    private void ForcePressStarted(ForcePressDetails details) => Widget.OnForcePressStart?.Invoke(details);

    private void ForcePressEnded(ForcePressDetails details) => Widget.OnForcePressEnd?.Invoke(details);

    private void HandleLongPressStart(LongPressStartDetails details) =>
        Widget.OnSingleLongTapStart?.Invoke(details);

    private void HandleLongPressMoveUpdate(LongPressMoveUpdateDetails details) =>
        Widget.OnSingleLongTapMoveUpdate?.Invoke(details);

    private void HandleLongPressEnd(LongPressEndDetails details) => Widget.OnSingleLongTapEnd?.Invoke(details);

    private void HandleLongPressCancel() => Widget.OnSingleLongTapCancel?.Invoke();

    public override Widget Build(BuildContext context)
    {
        var gestures = new Dictionary<Type, IGestureRecognizerFactory>
        {
            [typeof(TapGestureRecognizer)] = new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                () => new TapGestureRecognizer(),
                instance =>
                {
                    instance.OnSecondaryTap = Widget.OnSecondaryTap;
                    instance.OnSecondaryTapDown = Widget.OnSecondaryTapDown;
                }),
        };

        if (Widget.OnSingleLongTapStart is not null
            || Widget.OnSingleLongTapMoveUpdate is not null
            || Widget.OnSingleLongTapEnd is not null
            || Widget.OnSingleLongTapCancel is not null)
        {
            gestures[typeof(LongPressGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                    () => new LongPressGestureRecognizer
                    {
                        SupportedDevices = new HashSet<PointerDeviceKind> { PointerDeviceKind.Touch },
                    },
                    instance =>
                    {
                        instance.OnLongPressStart = HandleLongPressStart;
                        instance.OnLongPressMoveUpdate = HandleLongPressMoveUpdate;
                        instance.OnLongPressEnd = HandleLongPressEnd;
                        instance.OnLongPressCancel = HandleLongPressCancel;
                    });
        }

        if (Widget.OnDragSelectionStart is not null
            || Widget.OnDragSelectionUpdate is not null
            || Widget.OnDragSelectionEnd is not null)
        {
            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.Android:
                case TargetPlatform.Fuchsia:
                case TargetPlatform.IOS:
                    gestures[typeof(TapAndHorizontalDragGestureRecognizer)] =
                        new GestureRecognizerFactoryWithHandlers<TapAndHorizontalDragGestureRecognizer>(
                            () => new TapAndHorizontalDragGestureRecognizer(),
                            instance =>
                            {
                                // Text selection should start from the position of the first pointer
                                // down event.
                                instance.DragStartBehavior = DragStartBehavior.Down;
                                instance.EagerVictoryOnDrag = PlatformDefaults.TargetPlatform != TargetPlatform.IOS;
                                ConfigureTapAndDrag(instance);
                            });
                    break;
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    gestures[typeof(TapAndPanGestureRecognizer)] =
                        new GestureRecognizerFactoryWithHandlers<TapAndPanGestureRecognizer>(
                            () => new TapAndPanGestureRecognizer(),
                            instance =>
                            {
                                // Text selection should start from the position of the first pointer
                                // down event.
                                instance.DragStartBehavior = DragStartBehavior.Down;
                                ConfigureTapAndDrag(instance);
                            });
                    break;
            }
        }

        if (Widget.OnForcePressStart is not null || Widget.OnForcePressEnd is not null)
        {
            gestures[typeof(ForcePressGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<ForcePressGestureRecognizer>(
                    () => new ForcePressGestureRecognizer(debugOwner: this),
                    instance =>
                    {
                        instance.OnStart = Widget.OnForcePressStart is not null ? ForcePressStarted : null;
                        instance.OnEnd = Widget.OnForcePressEnd is not null ? ForcePressEnded : null;
                    });
        }

        return new RawGestureDetector(
            gestures: gestures,
            excludeFromSemantics: true,
            behavior: Widget.Behavior,
            child: Widget.Child);
    }

    private void ConfigureTapAndDrag(BaseTapAndDragGestureRecognizer instance)
    {
        instance.OnTapTrackStart = HandleTapTrackStart;
        instance.OnTapTrackReset = HandleTapTrackReset;
        instance.OnTapDown = HandleTapDown;
        instance.OnDragStart = HandleDragStart;
        instance.OnDragUpdate = HandleDragUpdate;
        instance.OnDragEnd = HandleDragEnd;
        instance.OnTapUp = HandleTapUp;
        instance.OnCancel = HandleTapCancel;
    }
}
