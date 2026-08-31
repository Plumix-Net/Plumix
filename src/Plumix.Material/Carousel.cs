using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Physics;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/carousel.dart
// Dart parity source: material_ui/lib/src/carousel_theme.dart

/// <summary>Dart's <c>NullableIndexedWidgetBuilder</c>, the builder a lazy carousel is given.</summary>
public delegate Widget? CarouselItemBuilder(BuildContext context, int index);

public sealed partial record CarouselViewThemeData(
    double? Elevation = null,
    Color? BackgroundColor = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    ShapeBorder? Shape = null,
    Thickness? Padding = null,
    Clip? ItemClipBehavior = null)
{
    public CarouselViewThemeData CopyWith(
        double? elevation = null,
        Color? backgroundColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        ShapeBorder? shape = null,
        Thickness? padding = null,
        Clip? itemClipBehavior = null)
    {
        return new CarouselViewThemeData(
            Elevation: elevation ?? Elevation,
            BackgroundColor: backgroundColor ?? BackgroundColor,
            OverlayColor: overlayColor ?? OverlayColor,
            Shape: shape ?? Shape,
            Padding: padding ?? Padding,
            ItemClipBehavior: itemClipBehavior ?? ItemClipBehavior);
    }
}

public sealed class CarouselViewTheme : InheritedTheme
{
    public CarouselViewTheme(CarouselViewThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public CarouselViewThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) => new CarouselViewTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((CarouselViewTheme)oldWidget).Data);

    public static CarouselViewThemeData? MaybeOf(BuildContext context) =>
        context.DependOnInherited<CarouselViewTheme>()?.Data;

    public static CarouselViewThemeData Of(BuildContext context) =>
        MaybeOf(context) ?? Theme.Of(context).CarouselViewTheme;
}

/// <summary>
/// A Material Design carousel: a scrollable strip of items whose extents can vary with the scroll
/// offset, so that items grow into and shrink out of the viewport edges.
/// </summary>
public sealed class CarouselView : StatefulWidget
{
    /// <summary>The uncontained layout: every item is <paramref name="itemExtent"/> wide.</summary>
    public CarouselView(
        double itemExtent,
        IReadOnlyList<Widget> children,
        Thickness? padding = null,
        Color? backgroundColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? itemClipBehavior = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        bool itemSnapping = false,
        double shrinkExtent = 0,
        CarouselController? controller = null,
        Axis scrollDirection = Axis.Horizontal,
        bool reverse = false,
        Action<int>? onTap = null,
        bool enableSplash = true,
        bool infinite = false,
        Action<int>? onIndexChanged = null,
        Key? key = null) : this(
        itemExtent,
        flexWeights: null,
        children,
        itemBuilder: null,
        itemCount: null,
        padding,
        backgroundColor,
        elevation,
        shape,
        itemClipBehavior,
        overlayColor,
        itemSnapping,
        shrinkExtent,
        controller,
        scrollDirection,
        reverse,
        consumeMaxWeight: true,
        onTap,
        enableSplash,
        infinite,
        onIndexChanged,
        key)
    {
    }

    private CarouselView(
        double? itemExtent,
        IReadOnlyList<int>? flexWeights,
        IReadOnlyList<Widget> children,
        CarouselItemBuilder? itemBuilder,
        int? itemCount,
        Thickness? padding,
        Color? backgroundColor,
        double? elevation,
        ShapeBorder? shape,
        Clip? itemClipBehavior,
        MaterialStateProperty<Color?>? overlayColor,
        bool itemSnapping,
        double shrinkExtent,
        CarouselController? controller,
        Axis scrollDirection,
        bool reverse,
        bool consumeMaxWeight,
        Action<int>? onTap,
        bool enableSplash,
        bool infinite,
        Action<int>? onIndexChanged,
        Key? key) : base(key)
    {
        ItemExtent = itemExtent;
        FlexWeights = flexWeights;
        Children = children ?? throw new ArgumentNullException(nameof(children));
        ItemBuilder = itemBuilder;
        ItemCount = itemCount;
        Padding = padding;
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        Shape = shape;
        ItemClipBehavior = itemClipBehavior;
        OverlayColor = overlayColor;
        ItemSnapping = itemSnapping;
        ShrinkExtent = shrinkExtent;
        Controller = controller;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        ConsumeMaxWeight = consumeMaxWeight;
        OnTap = onTap;
        EnableSplash = enableSplash;
        Infinite = infinite;
        OnIndexChanged = onIndexChanged;
    }

    public double? ItemExtent { get; }

    public IReadOnlyList<int>? FlexWeights { get; }

    public IReadOnlyList<Widget> Children { get; }

    public CarouselItemBuilder? ItemBuilder { get; }

    public int? ItemCount { get; }

    public Thickness? Padding { get; }

    public Color? BackgroundColor { get; }

    public double? Elevation { get; }

    public ShapeBorder? Shape { get; }

    public Clip? ItemClipBehavior { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public bool ItemSnapping { get; }

    public double ShrinkExtent { get; }

    public CarouselController? Controller { get; }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public bool ConsumeMaxWeight { get; }

    public Action<int>? OnTap { get; }

    public bool EnableSplash { get; }

    public bool Infinite { get; }

    public Action<int>? OnIndexChanged { get; }

    /// <summary>Dart's <c>CarouselView.weighted</c>: item extents follow <paramref name="flexWeights"/>.</summary>
    public static CarouselView Weighted(
        IReadOnlyList<int> flexWeights,
        IReadOnlyList<Widget> children,
        Thickness? padding = null,
        Color? backgroundColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? itemClipBehavior = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        bool itemSnapping = false,
        double shrinkExtent = 0,
        CarouselController? controller = null,
        Axis scrollDirection = Axis.Horizontal,
        bool reverse = false,
        bool consumeMaxWeight = true,
        Action<int>? onTap = null,
        bool enableSplash = true,
        bool infinite = false,
        Action<int>? onIndexChanged = null,
        Key? key = null)
    {
        return new CarouselView(
            null, flexWeights, children, null, null, padding, backgroundColor, elevation, shape,
            itemClipBehavior, overlayColor, itemSnapping, shrinkExtent, controller, scrollDirection,
            reverse, consumeMaxWeight, onTap, enableSplash, infinite, onIndexChanged, key);
    }

    /// <summary>Dart's <c>CarouselView.builder</c>: the uncontained layout, built lazily.</summary>
    public static CarouselView Builder(
        double itemExtent,
        CarouselItemBuilder itemBuilder,
        int? itemCount = null,
        Thickness? padding = null,
        Color? backgroundColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? itemClipBehavior = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        bool itemSnapping = false,
        double shrinkExtent = 0,
        CarouselController? controller = null,
        Axis scrollDirection = Axis.Horizontal,
        bool reverse = false,
        Action<int>? onTap = null,
        bool enableSplash = true,
        bool infinite = false,
        Action<int>? onIndexChanged = null,
        Key? key = null)
    {
        return new CarouselView(
            itemExtent, null, [], itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder)),
            itemCount, padding, backgroundColor, elevation, shape, itemClipBehavior, overlayColor,
            itemSnapping, shrinkExtent, controller, scrollDirection, reverse, true, onTap, enableSplash,
            infinite, onIndexChanged, key);
    }

    /// <summary>Dart's <c>CarouselView.weightedBuilder</c>: the weighted layout, built lazily.</summary>
    public static CarouselView WeightedBuilder(
        IReadOnlyList<int> flexWeights,
        CarouselItemBuilder itemBuilder,
        int? itemCount = null,
        Thickness? padding = null,
        Color? backgroundColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? itemClipBehavior = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        bool itemSnapping = false,
        double shrinkExtent = 0,
        CarouselController? controller = null,
        Axis scrollDirection = Axis.Horizontal,
        bool reverse = false,
        bool consumeMaxWeight = true,
        Action<int>? onTap = null,
        bool enableSplash = true,
        bool infinite = false,
        Action<int>? onIndexChanged = null,
        Key? key = null)
    {
        return new CarouselView(
            null, flexWeights, [], itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder)),
            itemCount, padding, backgroundColor, elevation, shape, itemClipBehavior, overlayColor,
            itemSnapping, shrinkExtent, controller, scrollDirection, reverse, consumeMaxWeight, onTap,
            enableSplash, infinite, onIndexChanged, key);
    }

    public override State CreateState() => new CarouselViewState();
}

