using Avalonia;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/selectable_region.dart

/// A [SelectionContainerDelegate] that handles a list of [ISelectable]s.
public abstract class MultiSelectableSelectionContainerDelegate : SelectionContainerDelegate
{
    private const double SelectionHandleDrawableAreaPadding = 5.0;
    private const double SelectableVerticalComparingThreshold = 3.0;
    private const double PrecisionErrorTolerance = 1e-10;

    private LayerLink? _startHandleLayer;
    private ISelectable? _startHandleLayerOwner;
    private LayerLink? _endHandleLayer;
    private ISelectable? _endHandleLayerOwner;
    private bool _isHandlingSelectionEvent;
    private bool _scheduledSelectableUpdate;
    private bool _selectionInProgress;
    private HashSet<ISelectable> _additions = [];
    private bool _extendSelectionInProgress;
    private SelectionGeometry _selectionGeometry = new(SelectionStatus.None, hasContent: false);

    /// The list of selectables managed by this delegate, in screen order.
    public List<ISelectable> Selectables { get; private set; } = [];

    /// The current selection end index, or -1 when there is no selection.
    protected int CurrentSelectionEndIndex { get; set; } = -1;

    /// The current selection start index, or -1 when there is no selection.
    protected int CurrentSelectionStartIndex { get; set; } = -1;

    public override SelectionGeometry Value => _selectionGeometry;

    /// The comparator used to sort the selectables into screen order.
    protected virtual Comparison<ISelectable> CompareOrder => CompareScreenOrder;

    public override void Add(ISelectable selectable)
    {
        _additions.Add(selectable);
        ScheduleSelectableUpdate();
    }

    public override void Remove(ISelectable selectable)
    {
        if (_additions.Remove(selectable))
        {
            // The selectable was added and removed in the same frame.
            return;
        }

        RemoveSelectable(selectable);
        ScheduleSelectableUpdate();
    }

    /// Notifies this delegate that layout of the subtree has changed.
    public void LayoutDidChange() => UpdateSelectionGeometry();

    private void ScheduleSelectableUpdate()
    {
        if (_scheduledSelectableUpdate)
        {
            return;
        }

        _scheduledSelectableUpdate = true;
        if (Scheduler.Phase == SchedulerPhase.PostFrameCallbacks)
        {
            Scheduler.ScheduleMicrotask(RunScheduledTask);
        }
        else
        {
            Scheduler.AddPostFrameCallback(_ => RunScheduledTask());
        }
    }

    private void RunScheduledTask()
    {
        if (!_scheduledSelectableUpdate)
        {
            return;
        }

        _scheduledSelectableUpdate = false;
        UpdateSelectables();
    }

    private void UpdateSelectables()
    {
        if (_additions.Count > 0)
        {
            FlushAdditions();
        }

        DidChangeSelectables();
    }

    private void FlushAdditions()
    {
        List<ISelectable> mergingSelectables = _additions.ToList();
        mergingSelectables.Sort(CompareOrder);
        List<ISelectable> existingSelectables = Selectables;
        Selectables = [];
        int mergingIndex = 0;
        int existingIndex = 0;
        int selectionStartIndex = CurrentSelectionStartIndex;
        int selectionEndIndex = CurrentSelectionEndIndex;
        while (mergingIndex < mergingSelectables.Count || existingIndex < existingSelectables.Count)
        {
            if (mergingIndex >= mergingSelectables.Count
                || (existingIndex < existingSelectables.Count
                    && CompareOrder(existingSelectables[existingIndex], mergingSelectables[mergingIndex]) < 0))
            {
                if (existingIndex == CurrentSelectionStartIndex)
                {
                    selectionStartIndex = Selectables.Count;
                }

                if (existingIndex == CurrentSelectionEndIndex)
                {
                    selectionEndIndex = Selectables.Count;
                }

                Selectables.Add(existingSelectables[existingIndex]);
                existingIndex += 1;
                continue;
            }

            ISelectable mergingSelectable = mergingSelectables[mergingIndex];
            if (existingIndex < Math.Max(CurrentSelectionStartIndex, CurrentSelectionEndIndex)
                && existingIndex > Math.Min(CurrentSelectionStartIndex, CurrentSelectionEndIndex))
            {
                EnsureChildUpdated(mergingSelectable);
            }

            mergingSelectable.AddListener(HandleSelectableGeometryChange);
            Selectables.Add(mergingSelectable);
            mergingIndex += 1;
        }

        CurrentSelectionEndIndex = selectionEndIndex;
        CurrentSelectionStartIndex = selectionStartIndex;
        _additions = [];
    }

    private void RemoveSelectable(ISelectable selectable)
    {
        int index = Selectables.IndexOf(selectable);
        if (index < 0)
        {
            return;
        }

        Selectables.RemoveAt(index);
        if (index <= CurrentSelectionEndIndex)
        {
            CurrentSelectionEndIndex -= 1;
        }

        if (index <= CurrentSelectionStartIndex)
        {
            CurrentSelectionStartIndex -= 1;
        }

        selectable.RemoveListener(HandleSelectableGeometryChange);
    }

    /// Called when this delegate's selectables have changed.
    protected virtual void DidChangeSelectables() => UpdateSelectionGeometry();

