using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/reorderable_list.dart

#pragma warning disable CS0618

public delegate void ReorderCallback(int oldIndex, int newIndex);

public delegate Widget ReorderItemProxyDecorator(
    Widget child,
    int index,
    Animation<double> animation);

public delegate DragBoundaryDelegate<Rect>? ReorderDragBoundaryProvider(BuildContext context);

public readonly record struct SliverLayoutDimensions(
    double ScrollOffset,
    double PrecedingScrollExtent,
    double ViewportMainAxisExtent,
    double CrossAxisExtent);

public delegate double? ItemExtentBuilder(int index, SliverLayoutDimensions dimensions);

/// <summary>A scrolling list whose keyed children can be reordered by a drag listener.</summary>
public sealed class ReorderableList : StatefulWidget
{
    public ReorderableList(
        IndexedWidgetBuilder itemBuilder,
        int itemCount,
        ReorderCallback? onReorder = null,
        ReorderCallback? onReorderItem = null,
        Action<int>? onReorderStart = null,
        Action<int>? onReorderEnd = null,
        double? itemExtent = null,
        ItemExtentBuilder? itemExtentBuilder = null,
        Widget? prototypeItem = null,
        ReorderItemProxyDecorator? proxyDecorator = null,
        Thickness? padding = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        double cacheExtent = 250.0,
        double? autoScrollerVelocityScalar = null,
        ReorderDragBoundaryProvider? dragBoundaryProvider = null,
        Key? key = null) : base(key)
    {
        ValidateArguments(
            itemCount,
            onReorder,
            onReorderItem,
            itemExtent,
            itemExtentBuilder,
            prototypeItem,
            cacheExtent,
            autoScrollerVelocityScalar);
        ItemBuilder = itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder));
        ItemCount = itemCount;
        OnReorder = onReorder;
        OnReorderItem = onReorderItem;
        OnReorderStart = onReorderStart;
        OnReorderEnd = onReorderEnd;
        ItemExtent = itemExtent;
        ItemExtentBuilder = itemExtentBuilder;
        PrototypeItem = prototypeItem;
        ProxyDecorator = proxyDecorator;
        Padding = padding ?? default;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        Physics = physics;
        ShrinkWrap = shrinkWrap;
        CacheExtent = cacheExtent;
        AutoScrollerVelocityScalar = autoScrollerVelocityScalar ?? 50.0;
        DragBoundaryProvider = dragBoundaryProvider;
    }

    public IndexedWidgetBuilder ItemBuilder { get; }

    public int ItemCount { get; }

    [Obsolete("Use OnReorderItem, which receives an index adjusted for the removed item.")]
    public ReorderCallback? OnReorder { get; }

    public ReorderCallback? OnReorderItem { get; }

    public Action<int>? OnReorderStart { get; }

    public Action<int>? OnReorderEnd { get; }

    public double? ItemExtent { get; }

    public ItemExtentBuilder? ItemExtentBuilder { get; }

    public Widget? PrototypeItem { get; }

    public ReorderItemProxyDecorator? ProxyDecorator { get; }

    public Thickness Padding { get; }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public bool? Primary { get; }

    public ScrollPhysics? Physics { get; }

    public bool ShrinkWrap { get; }

    public double CacheExtent { get; }

    public double AutoScrollerVelocityScalar { get; }

    public ReorderDragBoundaryProvider? DragBoundaryProvider { get; }

    public override State CreateState() => new ReorderableListState();

    internal static void ValidateArguments(
        int itemCount,
        ReorderCallback? onReorder,
        ReorderCallback? onReorderItem,
        double? itemExtent,
        ItemExtentBuilder? itemExtentBuilder,
        Widget? prototypeItem,
        double cacheExtent,
        double? autoScrollerVelocityScalar)
    {
        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount));
        }

        if ((onReorder is null) == (onReorderItem is null))
        {
            throw new ArgumentException("Exactly one of onReorder and onReorderItem must be provided.");
        }

        int extentOptions = (itemExtent.HasValue ? 1 : 0)
                            + (itemExtentBuilder is not null ? 1 : 0)
                            + (prototypeItem is not null ? 1 : 0);
        if (extentOptions > 1)
        {
            throw new ArgumentException("Only one of itemExtent, itemExtentBuilder, and prototypeItem may be set.");
        }

        if (itemExtent.HasValue && (!double.IsFinite(itemExtent.Value) || itemExtent.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent));
        }

        if (!double.IsFinite(cacheExtent) || cacheExtent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cacheExtent));
        }

        if (autoScrollerVelocityScalar.HasValue
            && (!double.IsFinite(autoScrollerVelocityScalar.Value) || autoScrollerVelocityScalar.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(autoScrollerVelocityScalar));
        }
    }
}

