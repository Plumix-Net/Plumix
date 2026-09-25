using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/text_selection.dart
// Dart parity source: flutter/packages/flutter/lib/src/services/text_input.dart (TextSelectionDelegate)

namespace Plumix.Widgets;

/// <summary>Which side of a selection a handle is drawn on.</summary>
// Dart parity source: flutter/packages/flutter/lib/src/rendering/selection.dart (TextSelectionHandleType)
public enum TextSelectionHandleType
{
    Left,
    Right,
    Collapsed,
}

/// <summary>
/// The parent data of a selection-toolbar item, tracking whether the item is on the visible page.
/// </summary>
public sealed class ToolbarItemsParentData : ContainerBoxParentData<RenderBox>
{
    /// <summary>Whether the child should be painted (and hit-tested) on the current page.</summary>
    public bool ShouldPaint { get; set; }
}

public enum ClipboardStatus
{
    Pasteable,
    Unknown,
    NotPasteable,
}

/// <summary>Tracks whether the clipboard currently holds pasteable content.</summary>
public class ClipboardStatusNotifier : ChangeNotifier, IValueListenable<ClipboardStatus>, WidgetsBindingObserver
{
    private ClipboardStatus _value;
    private bool _disposed;

    public ClipboardStatusNotifier(ClipboardStatus value = ClipboardStatus.Unknown)
    {
        _value = value;
    }

    public virtual ClipboardStatus Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            NotifyListeners();
        }
    }

    public virtual async Task Update()
    {
        if (_disposed)
        {
            return;
        }

        bool hasStrings;
        try
        {
            hasStrings = await Clipboard.HasStrings();
        }
        catch (Exception exception)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: exception,
                stack: exception.StackTrace,
                library: "widget library",
                context: new ErrorDescription("while checking if the clipboard has strings")));
            if (!_disposed)
            {
                Value = ClipboardStatus.Unknown;
            }
            return;
        }

        if (!_disposed)
        {
            Value = hasStrings ? ClipboardStatus.Pasteable : ClipboardStatus.NotPasteable;
        }
    }

    public override void AddListener(Action listener)
    {
        if (!HasListeners)
        {
            WidgetsBinding.Instance.AddObserver(this);
        }
        if (Value == ClipboardStatus.Unknown)
        {
            // Dart's `update()` yields at its first await, so the listener below is added before
            // the status changes; a C# await of a completed task does not yield.
            Scheduler.ScheduleMicrotask(() => Scheduler.RunAsync(Update));
        }
        base.AddListener(listener);
    }

    public override void RemoveListener(Action listener)
    {
        base.RemoveListener(listener);
        if (!_disposed && !HasListeners)
        {
            WidgetsBinding.Instance.RemoveObserver(this);
        }
    }

    public void DidChangeAppLifecycleState(AppLifecycleState state)
    {
        if (state == AppLifecycleState.Resumed)
        {
            Scheduler.RunAsync(Update);
        }
    }

    public override void Dispose()
    {
        WidgetsBinding.Instance.RemoveObserver(this);
        _disposed = true;
        base.Dispose();
    }
}

/// <summary>An enumeration that indicates whether the current device is available for Live Text
/// input.</summary>
public enum LiveTextInputStatus
{
    /// <summary>This device supports Live Text input currently.</summary>
    Enabled,

    /// <summary>The status of the Live Text input is unknown.</summary>
    Unknown,

    /// <summary>The current device doesn't support Live Text input.</summary>
    Disabled,
}

/// <summary>A <see cref="ValueNotifier{T}"/> whose value indicates whether the current device
/// supports Live Text input.</summary>
public class LiveTextInputStatusNotifier : ValueNotifier<LiveTextInputStatus>, WidgetsBindingObserver
{
    private bool _disposed;

    /// <summary>Create a new LiveTextStatusNotifier.</summary>
    public LiveTextInputStatusNotifier(LiveTextInputStatus value = LiveTextInputStatus.Unknown)
        : base(value)
    {
    }

    /// <summary>Check the <see cref="LiveTextInputStatus"/> and update <c>Value</c> if needed.
    /// </summary>
    public async Task Update()
    {
        if (_disposed)
        {
            return;
        }

        bool isLiveTextInputEnabled;
        try
        {
            isLiveTextInputEnabled = await LiveText.IsLiveTextInputAvailable();
        }
        catch (Exception exception)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: exception,
                stack: exception.StackTrace,
                library: "widget library",
                context: new ErrorDescription("while checking the availability of Live Text input")));
            // In the case of an error from the Live Text API, set the value to unknown so that it
            // will try to update again later.
            if (_disposed || Value == LiveTextInputStatus.Unknown)
            {
                return;
            }

            Value = LiveTextInputStatus.Unknown;
            return;
        }

        LiveTextInputStatus nextStatus = isLiveTextInputEnabled
            ? LiveTextInputStatus.Enabled
            : LiveTextInputStatus.Disabled;

        if (_disposed || nextStatus == Value)
        {
            return;
        }

        Value = nextStatus;
    }

    public override void AddListener(Action listener)
    {
        if (!HasListeners)
        {
            WidgetsBinding.Instance.AddObserver(this);
        }

        if (Value == LiveTextInputStatus.Unknown)
        {
            // See ClipboardStatusNotifier.AddListener.
            Scheduler.ScheduleMicrotask(() => Scheduler.RunAsync(Update));
        }

        base.AddListener(listener);
    }

    public override void RemoveListener(Action listener)
    {
        base.RemoveListener(listener);
        if (!_disposed && !HasListeners)
        {
            WidgetsBinding.Instance.RemoveObserver(this);
        }
    }

    public void DidChangeAppLifecycleState(AppLifecycleState state)
    {
        switch (state)
        {
            case AppLifecycleState.Resumed:
                Scheduler.RunAsync(Update);
                break;
            case AppLifecycleState.Detached:
            case AppLifecycleState.Inactive:
            case AppLifecycleState.Paused:
            case AppLifecycleState.Hidden:
                // Nothing to do.
                break;
        }
    }

    public override void Dispose()
    {
        WidgetsBinding.Instance.RemoveObserver(this);
        _disposed = true;
        base.Dispose();
    }
}

/// <summary>The editing surface a selection toolbar and its handles act on.</summary>
public interface ITextSelectionDelegate
{
    TextEditingValue TextEditingValue { get; }

    bool CutEnabled => true;

    bool CopyEnabled => true;

    bool PasteEnabled => true;

    bool SelectAllEnabled => true;

    /// <summary>Whether Live Text input is enabled. Dart's <c>liveTextInputEnabled</c>, false by
    /// default.</summary>
    bool LiveTextInputEnabled => false;

    void UserUpdateTextEditingValue(TextEditingValue value, SelectionChangedCause? cause);

    void CutSelection(SelectionChangedCause cause);

    void CopySelection(SelectionChangedCause cause);

    void PasteText(SelectionChangedCause cause);

    Task PasteTextAsync(SelectionChangedCause cause)
    {
        PasteText(cause);
        return Task.CompletedTask;
    }

    void SelectAll(SelectionChangedCause cause);

    void HideToolbar(bool hideHandles = true);

    void BringIntoView(TextPosition position)
    {
    }
}

/// <summary>A position in text, used when asking the editing surface to reveal a location.</summary>
public readonly record struct TextPosition(int Offset, TextAffinity Affinity = TextAffinity.Downstream);

public enum TextAffinity
{
    Upstream,
    Downstream,
}

/// <summary>Builds the platform-specific selection handles and (legacy) selection toolbar.</summary>
public abstract class TextSelectionControls
{
    /// <summary>
    /// Builds a handle of the given type. The top left corner of the returned widget is positioned at
    /// the bottom of the selection position.
    /// </summary>
    public abstract Widget BuildHandle(
        BuildContext context,
        TextSelectionHandleType type,
        double textLineHeight,
        Action? onTap = null);

    /// <summary>The anchor within the handle that is placed on the selection endpoint.</summary>
    public abstract Point GetHandleAnchor(TextSelectionHandleType type, double textLineHeight);

    public abstract Size GetHandleSize(double textLineHeight);

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public abstract Widget BuildToolbar(
        BuildContext context,
        Rect globalEditableRegion,
        double textLineHeight,
        Point selectionMidpoint,
        IReadOnlyList<TextSelectionPoint> endpoints,
        ITextSelectionDelegate @delegate,
        IValueListenable<ClipboardStatus>? clipboardStatus,
        Point? lastSecondaryTapDownPosition);

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public virtual bool CanCut(ITextSelectionDelegate @delegate)
    {
        return @delegate.CutEnabled && !@delegate.TextEditingValue.Selection.IsCollapsed;
    }

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public virtual bool CanCopy(ITextSelectionDelegate @delegate)
    {
        return @delegate.CopyEnabled && !@delegate.TextEditingValue.Selection.IsCollapsed;
    }

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public virtual bool CanPaste(ITextSelectionDelegate @delegate)
    {
        return @delegate.PasteEnabled;
    }

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public virtual bool CanSelectAll(ITextSelectionDelegate @delegate)
    {
        return @delegate.SelectAllEnabled
               && @delegate.TextEditingValue.Text.Length > 0
               && @delegate.TextEditingValue.Selection.IsCollapsed;
    }

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public virtual void HandleCut(ITextSelectionDelegate @delegate)
    {
        @delegate.CutSelection(SelectionChangedCause.Toolbar);
    }

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public virtual void HandleCopy(ITextSelectionDelegate @delegate)
    {
        @delegate.CopySelection(SelectionChangedCause.Toolbar);
    }

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public virtual void HandlePaste(ITextSelectionDelegate @delegate)
    {
        Scheduler.RunAsync(() => @delegate.PasteTextAsync(SelectionChangedCause.Toolbar));
    }