    private void HandleSelectableGeometryChange()
    {
        if (_isHandlingSelectionEvent)
        {
            return;
        }

        UpdateSelectionGeometry();
    }

    // -- Ordering --------------------------------------------------------------

    private static Rect GetBoundingBox(ISelectable selectable)
    {
        IReadOnlyList<Rect> boxes = selectable.BoundingBoxes;
        if (boxes.Count == 0)
        {
            return default;
        }

        Rect result = boxes[0];
        for (int index = 1; index < boxes.Count; index += 1)
        {
            result = result.Union(boxes[index]);
        }

        return result;
    }

    private static int CompareScreenOrder(ISelectable a, ISelectable b)
    {
        Rect rectA = RenderObject.TransformRect(a.GetTransformTo(null), GetBoundingBox(a));
        Rect rectB = RenderObject.TransformRect(b.GetTransformTo(null), GetBoundingBox(b));
        int result = CompareVertically(rectA, rectB);
        return result != 0 ? result : CompareHorizontally(rectA, rectB);
    }

    private static int CompareVertically(Rect a, Rect b)
    {
        if ((a.Top - b.Top < SelectableVerticalComparingThreshold
             && a.Bottom - b.Bottom > -SelectableVerticalComparingThreshold)
            || (b.Top - a.Top < SelectableVerticalComparingThreshold
                && b.Bottom - a.Bottom > -SelectableVerticalComparingThreshold))
        {
            return 0;
        }

        if (Math.Abs(a.Top - b.Top) > SelectableVerticalComparingThreshold)
        {
            return a.Top > b.Top ? 1 : -1;
        }

        return a.Bottom > b.Bottom ? 1 : -1;
    }

    private static int CompareHorizontally(Rect a, Rect b)
    {
        if (a.Left - b.Left < PrecisionErrorTolerance && a.Right - b.Right > -PrecisionErrorTolerance)
        {
            return -1;
        }

        if (b.Left - a.Left < PrecisionErrorTolerance && b.Right - a.Right > -PrecisionErrorTolerance)
        {
            return 1;
        }

        if (Math.Abs(a.Left - b.Left) > PrecisionErrorTolerance)
        {
            return a.Left > b.Left ? 1 : -1;
        }

        return a.Right > b.Right ? 1 : -1;
    }

    // -- Event dispatch ---------------------------------------------------------

    public override SelectionResult DispatchSelectionEvent(SelectionEvent @event)
    {
        bool selectionWillBeInProgress = @event is not ClearSelectionEvent;
        if (!_selectionInProgress && selectionWillBeInProgress)
        {
            Selectables.Sort(CompareOrder);
        }

        _selectionInProgress = selectionWillBeInProgress;
        _isHandlingSelectionEvent = true;
        SelectionResult result;
        switch (@event)
        {
            case SelectionEdgeUpdateEvent edgeUpdate:
                _extendSelectionInProgress = false;
                result = HandleSelectionEdgeUpdate(edgeUpdate);
                break;
            case ClearSelectionEvent clear:
                _extendSelectionInProgress = false;
                result = HandleClearSelection(clear);
                break;
            case SelectAllSelectionEvent selectAll:
                _extendSelectionInProgress = false;
                result = HandleSelectAll(selectAll);
                break;
            case SelectWordSelectionEvent selectWord:
                _extendSelectionInProgress = false;
                result = HandleSelectWord(selectWord);
                break;
            case SelectParagraphSelectionEvent selectParagraph:
                _extendSelectionInProgress = false;
                result = HandleSelectParagraph(selectParagraph);
                break;
            case GranularlyExtendSelectionEvent granular:
                _extendSelectionInProgress = true;
                result = HandleGranularlyExtendSelection(granular);
                break;
            case DirectionallyExtendSelectionEvent directional:
                _extendSelectionInProgress = true;
                result = HandleDirectionallyExtendSelection(directional);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(@event));
        }

