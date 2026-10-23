using Avalonia;
using Avalonia.Media;
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
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        double? autoScrollerVelocityScalar = null,
        ReorderDragBoundaryProvider? dragBoundaryProvider = null,
        Key? key = null,
        double anchor = 0.0,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge) : base(key)
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
        ScrollCacheExtent = scrollCacheExtent;
        AutoScrollerVelocityScalar = autoScrollerVelocityScalar;
        DragBoundaryProvider = dragBoundaryProvider;
        Anchor = anchor;
        DragStartBehavior = dragStartBehavior;
        KeyboardDismissBehavior = keyboardDismissBehavior;
        RestorationId = restorationId;
        ClipBehavior = clipBehavior;
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

    [Obsolete("Use ScrollCacheExtent.")]
    public double? CacheExtent { get; }

    public ScrollCacheExtent? ScrollCacheExtent { get; }

    public double? AutoScrollerVelocityScalar { get; }

    public ReorderDragBoundaryProvider? DragBoundaryProvider { get; }

    public double Anchor { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public ScrollViewKeyboardDismissBehavior? KeyboardDismissBehavior { get; }

    public string? RestorationId { get; }

    public Clip ClipBehavior { get; }

    public override State CreateState() => new ReorderableListState();

    public static ReorderableListState? MaybeOf(BuildContext context)
    {
        return context.FindAncestorStateOfType<ReorderableListState>();
    }

    public static ReorderableListState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("No ReorderableList ancestor was found.");
    }

    internal static void ValidateArguments(
        int itemCount,
        ReorderCallback? onReorder,
        ReorderCallback? onReorderItem,
        double? itemExtent,
        ItemExtentBuilder? itemExtentBuilder,
        Widget? prototypeItem,
        double? cacheExtent,
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

        if (cacheExtent.HasValue && (!double.IsFinite(cacheExtent.Value) || cacheExtent.Value < 0))
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

public sealed class ReorderableListState : State<ReorderableList>
{
    private readonly GlobalKey<SliverReorderableListState> _sliverKey =
        new LabeledGlobalKey<SliverReorderableListState>("ReorderableList");

    private ReorderableList CurrentWidget => (ReorderableList)StateWidget;

    public void CancelReorder()
    {
        _sliverKey.CurrentState?.CancelReorder();
    }

    public void StartItemDragReorder(int index, PointerDownEvent @event, bool delayed = false)
    {
        _sliverKey.CurrentState?.StartItemDragReorder(index, @event, delayed);
    }

    public void StartItemDragReorder(
        int index,
        PointerDownEvent @event,
        MultiDragGestureRecognizer recognizer)
    {
        _sliverKey.CurrentState?.StartItemDragReorder(index, @event, recognizer);
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        ScrollCacheExtent? effectiveCacheExtent = widget.ScrollCacheExtent
            ?? (widget.CacheExtent is { } pixels ? ScrollCacheExtent.Pixels(pixels) : null);

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
            key: _sliverKey);

        sliver = new SliverPadding(widget.Padding, sliver);

        return new CustomScrollView(
            slivers: [sliver],
            scrollDirection: widget.ScrollDirection,
            reverse: widget.Reverse,
            controller: widget.Controller,
            primary: widget.Primary,
            physics: widget.Physics,
            scrollCacheExtent: effectiveCacheExtent,
            shrinkWrap: widget.ShrinkWrap,
            anchor: widget.Anchor,
            dragStartBehavior: widget.DragStartBehavior,
            keyboardDismissBehavior: widget.KeyboardDismissBehavior,
            restorationId: widget.RestorationId,
            clipBehavior: widget.ClipBehavior);
    }
}