public sealed class ReorderableListState : State
{
    private readonly GlobalKey<SliverReorderableListState> _sliverKey =
        new LabeledGlobalKey<SliverReorderableListState>("ReorderableList");

    private ReorderableList CurrentWidget => (ReorderableList)StateWidget;

    public void CancelReorder()
    {
        _sliverKey.CurrentState?.CancelReorder();
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        bool usePrimary = widget.Primary ?? (widget.ScrollDirection == Axis.Vertical && widget.Controller is null);
        ScrollController? effectiveController = widget.Controller;
        if (effectiveController is null && usePrimary)
        {
            effectiveController = PrimaryScrollController.MaybeOf(context);
        }

        Widget sliver = new SliverReorderableList(
            itemBuilder: widget.ItemBuilder,
            itemCount: widget.ItemCount,
            onReorder: widget.OnReorder,
            onReorderItem: widget.OnReorderItem,
            onReorderStart: widget.OnReorderStart,
            onReorderEnd: widget.OnReorderEnd,
            itemExtent: widget.ItemExtent,
            itemExtentBuilder: widget.ItemExtentBuilder,
            prototypeItem: widget.PrototypeItem,
            proxyDecorator: widget.ProxyDecorator,
            autoScrollerVelocityScalar: widget.AutoScrollerVelocityScalar,
            dragBoundaryProvider: widget.DragBoundaryProvider,
            scrollController: effectiveController,
            key: _sliverKey);

        if (HasPadding(widget.Padding))
        {
            sliver = new SliverPadding(widget.Padding, sliver);
        }

        return new CustomScrollView(
            slivers: [sliver],
            scrollDirection: widget.ScrollDirection,
            reverse: widget.Reverse,
            controller: effectiveController,
            primary: widget.Primary,
            physics: widget.Physics,
            cacheExtent: widget.CacheExtent,
            shrinkWrap: widget.ShrinkWrap);
    }

    private static bool HasPadding(Thickness padding)
    {
        return padding.Left != 0 || padding.Top != 0 || padding.Right != 0 || padding.Bottom != 0;
    }
}

/// <summary>A sliver list that coordinates keyed item drag, gap, proxy, and reorder behavior.</summary>
public sealed class SliverReorderableList : StatefulWidget
{
    public SliverReorderableList(
        IndexedWidgetBuilder itemBuilder,
        int itemCount,
        ReorderCallback? onReorder = null,
        ReorderCallback? onReorderItem = null,
        Action<int>? onReorderStart = null,
        Action<int>? onReorderEnd = null,
        double? itemExtent = null,
        ItemExtentBuilder? itemExtentBuilder = null,
        Widget? prototypeItem = null,
        ReorderItemProxyDecorator? proxyDecorator = null,
        double autoScrollerVelocityScalar = 50.0,
        ReorderDragBoundaryProvider? dragBoundaryProvider = null,
        ScrollController? scrollController = null,
        Key? key = null) : base(key)
    {
        ReorderableList.ValidateArguments(
            itemCount,
            onReorder,
            onReorderItem,
            itemExtent,
            itemExtentBuilder,
            prototypeItem,
            cacheExtent: 0,
            autoScrollerVelocityScalar);
        ItemBuilder = itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder));
        ItemCount = itemCount;
        OnReorder = onReorder;
        OnReorderItem = onReorderItem;
        OnReorderStart = onReorderStart;
        OnReorderEnd = onReorderEnd;
        ItemExtent = itemExtent;
        ItemExtentBuilder = itemExtentBuilder;
        PrototypeItem = prototypeItem;
        ProxyDecorator = proxyDecorator;
        AutoScrollerVelocityScalar = autoScrollerVelocityScalar;
        DragBoundaryProvider = dragBoundaryProvider;
        ScrollController = scrollController;
    }

    public IndexedWidgetBuilder ItemBuilder { get; }

    public int ItemCount { get; }

    [Obsolete("Use OnReorderItem, which receives an index adjusted for the removed item.")]
    public ReorderCallback? OnReorder { get; }

    public ReorderCallback? OnReorderItem { get; }

    public Action<int>? OnReorderStart { get; }

    public Action<int>? OnReorderEnd { get; }

    public double? ItemExtent { get; }

    public ItemExtentBuilder? ItemExtentBuilder { get; }

    public Widget? PrototypeItem { get; }

    public ReorderItemProxyDecorator? ProxyDecorator { get; }

    public double AutoScrollerVelocityScalar { get; }

    public ReorderDragBoundaryProvider? DragBoundaryProvider { get; }

    internal ScrollController? ScrollController { get; }

    public override State CreateState() => new SliverReorderableListState();

    public static SliverReorderableListState? MaybeOf(BuildContext context)
    {
        return context.GetInherited<ReorderableListScope>()?.ListState;
    }

    public static SliverReorderableListState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("No SliverReorderableList ancestor was found.");
    }
}

