using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/reorderable_list.dart

#pragma warning disable CS0618

/// <summary>A Material scrolling list whose keyed items can be reordered by dragging.</summary>
public sealed class ReorderableListView : StatefulWidget
{
    public ReorderableListView(
        IReadOnlyList<Widget> children,
        ReorderCallback? onReorder = null,
        ReorderCallback? onReorderItem = null,
        Action<int>? onReorderStart = null,
        Action<int>? onReorderEnd = null,
        double? itemExtent = null,
        ItemExtentBuilder? itemExtentBuilder = null,
        Widget? prototypeItem = null,
        ReorderItemProxyDecorator? proxyDecorator = null,
        bool buildDefaultDragHandles = true,
        Thickness? padding = null,
        Widget? header = null,
        Widget? footer = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? scrollController = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        double cacheExtent = 250.0,
        double? autoScrollerVelocityScalar = null,
        MouseCursor? mouseCursor = null,
        Key? key = null) : this(
        itemBuilder: (_, index) => children[index],
        itemCount: children?.Count ?? throw new ArgumentNullException(nameof(children)),
        onReorder: onReorder,
        onReorderItem: onReorderItem,
        onReorderStart: onReorderStart,
        onReorderEnd: onReorderEnd,
        itemExtent: itemExtent,
        itemExtentBuilder: itemExtentBuilder,
        prototypeItem: prototypeItem,
        proxyDecorator: proxyDecorator,
        buildDefaultDragHandles: buildDefaultDragHandles,
        padding: padding,
        header: header,
        footer: footer,
        scrollDirection: scrollDirection,
        reverse: reverse,
        scrollController: scrollController,
        primary: primary,
        physics: physics,
        shrinkWrap: shrinkWrap,
        cacheExtent: cacheExtent,
        autoScrollerVelocityScalar: autoScrollerVelocityScalar,
        mouseCursor: mouseCursor,
        key: key)
    {
        if (children.Any(child => child.Key is null))
        {
            throw new ArgumentException("All ReorderableListView children must have a key.", nameof(children));
        }
    }

