using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/text_selection.dart

namespace Plumix.Widgets;

/// <summary>Which side of a selection a handle is drawn on.</summary>
// Dart parity source: flutter/packages/flutter/lib/src/rendering/selection.dart (TextSelectionHandleType)
public enum TextSelectionHandleType
{
    Left,
    Right,
    Collapsed,
}

/// <summary>A visual endpoint of a text selection, in the coordinate space of the edited text.</summary>
public readonly record struct TextSelectionPoint(Point Point, TextDirection? Direction);

public enum ClipboardStatus
{
    Pasteable,
    Unknown,
    NotPasteable,
}

/// <summary>Tracks whether the clipboard currently holds pasteable content.</summary>
public sealed class ClipboardStatusNotifier : ChangeNotifier, IValueListenable<ClipboardStatus>
{
    private ClipboardStatus _value;

    public ClipboardStatusNotifier(ClipboardStatus value = ClipboardStatus.Unknown)
    {
        _value = value;
    }

    public ClipboardStatus Value
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

    public void Update()
    {
        Value = string.IsNullOrEmpty(TextClipboard.GetText())
            ? ClipboardStatus.NotPasteable
            : ClipboardStatus.Pasteable;
    }
}

/// <summary>The editing surface a selection toolbar and its handles act on.</summary>
// Dart parity source: flutter/packages/flutter/lib/src/services/text_input.dart (TextSelectionDelegate)
public interface ITextSelectionDelegate
{
    TextEditingValue TextEditingValue { get; }

    bool CutEnabled => true;

    bool CopyEnabled => true;

    bool PasteEnabled => true;

    bool SelectAllEnabled => true;

    void UserUpdateTextEditingValue(TextEditingValue value, SelectionChangedCause? cause);

    void CutSelection(SelectionChangedCause cause);

    void CopySelection(SelectionChangedCause cause);

    void PasteText(SelectionChangedCause cause);

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
        @delegate.PasteText(SelectionChangedCause.Toolbar);
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
/// Owns the overlay entries that draw the two selection handles, the selection toolbar, and the
/// text magnifier for one editing surface.
/// </summary>
public sealed class SelectionOverlay : IDisposable
{
    /// <summary>Controls the fade-in and fade-out animations for the toolbar and handles.</summary>
    public static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(150);

    private readonly ValueNotifier<MagnifierInfo> _magnifierInfo = new(MagnifierInfo.Empty);
    private readonly MagnifierController _magnifierController = new();
    private readonly ContextMenuController _contextMenuController = new();

    private TextSelectionHandleType _startHandleType;
    private double _lineHeightAtStart;
    private TextSelectionHandleType _endHandleType;
    private double _lineHeightAtEnd;
    private IReadOnlyList<TextSelectionPoint> _selectionEndpoints;
    private Point? _toolbarLocation;

    private bool _isDraggingStartHandle;
    private bool _isDraggingEndHandle;
    private bool _startHandleDragInProgress;
    private bool _endHandleDragInProgress;

    private OverlayEntry? _startHandle;
    private OverlayEntry? _endHandle;
    private OverlayEntry? _toolbar;
    private bool _handlesInserted;

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
        SelectionDelegate = selectionDelegate;
        ClipboardStatus = clipboardStatus;
        StartHandleLayerLink = startHandleLayerLink;
        EndHandleLayerLink = endHandleLayerLink;
        ToolbarLayerLink = toolbarLayerLink;
        DragStartBehavior = dragStartBehavior;
        OnSelectionHandleTapped = onSelectionHandleTapped;
        _toolbarLocation = toolbarLocation;
        MagnifierConfiguration = magnifierConfiguration ?? TextMagnifierConfiguration.Disabled;
    }

    public BuildContext Context { get; }

    public Widget? DebugRequiredFor { get; }

    public IValueListenable<bool>? StartHandlesVisible { get; }

    public Action<DragStartDetails>? OnStartHandleDragStart { get; }

    public Action<DragUpdateDetails>? OnStartHandleDragUpdate { get; }

    public Action<DragEndDetails>? OnStartHandleDragEnd { get; }

    public IValueListenable<bool>? EndHandlesVisible { get; }

    public Action<DragStartDetails>? OnEndHandleDragStart { get; }

    public Action<DragUpdateDetails>? OnEndHandleDragUpdate { get; }

    public Action<DragEndDetails>? OnEndHandleDragEnd { get; }