/// <summary>A sliver list that coordinates keyed item drag, gap, proxy, and reorder behavior.</summary>
public sealed class SliverReorderableList : StatefulWidget
{
    public SliverReorderableList(
        IndexedWidgetBuilder itemBuilder,
        int itemCount,
        ChildIndexGetter? findChildIndexCallback = null,
        ReorderCallback? onReorder = null,
        ReorderCallback? onReorderItem = null,
        Action<int>? onReorderStart = null,
        Action<int>? onReorderEnd = null,
        double? itemExtent = null,
        ItemExtentBuilder? itemExtentBuilder = null,
        Widget? prototypeItem = null,
        ReorderItemProxyDecorator? proxyDecorator = null,
        double? autoScrollerVelocityScalar = null,
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
        FindChildIndexCallback = findChildIndexCallback;
        OnReorder = onReorder;
        OnReorderItem = onReorderItem;
        OnReorderStart = onReorderStart;
        OnReorderEnd = onReorderEnd;
        ItemExtent = itemExtent;
        ItemExtentBuilder = itemExtentBuilder;
        PrototypeItem = prototypeItem;
        ProxyDecorator = proxyDecorator;
        AutoScrollerVelocityScalar = autoScrollerVelocityScalar ?? 50.0;
        DragBoundaryProvider = dragBoundaryProvider;
        ScrollController = scrollController;
    }

    public IndexedWidgetBuilder ItemBuilder { get; }

    public int ItemCount { get; }

    public ChildIndexGetter? FindChildIndexCallback { get; }

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
        return context.FindAncestorStateOfType<SliverReorderableListState>();
    }

    public static SliverReorderableListState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("No SliverReorderableList ancestor was found.");
    }
}

public sealed class SliverReorderableListState : State<SliverReorderableList>
{
    private readonly Dictionary<int, ReorderableItemState> _items = [];
    private OverlayEntry? _overlayEntry;
    private int? _dragIndex;
    private ReorderDragInfo? _dragInfo;
    private int? _insertIndex;
    internal Point? FinalDropPosition { get; private set; }
    private MultiDragGestureRecognizer? _recognizer;
    private int? _recognizerPointer;
    private EdgeDraggingAutoScroller? _autoScroller;
    private ScrollableState _scrollable = null!;