    /// <summary>Selects the whole document. Does not hide the toolbar.</summary>
    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public virtual void HandleSelectAll(ITextSelectionDelegate @delegate)
    {
        @delegate.SelectAll(SelectionChangedCause.Toolbar);
    }
}

/// <summary>
/// Marks controls that build handles only, leaving the toolbar to a <c>contextMenuBuilder</c>.
/// </summary>
/// <remarks>
/// Dart uses the <c>TextSelectionHandleControls</c> mixin here. C# has no mixins, so the toolbar
/// suppression is a marker interface that concrete controls implement alongside their overrides.
/// </remarks>
public interface ITextSelectionHandleControls
{
}

/// <summary>Text selection controls that build nothing.</summary>
public class EmptyTextSelectionControls : TextSelectionControls
{
    public static TextSelectionControls Instance { get; } = new EmptyTextSelectionControls();

    public override Size GetHandleSize(double textLineHeight) => default;

    public override Widget BuildHandle(
        BuildContext context,
        TextSelectionHandleType type,
        double textLineHeight,
        Action? onTap = null)
    {
        return new SizedBox();
    }

    public override Point GetHandleAnchor(TextSelectionHandleType type, double textLineHeight) => default;

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override Widget BuildToolbar(
        BuildContext context,
        Rect globalEditableRegion,
        double textLineHeight,
        Point selectionMidpoint,
        IReadOnlyList<TextSelectionPoint> endpoints,
        ITextSelectionDelegate @delegate,
        IValueListenable<ClipboardStatus>? clipboardStatus,
        Point? lastSecondaryTapDownPosition)
    {
        return new SizedBox();
    }
}

/// <summary>
/// An object that manages a pair of text selection handles for a <see cref="RenderEditable"/>.
/// </summary>
/// <remarks>
/// Dart's <c>TextSelectionOverlay</c>: a <see cref="RenderEditable"/>-specific wrapper around
/// <see cref="SelectionOverlay"/>. It turns handle drags into selection changes and builds the
/// magnifier's info from the editable's geometry.
/// </remarks>
public sealed class TextSelectionOverlay : IDisposable
{
    private readonly ValueNotifier<bool> _effectiveStartHandleVisibility = new(false);
    private readonly ValueNotifier<bool> _effectiveEndHandleVisibility = new(false);
    private readonly ValueNotifier<bool> _effectiveToolbarVisibility = new(false);
    private TextEditingValue _value;
    private bool _handlesVisible;

    // The contact position of the gesture at the current end handle location, in global
    // coordinates. Updated when the handle moves.
    private double _endHandleDragPosition;

    // The distance from _endHandleDragPosition to the center of the line that it corresponds to,
    // in global coordinates.
    private double _endHandleDragTarget;

    // The contact position of the gesture at the current start handle location, in global
    // coordinates. Updated when the handle moves.
    private double _startHandleDragPosition;

    // The distance from _startHandleDragPosition to the center of the line that it corresponds
    // to, in global coordinates.
    private double _startHandleDragTarget;

    // The initial selection when a selection handle drag has started.
    private TextSelection? _dragStartSelection;

    /// <summary>Creates an object that manages overlay entries for selection handles.</summary>
    public TextSelectionOverlay(
        TextEditingValue value,
        BuildContext context,
        LayerLink toolbarLayerLink,
        LayerLink startHandleLayerLink,
        LayerLink endHandleLayerLink,
        RenderEditable renderObject,
        ITextSelectionDelegate selectionDelegate,
        TextMagnifierConfiguration magnifierConfiguration,
        Widget? debugRequiredFor = null,
        TextSelectionControls? selectionControls = null,
        bool handlesVisible = false,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        Action? onSelectionHandleTapped = null,
        ClipboardStatusNotifier? clipboardStatus = null,
        WidgetBuilder? contextMenuBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(magnifierConfiguration);
        _value = value;
        Context = context;
        RenderObject = renderObject;
        SelectionControls = selectionControls;
        SelectionDelegate = selectionDelegate;
        _handlesVisible = handlesVisible;
        ContextMenuBuilder = contextMenuBuilder;
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchCreated("widgets", "TextSelectionOverlay", this);
        }