public sealed class SliverReorderableListState : State
{
    private readonly Dictionary<int, ReorderableItemState> _items = [];
    private AnimationController? _proxyAnimation;
    private ReorderDragRecognizer? _recognizer;
    private int? _dragIndex;
    private int? _insertIndex;
    private Rect _dragOriginBounds;
    private Point _dragInitialPosition;
    private Point _dragPosition;
    private DragBoundaryDelegate<Rect>? _dragBoundary;

    internal SliverReorderableList CurrentWidget => (SliverReorderableList)StateWidget;

    internal Rect DragOriginBounds => _dragOriginBounds;

    internal bool IsDragging => _dragIndex.HasValue;

    internal int? DragIndex => _dragIndex;

    internal Vector DragTranslation => _dragPosition - _dragInitialPosition;

    internal Animation<double> ProxyAnimation => _proxyAnimation!;

    public override void InitState()
    {
        _proxyAnimation = new AnimationController(TimeSpan.FromMilliseconds(300))
        {
            Curve = Curves.EaseInOut,
        };
        _proxyAnimation.Changed += HandleProxyAnimationChanged;
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        if (((SliverReorderableList)oldWidget).ItemCount != CurrentWidget.ItemCount)
        {
            CancelReorder();
        }
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var childDelegate = new SliverChildBuilderDelegate(BuildItem, widget.ItemCount);
        Widget sliver;
        if (widget.ItemExtent.HasValue)
        {
            sliver = new SliverFixedExtentList(childDelegate, widget.ItemExtent.Value);
        }
        else if (widget.ItemExtentBuilder is not null)
        {
            sliver = new SliverVariedExtentList(childDelegate, widget.ItemExtentBuilder);
        }
        else if (widget.PrototypeItem is not null)
        {
            sliver = new SliverPrototypeExtentList(childDelegate, widget.PrototypeItem);
        }
        else
        {
            sliver = new SliverList(childDelegate);
        }

        return new ReorderableListScope(this, sliver);
    }

    public override void Dispose()
    {
        _recognizer?.Dispose();
        _proxyAnimation!.Changed -= HandleProxyAnimationChanged;
        _proxyAnimation.Dispose();
        _proxyAnimation = null;
    }

