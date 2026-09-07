using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_view.dart

namespace Plumix.Widgets;

/// <summary>
/// A widget that combines a <see cref="Scrollable"/> and a <see cref="Viewport"/> to create an
/// interactive scrolling pane of content in one dimension.
/// </summary>
/// <remarks>
/// Subclasses supply the slivers through <see cref="BuildSlivers"/>. See <see cref="CustomScrollView"/>
/// for the general-purpose form, and <see cref="ListView"/>/<see cref="GridView"/> for the
/// single-sliver forms.
/// </remarks>
public abstract class ScrollView : StatelessWidget
{
    protected ScrollView(
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        bool shrinkWrap = false,
        Key? center = null,
        double anchor = 0.0,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        int? semanticChildCount = null,
        SliverPaintOrder paintOrder = SliverPaintOrder.FirstIsTop,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null) : base(key)
    {
        if (controller is not null && (primary ?? false))
        {
            throw new ArgumentException(
                "Primary ScrollViews obtain their ScrollController via inheritance from a "
                + "PrimaryScrollController widget. You cannot both set primary to true and pass an "
                + "explicit controller.");
        }

        if (shrinkWrap && center is not null)
        {
            throw new ArgumentException("A shrink-wrapping scroll view cannot have a center sliver.");
        }

        if (!(anchor >= 0.0 && anchor <= 1.0))
        {
            throw new ArgumentOutOfRangeException(nameof(anchor));
        }

        if (semanticChildCount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(semanticChildCount));
        }

        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        // A vertical scroll view that will inherit the primary controller is always scrollable, so
        // that the controller has something to attach to even when the content fits.
        Physics = physics
                  ?? ((primary ?? false)
                      || (primary is null && controller is null && scrollDirection == Axis.Vertical)
                      ? new AlwaysScrollableScrollPhysics()
                      : null);
        ScrollBehavior = scrollBehavior;
        ShrinkWrap = shrinkWrap;
        Center = center;
        Anchor = anchor;
        CacheExtent = cacheExtent;
        ScrollCacheExtent = scrollCacheExtent;
        SemanticChildCount = semanticChildCount;
        PaintOrder = paintOrder;
        DragStartBehavior = dragStartBehavior;
        KeyboardDismissBehavior = keyboardDismissBehavior;
        RestorationId = restorationId;
        ClipBehavior = clipBehavior;
        HitTestBehavior = hitTestBehavior;
    }

    /// <summary>The axis along which the scroll view scrolls.</summary>
    public Axis ScrollDirection { get; }

    /// <summary>Whether the scroll view scrolls in the reading direction.</summary>
    public bool Reverse { get; }

    /// <summary>An object that can be used to control the position to which this view is scrolled.</summary>
    public ScrollController? Controller { get; }

    /// <summary>
    /// Whether this is the primary scroll view associated with the parent
    /// <see cref="PrimaryScrollController"/>.
    /// </summary>
    public bool? Primary { get; }

    /// <summary>How the scroll view should respond to user input.</summary>
    public ScrollPhysics? Physics { get; }

    /// <summary>A <see cref="ScrollBehavior"/> applied to this widget individually.</summary>
    public ScrollBehavior? ScrollBehavior { get; }

    /// <summary>Whether the extent of the scroll view should be determined by the contents.</summary>
    public bool ShrinkWrap { get; }

    /// <summary>
    /// The key of the first sliver laid out in the forward growth direction; every sliver before it
    /// grows in the reverse direction and occupies negative scroll offsets.
    /// </summary>
    public Key? Center { get; }

    /// <summary>The relative position of the zero scroll offset within the viewport.</summary>
    public double Anchor { get; }

    /// <summary>Obsolete: use <see cref="ScrollCacheExtent"/> instead.</summary>
    /// <remarks>
    /// Deprecated in Flutter after v3.41.0-0.0.pre; kept because <see cref="ScrollCacheExtent"/> can
    /// still be built from it.
    /// </remarks>
    [Obsolete("Use ScrollCacheExtent instead.")]
    public double? CacheExtent { get; }

    /// <summary>The viewport's cache extent, in pixels or in viewport fractions.</summary>
    public ScrollCacheExtent? ScrollCacheExtent { get; }

    /// <summary>
    /// The number of children an assistive technology should be told this view can show, or
    /// <c>null</c> when the count is unknown or unbounded.
    /// </summary>
    public int? SemanticChildCount { get; }

    /// <summary>The order in which the viewport paints its slivers.</summary>
    public SliverPaintOrder PaintOrder { get; }

    /// <summary>Determines the way that drag start behavior is handled.</summary>
    public DragStartBehavior DragStartBehavior { get; }

    /// <summary>
    /// Whether the on-screen keyboard is dismissed when the view starts to be dragged, or
    /// <c>null</c> to defer to the ambient <see cref="ScrollBehavior"/>.
    /// </summary>
    public ScrollViewKeyboardDismissBehavior? KeyboardDismissBehavior { get; }

    /// <summary>Restoration ID to save and restore the scroll offset of the scrollable.</summary>
    public string? RestorationId { get; }

    /// <summary>The content will be clipped (or not) according to this option.</summary>
    public Clip ClipBehavior { get; }

    /// <summary>How the scroll view should behave during hit testing.</summary>
    public HitTestBehavior HitTestBehavior { get; }

    /// <summary>The axis direction this view scrolls in.</summary>
    /// <remarks>Flutter's <c>ScrollView.getDirection</c>.</remarks>
    public AxisDirection GetDirection(BuildContext context)
    {
        return ScrollDirectionUtils.GetAxisDirectionFromAxisReverseAndDirectionality(
            context,
            ScrollDirection,
            Reverse);
    }

    /// <summary>Builds the slivers this view scrolls.</summary>
    public abstract IReadOnlyList<Widget> BuildSlivers(BuildContext context);

    /// <summary>
    /// Builds the viewport that holds <paramref name="slivers"/>. Subclasses override this to supply
    /// their own viewport render object.
    /// </summary>
    /// <remarks>Flutter's <c>ScrollView.buildViewport</c>.</remarks>
    public virtual Widget BuildViewport(
        BuildContext context,
        ViewportOffset offset,
        AxisDirection axisDirection,
        IReadOnlyList<Widget> slivers)
    {
#pragma warning disable CS0618 // The deprecated cacheExtent still feeds the modern ScrollCacheExtent.
        ScrollCacheExtent? effectiveScrollCacheExtent = ScrollCacheExtent
                                                        ?? (CacheExtent is { } pixels
                                                            ? Rendering.ScrollCacheExtent.Pixels(pixels)
                                                            : null);
#pragma warning restore CS0618
        if (ShrinkWrap)
        {
            return new ShrinkWrappingViewport(
                axisDirection: axisDirection,
                offset: offset,
                slivers: slivers,
                paintOrder: PaintOrder,
                clipBehavior: ClipBehavior,
                scrollCacheExtent: effectiveScrollCacheExtent);
        }

        return new Viewport(
            axisDirection: axisDirection,
            offset: offset,
            slivers: slivers,
            scrollCacheExtent: effectiveScrollCacheExtent,
            center: Center,
            anchor: Anchor,
            paintOrder: PaintOrder,
            clipBehavior: ClipBehavior);
    }

    public override Widget Build(BuildContext context)
    {
        IReadOnlyList<Widget> slivers = BuildSlivers(context);
        AxisDirection axisDirection = GetDirection(context);

        bool effectivePrimary = Primary
                                ?? (Controller is null
                                    && PrimaryScrollController.ShouldInherit(context, ScrollDirection));

        ScrollController? scrollController = effectivePrimary
            ? PrimaryScrollController.MaybeOf(context)
            : Controller;

        var scrollable = new Scrollable(
            dragStartBehavior: DragStartBehavior,
            axisDirection: axisDirection,
            controller: scrollController,
            physics: Physics,
            scrollBehavior: ScrollBehavior,
            semanticChildCount: SemanticChildCount,
            restorationId: RestorationId,
            hitTestBehavior: HitTestBehavior,
            viewportBuilder: (viewportContext, offset) =>
                BuildViewport(viewportContext, offset, axisDirection, slivers),
            clipBehavior: ClipBehavior);

        // Further descendant scroll views must not inherit the same PrimaryScrollController.
        Widget scrollableResult = effectivePrimary && scrollController is not null
            ? PrimaryScrollController.None(scrollable)
            : scrollable;

        ScrollViewKeyboardDismissBehavior effectiveKeyboardDismissBehavior =
            KeyboardDismissBehavior
            ?? ScrollBehavior?.GetKeyboardDismissBehavior(context)
            ?? ScrollConfiguration.Of(context).GetKeyboardDismissBehavior(context);

        if (effectiveKeyboardDismissBehavior != ScrollViewKeyboardDismissBehavior.OnDrag)
        {
            return scrollableResult;
        }

        return new NotificationListener<ScrollUpdateNotification>(
            onNotification: notification =>
            {
                FocusScopeNode currentScope = FocusScope.Of(context);
                if (notification.DragDetails is not null
                    && !currentScope.HasPrimaryFocus
                    && currentScope.HasFocus)
                {
                    FocusManager.Instance.PrimaryFocus?.Unfocus();
                }

                return false;
            },
            child: scrollableResult);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<Axis>("scrollDirection", ScrollDirection));
        properties.Add(new FlagProperty("reverse", value: Reverse, ifTrue: "reversed", showName: true));
        properties.Add(new DiagnosticsProperty<ScrollController>(
            "controller",
            Controller,
            showName: false,
            defaultValue: null));
        properties.Add(new FlagProperty(
            "primary",
            value: Primary,
            ifTrue: "using primary controller",
            showName: true));
        properties.Add(new DiagnosticsProperty<ScrollPhysics>(
            "physics",
            Physics,
            showName: false,
            defaultValue: null));
        properties.Add(new FlagProperty(
            "shrinkWrap",
            value: ShrinkWrap,
            ifTrue: "shrink-wrapping",
            showName: true));
        properties.Add(new DiagnosticsProperty<ScrollCacheExtent>(
            "scrollCacheExtent",
            ScrollCacheExtent,
            defaultValue: null));
    }
}