public sealed class CarouselViewState : State
{
    private double? _itemExtent;
    private CarouselController? _internalController;
    private int _lastReportedLeadingItem;

    internal CarouselView Current => (CarouselView)StateWidget;

    /// <summary>The item extent clamped to the viewport, recomputed by every layout pass.</summary>
    internal double? EffectiveItemExtent => _itemExtent;

    internal IReadOnlyList<int>? FlexWeights => Current.FlexWeights;

    internal bool ConsumeMaxWeight => Current.ConsumeMaxWeight;

    internal CarouselController Controller => Current.Controller ?? _internalController!;

    public override void InitState()
    {
        _itemExtent = Current.ItemExtent;
        if (Current.Controller is null)
        {
            _internalController = new CarouselController();
        }

        _lastReportedLeadingItem = GetInitialLeadingItem();
        Controller.AttachCarousel(this);
        Controller.AddListener(HandleScroll);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        CarouselView oldCarousel = (CarouselView)oldWidget;
        if (!ReferenceEquals(Current.Controller, oldCarousel.Controller))
        {
            oldCarousel.Controller?.DetachCarousel(this);
            if (Current.Controller is not null)
            {
                _internalController?.DetachCarousel(this);
                _internalController?.Dispose();
                _internalController = null;
                Current.Controller.AttachCarousel(this);
            }
            else
            {
                _internalController = new CarouselController();
                Controller.AttachCarousel(this);
            }
        }

        if (!WeightsEqual(Current.FlexWeights, oldCarousel.FlexWeights))
        {
            AttachedPosition?.SetFlexWeights(Current.FlexWeights);
        }

        if (Current.ItemExtent != oldCarousel.ItemExtent)
        {
            _itemExtent = Current.ItemExtent;
            AttachedPosition?.SetItemExtent(_itemExtent);
        }

        if (Current.ConsumeMaxWeight != oldCarousel.ConsumeMaxWeight)
        {
            AttachedPosition?.SetConsumeMaxWeight(Current.ConsumeMaxWeight);
        }
    }

    public override void Dispose()
    {
        Controller.RemoveListener(HandleScroll);
        Controller.DetachCarousel(this);
        _internalController?.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        ScrollPhysics physics = Current.ItemSnapping
            ? new CarouselScrollPhysics()
            : ScrollConfiguration.Of(context).GetScrollPhysics(context);

        return new LayoutBuilder((layoutContext, constraints) =>
        {
            double mainAxisExtent = Current.ScrollDirection == Axis.Horizontal
                ? constraints.MaxWidth
                : constraints.MaxHeight;
            _itemExtent = Current.ItemExtent is null
                ? null
                : Math.Clamp(Current.ItemExtent.Value, 0, double.IsFinite(mainAxisExtent) ? mainAxisExtent : 0);

            return new CustomScrollView(
                slivers: [BuildSliverCarousel()],
                scrollDirection: Current.ScrollDirection,
                reverse: Current.Reverse,
                controller: Controller,
                physics: physics,
                cacheExtent: 0,
                cacheExtentStyle: CacheExtentStyle.Viewport,
                clipBehavior: Clip.AntiAlias);
        });
    }

    internal int? ItemCount => Current.ItemBuilder is not null ? Current.ItemCount : Current.Children.Count;

    private CarouselScrollPosition? AttachedPosition => Controller.PrimaryPosition as CarouselScrollPosition;

    private Widget BuildSliverCarousel()
    {
        int? childCount = Current.Infinite
            ? null
            : Current.ItemBuilder is not null ? Current.ItemCount : Current.Children.Count;

        CarouselItemBuilder effectiveBuilder;
        if (Current.ItemBuilder is not null)
        {
            CarouselItemBuilder builder = Current.ItemBuilder;
            if (Current.Infinite && Current.ItemCount is > 0)
            {
                int itemCount = Current.ItemCount.Value;
                effectiveBuilder = (itemContext, index) => builder(itemContext, index % itemCount);
            }
            else
            {
                effectiveBuilder = builder;
            }
        }
        else
        {
            effectiveBuilder = (itemContext, index) => BuildCarouselItem(itemContext, index);
        }

        SliverChildDelegate childDelegate = new CarouselChildDelegate(effectiveBuilder, childCount);
        if (_itemExtent is { } itemExtent)
        {
            return new SliverFixedExtentCarousel(
                itemExtent: itemExtent,
                minExtent: Current.ShrinkExtent,
                infinite: Current.Infinite,
                @delegate: childDelegate);
        }

        if (FlexWeights is not { Count: > 0 } weights || weights.Any(weight => weight <= 0))
        {
            throw new InvalidOperationException("flexWeights is null or it contains non-positive integers");
        }

        return new SliverWeightedCarousel(
            consumeMaxWeight: ConsumeMaxWeight,
            shrinkExtent: Current.ShrinkExtent,
            weights: weights,
            infinite: Current.Infinite,
            @delegate: childDelegate);
    }

    private Widget BuildCarouselItem(BuildContext itemContext, int index)
    {
        if (Current.Infinite && Current.Children.Count > 0)
        {
            index %= Current.Children.Count;
        }

        CarouselViewThemeData carouselTheme = CarouselViewTheme.Of(itemContext);
        ColorScheme colorScheme = Theme.Of(itemContext).ColorScheme;
        Thickness effectivePadding = Current.Padding ?? carouselTheme.Padding ?? new Thickness(4.0);
        Color effectiveBackgroundColor =
            Current.BackgroundColor ?? carouselTheme.BackgroundColor ?? colorScheme.Surface;
        double effectiveElevation = Current.Elevation ?? carouselTheme.Elevation ?? 0.0;
        ShapeBorder effectiveShape = Current.Shape ?? carouselTheme.Shape ?? new RoundedRectangleBorder(
            borderRadius: Plumix.Rendering.BorderRadius.Circular(28.0));
        Clip effectiveClipBehavior = Current.ItemClipBehavior ?? carouselTheme.ItemClipBehavior ?? Clip.AntiAlias;
        MaterialStateProperty<Color?> effectiveOverlayColor = Current.OverlayColor
            ?? carouselTheme.OverlayColor
            ?? DefaultOverlayColor(colorScheme.OnSurface);

        Widget contents = index >= 0 && index < Current.Children.Count
            ? Current.Children[index]
            : new SizedBox();
        if (Current.EnableSplash)
        {
            int tapIndex = index;
            contents = new Stack(
                fit: StackFit.Expand,
                children:
                [
                    contents,
                    new Material(
                        color: Colors.Transparent,
                        child: new InkWell(
                            onTap: () => Current.OnTap?.Invoke(tapIndex),
                            overlayColor: effectiveOverlayColor)),
                ]);
        }
        else if (Current.OnTap is not null)
        {
            int tapIndex = index;
            Action<int> onTap = Current.OnTap;
            contents = new GestureDetector(onTap: () => onTap(tapIndex), child: contents);
        }

        return new Padding(
            effectivePadding,
            new Material(
                clipBehavior: effectiveClipBehavior,
                color: effectiveBackgroundColor,
                elevation: effectiveElevation,
                shape: effectiveShape,
                child: contents));
    }