    public IValueListenable<bool>? ToolbarVisible { get; }

    public TextSelectionControls? SelectionControls { get; }

    public ITextSelectionDelegate? SelectionDelegate { get; }

    public ClipboardStatusNotifier? ClipboardStatus { get; }

    public LayerLink StartHandleLayerLink { get; }

    public LayerLink EndHandleLayerLink { get; }

    public LayerLink ToolbarLayerLink { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public Action? OnSelectionHandleTapped { get; }

    public TextMagnifierConfiguration MagnifierConfiguration { get; }

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

    public double LineHeightAtStart
    {
        get => _lineHeightAtStart;
        set
        {
            if (_lineHeightAtStart.Equals(value))
            {
                return;
            }

            _lineHeightAtStart = value;
            MarkNeedsBuild();
        }
    }

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

    public double LineHeightAtEnd
    {
        get => _lineHeightAtEnd;
        set
        {
            if (_lineHeightAtEnd.Equals(value))
            {
                return;
            }

            _lineHeightAtEnd = value;
            MarkNeedsBuild();
        }
    }

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

    public IReadOnlyList<TextSelectionPoint> SelectionEndpoints
    {
        get => _selectionEndpoints;
        set
        {
            if (!_selectionEndpoints.SequenceEqual(value))
            {
                MarkNeedsBuild();
                if ((_isDraggingEndHandle || _isDraggingStartHandle)
                    && PlatformDefaults.TargetPlatform == TargetPlatform.Android)
                {
                    Feedback.ForSelectionClick();
                }
            }

            _selectionEndpoints = value;
        }
    }

    public bool IsDraggingStartHandle => _isDraggingStartHandle || _startHandleDragInProgress;

    public bool IsDraggingEndHandle => _isDraggingEndHandle || _endHandleDragInProgress;

    public bool HandlesAreInserted => _handlesInserted;

    public bool ToolbarIsVisible => SelectionControls is ITextSelectionHandleControls
        ? _contextMenuController.IsShown
        : _toolbar is not null;

    public bool MagnifierIsVisible => _magnifierController.Shown;

    public bool MagnifierExists => _magnifierController.OverlayEntry is not null;

    private bool CanDragStartHandle => !_isDraggingEndHandle || !IsApplePlatform;

    private bool CanDragEndHandle => !_isDraggingStartHandle || !IsApplePlatform;

    private static bool IsApplePlatform => PlatformDefaults.TargetPlatform is TargetPlatform.IOS
        or TargetPlatform.MacOS;

    public void ShowHandles()
    {
        if (_handlesInserted)
        {
            return;
        }

        OverlayState overlay = Overlay.Of(Context, rootOverlay: true);
        CapturedThemes capturedThemes = InheritedTheme.Capture(Context, overlay.Context);
        _startHandle = new OverlayEntry(context => capturedThemes.Wrap(BuildStartHandle(context)));
        _endHandle = new OverlayEntry(context => capturedThemes.Wrap(BuildEndHandle(context)));
        _handlesInserted = true;
        overlay.InsertAll([_startHandle, _endHandle]);
    }

    public void HideHandles()
    {
        if (!_handlesInserted)
        {
            return;
        }

        _handlesInserted = false;
        _startHandle!.Remove();
        _startHandle.Dispose();
        _startHandle = null;
        _endHandle!.Remove();
        _endHandle.Dispose();
        _endHandle = null;
    }

    public void ShowToolbar(BuildContext? context = null, WidgetBuilder? contextMenuBuilder = null)
    {
        if (contextMenuBuilder is null)
        {
            if (_toolbar is not null)
            {
                return;
            }

            _toolbar = new OverlayEntry(BuildToolbar);
            Overlay.Of(Context, rootOverlay: true).Insert(_toolbar);
            return;
        }

        if (context is null)
        {
            return;
        }

        _contextMenuController.Show(
            context.Value,
            menuContext => new SelectionToolbarWrapper(
                layerLink: ToolbarLayerLink,
                offset: -ResolveEditingRegion().TopLeft,
                visibility: ToolbarVisible,
                child: contextMenuBuilder(menuContext)));
    }

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

        _magnifierInfo.Value = initialMagnifierInfo;
        Widget? builtMagnifier = MagnifierConfiguration.MagnifierBuilder(
            Context,
            _magnifierController,
            _magnifierInfo);
        if (builtMagnifier is null)
        {
            return;
        }

        _ = _magnifierController.Show(Context, _ => builtMagnifier);
    }