/// <summary>
/// A <see cref="ScrollView"/> that creates custom scroll effects using slivers.
/// </summary>
public class CustomScrollView : ScrollView
{
    public CustomScrollView(
        IReadOnlyList<Widget>? slivers = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        bool shrinkWrap = false,
        Key? center = null,
        double anchor = 0.0,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        int? semanticChildCount = null,
        SliverPaintOrder paintOrder = SliverPaintOrder.FirstIsTop,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null) : base(
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            scrollBehavior: scrollBehavior,
            shrinkWrap: shrinkWrap,
            center: center,
            anchor: anchor,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount,
            paintOrder: paintOrder,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key)
    {
        Slivers = slivers ?? [];
    }

    /// <summary>The slivers to place inside the viewport.</summary>
    public IReadOnlyList<Widget> Slivers { get; }

    /// <inheritdoc />
    public override IReadOnlyList<Widget> BuildSlivers(BuildContext context) => Slivers;
}

/// <summary>
/// A <see cref="ScrollView"/> that uses a single child layout model, wrapped in the padding the
/// ambient <see cref="MediaQuery"/> reports when no explicit padding is given.
/// </summary>
public abstract class BoxScrollView : ScrollView
{
    protected BoxScrollView(
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        int? semanticChildCount = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null) : base(
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key)
    {
        Padding = padding;
    }