    internal SliverReorderableList CurrentWidget => Widget;
    internal Rect DragOriginBounds => _dragInfo is null ? default : new Rect(_dragInfo.Origin, _dragInfo.ItemSize);
    internal bool IsDragging => _dragInfo is not null;
    internal int? DragIndex => _dragIndex;
    internal Vector DragTranslation => _dragInfo is null
        ? default
        : (_dragInfo.DragPosition - _dragInfo.DragOffset) - _dragInfo.Origin;
    internal Axis ScrollAxis => ScrollDirectionUtils.AxisDirectionToAxis(_scrollable.AxisDirection);

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _scrollable = Scrollable.Of(Context);
        if (_autoScroller?.Scrollable != _scrollable)
        {
            ReplaceAutoScroller();
        }
    }

    public override void DidUpdateWidget(SliverReorderableList oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (oldWidget.ItemCount != CurrentWidget.ItemCount)
        {
            CancelReorder();
        }

        if (oldWidget.AutoScrollerVelocityScalar != CurrentWidget.AutoScrollerVelocityScalar)
        {
            ReplaceAutoScroller();
        }
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var childDelegate = new SliverChildBuilderDelegate(
            BuildItem,
            widget.ItemCount,
            findChildIndexCallback: widget.FindChildIndexCallback is null
                ? null
                : key => widget.FindChildIndexCallback(
                    key is ReorderableItemKey itemKey ? itemKey.SubKey : key));
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

        return sliver;
    }

    public override void Dispose()
    {
        ResetDrag();
        _recognizer?.Dispose();
        _autoScroller?.StopAutoScroll();
        base.Dispose();
    }

    public void CancelReorder()
    {
        SetState(ResetDrag);
    }

    // Published convenience overload; listeners use the Dart-shaped recognizer overload below.
    public void StartItemDragReorder(int index, PointerDownEvent @event, bool delayed = false)
    {
        MultiDragGestureRecognizer recognizer = delayed
            ? new DelayedMultiDragGestureRecognizer()
            : new ImmediateMultiDragGestureRecognizer();
        recognizer.GestureSettings = MediaQuery.MaybeGestureSettingsOf(Context);
        StartItemDragReorder(index, @event, recognizer);
    }

    public void StartItemDragReorder(
        int index,
        PointerDownEvent @event,
        MultiDragGestureRecognizer recognizer)
    {
        if (index < 0 || index >= CurrentWidget.ItemCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        SetState(() =>
        {
            if (_dragInfo is not null)
            {
                CancelReorder();
            }
            else if (_recognizer is not null && _recognizerPointer != @event.Pointer)
            {
                _recognizer.Dispose();
                _recognizer = null;
                _recognizerPointer = null;
            }

            if (!_items.ContainsKey(index))
            {
                throw new InvalidOperationException("Attempting to start a drag on a non-visible item.");
            }

            _dragIndex = index;
            _recognizer = recognizer;
            recognizer.OnStart = BeginDrag;
            recognizer.AddPointer(@event);
            _recognizerPointer = @event.Pointer;
        });
    }

    internal void RegisterItem(ReorderableItemState item)
    {
        if (_dragInfo is not null && (!_items.TryGetValue(item.Index, out var previous) || previous != item))
        {
            item.UpdateForGap(_dragInfo.Index, _dragInfo.Index, _dragInfo.ItemExtent, false, CurrentWidgetIsReverse());
        }

        _items[item.Index] = item;
        if (item.Index == _dragInfo?.Index)
        {
            item.SetDragging(true);
        }
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
            return ScrollAxis == Axis.Horizontal
                ? new SizedBox(width: _dragInfo?.ItemExtent)
                : new SizedBox(height: _dragInfo?.ItemExtent);
        }

        Widget child = CurrentWidget.ItemBuilder(context, index);
        if (child.Key is null)
        {
            throw new InvalidOperationException("All ReorderableList items must have a key.");
        }

        OverlayState overlay = Overlay.Of(context);
        return new ReorderableItem(
            index: index,
            child: WrapWithSemantics(context, child, index),
            capturedThemes: InheritedTheme.Capture(context, overlay.Context),
            key: new ReorderableItemKey(child.Key, index, this));
    }

    private Widget WrapWithSemantics(BuildContext context, Widget child, int index)
    {
        var actions = new Dictionary<CustomSemanticsAction, Action>();
        WidgetsLocalizations localizations = WidgetsLocalizations.Of(context);
        bool horizontal = ScrollAxis == Axis.Horizontal;
        TextDirection direction = Directionality.Of(context);

        if (index > 0)
        {
            actions[new CustomSemanticsAction(localizations.ReorderItemToStart)] =
                () => HandleSemanticsReorder(index, 0);
            string before = horizontal
                ? direction == TextDirection.Ltr
                    ? localizations.ReorderItemLeft
                    : localizations.ReorderItemRight
                : localizations.ReorderItemUp;
            actions[new CustomSemanticsAction(before)] = () => HandleSemanticsReorder(index, index - 1);
        }

        if (index < CurrentWidget.ItemCount - 1)
        {
            string after = horizontal
                ? direction == TextDirection.Ltr
                    ? localizations.ReorderItemRight
                    : localizations.ReorderItemLeft
                : localizations.ReorderItemDown;
            actions[new CustomSemanticsAction(after)] = () => HandleSemanticsReorder(index, index + 2);
            actions[new CustomSemanticsAction(localizations.ReorderItemToEnd)] =
                () => HandleSemanticsReorder(index, CurrentWidget.ItemCount);
        }

        return new Semantics(
            child: child,
            container: true,
            customSemanticsActions: actions);
    }

    private void HandleSemanticsReorder(int oldIndex, int insertionIndex)
    {
        InvokeReorder(oldIndex, insertionIndex);
        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    private Drag? BeginDrag(Point position)
    {
        ReorderableItemState item = _items[_dragIndex!.Value];
        item.SetDragging(true);
        CurrentWidget.OnReorderStart?.Invoke(_dragIndex.Value);
        _insertIndex = item.Index;
        _dragInfo = new ReorderDragInfo(
            item, position, ScrollAxis, HandleDragUpdate, HandleDragCancel, CompleteDrag,
            DropCompleted, CurrentWidget.ProxyDecorator, this);
        _dragInfo.StartDrag();
        _overlayEntry = new OverlayEntry(_dragInfo.CreateProxy);
        Overlay.Of(Context).Insert(_overlayEntry);
        foreach (ReorderableItemState child in _items.Values)
        {
            if (child != item && child.Mounted)
            {
                child.UpdateForGap(_insertIndex.Value, _insertIndex.Value,
                    _dragInfo.ItemExtent, false, CurrentWidgetIsReverse());
            }
        }

        return _dragInfo;
    }

    private void HandleDragUpdate(ReorderDragInfo item, Point position, Point delta)
    {
        SetState(() =>
        {
            _overlayEntry?.MarkNeedsBuild();
            UpdateInsertionIndex();
            _autoScroller?.StartAutoScrollIfNecessary(DragTargetRect());
        });
    }

    private void HandleDragCancel(ReorderDragInfo item) => SetState(ResetDrag);

    private void CompleteDrag(ReorderDragInfo item)
    {
        SetState(() =>
        {
            int insertionIndex = _insertIndex!.Value;
            if (insertionIndex - item.Index == 1)
            {
                FinalDropPosition = ItemOffsetAt(insertionIndex - 1);
            }
            else if (insertionIndex == item.Index)
            {
                FinalDropPosition = ItemOffsetAt(insertionIndex);
            }
            else if (CurrentWidgetIsReverse())
            {
                FinalDropPosition = insertionIndex >= _items.Count
                    ? ItemOffsetAt(_items.Count - 1) - ExtentOffset(item.ItemExtent)
                    : ItemOffsetAt(insertionIndex) + ExtentOffset(ItemExtentAt(insertionIndex));
            }
            else
            {
                FinalDropPosition = insertionIndex == 0
                    ? ItemOffsetAt(0) - ExtentOffset(item.ItemExtent)
                    : ItemOffsetAt(insertionIndex - 1) + ExtentOffset(ItemExtentAt(insertionIndex - 1));
            }
        });
        CurrentWidget.OnReorderEnd?.Invoke(_insertIndex!.Value);
    }

    private void DropCompleted()
    {
        InvokeReorder(_dragIndex!.Value, _insertIndex!.Value);
        SetState(ResetDrag);
    }

    private void ResetDrag()
    {
        if (_dragInfo is null)
        {
            return;
        }

        if (_dragIndex.HasValue && _items.TryGetValue(_dragIndex.Value, out var item))
        {
            item.SetDragging(false);
            _dragIndex = null;
        }

        _dragInfo.Dispose();
        _dragInfo = null;
        _autoScroller?.StopAutoScroll();
        foreach (ReorderableItemState child in _items.Values)
        {
            child.ResetGap();
        }

        _recognizer?.Dispose();
        _recognizer = null;
        _overlayEntry?.Remove();
        _overlayEntry?.Dispose();
        _overlayEntry = null;
        FinalDropPosition = null;
    }

    private void ReplaceAutoScroller()
    {
        _autoScroller?.StopAutoScroll();
        _autoScroller = new EdgeDraggingAutoScroller(
            _scrollable, CurrentWidget.AutoScrollerVelocityScalar, HandleAutoScroll);
    }

    private void HandleAutoScroll()
    {
        if (_dragInfo is null)
        {
            return;
        }

        UpdateInsertionIndex();
        _autoScroller?.StartAutoScrollIfNecessary(DragTargetRect());
    }

    private void UpdateInsertionIndex()
    {
        int dragIndex = _dragIndex!.Value;
        double gapExtent = _dragInfo!.ItemExtent;
        double proxyStart = MainAxisOffset(_dragInfo!.DragPosition - _dragInfo.DragOffset);
        double proxyEnd = proxyStart + gapExtent;
        int newIndex = _insertIndex!.Value;

        foreach (var pair in _items)
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

        if (newIndex == _insertIndex)
        {
            return;
        }

        _insertIndex = newIndex;
        foreach (ReorderableItemState item in _items.Values)
        {
            if (item.Index != _dragIndex && item.Mounted)
            {
                item.UpdateForGap(dragIndex, newIndex, gapExtent, true, CurrentWidgetIsReverse());
            }
        }
    }

    private bool CurrentWidgetIsReverse() =>
        _scrollable.AxisDirection is AxisDirection.Up or AxisDirection.Left;

    private double MainAxisOffset(Point point) => ScrollAxis == Axis.Vertical ? point.Y : point.X;
    private double MainAxisExtent(Size size) => ScrollAxis == Axis.Vertical ? size.Height : size.Width;
    private Vector ExtentOffset(double extent) =>
        ScrollAxis == Axis.Vertical ? new Vector(0, extent) : new Vector(extent, 0);
    private Rect DragTargetRect() => new(_dragInfo!.DragPosition - _dragInfo.DragOffset, _dragInfo.ItemSize);
    private Point ItemOffsetAt(int index) => _items[index].TargetGeometry().TopLeft;
    private double ItemExtentAt(int index) => MainAxisExtent(_items[index].TargetGeometry().Size);

    private void InvokeReorder(int oldIndex, int insertionIndex)
    {
        if (CurrentWidget.OnReorder is not null)
        {
            if (oldIndex != insertionIndex)
            {
#pragma warning disable CS0618
                CurrentWidget.OnReorder(oldIndex, insertionIndex);
#pragma warning restore CS0618
            }

            return;
        }

        int newIndex = insertionIndex > oldIndex ? insertionIndex - 1 : insertionIndex;
        if (oldIndex != newIndex)
        {
            CurrentWidget.OnReorderItem?.Invoke(oldIndex, newIndex);
        }
    }
}

