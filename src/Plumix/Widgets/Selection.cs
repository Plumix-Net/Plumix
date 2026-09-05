using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/selectable_region.dart

public enum SelectionChangedCause
{
    Tap,
    DoubleTap,
    LongPress,
    Drag,
    Keyboard,
    ForcePress,
    Toolbar,
    StylusHandwriting,
    [Obsolete("Use StylusHandwriting instead.")]
    Scribble = StylusHandwriting,
}

/// The status of the selection under a [SelectableRegion].
public enum SelectableRegionSelectionStatus
{
    /// The selection is changing.
    Changing,

    /// The selection is final.
    Finalized,
}

/// Notifies its listeners when the [SelectableRegionSelectionStatus] changes,
/// including when it is set to the same value.
internal sealed class SelectableRegionSelectionStatusNotifier
    : ChangeNotifier, IValueListenable<SelectableRegionSelectionStatus>
{
    private SelectableRegionSelectionStatus _value = SelectableRegionSelectionStatus.Finalized;

    public SelectableRegionSelectionStatus Value
    {
        get => _value;
        set
        {
            if (value == SelectableRegionSelectionStatus.Finalized
                && _value == SelectableRegionSelectionStatus.Finalized)
            {
                throw new InvalidOperationException(
                    "Attempting to finalize the selection when it is already finalized.");
            }

            _value = value;
            NotifyListeners();
        }
    }
}

/// Exposes the [SelectableRegionSelectionStatus] of the closest [SelectableRegion].
public sealed class SelectableRegionSelectionStatusScope : InheritedWidget
{
    internal SelectableRegionSelectionStatusScope(
        IValueListenable<SelectableRegionSelectionStatus> selectionStatusNotifier,
        Widget child,
        Key? key = null) : base(key)
    {
        SelectionStatusNotifier = selectionStatusNotifier;
        Child = child;
    }

    /// The [SelectableRegionSelectionStatus] of the ancestor [SelectableRegion].
    public IValueListenable<SelectableRegionSelectionStatus> SelectionStatusNotifier { get; }

    public Widget Child { get; }

    public static IValueListenable<SelectableRegionSelectionStatus>? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<SelectableRegionSelectionStatusScope>()?.SelectionStatusNotifier;
    }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(
            ((SelectableRegionSelectionStatusScope)oldWidget).SelectionStatusNotifier,
            SelectionStatusNotifier);
    }
}

/// A widget that introduces an area for user-driven content selection.
public sealed class SelectableRegion : StatefulWidget
{
    public SelectableRegion(
        Widget child,
        TextSelectionControls selectionControls,
        FocusNode? focusNode = null,
        SelectableRegionContextMenuBuilder? contextMenuBuilder = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        Action<SelectedContent?>? onSelectionChanged = null,
        MouseCursor? mouseCursor = null,
        Action? onTap = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        SelectionControls = selectionControls ?? throw new ArgumentNullException(nameof(selectionControls));
        FocusNode = focusNode;
        ContextMenuBuilder = contextMenuBuilder;
        MagnifierConfiguration = magnifierConfiguration ?? TextMagnifierConfiguration.Disabled;
        OnSelectionChanged = onSelectionChanged;
        MouseCursor = mouseCursor;
        OnTap = onTap;
    }

    public Widget Child { get; }
    public TextSelectionControls SelectionControls { get; }
    public FocusNode? FocusNode { get; }
    public SelectableRegionContextMenuBuilder? ContextMenuBuilder { get; }
    public TextMagnifierConfiguration MagnifierConfiguration { get; }
    public Action<SelectedContent?>? OnSelectionChanged { get; }
    public MouseCursor? MouseCursor { get; }
    public Action? OnTap { get; }

    /// Returns the [ContextMenuButtonItem]s representing the buttons in this
    /// platform's default selection menu.
    public static IReadOnlyList<ContextMenuButtonItem> GetSelectableButtonItems(
        SelectionGeometry selectionGeometry,
        Action onCopy,
        Action onSelectAll,
        Action? onShare)
    {
        bool canCopy = selectionGeometry.Status == SelectionStatus.Uncollapsed;
        bool canSelectAll = selectionGeometry.HasContent;
        bool platformCanShare = PlatformDefaults.TargetPlatform == TargetPlatform.Android
                                && selectionGeometry.Status == SelectionStatus.Uncollapsed;
        bool canShare = onShare is not null && platformCanShare;
        bool showShareBeforeSelectAll = PlatformDefaults.TargetPlatform == TargetPlatform.Android;

        var items = new List<ContextMenuButtonItem>();
        if (canCopy)
        {
            items.Add(new ContextMenuButtonItem(onCopy, ContextMenuButtonType.Copy));
        }

        if (canShare && showShareBeforeSelectAll)
        {
            items.Add(new ContextMenuButtonItem(onShare!, ContextMenuButtonType.Share));
        }

        if (canSelectAll)
        {
            items.Add(new ContextMenuButtonItem(onSelectAll, ContextMenuButtonType.SelectAll));
        }

        if (canShare && !showShareBeforeSelectAll)
        {
            items.Add(new ContextMenuButtonItem(onShare!, ContextMenuButtonType.Share));
        }

        return items;
    }

    public override State CreateState() => new SelectableRegionState();
}

public sealed class SelectableRegionState : State, ISelectionRegistrar
{
    /// Dart's `_kLongPressSelectionDevices`.
    private static readonly IReadOnlySet<PointerDeviceKind> LongPressSelectionDevices =
        new HashSet<PointerDeviceKind>
        {
            PointerDeviceKind.Touch,
            PointerDeviceKind.Stylus,
            PointerDeviceKind.InvertedStylus,
        };

    private readonly Dictionary<Type, IGestureRecognizerFactory> _gestureRecognizers = [];
    private readonly LayerLink _startHandleLayerLink = new();
    private readonly LayerLink _endHandleLayerLink = new();
    private readonly LayerLink _toolbarLayerLink = new();
    private readonly StaticSelectionContainerDelegate _selectionDelegate = new();
    private readonly SelectableRegionSelectionStatusNotifier _selectionStatusNotifier = new();