    public void CancelReorder()
    {
        if (!IsDragging && _recognizer is null)
        {
            return;
        }

        ResetDrag();
        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    internal void StartItemDragReorder(int index, PointerDownEvent @event, bool delayed)
    {
        if (index < 0 || index >= CurrentWidget.ItemCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (!_items.ContainsKey(index))
        {
            throw new InvalidOperationException("Attempting to start a drag on a non-visible item.");
        }

        _recognizer?.Dispose();
        _recognizer = new ReorderDragRecognizer(
            delayed: delayed,
            onStart: position => BeginDrag(index, position),
            onUpdate: HandleDragUpdate,
            onEnd: CompleteDrag,
            onCancel: CancelReorder);
        _recognizer.AddPointer(@event);
    }

    internal void RegisterItem(ReorderableItemState item)
    {
        _items[item.Index] = item;
        item.SetDragging(item.Index == _dragIndex);
        UpdateItemGap(item, animate: false);
    }

    internal void UnregisterItem(int index, ReorderableItemState item)
    {
        if (_items.TryGetValue(index, out var registered) && ReferenceEquals(registered, item))
        {
            _items.Remove(index);
        }
    }

    private Widget BuildItem(BuildContext context, int index)
    {
        if (index < 0 || index >= CurrentWidget.ItemCount)
        {
            return new SizedBox();
        }

        Widget child = CurrentWidget.ItemBuilder(context, index);
        if (child.Key is null)
        {
            throw new InvalidOperationException("All ReorderableList items must have a key.");
        }

        return new ReorderableItem(
            index: index,
            child: child,
            key: new ReorderableItemKey(child.Key, this));
    }

    private void BeginDrag(int index, Point position)
    {
        if (!_items.TryGetValue(index, out var item) || !item.TryGetGeometry(out Rect geometry))
        {
            CancelReorder();
            return;
        }

        _dragIndex = index;
        _insertIndex = index;
        _dragOriginBounds = geometry;
        _dragInitialPosition = position;
        _dragBoundary = CurrentWidget.DragBoundaryProvider is null
            ? DragBoundary.ForRectMaybeOf(Context)
            : CurrentWidget.DragBoundaryProvider(Context);
        _dragPosition = ConstrainDragPosition(position);
        item.SetDragging(true);
        _proxyAnimation!.Forward(from: 0.0);
        CurrentWidget.OnReorderStart?.Invoke(index);
        SetState(static () => { });
    }

    private void HandleDragUpdate(Point position)
    {
        if (!IsDragging)
        {
            return;
        }

        _dragPosition = ConstrainDragPosition(position);
        UpdateInsertionIndex();
        AutoScroll(_dragPosition);
        SetState(static () => { });
    }

    private void CompleteDrag()
    {
        if (!_dragIndex.HasValue || !_insertIndex.HasValue)
        {
            ResetDrag();
            return;
        }

        int oldIndex = _dragIndex.Value;
        int insertionIndex = _insertIndex.Value;
        CurrentWidget.OnReorderEnd?.Invoke(insertionIndex);

        if (CurrentWidget.OnReorder is not null)
        {
            if (oldIndex != insertionIndex)
            {
#pragma warning disable CS0618
                CurrentWidget.OnReorder(oldIndex, insertionIndex);
#pragma warning restore CS0618
            }
        }
        else
        {
            int newIndex = insertionIndex > oldIndex ? insertionIndex - 1 : insertionIndex;
            if (oldIndex != newIndex)
            {
                CurrentWidget.OnReorderItem?.Invoke(oldIndex, newIndex);
            }
        }

        ResetDrag();
        SetState(static () => { });
    }

    private void UpdateInsertionIndex()
    {
        int dragIndex = _dragIndex!.Value;
        double gapExtent = MainAxisExtent(_dragOriginBounds.Size);
        Vector translation = DragTranslation;
        double proxyStart = MainAxisOffset(_dragOriginBounds.TopLeft + translation);
        double proxyEnd = proxyStart + gapExtent;
        int newIndex = _insertIndex!.Value;

        foreach (var pair in _items.OrderBy(pair => pair.Key))
        {
            ReorderableItemState item = pair.Value;
            if (!item.TryGetTargetGeometry(out Rect geometry))
            {
                continue;
            }

            double itemStart = MainAxisOffset(geometry.TopLeft);
            double itemExtent = MainAxisExtent(geometry.Size);
            double itemEnd = itemStart + itemExtent;
            double itemMiddle = itemStart + (itemExtent / 2.0);

            if (CurrentWidgetIsReverse())
            {
                if (item.Index == dragIndex)
                {
                    continue;
                }

                if (itemEnd >= proxyEnd && proxyEnd >= itemMiddle)
                {
                    newIndex = item.Index;
                    break;
                }

                if (itemMiddle >= proxyStart && proxyStart >= itemStart)
                {
                    newIndex = item.Index + 1;
                    break;
                }

                if (itemStart > proxyEnd && newIndex < item.Index + 1)
                {
                    newIndex = item.Index + 1;
                }
                else if (proxyStart > itemEnd && newIndex > item.Index)
                {
                    newIndex = item.Index;
                }
            }
            else if (item.Index == dragIndex)
            {
                if (itemMiddle <= proxyEnd && proxyEnd <= itemEnd)
                {
                    newIndex = dragIndex;
                }
            }
            else if (itemStart <= proxyStart && proxyStart <= itemMiddle)
            {
                newIndex = item.Index;
                break;
            }
            else if (itemMiddle <= proxyEnd && proxyEnd <= itemEnd)
            {
                newIndex = item.Index + 1;
                break;
            }
            else if (itemEnd < proxyStart && newIndex < item.Index + 1)
            {
                newIndex = item.Index + 1;
            }
            else if (proxyEnd < itemStart && newIndex > item.Index)
            {
                newIndex = item.Index;
            }
        }

        newIndex = Math.Clamp(newIndex, 0, CurrentWidget.ItemCount);
        if (newIndex == _insertIndex)
        {
            return;
        }

        _insertIndex = newIndex;
        foreach (ReorderableItemState item in _items.Values)
        {
            UpdateItemGap(item, animate: true);
        }
    }

    private void UpdateItemGap(ReorderableItemState item, bool animate)
    {
        if (!_dragIndex.HasValue || !_insertIndex.HasValue || item.Index == _dragIndex.Value)
        {
            item.UpdateGap(default, animate);
            return;
        }

        int dragIndex = _dragIndex.Value;
        int gapIndex = _insertIndex.Value;
        double gapExtent = MainAxisExtent(_dragOriginBounds.Size);
        double signedExtent = CurrentWidgetIsReverse() ? -gapExtent : gapExtent;
        Vector target = default;
        if (gapIndex < dragIndex && item.Index < dragIndex && item.Index >= gapIndex)
        {
            target = ExtentOffset(signedExtent);
        }
        else if (gapIndex > dragIndex && item.Index > dragIndex && item.Index < gapIndex)
        {
            target = ExtentOffset(-signedExtent);
        }

        item.UpdateGap(target, animate);
    }

    private void AutoScroll(Point position)
    {
        ScrollController? controller = CurrentWidget.ScrollController;
        if (controller?.PrimaryPosition is not { } scrollPosition || !TryGetViewportBounds(out Rect viewport))
        {
            return;
        }

        const double edgeExtent = 24.0;
        double coordinate = MainAxisOffset(position);
        double start = MainAxisOffset(viewport.TopLeft);
        double end = CurrentWidgetAxis() == Axis.Vertical ? viewport.Bottom : viewport.Right;
        double overscrollFraction = 0;
        if (coordinate < start + edgeExtent)
        {
            overscrollFraction = -Math.Clamp((start + edgeExtent - coordinate) / edgeExtent, 0, 1);
        }
        else if (coordinate > end - edgeExtent)
        {
            overscrollFraction = Math.Clamp((coordinate - (end - edgeExtent)) / edgeExtent, 0, 1);
        }

        if (Math.Abs(overscrollFraction) <= double.Epsilon)
        {
            return;
        }

        double direction = CurrentWidgetIsReverse() ? -1.0 : 1.0;
        double delta = overscrollFraction * direction * CurrentWidget.AutoScrollerVelocityScalar * 0.016;
        controller.JumpTo(scrollPosition.Pixels + delta);
    }

    private bool TryGetViewportBounds(out Rect bounds)
    {
        RenderObject? renderObject = Context.FindRenderObject();
        while (renderObject is not null && renderObject is not RenderViewport)
        {
            renderObject = renderObject.Parent;
        }

        if (renderObject is not RenderBox viewport || !viewport.HasSize
            || !viewport.TryGetTransformFromRoot(out Matrix transform))
        {
            bounds = default;
            return false;
        }

        Point topLeft = transform.Transform(new Point());
        bounds = new Rect(topLeft, viewport.Size);
        return true;
    }

    private void ResetDrag()
    {
        _recognizer?.Dispose();
        _recognizer = null;
        foreach (ReorderableItemState item in _items.Values)
        {
            item.SetDragging(false);
            item.UpdateGap(default, animate: false);
        }

        _dragIndex = null;
        _insertIndex = null;
        _dragOriginBounds = default;
        _dragInitialPosition = default;
        _dragPosition = default;
        _dragBoundary = null;
        _proxyAnimation?.Stop();
        _proxyAnimation?.SetValue(0.0);
    }

    private Axis CurrentWidgetAxis()
    {
        RenderObject? renderObject = Context.FindRenderObject();
        while (renderObject is not null && renderObject is not RenderViewport)
        {
            renderObject = renderObject.Parent;
        }

        return renderObject is RenderViewport viewport ? viewport.Axis : Axis.Vertical;
    }

    private bool CurrentWidgetIsReverse()
    {
        RenderObject? renderObject = Context.FindRenderObject();
        while (renderObject is not null && renderObject is not RenderViewport)
        {
            renderObject = renderObject.Parent;
        }

        return renderObject is RenderViewport viewport
               && viewport.AxisDirection is AxisDirection.Up or AxisDirection.Left;
    }

    private double MainAxisOffset(Point point) => CurrentWidgetAxis() == Axis.Vertical ? point.Y : point.X;

    private double MainAxisExtent(Size size) => CurrentWidgetAxis() == Axis.Vertical ? size.Height : size.Width;

    private Vector ExtentOffset(double extent)
    {
        return CurrentWidgetAxis() == Axis.Vertical ? new Vector(0, extent) : new Vector(extent, 0);
    }

    private Point ConstrainDragPosition(Point position)
    {
        if (_dragBoundary is null)
        {
            return position;
        }

        Vector rawTranslation = position - _dragInitialPosition;
        Rect draggedBounds = new(
            _dragOriginBounds.X + rawTranslation.X,
            _dragOriginBounds.Y + rawTranslation.Y,
            _dragOriginBounds.Width,
            _dragOriginBounds.Height);
        Rect constrainedBounds = _dragBoundary.NearestPositionWithinBoundary(draggedBounds);
        Vector constrainedTranslation = constrainedBounds.TopLeft - _dragOriginBounds.TopLeft;
        return _dragInitialPosition + constrainedTranslation;
    }

    private void HandleProxyAnimationChanged()
    {
        if (Mounted && IsDragging)
        {
            SetState(static () => { });
        }
    }
}

internal sealed class ReorderableListScope : InheritedWidget
{
    public ReorderableListScope(SliverReorderableListState listState, Widget child)
    {
        ListState = listState;
        Child = child;
    }

    public SliverReorderableListState ListState { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((ReorderableListScope)oldWidget).ListState, ListState);
    }
}

internal sealed record ReorderableItemKey(Key SubKey, SliverReorderableListState Owner) : GlobalKey;

internal sealed class ReorderableItem : StatefulWidget
{
    public ReorderableItem(int index, Widget child, Key key) : base(key)
    {
        Index = index;
        Child = child;
    }