internal sealed class ReorderableItemKey(Key subKey, int index, SliverReorderableListState owner) : GlobalKey
{
    public Key SubKey { get; } = subKey;
    public int Index { get; } = index;
    public SliverReorderableListState Owner { get; } = owner;

    public override bool Equals(object? obj)
    {
        return obj is ReorderableItemKey other
            && other.SubKey == SubKey
            && other.Index == Index
            && ReferenceEquals(other.Owner, Owner);
    }

    public override int GetHashCode() => HashCode.Combine(SubKey, Index, Owner);
}

internal sealed class ReorderableItem : StatefulWidget
{
    public ReorderableItem(
        int index,
        Widget child,
        CapturedThemes capturedThemes,
        Key key) : base(key)
    {
        Index = index;
        Child = child;
        CapturedThemes = capturedThemes;
    }

    public int Index { get; }

    public Widget Child { get; }

    public CapturedThemes CapturedThemes { get; }

    public override State CreateState() => new ReorderableItemState();
}

internal sealed class ReorderableItemState : State<ReorderableItem>
{
    private SliverReorderableListState _listState = null!;
    private Vector _startOffset;
    private Vector _targetOffset;
    private AnimationController? _offsetAnimation;
    private bool _dragging;

    private ReorderableItem CurrentWidget => Widget;
    public int Index => CurrentWidget.Index;
    internal Widget Child => CurrentWidget.Child;
    internal CapturedThemes CapturedThemes => CurrentWidget.CapturedThemes;
    internal SliverReorderableListState ListState => _listState;