        renderObject.SelectionStartInViewport.AddListener(UpdateTextSelectionOverlayVisibilities);
        renderObject.SelectionEndInViewport.AddListener(UpdateTextSelectionOverlayVisibilities);
        UpdateTextSelectionOverlayVisibilities();
#pragma warning disable CS0618 // Dart forwards the deprecated legacy-toolbar arguments too.
        SelectionOverlay = new SelectionOverlay(
            magnifierConfiguration: magnifierConfiguration,
            context: context,
            debugRequiredFor: debugRequiredFor,
            // The metrics will be set when show handles is called.
            startHandleType: TextSelectionHandleType.Collapsed,
            startHandlesVisible: _effectiveStartHandleVisibility,
            lineHeightAtStart: 0.0,
            onStartHandleDragStart: HandleSelectionStartHandleDragStart,
            onStartHandleDragUpdate: HandleSelectionStartHandleDragUpdate,
            onEndHandleDragEnd: HandleAnyDragEnd,
            endHandleType: TextSelectionHandleType.Collapsed,
            endHandlesVisible: _effectiveEndHandleVisibility,
            lineHeightAtEnd: 0.0,
            onEndHandleDragStart: HandleSelectionEndHandleDragStart,
            onEndHandleDragUpdate: HandleSelectionEndHandleDragUpdate,
            onStartHandleDragEnd: HandleAnyDragEnd,
            toolbarVisible: _effectiveToolbarVisibility,
            selectionEndpoints: [],
            selectionControls: selectionControls,
            selectionDelegate: selectionDelegate,
            clipboardStatus: clipboardStatus,
            startHandleLayerLink: startHandleLayerLink,
            endHandleLayerLink: endHandleLayerLink,
            toolbarLayerLink: toolbarLayerLink,
            onSelectionHandleTapped: onSelectionHandleTapped,
            dragStartBehavior: dragStartBehavior,
            toolbarLocation: renderObject.LastSecondaryTapDownPosition);
#pragma warning restore CS0618
    }

    /// <summary>The context in which the selection UI should appear.</summary>
    public BuildContext Context { get; }

    /// <summary>The editable line in which the selected text is being displayed.</summary>
    public RenderEditable RenderObject { get; }

    /// <summary>Builds text selection handles and the (legacy) toolbar.</summary>
    public TextSelectionControls? SelectionControls { get; }

    /// <summary>The delegate for manipulating the current selection in the owning text field.
    /// </summary>
    public ITextSelectionDelegate SelectionDelegate { get; }

    /// <summary>Builds the context menu; replaces the deprecated toolbar of
    /// <see cref="SelectionControls"/>.</summary>
    public WidgetBuilder? ContextMenuBuilder { get; }

    /// <summary>The underlying <see cref="Widgets.SelectionOverlay"/>. Dart keeps it private
    /// (<c>_selectionOverlay</c>); Plumix exposes it for the framework's own callers.</summary>
    public SelectionOverlay SelectionOverlay { get; }

    /// <summary>Retrieve current value.</summary>
    public TextEditingValue Value => _value;

    private TextSelection Selection => _value.Selection;

    /// <summary>
    /// Whether selection handles are visible. Set to false to hide them, e.g. when the owning
    /// field loses focus; defaults to false.
    /// </summary>
    public bool HandlesVisible
    {
        get => _handlesVisible;
        set
        {
            if (_handlesVisible == value)
            {
                return;
            }

            _handlesVisible = value;
            UpdateTextSelectionOverlayVisibilities();
        }
    }

    /// <summary>Whether the handles are currently visible.</summary>
    public bool HandlesAreVisible => SelectionOverlay.HandlesAreInserted && HandlesVisible;

    /// <summary>Whether the toolbar is currently visible. Includes both the text selection toolbar
    /// and the spell check menu.</summary>
    public bool ToolbarIsVisible => SelectionOverlay.ToolbarIsVisible;

    /// <summary>Whether the magnifier is currently visible.</summary>
    public bool MagnifierIsVisible => SelectionOverlay.MagnifierIsVisible;

    /// <summary>Whether the magnifier currently exists (it may be hidden while it exists).</summary>
    public bool MagnifierExists => SelectionOverlay.MagnifierExists;

    /// <summary>Whether the spell check menu is currently visible.</summary>
    public bool SpellCheckToolbarIsVisible => SelectionOverlay.SpellCheckToolbarIsVisible;

    private void UpdateTextSelectionOverlayVisibilities()
    {
        _effectiveStartHandleVisibility.Value = _handlesVisible && RenderObject.SelectionStartInViewport.Value;
        _effectiveEndHandleVisibility.Value = _handlesVisible && RenderObject.SelectionEndInViewport.Value;
        _effectiveToolbarVisibility.Value = RenderObject.SelectionStartInViewport.Value
                                            || RenderObject.SelectionEndInViewport.Value;
    }

    /// <summary>Builds the handles by inserting them into the context's overlay.</summary>
    public void ShowHandles()
    {
        UpdateSelectionOverlay();
        SelectionOverlay.ShowHandles();
    }

    /// <summary>Destroys the handles by removing them from the overlay.</summary>
    public void HideHandles() => SelectionOverlay.HideHandles();

    /// <summary>Shows the toolbar by inserting it into the context's overlay.</summary>
    public void ShowToolbar()
    {
        if (Constants.KDebugMode && Scheduler.Phase == SchedulerPhase.PersistentCallbacks)
        {
            throw new AssertionError("showToolbar must not be called during the build or layout phase.");
        }

        UpdateSelectionOverlay();

        if (SelectionControls is not null and not ITextSelectionHandleControls)
        {
            SelectionOverlay.ShowToolbar();
            return;
        }

        if (ContextMenuBuilder is null)
        {
            return;
        }

        Debug.Assert(Context.Mounted);
        SelectionOverlay.ShowToolbar(context: Context, contextMenuBuilder: ContextMenuBuilder);
    }

    /// <summary>Shows the toolbar that suggests replacements for a misspelled word, and hides the
    /// handles.</summary>
    public void ShowSpellCheckSuggestionsToolbar(WidgetBuilder spellCheckSuggestionsToolbarBuilder)
    {
        UpdateSelectionOverlay();
        Debug.Assert(Context.Mounted);
        SelectionOverlay.ShowSpellCheckSuggestionsToolbar(
            context: Context,
            builder: spellCheckSuggestionsToolbarBuilder);
        HideHandles();
    }

    /// <summary>Shows the magnifier at <paramref name="positionToShow"/>, a global position.
    /// Does nothing if a magnifier is already shown.</summary>
    public void ShowMagnifier(Point positionToShow)
    {
        TextPosition position = RenderObject.GetPositionForPoint(positionToShow);
        UpdateSelectionOverlay();
        SelectionOverlay.ShowMagnifier(BuildMagnifier(
            currentTextPosition: position,
            globalGesturePosition: positionToShow,
            renderEditable: RenderObject));
    }

    /// <summary>Moves the magnifier to <paramref name="positionToShow"/>, a global position, if one
    /// exists.</summary>
    public void UpdateMagnifier(Point positionToShow)
    {
        TextPosition position = RenderObject.GetPositionForPoint(positionToShow);
        UpdateSelectionOverlay();
        SelectionOverlay.UpdateMagnifier(BuildMagnifier(
            currentTextPosition: position,
            globalGesturePosition: positionToShow,
            renderEditable: RenderObject));
    }

    /// <summary>Hides the current magnifier, if any.</summary>
    public void HideMagnifier() => SelectionOverlay.HideMagnifier();

    /// <summary>Updates the overlay after the selection has changed.</summary>
    public void Update(TextEditingValue newValue)
    {
        if (_value == newValue)
        {
            return;
        }

        _value = newValue;
        UpdateSelectionOverlay();
        // UpdateSelectionOverlay may not rebuild the selection overlay if the text metrics and
        // selection doesn't change even if the text has changed. This rebuild is needed for the
        // toolbar to update based on the latest text value.
        SelectionOverlay.MarkNeedsBuild();
    }

    private void UpdateSelectionOverlay()
    {
        SelectionOverlay.StartHandleType = ChooseType(
            RenderObject.TextDirection,
            TextSelectionHandleType.Left,
            TextSelectionHandleType.Right);
        SelectionOverlay.LineHeightAtStart = GetStartGlyphHeight();
        SelectionOverlay.EndHandleType = ChooseType(
            RenderObject.TextDirection,
            TextSelectionHandleType.Right,
            TextSelectionHandleType.Left);
        SelectionOverlay.LineHeightAtEnd = GetEndGlyphHeight();
        SelectionOverlay.SelectionEndpoints = RenderObject.GetEndpointsForSelection(Selection);
#pragma warning disable CS0618 // Dart keeps the deprecated location in sync too.
        SelectionOverlay.ToolbarLocation = RenderObject.LastSecondaryTapDownPosition;
#pragma warning restore CS0618
    }

    /// <summary>Causes the overlay to update its rendering after the owning field scrolled.
    /// </summary>
    public void UpdateForScroll()
    {
        UpdateSelectionOverlay();
        // This method may be called due to windows metrics changes. In that case, non of the
        // properties in _selectionOverlay will change, but a rebuild is still needed.
        SelectionOverlay.MarkNeedsBuild();
    }

    /// <summary>Hides the entire overlay including the toolbar and the handles.</summary>
    public void Hide() => SelectionOverlay.Hide();

    /// <summary>Hides the toolbar part of the overlay.</summary>
    public void HideToolbar() => SelectionOverlay.HideToolbar();

    /// <summary>Disposes this object and releases resources.</summary>
    public void Dispose()
    {
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchDisposed(this);
        }

        SelectionOverlay.Dispose();
        RenderObject.SelectionStartInViewport.RemoveListener(UpdateTextSelectionOverlayVisibilities);
        RenderObject.SelectionEndInViewport.RemoveListener(UpdateTextSelectionOverlayVisibilities);
        _effectiveToolbarVisibility.Dispose();
        _effectiveStartHandleVisibility.Dispose();
        _effectiveEndHandleVisibility.Dispose();
        HideToolbar();
    }

    private double GetStartGlyphHeight()
    {
        string currText = SelectionDelegate.TextEditingValue.Text;
        Rect? startHandleRect = null;
        // Only calculate handle rects if the text in the previous frame is the same as the text
        // in the current frame. This is done because widget.renderObject contains the renderEditable
        // from the previous frame. If the text changed between the current and previous frames
        // then widget.renderObject.getRectForComposingRange might fail. In cases where the current
        // frame is different from the previous we fall back to renderObject.preferredLineHeight.
        if (RenderObject.PlainText == currText && Selection.IsValid && !Selection.IsCollapsed)
        {
            string selectedGraphemes = Selection.TextInside(currText);
            int firstSelectedGraphemeExtent = StringInfo.GetNextTextElementLength(selectedGraphemes);
            startHandleRect = RenderObject.GetRectForComposingRange(
                new TextRange(Selection.Start, Selection.Start + firstSelectedGraphemeExtent));
        }

        return startHandleRect?.Height ?? RenderObject.PreferredLineHeight;
    }

    private double GetEndGlyphHeight()
    {
        string currText = SelectionDelegate.TextEditingValue.Text;
        Rect? endHandleRect = null;
        // See the explanation in GetStartGlyphHeight.
        if (RenderObject.PlainText == currText && Selection.IsValid && !Selection.IsCollapsed)
        {
            string selectedGraphemes = Selection.TextInside(currText);
            int[] graphemeStarts = StringInfo.ParseCombiningCharacters(selectedGraphemes);
            int lastSelectedGraphemeExtent = selectedGraphemes.Length - graphemeStarts[^1];
            endHandleRect = RenderObject.GetRectForComposingRange(
                new TextRange(Selection.End - lastSelectedGraphemeExtent, Selection.End));
        }

        return endHandleRect?.Height ?? RenderObject.PreferredLineHeight;
    }

    private MagnifierInfo BuildMagnifier(
        RenderEditable renderEditable,
        Point globalGesturePosition,
        TextPosition currentTextPosition)
    {
        TextSelection lineAtOffset = renderEditable.GetLineAtOffset(currentTextPosition);
        var positionAtEndOfLine = new TextPosition(lineAtOffset.ExtentOffset, TextAffinity.Upstream);

        // Default affinity is downstream.
        var positionAtBeginningOfLine = new TextPosition(lineAtOffset.BaseOffset);

        Rect lineStartCaret = renderEditable.GetLocalRectForCaret(positionAtBeginningOfLine);
        Rect lineEndCaret = renderEditable.GetLocalRectForCaret(positionAtEndOfLine);
        Rect localLineBoundaries = RectFromPoints(
            new Point(lineStartCaret.Center.X, lineStartCaret.Top),
            new Point(lineEndCaret.Center.X, lineEndCaret.Bottom));
        var overlay = Overlay.Of(Context, rootOverlay: true).Context.FindRenderObject() as RenderBox;
        Matrix4 transformToOverlay = renderEditable.GetTransformTo(overlay);
        Rect overlayLineBoundaries = MatrixUtils.TransformRect(transformToOverlay, localLineBoundaries);

        Rect localCaretRect = renderEditable.GetLocalRectForCaret(currentTextPosition);
        Rect overlayCaretRect = MatrixUtils.TransformRect(transformToOverlay, localCaretRect);

        Point overlayGesturePosition = overlay?.GlobalToLocal(globalGesturePosition) ?? globalGesturePosition;

        return new MagnifierInfo(
            GlobalGesturePosition: overlayGesturePosition,
            CaretRect: overlayCaretRect,
            FieldBounds: MatrixUtils.TransformRect(transformToOverlay, renderEditable.PaintBounds),
            CurrentLineBoundaries: overlayLineBoundaries);
    }

    private static Rect RectFromPoints(Point a, Point b)
    {
        return new Rect(
            Math.Min(a.X, b.X),
            Math.Min(a.Y, b.Y),
            Math.Abs(a.X - b.X),
            Math.Abs(a.Y - b.Y));
    }

    private static bool IsApplePlatform =>
        PlatformDefaults.TargetPlatform is TargetPlatform.IOS or TargetPlatform.MacOS;

    private void HandleSelectionEndHandleDragStart(DragStartDetails details)
    {
        if (!RenderObject.Attached)
        {
            return;
        }

        // This adjusts for the fact that the selection handles may not perfectly cover the
        // TextPosition that they correspond to.
        _endHandleDragPosition = details.GlobalPosition.Y;
        double centerOfLineLocal = SelectionOverlay.SelectionEndpoints[^1].Point.Y
                                   - RenderObject.PreferredLineHeight / 2;
        double centerOfLineGlobal = RenderObject.LocalToGlobal(new Point(0.0, centerOfLineLocal)).Y;
        _endHandleDragTarget = centerOfLineGlobal - details.GlobalPosition.Y;
        // Instead of finding the TextPosition at the handle's location directly, use the vertical
        // center of the line that it points to. This is because selection handles typically hang
        // above or below the line that they point to.
        TextPosition position = RenderObject.GetPositionForPoint(
            new Point(details.GlobalPosition.X, centerOfLineGlobal));
        if (IsApplePlatform)
        {
            _dragStartSelection ??= Selection;
        }

        SelectionOverlay.ShowMagnifier(BuildMagnifier(
            currentTextPosition: position,
            globalGesturePosition: details.GlobalPosition,
            renderEditable: RenderObject));
    }

    /// <summary>
    /// Given a handle position and drag position, returns the position of handle after the drag.
    /// The handle jumps instantly between lines when the drag reaches a full line's height away
    /// from the original handle position. In other words, the line jump happens when the contact
    /// point would be located at the same place on the handle at the new line as when the gesture
    /// started, for both directions. Returns null when there is no well-defined position.
    /// </summary>
    /// <remarks>Dart's <c>_getHandleDy</c>. Both parameters and the result are in local
    /// coordinates.</remarks>
    private double? GetHandleDy(double dragDy, double handleDy)
    {
        double preferredLineHeight = RenderObject.PreferredLineHeight;
        Debug.Assert(
            double.IsFinite(preferredLineHeight),
            "Preferred line height is expected to always be finite.");
        if (preferredLineHeight <= 0.0 || !double.IsFinite(dragDy) || !double.IsFinite(handleDy))
        {
            return null;
        }

        double distanceDragged = dragDy - handleDy;
        int dragDirection = distanceDragged < 0.0 ? -1 : 1;
        int linesDragged = dragDirection * (int)Math.Floor(Math.Abs(distanceDragged) / preferredLineHeight);
        return handleDy + linesDragged * preferredLineHeight;
    }

    private void HandleSelectionEndHandleDragUpdate(DragUpdateDetails details)
    {
        if (!RenderObject.Attached)
        {
            return;
        }

        // This is NOT the same as details.LocalPosition. That is relative to the selection handle,
        // whereas this is relative to the RenderEditable.
        Point localPosition = RenderObject.GlobalToLocal(details.GlobalPosition);
        double? nextEndHandleDragPositionLocal = GetHandleDy(
            localPosition.Y,
            RenderObject.GlobalToLocal(new Point(0.0, _endHandleDragPosition)).Y);
        if (nextEndHandleDragPositionLocal is not { } nextLocal)
        {
            return;
        }

        _endHandleDragPosition = RenderObject.LocalToGlobal(new Point(0.0, nextLocal)).Y;
        var handleTargetGlobal = new Point(
            details.GlobalPosition.X,
            _endHandleDragPosition + _endHandleDragTarget);
        TextPosition position = RenderObject.GetPositionForPoint(handleTargetGlobal);

        TextSelection newSelection;
        if (IsApplePlatform)
        {
            // On Apple platforms, dragging the base handle makes it the extent.
            Debug.Assert(_dragStartSelection is not null);
            TextSelection dragStartSelection = _dragStartSelection!.Value;
            if (dragStartSelection.IsCollapsed)
            {
                SelectionOverlay.UpdateMagnifier(BuildMagnifier(
                    currentTextPosition: position,
                    globalGesturePosition: details.GlobalPosition,
                    renderEditable: RenderObject));
                HandleSelectionHandleChanged(TextSelection.FromPosition(position));
                return;
            }

            // Use this instead of _dragStartSelection.IsNormalized because TextRange.IsNormalized
            // always returns true for a TextSelection.
            bool dragStartSelectionNormalized = dragStartSelection.ExtentOffset >= dragStartSelection.BaseOffset;
            newSelection = new TextSelection(
                dragStartSelectionNormalized ? dragStartSelection.BaseOffset : dragStartSelection.ExtentOffset,
                position.Offset);
        }
        else
        {
            // On non-Apple platforms, dragging the base handle makes it the base.
            if (Selection.IsCollapsed)
            {
                SelectionOverlay.UpdateMagnifier(BuildMagnifier(
                    currentTextPosition: position,
                    globalGesturePosition: details.GlobalPosition,
                    renderEditable: RenderObject));
                HandleSelectionHandleChanged(TextSelection.FromPosition(position));
                return;
            }

            newSelection = new TextSelection(Selection.BaseOffset, position.Offset);
            if (newSelection.BaseOffset >= newSelection.ExtentOffset)
            {
                return; // Don't allow order swapping.
            }
        }

        HandleSelectionHandleChanged(newSelection);

        SelectionOverlay.UpdateMagnifier(BuildMagnifier(
            currentTextPosition: newSelection.Extent,
            globalGesturePosition: details.GlobalPosition,
            renderEditable: RenderObject));
    }

    private void HandleSelectionStartHandleDragStart(DragStartDetails details)
    {
        if (!RenderObject.Attached)
        {
            return;
        }

        // This adjusts for the fact that the selection handles may not perfectly cover the
        // TextPosition that they correspond to.
        _startHandleDragPosition = details.GlobalPosition.Y;
        double centerOfLineLocal = SelectionOverlay.SelectionEndpoints[0].Point.Y
                                   - RenderObject.PreferredLineHeight / 2;
        double centerOfLineGlobal = RenderObject.LocalToGlobal(new Point(0.0, centerOfLineLocal)).Y;
        _startHandleDragTarget = centerOfLineGlobal - details.GlobalPosition.Y;
        // Instead of finding the TextPosition at the handle's location directly, use the vertical
        // center of the line that it points to. This is because selection handles typically hang
        // above or below the line that they point to.
        TextPosition position = RenderObject.GetPositionForPoint(
            new Point(details.GlobalPosition.X, centerOfLineGlobal));
        if (IsApplePlatform)
        {
            _dragStartSelection ??= Selection;
        }

        SelectionOverlay.ShowMagnifier(BuildMagnifier(
            currentTextPosition: position,
            globalGesturePosition: details.GlobalPosition,
            renderEditable: RenderObject));
    }

    private void HandleSelectionStartHandleDragUpdate(DragUpdateDetails details)
    {
        if (!RenderObject.Attached)
        {
            return;
        }

        // This is NOT the same as details.LocalPosition. That is relative to the selection handle,
        // whereas this is relative to the RenderEditable.
        Point localPosition = RenderObject.GlobalToLocal(details.GlobalPosition);
        double? nextStartHandleDragPositionLocal = GetHandleDy(
            localPosition.Y,
            RenderObject.GlobalToLocal(new Point(0.0, _startHandleDragPosition)).Y);
        if (nextStartHandleDragPositionLocal is not { } nextLocal)
        {
            return;
        }

        _startHandleDragPosition = RenderObject.LocalToGlobal(new Point(0.0, nextLocal)).Y;
        var handleTargetGlobal = new Point(
            details.GlobalPosition.X,
            _startHandleDragPosition + _startHandleDragTarget);
        TextPosition position = RenderObject.GetPositionForPoint(handleTargetGlobal);

        TextSelection newSelection;
        if (IsApplePlatform)
        {
            // On Apple platforms, dragging the base handle makes it the extent.
            Debug.Assert(_dragStartSelection is not null);
            TextSelection dragStartSelection = _dragStartSelection!.Value;
            if (dragStartSelection.IsCollapsed)
            {
                SelectionOverlay.UpdateMagnifier(BuildMagnifier(
                    currentTextPosition: position,
                    globalGesturePosition: details.GlobalPosition,
                    renderEditable: RenderObject));
                HandleSelectionHandleChanged(TextSelection.FromPosition(position));
                return;
            }

            // Use this instead of _dragStartSelection.IsNormalized because TextRange.IsNormalized
            // always returns true for a TextSelection.
            bool dragStartSelectionNormalized = dragStartSelection.ExtentOffset >= dragStartSelection.BaseOffset;
            newSelection = new TextSelection(
                dragStartSelectionNormalized ? dragStartSelection.ExtentOffset : dragStartSelection.BaseOffset,
                position.Offset);
        }
        else
        {
            // On non-Apple platforms, dragging the base handle makes it the base.
            if (Selection.IsCollapsed)
            {
                SelectionOverlay.UpdateMagnifier(BuildMagnifier(
                    currentTextPosition: position,
                    globalGesturePosition: details.GlobalPosition,
                    renderEditable: RenderObject));
                HandleSelectionHandleChanged(TextSelection.FromPosition(position));
                return;
            }

            newSelection = new TextSelection(position.Offset, Selection.ExtentOffset);
            if (newSelection.BaseOffset >= newSelection.ExtentOffset)
            {
                return; // Don't allow order swapping.
            }
        }

        SelectionOverlay.UpdateMagnifier(BuildMagnifier(
            currentTextPosition: newSelection.Extent.Offset < newSelection.Base.Offset
                ? newSelection.Extent
                : newSelection.Base,
            globalGesturePosition: details.GlobalPosition,
            renderEditable: RenderObject));

        HandleSelectionHandleChanged(newSelection);
    }

    private void HandleAnyDragEnd(DragEndDetails details)
    {
        if (!Context.Mounted)
        {
            return;
        }

        _dragStartSelection = null;
        bool draggingHandles = SelectionOverlay.IsDraggingStartHandle || SelectionOverlay.IsDraggingEndHandle;
        if (SelectionControls is not ITextSelectionHandleControls)
        {
            if (!draggingHandles)
            {
                SelectionOverlay.HideMagnifier();
                if (!Selection.IsCollapsed)
                {
                    SelectionOverlay.ShowToolbar();
                }
            }

            return;
        }

        if (!draggingHandles)
        {
            SelectionOverlay.HideMagnifier();
            if (!Selection.IsCollapsed)
            {
                SelectionOverlay.ShowToolbar(context: Context, contextMenuBuilder: ContextMenuBuilder);
            }
        }
    }

    private void HandleSelectionHandleChanged(TextSelection newSelection)
    {
        TextEditingValue newValue = _value.CopyWith(selection: newSelection);
        SelectionDelegate.UserUpdateTextEditingValue(newValue, SelectionChangedCause.Drag);
    }

    private TextSelectionHandleType ChooseType(
        TextDirection textDirection,
        TextSelectionHandleType ltrType,
        TextSelectionHandleType rtlType)
    {
        if (Selection.IsCollapsed)
        {
            return TextSelectionHandleType.Collapsed;
        }

        return textDirection switch
        {
            TextDirection.Ltr => ltrType,
            TextDirection.Rtl => rtlType,
            _ => ltrType,
        };
    }
}