    private void HandleScroll()
    {
        if (Current.OnIndexChanged is null || AttachedPosition is not { } position)
        {
            return;
        }

        int currentLeadingItem = position.LeadingItem;
        if (currentLeadingItem != _lastReportedLeadingItem)
        {
            _lastReportedLeadingItem = currentLeadingItem;
            Current.OnIndexChanged(currentLeadingItem);
        }
    }

    private int GetInitialLeadingItem()
    {
        if (Current.FlexWeights is { Count: > 0 } weights)
        {
            return Math.Max(Controller.InitialItem - FirstMaximumWeightIndex(weights), 0);
        }

        return Controller.InitialItem;
    }

    internal static int FirstMaximumWeightIndex(IReadOnlyList<int> weights)
    {
        int maximum = weights.Max();
        for (int index = 0; index < weights.Count; index += 1)
        {
            if (weights[index] == maximum)
            {
                return index;
            }
        }

        return 0;
    }

    private static bool WeightsEqual(IReadOnlyList<int>? a, IReadOnlyList<int>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null || a.Count != b.Count)
        {
            return false;
        }

        return !a.Where((weight, index) => weight != b[index]).Any();
    }

    private static MaterialStateProperty<Color?> DefaultOverlayColor(Color onSurface)
    {
        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Pressed))
            {
                return onSurface.WithOpacity(0.1);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return onSurface.WithOpacity(0.08);
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                return onSurface.WithOpacity(0.1);
            }

            return null;
        });
    }

    /// <summary>
    /// Dart passes a <c>NullableIndexedWidgetBuilder</c> to <c>SliverChildBuilderDelegate</c>;
    /// Plumix's builder delegate takes a non-nullable builder, so the carousel keeps its own.
    /// </summary>
    private sealed class CarouselChildDelegate : SliverChildDelegate
    {
        private readonly CarouselItemBuilder _builder;
        private readonly int? _childCount;

        public CarouselChildDelegate(CarouselItemBuilder builder, int? childCount)
        {
            _builder = builder;
            _childCount = childCount;
        }

        public override int? EstimatedChildCount => _childCount;

        public override Widget? Build(BuildContext context, int index)
        {
            if (index < 0 || (_childCount.HasValue && index >= _childCount.Value))
            {
                return null;
            }

            Widget? child = _builder(context, index);
            return child is null ? null : new AutomaticKeepAlive(child);
        }
    }
}

/// <summary>Dart's <c>_SliverFixedExtentCarousel</c>.</summary>
internal sealed class SliverFixedExtentCarousel : SliverMultiBoxAdaptorWidget
{
    public SliverFixedExtentCarousel(
        double itemExtent,
        double minExtent,
        bool infinite,
        SliverChildDelegate @delegate,
        Key? key = null) : base(@delegate, key)
    {
        ItemExtent = itemExtent;
        MinExtent = minExtent;
        Infinite = infinite;
    }

    public double ItemExtent { get; }

    public double MinExtent { get; }

    public bool Infinite { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderSliverFixedExtentCarousel(maxExtent: ItemExtent, minExtent: MinExtent, infinite: Infinite);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var carousel = (RenderSliverFixedExtentCarousel)renderObject;
        carousel.MaxExtent = ItemExtent;
        carousel.MinExtent = MinExtent;
        carousel.Infinite = Infinite;
    }
}

/// <summary>Dart's <c>_SliverWeightedCarousel</c>.</summary>
internal sealed class SliverWeightedCarousel : SliverMultiBoxAdaptorWidget
{
    public SliverWeightedCarousel(
        bool consumeMaxWeight,
        double shrinkExtent,
        IReadOnlyList<int> weights,
        bool infinite,
        SliverChildDelegate @delegate,
        Key? key = null) : base(@delegate, key)
    {
        ConsumeMaxWeight = consumeMaxWeight;
        ShrinkExtent = shrinkExtent;
        Weights = weights;
        Infinite = infinite;
    }

    public bool ConsumeMaxWeight { get; }

    public double ShrinkExtent { get; }

    public IReadOnlyList<int> Weights { get; }

    public bool Infinite { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderSliverWeightedCarousel(
        consumeMaxWeight: ConsumeMaxWeight,
        shrinkExtent: ShrinkExtent,
        weights: Weights,
        infinite: Infinite);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var carousel = (RenderSliverWeightedCarousel)renderObject;
        carousel.ConsumeMaxWeight = ConsumeMaxWeight;
        carousel.ShrinkExtent = ShrinkExtent;
        carousel.Weights = Weights;
        carousel.Infinite = Infinite;
    }
}

/// <summary>
/// Dart's <c>_RenderSliverFixedExtentCarousel</c>: a fixed-extent list whose leading and trailing
/// items shrink instead of being clipped by the viewport.
/// </summary>
internal sealed class RenderSliverFixedExtentCarousel : RenderSliverFixedExtentBoxAdaptor
{
    private readonly ItemExtentBuilder _extentBuilder;
    private double _maxExtent;
    private double _minExtent;
    private bool _infinite;

    public RenderSliverFixedExtentCarousel(double maxExtent, double minExtent, bool infinite)
    {
        _maxExtent = maxExtent;
        _minExtent = minExtent;
        _infinite = infinite;
        _extentBuilder = BuildItemExtent;
    }

    public double MaxExtent
    {
        get => _maxExtent;
        set
        {
            if (_maxExtent == value)
            {
                return;
            }

            _maxExtent = value;
            MarkNeedsLayout();
        }
    }

    public double MinExtent
    {
        get => _minExtent;
        set
        {
            if (_minExtent == value)
            {
                return;
            }

            _minExtent = value;
            MarkNeedsLayout();
        }
    }

    public bool Infinite
    {
        get => _infinite;
        set
        {
            if (_infinite == value)
            {
                return;
            }

            _infinite = value;
            MarkNeedsLayout();
        }
    }

    public override double? ItemExtent => null;

    public override ItemExtentBuilder? ItemExtentBuilder => _extentBuilder;

    public override double IndexToLayoutOffset(double itemExtent, int index)
    {
        if (_maxExtent == 0.0)
        {
            return _maxExtent;
        }

        SliverConstraints constraints = ConstraintsForSliver;
        int firstVisibleIndex = (int)Math.Floor(constraints.ScrollOffset / _maxExtent);
        double effectiveMinExtent = Math.Max(
            EuclideanRemainder(constraints.RemainingPaintExtent, _maxExtent),
            _minExtent);
        if (index == firstVisibleIndex)
        {
            double firstVisibleItemExtent = BuildItemExtent(index, LayoutDimensions) ?? 0;
            return firstVisibleItemExtent <= effectiveMinExtent
                ? (_maxExtent * index) - effectiveMinExtent + _maxExtent
                : constraints.ScrollOffset;
        }

        return _maxExtent * index;
    }

