using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scrollable.dart

namespace Plumix.Widgets;

/// <summary>
/// Dart's private <c>_ScrollableSelectionHandler</c>: wraps a <see cref="Scrollable"/> in a
/// <see cref="SelectionContainer"/> whose delegate keeps a selection anchored to the scrolled
/// content and auto-scrolls while a selection edge is dragged past the viewport.
/// </summary>
/// <remarks>
/// Only built when the scrollable has an ancestor <see cref="ISelectionRegistrar"/>; see
/// <c>Scrollable.ScrollableState.Build</c>.
/// </remarks>
internal sealed class ScrollableSelectionHandler : StatefulWidget
{
    public ScrollableSelectionHandler(
        Scrollable.ScrollableState state,
        ScrollPosition position,
        Widget child,
        ISelectionRegistrar registrar,
        Key? key = null) : base(key)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        Position = position ?? throw new ArgumentNullException(nameof(position));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Registrar = registrar ?? throw new ArgumentNullException(nameof(registrar));
    }

    public Scrollable.ScrollableState State { get; }

    public ScrollPosition Position { get; }

    public Widget Child { get; }

    public ISelectionRegistrar Registrar { get; }

    public override State CreateState() => new ScrollableSelectionHandlerState();
}

internal sealed class ScrollableSelectionHandlerState : State
{
    private ScrollableSelectionContainerDelegate _selectionDelegate = null!;

    private ScrollableSelectionHandler Current => (ScrollableSelectionHandler)StateWidget;

    internal ScrollableSelectionContainerDelegate DelegateForTests => _selectionDelegate;

    public override void InitState()
    {
        base.InitState();
        _selectionDelegate = new ScrollableSelectionContainerDelegate(Current.State, Current.Position);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var previous = (ScrollableSelectionHandler)oldWidget;
        if (!ReferenceEquals(previous.Position, Current.Position))
        {
            _selectionDelegate.Position = Current.Position;
        }
    }

    public override void Dispose()
    {
        _selectionDelegate.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new SelectionContainer(
            @delegate: _selectionDelegate,
            child: Current.Child,
            registrar: Current.Registrar);
    }
}

/// <summary>
/// Dart's private <c>_ScrollableSelectionContainerDelegate</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every drag location it stores is expressed relative to the scroll origin — the raw global
/// position plus <see cref="Scrollable.ScrollableState.DeltaToScrollOrigin"/> — so it survives
/// scrolling; the current delta is subtracted again whenever an event is dispatched onward.
/// </para>
/// <para>
/// Selectables that scroll out of the viewport are destroyed and rebuilt, so the delegate records
/// the scroll offset at which each selectable last saw a start/end edge update and replays the
/// missed update from <see cref="EnsureChildUpdated"/> when the offset has moved on.
/// </para>
/// </remarks>
internal sealed class ScrollableSelectionContainerDelegate : MultiSelectableSelectionContainerDelegate
{
    /// The pointer drag is a single point, it should not have a size.
    private const double DefaultDragTargetSize = 0.0;

    /// An eyeballed value for a smooth scrolling speed.
    private const double DefaultSelectToScrollVelocityScalar = 30.0;

    private readonly EdgeDraggingAutoScroller _autoScroller;

    private readonly Dictionary<ISelectable, double> _selectableStartEdgeUpdateRecords = [];
    private readonly Dictionary<ISelectable, double> _selectableEndEdgeUpdateRecords = [];

    private bool _scheduledLayoutChange;
    private bool _disposed;
    private Point? _currentDragStartRelatedToOrigin;
    private Point? _currentDragEndRelatedToOrigin;

    /// The scrollable only auto scrolls if the selection starts in the scrollable.
    private bool _selectionStartsInScrollable;

    private ScrollPosition _position;

