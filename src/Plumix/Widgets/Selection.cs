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
    private static readonly TimeSpan ConsecutiveTapTimeout = TimeSpan.FromMilliseconds(300);
    private const double ConsecutiveTapSlop = 100.0;

    private readonly LayerLink _startHandleLayerLink = new();
    private readonly LayerLink _endHandleLayerLink = new();
    private readonly LayerLink _toolbarLayerLink = new();
    private readonly StaticSelectionContainerDelegate _selectionDelegate = new();
    private readonly SelectableRegionSelectionStatusNotifier _selectionStatusNotifier = new();
    private readonly ContextMenuController _contextMenuController = new();

    private ISelectable? _selectable;
    private SelectedContent? _lastSelectedContent;
    private FocusNode? _localFocusNode;
    private FocusNode? _attachedFocusNode;
    private Point? _lastSecondaryTapDownPosition;
    private PointerDeviceKind? _lastPointerDeviceKind;
    private bool? _adjustingSelectionEnd;
    private double? _directionalHorizontalBaseline;
    private bool _showingToolbar;
    private int _consecutiveTapCount;
    private DateTime _lastTapTime;
    private Point _lastTapPosition;
    private bool _dragging;

    private SelectableRegion Current => (SelectableRegion)StateWidget;

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
    public bool ContextMenuIsVisible => _contextMenuController.IsShown;

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

            Matrix transform = renderBox.GetTransformTo(null);
            Point primary = transform.Transform(new Point(
                endpoints[0].Point.X,
                endpoints[0].Point.Y - StartGlyphHeight));
            Point secondaryAnchor = transform.Transform(endpoints[^1].Point);
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
        _contextMenuController.Hide();
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
            ShowToolbar();
        }

        UpdateSelectedContentIfNeeded();
        SetChangingThenFinalize();
    }

    /// Clears the ongoing selection.
    public void ClearSelection()
    {
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

    /// Shows the context menu for the current selection.
    public bool ShowToolbar(Point? location = null)
    {
        if (Current.ContextMenuBuilder is null || ContextMenuButtonItems.Count == 0)
        {
            return false;
        }

        if (location is { } anchor)
        {
            _lastSecondaryTapDownPosition = anchor;
        }

        // The menu is route-backed here (see `DIVERGENCES.md`), so pushing it hands
        // focus to the modal scope. That focus loss must not tear down the route
        // that is still being pushed, so re-entrant hides are ignored while showing.
        _showingToolbar = true;
        try
        {
            return _contextMenuController.Show(Context, context => Current.ContextMenuBuilder!(context, this));
        }
        finally
        {
            _showingToolbar = false;
        }
    }

    /// Hides the context menu, and optionally the selection handles.
    public void HideToolbar(bool hideHandles = true)
    {
        if (_showingToolbar)
        {
            return;
        }

        _contextMenuController.Hide();
    }

    // -- Selection primitives ---------------------------------------------------

    private void SelectStartTo(Point offset, TextGranularity? granularity = null)
    {
        _selectable?.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForStart(offset, granularity));
    }

    private void SelectEndTo(Point offset, TextGranularity? granularity = null)
    {
        _selectable?.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForEnd(offset, granularity));
    }

    private void CollapseSelectionAt(Point offset)
    {
        SelectStartTo(offset);
        SelectEndTo(offset);
    }

    private void SelectWordAt(Point offset)
    {
        _selectable?.DispatchSelectionEvent(new SelectWordSelectionEvent(offset));
    }

    private void SelectParagraphAt(Point offset)
    {
        _selectable?.DispatchSelectionEvent(new SelectParagraphSelectionEvent(offset));
    }

    private bool PositionIsOnActiveSelection(Point globalPosition)
    {
        if (_selectable is null)
        {
            return false;
        }

        Matrix transform = _selectable.GetTransformTo(null);
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
        if (_selectionStatusNotifier.Value == SelectableRegionSelectionStatus.Changing)
        {
            _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Finalized;
        }
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
        if (!HasSelectionOverlayGeometry)
        {
            HideToolbar();
        }

        SetState(() => { });
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

    private void HandleTapDown(PointerDownEvent @event)
    {
        _lastPointerDeviceKind = @event.Kind;
        DateTime now = @event.TimestampUtc;
        bool consecutive = now - _lastTapTime <= ConsecutiveTapTimeout
                           && Math.Abs(_lastTapPosition.X - @event.Position.X)
                           + Math.Abs(_lastTapPosition.Y - @event.Position.Y) <= ConsecutiveTapSlop;
        _consecutiveTapCount = consecutive ? _consecutiveTapCount + 1 : 1;
        _lastTapTime = now;
        _lastTapPosition = @event.Position;

        FocusNode.RequestFocus();
        switch (GetEffectiveConsecutiveTapCount(_consecutiveTapCount))
        {
            case 1:
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                    case TargetPlatform.IOS:
                        break;
                    default:
                        HideToolbar();
                        if (IsShiftPressed() && _selectionDelegate.Value.StartSelectionPoint is not null)
                        {
                            SelectEndTo(@event.Position);
                            _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                            break;
                        }

                        ClearSelection();
                        CollapseSelectionAt(@event.Position);
                        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        break;
                }

                break;
            case 2:
                SelectWordAt(@event.Position);
                _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                break;
            case 3:
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                    case TargetPlatform.IOS:
                        if (IsPrecisePointerDevice(@event.Kind))
                        {
                            SelectParagraphAt(@event.Position);
                            _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        }

                        break;
                    default:
                        SelectParagraphAt(@event.Position);
                        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                        break;
                }

                break;
        }

        UpdateSelectedContentIfNeeded();
    }

    private void HandleTapUp(PointerUpEvent @event)
    {
        if (PlatformDefaults.TargetPlatform == TargetPlatform.IOS && PositionIsOnActiveSelection(@event.Position))
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

        if (GetEffectiveConsecutiveTapCount(_consecutiveTapCount) != 1)
        {
            SetChangingThenFinalize();
            UpdateSelectedContentIfNeeded();
            return;
        }

        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.IOS:
                HideToolbar();
                CollapseSelectionAt(@event.Position);
                _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
                break;
        }

        SetChangingThenFinalize();
        UpdateSelectedContentIfNeeded();
        Current.OnTap?.Invoke();
    }

    private void HandleDragStart(DragStartDetails details)
    {
        _dragging = true;
        int tapCount = GetEffectiveConsecutiveTapCount(_consecutiveTapCount);
        if (tapCount != 1)
        {
            return;
        }

        if (_lastPointerDeviceKind is { } kind && !IsPrecisePointerDevice(kind))
        {
            return;
        }

        // `DragStartBehavior.Down`: the selection starts where the pointer went
        // down, not where the drag was recognized.
        SelectStartTo(_lastTapPosition);
        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
        UpdateSelectedContentIfNeeded();
    }

    private void HandleDragUpdate(DragUpdateDetails details)
    {
        if (!_dragging)
        {
            return;
        }

        int tapCount = GetEffectiveConsecutiveTapCount(_consecutiveTapCount);
        bool precise = _lastPointerDeviceKind is not { } kind || IsPrecisePointerDevice(kind);
        TextGranularity? granularity = tapCount switch
        {
            2 => TextGranularity.Word,
            3 => TextGranularity.Paragraph,
            _ => null,
        };
        if (tapCount == 1 && !precise)
        {
            return;
        }

        if (tapCount == 3 && !precise)
        {
            return;
        }

        SelectEndTo(details.GlobalPosition, granularity);
        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
        UpdateSelectedContentIfNeeded();
    }

    private void HandleDragEnd(DragEndDetails details)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        bool shouldShowSelectionOverlayOnMobile =
            _lastPointerDeviceKind is { } kind && !IsPrecisePointerDevice(kind);
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.IOS:
                if (shouldShowSelectionOverlayOnMobile)
                {
                    ShowToolbar();
                }

                break;
        }

        UpdateSelectedContentIfNeeded();
        SetChangingThenFinalize();
    }

    private void HandleLongPress()
    {
        HapticFeedback.SelectionClick();
        FocusNode.RequestFocus();
        SelectWordAt(_lastTapPosition);
        _selectionStatusNotifier.Value = SelectableRegionSelectionStatus.Changing;
        UpdateSelectedContentIfNeeded();
        SetChangingThenFinalize();
        ShowToolbar();
    }

    private void HandleRightClickDown(PointerDownEvent @event)
    {
        Point? previousSecondaryTapDownPosition = _lastSecondaryTapDownPosition;
        bool toolbarIsVisible = ContextMenuIsVisible;
        _lastSecondaryTapDownPosition = @event.Position;
        FocusNode.RequestFocus();
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.Windows:
                if (PositionIsOnActiveSelection(@event.Position))
                {
                    ShowToolbar(@event.Position);
                    UpdateSelectedContentIfNeeded();
                    return;
                }

                CollapseSelectionAt(@event.Position);
                break;
            case TargetPlatform.IOS:
                SelectWordAt(@event.Position);
                break;
            case TargetPlatform.MacOS:
                if (previousSecondaryTapDownPosition == _lastSecondaryTapDownPosition && toolbarIsVisible)
                {
                    HideToolbar();
                    return;
                }

                SelectWordAt(@event.Position);
                break;
            case TargetPlatform.Linux:
                if (toolbarIsVisible)
                {
                    HideToolbar();
                    return;
                }

                if (!PositionIsOnActiveSelection(@event.Position))
                {
                    CollapseSelectionAt(@event.Position);
                }

                break;
        }

        SetChangingThenFinalize();
        ShowToolbar(@event.Position);
        UpdateSelectedContentIfNeeded();
    }

    private static bool IsShiftPressed()
    {
        IReadOnlySet<string> pressed = HardwareKeyboard.Instance.LogicalKeysPressed;
        return pressed.Contains("Shift") || pressed.Contains("LeftShift") || pressed.Contains("RightShift");
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
            ? renderObject.GetTransformTo(null).Transform(new Point(_directionalHorizontalBaseline.Value, 0))
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
            [typeof(SelectAllTextIntent)] = new CallbackAction<SelectAllTextIntent>(_ =>
            {
                SelectAll(SelectionChangedCause.Keyboard);
                return null;
            }),
            [typeof(CopySelectionTextIntent)] = new CallbackAction<CopySelectionTextIntent>(_ =>
            {
                CopySelection();
                return null;
            }),
            [typeof(ExtendSelectionToNextWordBoundaryOrCaretLocationIntent)] =
                new CallbackAction<ExtendSelectionToNextWordBoundaryOrCaretLocationIntent>(intent =>
                {
                    GranularlyExtendSelection(TextGranularity.Word, intent.Forward);
                    return null;
                }),
            [typeof(ExpandSelectionToDocumentBoundaryIntent)] =
                new CallbackAction<ExpandSelectionToDocumentBoundaryIntent>(intent =>
                {
                    GranularlyExtendSelection(TextGranularity.Document, intent.Forward);
                    return null;
                }),
            [typeof(ExpandSelectionToLineBreakIntent)] =
                new CallbackAction<ExpandSelectionToLineBreakIntent>(intent =>
                {
                    GranularlyExtendSelection(TextGranularity.Line, intent.Forward);
                    return null;
                }),
            [typeof(ExtendSelectionByCharacterIntent)] =
                new CallbackAction<ExtendSelectionByCharacterIntent>(intent =>
                {
                    if (!intent.CollapseSelection)
                    {
                        GranularlyExtendSelection(TextGranularity.Character, intent.Forward);
                    }

                    return null;
                }),
            [typeof(ExtendSelectionToNextWordBoundaryIntent)] =
                new CallbackAction<ExtendSelectionToNextWordBoundaryIntent>(intent =>
                {
                    if (!intent.CollapseSelection)
                    {
                        GranularlyExtendSelection(TextGranularity.Word, intent.Forward);
                    }

                    return null;
                }),
            [typeof(ExtendSelectionToLineBreakIntent)] =
                new CallbackAction<ExtendSelectionToLineBreakIntent>(intent =>
                {
                    if (!intent.CollapseSelection)
                    {
                        GranularlyExtendSelection(TextGranularity.Line, intent.Forward);
                    }

                    return null;
                }),
            [typeof(ExtendSelectionToDocumentBoundaryIntent)] =
                new CallbackAction<ExtendSelectionToDocumentBoundaryIntent>(intent =>
                {
                    if (!intent.CollapseSelection)
                    {
                        GranularlyExtendSelection(TextGranularity.Document, intent.Forward);
                    }

                    return null;
                }),
            [typeof(ExtendSelectionVerticallyToAdjacentLineIntent)] =
                new CallbackAction<ExtendSelectionVerticallyToAdjacentLineIntent>(intent =>
                {
                    if (!intent.CollapseSelection)
                    {
                        DirectionallyExtendSelection(intent.Forward);
                    }

                    return null;
                }),
            [typeof(DismissIntent)] = new CallbackAction<DismissIntent>(_ =>
            {
                if (ContextMenuIsVisible)
                {
                    HideToolbar(hideHandles: false);
                }

                return null;
            }),
        };
    }

    /// The selection-relevant half of Dart's `DefaultTextEditingShortcuts`,
    /// scoped to this region until the full platform map is ported.
    private static IReadOnlyDictionary<ShortcutActivator, Intent> BuildShortcuts()
    {
        bool apple = PlatformDefaults.TargetPlatform is TargetPlatform.MacOS or TargetPlatform.IOS;
        var shortcuts = new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator("A", control: !apple, meta: apple)] =
                new SelectAllTextIntent(SelectionChangedCause.Keyboard),
            [new SingleActivator("C", control: !apple, meta: apple)] = CopySelectionTextIntent.Copy,
            [new SingleActivator("Left", shift: true)] =
                new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: false),
            [new SingleActivator("Right", shift: true)] =
                new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: false),
            [new SingleActivator("Left", shift: true, control: !apple, alt: apple)] =
                new ExtendSelectionToNextWordBoundaryIntent(forward: false, collapseSelection: false),
            [new SingleActivator("Right", shift: true, control: !apple, alt: apple)] =
                new ExtendSelectionToNextWordBoundaryIntent(forward: true, collapseSelection: false),
            [new SingleActivator("Left", shift: true, alt: !apple, meta: apple)] =
                new ExtendSelectionToLineBreakIntent(forward: false, collapseSelection: false),
            [new SingleActivator("Right", shift: true, alt: !apple, meta: apple)] =
                new ExtendSelectionToLineBreakIntent(forward: true, collapseSelection: false),
            [new SingleActivator("Up", shift: true)] =
                new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: false),
            [new SingleActivator("Down", shift: true)] =
                new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: false),
            [new SingleActivator("Up", shift: true, alt: !apple, meta: apple)] =
                new ExtendSelectionToDocumentBoundaryIntent(forward: false, collapseSelection: false),
            [new SingleActivator("Down", shift: true, alt: !apple, meta: apple)] =
                new ExtendSelectionToDocumentBoundaryIntent(forward: true, collapseSelection: false),
            [new SingleActivator("Escape")] = new DismissIntent(),
        };
        return shortcuts;
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
        // Flutter's context menu is an overlay entry and never takes focus, so the
        // selection survives while it is open. Plumix's menu is a route, so focus
        // moves to its modal scope; keep the selection in that case.
        if (_attachedFocusNode?.HasFocus == false && !_showingToolbar && !ContextMenuIsVisible)
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

        result = new Focus(
            focusNode: FocusNode,
            includeSemantics: false,
            child: result);

        result = new Actions(actions: BuildActions(), child: result);
        result = new Shortcuts(shortcuts: BuildShortcuts(), child: result);

        result = new RawGestureDetector(
            behavior: HitTestBehavior.Translucent,
            onTapDown: HandleTapDown,
            onTapUp: HandleTapUp,
            onLongPress: HandleLongPress,
            onSecondaryTapDown: HandleRightClickDown,
            onPanStart: HandleDragStart,
            onPanUpdate: HandleDragUpdate,
            onPanEnd: HandleDragEnd,
            dragStartBehavior: DragStartBehavior.Down,
            child: result);

        result = new CompositedTransformTarget(link: _toolbarLayerLink, child: result);

        return new MouseRegion(
            cursor: Current.MouseCursor ?? SystemMouseCursors.Text,
            child: result);
    }
}