    public override int GetMinChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        return _maxExtent > 0.0 ? Math.Max((int)Math.Floor(scrollOffset / _maxExtent), 0) : 0;
    }

    public override int GetMaxChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        if (_maxExtent <= 0.0)
        {
            return 0;
        }

        double actual = (scrollOffset / _maxExtent) - 1;
        int round = RoundHalfAwayFromZero(actual);
        return Math.Abs((actual * _maxExtent) - (round * _maxExtent)) < Constants.PrecisionErrorTolerance
            ? Math.Max(0, round)
            : Math.Max(0, (int)Math.Ceiling(actual));
    }

    private double? BuildItemExtent(int index, SliverLayoutDimensions dimensions)
    {
        if (_maxExtent == 0.0)
        {
            return _maxExtent;
        }

        SliverConstraints constraints = ConstraintsForSliver;
        int offscreenItems = (int)Math.Floor(constraints.ScrollOffset / _maxExtent);
        double offscreenExtent = constraints.ScrollOffset - (offscreenItems * _maxExtent);
        double effectiveMinExtent = Math.Max(
            EuclideanRemainder(constraints.RemainingPaintExtent, _maxExtent),
            _minExtent);
        if (index == offscreenItems)
        {
            return Math.Max(_maxExtent - offscreenExtent, effectiveMinExtent);
        }

        double scrollOffsetForLastIndex = constraints.ScrollOffset + constraints.RemainingPaintExtent;
        if (index == GetMaxChildIndexForScrollOffset(scrollOffsetForLastIndex, _maxExtent))
        {
            return Math.Clamp(scrollOffsetForLastIndex - (_maxExtent * index), effectiveMinExtent, _maxExtent);
        }

        return _maxExtent;
    }

    /// <summary>Dart's <c>%</c> on doubles, which is the Euclidean (never negative) remainder.</summary>
    private static double EuclideanRemainder(double value, double divisor)
    {
        if (divisor == 0)
        {
            return double.NaN;
        }

        double remainder = value % divisor;
        return remainder < 0 ? remainder + Math.Abs(divisor) : remainder;
    }
}

/// <summary>
/// Dart's <c>_RenderSliverWeightedCarousel</c>: item extents are distributed by weight, and each
/// item morphs toward the size of its predecessor as the leading item scrolls off.
/// </summary>
internal sealed class RenderSliverWeightedCarousel : RenderSliverFixedExtentBoxAdaptor
{
    private readonly ItemExtentBuilder _extentBuilder;
    private bool _consumeMaxWeight;
    private double _shrinkExtent;
    private IReadOnlyList<int> _weights;
    private bool _infinite;

    public RenderSliverWeightedCarousel(
        bool consumeMaxWeight,
        double shrinkExtent,
        IReadOnlyList<int> weights,
        bool infinite)
    {
        _consumeMaxWeight = consumeMaxWeight;
        _shrinkExtent = shrinkExtent;
        _weights = weights;
        _infinite = infinite;
        _extentBuilder = BuildItemExtent;
    }

    public bool ConsumeMaxWeight
    {
        get => _consumeMaxWeight;
        set
        {
            if (_consumeMaxWeight == value)
            {
                return;
            }

            _consumeMaxWeight = value;
            MarkNeedsLayout();
        }
    }

    public double ShrinkExtent
    {
        get => _shrinkExtent;
        set
        {
            if (_shrinkExtent == value)
            {
                return;
            }

            _shrinkExtent = value;
            MarkNeedsLayout();
        }
    }

    public IReadOnlyList<int> Weights
    {
        get => _weights;
        set
        {
            if (ReferenceEquals(_weights, value))
            {
                return;
            }

            _weights = value;
            MarkNeedsLayout();
        }
    }

    public bool Infinite
    {
        get => _infinite;
        set
        {
            if (_infinite == value)
            {
                return;
            }

            _infinite = value;
            MarkNeedsLayout();
        }
    }

    public override double? ItemExtent => null;

    public override ItemExtentBuilder? ItemExtentBuilder => _extentBuilder;

    private double ExtentUnit => ConstraintsForSliver.ViewportMainAxisExtent / _weights.Sum();

    private double FirstChildExtent => _weights[0] * ExtentUnit;

    private double MaxChildExtent => _weights.Max() * ExtentUnit;

    private double MinChildExtent => _weights.Min() * ExtentUnit;

    private double EffectiveShrinkExtent => Math.Clamp(_shrinkExtent, 0, MinChildExtent);

    /// <summary>
    /// The index of the item that occupies the leading weight slot. With
    /// <see cref="ConsumeMaxWeight"/> the index is biased backwards by the weights that precede the
    /// maximum one, which is what reserves the leading slots for item 0.
    /// </summary>
    private int FirstVisibleItemIndex
    {
        get
        {
            if (ConstraintsForSliver.ViewportMainAxisExtent == 0.0)
            {
                return 0;
            }

            int smallerWeightCount = 0;
            int maximum = _weights.Max();
            foreach (int weight in _weights)
            {
                if (weight == maximum)
                {
                    break;
                }

                smallerWeightCount += 1;
            }

            int index = ScrollOffsetInFirstChildExtents();
            return _consumeMaxWeight ? index - smallerWeightCount : index;
        }
    }

    private double FirstVisibleItemOffscreenExtent
    {
        get
        {
            if (ConstraintsForSliver.ViewportMainAxisExtent == 0.0)
            {
                return 0;
            }

            return ConstraintsForSliver.ScrollOffset - (ScrollOffsetInFirstChildExtents() * FirstChildExtent);
        }
    }

    private double DistanceToLeadingEdge => FirstChildExtent - FirstVisibleItemOffscreenExtent;

    public override double IndexToLayoutOffset(double itemExtent, int index)
    {
        SliverConstraints constraints = ConstraintsForSliver;
        int firstVisibleItemIndex = FirstVisibleItemIndex;
        if (index == firstVisibleItemIndex)
        {
            return DistanceToLeadingEdge <= EffectiveShrinkExtent
                ? constraints.ScrollOffset - EffectiveShrinkExtent + DistanceToLeadingEdge
                : constraints.ScrollOffset;
        }

        double visibleItemsTotalExtent = DistanceToLeadingEdge;
        for (int i = firstVisibleItemIndex + 1; i < index; i += 1)
        {
            visibleItemsTotalExtent += BuildItemExtent(i, LayoutDimensions) ?? 0;
        }

        return constraints.ScrollOffset + visibleItemsTotalExtent;
    }