    public override void InitState()
    {
        _listState = SliverReorderableList.Of(Context);
        _listState.RegisterItem(this);
        base.InitState();
    }

    public override void Dispose()
    {
        DisposeOffsetAnimation();
        _listState.UnregisterItem(Index, this);
        base.Dispose();
    }

    public override void DidUpdateWidget(ReorderableItem oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (oldWidget.Index != Index)
        {
            _listState.UnregisterItem(oldWidget.Index, this);
            _listState.RegisterItem(this);
        }
    }

    public override Widget Build(BuildContext context)
    {
        if (_dragging)
        {
            Size size = _listState.DragOriginBounds.Size;
            return _listState.ScrollAxis == Axis.Vertical
                ? new SizedBox(width: 0, height: size.Height)
                : new SizedBox(width: size.Width, height: 0);
        }

        _listState.RegisterItem(this);
        Vector offset = Offset;
        return new Transform(Matrix4.TranslationValues(offset.X, offset.Y, 0.0), CurrentWidget.Child);
    }

    public override void Deactivate()
    {
        _listState.UnregisterItem(Index, this);
        base.Deactivate();
    }

    internal void SetDragging(bool dragging)
    {
        if (Mounted)
        {
            SetState(() => _dragging = dragging);
        }
    }

    internal Vector Offset
    {
        get
        {
            double t = _offsetAnimation is null ? 1.0 : Curves.EaseInOut.Transform(_offsetAnimation.Value);
            return _startOffset + ((_targetOffset - _startOffset) * t);
        }
    }

