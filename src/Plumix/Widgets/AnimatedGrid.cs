using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/animated_scroll_view.dart

/// <summary>A scrolling grid that animates items as they are inserted and removed.</summary>
public sealed class AnimatedGrid : StatefulWidget
{
    public AnimatedGrid(
        AnimatedItemBuilder itemBuilder,
        SliverGridDelegate gridDelegate,
        int initialItemCount = 0,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        Thickness? padding = null,
        Clip clipBehavior = Clip.HardEdge,
        ScrollCacheExtent? scrollCacheExtent = null,
        Key? key = null) : base(key)
    {
        if (initialItemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialItemCount));
        }

        ItemBuilder = itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder));
        GridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
        InitialItemCount = initialItemCount;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        Physics = physics;
        Padding = padding;
        ClipBehavior = clipBehavior;
        ScrollCacheExtent = scrollCacheExtent;
    }

    public AnimatedItemBuilder ItemBuilder { get; }

    public SliverGridDelegate GridDelegate { get; }

    public int InitialItemCount { get; }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public bool? Primary { get; }

    public ScrollPhysics? Physics { get; }

    public Thickness? Padding { get; }

    public Clip ClipBehavior { get; }

    public ScrollCacheExtent? ScrollCacheExtent { get; }

    public static AnimatedGridState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "AnimatedGrid.Of() called with a context that does not contain an AnimatedGrid.");
    }

    public static AnimatedGridState? MaybeOf(BuildContext context)
    {
        return context.FindAncestorStateOfType<AnimatedGridState>();
    }

    public override State CreateState() => new AnimatedGridState();
}

public sealed class AnimatedGridState : State
{
    private readonly LabeledGlobalKey<SliverAnimatedGridState> _sliverKey = new("AnimatedGrid");

    private AnimatedGrid CurrentWidget => (AnimatedGrid)StateWidget;

    private SliverAnimatedGridState SliverState => _sliverKey.CurrentState
        ?? throw new InvalidOperationException("AnimatedGrid is not mounted.");

    public void InsertItem(int index, TimeSpan? duration = null)
    {
        SliverState.InsertItem(index, duration);
    }

    public void InsertAllItems(
        int index,
        int length,
        TimeSpan? duration = null,
        bool isAsync = false)
    {
        SliverState.InsertAllItems(index, length, duration);
    }

    public void RemoveItem(
        int index,
        AnimatedRemovedItemBuilder builder,
        TimeSpan? duration = null)
    {
        SliverState.RemoveItem(index, builder, duration);
    }

    public void RemoveAllItems(AnimatedRemovedItemBuilder builder, TimeSpan? duration = null)
    {
        SliverState.RemoveAllItems(builder, duration);
    }

    public override Widget Build(BuildContext context)
    {
        AnimatedGrid widget = CurrentWidget;
        Widget sliver = new SliverAnimatedGrid(
            itemBuilder: widget.ItemBuilder,
            gridDelegate: widget.GridDelegate,
            initialItemCount: widget.InitialItemCount,
            key: _sliverKey);

        Thickness? effectivePadding = widget.Padding;
        if (!effectivePadding.HasValue && MediaQuery.MaybeOf(context) is { } mediaQuery)
        {
            Thickness horizontalPadding = new(mediaQuery.Padding.Left, 0, mediaQuery.Padding.Right, 0);
            Thickness verticalPadding = new(0, mediaQuery.Padding.Top, 0, mediaQuery.Padding.Bottom);
            effectivePadding = widget.ScrollDirection == Axis.Vertical ? verticalPadding : horizontalPadding;
            sliver = new MediaQuery(
                mediaQuery.CopyWith(
                    padding: widget.ScrollDirection == Axis.Vertical ? horizontalPadding : verticalPadding),
                sliver);
        }

        if (effectivePadding.HasValue)
        {
            sliver = new SliverPadding(effectivePadding.Value, sliver);
        }

        return new CustomScrollView(
            slivers: [sliver],
            scrollDirection: widget.ScrollDirection,
            reverse: widget.Reverse,
            controller: widget.Controller,
            primary: widget.Primary,
            physics: widget.Physics,
            cacheExtent: widget.ScrollCacheExtent?.Value ?? 250.0,
            cacheExtentStyle: widget.ScrollCacheExtent?.Style ?? CacheExtentStyle.Pixel,
            clipBehavior: widget.ClipBehavior);
    }
}