/// <summary>
/// An object that manages a pair of selection handles and a toolbar.
/// </summary>
/// <remarks>
/// Dart's <c>SelectionOverlay</c>. The handles are two <see cref="OverlayEntry"/>s in the root
/// overlay, placed through <see cref="CompositedTransformFollower"/>s that follow the start and end
/// handle <see cref="LayerLink"/>s; the toolbar is either the deprecated
/// <see cref="TextSelectionControls.BuildToolbar"/> entry or a context menu shown through a
/// <see cref="ContextMenuController"/>.
/// </remarks>
public sealed class SelectionOverlay : IDisposable
{
    private const string ContextMenuBuilderDeprecation =
        "Use `contextMenuBuilder` instead. This feature was deprecated after v3.3.0-0.5.pre.";

    private const string ToolbarLocationDeprecation =
        "Use the `contextMenuBuilder` parameter in `showToolbar` instead. "
        + "This feature was deprecated after v3.3.0-0.5.pre.";

    /// <summary>Controls the fade-in and fade-out animations for the toolbar and handles.</summary>
    public static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(150);

    private readonly ValueNotifier<MagnifierInfo> _magnifierInfo = new(MagnifierInfo.Empty);

    // The contrete magnifier controller; see MagnifierController.
    private readonly MagnifierController _magnifierController = new();