    public int Index { get; }

    public Widget Child { get; }

    public override State CreateState() => new ReorderableItemState();
}

internal sealed class ReorderableItemState : State
{
    private SliverReorderableListState? _listState;
    private AnimationController? _offsetAnimation;
    private Vector _offsetBegin;
    private Vector _offsetTarget;
    private bool _dragging;

    private ReorderableItem CurrentWidget => (ReorderableItem)StateWidget;

    public int Index => CurrentWidget.Index;

    public override void DidChangeDependencies()
    {
        var nextListState = SliverReorderableList.Of(Context);
        if (!ReferenceEquals(_listState, nextListState))
        {
            _listState?.UnregisterItem(Index, this);
            _listState = nextListState;
        }

        _listState.RegisterItem(this);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldItem = (ReorderableItem)oldWidget;
        if (oldItem.Index != Index)
        {
            _listState?.UnregisterItem(oldItem.Index, this);
            _listState?.RegisterItem(this);
        }
    }

    public override Widget Build(BuildContext context)
    {
        Widget child = CurrentWidget.Child;
        if (_dragging)
        {
            if (_listState!.CurrentWidget.ProxyDecorator is not null)
            {
                child = _listState.CurrentWidget.ProxyDecorator(
                    child,
                    Index,
                    _listState.ProxyAnimation);
            }

            Vector translation = _listState.DragTranslation;
            child = new Transform(
                Matrix.CreateTranslation(translation.X, translation.Y),
                new IgnorePointer(ignoring: true, child: child));
        }
        else
        {
            Vector offset = EvaluateOffset();
            if (offset != default)
            {
                child = new Transform(Matrix.CreateTranslation(offset.X, offset.Y), child);
            }
        }

        return child;
    }