    internal void UpdateForGap(int dragIndex, int gapIndex, double gapExtent, bool animate, bool reverse)
    {
        double extent = 0;
        if (gapIndex < dragIndex && Index < dragIndex && Index >= gapIndex)
        {
            extent = reverse ? -gapExtent : gapExtent;
        }
        else if (gapIndex > dragIndex && Index > dragIndex && Index < gapIndex)
        {
            extent = reverse ? gapExtent : -gapExtent;
        }

        Vector newTarget = _listState.ScrollAxis == Axis.Vertical ? new Vector(0, extent) : new Vector(extent, 0);
        if (newTarget == _targetOffset)
        {
            return;
        }

        Vector previousTarget = _targetOffset;
        _targetOffset = newTarget;
        if (animate)
        {
            if (_offsetAnimation is null)
            {
                _offsetAnimation = new AnimationController(duration: TimeSpan.FromMilliseconds(250), vsync: _listState);
                _offsetAnimation.AddListener(Rebuild);
                _offsetAnimation.AddStatusListener(HandleOffsetStatus);
                _offsetAnimation.Forward();
            }
            else
            {
                double t = Curves.EaseInOut.Transform(_offsetAnimation.Value);
                _startOffset += (previousTarget - _startOffset) * t;
                _offsetAnimation.Forward(from: 0.0);
            }
        }
        else
        {
            DisposeOffsetAnimation();
            _startOffset = _targetOffset;
        }

        Rebuild();
    }

    internal void ResetGap()
    {
        DisposeOffsetAnimation();
        _startOffset = default;
        _targetOffset = default;
        Rebuild();
    }

    internal Rect TargetGeometry()
    {
        var box = (RenderBox)Context.FindRenderObject()!;
        return new Rect(box.LocalToGlobal(default) + _targetOffset, box.Size);
    }

    internal bool TryGetTargetGeometry(out Rect geometry)
    {
        if (!Mounted || Context.FindRenderObject() is not RenderBox { HasSize: true })
        {
            geometry = default;
            return false;
        }

        geometry = TargetGeometry();
        return true;
    }

    private void Rebuild()
    {
        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    private void HandleOffsetStatus(AnimationStatus status)
    {
        if (status == AnimationStatus.Completed)
        {
            _startOffset = _targetOffset;
            DisposeOffsetAnimation();
        }
    }

    private void DisposeOffsetAnimation()
    {
        _offsetAnimation?.RemoveListener(Rebuild);
        _offsetAnimation?.RemoveStatusListener(HandleOffsetStatus);
        _offsetAnimation?.Dispose();
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

    protected virtual MultiDragGestureRecognizer CreateRecognizer() => Delayed
        ? new DelayedMultiDragGestureRecognizer(debugOwner: this)
        : new ImmediateMultiDragGestureRecognizer(debugOwner: this);

    public override Widget Build(BuildContext context)
    {
        return new Listener(
            onPointerDown: Enabled ? @event => StartDragging(context, @event) : null,
            child: Child);
    }

    private void StartDragging(BuildContext context, PointerDownEvent @event)
    {
        DeviceGestureSettings? settings = MediaQuery.MaybeGestureSettingsOf(context);
        SliverReorderableListState? list = SliverReorderableList.MaybeOf(context);
        if (list is not null)
        {
            MultiDragGestureRecognizer recognizer = CreateRecognizer();
            recognizer.GestureSettings = settings;
            list.StartItemDragReorder(Index, @event, recognizer);
        }
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

    protected override MultiDragGestureRecognizer CreateRecognizer() =>
        new DelayedMultiDragGestureRecognizer(debugOwner: this);
}

#pragma warning restore CS0618