    public override int GetMinChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        return Math.Max(FirstVisibleItemIndex, 0);
    }

    public override int GetMaxChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        SliverConstraints constraints = ConstraintsForSliver;
        int? childCount = ChildManager?.EstimatedChildCount;
        int firstVisibleItemIndex = FirstVisibleItemIndex;
        if (_infinite && childCount is null)
        {
            double visibleItemsTotalExtent = DistanceToLeadingEdge;
            int index = firstVisibleItemIndex + 1;
            double safeMinExtent = Math.Max(MinChildExtent, 1.0);
            int estimatedUpperBound = firstVisibleItemIndex
                                      + (int)Math.Ceiling(constraints.ViewportMainAxisExtent / safeMinExtent);
            while (visibleItemsTotalExtent < constraints.ViewportMainAxisExtent && index < estimatedUpperBound)
            {
                visibleItemsTotalExtent += BuildItemExtent(index, LayoutDimensions) ?? 0;
                if (visibleItemsTotalExtent >= constraints.ViewportMainAxisExtent)
                {
                    return index;
                }

                index += 1;
            }

            return index;
        }

        if (childCount is not null)
        {
            double visibleItemsTotalExtent = DistanceToLeadingEdge;
            for (int i = firstVisibleItemIndex + 1; i < childCount.Value; i += 1)
            {
                visibleItemsTotalExtent += BuildItemExtent(i, LayoutDimensions) ?? 0;
                if (visibleItemsTotalExtent >= constraints.ViewportMainAxisExtent)
                {
                    return i;
                }
            }
        }

        return childCount ?? 0;
    }

    public override double ComputeMaxScrollOffset(SliverConstraints constraints, double itemExtent)
    {
        return _infinite ? double.PositiveInfinity : (ChildManager?.ChildCount ?? 0) * MaxChildExtent;
    }

    /// <summary>
    /// Dart copies <c>RenderSliverFixedExtentBoxAdaptor.performLayout</c> here to add
    /// <c>extraLayoutOffset</c>, the trailing-item scroll extent, and the <c>consumeMaxWeight</c>
    /// paint origin. The copy is kept 1:1 so the two can be diffed against each other.
    /// </summary>
    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        IRenderSliverBoxChildManager? childManager = ChildManager;
        if (childManager is null || _weights.Count == 0)
        {
            Geometry = default;
            return;
        }

        childManager.DidStartLayout();
        childManager.SetDidUnderflow(false);
        SetLayoutDimensions(constraints);

        double scrollOffset = Math.Max(0, constraints.ScrollOffset + constraints.CacheOrigin);
        double remainingExtent = Math.Max(0, constraints.RemainingCacheExtent);
        double targetEndScrollOffset = scrollOffset + remainingExtent;
        int firstIndex = GetMinChildIndexForScrollOffset(scrollOffset, DeprecatedExtraItemExtent);
        int? targetLastIndex = double.IsFinite(targetEndScrollOffset)
            ? GetMaxChildIndexForScrollOffset(targetEndScrollOffset, DeprecatedExtraItemExtent)
            : null;

        if (FirstChild is not null)
        {
            int leadingGarbage = CalculateLeadingGarbage(firstIndex);
            int trailingGarbage = targetLastIndex is null ? 0 : CalculateTrailingGarbage(targetLastIndex.Value);
            CollectGarbage(leadingGarbage, trailingGarbage);
        }
        else
        {
            CollectGarbage(0, 0);
        }

        if (FirstChild is null
            && !AddInitialChild(firstIndex, IndexToLayoutOffset(DeprecatedExtraItemExtent, firstIndex)))
        {
            double max = firstIndex <= 0 ? 0.0 : ComputeMaxScrollOffset(constraints, DeprecatedExtraItemExtent);
            Geometry = new SliverGeometry(ScrollExtent: max, MaxPaintExtent: max);
            childManager.DidFinishLayout();
            return;
        }

        RenderBox? trailingChildWithLayout = null;
        for (int index = IndexOf(FirstChild!) - 1; index >= firstIndex; index -= 1)
        {
            RenderBox? leading = InsertAndLayoutLeadingChild(ChildConstraintsForIndex(constraints, index));
            if (leading is null)
            {
                Geometry = new SliverGeometry(
                    ScrollOffsetCorrection: IndexToLayoutOffset(DeprecatedExtraItemExtent, index));
                return;
            }

            SetChildGeometry(leading, constraints, IndexToLayoutOffset(DeprecatedExtraItemExtent, index));
            trailingChildWithLayout ??= leading;
        }

        if (trailingChildWithLayout is null)
        {
            RenderBox first = FirstChild!;
            first.Layout(ChildConstraintsForIndex(constraints, IndexOf(first)), parentUsesSize: true);
            SetChildGeometry(first, constraints, IndexToLayoutOffset(DeprecatedExtraItemExtent, firstIndex));
            trailingChildWithLayout = first;
        }

        double extraLayoutOffset = 0;
        if (_consumeMaxWeight)
        {
            int maximum = _weights.Max();
            for (int i = _weights.Count - 1; i >= 0; i -= 1)
            {
                if (_weights[i] == maximum)
                {
                    break;
                }

                extraLayoutOffset += _weights[i] * ExtentUnit;
            }
        }

        double estimatedMaxScrollOffset = double.PositiveInfinity;
        for (int index = IndexOf(trailingChildWithLayout) + 1;
             targetLastIndex is null || index <= targetLastIndex.Value;
             index += 1)
        {
            RenderBox? child = ChildAfter(trailingChildWithLayout);
            if (child is null || IndexOf(child) != index)
            {
                child = InsertAndLayoutChild(ChildConstraintsForIndex(constraints, index), trailingChildWithLayout);
                if (child is null)
                {
                    estimatedMaxScrollOffset =
                        IndexToLayoutOffset(DeprecatedExtraItemExtent, index) + extraLayoutOffset;
                    break;
                }
            }
            else
            {
                child.Layout(ChildConstraintsForIndex(constraints, index), parentUsesSize: true);
            }

            trailingChildWithLayout = child;
            SetChildGeometry(child, constraints, IndexToLayoutOffset(DeprecatedExtraItemExtent, IndexOf(child)));
        }

        int lastIndex = IndexOf(LastChild!);
        double leadingScrollOffset = IndexToLayoutOffset(DeprecatedExtraItemExtent, firstIndex);
        double trailingScrollOffset;
        if (!_infinite && lastIndex + 1 == childManager.ChildCount)
        {
            trailingScrollOffset = IndexToLayoutOffset(DeprecatedExtraItemExtent, lastIndex);
            trailingScrollOffset += Math.Max(
                _weights[^1] * ExtentUnit,
                BuildItemExtent(lastIndex, LayoutDimensions) ?? 0);
            trailingScrollOffset += extraLayoutOffset;
        }
        else
        {
            trailingScrollOffset = IndexToLayoutOffset(DeprecatedExtraItemExtent, lastIndex + 1);
        }

        Geometry = BuildGeometry(
            constraints,
            DeprecatedExtraItemExtent,
            firstIndex,
            lastIndex,
            leadingScrollOffset,
            trailingScrollOffset,
            estimatedMaxScrollOffset,
            paintFrom: _consumeMaxWeight ? 0 : leadingScrollOffset);
        childManager.DidFinishLayout();
    }

    private double? BuildItemExtent(int index, SliverLayoutDimensions dimensions)
    {
        SliverConstraints constraints = ConstraintsForSliver;
        if (constraints.ViewportMainAxisExtent == 0)
        {
            return 0;
        }

        int firstVisibleItemIndex = FirstVisibleItemIndex;
        if (index == firstVisibleItemIndex)
        {
            return Math.Max(DistanceToLeadingEdge, EffectiveShrinkExtent);
        }

        if (index > firstVisibleItemIndex && index - firstVisibleItemIndex + 1 <= _weights.Count)
        {
            int currentIndexOnWeightList = index - firstVisibleItemIndex;
            int currentWeight = _weights[currentIndexOnWeightList];
            double extent = ExtentUnit * currentWeight;
            double progress = FirstVisibleItemOffscreenExtent / FirstChildExtent;
            int previousWeight = _weights[currentIndexOnWeightList - 1];
            double finalIncrease = (previousWeight - currentWeight) / (double)_weights.Max();
            return extent + (finalIncrease * progress * MaxChildExtent);
        }

        if (index > firstVisibleItemIndex)
        {
            double visibleItemsTotalExtent = DistanceToLeadingEdge;
            for (int i = firstVisibleItemIndex + 1; i < index; i += 1)
            {
                visibleItemsTotalExtent += BuildItemExtent(i, dimensions) ?? 0;
            }

            return Math.Max(constraints.RemainingPaintExtent - visibleItemsTotalExtent, EffectiveShrinkExtent);
        }

        return Math.Max(MinChildExtent, EffectiveShrinkExtent);
    }

    private int ScrollOffsetInFirstChildExtents()
    {
        double firstChildExtent = FirstChildExtent;
        if (firstChildExtent <= 0)
        {
            return 0;
        }

        double actual = ConstraintsForSliver.ScrollOffset / firstChildExtent;
        int round = RoundHalfAwayFromZero(actual);
        return Math.Abs(actual - round) < Constants.PrecisionErrorTolerance ? round : (int)Math.Floor(actual);
    }
}