        _isHandlingSelectionEvent = false;
        UpdateSelectionGeometry();
        return result;
    }

    /// Dispatches a selection event to one of this delegate's selectables.
    protected virtual SelectionResult DispatchSelectionEventToChild(ISelectable selectable, SelectionEvent @event)
    {
        return selectable.DispatchSelectionEvent(@event);
    }

    /// Ensures the `selectable` has received the selection events it missed.
    protected abstract void EnsureChildUpdated(ISelectable selectable);

    protected virtual SelectionResult HandleSelectionEdgeUpdate(SelectionEdgeUpdateEvent @event)
    {
        if (@event.Type == SelectionEventType.EndEdgeUpdate)
        {
            return CurrentSelectionEndIndex == -1
                ? InitSelection(@event, isEnd: true)
                : AdjustSelection(@event, isEnd: true);
        }

        return CurrentSelectionStartIndex == -1
            ? InitSelection(@event, isEnd: false)
            : AdjustSelection(@event, isEnd: false);
    }

    private SelectionResult InitSelection(SelectionEdgeUpdateEvent @event, bool isEnd)
    {
        int newIndex = -1;
        bool hasFoundEdgeIndex = false;
        SelectionResult? result = null;
        bool? forward = null;
        int oppositeEdgeIndex = isEnd ? CurrentSelectionStartIndex : CurrentSelectionEndIndex;
        int index = Math.Max(oppositeEdgeIndex, 0);
        while (index >= 0 && index < Selectables.Count)
        {
            SelectionResult childResult = DispatchSelectionEventToChild(Selectables[index], @event);
            switch (childResult)
            {
                case SelectionResult.Next:
                    if (forward == false)
                    {
                        hasFoundEdgeIndex = true;
                        result = SelectionResult.End;
                    }
                    else
                    {
                        forward = true;
                        newIndex = index;
                    }

                    break;
                case SelectionResult.None:
                    newIndex = index;
                    break;
                case SelectionResult.End:
                    newIndex = index;
                    result = SelectionResult.End;
                    hasFoundEdgeIndex = true;
                    break;
                case SelectionResult.Previous:
                    if (index == 0)
                    {
                        hasFoundEdgeIndex = true;
                        newIndex = 0;
                        result = SelectionResult.Previous;
                    }
                    else if (forward ?? false)
                    {
                        hasFoundEdgeIndex = true;
                        result = SelectionResult.End;
                    }
                    else
                    {
                        forward = false;
                        newIndex = index;
                    }

                    break;
                case SelectionResult.Pending:
                    newIndex = index;
                    result = SelectionResult.Pending;
                    hasFoundEdgeIndex = true;
                    break;
            }

            if (hasFoundEdgeIndex)
            {
                break;
            }

            index += (forward ?? true) ? 1 : -1;
        }

        if (newIndex == -1)
        {
            return SelectionResult.None;
        }

        if (isEnd)
        {
            CurrentSelectionEndIndex = newIndex;
        }
        else
        {
            CurrentSelectionStartIndex = newIndex;
        }

        FlushInactiveSelections();
        return result ?? SelectionResult.Next;
    }

    private SelectionResult AdjustSelection(SelectionEdgeUpdateEvent @event, bool isEnd)
    {
        SelectionResult? finalResult = null;
        bool isCurrentEdgeWithinViewport = isEnd
            ? _selectionGeometry.EndSelectionPoint is not null
            : _selectionGeometry.StartSelectionPoint is not null;
        bool isOppositeEdgeWithinViewport = isEnd
            ? _selectionGeometry.StartSelectionPoint is not null
            : _selectionGeometry.EndSelectionPoint is not null;
        int newIndex = (isEnd, isCurrentEdgeWithinViewport, isOppositeEdgeWithinViewport) switch
        {
            (true, true, true) => CurrentSelectionEndIndex,
            (true, true, false) => CurrentSelectionEndIndex,
            (true, false, true) => CurrentSelectionStartIndex,
            (true, false, false) => 0,
            (false, true, true) => CurrentSelectionStartIndex,
            (false, true, false) => CurrentSelectionStartIndex,
            (false, false, true) => CurrentSelectionEndIndex,
            (false, false, false) => 0,
        };

        bool? forward = null;
        while (newIndex < Selectables.Count && newIndex >= 0 && finalResult is null)
        {
            SelectionResult currentSelectableResult = DispatchSelectionEventToChild(Selectables[newIndex], @event);
            switch (currentSelectableResult)
            {
                case SelectionResult.End:
                case SelectionResult.Pending:
                case SelectionResult.None:
                    finalResult = currentSelectableResult;
                    break;
                case SelectionResult.Next:
                    if (forward == false)
                    {
                        newIndex += 1;
                        finalResult = SelectionResult.End;
                    }
                    else if (newIndex == Selectables.Count - 1)
                    {
                        finalResult = currentSelectableResult;
                    }
                    else
                    {
                        forward = true;
                        newIndex += 1;
                    }

                    break;
                case SelectionResult.Previous:
                    if (forward ?? false)
                    {
                        newIndex -= 1;
                        finalResult = SelectionResult.End;
                    }
                    else if (newIndex == 0)
                    {
                        finalResult = currentSelectableResult;
                    }
                    else
                    {
                        forward = false;
                        newIndex -= 1;
                    }

                    break;
            }
        }

        if (isEnd)
        {
            CurrentSelectionEndIndex = newIndex;
        }
        else
        {
            CurrentSelectionStartIndex = newIndex;
        }

        FlushInactiveSelections();
        return finalResult ?? SelectionResult.None;
    }

    private void FlushInactiveSelections()
    {
        if (CurrentSelectionStartIndex == -1 && CurrentSelectionEndIndex == -1)
        {
            return;
        }

        if (CurrentSelectionStartIndex == -1 || CurrentSelectionEndIndex == -1)
        {
            ClearSelectables(CurrentSelectionStartIndex == -1
                ? CurrentSelectionEndIndex
                : CurrentSelectionStartIndex);
            return;
        }

        int skipStart = Math.Min(CurrentSelectionStartIndex, CurrentSelectionEndIndex);
        int skipEnd = Math.Max(CurrentSelectionStartIndex, CurrentSelectionEndIndex);
        for (int index = 0; index < Selectables.Count; index += 1)
        {
            if (index >= skipStart && index <= skipEnd)
            {
                continue;
            }

            DispatchSelectionEventToChild(Selectables[index], new ClearSelectionEvent());
        }
    }

    private void ClearSelectables(int? skipIndex = null)
    {
        for (int index = 0; index < Selectables.Count; index += 1)
        {
            if (index == skipIndex)
            {
                continue;
            }

            DispatchSelectionEventToChild(Selectables[index], new ClearSelectionEvent());
        }
    }

    protected virtual SelectionResult HandleSelectAll(SelectAllSelectionEvent @event)
    {
        foreach (ISelectable selectable in Selectables)
        {
            DispatchSelectionEventToChild(selectable, @event);
        }

        CurrentSelectionStartIndex = 0;
        CurrentSelectionEndIndex = Selectables.Count - 1;
        return SelectionResult.None;
    }

    protected virtual SelectionResult HandleClearSelection(ClearSelectionEvent @event)
    {
        foreach (ISelectable selectable in Selectables)
        {
            DispatchSelectionEventToChild(selectable, @event);
        }

        CurrentSelectionEndIndex = -1;
        CurrentSelectionStartIndex = -1;
        return SelectionResult.None;
    }

    protected virtual SelectionResult HandleSelectWord(SelectWordSelectionEvent @event)
    {
        return HandleSelectBoundary(@event, @event.GlobalPosition);
    }

    protected virtual SelectionResult HandleSelectParagraph(SelectParagraphSelectionEvent @event)
    {
        return HandleSelectBoundary(@event, @event.GlobalPosition);
    }

    private SelectionResult HandleSelectBoundary(SelectionEvent @event, Point effectiveGlobalPosition)
    {
        SelectionResult? lastSelectionResult = null;
        double minDistanceSquared = double.PositiveInfinity;
        int nearestIndex = 0;
        for (int index = 0; index < Selectables.Count; index += 1)
        {
            bool globalRectsContainPosition = false;
            Matrix4 transform = Selectables[index].GetTransformTo(null);
            foreach (Rect rect in Selectables[index].BoundingBoxes)
            {
                Rect globalRect = RenderObject.TransformRect(transform, rect);
                if (SelectionUtils.RectContains(globalRect, effectiveGlobalPosition))
                {
                    globalRectsContainPosition = true;
                    break;
                }

                double dx = effectiveGlobalPosition.X
                            - Math.Clamp(effectiveGlobalPosition.X, globalRect.Left, globalRect.Right);
                double dy = effectiveGlobalPosition.Y
                            - Math.Clamp(effectiveGlobalPosition.Y, globalRect.Top, globalRect.Bottom);
                double distanceSquared = (dx * dx) + (dy * dy);
                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    nearestIndex = index;
                }
            }

            if (globalRectsContainPosition)
            {
                SelectionGeometry existingGeometry = Selectables[index].Value;
                lastSelectionResult = DispatchSelectionEventToChild(Selectables[index], @event);
                if (index == Selectables.Count - 1 && lastSelectionResult == SelectionResult.Next)
                {
                    return SelectionResult.Next;
                }

                if (lastSelectionResult == SelectionResult.Next)
                {
                    continue;
                }

                if (index == 0 && lastSelectionResult == SelectionResult.Previous)
                {
                    return SelectionResult.Previous;
                }

                if (!Selectables[index].Value.Equals(existingGeometry))
                {
                    ClearSelectables(index);
                    CurrentSelectionStartIndex = index;
                    CurrentSelectionEndIndex = index;
                }

                return SelectionResult.End;
            }

            if (lastSelectionResult == SelectionResult.Next)
            {
                CurrentSelectionStartIndex = index - 1;
                CurrentSelectionEndIndex = index - 1;
                return SelectionResult.End;
            }
        }

        if (Selectables.Count > 0)
        {
            SelectionGeometry existingGeometry = Selectables[nearestIndex].Value;
            DispatchSelectionEventToChild(Selectables[nearestIndex], @event);
            if (!Selectables[nearestIndex].Value.Equals(existingGeometry))
            {
                ClearSelectables(nearestIndex);
                CurrentSelectionStartIndex = nearestIndex;
                CurrentSelectionEndIndex = nearestIndex;
            }
        }

        return SelectionResult.End;
    }

    protected virtual SelectionResult HandleGranularlyExtendSelection(GranularlyExtendSelectionEvent @event)
    {
        if (CurrentSelectionStartIndex == -1)
        {
            if (Selectables.Count == 0)
            {
                return SelectionResult.None;
            }

            CurrentSelectionStartIndex = @event.Forward ? 0 : Selectables.Count - 1;
            CurrentSelectionEndIndex = CurrentSelectionStartIndex;
        }

        int targetIndex = @event.IsEnd ? CurrentSelectionEndIndex : CurrentSelectionStartIndex;
        SelectionResult result = DispatchSelectionEventToChild(Selectables[targetIndex], @event);
        if (@event.Forward)
        {
            while (targetIndex < Selectables.Count - 1 && result == SelectionResult.Next)
            {
                targetIndex += 1;
                result = DispatchSelectionEventToChild(Selectables[targetIndex], @event);
            }
        }
        else
        {
            while (targetIndex > 0 && result == SelectionResult.Previous)
            {
                targetIndex -= 1;
                result = DispatchSelectionEventToChild(Selectables[targetIndex], @event);
            }
        }

        if (@event.IsEnd)
        {
            CurrentSelectionEndIndex = targetIndex;
        }
        else
        {
            CurrentSelectionStartIndex = targetIndex;
        }

        return result;
    }

    protected virtual SelectionResult HandleDirectionallyExtendSelection(DirectionallyExtendSelectionEvent @event)
    {
        if (CurrentSelectionStartIndex == -1)
        {
            if (Selectables.Count == 0)
            {
                return SelectionResult.None;
            }

            CurrentSelectionStartIndex = @event.Direction switch
            {
                SelectionExtendDirection.PreviousLine or SelectionExtendDirection.Backward => Selectables.Count - 1,
                _ => 0,
            };
            CurrentSelectionEndIndex = CurrentSelectionStartIndex;
        }

        int targetIndex = @event.IsEnd ? CurrentSelectionEndIndex : CurrentSelectionStartIndex;
        SelectionResult result = DispatchSelectionEventToChild(Selectables[targetIndex], @event);
        switch (@event.Direction)
        {
            case SelectionExtendDirection.PreviousLine:
                if (result == SelectionResult.Previous && targetIndex > 0)
                {
                    targetIndex -= 1;
                    result = DispatchSelectionEventToChild(
                        Selectables[targetIndex],
                        @event.CopyWith(direction: SelectionExtendDirection.Backward));
                }

                break;
            case SelectionExtendDirection.NextLine:
                if (result == SelectionResult.Next && targetIndex < Selectables.Count - 1)
                {
                    targetIndex += 1;
                    result = DispatchSelectionEventToChild(
                        Selectables[targetIndex],
                        @event.CopyWith(direction: SelectionExtendDirection.Forward));
                }

                break;
        }

        if (@event.IsEnd)
        {
            CurrentSelectionEndIndex = targetIndex;
        }
        else
        {
            CurrentSelectionStartIndex = targetIndex;
        }

        return result;
    }

    // -- Geometry ---------------------------------------------------------------

    private void UpdateSelectionGeometry()
    {
        SelectionGeometry newValue = GetSelectionGeometry();
        if (!_selectionGeometry.Equals(newValue))
        {
            _selectionGeometry = newValue;
            NotifyListeners();
        }

        UpdateHandleLayersAndOwners();
    }

    /// Computes the aggregated geometry of this delegate's selectables.
    protected virtual SelectionGeometry GetSelectionGeometry()
    {
        if (CurrentSelectionEndIndex == -1 || CurrentSelectionStartIndex == -1 || Selectables.Count == 0)
        {
            return new SelectionGeometry(SelectionStatus.None, hasContent: Selectables.Count > 0);
        }

        if (!_extendSelectionInProgress)
        {
            CurrentSelectionStartIndex = AdjustSelectionIndexBasedOnSelectionGeometry(
                CurrentSelectionStartIndex,
                CurrentSelectionEndIndex);
            CurrentSelectionEndIndex = AdjustSelectionIndexBasedOnSelectionGeometry(
                CurrentSelectionEndIndex,
                CurrentSelectionStartIndex);
        }

        bool forwardSelection = CurrentSelectionEndIndex >= CurrentSelectionStartIndex;
        SelectionGeometry startGeometry = Selectables[CurrentSelectionStartIndex].Value;
        int startIndexWalker = CurrentSelectionStartIndex;
        while (startIndexWalker != CurrentSelectionEndIndex && startGeometry.StartSelectionPoint is null)
        {
            startIndexWalker += forwardSelection ? 1 : -1;
            startGeometry = Selectables[startIndexWalker].Value;
        }

        SelectionPoint? startPoint = null;
        if (startGeometry.StartSelectionPoint is { } geometryStart)
        {
            Matrix4 startTransform = GetTransformFrom(Selectables[startIndexWalker]);
            Point start = MatrixUtils.TransformPoint(startTransform, geometryStart.LocalPosition);
            if (IsFinite(start))
            {
                startPoint = new SelectionPoint(start, geometryStart.LineHeight, geometryStart.HandleType);
            }
        }

        SelectionGeometry endGeometry = Selectables[CurrentSelectionEndIndex].Value;
        int endIndexWalker = CurrentSelectionEndIndex;
        while (endIndexWalker != CurrentSelectionStartIndex && endGeometry.EndSelectionPoint is null)
        {
            endIndexWalker += forwardSelection ? -1 : 1;
            endGeometry = Selectables[endIndexWalker].Value;
        }

        SelectionPoint? endPoint = null;
        if (endGeometry.EndSelectionPoint is { } geometryEnd)
        {
            Matrix4 endTransform = GetTransformFrom(Selectables[endIndexWalker]);
            Point end = MatrixUtils.TransformPoint(endTransform, geometryEnd.LocalPosition);
            if (IsFinite(end))
            {
                endPoint = new SelectionPoint(end, geometryEnd.LineHeight, geometryEnd.HandleType);
            }
        }

        var selectionRects = new List<Rect>();
        Rect? drawableArea = HasSize ? new Rect(default, ContainerSize) : null;
        for (int index = CurrentSelectionStartIndex; index <= CurrentSelectionEndIndex; index += 1)
        {
            Matrix4 transform = GetTransformFrom(Selectables[index]);
            foreach (Rect rect in Selectables[index].Value.SelectionRects)
            {
                Rect localRect = RenderObject.TransformRect(transform, rect);
                Rect resolved = drawableArea?.Intersect(localRect) ?? localRect;
                if (IsFinite(resolved.Position) && resolved.Width > 0 && resolved.Height > 0)
                {
                    selectionRects.Add(resolved);
                }
            }
        }

        return new SelectionGeometry(
            status: !startGeometry.Equals(endGeometry) ? SelectionStatus.Uncollapsed : startGeometry.Status,
            hasContent: true,
            startSelectionPoint: startPoint,
            endSelectionPoint: endPoint,
            selectionRects: selectionRects);
    }

    private static bool IsFinite(Point point)
    {
        return double.IsFinite(point.X) && double.IsFinite(point.Y);
    }

    private int AdjustSelectionIndexBasedOnSelectionGeometry(int currentIndex, int towardIndex)
    {
        bool forward = towardIndex > currentIndex;
        while (currentIndex != towardIndex
               && Selectables[currentIndex].Value.Status != SelectionStatus.Uncollapsed)
        {
            currentIndex += forward ? 1 : -1;
        }

        return currentIndex;
    }

    // -- Handle layers ----------------------------------------------------------

    public override void PushHandleLayers(LayerLink? startHandle, LayerLink? endHandle)
    {
        if (ReferenceEquals(_startHandleLayer, startHandle) && ReferenceEquals(_endHandleLayer, endHandle))
        {
            return;
        }

        _startHandleLayer = startHandle;
        _endHandleLayer = endHandle;
        UpdateHandleLayersAndOwners();
    }

    private void UpdateHandleLayersAndOwners()
    {
        LayerLink? effectiveStartHandle = _startHandleLayer;
        LayerLink? effectiveEndHandle = _endHandleLayer;
        if (effectiveStartHandle is not null || effectiveEndHandle is not null)
        {
            Rect? drawableArea = HasSize
                ? new Rect(default, ContainerSize).Inflate(SelectionHandleDrawableAreaPadding)
                : null;
            bool hideStartHandle = Value.StartSelectionPoint is not { } start
                                   || drawableArea is not { } startArea
                                   || !SelectionUtils.RectContains(startArea, start.LocalPosition);
            bool hideEndHandle = Value.EndSelectionPoint is not { } end
                                 || drawableArea is not { } endArea
                                 || !SelectionUtils.RectContains(endArea, end.LocalPosition);
            effectiveStartHandle = hideStartHandle ? null : _startHandleLayer;
            effectiveEndHandle = hideEndHandle ? null : _endHandleLayer;
        }

        if (CurrentSelectionStartIndex == -1 || CurrentSelectionEndIndex == -1)
        {
            if (_startHandleLayerOwner is not null)
            {
                _startHandleLayerOwner.PushHandleLayers(null, null);
                _startHandleLayerOwner = null;
            }

            if (_endHandleLayerOwner is not null)
            {
                _endHandleLayerOwner.PushHandleLayers(null, null);
                _endHandleLayerOwner = null;
            }

            return;
        }

        if (!ReferenceEquals(Selectables[CurrentSelectionStartIndex], _startHandleLayerOwner))
        {
            _startHandleLayerOwner?.PushHandleLayers(null, null);
        }

        if (!ReferenceEquals(Selectables[CurrentSelectionEndIndex], _endHandleLayerOwner))
        {
            _endHandleLayerOwner?.PushHandleLayers(null, null);
        }

        _startHandleLayerOwner = Selectables[CurrentSelectionStartIndex];
        if (CurrentSelectionStartIndex == CurrentSelectionEndIndex)
        {
            _endHandleLayerOwner = _startHandleLayerOwner;
            _startHandleLayerOwner.PushHandleLayers(effectiveStartHandle, effectiveEndHandle);
            return;
        }

        _startHandleLayerOwner.PushHandleLayers(effectiveStartHandle, null);
        _endHandleLayerOwner = Selectables[CurrentSelectionEndIndex];
        _endHandleLayerOwner.PushHandleLayers(null, effectiveEndHandle);
    }

    // -- Content ----------------------------------------------------------------

    public override SelectedContent? GetSelectedContent()
    {
        var selections = new List<SelectedContent>();
        foreach (ISelectable selectable in Selectables)
        {
            if (selectable.GetSelectedContent() is { } data)
            {
                selections.Add(data);
            }
        }

        if (selections.Count == 0)
        {
            return null;
        }

        var buffer = new System.Text.StringBuilder();
        foreach (SelectedContent selection in selections)
        {
            buffer.Append(selection.PlainText);
        }

        return new SelectedContent(buffer.ToString());
    }

    public override int ContentLength => Selectables.Sum(selectable => selectable.ContentLength);

    public override SelectedContentRange? GetSelection()
    {
        var selections = new List<(int ContentLength, SelectedContentRange? Range)>();
        foreach (ISelectable selectable in Selectables)
        {
            selections.Add((selectable.ContentLength, selectable.GetSelection()));
        }

        return CalculateLocalRange(selections);
    }

    private SelectedContentRange? CalculateLocalRange(
        IReadOnlyList<(int ContentLength, SelectedContentRange? Range)> selections)
    {
        if (CurrentSelectionStartIndex == -1 || CurrentSelectionEndIndex == -1)
        {
            return null;
        }

        int startOffset = 0;
        int endOffset = 0;
        bool foundStart = false;
        bool forwardSelection = CurrentSelectionEndIndex >= CurrentSelectionStartIndex;
        if (CurrentSelectionEndIndex == CurrentSelectionStartIndex
            && Selectables[CurrentSelectionStartIndex].GetSelection() is { } singleRange)
        {
            forwardSelection = singleRange.EndOffset >= singleRange.StartOffset;
        }

        for (int index = 0; index < selections.Count; index += 1)
        {
            (int contentLength, SelectedContentRange? range) = selections[index];
            if (range is null)
            {
                if (foundStart)
                {
                    return new SelectedContentRange(
                        forwardSelection ? startOffset : endOffset,
                        forwardSelection ? endOffset : startOffset);
                }

                startOffset += contentLength;
                endOffset = startOffset;
                continue;
            }

            int selectionStartNormalized = Math.Min(range.StartOffset, range.EndOffset);
            int selectionEndNormalized = Math.Max(range.StartOffset, range.EndOffset);
            if (!foundStart)
            {
                startOffset += selectionStartNormalized;
                endOffset = startOffset + Math.Abs(selectionEndNormalized - selectionStartNormalized);
                foundStart = true;
            }
            else
            {
                endOffset += Math.Abs(selectionEndNormalized - selectionStartNormalized);
            }
        }

        if (!foundStart)
        {
            return null;
        }

        return new SelectedContentRange(
            forwardSelection ? startOffset : endOffset,
            forwardSelection ? endOffset : startOffset);
    }

    public override void Dispose()
    {
        foreach (ISelectable selectable in Selectables)
        {
            selectable.RemoveListener(HandleSelectableGeometryChange);
        }

        Selectables = [];
        _scheduledSelectableUpdate = false;
        base.Dispose();
    }
}