    public ScrollableSelectionContainerDelegate(Scrollable.ScrollableState state, ScrollPosition position)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        _position = position ?? throw new ArgumentNullException(nameof(position));
        _autoScroller = new EdgeDraggingAutoScroller(
            state,
            velocityScalar: DefaultSelectToScrollVelocityScalar);
        _position.AddListener(ScheduleLayoutChange);
    }

    public Scrollable.ScrollableState State { get; }

    internal bool IsAutoScrollingForTests => _autoScroller.IsAutoScrolling;

    public ScrollPosition Position
    {
        get => _position;
        set
        {
            if (ReferenceEquals(_position, value))
            {
                return;
            }

            _position.RemoveListener(ScheduleLayoutChange);
            _position = value;
            _position.AddListener(ScheduleLayoutChange);
        }
    }

    /// The layout is one frame behind the position change, so the geometry has to be recomputed
    /// after the frame that the change produced.
    private void ScheduleLayoutChange()
    {
        if (_scheduledLayoutChange || _disposed)
        {
            return;
        }

        _scheduledLayoutChange = true;
        Scheduler.AddPostFrameCallback(_ =>
        {
            if (!_scheduledLayoutChange || _disposed)
            {
                return;
            }

            _scheduledLayoutChange = false;
            LayoutDidChange();
        });
    }

    protected override void DidChangeSelectables()
    {
        var selectableSet = Selectables.ToHashSet();
        RemoveMissingRecords(_selectableStartEdgeUpdateRecords, selectableSet);
        RemoveMissingRecords(_selectableEndEdgeUpdateRecords, selectableSet);
        base.DidChangeSelectables();
    }

    private static void RemoveMissingRecords(
        Dictionary<ISelectable, double> records,
        HashSet<ISelectable> selectableSet)
    {
        foreach (ISelectable selectable in records.Keys.Where(key => !selectableSet.Contains(key)).ToList())
        {
            records.Remove(selectable);
        }
    }

    protected override SelectionResult HandleClearSelection(ClearSelectionEvent @event)
    {
        _selectableStartEdgeUpdateRecords.Clear();
        _selectableEndEdgeUpdateRecords.Clear();
        _currentDragStartRelatedToOrigin = null;
        _currentDragEndRelatedToOrigin = null;
        _selectionStartsInScrollable = false;
        return base.HandleClearSelection(@event);
    }

    protected override SelectionResult HandleSelectionEdgeUpdate(SelectionEdgeUpdateEvent @event)
    {
        if (_currentDragEndRelatedToOrigin is null && _currentDragStartRelatedToOrigin is null)
        {
            Debug.Assert(!_selectionStartsInScrollable);
            _selectionStartsInScrollable = GlobalPositionInScrollable(@event.GlobalPosition);
        }

        Point deltaToOrigin = GetDeltaToScrollOrigin(State);
        if (@event.Type == SelectionEventType.EndEdgeUpdate)
        {
            _currentDragEndRelatedToOrigin = InferPositionRelatedToOrigin(@event.GlobalPosition);
            Point endOffset = Translate(_currentDragEndRelatedToOrigin.Value, -deltaToOrigin.X, -deltaToOrigin.Y);
            @event = SelectionEdgeUpdateEvent.ForEnd(endOffset, @event.Granularity);
        }
        else
        {
            _currentDragStartRelatedToOrigin = InferPositionRelatedToOrigin(@event.GlobalPosition);
            Point startOffset = Translate(_currentDragStartRelatedToOrigin.Value, -deltaToOrigin.X, -deltaToOrigin.Y);
            @event = SelectionEdgeUpdateEvent.ForStart(startOffset, @event.Granularity);
        }

        SelectionResult result = base.HandleSelectionEdgeUpdate(@event);

        // The result may be pending if the selection is not fully initialized: the child that
        // received the event may itself be a scrollable that is still scrolling.
        if (result == SelectionResult.Pending)
        {
            _autoScroller.StopAutoScroll();
            return result;
        }

        if (_selectionStartsInScrollable)
        {
            _autoScroller.StartAutoScrollIfNecessary(DragTargetFromEvent(@event));
            if (_autoScroller.IsAutoScrolling)
            {
                return SelectionResult.Pending;
            }
        }

        return result;
    }

    private Point InferPositionRelatedToOrigin(Point globalPosition)
    {
        var box = (RenderBox)State.Context.FindRenderObject()!;
        Point localPosition = box.GlobalToLocal(globalPosition);
        if (!_selectionStartsInScrollable)
        {
            // If the selection starts outside of the scrollable, selecting across the entire
            // scrollable should select the entire content of the scrollable.
            if (localPosition.Y < 0 || localPosition.X < 0)
            {
                return box.LocalToGlobal(new Point());
            }

            if (localPosition.Y > box.Size.Height || localPosition.X > box.Size.Width)
            {
                return Infinite;
            }
        }

        Point deltaToOrigin = GetDeltaToScrollOrigin(State);
        return box.LocalToGlobal(Translate(localPosition, deltaToOrigin.X, deltaToOrigin.Y));
    }

    /// Infers the <see cref="_currentDragStartRelatedToOrigin"/> and
    /// <see cref="_currentDragEndRelatedToOrigin"/> from the geometries, for selections that were
    /// not driven by a drag (select-all, select-word, keyboard extension).
    private void UpdateDragLocationsFromGeometries(bool forceUpdateStart = true, bool forceUpdateEnd = true)
    {
        Point deltaToOrigin = GetDeltaToScrollOrigin(State);
        var box = (RenderBox)State.Context.FindRenderObject()!;
        Matrix4 transform = box.GetTransformTo(null);
        if (CurrentSelectionStartIndex != -1 && (_currentDragStartRelatedToOrigin is null || forceUpdateStart))
        {
            SelectionGeometry geometry = Selectables[CurrentSelectionStartIndex].Value;
            Debug.Assert(geometry.HasSelection);
            SelectionPoint start = geometry.StartSelectionPoint!;
            Matrix4 childTransform = Selectables[CurrentSelectionStartIndex].GetTransformTo(box);
            Point localDragStart = MatrixUtils.TransformPoint(
                childTransform,
                Translate(start.LocalPosition, 0, -start.LineHeight / 2));
            _currentDragStartRelatedToOrigin = MatrixUtils.TransformPoint(
                transform,
                Translate(localDragStart, deltaToOrigin.X, deltaToOrigin.Y));
        }

        if (CurrentSelectionEndIndex != -1 && (_currentDragEndRelatedToOrigin is null || forceUpdateEnd))
        {
            SelectionGeometry geometry = Selectables[CurrentSelectionEndIndex].Value;
            Debug.Assert(geometry.HasSelection);
            SelectionPoint end = geometry.EndSelectionPoint!;
            Matrix4 childTransform = Selectables[CurrentSelectionEndIndex].GetTransformTo(box);
            Point localDragEnd = MatrixUtils.TransformPoint(
                childTransform,
                Translate(end.LocalPosition, 0, -end.LineHeight / 2));
            _currentDragEndRelatedToOrigin = MatrixUtils.TransformPoint(
                transform,
                Translate(localDragEnd, deltaToOrigin.X, deltaToOrigin.Y));
        }
    }

    protected override SelectionResult HandleSelectAll(SelectAllSelectionEvent @event)
    {
        Debug.Assert(!_selectionStartsInScrollable);
        SelectionResult result = base.HandleSelectAll(@event);
        Debug.Assert((CurrentSelectionStartIndex == -1) == (CurrentSelectionEndIndex == -1));
        if (CurrentSelectionStartIndex != -1)
        {
            UpdateDragLocationsFromGeometries();
        }

        return result;
    }

    protected override SelectionResult HandleSelectWord(SelectWordSelectionEvent @event)
    {
        _selectionStartsInScrollable = GlobalPositionInScrollable(@event.GlobalPosition);
        SelectionResult result = base.HandleSelectWord(@event);
        UpdateDragLocationsFromGeometries();
        return result;
    }

    protected override SelectionResult HandleGranularlyExtendSelection(GranularlyExtendSelectionEvent @event)
    {
        SelectionResult result = base.HandleGranularlyExtendSelection(@event);
        // The selection geometry may not have the accurate offset for the edges that are outside
        // of the viewport yet. Only the edge this event is updating is trustworthy.
        UpdateDragLocationsFromGeometries(forceUpdateStart: !@event.IsEnd, forceUpdateEnd: @event.IsEnd);
        if (_selectionStartsInScrollable)
        {
            JumpToEdge(@event.IsEnd);
        }

        return result;
    }

    protected override SelectionResult HandleDirectionallyExtendSelection(DirectionallyExtendSelectionEvent @event)
    {
        SelectionResult result = base.HandleDirectionallyExtendSelection(@event);
        UpdateDragLocationsFromGeometries(forceUpdateStart: !@event.IsEnd, forceUpdateEnd: @event.IsEnd);
        if (_selectionStartsInScrollable)
        {
            JumpToEdge(@event.IsEnd);
        }

        return result;
    }

    private void JumpToEdge(bool isExtent)
    {
        ISelectable selectable;
        double? lineHeight;
        SelectionPoint? edge;
        if (isExtent)
        {
            selectable = Selectables[CurrentSelectionEndIndex];
            edge = selectable.Value.EndSelectionPoint;
            lineHeight = selectable.Value.EndSelectionPoint?.LineHeight;
        }
        else
        {
            selectable = Selectables[CurrentSelectionStartIndex];
            edge = selectable.Value.StartSelectionPoint;
            lineHeight = selectable.Value.StartSelectionPoint?.LineHeight;
        }

        if (lineHeight is null || edge is null)
        {
            return;
        }

        var scrollableBox = (RenderBox)State.Context.FindRenderObject()!;
        Matrix4 transform = selectable.GetTransformTo(scrollableBox);
        Point edgeOffsetInScrollableCoordinates = MatrixUtils.TransformPoint(transform, edge.LocalPosition);
        var scrollableRect = new Rect(
            0,
            0,
            scrollableBox.Size.Width,
            scrollableBox.Size.Height);
        switch (State.AxisDirection)
        {
            case AxisDirection.Up:
            {
                double edgeBottom = edgeOffsetInScrollableCoordinates.Y;
                double edgeTop = edgeOffsetInScrollableCoordinates.Y - lineHeight.Value;
                if (edgeBottom >= scrollableRect.Bottom && edgeTop <= scrollableRect.Top)
                {
                    return;
                }

                if (edgeBottom > scrollableRect.Bottom)
                {
                    Position.JumpTo(Position.Pixels + scrollableRect.Bottom - edgeBottom);
                    return;
                }

                if (edgeTop < scrollableRect.Top)
                {
                    Position.JumpTo(Position.Pixels + scrollableRect.Top - edgeTop);
                }

                return;
            }

            case AxisDirection.Right:
            {
                double edge0 = edgeOffsetInScrollableCoordinates.X;
                if (edge0 >= scrollableRect.Right && edge0 <= scrollableRect.Left)
                {
                    return;
                }

                if (edge0 > scrollableRect.Right)
                {
                    Position.JumpTo(Position.Pixels + edge0 - scrollableRect.Right);
                    return;
                }

                if (edge0 < scrollableRect.Left)
                {
                    Position.JumpTo(Position.Pixels + edge0 - scrollableRect.Left);
                }

                return;
            }

            case AxisDirection.Down:
            {
                double edgeBottom = edgeOffsetInScrollableCoordinates.Y;
                double edgeTop = edgeOffsetInScrollableCoordinates.Y - lineHeight.Value;
                if (edgeBottom >= scrollableRect.Bottom && edgeTop <= scrollableRect.Top)
                {
                    return;
                }

                if (edgeBottom > scrollableRect.Bottom)
                {
                    Position.JumpTo(Position.Pixels + edgeBottom - scrollableRect.Bottom);
                    return;
                }

                if (edgeTop < scrollableRect.Top)
                {
                    Position.JumpTo(Position.Pixels + edgeTop - scrollableRect.Top);
                }

                return;
            }

            case AxisDirection.Left:
            {
                double edge0 = edgeOffsetInScrollableCoordinates.X;
                if (edge0 >= scrollableRect.Right && edge0 <= scrollableRect.Left)
                {
                    return;
                }

                if (edge0 > scrollableRect.Right)
                {
                    Position.JumpTo(Position.Pixels + scrollableRect.Right - edge0);
                    return;
                }

                if (edge0 < scrollableRect.Left)
                {
                    Position.JumpTo(Position.Pixels + scrollableRect.Left - edge0);
                }

                return;
            }
        }
    }

    private bool GlobalPositionInScrollable(Point globalPosition)
    {
        var box = (RenderBox)State.Context.FindRenderObject()!;
        Point localPosition = box.GlobalToLocal(globalPosition);
        var rect = new Rect(0, 0, box.Size.Width, box.Size.Height);
        return rect.Contains(localPosition);
    }

    private static Rect DragTargetFromEvent(SelectionEdgeUpdateEvent @event)
    {
        return RectFromCenter(@event.GlobalPosition, DefaultDragTargetSize, DefaultDragTargetSize);
    }

    protected override SelectionResult DispatchSelectionEventToChild(ISelectable selectable, SelectionEvent @event)
    {
        switch (@event.Type)
        {
            case SelectionEventType.StartEdgeUpdate:
                _selectableStartEdgeUpdateRecords[selectable] = State.Position.Pixels;
                EnsureChildUpdated(selectable);
                break;
            case SelectionEventType.EndEdgeUpdate:
                _selectableEndEdgeUpdateRecords[selectable] = State.Position.Pixels;
                EnsureChildUpdated(selectable);
                break;
            case SelectionEventType.Clear:
                _selectableEndEdgeUpdateRecords.Remove(selectable);
                _selectableStartEdgeUpdateRecords.Remove(selectable);
                break;
            case SelectionEventType.SelectAll:
            case SelectionEventType.SelectWord:
            case SelectionEventType.SelectParagraph:
                _selectableEndEdgeUpdateRecords[selectable] = State.Position.Pixels;
                _selectableStartEdgeUpdateRecords[selectable] = State.Position.Pixels;
                break;
            case SelectionEventType.GranularlyExtendSelection:
            case SelectionEventType.DirectionallyExtendSelection:
                EnsureChildUpdated(selectable);
                _selectableEndEdgeUpdateRecords[selectable] = State.Position.Pixels;
                _selectableStartEdgeUpdateRecords[selectable] = State.Position.Pixels;
                break;
        }

        return base.DispatchSelectionEventToChild(selectable, @event);
    }

    protected override void EnsureChildUpdated(ISelectable selectable)
    {
        double newRecord = State.Position.Pixels;
        double? previousStartRecord = _selectableStartEdgeUpdateRecords.TryGetValue(
            selectable,
            out double startRecord)
            ? startRecord
            : null;
        if (_currentDragStartRelatedToOrigin is { } dragStart
            && (previousStartRecord is null
                || Math.Abs(newRecord - previousStartRecord.Value) > Constants.PrecisionErrorTolerance))
        {
            Point deltaToOrigin = GetDeltaToScrollOrigin(State);
            Point startOffset = Translate(dragStart, -deltaToOrigin.X, -deltaToOrigin.Y);
            selectable.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForStart(startOffset));
            _selectableStartEdgeUpdateRecords[selectable] = State.Position.Pixels;
        }

        double? previousEndRecord = _selectableEndEdgeUpdateRecords.TryGetValue(
            selectable,
            out double endRecord)
            ? endRecord
            : null;
        if (_currentDragEndRelatedToOrigin is { } dragEnd
            && (previousEndRecord is null
                || Math.Abs(newRecord - previousEndRecord.Value) > Constants.PrecisionErrorTolerance))
        {
            Point deltaToOrigin = GetDeltaToScrollOrigin(State);
            Point endOffset = Translate(dragEnd, -deltaToOrigin.X, -deltaToOrigin.Y);
            selectable.DispatchSelectionEvent(SelectionEdgeUpdateEvent.ForEnd(endOffset));
            _selectableEndEdgeUpdateRecords[selectable] = State.Position.Pixels;
        }
    }

    public override void Dispose()
    {
        _selectableStartEdgeUpdateRecords.Clear();
        _selectableEndEdgeUpdateRecords.Clear();
        _scheduledLayoutChange = false;
        _disposed = true;

        // Dart leaves the listener attached, because the `ScrollPosition` is torn down with the
        // scrollable and never notifies again. A Plumix position does notify while it is being
        // detached, which would schedule a layout change into an already-disposed delegate, so the
        // subscription is dropped here instead. Same observable behavior, different teardown order.
        _position.RemoveListener(ScheduleLayoutChange);
        _autoScroller.StopAutoScroll();
        base.Dispose();
    }

    // -- Geometry helpers -----------------------------------------------------------------------

    /// Dart's `Offset.infinite`.
    private static Point Infinite => new(double.PositiveInfinity, double.PositiveInfinity);

    private static Point Translate(Point point, double dx, double dy) => new(point.X + dx, point.Y + dy);

    private static Rect RectFromCenter(Point center, double width, double height) =>
        new(center.X - (width / 2), center.Y - (height / 2), width, height);

    /// Dart's free function `_getDeltaToScrollOrigin`, which the delegate uses in place of the
    /// identical <see cref="Scrollable.ScrollableState.DeltaToScrollOrigin"/> getter.
    internal static Point GetDeltaToScrollOrigin(Scrollable.ScrollableState scrollableState)
    {
        return scrollableState.AxisDirection switch
        {
            AxisDirection.Up => new Point(0, -scrollableState.Position.Pixels),
            AxisDirection.Down => new Point(0, scrollableState.Position.Pixels),
            AxisDirection.Left => new Point(-scrollableState.Position.Pixels, 0),
            _ => new Point(scrollableState.Position.Pixels, 0),
        };
    }
}