    private readonly ContextMenuController _contextMenuController = new();
    private readonly ContextMenuController _spellCheckToolbarController = new();

    private TextSelectionHandleType _startHandleType;
    private double _lineHeightAtStart;
    private bool _startHandleDragInProgress;
    private bool _isDraggingStartHandle;
    private TextSelectionHandleType _endHandleType;
    private double _lineHeightAtEnd;
    private bool _endHandleDragInProgress;
    private bool _isDraggingEndHandle;
    private IReadOnlyList<TextSelectionPoint> _selectionEndpoints;
    private Point? _toolbarLocation;

    // A pair of handles. If this is non-null, there are always 2, though the second is hidden when
    // the selection is collapsed. Dart's `_handles` record.
    private (OverlayEntry Start, OverlayEntry End)? _handles;

    // A copy/paste toolbar.
    private OverlayEntry? _toolbar;

    private bool _buildScheduled;

    /// <summary>Creates an object that manages overlay entries for selection handles.</summary>
    /// <remarks><paramref name="selectionDelegate"/> and <paramref name="toolbarLocation"/> are
    /// deprecated in Dart: pass a <c>contextMenuBuilder</c> to <see cref="ShowToolbar"/> instead.
    /// </remarks>
    public SelectionOverlay(
        BuildContext context,
        TextSelectionHandleType startHandleType,
        double lineHeightAtStart,
        TextSelectionHandleType endHandleType,
        double lineHeightAtEnd,
        IReadOnlyList<TextSelectionPoint> selectionEndpoints,
        TextSelectionControls? selectionControls,
        ITextSelectionDelegate? selectionDelegate,
        ClipboardStatusNotifier? clipboardStatus,
        LayerLink startHandleLayerLink,
        LayerLink endHandleLayerLink,
        LayerLink toolbarLayerLink,
        Widget? debugRequiredFor = null,
        IValueListenable<bool>? startHandlesVisible = null,
        Action<DragStartDetails>? onStartHandleDragStart = null,
        Action<DragUpdateDetails>? onStartHandleDragUpdate = null,
        Action<DragEndDetails>? onStartHandleDragEnd = null,
        IValueListenable<bool>? endHandlesVisible = null,
        Action<DragStartDetails>? onEndHandleDragStart = null,
        Action<DragUpdateDetails>? onEndHandleDragUpdate = null,
        Action<DragEndDetails>? onEndHandleDragEnd = null,
        IValueListenable<bool>? toolbarVisible = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        Action? onSelectionHandleTapped = null,
        Point? toolbarLocation = null,
        TextMagnifierConfiguration? magnifierConfiguration = null)
    {
        Debug.Assert(WidgetsDebug.DebugCheckHasOverlay(context));
        Context = context;
        DebugRequiredFor = debugRequiredFor;
        _startHandleType = startHandleType;
        _lineHeightAtStart = lineHeightAtStart;
        StartHandlesVisible = startHandlesVisible;
        OnStartHandleDragStart = onStartHandleDragStart;
        OnStartHandleDragUpdate = onStartHandleDragUpdate;
        OnStartHandleDragEnd = onStartHandleDragEnd;
        _endHandleType = endHandleType;
        _lineHeightAtEnd = lineHeightAtEnd;
        EndHandlesVisible = endHandlesVisible;
        OnEndHandleDragStart = onEndHandleDragStart;
        OnEndHandleDragUpdate = onEndHandleDragUpdate;
        OnEndHandleDragEnd = onEndHandleDragEnd;
        ToolbarVisible = toolbarVisible;
        _selectionEndpoints = selectionEndpoints;
        SelectionControls = selectionControls;
#pragma warning disable CS0618
        SelectionDelegate = selectionDelegate;
#pragma warning restore CS0618
        ClipboardStatus = clipboardStatus;
        StartHandleLayerLink = startHandleLayerLink;
        EndHandleLayerLink = endHandleLayerLink;
        ToolbarLayerLink = toolbarLayerLink;
        DragStartBehavior = dragStartBehavior;
        OnSelectionHandleTapped = onSelectionHandleTapped;
        _toolbarLocation = toolbarLocation;
        MagnifierConfiguration = magnifierConfiguration ?? TextMagnifierConfiguration.Disabled;
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchCreated("widgets", "SelectionOverlay", this);
        }
    }

    /// <summary>The context in which the selection UI should appear.</summary>
    public BuildContext Context { get; }

    /// <summary>The configuration for the magnifier; defaults to
    /// <see cref="TextMagnifierConfiguration.Disabled"/>.</summary>
    public TextMagnifierConfiguration MagnifierConfiguration { get; }

    /// <summary>Whether the toolbar is currently visible, including the spell check menu.</summary>
    public bool ToolbarIsVisible => SelectionControls is ITextSelectionHandleControls
        ? _contextMenuController.IsShown || _spellCheckToolbarController.IsShown
        : _toolbar is not null || _spellCheckToolbarController.IsShown;

    /// <summary>Whether the spell check menu is currently visible. Plumix-only access to what
    /// Dart's <c>TextSelectionOverlay</c> reads through the private controller.</summary>
    public bool SpellCheckToolbarIsVisible => _spellCheckToolbarController.IsShown;

    /// <summary>Whether the magnifier is currently visible.</summary>
    public bool MagnifierIsVisible => _magnifierController.Shown;

    /// <summary>Whether the magnifier currently exists; it may be hidden while it exists.</summary>
    public bool MagnifierExists => _magnifierController.OverlayEntry is not null;

    /// <summary>Whether the handle overlay entries are inserted. Dart reads its private
    /// <c>_handles != null</c>; Plumix exposes it for <see cref="TextSelectionOverlay"/>.</summary>
    public bool HandlesAreInserted => _handles is not null;

    /// <summary>
    /// Shows the magnifier, and hides the toolbar if it was showing when this was called. Does
    /// nothing if a magnifier is already shown, or if the configuration builds none.
    /// </summary>
    public void ShowMagnifier(MagnifierInfo initialMagnifierInfo)
    {
        if (_magnifierController.OverlayEntry is not null)
        {
            return;
        }

        if (ToolbarIsVisible)
        {
            HideToolbar();
        }

        // Start from empty, so we don't utilize any remnant values.
        _magnifierInfo.Value = initialMagnifierInfo;

        // Pre-build the magnifiers so we can tell if we've built something or not. If we don't
        // build something, this will be null.
        Widget? builtMagnifier = MagnifierConfiguration.MagnifierBuilder(
            Context,
            _magnifierController,
            _magnifierInfo);

        if (builtMagnifier is null)
        {
            return;
        }

        _ = _magnifierController.Show(
            Context,
            _ => builtMagnifier,
            below: MagnifierConfiguration.ShouldDisplayHandlesInMagnifier ? null : _handles?.Start);
    }

    /// <summary>Hides the magnifier, if one exists.</summary>
    public void HideMagnifier()
    {
        // This cannot be a check on `MagnifierController.Shown`, since the magnifier may be
        // hidden itself.
        if (_magnifierController.OverlayEntry is null)
        {
            return;
        }

        _ = _magnifierController.Hide();
    }

    /// <summary>The type of start selection handle.</summary>
    public TextSelectionHandleType StartHandleType
    {
        get => _startHandleType;
        set
        {
            if (_startHandleType == value)
            {
                return;
            }

            _startHandleType = value;
            MarkNeedsBuild();
        }
    }

    /// <summary>The line height at the selection start, used for sizing the start handle.
    /// </summary>
    public double LineHeightAtStart
    {
        get => _lineHeightAtStart;
        set
        {
            if (_lineHeightAtStart == value)
            {
                return;
            }

            _lineHeightAtStart = value;
            MarkNeedsBuild();
        }
    }

    /// <summary>Whether the start handle is being dragged by any pointer.</summary>
    public bool IsDraggingStartHandle => _isDraggingStartHandle || _startHandleDragInProgress;

    // On Apple platforms and the web only one handle can be dragged at a time.
    private bool CanDragStartHandle => !_isDraggingEndHandle || !OnlyOneHandleDragsAtATime;

    private bool CanDragEndHandle => !_isDraggingStartHandle || !OnlyOneHandleDragsAtATime;

    private static bool OnlyOneHandleDragsAtATime =>
        PlatformDefaults.TargetPlatform is TargetPlatform.IOS or TargetPlatform.MacOS || PlatformDefaults.IsWeb;

    /// <summary>Whether the start handle is visible. <see langword="null"/> means always visible.
    /// </summary>
    public IValueListenable<bool>? StartHandlesVisible { get; }

    /// <summary>Called when the user starts dragging the start selection handle.</summary>
    public Action<DragStartDetails>? OnStartHandleDragStart { get; }

    private void HandleStartHandleDragStart(DragStartDetails details)
    {
        Debug.Assert(!_isDraggingStartHandle);
        // Calling OnStartHandleDragStart in Dart's SelectableRegion can hide the handles, but the
        // entry is only removed in the next frame. Skip gestures that arrive in between.
        if (_handles is null)
        {
            _isDraggingStartHandle = false;
            return;
        }

        _startHandleDragInProgress = true;
        if (!CanDragStartHandle)
        {
            return;
        }

        _isDraggingStartHandle = details.Kind == PointerDeviceKind.Touch;
        OnStartHandleDragStart?.Invoke(details);
    }

    private void HandleStartHandleDragUpdate(DragUpdateDetails details)
    {
        // See HandleStartHandleDragStart.
        if (_handles is null)
        {
            _isDraggingStartHandle = false;
            return;
        }

        if (!CanDragStartHandle)
        {
            return;
        }

        if (!_isDraggingStartHandle)
        {
            // The start drag was blocked by the other handle, or was not a touch; start it now.
            _isDraggingStartHandle = details.Kind == PointerDeviceKind.Touch;
            OnStartHandleDragStart?.Invoke(new DragStartDetails(
                GlobalPosition: details.GlobalPosition,
                LocalPosition: details.LocalPosition,
                SourceTimeStampUtc: details.SourceTimeStampUtc,
                Kind: details.Kind));
        }

        OnStartHandleDragUpdate?.Invoke(details);
    }

    /// <summary>Called when the user drags the start selection handle to a new location.</summary>
    public Action<DragUpdateDetails>? OnStartHandleDragUpdate { get; }

    /// <summary>Called when the user ends dragging the start selection handle.</summary>
    public Action<DragEndDetails>? OnStartHandleDragEnd { get; }

    private void HandleStartHandleDragEnd(DragEndDetails details)
    {
        _isDraggingStartHandle = false;
        // See HandleStartHandleDragStart.
        if (_handles is null)
        {
            return;
        }

        _startHandleDragInProgress = false;
        if (!CanDragStartHandle)
        {
            return;
        }

        OnStartHandleDragEnd?.Invoke(details);
    }

    /// <summary>The type of end selection handle.</summary>
    public TextSelectionHandleType EndHandleType
    {
        get => _endHandleType;
        set
        {
            if (_endHandleType == value)
            {
                return;
            }

            _endHandleType = value;
            MarkNeedsBuild();
        }
    }

    /// <summary>The line height at the selection end, used for sizing the end handle.</summary>
    public double LineHeightAtEnd
    {
        get => _lineHeightAtEnd;
        set
        {
            if (_lineHeightAtEnd == value)
            {
                return;
            }

            _lineHeightAtEnd = value;
            MarkNeedsBuild();
        }
    }

    /// <summary>Whether the end handle is being dragged by any pointer.</summary>
    public bool IsDraggingEndHandle => _isDraggingEndHandle || _endHandleDragInProgress;

    /// <summary>Whether the end handle is visible. <see langword="null"/> means always visible.
    /// </summary>
    public IValueListenable<bool>? EndHandlesVisible { get; }

    /// <summary>Called when the user starts dragging the end selection handle.</summary>
    public Action<DragStartDetails>? OnEndHandleDragStart { get; }

    private void HandleEndHandleDragStart(DragStartDetails details)
    {
        Debug.Assert(!_isDraggingEndHandle);
        // See HandleStartHandleDragStart.
        if (_handles is null)
        {
            _isDraggingEndHandle = false;
            return;
        }

        _endHandleDragInProgress = true;
        if (!CanDragEndHandle)
        {
            return;
        }

        _isDraggingEndHandle = details.Kind == PointerDeviceKind.Touch;
        OnEndHandleDragStart?.Invoke(details);
    }

    private void HandleEndHandleDragUpdate(DragUpdateDetails details)
    {
        // See HandleStartHandleDragStart.
        if (_handles is null)
        {
            _isDraggingEndHandle = false;
            return;
        }

        if (!CanDragEndHandle)
        {
            return;
        }

        if (!_isDraggingEndHandle)
        {
            // The end drag was blocked by the other handle, or was not a touch; start it now.
            _isDraggingEndHandle = details.Kind == PointerDeviceKind.Touch;
            OnEndHandleDragStart?.Invoke(new DragStartDetails(
                GlobalPosition: details.GlobalPosition,
                LocalPosition: details.LocalPosition,
                SourceTimeStampUtc: details.SourceTimeStampUtc,
                Kind: details.Kind));
        }

        OnEndHandleDragUpdate?.Invoke(details);
    }

    /// <summary>Called when the user drags the end selection handle to a new location.</summary>
    public Action<DragUpdateDetails>? OnEndHandleDragUpdate { get; }

    /// <summary>Called when the user ends dragging the end selection handle.</summary>
    public Action<DragEndDetails>? OnEndHandleDragEnd { get; }

    private void HandleEndHandleDragEnd(DragEndDetails details)
    {
        _isDraggingEndHandle = false;
        // See HandleStartHandleDragStart.
        if (_handles is null)
        {
            return;
        }

        _endHandleDragInProgress = false;
        if (!CanDragEndHandle)
        {
            return;
        }

        OnEndHandleDragEnd?.Invoke(details);
    }

    /// <summary>Whether the toolbar is visible. <see langword="null"/> means always visible.
    /// </summary>
    public IValueListenable<bool>? ToolbarVisible { get; }

    /// <summary>The text selection positions of selection start and end.</summary>
    public IReadOnlyList<TextSelectionPoint> SelectionEndpoints
    {
        get => _selectionEndpoints;
        set
        {
            if (!_selectionEndpoints.SequenceEqual(value))
            {
                MarkNeedsBuild();
                if (_isDraggingEndHandle || _isDraggingStartHandle)
                {
                    switch (PlatformDefaults.TargetPlatform)
                    {
                        case TargetPlatform.Android:
                            _ = HapticFeedback.SelectionClick();
                            break;
                        case TargetPlatform.Fuchsia:
                        case TargetPlatform.IOS:
                        case TargetPlatform.Linux:
                        case TargetPlatform.MacOS:
                        case TargetPlatform.Windows:
                            break;
                    }
                }
            }

            _selectionEndpoints = value;
        }
    }

    /// <summary>Debugging information for explaining why the Overlay is required.</summary>
    public Widget? DebugRequiredFor { get; }

    /// <summary>The object supplied to the CompositedTransformTarget that wraps the text field.
    /// </summary>
    public LayerLink ToolbarLayerLink { get; }

    /// <summary>The objects supplied to the CompositedTransformTarget that wraps the location of
    /// the start selection handle.</summary>
    public LayerLink StartHandleLayerLink { get; }

    /// <summary>The objects supplied to the CompositedTransformTarget that wraps the location of
    /// the end selection handle.</summary>
    public LayerLink EndHandleLayerLink { get; }

    /// <summary>Builds text selection handles and toolbar.</summary>
    public TextSelectionControls? SelectionControls { get; }

    /// <summary>The delegate for manipulating the current selection in the owning text field.
    /// </summary>
    [Obsolete(ContextMenuBuilderDeprecation)]
    public ITextSelectionDelegate? SelectionDelegate { get; }

    /// <summary>Determines the way that drag start behavior is handled.</summary>
    public DragStartBehavior DragStartBehavior { get; }

    /// <summary>A callback that's optionally invoked when a selection handle is tapped.</summary>
    public Action? OnSelectionHandleTapped { get; }

    /// <summary>Maintains the status of the clipboard for determining if its contents can be
    /// pasted or not.</summary>
    public ClipboardStatusNotifier? ClipboardStatus { get; }

    /// <summary>The location of where the toolbar should be drawn in relative to the location of
    /// the toolbar layer link.</summary>
    [Obsolete(ToolbarLocationDeprecation)]
    public Point? ToolbarLocation
    {
        get => _toolbarLocation;
        set
        {
            if (Nullable.Equals(_toolbarLocation, value))
            {
                return;
            }

            _toolbarLocation = value;
            MarkNeedsBuild();
        }
    }

    /// <summary>Builds the handles by inserting them into the context's overlay.</summary>
    public void ShowHandles()
    {
        if (_handles is not null)
        {
            return;
        }

        OverlayState overlay = Overlay.Of(Context, rootOverlay: true, debugRequiredFor: DebugRequiredFor);

        CapturedThemes capturedThemes = InheritedTheme.Capture(Context, overlay.Context);

        var start = new OverlayEntry(context => capturedThemes.Wrap(BuildStartHandle(context)));
        var end = new OverlayEntry(context => capturedThemes.Wrap(BuildEndHandle(context)));
        _handles = (start, end);
        overlay.InsertAll([start, end]);
    }

    /// <summary>Destroys the handles by removing them from overlay.</summary>
    public void HideHandles()
    {
        if (_handles is not { } handles)
        {
            return;
        }

        handles.Start.Remove();
        handles.Start.Dispose();
        handles.End.Remove();
        handles.End.Dispose();
        _handles = null;
    }

    /// <summary>
    /// Shows the toolbar by inserting it into the context's overlay. Without a
    /// <paramref name="contextMenuBuilder"/> the deprecated
    /// <see cref="TextSelectionControls.BuildToolbar"/> is used.
    /// </summary>
    public void ShowToolbar(BuildContext? context = null, WidgetBuilder? contextMenuBuilder = null)
    {
        if (contextMenuBuilder is null)
        {
            if (_toolbar is not null)
            {
                return;
            }

            _toolbar = new OverlayEntry(BuildToolbar);
            Overlay.Of(Context, rootOverlay: true, debugRequiredFor: DebugRequiredFor)
                .Insert(_toolbar, above: _handles?.End);
            return;
        }

        if (context is null)
        {
            return;
        }

        var renderBox = (RenderBox)context.FindRenderObject()!;
        _contextMenuController.Show(
            context,
            menuContext => new SelectionToolbarWrapper(
                visibility: ToolbarVisible,
                layerLink: ToolbarLayerLink,
                offset: -renderBox.LocalToGlobal(default),
                child: contextMenuBuilder(menuContext)),
            debugRequiredFor: DebugRequiredFor);
    }

    /// <summary>Shows toolbar with spell check suggestions of misspelled words that are available
    /// for click-and-replace.</summary>
    public void ShowSpellCheckSuggestionsToolbar(BuildContext? context, WidgetBuilder builder)
    {
        if (context is null)
        {
            return;
        }

        var renderBox = (RenderBox)context.FindRenderObject()!;
        _spellCheckToolbarController.Show(
            context,
            menuContext => new SelectionToolbarWrapper(
                layerLink: ToolbarLayerLink,
                offset: -renderBox.LocalToGlobal(default),
                child: builder(menuContext)),
            debugRequiredFor: DebugRequiredFor);
    }

    /// <summary>Rebuilds the selection toolbar or handles if they are present.</summary>
    /// <remarks>During the persistent callbacks phase the rebuild is deferred to a single
    /// post-frame callback.</remarks>
    public void MarkNeedsBuild()
    {
        if (_handles is null && _toolbar is null)
        {
            return;
        }

        // If we are in build state, it will be too late to update visibility. We will need to
        // schedule the build in next frame.
        if (Scheduler.Phase == SchedulerPhase.PersistentCallbacks)
        {
            if (_buildScheduled)
            {
                return;
            }

            _buildScheduled = true;
            Scheduler.AddPostFrameCallback(
                _ =>
                {
                    _buildScheduled = false;
                    RebuildEntries();
                },
                debugLabel: "SelectionOverlay.markNeedsBuild");
        }
        else
        {
            RebuildEntries();
        }
    }

    private void RebuildEntries()
    {
        if (_handles is { } handles)
        {
            handles.Start.MarkNeedsBuild();
            handles.End.MarkNeedsBuild();
        }

        _toolbar?.MarkNeedsBuild();
        if (_contextMenuController.IsShown)
        {
            _contextMenuController.MarkNeedsBuild();
        }
        else if (_spellCheckToolbarController.IsShown)
        {
            _spellCheckToolbarController.MarkNeedsBuild();
        }
    }

    /// <summary>Hides the entire overlay including the toolbar and the handles.</summary>
    public void Hide()
    {
        _ = _magnifierController.Hide();
        HideHandles();
        if (_toolbar is not null || _contextMenuController.IsShown || _spellCheckToolbarController.IsShown)
        {
            HideToolbar();
        }
    }

    /// <summary>Hides the toolbar part of the overlay.</summary>
    public void HideToolbar()
    {
        _contextMenuController.Remove();
        _spellCheckToolbarController.Remove();
        if (_toolbar is null)
        {
            return;
        }

        _toolbar.Remove();
        _toolbar.Dispose();
        _toolbar = null;
    }

    /// <summary>Disposes this object and releases resources.</summary>
    public void Dispose()
    {
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchDisposed(this);
        }

        Hide();
        _magnifierInfo.Dispose();
    }

    internal Widget BuildStartHandle(BuildContext context)
    {
        Widget handle;
        TextSelectionControls? selectionControls = SelectionControls;
        if (selectionControls is null
            || (_startHandleType == TextSelectionHandleType.Collapsed && _isDraggingEndHandle))
        {
            // Hide the start handle when dragging the end handle and collapsing the selection.
            handle = new SizedBox();
        }
        else
        {
            handle = new SelectionHandleOverlay(
                type: _startHandleType,
                handleLayerLink: StartHandleLayerLink,
                onSelectionHandleTapped: OnSelectionHandleTapped,
                onSelectionHandleDragStart: HandleStartHandleDragStart,
                onSelectionHandleDragUpdate: HandleStartHandleDragUpdate,
                onSelectionHandleDragEnd: HandleStartHandleDragEnd,
                selectionControls: selectionControls,
                visibility: StartHandlesVisible,
                preferredLineHeight: _lineHeightAtStart,
                dragStartBehavior: DragStartBehavior);
        }

        return new TapRegion(
            groupId: typeof(SelectableRegion),
            child: new TextFieldTapRegion(child: new ExcludeSemantics(child: handle)));
    }

    internal Widget BuildEndHandle(BuildContext context)
    {
        Widget handle;
        TextSelectionControls? selectionControls = SelectionControls;
        if (selectionControls is null
            || (_endHandleType == TextSelectionHandleType.Collapsed && _isDraggingStartHandle)
            || (_endHandleType == TextSelectionHandleType.Collapsed
                && !_isDraggingStartHandle
                && !_isDraggingEndHandle))
        {
            // Hide the end handle when dragging the start handle and collapsing the selection or
            // when the selection is collapsed and no handle is being dragged.
            handle = new SizedBox();
        }
        else
        {
            handle = new SelectionHandleOverlay(
                type: _endHandleType,
                handleLayerLink: EndHandleLayerLink,
                onSelectionHandleTapped: OnSelectionHandleTapped,
                onSelectionHandleDragStart: HandleEndHandleDragStart,
                onSelectionHandleDragUpdate: HandleEndHandleDragUpdate,
                onSelectionHandleDragEnd: HandleEndHandleDragEnd,
                selectionControls: selectionControls,
                visibility: EndHandlesVisible,
                preferredLineHeight: _lineHeightAtEnd,
                dragStartBehavior: DragStartBehavior);
        }

        return new TapRegion(
            groupId: typeof(SelectableRegion),
            child: new TextFieldTapRegion(child: new ExcludeSemantics(child: handle)));
    }

    // Build the toolbar via TextSelectionControls.
    private Widget BuildToolbar(BuildContext context)
    {
        if (SelectionControls is null)
        {
            return new SizedBox();
        }

#pragma warning disable CS0618 // The legacy buildToolbar path is deprecated in Flutter too.
        ITextSelectionDelegate? selectionDelegate = SelectionDelegate;
        if (Constants.KDebugMode && selectionDelegate is null)
        {
            throw new AssertionError("If not using contextMenuBuilder, must pass selectionDelegate.");
        }

        var renderBox = (RenderBox)Context.FindRenderObject()!;

        Rect editingRegion = RectFromPoints(
            renderBox.LocalToGlobal(default),
            renderBox.LocalToGlobal(new Point(renderBox.Size.Width, renderBox.Size.Height)));

        bool isMultiline = _selectionEndpoints[^1].Point.Y - _selectionEndpoints[0].Point.Y
                           > _lineHeightAtEnd / 2;

        // If the selected text spans more than 1 line, horizontally center the toolbar. Derived
        // from both iOS and Android.
        double midX = isMultiline
            ? editingRegion.Width / 2
            : (_selectionEndpoints[0].Point.X + _selectionEndpoints[^1].Point.X) / 2;

        var midpoint = new Point(midX, _selectionEndpoints[0].Point.Y - _lineHeightAtStart);

        return new SelectionToolbarWrapper(
            visibility: ToolbarVisible,
            layerLink: ToolbarLayerLink,
            offset: -editingRegion.TopLeft,
            child: new Builder(builder: toolbarContext =>
                SelectionControls.BuildToolbar(
                    toolbarContext,
                    editingRegion,
                    _lineHeightAtStart,
                    midpoint,
                    _selectionEndpoints,
                    selectionDelegate!,
                    ClipboardStatus,
                    _toolbarLocation)));
#pragma warning restore CS0618
    }

    /// <summary>Update the current magnifier with new selection data, so the magnifier can respond
    /// accordingly.</summary>
    /// <remarks>If the magnifier is not shown, this still updates the magnifier position because
    /// the magnifier may have hidden itself and is looking for a cue to reshow itself.</remarks>
    public void UpdateMagnifier(MagnifierInfo magnifierInfo)
    {
        if (_magnifierController.OverlayEntry is null)
        {
            return;
        }

        _magnifierInfo.Value = magnifierInfo;
    }

    private static Rect RectFromPoints(Point a, Point b)
    {
        return new Rect(
            Math.Min(a.X, b.X),
            Math.Min(a.Y, b.Y),
            Math.Abs(a.X - b.X),
            Math.Abs(a.Y - b.Y));
    }
}