/// <summary>Scroll physics that snap the carousel to whole items.</summary>
public sealed class CarouselScrollPhysics : ScrollPhysics
{
    public CarouselScrollPhysics(ScrollPhysics? parent = null) : base(parent)
    {
    }

    public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor) => new CarouselScrollPhysics(BuildParent(ancestor));

    public override bool AllowImplicitScrolling => true;

    public override Simulation? CreateBallisticSimulation(IScrollMetrics position, double velocity)
    {
        if (position is not CarouselScrollPosition metrics)
        {
            throw new InvalidOperationException(
                "CarouselScrollPhysics can only be used with Scrollables that uses the CarouselController");
        }

        if ((velocity <= 0.0 && metrics.Pixels <= metrics.MinScrollExtent)
            || (velocity >= 0.0 && metrics.Pixels >= metrics.MaxScrollExtent))
        {
            return base.CreateBallisticSimulation(position, velocity);
        }

        Tolerance tolerance = ToleranceFor(position);
        double target = GetTargetPixels(metrics, tolerance, velocity);
        return target != metrics.Pixels
            ? new ScrollSpringSimulation(Spring, metrics.Pixels, target, velocity, tolerance: tolerance)
            : null;
    }

    private static double GetTargetPixels(CarouselScrollPosition position, Tolerance tolerance, double velocity)
    {
        double fraction = position.ItemExtent is { } itemExtent
            ? itemExtent / position.ViewportDimension
            : position.FlexWeights![0] / (double)position.FlexWeights!.Sum();
        double itemWidth = position.ViewportDimension * fraction;
        if (itemWidth <= 0)
        {
            return position.Pixels;
        }

        double actual = Math.Max(0.0, position.Pixels) / itemWidth;
        double round = Math.Round(actual, MidpointRounding.AwayFromZero);
        double item = Math.Abs(actual - round) < Constants.PrecisionErrorTolerance ? round : actual;
        if (velocity < -tolerance.Velocity)
        {
            item -= 0.5;
        }
        else if (velocity > tolerance.Velocity)
        {
            item += 0.5;
        }

        return Math.Round(item, MidpointRounding.AwayFromZero) * itemWidth;
    }
}

/// <summary>Dart's <c>_CarouselMetrics</c>.</summary>
public class CarouselMetrics : FixedScrollMetrics
{
    public CarouselMetrics(
        double? minScrollExtent,
        double? maxScrollExtent,
        double? pixels,
        double? viewportDimension,
        AxisDirection axisDirection,
        double? itemExtent,
        IReadOnlyList<int>? flexWeights,
        bool? consumeMaxWeight,
        double devicePixelRatio)
        : base(minScrollExtent, maxScrollExtent, pixels, viewportDimension, axisDirection, devicePixelRatio)
    {
        ItemExtent = itemExtent;
        FlexWeights = flexWeights;
        ConsumeMaxWeight = consumeMaxWeight;
    }

    /// <summary>Extent of each item in the main axis, when the carousel is not weighted.</summary>
    public double? ItemExtent { get; }

    /// <summary>Weights of the visible items, when the carousel is weighted.</summary>
    public IReadOnlyList<int>? FlexWeights { get; }

    /// <summary>Whether the first item can expand into the maximum weight slot.</summary>
    public bool? ConsumeMaxWeight { get; }

    public override CarouselMetrics CopyWith(
        double? minScrollExtent = null,
        double? maxScrollExtent = null,
        double? pixels = null,
        double? viewportDimension = null,
        AxisDirection? axisDirection = null,
        double? devicePixelRatio = null)
    {
        return CopyWith(
            itemExtent: null,
            flexWeights: null,
            consumeMaxWeight: null,
            minScrollExtent: minScrollExtent,
            maxScrollExtent: maxScrollExtent,
            pixels: pixels,
            viewportDimension: viewportDimension,
            axisDirection: axisDirection,
            devicePixelRatio: devicePixelRatio);
    }

    /// <summary>
    /// Dart adds the carousel fields to <c>copyWith</c>'s named arguments. C# forbids widening an
    /// override's parameter list, so they move to the front of a separate overload.
    /// </summary>
    public CarouselMetrics CopyWith(
        double? itemExtent,
        IReadOnlyList<int>? flexWeights,
        bool? consumeMaxWeight,
        double? minScrollExtent = null,
        double? maxScrollExtent = null,
        double? pixels = null,
        double? viewportDimension = null,
        AxisDirection? axisDirection = null,
        double? devicePixelRatio = null)
    {
        return new CarouselMetrics(
            minScrollExtent: minScrollExtent ?? (HasContentDimensions ? MinScrollExtent : null),
            maxScrollExtent: maxScrollExtent ?? (HasContentDimensions ? MaxScrollExtent : null),
            pixels: pixels ?? (HasPixels ? Pixels : null),
            viewportDimension: viewportDimension ?? (HasViewportDimension ? ViewportDimension : null),
            axisDirection: axisDirection ?? AxisDirection,
            itemExtent: itemExtent ?? ItemExtent,
            flexWeights: flexWeights ?? FlexWeights,
            consumeMaxWeight: consumeMaxWeight ?? ConsumeMaxWeight,
            devicePixelRatio: devicePixelRatio ?? DevicePixelRatio);
    }
}