    /// <summary>
    /// Updates the magnifier position even while it is hidden, because the magnifier may have hidden
    /// itself and is looking for a cue to reshow itself.
    /// </summary>
    public void UpdateMagnifier(MagnifierInfo magnifierInfo)
    {
        if (_magnifierController.OverlayEntry is null)
        {
            return;
        }

        _magnifierInfo.Value = magnifierInfo;
    }

    public void HideMagnifier()
    {
        if (_magnifierController.OverlayEntry is null)
        {
            return;
        }

        _ = _magnifierController.Hide();
    }

    public void Hide()
    {
        _ = _magnifierController.Hide();
        HideHandles();
        if (_toolbar is not null || _contextMenuController.IsShown)
        {
            HideToolbar();
        }
    }

    public void HideToolbar()
    {
        _contextMenuController.Remove();
        if (_toolbar is null)
        {
            return;
        }

        _toolbar.Remove();
        _toolbar.Dispose();
        _toolbar = null;
    }

    public void MarkNeedsBuild()
    {
        if (!_handlesInserted && _toolbar is null)
        {
            return;
        }

        _startHandle?.MarkNeedsBuild();
        _endHandle?.MarkNeedsBuild();
        _toolbar?.MarkNeedsBuild();
    }

    public void Dispose()
    {
        Hide();
        _magnifierInfo.Dispose();
    }

    internal Widget BuildStartHandle(BuildContext context)
    {
        // Hide the start handle when dragging the end handle and collapsing the selection.
        if (SelectionControls is null
            || (_startHandleType == TextSelectionHandleType.Collapsed && _isDraggingEndHandle))
        {
            return new SizedBox();
        }

        return WrapHandle(new SelectionHandleOverlay(
            type: _startHandleType,
            handleLayerLink: StartHandleLayerLink,
            onSelectionHandleTapped: OnSelectionHandleTapped,
            onSelectionHandleDragStart: HandleStartHandleDragStart,
            onSelectionHandleDragUpdate: HandleStartHandleDragUpdate,
            onSelectionHandleDragEnd: HandleStartHandleDragEnd,
            selectionControls: SelectionControls,
            visibility: StartHandlesVisible,
            preferredLineHeight: _lineHeightAtStart,
            dragStartBehavior: DragStartBehavior));
    }

    internal Widget BuildEndHandle(BuildContext context)
    {
        if (SelectionControls is null
            || (_endHandleType == TextSelectionHandleType.Collapsed && _isDraggingStartHandle)
            || (_endHandleType == TextSelectionHandleType.Collapsed
                && !_isDraggingStartHandle
                && !_isDraggingEndHandle))
        {
            return new SizedBox();
        }

        return WrapHandle(new SelectionHandleOverlay(
            type: _endHandleType,
            handleLayerLink: EndHandleLayerLink,
            onSelectionHandleTapped: OnSelectionHandleTapped,
            onSelectionHandleDragStart: HandleEndHandleDragStart,
            onSelectionHandleDragUpdate: HandleEndHandleDragUpdate,
            onSelectionHandleDragEnd: HandleEndHandleDragEnd,
            selectionControls: SelectionControls,
            visibility: EndHandlesVisible,
            preferredLineHeight: _lineHeightAtEnd,
            dragStartBehavior: DragStartBehavior));
    }

    private static Widget WrapHandle(Widget handle)
    {
        return new TapRegion(
            groupId: typeof(SelectableRegion),
            child: new TextFieldTapRegion(
                child: new ExcludeSemantics(child: handle)));
    }

    private Widget BuildToolbar(BuildContext context)
    {
        if (SelectionControls is null)
        {
            return new SizedBox();
        }

        if (SelectionDelegate is null)
        {
            throw new InvalidOperationException(
                "If not using contextMenuBuilder, must pass selectionDelegate.");
        }

        Rect editingRegion = ResolveEditingRegion();
        bool isMultiline = _selectionEndpoints[^1].Point.Y - _selectionEndpoints[0].Point.Y
                           > _lineHeightAtEnd / 2;
        double midX = isMultiline
            ? editingRegion.Width / 2
            : (_selectionEndpoints[0].Point.X + _selectionEndpoints[^1].Point.X) / 2;
        Point midpoint = new(midX, _selectionEndpoints[0].Point.Y - _lineHeightAtStart);

        return new SelectionToolbarWrapper(
            layerLink: ToolbarLayerLink,
            offset: -editingRegion.TopLeft,
            visibility: ToolbarVisible,
            child: new Builder(builder: toolbarContext =>
#pragma warning disable CS0618 // The legacy buildToolbar path is deprecated in Flutter too.
                SelectionControls.BuildToolbar(
                    toolbarContext,
                    editingRegion,
                    _lineHeightAtStart,
                    midpoint,
                    _selectionEndpoints,
                    SelectionDelegate,
                    ClipboardStatus,
                    _toolbarLocation)));
#pragma warning restore CS0618
    }