/// A [MultiSelectableSelectionContainerDelegate] for a subtree whose selectables
/// do not move, replaying missed edge updates onto newly added children.
public class StaticSelectionContainerDelegate : MultiSelectableSelectionContainerDelegate
{
    private readonly HashSet<ISelectable> _hasReceivedStartEvent = [];
    private readonly HashSet<ISelectable> _hasReceivedEndEvent = [];
    private Point? _lastStartEdgeUpdateGlobalPosition;
    private Point? _lastEndEdgeUpdateGlobalPosition;

    /// Records that the `selectable` has received a selection edge event.
    protected void DidReceiveSelectionEventFor(ISelectable selectable, bool? forEnd = null)
    {
        switch (forEnd)
        {
            case true:
                _hasReceivedEndEvent.Add(selectable);
                break;
            case false:
                _hasReceivedStartEvent.Add(selectable);
                break;
            default:
                _hasReceivedStartEvent.Add(selectable);
                _hasReceivedEndEvent.Add(selectable);
                break;
        }
    }

    /// Records that every selectable in the active range has received both edges.
    protected void DidReceiveSelectionBoundaryEvents()
    {
        if (CurrentSelectionStartIndex == -1 || CurrentSelectionEndIndex == -1)
        {
            return;
        }

        int start = Math.Min(CurrentSelectionStartIndex, CurrentSelectionEndIndex);
        int end = Math.Max(CurrentSelectionStartIndex, CurrentSelectionEndIndex);
        for (int index = start; index <= end; index += 1)
        {
            DidReceiveSelectionEventFor(Selectables[index]);
        }

        UpdateLastSelectionEdgeLocationsFromGeometries();
    }