internal sealed class SelectionToolbarWrapper : StatefulWidget
{
    public SelectionToolbarWrapper(
        LayerLink layerLink,
        Point offset,
        Widget child,
        IValueListenable<bool>? visibility = null,
        Key? key = null) : base(key)
    {
        LayerLink = layerLink;
        Offset = offset;
        Child = child;
        Visibility = visibility;
    }

    public LayerLink LayerLink { get; }

    public Point Offset { get; }

    public Widget Child { get; }

    public IValueListenable<bool>? Visibility { get; }

    public override State CreateState() => new SelectionToolbarWrapperState();

    private sealed class SelectionToolbarWrapperState : State<SelectionToolbarWrapper>
    {
        private AnimationController? _controller;

        private SelectionToolbarWrapper CurrentWidget => (SelectionToolbarWrapper)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(duration: SelectionOverlay.FadeDuration, vsync: this);
            HandleVisibilityChanged();
            CurrentWidget.Visibility?.AddListener(HandleVisibilityChanged);
        }

        public override void DidUpdateWidget(SelectionToolbarWrapper oldWidget)
        {
            var previous = (SelectionToolbarWrapper)oldWidget;
            if (ReferenceEquals(previous.Visibility, CurrentWidget.Visibility))
            {
                return;
            }

            previous.Visibility?.RemoveListener(HandleVisibilityChanged);
            HandleVisibilityChanged();
            CurrentWidget.Visibility?.AddListener(HandleVisibilityChanged);
        }