    private SelectionOverlay? _selectionOverlay;
    private ISelectable? _selectable;
    private SelectedContent? _lastSelectedContent;
    private FocusNode? _localFocusNode;
    private FocusNode? _attachedFocusNode;
    private Point? _lastSecondaryTapDownPosition;
    private PointerDeviceKind? _lastPointerDeviceKind;
    private Point? _doubleTapOffset;
    private Point? _selectionStartPosition;
    private Point? _selectionEndPosition;
    private bool _scheduledSelectionStartEdgeUpdate;
    private bool _scheduledSelectionEndEdgeUpdate;
    private Point _selectionStartHandleDragPosition;
    private Point _selectionEndHandleDragPosition;
    private Orientation? _lastOrientation;
    private bool _isShiftPressed;
    private bool? _adjustingSelectionEnd;
    private double? _directionalHorizontalBaseline;

    private SelectableRegion Current => (SelectableRegion)StateWidget;

    /// The selection overlay that owns the handles, toolbar and magnifier.
    internal SelectionOverlay? SelectionOverlay => _selectionOverlay;

    private FocusNode FocusNode => Current.FocusNode ?? (_localFocusNode ??= new FocusNode());

    private bool HasSelectionOverlayGeometry =>
        _selectionDelegate.Value.StartSelectionPoint is not null
        || _selectionDelegate.Value.EndSelectionPoint is not null;

    /// The current selected content, or null when nothing is selected.
    public SelectedContent? SelectedContent => _selectable?.GetSelectedContent();

    /// The line height at the start of the current selection.
    public double StartGlyphHeight => _selectionDelegate.Value.StartSelectionPoint!.LineHeight;

    /// The line height at the end of the current selection.
    public double EndGlyphHeight => _selectionDelegate.Value.EndSelectionPoint!.LineHeight;

    /// Whether the context menu is currently shown.
    public bool ContextMenuIsVisible => _selectionOverlay?.ToolbarIsVisible ?? false;

    /// The endpoints of the current selection, ordered top to bottom.
    public IReadOnlyList<TextSelectionPoint> SelectionEndpoints
    {
        get
        {
            SelectionPoint? start = _selectionDelegate.Value.StartSelectionPoint;
            SelectionPoint? end = _selectionDelegate.Value.EndSelectionPoint;
            if (start is null && end is null)
            {
                return [];
            }

            Point startLocalPosition = start?.LocalPosition ?? end!.LocalPosition;
            Point endLocalPosition = end?.LocalPosition ?? start!.LocalPosition;
            return startLocalPosition.Y > endLocalPosition.Y
                ?
                [
                    new TextSelectionPoint(endLocalPosition, TextDirection.Ltr),
                    new TextSelectionPoint(startLocalPosition, TextDirection.Ltr),
                ]
                :
                [
                    new TextSelectionPoint(startLocalPosition, TextDirection.Ltr),
                    new TextSelectionPoint(endLocalPosition, TextDirection.Ltr),
                ];
        }
    }

    /// The anchors the context menu should be positioned against.
    public TextSelectionToolbarAnchors ContextMenuAnchors
    {
        get
        {
            if (_lastSecondaryTapDownPosition is { } secondary)
            {
                _lastSecondaryTapDownPosition = null;
                return new TextSelectionToolbarAnchors(secondary, secondary);
            }

            IReadOnlyList<TextSelectionPoint> endpoints = SelectionEndpoints;
            if (endpoints.Count == 0 || Context.FindRenderObject() is not RenderBox renderBox)
            {
                return new TextSelectionToolbarAnchors(default, default);
            }

            Matrix4 transform = renderBox.GetTransformTo(null);
            Point primary = MatrixUtils.TransformPoint(
                transform,
                new Point(endpoints[0].Point.X, endpoints[0].Point.Y - StartGlyphHeight));
            Point secondaryAnchor = MatrixUtils.TransformPoint(transform, endpoints[^1].Point);
            return new TextSelectionToolbarAnchors(primary, secondaryAnchor);
        }
    }