    /// Stores the last global location of a selection edge.
    protected void UpdateLastSelectionEdgeLocation(Point globalSelectionEdgeLocation, bool forEnd)
    {
        if (forEnd)
        {
            _lastEndEdgeUpdateGlobalPosition = globalSelectionEdgeLocation;
        }
        else
        {
            _lastStartEdgeUpdateGlobalPosition = globalSelectionEdgeLocation;
        }
    }

    private void UpdateLastSelectionEdgeLocationsFromGeometries()
    {
        if (CurrentSelectionStartIndex != -1 && Selectables[CurrentSelectionStartIndex].Value.HasSelection)
        {
            ISelectable start = Selectables[CurrentSelectionStartIndex];
            SelectionPoint startPoint = start.Value.StartSelectionPoint!;
            var localStartEdge = new Point(
                startPoint.LocalPosition.X,
                startPoint.LocalPosition.Y - (startPoint.LineHeight / 2));
            UpdateLastSelectionEdgeLocation(
                MatrixUtils.TransformPoint(start.GetTransformTo(null), localStartEdge),
                forEnd: false);
        }

        if (CurrentSelectionEndIndex != -1 && Selectables[CurrentSelectionEndIndex].Value.HasSelection)
        {
            ISelectable end = Selectables[CurrentSelectionEndIndex];
            SelectionPoint endPoint = end.Value.EndSelectionPoint!;
            var localEndEdge = new Point(
                endPoint.LocalPosition.X,
                endPoint.LocalPosition.Y - (endPoint.LineHeight / 2));
            UpdateLastSelectionEdgeLocation(
                MatrixUtils.TransformPoint(end.GetTransformTo(null), localEndEdge),
                forEnd: true);
        }
    }