        public override void Dispose()
        {
            CurrentWidget.Visibility?.RemoveListener(HandleVisibilityChanged);
            _controller!.Dispose();

            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return new TapRegion(
                groupId: typeof(SelectableRegion),
                child: new TextFieldTapRegion(
                    child: new Directionality(
                        textDirection: Directionality.Of(Context),
                        child: new FadeTransition(
                            opacity: _controller!,
                            child: new CompositedTransformFollower(
                                link: CurrentWidget.LayerLink,
                                showWhenUnlinked: false,
                                offset: new Point(CurrentWidget.Offset.X, CurrentWidget.Offset.Y),
                                child: CurrentWidget.Child)))));
        }

        private void HandleVisibilityChanged()
        {
            if (CurrentWidget.Visibility?.Value ?? true)
            {
                _controller!.Forward();
            }
            else
            {
                _controller!.Reverse();
            }
        }
    }
}

internal sealed class SelectionHandleOverlay : StatefulWidget
{
    public SelectionHandleOverlay(
        TextSelectionHandleType type,
        LayerLink handleLayerLink,
        TextSelectionControls selectionControls,
        double preferredLineHeight,
        Action<DragStartDetails>? onSelectionHandleDragStart = null,
        Action<DragUpdateDetails>? onSelectionHandleDragUpdate = null,
        Action<DragEndDetails>? onSelectionHandleDragEnd = null,
        Action? onSelectionHandleTapped = null,
        IValueListenable<bool>? visibility = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        Key? key = null) : base(key)
    {
        Type = type;
        HandleLayerLink = handleLayerLink;
        SelectionControls = selectionControls;
        PreferredLineHeight = preferredLineHeight;
        OnSelectionHandleDragStart = onSelectionHandleDragStart;
        OnSelectionHandleDragUpdate = onSelectionHandleDragUpdate;
        OnSelectionHandleDragEnd = onSelectionHandleDragEnd;
        OnSelectionHandleTapped = onSelectionHandleTapped;
        Visibility = visibility;
        DragStartBehavior = dragStartBehavior;
    }