    private Rect ResolveEditingRegion()
    {
        if (Context.FindRenderObject() is not RenderBox renderBox
            || !renderBox.TryGetTransformFromRoot(out Matrix transform))
        {
            return default;
        }

        return RenderObject.TransformRect(transform, new Rect(default, renderBox.Size));
    }

    private void HandleStartHandleDragStart(DragStartDetails details)
    {
        if (!_handlesInserted)
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
        if (!_handlesInserted)
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
            _isDraggingStartHandle = details.Kind == PointerDeviceKind.Touch;
            OnStartHandleDragStart?.Invoke(new DragStartDetails(
                GlobalPosition: details.GlobalPosition,
                LocalPosition: details.LocalPosition,
                SourceTimeStampUtc: details.SourceTimeStampUtc,
                Kind: details.Kind));
        }

        OnStartHandleDragUpdate?.Invoke(details);
    }

    private void HandleStartHandleDragEnd(DragEndDetails details)
    {
        _isDraggingStartHandle = false;
        if (!_handlesInserted)
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

    private void HandleEndHandleDragStart(DragStartDetails details)
    {
        if (!_handlesInserted)
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
        if (!_handlesInserted)
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
            _isDraggingEndHandle = details.Kind == PointerDeviceKind.Touch;
            OnEndHandleDragStart?.Invoke(new DragStartDetails(
                GlobalPosition: details.GlobalPosition,
                LocalPosition: details.LocalPosition,
                SourceTimeStampUtc: details.SourceTimeStampUtc,
                Kind: details.Kind));
        }

        OnEndHandleDragUpdate?.Invoke(details);
    }

    private void HandleEndHandleDragEnd(DragEndDetails details)
    {
        _isDraggingEndHandle = false;
        if (!_handlesInserted)
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

    private sealed class SelectionToolbarWrapperState : State
    {
        private AnimationController? _controller;

        private SelectionToolbarWrapper CurrentWidget => (SelectionToolbarWrapper)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(SelectionOverlay.FadeDuration);
            HandleVisibilityChanged();
            CurrentWidget.Visibility?.AddListener(HandleVisibilityChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
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
                                offset: new Vector(CurrentWidget.Offset.X, CurrentWidget.Offset.Y),
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

    private sealed class SelectionHandleOverlayState : State
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
            _controller = new AnimationController(SelectionOverlay.FadeDuration);
            HandleVisibilityChanged();
            CurrentWidget.Visibility?.AddListener(HandleVisibilityChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            ((SelectionHandleOverlay)oldWidget).Visibility?.RemoveListener(HandleVisibilityChanged);
            HandleVisibilityChanged();
            CurrentWidget.Visibility?.AddListener(HandleVisibilityChanged);
        }

        public override void Dispose()
        {
            CurrentWidget.Visibility?.RemoveListener(HandleVisibilityChanged);
            _controller!.Dispose();
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
                offset: new Vector(-handleAnchor.X - padding.Left, -handleAnchor.Y - padding.Top),
                showWhenUnlinked: false,
                child: new FadeTransition(
                    opacity: _controller!,
                    child: new SizedBox(
                        width: interactiveRect.Width,
                        height: interactiveRect.Height,
                        child: new Align(
                            alignment: Alignment.TopLeft,
                            child: new RawGestureDetector(
                                behavior: HitTestBehavior.Translucent,
                                supportedDevices: DragDevices,
                                dragStartBehavior: widget.DragStartBehavior,
                                gestureSettings: eagerlyAcceptDrag
                                    ? new DeviceGestureSettings(TouchSlop: 1.0)
                                    : null,
                                onPanStart: widget.OnSelectionHandleDragStart,
                                onPanUpdate: widget.OnSelectionHandleDragUpdate,
                                onPanEnd: widget.OnSelectionHandleDragEnd,
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