    /// Clears the recorded edge state for every selectable.
    protected void ClearInternalSelectionState()
    {
        foreach (ISelectable selectable in Selectables)
        {
            ClearInternalSelectionStateForSelectable(selectable);
        }

        _lastStartEdgeUpdateGlobalPosition = null;
        _lastEndEdgeUpdateGlobalPosition = null;
    }

    /// Clears the recorded edge state for one selectable.
    protected void ClearInternalSelectionStateForSelectable(ISelectable selectable)
    {
        _hasReceivedStartEvent.Remove(selectable);
        _hasReceivedEndEvent.Remove(selectable);
    }

    public override void Remove(ISelectable selectable)
    {
        ClearInternalSelectionStateForSelectable(selectable);
        base.Remove(selectable);
    }

    protected override SelectionResult HandleSelectAll(SelectAllSelectionEvent @event)
    {
        SelectionResult result = base.HandleSelectAll(@event);
        DidReceiveSelectionBoundaryEvents();
        return result;
    }

    protected override SelectionResult HandleSelectWord(SelectWordSelectionEvent @event)
    {
        SelectionResult result = base.HandleSelectWord(@event);
        DidReceiveSelectionBoundaryEvents();
        return result;
    }

    protected override SelectionResult HandleSelectParagraph(SelectParagraphSelectionEvent @event)
    {
        SelectionResult result = base.HandleSelectParagraph(@event);
        DidReceiveSelectionBoundaryEvents();
        return result;
    }