    public TextSelectionHandleType Type { get; }

    public LayerLink HandleLayerLink { get; }

    public TextSelectionControls SelectionControls { get; }

    public double PreferredLineHeight { get; }

    public Action<DragStartDetails>? OnSelectionHandleDragStart { get; }

    public Action<DragUpdateDetails>? OnSelectionHandleDragUpdate { get; }

    public Action<DragEndDetails>? OnSelectionHandleDragEnd { get; }

    public Action? OnSelectionHandleTapped { get; }

    public IValueListenable<bool>? Visibility { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public override State CreateState() => new SelectionHandleOverlayState();

    private sealed class SelectionHandleOverlayState : State<SelectionHandleOverlay>
    {
        private static readonly IReadOnlySet<PointerDeviceKind> DragDevices = new HashSet<PointerDeviceKind>
        {
            PointerDeviceKind.Touch,
            PointerDeviceKind.Stylus,
            PointerDeviceKind.Unknown,
        };

        private AnimationController? _controller;

        private SelectionHandleOverlay CurrentWidget => (SelectionHandleOverlay)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(duration: SelectionOverlay.FadeDuration, vsync: this);
            HandleVisibilityChanged();
            CurrentWidget.Visibility?.AddListener(HandleVisibilityChanged);
        }

        public override void DidUpdateWidget(SelectionHandleOverlay oldWidget)
        {
            ((SelectionHandleOverlay)oldWidget).Visibility?.RemoveListener(HandleVisibilityChanged);
            HandleVisibilityChanged();
            CurrentWidget.Visibility?.AddListener(HandleVisibilityChanged);
        }

        public override void Dispose()
        {
            CurrentWidget.Visibility?.RemoveListener(HandleVisibilityChanged);
            _controller!.Dispose();

            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            SelectionHandleOverlay widget = CurrentWidget;
            Point handleAnchor = widget.SelectionControls.GetHandleAnchor(
                widget.Type,
                widget.PreferredLineHeight);
            Size handleSize = widget.SelectionControls.GetHandleSize(widget.PreferredLineHeight);

            Rect handleRect = new(0.0, 0.0, handleSize.Width, handleSize.Height);
            Rect interactiveRect = IsEmptyRect(handleRect)
                ? handleRect
                : handleRect.Union(RectFromCircle(
                    handleRect.Center,
                    WidgetConstants.MinInteractiveDimension / 2));
            Rendering.RelativeRect padding = IsEmptyRect(interactiveRect)
                ? new Rendering.RelativeRect(0.0, 0.0, 0.0, 0.0)
                : new Rendering.RelativeRect(
                    Left: Math.Max((interactiveRect.Width - handleRect.Width) / 2, 0),
                    Top: Math.Max((interactiveRect.Height - handleRect.Height) / 2, 0),
                    Right: Math.Max((interactiveRect.Width - handleRect.Width) / 2, 0),
                    Bottom: Math.Max((interactiveRect.Height - handleRect.Height) / 2, 0));

            // A drag directly on a collapsed handle must always win against other drag gestures.
            bool eagerlyAcceptDrag = widget.Type == TextSelectionHandleType.Collapsed
                                     && PlatformDefaults.TargetPlatform == TargetPlatform.IOS;

            return new CompositedTransformFollower(
                link: widget.HandleLayerLink,
                offset: new Point(-handleAnchor.X - padding.Left, -handleAnchor.Y - padding.Top),
                showWhenUnlinked: false,
                child: new FadeTransition(
                    opacity: _controller!,
                    child: new SizedBox(
                        width: interactiveRect.Width,
                        height: interactiveRect.Height,
                        child: new Align(
                            alignment: Alignment.TopLeft,
                            child: new RawGestureDetector(
                                excludeFromSemantics: true,
                                behavior: HitTestBehavior.Translucent,
                                gestures: new Dictionary<Type, IGestureRecognizerFactory>
                                {
                                    [typeof(PanGestureRecognizer)] =
                                        new GestureRecognizerFactoryWithHandlers<PanGestureRecognizer>(
                                            // Mouse events select the text and do not drag the cursor.
                                            () => new PanGestureRecognizer
                                            {
                                                DebugOwner = this,
                                                SupportedDevices = DragDevices,
                                            },
                                            instance =>
                                            {
                                                instance.DragStartBehavior = widget.DragStartBehavior;
                                                instance.GestureSettings = eagerlyAcceptDrag
                                                    ? new DeviceGestureSettings(TouchSlop: 1.0)
                                                    : null;
                                                instance.OnStart = widget.OnSelectionHandleDragStart;
                                                instance.OnUpdate = widget.OnSelectionHandleDragUpdate;
                                                instance.OnEnd = widget.OnSelectionHandleDragEnd;
                                            }),
                                },
                                child: new Padding(
                                    insets: new Thickness(
                                        padding.Left,
                                        padding.Top,
                                        padding.Right,
                                        padding.Bottom),
                                    child: widget.SelectionControls.BuildHandle(
                                        context,
                                        widget.Type,
                                        widget.PreferredLineHeight,
                                        widget.OnSelectionHandleTapped)))))));
        }

        private static bool IsEmptyRect(Rect rect) => rect.Width <= 0.0 || rect.Height <= 0.0;

        private static Rect RectFromCircle(Point center, double radius)
        {
            return new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        }

        private void HandleVisibilityChanged()
        {
            if (CurrentWidget.Visibility?.Value ?? true)
            {
                _controller!.Forward();
            }
            else
            {
                _controller!.Reverse();
            }
        }
    }
}