    /// <summary>The amount of space by which to inset the children.</summary>
    public Thickness? Padding { get; }

    /// <inheritdoc />
    public override IReadOnlyList<Widget> BuildSlivers(BuildContext context)
    {
        Widget sliver = BuildChildLayout(context);
        Thickness? effectivePadding = Padding;
        if (Padding is null && MediaQuery.MaybeOf(context) is { } mediaQuery)
        {
            // Automatically pad the sliver with the padding from the MediaQuery.
            var mediaQueryHorizontalPadding = new Thickness(
                mediaQuery.Padding.Left,
                0.0,
                mediaQuery.Padding.Right,
                0.0);
            var mediaQueryVerticalPadding = new Thickness(
                0.0,
                mediaQuery.Padding.Top,
                0.0,
                mediaQuery.Padding.Bottom);
            // Consume the main-axis padding with a SliverPadding.
            effectivePadding = ScrollDirection == Axis.Vertical
                ? mediaQueryVerticalPadding
                : mediaQueryHorizontalPadding;
            // Leave behind the cross-axis padding.
            sliver = new MediaQuery(
                mediaQuery.CopyWith(
                    padding: ScrollDirection == Axis.Vertical
                        ? mediaQueryHorizontalPadding
                        : mediaQueryVerticalPadding),
                sliver);
        }

        if (effectivePadding is { } resolvedPadding)
        {
            sliver = new SliverPadding(resolvedPadding, sliver);
        }

        return [sliver];
    }