/// <summary>Dart's <c>_CarouselPosition</c>: a scroll position measured in whole carousel items.</summary>
/// <remarks>
/// A primary constructor so the field initializers run before the base constructor absorbs
/// <paramref name="oldPosition"/>, the way Dart's initializer list runs before <c>super</c>: an
/// absorbed startup item must win over the widget's <paramref name="initialItem"/>.
/// </remarks>
public sealed class CarouselScrollPosition(
    ScrollPhysics physics,
    IScrollContext context,
    int initialItem = 0,
    double? itemExtent = null,
    IReadOnlyList<int>? flexWeights = null,
    bool consumeMaxWeight = true,
    bool infinite = false,
    int? itemCount = null,
    ScrollPosition? oldPosition = null)
    : ScrollPosition(
        physics: physics,
        context: context,
        initialPixels: null,
        oldPosition: oldPosition)
{
    private double _itemToShowOnStartup = initialItem;
    private double? _cachedItem;
    private double? _itemExtent = itemExtent;
    private IReadOnlyList<int>? _flexWeights = flexWeights;
    private bool _consumeMaxWeight = consumeMaxWeight;
    private bool _infinite = infinite;
    private int? _itemCount = itemCount;

    public int InitialItem { get; } = initialItem;

    public double? ItemExtent => _itemExtent;

    public IReadOnlyList<int>? FlexWeights => _flexWeights;

    public bool ConsumeMaxWeight => _consumeMaxWeight;

    public bool Infinite => _infinite;

    public int? ItemCount => _itemCount;

    /// <summary>The index of the item that currently occupies the leading slot.</summary>
    public int LeadingItem
    {
        get
        {
            if (!HasViewportDimension || ViewportDimension <= 0)
            {
                return 0;
            }

            int leadingItem = (int)GetItemFromPixels(Pixels, ViewportDimension);
            if (_consumeMaxWeight && _flexWeights is { Count: > 0 } weights)
            {
                leadingItem = Math.Max(leadingItem - CarouselViewState.FirstMaximumWeightIndex(weights), 0);
            }

            if (_infinite && _itemCount is > 0)
            {
                leadingItem %= _itemCount.Value;
            }

            return leadingItem;
        }
    }

    internal void SetItemCount(int? value) => _itemCount = value;

    internal void SetInfinite(bool value) => _infinite = value;

    internal void SetConsumeMaxWeight(bool value)
    {
        if (_consumeMaxWeight == value)
        {
            return;
        }

        if (HasPixels && _flexWeights is not null)
        {
            ForcePixels(GetPixelsFromItem(UpdateLeadingItem(_flexWeights, value), _flexWeights, _itemExtent));
        }

        _consumeMaxWeight = value;
    }

    internal void SetItemExtent(double? value)
    {
        if (_itemExtent == value)
        {
            return;
        }

        if (HasPixels && _itemExtent is not null && HasViewportDimension && ViewportDimension != 0.0)
        {
            double item = GetItemFromPixels(Pixels, ViewportDimension);
            ForcePixels(GetPixelsFromItem(item, _flexWeights, value));
        }

        _itemExtent = value;
    }

    internal void SetFlexWeights(IReadOnlyList<int>? value)
    {
        if (ReferenceEquals(_flexWeights, value))
        {
            return;
        }

        IReadOnlyList<int>? oldWeights = _flexWeights;
        if (HasPixels && oldWeights is not null)
        {
            ForcePixels(GetPixelsFromItem(UpdateLeadingItem(value, _consumeMaxWeight), value, _itemExtent));
        }

        _flexWeights = value;
    }

    /// <summary>Dart's <c>_updateLeadingItem</c>: the item to keep in view across a layout change.</summary>
    internal double UpdateLeadingItem(IReadOnlyList<int>? newFlexWeights, bool newConsumeMaxWeight)
    {
        double maxItem;
        if (HasPixels && _flexWeights is { Count: > 0 } weights && HasViewportDimension && ViewportDimension > 0)
        {
            double leadingItem = GetItemFromPixels(Pixels, ViewportDimension);
            maxItem = _consumeMaxWeight
                ? leadingItem
                : leadingItem + CarouselViewState.FirstMaximumWeightIndex(weights);
        }
        else
        {
            if (!newConsumeMaxWeight)
            {
                return _itemToShowOnStartup;
            }

            maxItem = _itemToShowOnStartup;
        }

        if (newFlexWeights is { Count: > 0 } && !newConsumeMaxWeight)
        {
            return maxItem - CarouselViewState.FirstMaximumWeightIndex(newFlexWeights);
        }

        return maxItem;
    }

    public double GetItemFromPixels(double pixels, double viewportDimension)
    {
        if (viewportDimension <= 0)
        {
            return 0;
        }

        double fraction = _itemExtent is { } itemExtent
            ? itemExtent / viewportDimension
            : _flexWeights is { Count: > 0 } weights ? weights[0] / (double)weights.Sum() : 0;
        double itemWidth = viewportDimension * fraction;
        if (itemWidth <= 0)
        {
            return 0;
        }

        double actual = Math.Max(0.0, pixels) / itemWidth;
        double round = Math.Round(actual, MidpointRounding.AwayFromZero);
        return Math.Abs(actual - round) < Constants.PrecisionErrorTolerance ? round : actual;
    }

    public double GetPixelsFromItem(double item, IReadOnlyList<int>? flexWeights, double? itemExtent)
    {
        if (!HasViewportDimension || ViewportDimension == 0.0)
        {
            return 0;
        }

        double fraction = itemExtent is { } extent
            ? extent / ViewportDimension
            : flexWeights is { Count: > 0 } weights ? weights[0] / (double)weights.Sum() : 0;
        return item * ViewportDimension * fraction;
    }

    /// <summary>The pixel length of one full pass through an infinite carousel's items.</summary>
    internal double GetCycleLengthInPixels()
    {
        if (_itemCount is not > 0 || !HasViewportDimension || ViewportDimension == 0)
        {
            return 0.0;
        }

        double fraction;
        if (_itemExtent is { } itemExtent)
        {
            fraction = itemExtent / ViewportDimension;
        }
        else if (_flexWeights is { Count: > 0 } weights)
        {
            fraction = weights[0] / (double)weights.Sum();
        }
        else
        {
            return 0.0;
        }

        return _itemCount.Value * ViewportDimension * fraction;
    }

    public override bool ApplyViewportDimension(double viewportDimension)
    {
        double? oldViewportDimensions = HasViewportDimension ? ViewportDimension : null;
        if (oldViewportDimensions is { } previous && previous == viewportDimension)
        {
            return true;
        }

        bool result = base.ApplyViewportDimension(viewportDimension);
        double? oldPixels = HasPixels ? Pixels : null;
        double item;
        if (oldPixels is null)
        {
            item = UpdateLeadingItem(_flexWeights, _consumeMaxWeight);
        }
        else if (oldViewportDimensions == 0.0)
        {
            item = _cachedItem ?? _itemToShowOnStartup;
        }
        else
        {
            item = GetItemFromPixels(oldPixels.Value, oldViewportDimensions ?? viewportDimension);
        }

        double newPixels = GetPixelsFromItem(item, _flexWeights, _itemExtent);
        _cachedItem = viewportDimension == 0.0 ? item : null;
        if (oldPixels is null || Math.Abs(newPixels - oldPixels.Value) > Constants.PrecisionErrorTolerance)
        {
            CorrectPixels(newPixels);
            return false;
        }

        return result;
    }

    public override bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent)
    {
        if (_infinite && HasPixels)
        {
            double cycleLength = GetCycleLengthInPixels();
            if (cycleLength > 0 && Pixels < cycleLength)
            {
                int cyclesToAdd = (int)Math.Ceiling((cycleLength - Pixels) / cycleLength);
                CorrectPixels(Pixels + (cyclesToAdd * cycleLength));
                return false;
            }
        }

        return base.ApplyContentDimensions(_infinite ? 0.0 : minScrollExtent, maxScrollExtent);
    }

    public override void Absorb(ScrollPosition other)
    {
        base.Absorb(other);
        if (other is not CarouselScrollPosition carousel)
        {
            return;
        }

        _cachedItem = carousel._cachedItem;
        _itemExtent = carousel._itemExtent;
        _itemToShowOnStartup = carousel._itemToShowOnStartup;
    }

    public override CarouselMetrics CopyWith(
        double? minScrollExtent = null,
        double? maxScrollExtent = null,
        double? pixels = null,
        double? viewportDimension = null,
        AxisDirection? axisDirection = null,
        double? devicePixelRatio = null)
    {
        return new CarouselMetrics(
            minScrollExtent: minScrollExtent ?? (HasContentDimensions ? MinScrollExtent : null),
            maxScrollExtent: maxScrollExtent ?? (HasContentDimensions ? MaxScrollExtent : null),
            pixels: pixels ?? (HasPixels ? Pixels : null),
            viewportDimension: viewportDimension ?? (HasViewportDimension ? ViewportDimension : null),
            axisDirection: axisDirection ?? AxisDirection,
            itemExtent: _itemExtent,
            flexWeights: _flexWeights,
            consumeMaxWeight: _consumeMaxWeight,
            devicePixelRatio: DevicePixelRatio);
    }
}

