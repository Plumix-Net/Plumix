using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/animated_scroll_view.dart

public delegate Widget AnimatedItemBuilder(
    BuildContext context,
    int index,
    Animation<double> animation);

public delegate Widget AnimatedRemovedItemBuilder(
    BuildContext context,
    Animation<double> animation);

/// <summary>A scrolling list that animates items as they are inserted and removed.</summary>
public sealed class AnimatedList : StatefulWidget
{
    public AnimatedList(
        AnimatedItemBuilder itemBuilder,
        int initialItemCount = 0,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        Clip clipBehavior = Clip.HardEdge,
        ScrollCacheExtent? scrollCacheExtent = null,
        Key? key = null) : this(
            itemBuilder,
            removedSeparatorBuilder: null,
            initialItemCount,
            scrollDirection,
            reverse,
            controller,
            primary,
            physics,
            shrinkWrap,
            padding,
            clipBehavior,
            scrollCacheExtent,
            key)
    {
    }

    private AnimatedList(
        AnimatedItemBuilder itemBuilder,
        AnimatedItemBuilder? removedSeparatorBuilder,
        int initialItemCount,
        Axis scrollDirection,
        bool reverse,
        ScrollController? controller,
        bool? primary,
        ScrollPhysics? physics,
        bool shrinkWrap,
        Thickness? padding,
        Clip clipBehavior,
        ScrollCacheExtent? scrollCacheExtent,
        Key? key) : base(key)
    {
        if (initialItemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialItemCount));
        }

        ItemBuilder = itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder));
        RemovedSeparatorBuilder = removedSeparatorBuilder;
        InitialItemCount = initialItemCount;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        Physics = physics;
        ShrinkWrap = shrinkWrap;
        Padding = padding;
        ClipBehavior = clipBehavior;
        ScrollCacheExtent = scrollCacheExtent;
    }

    public AnimatedItemBuilder ItemBuilder { get; }

    public int InitialItemCount { get; }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public bool? Primary { get; }

    public ScrollPhysics? Physics { get; }

    public bool ShrinkWrap { get; }

    public Thickness? Padding { get; }

    public Clip ClipBehavior { get; }

    public ScrollCacheExtent? ScrollCacheExtent { get; }

    internal AnimatedItemBuilder? RemovedSeparatorBuilder { get; }

    public static AnimatedList Separated(
        AnimatedItemBuilder itemBuilder,
        AnimatedItemBuilder separatorBuilder,
        AnimatedItemBuilder removedSeparatorBuilder,
        int initialItemCount = 0,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        Clip clipBehavior = Clip.HardEdge,
        ScrollCacheExtent? scrollCacheExtent = null,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(itemBuilder);
        ArgumentNullException.ThrowIfNull(separatorBuilder);
        ArgumentNullException.ThrowIfNull(removedSeparatorBuilder);
        if (initialItemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialItemCount));
        }

        int childCount = ComputeChildCountWithSeparators(initialItemCount);
        return new AnimatedList(
            itemBuilder: (context, index, animation) => index % 2 == 0
                ? itemBuilder(context, index / 2, animation)
                : separatorBuilder(context, index / 2, animation),
            removedSeparatorBuilder: removedSeparatorBuilder,
            initialItemCount: childCount,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            clipBehavior: clipBehavior,
            scrollCacheExtent: scrollCacheExtent,
            key: key);
    }

    public static AnimatedListState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "AnimatedList.Of() called with a context that does not contain an AnimatedList.");
    }

    public static AnimatedListState? MaybeOf(BuildContext context)
    {
        return context.FindAncestorStateOfType<AnimatedListState>();
    }

    public override State CreateState() => new AnimatedListState();

    private static int ComputeChildCountWithSeparators(int itemCount)
    {
        return itemCount == 0 ? 0 : checked((itemCount * 2) - 1);
    }
}

public sealed class AnimatedListState : State
{
    private readonly LabeledGlobalKey<SliverAnimatedListState> _sliverKey = new("AnimatedList");

    private AnimatedList CurrentWidget => (AnimatedList)StateWidget;

    private SliverAnimatedListState SliverState => _sliverKey.CurrentState
        ?? throw new InvalidOperationException("AnimatedList is not mounted.");

    public void InsertItem(int index, TimeSpan? duration = null)
    {
        var widget = CurrentWidget;
        if (widget.RemovedSeparatorBuilder is null)
        {
            SliverState.InsertItem(index, duration);
            return;
        }

        int itemIndex = ComputeItemIndex(index);
        SliverState.InsertItem(itemIndex, duration);
        if (ItemsCount > 1)
        {
            SliverState.InsertItem(itemIndex, duration);
        }
    }

    public void InsertAllItems(
        int index,
        int length,
        TimeSpan? duration = null,
        bool isAsync = false)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var widget = CurrentWidget;
        if (widget.RemovedSeparatorBuilder is null)
        {
            SliverState.InsertAllItems(index, length, duration);
            return;
        }