    /// The buttons the platform's selection menu should show.
    public IReadOnlyList<ContextMenuButtonItem> ContextMenuButtonItems =>
        SelectableRegion.GetSelectableButtonItems(
            _selectionDelegate.Value,
            onCopy: () =>
            {
                CopySelection();
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                        ClearSelection();
                        SetChangingThenFinalize();
                        break;
                    case TargetPlatform.IOS:
                        HideToolbar(hideHandles: false);
                        break;
                    default:
                        HideToolbar();
                        break;
                }
            },
            onSelectAll: () =>
            {
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.IOS:
                    case TargetPlatform.Fuchsia:
                        SelectAll(SelectionChangedCause.Toolbar);
                        break;
                    default:
                        SelectAll();
                        HideToolbar();
                        break;
                }
            },
            onShare: null);

    public override void InitState()
    {
        base.InitState();
        AttachFocusNode();
        InitMouseGestureRecognizer();
        InitTouchGestureRecognizer();
        // Right clicks are recognized on every device kind.
        _gestureRecognizers[typeof(TapGestureRecognizer)] =
            new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                () => new TapGestureRecognizer { DebugOwner = this },
                instance => instance.OnSecondaryTapDown = HandleRightClickDown);
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        Orientation orientation = MediaQuery.OrientationOf(Context);
        if (_lastOrientation is { } lastOrientation && lastOrientation != orientation)
        {
            HideToolbar(hideHandles: PlatformDefaults.TargetPlatform == TargetPlatform.Android);
        }

        _lastOrientation = orientation;
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var previous = (SelectableRegion)oldWidget;
        if (!ReferenceEquals(previous.FocusNode, Current.FocusNode))
        {
            DetachFocusNode();
            AttachFocusNode();
        }

        if (previous.ContextMenuBuilder != Current.ContextMenuBuilder)
        {
            HideToolbar();
        }
    }

    public override void Dispose()
    {
        _selectable?.RemoveListener(UpdateSelectionStatus);
        _selectable?.PushHandleLayers(null, null);
        _selectionDelegate.Dispose();
        _selectionStatusNotifier.Dispose();
        // The magnifier is hidden explicitly so it cannot outlive the overlay entry.
        _selectionOverlay?.HideMagnifier();
        _selectionOverlay?.Dispose();
        _selectionOverlay = null;
        DetachFocusNode();
        _localFocusNode?.Dispose();
        _localFocusNode = null;
        base.Dispose();
    }

    // -- SelectionRegistrar ----------------------------------------------------

    public void Add(ISelectable selectable)
    {
        _selectable = selectable;
        _selectable.AddListener(UpdateSelectionStatus);
        _selectable.PushHandleLayers(_startHandleLayerLink, _endHandleLayerLink);
    }

    public void Remove(ISelectable selectable)
    {
        if (!ReferenceEquals(_selectable, selectable))
        {
            return;
        }

        _selectable.RemoveListener(UpdateSelectionStatus);
        _selectable.PushHandleLayers(null, null);
        _selectable = null;
    }

    // -- Public commands --------------------------------------------------------

    /// Selects the entire content of this region.
    public void SelectAll(SelectionChangedCause? cause = null)
    {
        ClearSelection();
        _selectable?.DispatchSelectionEvent(new SelectAllSelectionEvent());
        if (cause == SelectionChangedCause.Toolbar)
        {
            ShowHandles();
            ShowToolbar();
        }

        UpdateSelectedContentIfNeeded();
        SetChangingThenFinalize();
    }

    /// Clears the ongoing selection.
    public void ClearSelection()
    {
        FinalizeSelection();
        _directionalHorizontalBaseline = null;
        _adjustingSelectionEnd = null;
        _selectable?.DispatchSelectionEvent(new ClearSelectionEvent());
        UpdateSelectedContentIfNeeded();
    }

    /// Copies the current selection to the clipboard.
    public void CopySelection()
    {
        if (_selectable?.GetSelectedContent() is not { } data || data.PlainText.Length == 0)
        {
            return;
        }

        TextClipboard.SetText(data.PlainText);
    }

    /// Shows the selection handles, creating the overlay when the geometry allows it.
    public bool ShowHandles()
    {
        if (_selectionOverlay is { } existing)
        {
            existing.ShowHandles();
            return true;
        }

        if (!HasSelectionOverlayGeometry)
        {
            return false;
        }

        CreateSelectionOverlay();
        _selectionOverlay!.ShowHandles();
        return true;
    }

    /// Shows the context menu for the current selection.
    public bool ShowToolbar(Point? location = null)
    {
        if (!HasSelectionOverlayGeometry && _selectionOverlay is null)
        {
            return false;
        }

        if (Current.ContextMenuBuilder is null || ContextMenuButtonItems.Count == 0)
        {
            return false;
        }

        if (location is { } anchor)
        {
            _lastSecondaryTapDownPosition = anchor;
        }

        if (_selectionOverlay is null)
        {
            CreateSelectionOverlay();
        }

        _selectionOverlay!.ToolbarLocation = location;

        if (Current.SelectionControls is not ITextSelectionHandleControls)
        {
            _selectionOverlay.ShowToolbar();
            return true;
        }

        _selectionOverlay.HideToolbar();
        _selectionOverlay.ShowToolbar(
            context: Context,
            contextMenuBuilder: context => Current.ContextMenuBuilder!(context, this));
        return true;
    }

    /// Hides the context menu, and optionally the selection handles.
    public void HideToolbar(bool hideHandles = true)
    {
        _selectionOverlay?.HideToolbar();
        if (hideHandles)
        {
            _selectionOverlay?.HideHandles();
        }
    }

    // -- Selection primitives ---------------------------------------------------

    private void SelectStartTo(Point offset, bool continuous = false, TextGranularity? granularity = null)
    {
        if (!continuous)
        {
            _selectable?.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForStart(offset, granularity));
            return;
        }

        if (_selectionStartPosition != offset)
        {
            _selectionStartPosition = offset;
            TriggerSelectionStartEdgeUpdate(granularity);
        }
    }

    private void SelectEndTo(Point offset, bool continuous = false, TextGranularity? granularity = null)
    {
        if (!continuous)
        {
            _selectable?.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForEnd(offset, granularity));
            return;
        }

        if (_selectionEndPosition != offset)
        {
            _selectionEndPosition = offset;
            TriggerSelectionEndEdgeUpdate(granularity);
        }
    }

    /// <summary>
    /// Re-dispatches the pending start-edge update until the selectables stop reporting
    /// <see cref="SelectionResult.Pending"/>, matching Dart's post-frame retry loop.
    /// </summary>
    private void TriggerSelectionStartEdgeUpdate(TextGranularity? granularity = null)
    {
        if (_scheduledSelectionStartEdgeUpdate || _selectionStartPosition is not { } position)
        {
            return;
        }

        if (_selectable?.DispatchSelectionEvent(
                SelectionEdgeUpdateEvent.ForStart(position, granularity)) == SelectionResult.Pending)
        {
            _scheduledSelectionStartEdgeUpdate = true;
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (!_scheduledSelectionStartEdgeUpdate)
                {
                    return;
                }

                _scheduledSelectionStartEdgeUpdate = false;
                TriggerSelectionStartEdgeUpdate(granularity);
            });
        }
    }

    private void TriggerSelectionEndEdgeUpdate(TextGranularity? granularity = null)
    {
        if (_scheduledSelectionEndEdgeUpdate || _selectionEndPosition is not { } position)
        {
            return;
        }

        if (_selectable?.DispatchSelectionEvent(
                SelectionEdgeUpdateEvent.ForEnd(position, granularity)) == SelectionResult.Pending)
        {
            _scheduledSelectionEndEdgeUpdate = true;
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (!_scheduledSelectionEndEdgeUpdate)
                {
                    return;
                }

                _scheduledSelectionEndEdgeUpdate = false;
                TriggerSelectionEndEdgeUpdate(granularity);
            });
        }
    }

    private void FinalizeSelection()
    {
        _scheduledSelectionEndEdgeUpdate = false;
        _selectionEndPosition = null;
        _scheduledSelectionStartEdgeUpdate = false;
        _selectionStartPosition = null;
    }

    private void CollapseSelectionAt(Point offset)
    {
        FinalizeSelection();
        SelectStartTo(offset);
        SelectEndTo(offset);
    }

    private void SelectWordAt(Point offset)
    {
        FinalizeSelection();
        _selectable?.DispatchSelectionEvent(new SelectWordSelectionEvent(offset));
    }

    private void SelectParagraphAt(Point offset)
    {
        FinalizeSelection();
        _selectable?.DispatchSelectionEvent(new SelectParagraphSelectionEvent(offset));
    }

    private bool PositionIsOnActiveSelection(Point globalPosition)
    {
        if (_selectable is null)
        {
            return false;
        }

        Matrix4 transform = _selectable.GetTransformTo(null);
        foreach (Rect selectionRect in _selectionDelegate.Value.SelectionRects)
        {
            if (SelectionUtils.RectContains(RenderObject.TransformRect(transform, selectionRect), globalPosition))
            {
                return true;
            }
        }

        return false;
    }

    private void SetChangingThenFinalize()
    {
        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
        FinalizeSelectableRegionStatus();
    }

    /// Dart's `_finalizeSelectableRegionStatus`: only a changing selection can be finalized.
    private void FinalizeSelectableRegionStatus()
    {
        if (_selectionStatusNotifier.Value != SelectableRegionSelectionStatus.Changing)
        {
            return;
        }

        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Finalized;
    }

    private void UpdateSelectedContentIfNeeded()
    {
        if (Current.OnSelectionChanged is null)
        {
            return;
        }

        SelectedContent? content = _selectable?.GetSelectedContent();
        if (_lastSelectedContent?.PlainText != content?.PlainText)
        {
            _lastSelectedContent = content;
            Current.OnSelectionChanged(_lastSelectedContent);
        }
    }

    private void UpdateSelectionStatus()
    {
        if (HasSelectionOverlayGeometry)
        {
            UpdateSelectionOverlay();
        }
        else
        {
            _selectionOverlay?.Dispose();
            _selectionOverlay = null;
        }

        SetState(() => { });
    }

    // -- Selection overlay --------------------------------------------------------

    private void CreateSelectionOverlay()
    {
        if (_selectionOverlay is not null || !HasSelectionOverlayGeometry)
        {
            return;
        }

        SelectionPoint? start = _selectionDelegate.Value.StartSelectionPoint;
        SelectionPoint? end = _selectionDelegate.Value.EndSelectionPoint;
        _selectionOverlay = new SelectionOverlay(
            context: Context,
            debugRequiredFor: Current,
            startHandleType: start?.HandleType ?? TextSelectionHandleType.Collapsed,
            lineHeightAtStart: start?.LineHeight ?? end!.LineHeight,
            onStartHandleDragStart: HandleSelectionStartHandleDragStart,
            onStartHandleDragUpdate: HandleSelectionStartHandleDragUpdate,
            onStartHandleDragEnd: OnAnyDragEnd,
            endHandleType: end?.HandleType ?? TextSelectionHandleType.Collapsed,
            lineHeightAtEnd: end?.LineHeight ?? start!.LineHeight,
            onEndHandleDragStart: HandleSelectionEndHandleDragStart,
            onEndHandleDragUpdate: HandleSelectionEndHandleDragUpdate,
            onEndHandleDragEnd: OnAnyDragEnd,
            selectionEndpoints: SelectionEndpoints,
            selectionControls: Current.SelectionControls,
            selectionDelegate: null,
            clipboardStatus: null,
            startHandleLayerLink: _startHandleLayerLink,
            endHandleLayerLink: _endHandleLayerLink,
            toolbarLayerLink: _toolbarLayerLink,
            magnifierConfiguration: Current.MagnifierConfiguration);
    }

    private void UpdateSelectionOverlay()
    {
        if (_selectionOverlay is not { } overlay)
        {
            return;
        }

        SelectionPoint? start = _selectionDelegate.Value.StartSelectionPoint;
        SelectionPoint? end = _selectionDelegate.Value.EndSelectionPoint;
        overlay.StartHandleType = start?.HandleType ?? TextSelectionHandleType.Left;
        overlay.LineHeightAtStart = start?.LineHeight ?? end!.LineHeight;
        overlay.EndHandleType = end?.HandleType ?? TextSelectionHandleType.Right;
        overlay.LineHeightAtEnd = end?.LineHeight ?? start!.LineHeight;
        overlay.SelectionEndpoints = SelectionEndpoints;
    }

    private MagnifierInfo BuildInfoForMagnifier(Point globalGesturePosition, SelectionPoint selectionPoint)
    {
        Matrix4 transform = _selectable!.GetTransformTo(null);
        var globalTransformAsOffset = new Point(transform[12], transform[13]);
        Point position = selectionPoint.LocalPosition + globalTransformAsOffset;
        var caretRect = new Rect(
            position.X,
            position.Y - selectionPoint.LineHeight,
            0.0,
            selectionPoint.LineHeight);
        var fieldBounds = new Rect(globalTransformAsOffset, _selectable.Size);
        return new MagnifierInfo(
            GlobalGesturePosition: globalGesturePosition,
            CaretRect: caretRect,
            FieldBounds: fieldBounds,
            CurrentLineBoundaries: fieldBounds);
    }

    private void HandleSelectionStartHandleDragStart(DragStartDetails details)
    {
        SelectionPoint startPoint = _selectionDelegate.Value.StartSelectionPoint!;
        Matrix4 globalTransform = _selectable!.GetTransformTo(null);
        _selectionStartHandleDragPosition =
            MatrixUtils.TransformPoint(globalTransform, startPoint.LocalPosition);
        _selectionOverlay!.ShowMagnifier(BuildInfoForMagnifier(details.GlobalPosition, startPoint));
        UpdateSelectedContentIfNeeded();
    }

    private void HandleSelectionStartHandleDragUpdate(DragUpdateDetails details)
    {
        SelectionPoint startPoint = _selectionDelegate.Value.StartSelectionPoint!;
        _selectionStartHandleDragPosition += details.Delta;
        // The handle anchors at the paint origin; the selection edge sits at the line's centre.
        _selectionStartPosition = _selectionStartHandleDragPosition - new Point(0, startPoint.LineHeight / 2);
        TriggerSelectionStartEdgeUpdate();
        _selectionOverlay!.UpdateMagnifier(BuildInfoForMagnifier(details.GlobalPosition, startPoint));
        UpdateSelectedContentIfNeeded();
        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
    }

    private void HandleSelectionEndHandleDragStart(DragStartDetails details)
    {
        SelectionPoint endPoint = _selectionDelegate.Value.EndSelectionPoint!;
        Matrix4 globalTransform = _selectable!.GetTransformTo(null);
        _selectionEndHandleDragPosition =
            MatrixUtils.TransformPoint(globalTransform, endPoint.LocalPosition);
        _selectionOverlay!.ShowMagnifier(BuildInfoForMagnifier(details.GlobalPosition, endPoint));
        UpdateSelectedContentIfNeeded();
    }

    private void HandleSelectionEndHandleDragUpdate(DragUpdateDetails details)
    {
        SelectionPoint endPoint = _selectionDelegate.Value.EndSelectionPoint!;
        _selectionEndHandleDragPosition += details.Delta;
        _selectionEndPosition = _selectionEndHandleDragPosition - new Point(0, endPoint.LineHeight / 2);
        TriggerSelectionEndEdgeUpdate();
        _selectionOverlay!.UpdateMagnifier(BuildInfoForMagnifier(details.GlobalPosition, endPoint));
        UpdateSelectedContentIfNeeded();
        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
    }

    private void OnAnyDragEnd(DragEndDetails details)
    {
        bool draggingHandles = _selectionOverlay is { } overlay
                               && (overlay.IsDraggingStartHandle || overlay.IsDraggingEndHandle);
        if (!draggingHandles)
        {
            _selectionOverlay!.HideMagnifier();
            ShowToolbar();
        }

        FinalizeSelection();
        UpdateSelectedContentIfNeeded();
        FinalizeSelectableRegionStatus();
    }

    // -- Gestures ----------------------------------------------------------------

    /// Matches Dart's `_getEffectiveConsecutiveTapCount` platform table.
    private int GetEffectiveConsecutiveTapCount(int rawCount)
    {
        int maxConsecutiveTap = 3;
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
                if (_lastPointerDeviceKind is { } kind && kind != PointerDeviceKind.Mouse)
                {
                    maxConsecutiveTap = 2;
                }

                return rawCount <= maxConsecutiveTap
                    ? rawCount
                    : rawCount % maxConsecutiveTap == 0 ? maxConsecutiveTap : rawCount % maxConsecutiveTap;
            case TargetPlatform.Linux:
                return rawCount <= maxConsecutiveTap
                    ? rawCount
                    : rawCount % maxConsecutiveTap == 0 ? maxConsecutiveTap : rawCount % maxConsecutiveTap;
            default:
                return Math.Min(rawCount, maxConsecutiveTap);
        }
    }

    private static bool IsPrecisePointerDevice(PointerDeviceKind kind) => kind == PointerDeviceKind.Mouse;

    private static bool IsApplePlatform => PlatformDefaults.TargetPlatform
        is TargetPlatform.IOS or TargetPlatform.MacOS;

    private void InitMouseGestureRecognizer()
    {
        _gestureRecognizers[typeof(TapAndPanGestureRecognizer)] =
            new GestureRecognizerFactoryWithHandlers<TapAndPanGestureRecognizer>(
                () => new TapAndPanGestureRecognizer
                {
                    DebugOwner = this,
                    SupportedDevices = new HashSet<PointerDeviceKind> { PointerDeviceKind.Mouse },
                },
                instance =>
                {
                    instance.OnTapTrackStart = OnTapTrackStart;
                    instance.OnTapTrackReset = OnTapTrackReset;
                    instance.OnTapDown = StartNewMouseSelectionGesture;
                    instance.OnTapUp = HandleMouseTapUp;
                    instance.OnDragStart = HandleMouseDragStart;
                    instance.OnDragUpdate = HandleMouseDragUpdate;
                    instance.OnDragEnd = HandleMouseDragEnd;
                    instance.OnCancel = ClearSelection;
                    instance.DragStartBehavior = DragStartBehavior.Down;
                });
    }

    private void InitTouchGestureRecognizer()
    {
        // Only a horizontal drag is recognized on touch, so an ancestor vertical `Scrollable` still
        // wins a vertical drag. On iOS the scrollable's slop equals this recognizer's, so victory is
        // deferred until every other recognizer has lost.
        var nonMouseDevices = new HashSet<PointerDeviceKind>(
            Enum.GetValues<PointerDeviceKind>().Where(kind => kind != PointerDeviceKind.Mouse));
        _gestureRecognizers[typeof(TapAndHorizontalDragGestureRecognizer)] =
            new GestureRecognizerFactoryWithHandlers<TapAndHorizontalDragGestureRecognizer>(
                () => new TapAndHorizontalDragGestureRecognizer
                {
                    DebugOwner = this,
                    SupportedDevices = nonMouseDevices,
                },
                instance =>
                {
                    instance.EagerVictoryOnDrag = PlatformDefaults.TargetPlatform != TargetPlatform.IOS;
                    instance.OnTapDown = StartNewMouseSelectionGesture;
                    instance.OnTapUp = HandleMouseTapUp;
                    instance.OnDragStart = HandleMouseDragStart;
                    instance.OnDragUpdate = HandleMouseDragUpdate;
                    instance.OnDragEnd = HandleMouseDragEnd;
                    instance.OnCancel = ClearSelection;
                    instance.DragStartBehavior = DragStartBehavior.Down;
                });

        _gestureRecognizers[typeof(LongPressGestureRecognizer)] =
            new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                () => new LongPressGestureRecognizer
                {
                    DebugOwner = this,
                    SupportedDevices = LongPressSelectionDevices,
                },
                instance =>
                {
                    instance.OnLongPressStart = HandleTouchLongPressStart;
                    instance.OnLongPressMoveUpdate = HandleTouchLongPressMoveUpdate;
                    instance.OnLongPressEnd = HandleTouchLongPressEnd;
                });
    }

    private void OnTapTrackStart()
    {
        _isShiftPressed = HardwareKeyboard.Instance.IsShiftPressed;
    }

    private void OnTapTrackReset()
    {
        _isShiftPressed = false;
    }

    private void StartNewMouseSelectionGesture(TapDragDownDetails details)
    {
        _lastPointerDeviceKind = details.Kind;
        switch (GetEffectiveConsecutiveTapCount(details.ConsecutiveTapCount))
        {
            case 1:
                FocusNode.RequestFocus();
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                    case TargetPlatform.IOS:
                        // On mobile the selection is set on tap up.
                        break;
                    default:
                        HideToolbar();
                        if (_isShiftPressed && _selectionDelegate.Value.StartSelectionPoint is not null)
                        {
                            SelectEndTo(details.GlobalPosition);
                            _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                            break;
                        }

                        ClearSelection();
                        CollapseSelectionAt(details.GlobalPosition);
                        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        break;
                }

                break;
            case 2:
                if (PlatformDefaults.TargetPlatform == TargetPlatform.IOS)
                {
                    if (PlatformDefaults.IsWeb
                        && details.Kind is { } webKind
                        && !IsPrecisePointerDevice(webKind))
                    {
                        // iOS web defers the word selection to the drag that follows.
                        _doubleTapOffset = details.GlobalPosition;
                        break;
                    }

                    SelectWordAt(details.GlobalPosition);
                    _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                    if (details.Kind is { } iosKind && !IsPrecisePointerDevice(iosKind))
                    {
                        ShowHandles();
                    }

                    break;
                }

                SelectWordAt(details.GlobalPosition);
                _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                break;
            case 3:
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                    case TargetPlatform.IOS:
                        // A triple tap on mobile requires a precise pointer.
                        if (details.Kind is { } kind && IsPrecisePointerDevice(kind))
                        {
                            SelectParagraphAt(details.GlobalPosition);
                            _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        }

                        break;
                    default:
                        SelectParagraphAt(details.GlobalPosition);
                        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        break;
                }

                break;
        }

        UpdateSelectedContentIfNeeded();
    }

    private void HandleMouseTapUp(TapDragUpDetails details)
    {
        if (PlatformDefaults.TargetPlatform == TargetPlatform.IOS
            && PositionIsOnActiveSelection(details.GlobalPosition))
        {
            if (ContextMenuIsVisible)
            {
                HideToolbar(hideHandles: false);
            }
            else
            {
                ShowToolbar();
            }

            return;
        }

        switch (GetEffectiveConsecutiveTapCount(details.ConsecutiveTapCount))
        {
            case 1:
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                    case TargetPlatform.IOS:
                        HideToolbar();
                        CollapseSelectionAt(details.GlobalPosition);
                        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        break;
                }

                break;
            case 2:
                bool isPointerPrecise = IsPrecisePointerDevice(details.Kind);
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                        if (!isPointerPrecise)
                        {
                            ShowHandles();
                            ShowToolbar();
                        }

                        break;
                    case TargetPlatform.IOS:
                        if (!isPointerPrecise && !PlatformDefaults.IsWeb)
                        {
                            ShowToolbar();
                        }

                        break;
                }

                break;
        }

        FinalizeSelectableRegionStatus();
        UpdateSelectedContentIfNeeded();
        Current.OnTap?.Invoke();
    }

    private void HandleMouseDragStart(TapDragStartDetails details)
    {
        if (GetEffectiveConsecutiveTapCount(details.ConsecutiveTapCount) != 1)
        {
            return;
        }

        // Drag-to-select is only supported by precise pointers.
        if (details.Kind is { } kind && !IsPrecisePointerDevice(kind))
        {
            return;
        }

        SelectStartTo(details.GlobalPosition);
        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
        UpdateSelectedContentIfNeeded();
    }

    private void HandleMouseDragUpdate(TapDragUpdateDetails details)
    {
        switch (GetEffectiveConsecutiveTapCount(details.ConsecutiveTapCount))
        {
            case 1:
                if (details.Kind is { } dragKind && !IsPrecisePointerDevice(dragKind))
                {
                    return;
                }

                SelectEndTo(details.GlobalPosition, continuous: true);
                _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                break;
            case 2:
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                        if (!PlatformDefaults.IsWeb
                            || (details.Kind is { } androidKind && IsPrecisePointerDevice(androidKind)))
                        {
                            SelectEndTo(
                                details.GlobalPosition,
                                continuous: true,
                                granularity: TextGranularity.Word);
                            _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        }

                        break;
                    case TargetPlatform.IOS:
                        if (PlatformDefaults.IsWeb
                            && details.Kind is { } iosKind
                            && !IsPrecisePointerDevice(iosKind)
                            && _doubleTapOffset is { } doubleTapOffset)
                        {
                            SelectWordAt(doubleTapOffset);
                            _doubleTapOffset = null;
                        }

                        SelectEndTo(details.GlobalPosition, continuous: true, granularity: TextGranularity.Word);
                        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        if (details.Kind is { } handleKind && !IsPrecisePointerDevice(handleKind))
                        {
                            ShowHandles();
                        }

                        break;
                    default:
                        SelectEndTo(details.GlobalPosition, continuous: true, granularity: TextGranularity.Word);
                        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        break;
                }

                break;
            case 3:
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                    case TargetPlatform.IOS:
                        if (details.Kind is { } paragraphKind && IsPrecisePointerDevice(paragraphKind))
                        {
                            SelectEndTo(
                                details.GlobalPosition,
                                continuous: true,
                                granularity: TextGranularity.Paragraph);
                            _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        }

                        break;
                    default:
                        SelectEndTo(
                            details.GlobalPosition,
                            continuous: true,
                            granularity: TextGranularity.Paragraph);
                        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        break;
                }

                break;
        }

        UpdateSelectedContentIfNeeded();
    }

    private void HandleMouseDragEnd(TapDragEndDetails details)
    {
        // `TapDragEndDetails` carries no device kind, so the last tap-down kind decides.
        bool shouldShowSelectionOverlayOnMobile =
            _lastPointerDeviceKind is not { } kind || !IsPrecisePointerDevice(kind);
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
                if (shouldShowSelectionOverlayOnMobile)
                {
                    ShowHandles();
                    ShowToolbar();
                }

                break;
            case TargetPlatform.IOS:
                if (shouldShowSelectionOverlayOnMobile)
                {
                    ShowToolbar();
                }

                break;
        }

        FinalizeSelection();
        UpdateSelectedContentIfNeeded();
        FinalizeSelectableRegionStatus();
    }

    private void HandleTouchLongPressStart(LongPressStartDetails details)
    {
        HapticFeedback.SelectionClick();
        FocusNode.RequestFocus();
        SelectWordAt(details.GlobalPosition);
        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
        // Android shows the handles when the long press ends instead.
        if (PlatformDefaults.TargetPlatform != TargetPlatform.Android)
        {
            ShowHandles();
        }

        UpdateSelectedContentIfNeeded();
    }

    private void HandleTouchLongPressMoveUpdate(LongPressMoveUpdateDetails details)
    {
        SelectEndTo(details.GlobalPosition, granularity: TextGranularity.Word);
        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
        UpdateSelectedContentIfNeeded();
    }

    private void HandleTouchLongPressEnd(LongPressEndDetails details)
    {
        FinalizeSelection();
        UpdateSelectedContentIfNeeded();
        FinalizeSelectableRegionStatus();
        if (PlatformDefaults.TargetPlatform == TargetPlatform.Android)
        {
            ShowHandles();
        }

        ShowToolbar();
    }

    private void HandleRightClickDown(TapDownDetails details)
    {
        Point? previousSecondaryTapDownPosition = _lastSecondaryTapDownPosition;
        bool toolbarIsVisible = ContextMenuIsVisible;
        _lastSecondaryTapDownPosition = details.GlobalPosition;
        FocusNode.RequestFocus();
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.Windows:
                if (PositionIsOnActiveSelection(details.GlobalPosition))
                {
                    // `ContextMenuAnchors` consumes the recorded position, so it is set again here.
                    _lastSecondaryTapDownPosition = details.GlobalPosition;
                    ShowHandles();
                    ShowToolbar(_lastSecondaryTapDownPosition);
                    UpdateSelectedContentIfNeeded();
                    return;
                }

                CollapseSelectionAt(details.GlobalPosition);
                break;
            case TargetPlatform.IOS:
                SelectWordAt(details.GlobalPosition);
                break;
            case TargetPlatform.MacOS:
                if (previousSecondaryTapDownPosition == _lastSecondaryTapDownPosition && toolbarIsVisible)
                {
                    HideToolbar();
                    return;
                }

                SelectWordAt(details.GlobalPosition);
                break;
            case TargetPlatform.Linux:
                if (toolbarIsVisible)
                {
                    HideToolbar();
                    return;
                }

                if (!PositionIsOnActiveSelection(details.GlobalPosition))
                {
                    CollapseSelectionAt(details.GlobalPosition);
                }

                break;
        }

        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
        FinalizeSelectableRegionStatus();
        _lastSecondaryTapDownPosition = details.GlobalPosition;
        ShowHandles();
        ShowToolbar(_lastSecondaryTapDownPosition);
        UpdateSelectedContentIfNeeded();
    }

    // -- Keyboard -----------------------------------------------------------------

    private bool DetermineIsAdjustingSelectionEnd(bool forward)
    {
        if (_adjustingSelectionEnd is { } adjusting)
        {
            return adjusting;
        }

        SelectionPoint start = _selectionDelegate.Value.StartSelectionPoint!;
        SelectionPoint end = _selectionDelegate.Value.EndSelectionPoint!;
        bool isReversed;
        if (start.LocalPosition.Y > end.LocalPosition.Y)
        {
            isReversed = true;
        }
        else if (start.LocalPosition.Y < end.LocalPosition.Y)
        {
            isReversed = false;
        }
        else
        {
            isReversed = start.LocalPosition.X > end.LocalPosition.X;
        }

        _adjustingSelectionEnd = forward != isReversed;
        return _adjustingSelectionEnd.Value;
    }

    private void GranularlyExtendSelection(TextGranularity granularity, bool forward)
    {
        _directionalHorizontalBaseline = null;
        if (!_selectionDelegate.Value.HasSelection)
        {
            return;
        }

        _selectable?.DispatchSelectionEvent(new GranularlyExtendSelectionEvent(
            forward,
            DetermineIsAdjustingSelectionEnd(forward),
            granularity));
        UpdateSelectedContentIfNeeded();
        SetChangingThenFinalize();
    }

    private void DirectionallyExtendSelection(bool forward)
    {
        if (!_selectionDelegate.Value.HasSelection)
        {
            return;
        }

        bool adjustingSelectionExtend = DetermineIsAdjustingSelectionEnd(forward);
        SelectionPoint baseLinePoint = adjustingSelectionExtend
            ? _selectionDelegate.Value.EndSelectionPoint!
            : _selectionDelegate.Value.StartSelectionPoint!;
        _directionalHorizontalBaseline ??= baseLinePoint.LocalPosition.X;
        Point globalSelectionPointOffset = Context.FindRenderObject() is { } renderObject
            ? MatrixUtils.TransformPoint(
                renderObject.GetTransformTo(null),
                new Point(_directionalHorizontalBaseline.Value, 0))
            : new Point(_directionalHorizontalBaseline.Value, 0);
        _selectable?.DispatchSelectionEvent(new DirectionallyExtendSelectionEvent(
            globalSelectionPointOffset.X,
            _adjustingSelectionEnd!.Value,
            forward ? SelectionExtendDirection.NextLine : SelectionExtendDirection.PreviousLine));
        UpdateSelectedContentIfNeeded();
        SetChangingThenFinalize();
    }

    private IReadOnlyDictionary<Type, FlutterAction> BuildActions()
    {
        return new Dictionary<Type, FlutterAction>
        {
            [typeof(SelectAllTextIntent)] = new SelectAllAction(this),
            [typeof(CopySelectionTextIntent)] = new CopySelectionAction(this),
            [typeof(ExtendSelectionToNextWordBoundaryOrCaretLocationIntent)] =
                new GranularlyExtendSelectionAction<ExtendSelectionToNextWordBoundaryOrCaretLocationIntent>(
                    this, TextGranularity.Word),
            [typeof(ExpandSelectionToDocumentBoundaryIntent)] =
                new GranularlyExtendSelectionAction<ExpandSelectionToDocumentBoundaryIntent>(
                    this, TextGranularity.Document),
            [typeof(ExpandSelectionToLineBreakIntent)] =
                new GranularlyExtendSelectionAction<ExpandSelectionToLineBreakIntent>(this, TextGranularity.Line),
            [typeof(ExtendSelectionByCharacterIntent)] =
                new GranularlyExtendCaretSelectionAction<ExtendSelectionByCharacterIntent>(
                    this, TextGranularity.Character),
            [typeof(ExtendSelectionToNextWordBoundaryIntent)] =
                new GranularlyExtendCaretSelectionAction<ExtendSelectionToNextWordBoundaryIntent>(
                    this, TextGranularity.Word),
            [typeof(ExtendSelectionToLineBreakIntent)] =
                new GranularlyExtendCaretSelectionAction<ExtendSelectionToLineBreakIntent>(
                    this, TextGranularity.Line),
            [typeof(ExtendSelectionVerticallyToAdjacentLineIntent)] =
                new DirectionallyExtendCaretSelectionAction<ExtendSelectionVerticallyToAdjacentLineIntent>(this),
            [typeof(ExtendSelectionToDocumentBoundaryIntent)] =
                new GranularlyExtendCaretSelectionAction<ExtendSelectionToDocumentBoundaryIntent>(
                    this, TextGranularity.Document),
            [typeof(DismissIntent)] = new CallbackAction<DismissIntent>(HideToolbarIfVisible),
        };
    }

    private object? HideToolbarIfVisible(DismissIntent intent)
    {
        if (ContextMenuIsVisible)
        {
            HideToolbar(hideHandles: false);
            return null;
        }

        return Actions.Invoke(Context, intent);
    }

    /// An action that does not override any overridable action in the subtree.
    ///
    /// If this action is invoked by an action created with <see cref="FlutterAction.Overridable{T}"/>,
    /// it immediately invokes that action and does nothing else. Otherwise it calls
    /// <see cref="InvokeAction"/>. Ports Dart's private `_NonOverrideAction`.
    private abstract class NonOverrideAction<TIntent> : ContextAction<TIntent>
        where TIntent : Intent
    {
        protected abstract object? InvokeAction(TIntent intent, BuildContext? context);

        public sealed override object? Invoke(TIntent intent, BuildContext? context)
        {
            return CallingAction is { } callingAction
                ? callingAction.Invoke(intent)
                : InvokeAction(intent, context);
        }
    }

    /// Selects every selectable in the region.
    private sealed class SelectAllAction(SelectableRegionState state)
        : NonOverrideAction<SelectAllTextIntent>
    {
        protected override object? InvokeAction(SelectAllTextIntent intent, BuildContext? context)
        {
            state.SelectAll(SelectionChangedCause.Keyboard);
            return null;
        }
    }

    /// Copies the selection without clearing it, unlike the toolbar's copy button.
    private sealed class CopySelectionAction(SelectableRegionState state)
        : NonOverrideAction<CopySelectionTextIntent>
    {
        protected override object? InvokeAction(CopySelectionTextIntent intent, BuildContext? context)
        {
            state.CopySelection();
            return null;
        }
    }

    /// Extends the selection by one unit of the given granularity.
    private sealed class GranularlyExtendSelectionAction<TIntent>(
        SelectableRegionState state,
        TextGranularity granularity) : NonOverrideAction<TIntent>
        where TIntent : DirectionalTextEditingIntent
    {
        protected override object? InvokeAction(TIntent intent, BuildContext? context)
        {
            state.GranularlyExtendSelection(granularity, intent.Forward);
            return null;
        }
    }

    /// The caret-movement variant: a selectable region never collapses the selection.
    private sealed class GranularlyExtendCaretSelectionAction<TIntent>(
        SelectableRegionState state,
        TextGranularity granularity) : NonOverrideAction<TIntent>
        where TIntent : DirectionalCaretMovementIntent
    {
        protected override object? InvokeAction(TIntent intent, BuildContext? context)
        {
            if (intent.CollapseSelection)
            {
                return null;
            }

            state.GranularlyExtendSelection(granularity, intent.Forward);
            return null;
        }
    }

    /// Extends the selection to the adjacent line, keeping the horizontal baseline.
    private sealed class DirectionallyExtendCaretSelectionAction<TIntent>(SelectableRegionState state)
        : NonOverrideAction<TIntent>
        where TIntent : DirectionalCaretMovementIntent
    {
        protected override object? InvokeAction(TIntent intent, BuildContext? context)
        {
            if (intent.CollapseSelection)
            {
                return null;
            }

            state.DirectionallyExtendSelection(intent.Forward);
            return null;
        }
    }

    // -- Focus ---------------------------------------------------------------------

    private void AttachFocusNode()
    {
        _attachedFocusNode = FocusNode;
        _attachedFocusNode.AddListener(HandleFocusChanged);
    }

    private void DetachFocusNode()
    {
        _attachedFocusNode?.RemoveListener(HandleFocusChanged);
        _attachedFocusNode = null;
    }

    private void HandleFocusChanged()
    {
        if (_attachedFocusNode?.HasFocus == false)
        {
            ClearSelection();
            SetChangingThenFinalize();
        }

        SetState(() => { });
    }

    // -- Build -----------------------------------------------------------------------

    public override Widget Build(BuildContext context)
    {
        Widget result = new SelectableRegionSelectionStatusScope(
            _selectionStatusNotifier,
            new SelectionContainer(_selectionDelegate, Current.Child, registrar: this));

        // `Actions` sits outside `Focus` so a focused region resolves the text-editing intents that
        // the ambient `DefaultTextEditingShortcuts` produces.
        result = new Focus(
            focusNode: FocusNode,
            includeSemantics: false,
            child: result);

        result = new Actions(actions: BuildActions(), child: result);

        result = new RawGestureDetector(
            excludeFromSemantics: true,
            gestures: _gestureRecognizers,
            behavior: HitTestBehavior.Translucent,
            child: result);

        result = new CompositedTransformTarget(link: _toolbarLayerLink, child: result);

        // The handles share this group id, so dragging one is not a tap outside the region.
        result = new TapRegion(
            groupId: typeof(SelectableRegion),
            onTapOutside: _ =>
            {
                if (PlatformDefaults.IsWeb)
                {
                    FocusNode.Unfocus();
                }
            },
            child: result);

        return new MouseRegion(
            cursor: Current.MouseCursor ?? SystemMouseCursors.Text,
            child: result);
    }
}