    private ReorderableListView(
        IndexedWidgetBuilder itemBuilder,
        int itemCount,
        ReorderCallback? onReorder,
        ReorderCallback? onReorderItem,
        Action<int>? onReorderStart,
        Action<int>? onReorderEnd,
        double? itemExtent,
        ItemExtentBuilder? itemExtentBuilder,
        Widget? prototypeItem,
        ReorderItemProxyDecorator? proxyDecorator,
        bool buildDefaultDragHandles,
        Thickness? padding,
        Widget? header,
        Widget? footer,
        Axis scrollDirection,
        bool reverse,
        ScrollController? scrollController,
        bool? primary,
        ScrollPhysics? physics,
        bool shrinkWrap,
        double cacheExtent,
        double? autoScrollerVelocityScalar,
        MouseCursor? mouseCursor,
        Key? key) : base(key)
    {
        ReorderableList.ValidateArguments(
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
        BuildDefaultDragHandles = buildDefaultDragHandles;
        Padding = padding ?? default;
        Header = header;
        Footer = footer;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        ScrollController = scrollController;
        Primary = primary;
        Physics = physics;
        ShrinkWrap = shrinkWrap;
        CacheExtent = cacheExtent;
        AutoScrollerVelocityScalar = autoScrollerVelocityScalar ?? 50.0;
        MouseCursor = mouseCursor;
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

    public bool BuildDefaultDragHandles { get; }

    public Thickness Padding { get; }

    public Widget? Header { get; }

    public Widget? Footer { get; }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? ScrollController { get; }

    public bool? Primary { get; }

    public ScrollPhysics? Physics { get; }

    public bool ShrinkWrap { get; }

    public double CacheExtent { get; }

    public double AutoScrollerVelocityScalar { get; }

    public MouseCursor? MouseCursor { get; }

    public static ReorderableListView Builder(
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
        bool buildDefaultDragHandles = true,
        Thickness? padding = null,
        Widget? header = null,
        Widget? footer = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? scrollController = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        double cacheExtent = 250.0,
        double? autoScrollerVelocityScalar = null,
        MouseCursor? mouseCursor = null,
        Key? key = null)
    {
        return new ReorderableListView(
            itemBuilder,
            itemCount,
            onReorder,
            onReorderItem,
            onReorderStart,
            onReorderEnd,
            itemExtent,
            itemExtentBuilder,
            prototypeItem,
            proxyDecorator,
            buildDefaultDragHandles,
            padding,
            header,
            footer,
            scrollDirection,
            reverse,
            scrollController,
            primary,
            physics,
            shrinkWrap,
            cacheExtent,
            autoScrollerVelocityScalar,
            mouseCursor,
            key);
    }

    public override State CreateState() => new ReorderableListViewState();

    private sealed class ReorderableListViewState : State
    {
        private bool _dragging;

        private ReorderableListView CurrentWidget => (ReorderableListView)StateWidget;

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            bool usePrimary = widget.Primary ?? (widget.ScrollDirection == Axis.Vertical
                                                  && widget.ScrollController is null);
            ScrollController? effectiveController = widget.ScrollController;
            if (effectiveController is null && usePrimary)
            {
                effectiveController = PrimaryScrollController.MaybeOf(context);
            }

            (Thickness headerPadding, Thickness listPadding, Thickness footerPadding) = ResolvePadding(widget);
            var slivers = new List<Widget>();
            if (widget.Header is not null)
            {
                slivers.Add(new SliverPadding(
                    headerPadding,
                    new SliverToBoxAdapter(widget.Header)));
            }

            slivers.Add(new SliverPadding(
                listPadding,
                new SliverReorderableList(
                    itemBuilder: BuildItem,
                    itemCount: widget.ItemCount,
                    onReorder: widget.OnReorder,
                    onReorderItem: widget.OnReorderItem,
                    onReorderStart: HandleReorderStart,
                    onReorderEnd: HandleReorderEnd,
                    itemExtent: widget.ItemExtent,
                    itemExtentBuilder: widget.ItemExtentBuilder,
                    prototypeItem: widget.PrototypeItem,
                    proxyDecorator: widget.ProxyDecorator ?? DefaultProxyDecorator,
                    autoScrollerVelocityScalar: widget.AutoScrollerVelocityScalar,
                    scrollController: effectiveController)));

            if (widget.Footer is not null)
            {
                slivers.Add(new SliverPadding(
                    footerPadding,
                    new SliverToBoxAdapter(widget.Footer)));
            }

            return new CustomScrollView(
                slivers: slivers,
                scrollDirection: widget.ScrollDirection,
                reverse: widget.Reverse,
                controller: effectiveController,
                primary: widget.Primary,
                physics: widget.Physics,
                cacheExtent: widget.CacheExtent,
                shrinkWrap: widget.ShrinkWrap);
        }

        private Widget BuildItem(BuildContext context, int index)
        {
            Widget item = CurrentWidget.ItemBuilder(context, index);
            if (item.Key is null)
            {
                throw new InvalidOperationException("Every ReorderableListView item must have a key.");
            }

            Key itemGlobalKey = new ReorderableListViewChildKey(item.Key, this);
            if (!CurrentWidget.BuildDefaultDragHandles)
            {
                return new KeyedSubtree(item, itemGlobalKey);
            }

            TargetPlatform platform = Theme.Of(context).Platform;
            if (platform is TargetPlatform.Android or TargetPlatform.Fuchsia or TargetPlatform.IOS)
            {
                return new ReorderableDelayedDragStartListener(
                    child: item,
                    index: index,
                    key: itemGlobalKey);
            }

            MouseCursor effectiveCursor = CurrentWidget.MouseCursor
                                          ?? (_dragging
                                              ? SystemMouseCursors.Grabbing
                                              : SystemMouseCursors.Grab);
            Widget dragHandle = new MouseRegion(
                cursor: effectiveCursor,
                child: new Icon(Icons.DragHandle));
            var listener = new ReorderableDragStartListener(dragHandle, index);
            TextDirection direction = Directionality.Of(context);

            Widget positionedHandle = CurrentWidget.ScrollDirection == Axis.Horizontal
                ? Positioned.Directional(
                    textDirection: direction,
                    start: 0,
                    end: 0,
                    bottom: 8,
                    child: new Align(listener, AlignmentDirectional.BottomCenter))
                : Positioned.Directional(
                    textDirection: direction,
                    top: 0,
                    bottom: 0,
                    end: 8,
                    child: new Align(listener, AlignmentDirectional.CenterEnd));

            return new Stack(
                key: itemGlobalKey,
                children: [item, positionedHandle]);
        }

        private Widget DefaultProxyDecorator(Widget child, int index, Animation<double> animation)
        {
            double elevation = 6.0 * Curves.EaseInOut(animation.Value);
            return new Material(elevation: elevation, child: child);
        }

        private void HandleReorderStart(int index)
        {
            SetState(() => _dragging = true);
            CurrentWidget.OnReorderStart?.Invoke(index);
        }

        private void HandleReorderEnd(int index)
        {
            SetState(() => _dragging = false);
            CurrentWidget.OnReorderEnd?.Invoke(index);
        }

        private static (Thickness Header, Thickness List, Thickness Footer) ResolvePadding(
            ReorderableListView widget)
        {
            Thickness padding = widget.Padding;
            double? start = widget.Header is null ? null : 0.0;
            double? end = widget.Footer is null ? null : 0.0;
            if (widget.Reverse)
            {
                (start, end) = (end, start);
            }

            Thickness startPadding;
            Thickness endPadding;
            Thickness listPadding;
            if (start is null && end is null)
            {
                startPadding = default;
                endPadding = default;
                listPadding = padding;
            }
            else if (widget.ScrollDirection == Axis.Horizontal)
            {
                startPadding = new Thickness(padding.Left, padding.Top, 0, padding.Bottom);
                endPadding = new Thickness(0, padding.Top, padding.Right, padding.Bottom);
                listPadding = new Thickness(start ?? padding.Left, padding.Top, end ?? padding.Right, padding.Bottom);
            }
            else
            {
                startPadding = new Thickness(padding.Left, padding.Top, padding.Right, 0);
                endPadding = new Thickness(padding.Left, 0, padding.Right, padding.Bottom);
                listPadding = new Thickness(padding.Left, start ?? padding.Top, padding.Right, end ?? padding.Bottom);
            }

            return widget.Reverse
                ? (startPadding, listPadding, endPadding)
                : (endPadding, listPadding, startPadding);
        }
    }
}

internal sealed record ReorderableListViewChildKey(Key SubKey, State Owner) : GlobalKey;

#pragma warning restore CS0618