        int itemIndex = ComputeItemIndex(index);
        int lengthWithSeparators = ItemsCount == 0 ? Math.Max(0, (length * 2) - 1) : length * 2;
        SliverState.InsertAllItems(itemIndex, lengthWithSeparators, duration);
    }

    public void RemoveItem(
        int index,
        AnimatedRemovedItemBuilder builder,
        TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        AnimatedItemBuilder? removedSeparatorBuilder = CurrentWidget.RemovedSeparatorBuilder;
        if (removedSeparatorBuilder is null)
        {
            SliverState.RemoveItem(index, builder, duration);
            return;
        }

        int itemIndex = ComputeItemIndex(index);
        SliverState.RemoveItem(itemIndex, builder, duration);
        if (ItemsCount <= 1)
        {
            return;
        }

        if (itemIndex == ItemsCount - 1)
        {
            SliverState.RemoveItem(
                itemIndex - 1,
                ToRemovedItemBuilder(removedSeparatorBuilder, index - 1),
                duration);
        }
        else
        {
            SliverState.RemoveItem(
                itemIndex,
                ToRemovedItemBuilder(removedSeparatorBuilder, index),
                duration);
        }
    }

    public void RemoveAllItems(AnimatedRemovedItemBuilder builder, TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        AnimatedItemBuilder? removedSeparatorBuilder = CurrentWidget.RemovedSeparatorBuilder;
        if (removedSeparatorBuilder is null)
        {
            SliverState.RemoveAllItems(builder, duration);
            return;
        }

        for (int index = ItemsCount - 1; index >= 0; index--)
        {
            SliverState.RemoveItem(
                index,
                index % 2 == 0
                    ? builder
                    : ToRemovedItemBuilder(removedSeparatorBuilder, index / 2),
                duration);
        }
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        Widget sliver = new SliverAnimatedList(
            itemBuilder: widget.ItemBuilder,
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
            shrinkWrap: widget.ShrinkWrap,
            clipBehavior: widget.ClipBehavior);
    }

    private int ItemsCount => SliverState.ItemsCount;

    private int ComputeItemIndex(int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index == 0)
        {
            return 0;
        }

        int itemsAndSeparatorsCount = ItemsCount;
        int separatorsCount = itemsAndSeparatorsCount / 2;
        int separatedItemsCount = itemsAndSeparatorsCount - separatorsCount;
        int indexAdjustedForSeparators = checked(index * 2);
        return index == separatedItemsCount
            ? indexAdjustedForSeparators - 1
            : indexAdjustedForSeparators;
    }

    private static AnimatedRemovedItemBuilder ToRemovedItemBuilder(
        AnimatedItemBuilder builder,
        int index)
    {
        return (context, animation) => builder(context, index, animation);
    }
}

/// <summary>A sliver list that animates items as they are inserted and removed.</summary>
public sealed class SliverAnimatedList : StatefulWidget
{
    public SliverAnimatedList(
        AnimatedItemBuilder itemBuilder,
        ChildIndexGetter? findChildIndexCallback = null,
        int initialItemCount = 0,
        Key? key = null) : base(key)
    {
        if (initialItemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialItemCount));
        }

        ItemBuilder = itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder));
        FindChildIndexCallback = findChildIndexCallback;
        InitialItemCount = initialItemCount;
    }

    public AnimatedItemBuilder ItemBuilder { get; }

    public ChildIndexGetter? FindChildIndexCallback { get; }

    public int InitialItemCount { get; }

    public static SliverAnimatedListState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "SliverAnimatedList.Of() called with a context that does not contain a SliverAnimatedList.");
    }

    public static SliverAnimatedListState? MaybeOf(BuildContext context)
    {
        return context.FindAncestorStateOfType<SliverAnimatedListState>();
    }

    public override State CreateState() => new SliverAnimatedListState();
}

public sealed class SliverAnimatedListState : State
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(300);
    private static readonly Animation<double> AlwaysCompleteAnimation = new CompleteAnimation();

    private readonly List<ActiveItem> _incomingItems = [];
    private readonly List<ActiveItem> _outgoingItems = [];
    private int _itemsCount;

    private SliverAnimatedList CurrentWidget => (SliverAnimatedList)StateWidget;

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

        foreach (var item in _incomingItems)
        {
            if (item.ItemIndex >= itemIndex)
            {
                item.ItemIndex++;
            }
        }

        foreach (var item in _outgoingItems)
        {
            if (item.ItemIndex >= itemIndex)
            {
                item.ItemIndex++;
            }
        }

        var controller = CreateController(effectiveDuration);
        var incomingItem = ActiveItem.Incoming(controller, itemIndex);
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
        var outgoingItem = ActiveItem.Outgoing(controller, itemIndex, builder);
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
        return new SliverList(new SliverChildBuilderDelegate(
            builder: BuildItem,
            childCount: _itemsCount,
            findChildIndexCallback: CurrentWidget.FindChildIndexCallback is null
                ? null
                : key =>
                {
                    int? index = CurrentWidget.FindChildIndexCallback(key);
                    return index.HasValue ? IndexToItemIndex(index.Value) : null;
                }));
    }

    public override void Dispose()
    {
        foreach (var item in _incomingItems.Concat(_outgoingItems).DistinctBy(static item => item.Controller))
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
        foreach (var item in _incomingItems)
        {
            if (item.ItemIndex > outgoingItem.ItemIndex)
            {
                item.ItemIndex--;
            }
        }

        foreach (var item in _outgoingItems)
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
        foreach (var item in _outgoingItems)
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
        foreach (var item in _outgoingItems)
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