    /// <summary>Builds the sliver that lays out this view's children.</summary>
    public abstract Widget BuildChildLayout(BuildContext context);

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Thickness?>("padding", Padding, defaultValue: null));
    }
}

/// <summary>
/// A scrollable list of widgets arranged linearly.
/// </summary>
public sealed class ListView : BoxScrollView
{
    private ListView(
        SliverChildDelegate childrenDelegate,
        double? itemExtent,
        ItemExtentBuilder? itemExtentBuilder,
        Widget? prototypeItem,
        Axis scrollDirection,
        bool reverse,
        ScrollController? controller,
        bool? primary,
        ScrollPhysics? physics,
        bool shrinkWrap,
        Thickness? padding,
        double? cacheExtent,
        ScrollCacheExtent? scrollCacheExtent,
        int? semanticChildCount,
        DragStartBehavior dragStartBehavior,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior,
        string? restorationId,
        Clip clipBehavior,
        HitTestBehavior hitTestBehavior,
        Key? key) : base(
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key)
    {
        if (!((itemExtent is null && prototypeItem is null)
              || (itemExtent is null && itemExtentBuilder is null)
              || (prototypeItem is null && itemExtentBuilder is null)))
        {
            throw new ArgumentException(
                "You can only pass one of itemExtent, prototypeItem and itemExtentBuilder.");
        }

        ItemExtent = itemExtent;
        ItemExtentBuilder = itemExtentBuilder;
        PrototypeItem = prototypeItem;
        ChildrenDelegate = childrenDelegate;
    }

    public ListView(
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        double? itemExtent = null,
        ItemExtentBuilder? itemExtentBuilder = null,
        Widget? prototypeItem = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        int? semanticChildCount = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null) : this(
            childrenDelegate: new SliverChildListDelegate(
                children ?? [],
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes),
            itemExtent: itemExtent,
            itemExtentBuilder: itemExtentBuilder,
            prototypeItem: prototypeItem,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount ?? (children?.Count ?? 0),
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key)
    {
    }

    /// <summary>If non-null, forces the children to have the given extent in the scroll direction.</summary>
    public double? ItemExtent { get; }

    /// <summary>If non-null, called to compute each child's extent in the scroll direction.</summary>
    public ItemExtentBuilder? ItemExtentBuilder { get; }

    /// <summary>If non-null, forces the children to have the same extent as this widget.</summary>
    public Widget? PrototypeItem { get; }

    /// <summary>The delegate that provides the children for this widget.</summary>
    public SliverChildDelegate ChildrenDelegate { get; }