    protected override SelectionResult HandleClearSelection(ClearSelectionEvent @event)
    {
        SelectionResult result = base.HandleClearSelection(@event);
        ClearInternalSelectionState();
        return result;
    }

    protected override SelectionResult HandleSelectionEdgeUpdate(SelectionEdgeUpdateEvent @event)
    {
        UpdateLastSelectionEdgeLocation(
            @event.GlobalPosition,
            forEnd: @event.Type == SelectionEventType.EndEdgeUpdate);
        return base.HandleSelectionEdgeUpdate(@event);
    }

    protected override SelectionResult DispatchSelectionEventToChild(
        ISelectable selectable,
        SelectionEvent @event)
    {
        switch (@event.Type)
        {
            case SelectionEventType.StartEdgeUpdate:
                DidReceiveSelectionEventFor(selectable, forEnd: false);
                EnsureChildUpdated(selectable);
                break;
            case SelectionEventType.EndEdgeUpdate:
                DidReceiveSelectionEventFor(selectable, forEnd: true);
                EnsureChildUpdated(selectable);
                break;
            case SelectionEventType.Clear:
                ClearInternalSelectionStateForSelectable(selectable);
                break;
            case SelectionEventType.GranularlyExtendSelection:
            case SelectionEventType.DirectionallyExtendSelection:
                DidReceiveSelectionEventFor(selectable);
                EnsureChildUpdated(selectable);
                break;
        }

        return base.DispatchSelectionEventToChild(selectable, @event);
    }