    public override void Deactivate()
    {
        _listState?.UnregisterItem(Index, this);
        base.Deactivate();
    }

    public override void Dispose()
    {
        _listState?.UnregisterItem(Index, this);
        DisposeOffsetAnimation();
    }

    internal void SetDragging(bool dragging)
    {
        if (_dragging == dragging)
        {
            return;
        }

        _dragging = dragging;
        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    internal void UpdateGap(Vector target, bool animate)
    {
        Vector current = EvaluateOffset();
        if (_offsetTarget == target && (animate || _offsetAnimation is null))
        {
            return;
        }

        _offsetBegin = animate ? current : target;
        _offsetTarget = target;
        DisposeOffsetAnimation();
        if (animate)
        {
            _offsetAnimation = new AnimationController(TimeSpan.FromMilliseconds(250))
            {
                Curve = Curves.EaseInOut,
            };
            _offsetAnimation.Changed += HandleOffsetAnimationChanged;
            _offsetAnimation.Completed += HandleOffsetAnimationCompleted;
            _offsetAnimation.Forward(from: 0.0);
        }

        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    internal bool TryGetGeometry(out Rect geometry)
    {
        if (Context.FindRenderObject() is not RenderBox renderBox || !renderBox.HasSize
            || !renderBox.TryGetTransformFromRoot(out Matrix transform))
        {
            geometry = default;
            return false;
        }

        Point topLeft = transform.Transform(new Point());
        geometry = new Rect(topLeft, renderBox.Size);
        return true;
    }

    internal bool TryGetTargetGeometry(out Rect geometry)
    {
        if (!TryGetGeometry(out geometry))
        {
            return false;
        }

        if (_dragging)
        {
            Vector translation = _listState!.DragTranslation;
            Rect origin = _listState.DragOriginBounds;
            geometry = new Rect(
                origin.X + translation.X,
                origin.Y + translation.Y,
                origin.Width,
                origin.Height);
        }

        return true;
    }

    private Vector EvaluateOffset()
    {
        double t = _offsetAnimation?.Value ?? 1.0;
        return new Vector(
            _offsetBegin.X + ((_offsetTarget.X - _offsetBegin.X) * t),
            _offsetBegin.Y + ((_offsetTarget.Y - _offsetBegin.Y) * t));
    }

    private void HandleOffsetAnimationChanged()
    {
        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    private void HandleOffsetAnimationCompleted()
    {
        _offsetBegin = _offsetTarget;
        DisposeOffsetAnimation();
    }

    private void DisposeOffsetAnimation()
    {
        if (_offsetAnimation is null)
        {
            return;
        }

        _offsetAnimation.Changed -= HandleOffsetAnimationChanged;
        _offsetAnimation.Completed -= HandleOffsetAnimationCompleted;
        _offsetAnimation.Dispose();
        _offsetAnimation = null;
    }
}

/// <summary>Starts an item drag immediately after the primary pointer is pressed.</summary>
public class ReorderableDragStartListener : StatelessWidget
{
    public ReorderableDragStartListener(
        Widget child,
        int index,
        bool enabled = true,
        Key? key = null) : base(key)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Child = child;
        Index = index;
        Enabled = enabled;
    }

    public Widget Child { get; }

    public int Index { get; }

    public bool Enabled { get; }

    protected virtual bool Delayed => false;

    public override Widget Build(BuildContext context)
    {
        return new Listener(
            onPointerDown: Enabled
                ? @event => SliverReorderableList.MaybeOf(context)
                    ?.StartItemDragReorder(Index, @event, Delayed)
                : null,
            behavior: HitTestBehavior.Opaque,
            child: Child);
    }
}

/// <summary>Starts an item drag after Flutter's 500ms long-press deadline.</summary>
public sealed class ReorderableDelayedDragStartListener : ReorderableDragStartListener
{
    public ReorderableDelayedDragStartListener(
        Widget child,
        int index,
        bool enabled = true,
        Key? key = null) : base(child, index, enabled, key)
    {
    }

    protected override bool Delayed => true;
}

internal sealed class ReorderDragRecognizer : GestureRecognizer, IGestureArenaMember
{
    private const double TouchSlop = 18.0;
    private readonly bool _delayed;
    private readonly Action<Point> _onStart;
    private readonly Action<Point> _onUpdate;
    private readonly Action _onEnd;
    private readonly Action _onCancel;
    private GestureArenaEntry _arenaEntry;
    private CancellationTokenSource? _deadlineCancellation;
    private Point _initialPosition;
    private int? _pointer;
    private bool _accepted;
    private bool _deadlineExceeded;

    public ReorderDragRecognizer(
        bool delayed,
        Action<Point> onStart,
        Action<Point> onUpdate,
        Action onEnd,
        Action onCancel)
    {
        _delayed = delayed;
        _onStart = onStart;
        _onUpdate = onUpdate;
        _onEnd = onEnd;
        _onCancel = onCancel;
    }

    public override void AddPointer(PointerDownEvent @event)
    {
        if (_pointer.HasValue)
        {
            return;
        }

        _pointer = @event.Pointer;
        _initialPosition = @event.Position;
        _arenaEntry = GestureArena.Add(@event.Pointer, this);
        StartTrackingPointer(@event.Pointer);
        if (_delayed)
        {
            StartDeadline(@event.Pointer);
        }
        else
        {
            _deadlineExceeded = true;
            _arenaEntry.Resolve(GestureDisposition.Accepted);
        }
    }

    public void AcceptGesture(int pointer)
    {
        if (_pointer != pointer || !_deadlineExceeded)
        {
            return;
        }

        _accepted = true;
        _onStart(_initialPosition);
    }

    public void RejectGesture(int pointer)
    {
        if (_pointer != pointer)
        {
            return;
        }

        Cleanup();
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        switch (@event)
        {
            case PointerMoveEvent:
                if (!_accepted && Distance(_initialPosition, @event.Position) > TouchSlop)
                {
                    _arenaEntry.Resolve(GestureDisposition.Rejected);
                }
                else if (_accepted)
                {
                    _onUpdate(@event.Position);
                }

                break;
            case PointerUpEvent:
                if (_accepted)
                {
                    _onEnd();
                }
                else
                {
                    _arenaEntry.Resolve(GestureDisposition.Rejected);
                }

                Cleanup();
                break;
            case PointerCancelEvent:
                if (_accepted)
                {
                    _onCancel();
                }

                _arenaEntry.Resolve(GestureDisposition.Rejected);
                Cleanup();
                break;
        }
    }

    public override void Dispose()
    {
        Cleanup();
        base.Dispose();
    }

    private void StartDeadline(int pointer)
    {
        _deadlineCancellation = new CancellationTokenSource();
        CancellationToken token = _deadlineCancellation.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_pointer != pointer || token.IsCancellationRequested)
                {
                    return;
                }

                _deadlineExceeded = true;
                _arenaEntry.Resolve(GestureDisposition.Accepted);
            });
        });
    }

    private void Cleanup()
    {
        _deadlineCancellation?.Cancel();
        _deadlineCancellation?.Dispose();
        _deadlineCancellation = null;
        if (_pointer is int pointer)
        {
            StopTrackingPointer(pointer);
        }

        _pointer = null;
        _accepted = false;
    }

    private static double Distance(Point first, Point second)
    {
        double dx = first.X - second.X;
        double dy = first.Y - second.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}

#pragma warning restore CS0618