/// <summary>A sliver grid that animates items as they are inserted and removed.</summary>
public sealed class SliverAnimatedGrid : StatefulWidget
{
    public SliverAnimatedGrid(
        AnimatedItemBuilder itemBuilder,
        SliverGridDelegate gridDelegate,
        ChildIndexGetter? findChildIndexCallback = null,
        int initialItemCount = 0,
        Key? key = null) : base(key)
    {
        if (initialItemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialItemCount));
        }

        ItemBuilder = itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder));
        GridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
        FindChildIndexCallback = findChildIndexCallback;
        InitialItemCount = initialItemCount;
    }

    public AnimatedItemBuilder ItemBuilder { get; }

    public SliverGridDelegate GridDelegate { get; }

    public ChildIndexGetter? FindChildIndexCallback { get; }

    public int InitialItemCount { get; }

    public static SliverAnimatedGridState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "SliverAnimatedGrid.Of() called with a context that does not contain a SliverAnimatedGrid.");
    }

    public static SliverAnimatedGridState? MaybeOf(BuildContext context)
    {
        return context.FindAncestorStateOfType<SliverAnimatedGridState>();
    }

    public override State CreateState() => new SliverAnimatedGridState();
}

public sealed class SliverAnimatedGridState : State
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(300);
    private static readonly Animation<double> AlwaysCompleteAnimation = new CompleteAnimation();

    private readonly List<ActiveItem> _incomingItems = [];
    private readonly List<ActiveItem> _outgoingItems = [];
    private int _itemsCount;

    private SliverAnimatedGrid CurrentWidget => (SliverAnimatedGrid)StateWidget;

    public int ItemsCount => _itemsCount;

    public override void InitState()
    {
        _itemsCount = CurrentWidget.InitialItemCount;
    }

    public void InsertItem(int index, TimeSpan? duration = null)
    {
        TimeSpan effectiveDuration = ResolveDuration(duration);
        int itemIndex = IndexToItemIndex(index);
        if (index < 0 || itemIndex < 0 || itemIndex > _itemsCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        foreach (ActiveItem item in _incomingItems)
        {
            if (item.ItemIndex >= itemIndex)
            {
                item.ItemIndex++;
            }
        }

        foreach (ActiveItem item in _outgoingItems)
        {
            if (item.ItemIndex >= itemIndex)
            {
                item.ItemIndex++;
            }
        }

        var controller = CreateController(effectiveDuration);
        ActiveItem incomingItem = ActiveItem.Incoming(controller, itemIndex);
        SetState(() =>
        {
            _incomingItems.Add(incomingItem);
            _incomingItems.Sort();
            _itemsCount++;
        });
        controller.Forward();
    }

    public void InsertAllItems(int index, int length, TimeSpan? duration = null)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        for (int offset = 0; offset < length; offset++)
        {
            InsertItem(index + offset, duration);
        }
    }

    public void RemoveItem(
        int index,
        AnimatedRemovedItemBuilder builder,
        TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        TimeSpan effectiveDuration = ResolveDuration(duration);
        int itemIndex = IndexToItemIndex(index);
        if (index < 0 || itemIndex < 0 || itemIndex >= _itemsCount
            || ActiveItemAt(_outgoingItems, itemIndex) is not null)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        ActiveItem? incomingItem = RemoveActiveItemAt(_incomingItems, itemIndex);
        AnimationController controller;
        if (incomingItem is not null)
        {
            controller = incomingItem.Controller;
        }
        else
        {
            controller = CreateController(effectiveDuration);
            controller.SetValue(1.0);
        }

        controller.Duration = effectiveDuration;
        ActiveItem outgoingItem = ActiveItem.Outgoing(controller, itemIndex, builder);
        SetState(() =>
        {
            _outgoingItems.Add(outgoingItem);
            _outgoingItems.Sort();
        });
        controller.Reverse();
    }

    public void RemoveAllItems(AnimatedRemovedItemBuilder builder, TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        int visibleItemCount = _itemsCount - _outgoingItems.Count;
        for (int index = visibleItemCount - 1; index >= 0; index--)
        {
            RemoveItem(index, builder, duration);
        }
    }

    public override Widget Build(BuildContext context)
    {
        return new SliverGrid(
            new SliverChildBuilderDelegate(
                builder: BuildItem,
                childCount: _itemsCount,
                findChildIndexCallback: CurrentWidget.FindChildIndexCallback is null
                    ? null
                    : key =>
                    {
                        int? index = CurrentWidget.FindChildIndexCallback(key);
                        return index.HasValue ? IndexToItemIndex(index.Value) : null;
                    }),
            CurrentWidget.GridDelegate);
    }

    public override void Dispose()
    {
        foreach (ActiveItem item in _incomingItems
                     .Concat(_outgoingItems)
                     .DistinctBy(static item => item.Controller))
        {
            DisposeController(item.Controller);
        }

        _incomingItems.Clear();
        _outgoingItems.Clear();
    }

    private Widget BuildItem(BuildContext context, int itemIndex)
    {
        ActiveItem? outgoingItem = ActiveItemAt(_outgoingItems, itemIndex);
        if (outgoingItem is not null)
        {
            return outgoingItem.RemovedItemBuilder!(context, outgoingItem.Controller);
        }

        ActiveItem? incomingItem = ActiveItemAt(_incomingItems, itemIndex);
        Animation<double> animation = incomingItem?.Controller ?? AlwaysCompleteAnimation;
        return CurrentWidget.ItemBuilder(context, ItemIndexToIndex(itemIndex), animation);
    }

    private AnimationController CreateController(TimeSpan duration)
    {
        var controller = new AnimationController(duration, this);
        controller.Changed += HandleAnimationChanged;
        controller.Completed += HandleAnimationCompleted;
        controller.Dismissed += HandleAnimationDismissed;
        return controller;
    }

    private void HandleAnimationChanged()
    {
        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    private void HandleAnimationCompleted()
    {
        ActiveItem? item = _incomingItems.FirstOrDefault(static candidate =>
            candidate.Controller.Status == AnimationStatus.Completed);
        if (item is null)
        {
            return;
        }

        _incomingItems.Remove(item);
        DisposeController(item.Controller);
    }

    private void HandleAnimationDismissed()
    {
        ActiveItem? outgoingItem = _outgoingItems.FirstOrDefault(static candidate =>
            candidate.Controller.Status == AnimationStatus.Dismissed);
        if (outgoingItem is null)
        {
            return;
        }

        _outgoingItems.Remove(outgoingItem);
        foreach (ActiveItem item in _incomingItems)
        {
            if (item.ItemIndex > outgoingItem.ItemIndex)
            {
                item.ItemIndex--;
            }
        }

        foreach (ActiveItem item in _outgoingItems)
        {
            if (item.ItemIndex > outgoingItem.ItemIndex)
            {
                item.ItemIndex--;
            }
        }

        DisposeController(outgoingItem.Controller);
        if (Mounted)
        {
            SetState(() => _itemsCount--);
        }
    }

    private void DisposeController(AnimationController controller)
    {
        controller.Changed -= HandleAnimationChanged;
        controller.Completed -= HandleAnimationCompleted;
        controller.Dismissed -= HandleAnimationDismissed;
        controller.Dispose();
    }

    private int IndexToItemIndex(int index)
    {
        int itemIndex = index;
        foreach (ActiveItem item in _outgoingItems)
        {
            if (item.ItemIndex <= itemIndex)
            {
                itemIndex++;
            }
            else
            {
                break;
            }
        }

        return itemIndex;
    }

    private int ItemIndexToIndex(int itemIndex)
    {
        int index = itemIndex;
        foreach (ActiveItem item in _outgoingItems)
        {
            if (item.ItemIndex < itemIndex)
            {
                index--;
            }
            else
            {
                break;
            }
        }

        return index;
    }

    private static ActiveItem? RemoveActiveItemAt(List<ActiveItem> items, int itemIndex)
    {
        ActiveItem? item = ActiveItemAt(items, itemIndex);
        if (item is not null)
        {
            items.Remove(item);
        }

        return item;
    }

    private static ActiveItem? ActiveItemAt(IReadOnlyList<ActiveItem> items, int itemIndex)
    {
        int low = 0;
        int high = items.Count - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            int comparison = items[middle].ItemIndex.CompareTo(itemIndex);
            if (comparison == 0)
            {
                return items[middle];
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }

    private static TimeSpan ResolveDuration(TimeSpan? duration)
    {
        TimeSpan value = duration ?? DefaultDuration;
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        return value;
    }

    private sealed class ActiveItem : IComparable<ActiveItem>
    {
        private ActiveItem(
            AnimationController controller,
            int itemIndex,
            AnimatedRemovedItemBuilder? removedItemBuilder)
        {
            Controller = controller;
            ItemIndex = itemIndex;
            RemovedItemBuilder = removedItemBuilder;
        }

        public AnimationController Controller { get; }

        public int ItemIndex { get; set; }

        public AnimatedRemovedItemBuilder? RemovedItemBuilder { get; }

        public static ActiveItem Incoming(AnimationController controller, int itemIndex)
        {
            return new ActiveItem(controller, itemIndex, removedItemBuilder: null);
        }

        public static ActiveItem Outgoing(
            AnimationController controller,
            int itemIndex,
            AnimatedRemovedItemBuilder removedItemBuilder)
        {
            return new ActiveItem(controller, itemIndex, removedItemBuilder);
        }

        public int CompareTo(ActiveItem? other)
        {
            return other is null ? 1 : ItemIndex.CompareTo(other.ItemIndex);
        }
    }

    private sealed class CompleteAnimation : Animation<double>
    {
        public override double Value => 1.0;

        public override AnimationStatus Status => AnimationStatus.Completed;

        public override void AddListener(Action listener)
        {
        }

        public override void RemoveListener(Action listener)
        {
        }

        public override void AddStatusListener(Action<AnimationStatus> listener)
        {
        }

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
        }
    }
}