/// <summary>A <see cref="ScrollController"/> that addresses a <see cref="CarouselView"/> by item.</summary>
public sealed class CarouselController : ScrollController
{
    private CarouselViewState? _carouselState;

    public CarouselController(int initialItem = 0)
    {
        InitialItem = initialItem;
    }

    /// <summary>The item to show when the carousel is first built.</summary>
    public int InitialItem { get; }

    /// <summary>The index of the item that currently occupies the leading slot.</summary>
    public int LeadingItem
    {
        get
        {
            if (Positions.Count == 0)
            {
                throw new InvalidOperationException(
                    "CarouselController.leadingItem cannot be accessed before a CarouselView is built with it.");
            }

            if (Positions.Count > 1)
            {
                throw new InvalidOperationException(
                    "CarouselController.leadingItem cannot be read when multiple CarouselViews are "
                    + "attached to the same controller.");
            }

            return ((CarouselScrollPosition)Positions[0]).LeadingItem;
        }
    }

    /// <summary>Animates the controlled carousel so that <paramref name="index"/> leads it.</summary>
    public Task AnimateToItem(int index, TimeSpan? duration = null, Curve? curve = null)
    {
        if (!HasClients || _carouselState is null)
        {
            return Task.CompletedTask;
        }

        bool hasFlexWeights = _carouselState.FlexWeights is { Count: > 0 };
        index = ClampToItemCount(index);

        List<Task> animations = [];
        foreach (ScrollPosition position in Positions)
        {
            if (position is not CarouselScrollPosition carouselPosition)
            {
                continue;
            }

            animations.Add(carouselPosition.AnimateTo(
                GetTargetOffset(carouselPosition, index, hasFlexWeights),
                duration ?? TimeSpan.FromMilliseconds(300),
                curve ?? Curves.Ease));
        }

        return Task.WhenAll(animations);
    }

    /// <summary>Jumps the controlled carousel so that <paramref name="index"/> leads it.</summary>
    public void JumpToItem(int index)
    {
        if (!HasClients || _carouselState is null)
        {
            return;
        }

        bool hasFlexWeights = _carouselState.FlexWeights is { Count: > 0 };
        index = ClampToItemCount(index);
        foreach (ScrollPosition position in Positions)
        {
            if (position is CarouselScrollPosition carouselPosition)
            {
                carouselPosition.JumpTo(GetTargetOffset(carouselPosition, index, hasFlexWeights));
            }
        }
    }

    public override ScrollPosition CreateScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        ScrollPosition? oldPosition)
    {
        return new CarouselScrollPosition(
            physics: physics,
            context: context,
            initialItem: InitialItem,
            itemExtent: _carouselState?.EffectiveItemExtent,
            flexWeights: _carouselState?.FlexWeights,
            consumeMaxWeight: _carouselState?.ConsumeMaxWeight ?? true,
            infinite: _carouselState?.Current.Infinite ?? false,
            itemCount: GetItemCount(),
            oldPosition: oldPosition);
    }

    internal override void Attach(ScrollPosition position)
    {
        base.Attach(position);
        if (position is not CarouselScrollPosition carouselPosition || _carouselState is null)
        {
            return;
        }

        carouselPosition.SetFlexWeights(_carouselState.FlexWeights);
        carouselPosition.SetItemExtent(_carouselState.EffectiveItemExtent);
        carouselPosition.SetConsumeMaxWeight(_carouselState.ConsumeMaxWeight);
        carouselPosition.SetInfinite(_carouselState.Current.Infinite);
        carouselPosition.SetItemCount(GetItemCount());
    }

    internal void AttachCarousel(CarouselViewState state) => _carouselState = state;

    internal void DetachCarousel(CarouselViewState state)
    {
        if (ReferenceEquals(_carouselState, state))
        {
            _carouselState = null;
        }
    }

    private int? GetItemCount() => _carouselState?.ItemCount;

    private int ClampToItemCount(int index)
    {
        int? itemCount = GetItemCount();
        if (_carouselState?.Current.ItemBuilder is not null)
        {
            return itemCount is not null ? Math.Clamp(index, 0, Math.Max(0, itemCount.Value - 1)) : 0;
        }

        return itemCount is > 0 ? Math.Clamp(index, 0, itemCount.Value - 1) : 0;
    }

    private double GetTargetOffset(CarouselScrollPosition position, int index, bool hasFlexWeights)
    {
        if (!hasFlexWeights)
        {
            double target = index * (_carouselState?.EffectiveItemExtent ?? 0);
            return _carouselState?.Current.Infinite == true ? AdjustForInfiniteCycle(position, target) : target;
        }

        IReadOnlyList<int> weights = _carouselState!.FlexWeights!;
        int totalWeight = weights.Sum();
        double dimension = position.ViewportDimension;
        int leadingIndex = _carouselState.ConsumeMaxWeight
            ? index
            : index - CarouselViewState.FirstMaximumWeightIndex(weights);
        leadingIndex = ClampToItemCount(leadingIndex);
        double targetInFirstCycle = dimension * (weights[0] / (double)totalWeight) * leadingIndex;
        return _carouselState.Current.Infinite
            ? AdjustForInfiniteCycle(position, targetInFirstCycle)
            : targetInFirstCycle;
    }

    /// <summary>
    /// Maps an item offset inside the first cycle onto the cycle the carousel is currently in, always
    /// moving forwards so that an infinite carousel never scrolls backwards to reach a nearby item.
    /// </summary>
    private static double AdjustForInfiniteCycle(CarouselScrollPosition position, double targetInFirstCycle)
    {
        double cycleLength = position.GetCycleLengthInPixels();
        if (cycleLength <= 0)
        {
            return targetInFirstCycle;
        }

        double currentPixels = position.Pixels;
        double currentCycleStart = Math.Floor(currentPixels / cycleLength) * cycleLength;
        double sameCycleTarget = currentCycleStart + targetInFirstCycle;
        return sameCycleTarget >= currentPixels ? sameCycleTarget : sameCycleTarget + cycleLength;
    }
}