    protected override void EnsureChildUpdated(ISelectable selectable)
    {
        if (_lastEndEdgeUpdateGlobalPosition is { } endPosition && _hasReceivedEndEvent.Add(selectable))
        {
            SelectionEdgeUpdateEvent synthesizedEvent = SelectionEdgeUpdateEvent.ForEnd(endPosition);
            if (CurrentSelectionEndIndex == -1)
            {
                HandleSelectionEdgeUpdate(synthesizedEvent);
            }

            selectable.DispatchSelectionEvent(synthesizedEvent);
        }

        if (_lastStartEdgeUpdateGlobalPosition is { } startPosition && _hasReceivedStartEvent.Add(selectable))
        {
            SelectionEdgeUpdateEvent synthesizedEvent = SelectionEdgeUpdateEvent.ForStart(startPosition);
            if (CurrentSelectionStartIndex == -1)
            {
                HandleSelectionEdgeUpdate(synthesizedEvent);
            }

            selectable.DispatchSelectionEvent(synthesizedEvent);
        }
    }

    protected override void DidChangeSelectables()
    {
        if (_lastEndEdgeUpdateGlobalPosition is { } endPosition)
        {
            HandleSelectionEdgeUpdate(SelectionEdgeUpdateEvent.ForEnd(endPosition));
        }

        if (_lastStartEdgeUpdateGlobalPosition is { } startPosition)
        {
            HandleSelectionEdgeUpdate(SelectionEdgeUpdateEvent.ForStart(startPosition));
        }

        var selectableSet = Selectables.ToHashSet();
        _hasReceivedEndEvent.RemoveWhere(selectable => !selectableSet.Contains(selectable));
        _hasReceivedStartEvent.RemoveWhere(selectable => !selectableSet.Contains(selectable));
        base.DidChangeSelectables();
    }

    public override void Dispose()
    {
        ClearInternalSelectionState();
        base.Dispose();
    }
}