    /// <remarks>Flutter's <c>ListView.builder</c>; a null <paramref name="itemCount"/> is unbounded.</remarks>
    public static ListView Builder(
        NullableIndexedWidgetBuilder itemBuilder,
        int? itemCount = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        double? itemExtent = null,
        ItemExtentBuilder? itemExtentBuilder = null,
        Widget? prototypeItem = null,
        ChildIndexGetter? findChildIndexCallback = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        int? semanticChildCount = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null)
    {
        if (itemCount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), "itemCount cannot be negative.");
        }

        if (semanticChildCount is not null && semanticChildCount > itemCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(semanticChildCount),
                "semanticChildCount must be between 0 and itemCount.");
        }

        return new ListView(
            childrenDelegate: new SliverChildBuilderDelegate(
                itemBuilder,
                itemCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes,
                findChildIndexCallback: findChildIndexCallback),
            itemExtent: itemExtent,
            itemExtentBuilder: itemExtentBuilder,
            prototypeItem: prototypeItem,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount ?? itemCount,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key);
    }

    /// <remarks>
    /// Flutter's <c>ListView.separated</c>. <paramref name="findItemIndexCallback"/> returns an item
    /// index and is doubled internally; <paramref name="findChildIndexCallback"/> is Dart's
    /// deprecated form, which already returns a child index. Only one of the two may be given.
    /// </remarks>
    public static ListView Separated(
        int itemCount,
        NullableIndexedWidgetBuilder itemBuilder,
        IndexedWidgetBuilder separatorBuilder,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        ChildIndexGetter? findItemIndexCallback = null,
        ChildIndexGetter? findChildIndexCallback = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null)
    {
        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), "itemCount cannot be negative.");
        }

        if (findItemIndexCallback is not null && findChildIndexCallback is not null)
        {
            throw new ArgumentException(
                "Cannot provide both findItemIndexCallback and findChildIndexCallback. "
                + "Use findItemIndexCallback as findChildIndexCallback is deprecated.");
        }

        // A separated list holds two delegate children per item, so an *item* index has to be
        // doubled before the delegate can use it. Dart's deprecated `findChildIndexCallback`
        // already returns a child index and is passed through.
        ChildIndexGetter? effectiveFindChildIndexCallback = findItemIndexCallback is null
            ? findChildIndexCallback
            : childKey => findItemIndexCallback(childKey) is { } itemIndex ? itemIndex * 2 : null;

        return new ListView(
            childrenDelegate: new SliverChildBuilderDelegate(
                (buildContext, index) =>
                {
                    int itemIndex = index / 2;
                    return index % 2 == 0
                        ? itemBuilder(buildContext, itemIndex)
                        : separatorBuilder(buildContext, itemIndex);
                },
                ComputeActualChildCount(itemCount),
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes,
                // A separated list gives its separators no semantic index at all, so item n keeps
                // index n.
                semanticIndexCallback: static (_, index) => index % 2 == 0 ? index / 2 : null,
                findChildIndexCallback: effectiveFindChildIndexCallback),
            itemExtent: null,
            itemExtentBuilder: null,
            prototypeItem: null,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: itemCount,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key);
    }

    /// <remarks>Flutter's <c>ListView.custom</c>.</remarks>
    public static ListView Custom(
        SliverChildDelegate childrenDelegate,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        double? itemExtent = null,
        Widget? prototypeItem = null,
        ItemExtentBuilder? itemExtentBuilder = null,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        int? semanticChildCount = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null)
    {
        return new ListView(
            childrenDelegate: childrenDelegate,
            itemExtent: itemExtent,
            itemExtentBuilder: itemExtentBuilder,
            prototypeItem: prototypeItem,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key);
    }

    /// <inheritdoc />
    public override Widget BuildChildLayout(BuildContext context)
    {
        if (ItemExtent is { } itemExtent)
        {
            return new SliverFixedExtentList(ChildrenDelegate, itemExtent);
        }

        if (ItemExtentBuilder is { } itemExtentBuilder)
        {
            return new SliverVariedExtentList(ChildrenDelegate, itemExtentBuilder);
        }

        if (PrototypeItem is { } prototypeItem)
        {
            return new SliverPrototypeExtentList(ChildrenDelegate, prototypeItem);
        }

        return new SliverList(ChildrenDelegate);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("itemExtent", ItemExtent, defaultValue: null));
    }

    /// <remarks>Flutter's <c>ListView._computeActualChildCount</c>.</remarks>
    private static int ComputeActualChildCount(int itemCount) => Math.Max(0, (itemCount * 2) - 1);
}

/// <summary>
/// A scrollable, 2D array of widgets.
/// </summary>
public sealed class GridView : BoxScrollView
{
    private GridView(
        SliverGridDelegate gridDelegate,
        SliverChildDelegate childrenDelegate,
        Axis scrollDirection,
        bool reverse,
        ScrollController? controller,
        bool? primary,
        ScrollPhysics? physics,
        bool shrinkWrap,
        Thickness? padding,
        double? cacheExtent,
        ScrollCacheExtent? scrollCacheExtent,
        int? semanticChildCount,
        DragStartBehavior dragStartBehavior,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior,
        string? restorationId,
        Clip clipBehavior,
        HitTestBehavior hitTestBehavior,
        Key? key) : base(
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key)
    {
        GridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
        ChildrenDelegate = childrenDelegate;
    }

    public GridView(
        SliverGridDelegate gridDelegate,
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        int? semanticChildCount = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null) : this(
            gridDelegate: gridDelegate,
            childrenDelegate: new SliverChildListDelegate(
                children ?? [],
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes),
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount ?? (children?.Count ?? 0),
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key)
    {
    }

    /// <summary>The delegate that controls the layout of the children within the grid.</summary>
    public SliverGridDelegate GridDelegate { get; }

    /// <summary>The delegate that provides the children for this widget.</summary>
    public SliverChildDelegate ChildrenDelegate { get; }

    /// <remarks>Flutter's <c>GridView.builder</c>; a null <paramref name="itemCount"/> is unbounded.</remarks>
    public static GridView Builder(
        NullableIndexedWidgetBuilder itemBuilder,
        SliverGridDelegate gridDelegate,
        int? itemCount = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        ChildIndexGetter? findChildIndexCallback = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        int? semanticChildCount = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null)
    {
        return new GridView(
            gridDelegate: gridDelegate,
            childrenDelegate: new SliverChildBuilderDelegate(
                itemBuilder,
                itemCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes,
                findChildIndexCallback: findChildIndexCallback),
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount ?? itemCount,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key);
    }

    /// <remarks>Flutter's <c>GridView.custom</c>.</remarks>
    public static GridView Custom(
        SliverGridDelegate gridDelegate,
        SliverChildDelegate childrenDelegate,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        int? semanticChildCount = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null)
    {
        return new GridView(
            gridDelegate: gridDelegate,
            childrenDelegate: childrenDelegate,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key);
    }

    /// <remarks>Flutter's <c>GridView.count</c>.</remarks>
    public static GridView Count(
        int crossAxisCount,
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        double mainAxisSpacing = 0.0,
        double crossAxisSpacing = 0.0,
        double childAspectRatio = 1.0,
        double? mainAxisExtent = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        int? semanticChildCount = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null)
    {
        return new GridView(
            gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: crossAxisCount,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio,
                mainAxisExtent: mainAxisExtent),
            children: children,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            addRepaintBoundaries: addRepaintBoundaries,
            addSemanticIndexes: addSemanticIndexes,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key);
    }

    /// <remarks>Flutter's <c>GridView.extent</c>.</remarks>
    public static GridView Extent(
        double maxCrossAxisExtent,
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        bool shrinkWrap = false,
        Thickness? padding = null,
        double mainAxisSpacing = 0.0,
        double crossAxisSpacing = 0.0,
        double childAspectRatio = 1.0,
        double? mainAxisExtent = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        double? cacheExtent = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        int? semanticChildCount = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null)
    {
        return new GridView(
            gridDelegate: new SliverGridDelegateWithMaxCrossAxisExtent(
                maxCrossAxisExtent: maxCrossAxisExtent,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio,
                mainAxisExtent: mainAxisExtent),
            children: children,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            primary: primary,
            physics: physics,
            shrinkWrap: shrinkWrap,
            padding: padding,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            addRepaintBoundaries: addRepaintBoundaries,
            addSemanticIndexes: addSemanticIndexes,
            cacheExtent: cacheExtent,
            scrollCacheExtent: scrollCacheExtent,
            semanticChildCount: semanticChildCount,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key);
    }

    /// <inheritdoc />
    public override Widget BuildChildLayout(BuildContext context)
    {
        return new SliverGrid(ChildrenDelegate, GridDelegate);
    }
}
