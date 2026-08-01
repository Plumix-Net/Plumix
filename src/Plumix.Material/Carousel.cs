using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/carousel.dart
// Dart parity source: flutter/packages/flutter/lib/src/material/carousel_theme.dart

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

public sealed class CarouselViewTheme : InheritedWidget
{
    public CarouselViewTheme(CarouselViewThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public CarouselViewThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => !Equals(Data, ((CarouselViewTheme)oldWidget).Data);

    public static CarouselViewThemeData Of(BuildContext context) =>
        context.DependOnInherited<CarouselViewTheme>()?.Data ?? Theme.Of(context).CarouselViewTheme;
}

public sealed class CarouselController : ScrollController
{
    private CarouselViewState? _carouselState;
    private AnimationController? _animationController;
    private double _animationStart;
    private double _animationTarget;
    private double? _itemExtent;
    private IReadOnlyList<int>? _flexWeights;
    private bool _consumeMaxWeight = true;
    private bool _infinite;
    private int? _itemCount;

    public CarouselController(int initialItem = 0) : base()
    {
        if (initialItem < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialItem));
        }

        InitialItem = initialItem;
    }

    public int InitialItem { get; }

    public int LeadingItem => PrimaryPosition is CarouselScrollPosition position
        ? position.LeadingItem
        : throw new InvalidOperationException("CarouselController.leadingItem cannot be accessed before a CarouselView is attached.");

    public override ScrollPosition CreateScrollPosition(ScrollPhysics? physics = null)
    {
        return new CarouselScrollPosition(
            initialItem: InitialItem,
            itemExtent: _itemExtent,
            flexWeights: _flexWeights,
            consumeMaxWeight: _consumeMaxWeight,
            infinite: _infinite,
            itemCount: _itemCount,
            physics: physics ?? Physics);
    }

    public void JumpToItem(int index) => _carouselState?.JumpToItem(index);

    public void AnimateToItem(int index, TimeSpan? duration = null, Curve? curve = null)
    {
        double? target = _carouselState?.GetTargetPixels(index);
        if (!target.HasValue)
        {
            return;
        }

        AnimateTo(target.Value, duration ?? TimeSpan.FromMilliseconds(300), curve ?? Curves.Ease);
    }

    public override void Dispose()
    {
        StopAnimation();
        base.Dispose();
    }

    internal void Attach(CarouselViewState state)
    {
        _carouselState = state;
    }

    internal void Detach(CarouselViewState state)
    {
        if (ReferenceEquals(_carouselState, state))
        {
            _carouselState = null;
        }
    }

    internal void Configure(
        double? itemExtent,
        IReadOnlyList<int>? flexWeights,
        bool consumeMaxWeight,
        bool infinite,
        int? itemCount)
    {
        _itemExtent = itemExtent;
        _flexWeights = flexWeights;
        _consumeMaxWeight = consumeMaxWeight;
        _infinite = infinite;
        _itemCount = itemCount;

        if (PrimaryPosition is CarouselScrollPosition position)
        {
            position.Configure(itemExtent, flexWeights, consumeMaxWeight, infinite, itemCount);
        }
    }

    private void AnimateTo(double target, TimeSpan duration, Curve curve)
    {
        if (duration <= TimeSpan.Zero || Math.Abs(Offset - target) < 0.0001)
        {
            JumpTo(target);
            return;
        }

        StopAnimation();
        _animationStart = Offset;
        _animationTarget = target;
        _animationController = new AnimationController(duration)
        {
            Curve = curve,
        };
        _animationController.Changed += HandleAnimationChanged;
        _animationController.Completed += HandleAnimationCompleted;
        _animationController.Forward(0);
    }

    private void HandleAnimationChanged()
    {
        if (_animationController is not null)
        {
            JumpTo(_animationStart + ((_animationTarget - _animationStart) * _animationController.Evaluate()));
        }
    }

    private void HandleAnimationCompleted()
    {
        JumpTo(_animationTarget);
        StopAnimation();
    }

    private void StopAnimation()
    {
        if (_animationController is null)
        {
            return;
        }

        _animationController.Changed -= HandleAnimationChanged;
        _animationController.Completed -= HandleAnimationCompleted;
        _animationController.Dispose();
        _animationController = null;
    }
}

public sealed class CarouselScrollPhysics : ScrollPhysics
{
    public CarouselScrollPhysics(ScrollPhysics? parent = null) : base(parent)
    {
    }

    public override Simulation? CreateBallisticSimulation(IScrollMetrics position, double velocity)
    {
        if (position is not CarouselScrollPosition carouselPosition)
        {
            throw new InvalidOperationException("CarouselScrollPhysics requires CarouselScrollPosition.");
        }

        if ((velocity <= 0 && carouselPosition.Pixels <= carouselPosition.MinScrollExtent)
            || (velocity >= 0 && carouselPosition.Pixels >= carouselPosition.MaxScrollExtent))
        {
            return base.CreateBallisticSimulation(position, velocity);
        }

        double itemExtent = carouselPosition.ScrollExtentPerItem;
        if (itemExtent <= 0)
        {
            return null;
        }

        double item = Math.Max(0, carouselPosition.Pixels) / itemExtent;
        if (velocity < -20)
        {
            item -= 0.5;
        }
        else if (velocity > 20)
        {
            item += 0.5;
        }

        double target = Math.Clamp(
            Math.Round(item, MidpointRounding.AwayFromZero) * itemExtent,
            carouselPosition.MinScrollExtent,
            carouselPosition.MaxScrollExtent);
        return Math.Abs(target - carouselPosition.Pixels) < 0.0001
            ? null
            : new FrictionSimulation(8, carouselPosition.Pixels, (target - carouselPosition.Pixels) * 8);
    }
}

public sealed class CarouselScrollPosition : ScrollPosition
{
    private double? _itemExtent;
    private IReadOnlyList<int>? _flexWeights;
    private bool _consumeMaxWeight;
    private bool _infinite;
    private int? _itemCount;
    private double? _cachedItem;

    public CarouselScrollPosition(
        int initialItem,
        double? itemExtent,
        IReadOnlyList<int>? flexWeights,
        bool consumeMaxWeight,
        bool infinite,
        int? itemCount,
        ScrollPhysics physics) : base(physics: physics)
    {
        if ((itemExtent is null) == (flexWeights is null))
        {
            throw new ArgumentException("Exactly one of itemExtent and flexWeights must be supplied.");
        }

        InitialItem = initialItem;
        _itemExtent = itemExtent;
        _flexWeights = flexWeights;
        _consumeMaxWeight = consumeMaxWeight;
        _infinite = infinite;
        _itemCount = itemCount;
    }

    public int InitialItem { get; }

    public double? ItemExtent => _itemExtent;

    public IReadOnlyList<int>? FlexWeights => _flexWeights;

    public bool ConsumeMaxWeight => _consumeMaxWeight;

    public bool Infinite => _infinite;

    public int LeadingItem
    {
        get
        {
            if (ViewportDimension <= 0)
            {
                return 0;
            }

            int item = (int)Math.Floor(GetItemFromPixels(Pixels, ViewportDimension));
            if (_consumeMaxWeight && _flexWeights is not null)
            {
                item = Math.Max(item - FirstMaximumWeightIndex(_flexWeights), 0);
            }

            return _infinite && _itemCount is > 0 ? item % _itemCount.Value : item;
        }
    }

    internal double ScrollExtentPerItem => GetScrollExtentPerItem(ViewportDimension);

    internal void Configure(
        double? itemExtent,
        IReadOnlyList<int>? flexWeights,
        bool consumeMaxWeight,
        bool infinite,
        int? itemCount)
    {
        double item = ViewportDimension > 0 ? GetItemFromPixels(Pixels, ViewportDimension) : InitialItem;
        _itemExtent = itemExtent;
        _flexWeights = flexWeights;
        _consumeMaxWeight = consumeMaxWeight;
        _infinite = infinite;
        _itemCount = itemCount;
        CorrectPixels(GetPixelsFromItem(item, ViewportDimension));
    }

    public override bool ApplyViewportDimension(double viewportDimension)
    {
        double oldViewportDimension = ViewportDimension;
        double item = oldViewportDimension > 0
            ? GetItemFromPixels(Pixels, oldViewportDimension)
            : InitialLeadingItem();

        bool changed = base.ApplyViewportDimension(viewportDimension);
        _cachedItem = viewportDimension == 0 ? item : null;
        return CorrectPixels(GetPixelsFromItem(_cachedItem ?? item, viewportDimension)) || changed;
    }

    public override bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent)
    {
        if (_infinite && Pixels < GetCycleLengthInPixels())
        {
            double cycleLength = GetCycleLengthInPixels();
            if (cycleLength > 0)
            {
                double cycles = Math.Ceiling((cycleLength - Pixels) / cycleLength);
                CorrectPixels(Pixels + cycles * cycleLength);
            }
        }

        return base.ApplyContentDimensions(_infinite ? 0 : minScrollExtent, maxScrollExtent);
    }

    internal double GetPixelsForItem(double item) => GetPixelsFromItem(item, ViewportDimension);

    private int InitialLeadingItem()
    {
        return _flexWeights is null
            ? InitialItem
            : Math.Max(InitialItem - FirstMaximumWeightIndex(_flexWeights), 0);
    }

    private double GetItemFromPixels(double pixels, double viewportDimension)
    {
        double extent = GetScrollExtentPerItem(viewportDimension);
        return extent <= 0 ? 0 : Math.Max(0, pixels) / extent;
    }

    private double GetPixelsFromItem(double item, double viewportDimension)
    {
        return item * GetScrollExtentPerItem(viewportDimension);
    }

    private double GetScrollExtentPerItem(double viewportDimension)
    {
        if (viewportDimension <= 0)
        {
            return 0;
        }

        if (_itemExtent.HasValue)
        {
            return Math.Min(_itemExtent.Value, viewportDimension);
        }

        return _flexWeights is null ? 0 : viewportDimension * _flexWeights[0] / _flexWeights.Sum();
    }

    private double GetCycleLengthInPixels()
    {
        return _itemCount is > 0 ? _itemCount.Value * GetScrollExtentPerItem(ViewportDimension) : 0;
    }

    private static int FirstMaximumWeightIndex(IReadOnlyList<int> weights)
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
}

public sealed class CarouselView : StatefulWidget
{
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
        if ((itemExtent is null) == (flexWeights is null))
        {
            throw new ArgumentException("Exactly one of itemExtent and flexWeights must be supplied.");
        }

        if (itemExtent is <= 0 or double.NaN or double.PositiveInfinity or double.NegativeInfinity)
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent));
        }

        if (flexWeights is not null && (flexWeights.Count == 0 || flexWeights.Any(weight => weight <= 0)))
        {
            throw new ArgumentOutOfRangeException(nameof(flexWeights));
        }

        if (!double.IsFinite(shrinkExtent) || shrinkExtent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shrinkExtent));
        }

        if (elevation is < 0 or double.NaN or double.PositiveInfinity or double.NegativeInfinity)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation));
        }

        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount));
        }

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
        return new CarouselView(null, flexWeights, children, null, null, padding, backgroundColor, elevation, shape, itemClipBehavior, overlayColor, itemSnapping, shrinkExtent, controller, scrollDirection, reverse, consumeMaxWeight, onTap, enableSplash, infinite, onIndexChanged, key);
    }

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
        return new CarouselView(itemExtent, null, [], itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder)), itemCount, padding, backgroundColor, elevation, shape, itemClipBehavior, overlayColor, itemSnapping, shrinkExtent, controller, scrollDirection, reverse, true, onTap, enableSplash, infinite, onIndexChanged, key);
    }

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
        return new CarouselView(null, flexWeights, [], itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder)), itemCount, padding, backgroundColor, elevation, shape, itemClipBehavior, overlayColor, itemSnapping, shrinkExtent, controller, scrollDirection, reverse, consumeMaxWeight, onTap, enableSplash, infinite, onIndexChanged, key);
    }

    public override State CreateState() => new CarouselViewState();
}

public sealed class CarouselViewState : State
{
    private CarouselController? _internalController;
    private int _lastReportedLeadingItem;

    private CarouselView Current => (CarouselView)StateWidget;

    private CarouselController Controller => Current.Controller ?? (_internalController ??= new CarouselController());

    public override void InitState()
    {
        ConfigureController();
        Controller.Attach(this);
        Controller.AddListener(HandleScroll);
        _lastReportedLeadingItem = InitialLeadingItem();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        CarouselView oldCarousel = (CarouselView)oldWidget;
        if (!ReferenceEquals(oldCarousel.Controller, Current.Controller))
        {
            CarouselController oldController = oldCarousel.Controller ?? _internalController!;
            oldController.RemoveListener(HandleScroll);
            oldController.Detach(this);
            if (Current.Controller is not null)
            {
                _internalController?.Dispose();
                _internalController = null;
            }

            ConfigureController();
            Controller.Attach(this);
            Controller.AddListener(HandleScroll);
        }
        else
        {
            ConfigureController();
        }
    }

    public override void Dispose()
    {
        Controller.RemoveListener(HandleScroll);
        Controller.Detach(this);
        _internalController?.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        ConfigureController();
        int? childCount = Current.Infinite ? null : (Current.ItemBuilder is null ? Current.Children.Count : Current.ItemCount);
        SliverChildDelegate childDelegate = new CarouselChildDelegate(this, childCount);
        SliverVariableExtentLayout layout = Current.ItemExtent.HasValue
            ? new FixedCarouselLayout(Current.ItemExtent.Value, Current.ShrinkExtent, Current.Infinite)
            : new WeightedCarouselLayout(Current.FlexWeights!, Current.ShrinkExtent, Current.ConsumeMaxWeight, Current.Infinite);

        return new Scrollable(
            slivers: [new SliverVariableExtentList(childDelegate, layout)],
            axis: Current.ScrollDirection,
            reverse: Current.Reverse,
            controller: Controller,
            physics: Current.ItemSnapping ? new CarouselScrollPhysics() : new ClampingScrollPhysics(),
            cacheExtent: 0,
            cacheExtentStyle: CacheExtentStyle.Viewport);
    }

    internal Widget? BuildItem(BuildContext itemContext, int index)
    {
        int itemIndex = index;
        int? count = Current.ItemBuilder is null ? Current.Children.Count : Current.ItemCount;
        if (Current.Infinite && count is > 0)
        {
            itemIndex %= count.Value;
        }

        Widget? contents = Current.ItemBuilder is null
            ? itemIndex >= 0 && itemIndex < Current.Children.Count ? Current.Children[itemIndex] : null
            : Current.ItemBuilder(itemContext, itemIndex);
        if (contents is null)
        {
            return null;
        }

        CarouselViewThemeData carouselTheme = CarouselViewTheme.Of(itemContext);
        ThemeData theme = Theme.Of(itemContext);
        Thickness padding = Current.Padding ?? carouselTheme.Padding ?? new Thickness(4);
        Color backgroundColor = Current.BackgroundColor ?? carouselTheme.BackgroundColor ?? theme.SurfaceColor;
        double elevation = Current.Elevation ?? carouselTheme.Elevation ?? 0;
        ShapeBorder shape = Current.Shape ?? carouselTheme.Shape ?? ShapeBorder.RoundedRectangle(28);
        Clip clipBehavior = Current.ItemClipBehavior ?? carouselTheme.ItemClipBehavior ?? Clip.AntiAlias;
        MaterialStateProperty<Color?> overlayColor = Current.OverlayColor
            ?? carouselTheme.OverlayColor
            ?? DefaultOverlayColor(theme.OnSurfaceColor);

        if (Current.EnableSplash)
        {
            contents = new Stack(
                fit: StackFit.Expand,
                children:
                [
                    contents,
                    new InkWell(
                        onTap: Current.OnTap is null ? null : () => Current.OnTap(itemIndex),
                        overlayColor: overlayColor),
                ]);
        }
        else if (Current.OnTap is not null)
        {
            contents = new GestureDetector(onTap: () => Current.OnTap(itemIndex), child: contents);
        }

        Widget material = new DecoratedBox(
            new BoxDecoration(
                Color: backgroundColor,
                Border: shape.Side,
                BorderRadius: shape.BorderRadius,
                Shape: shape.Shape),
            contents);
        if (clipBehavior != Clip.None)
        {
            material = new ClipRRect(shape.BorderRadius, material);
        }

        _ = elevation;
        return new Padding(padding, material);
    }

    internal void JumpToItem(int index)
    {
        double? target = GetTargetPixels(index);
        if (target.HasValue)
        {
            Controller.JumpTo(target.Value);
        }
    }

    internal double? GetTargetPixels(int index)
    {
        CarouselScrollPosition? position = Controller.PrimaryPosition as CarouselScrollPosition;
        return position is null
            ? null
            : position.GetPixelsForItem(TargetItemForIndex(ClampItemIndex(index)));
    }

    private void ConfigureController()
    {
        int? count = Current.ItemBuilder is null ? Current.Children.Count : Current.ItemCount;
        Controller.Configure(Current.ItemExtent, Current.FlexWeights, Current.ConsumeMaxWeight, Current.Infinite, count);
    }

    private void HandleScroll()
    {
        if (Current.OnIndexChanged is null || Controller.PrimaryPosition is not CarouselScrollPosition position)
        {
            return;
        }

        int leadingItem = position.LeadingItem;
        if (leadingItem != _lastReportedLeadingItem)
        {
            _lastReportedLeadingItem = leadingItem;
            Current.OnIndexChanged(leadingItem);
        }
    }

    private int InitialLeadingItem()
    {
        return Current.FlexWeights is null
            ? Controller.InitialItem
            : Math.Max(Controller.InitialItem - FirstMaximumWeightIndex(Current.FlexWeights), 0);
    }

    private int ClampItemIndex(int index)
    {
        int? count = Current.ItemBuilder is null ? Current.Children.Count : Current.ItemCount;
        if (Current.Infinite)
        {
            return Math.Max(0, index);
        }

        if (count is null)
        {
            return Current.ItemBuilder is null ? Math.Max(0, index) : 0;
        }

        return count.Value == 0 ? 0 : Math.Clamp(index, 0, count.Value - 1);
    }

    private double TargetItemForIndex(int index)
    {
        if (Current.FlexWeights is null)
        {
            return index;
        }

        return Current.ConsumeMaxWeight ? index : index - FirstMaximumWeightIndex(Current.FlexWeights);
    }

    private static int FirstMaximumWeightIndex(IReadOnlyList<int> weights)
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

    private static MaterialStateProperty<Color?> DefaultOverlayColor(Color onSurfaceColor)
    {
        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            double opacity = states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused) ? 0.1
                : states.HasFlag(MaterialState.Hovered) ? 0.08
                : 0;
            return opacity == 0 ? null : Color.FromArgb((byte)Math.Round(onSurfaceColor.A * opacity), onSurfaceColor.R, onSurfaceColor.G, onSurfaceColor.B);
        });
    }

    private sealed class CarouselChildDelegate : SliverChildDelegate
    {
        private readonly CarouselViewState _state;
        private readonly int? _childCount;

        public CarouselChildDelegate(CarouselViewState state, int? childCount)
        {
            _state = state;
            _childCount = childCount;
        }

        public override int? EstimatedChildCount => _childCount;

        public override Widget? Build(BuildContext context, int index)
        {
            if (index < 0 || (_childCount.HasValue && index >= _childCount.Value))
            {
                return null;
            }

            Widget? child = _state.BuildItem(context, index);
            return child is null ? null : new AutomaticKeepAlive(child);
        }
    }
}

internal sealed class FixedCarouselLayout : SliverVariableExtentLayout
{
    private readonly double _maxExtent;
    private readonly double _minExtent;
    private readonly bool _infinite;

    public FixedCarouselLayout(double maxExtent, double minExtent, bool infinite)
    {
        _maxExtent = maxExtent;
        _minExtent = minExtent;
        _infinite = infinite;
    }

    public override int GetMinChildIndexForScrollOffset(SliverConstraints constraints, double scrollOffset)
    {
        double extent = EffectiveMaxExtent(constraints);
        return extent <= 0 ? 0 : Math.Max((int)Math.Floor(scrollOffset / extent), 0);
    }

    public override int GetMaxChildIndexForScrollOffset(SliverConstraints constraints, double scrollOffset)
    {
        double extent = EffectiveMaxExtent(constraints);
        if (extent <= 0)
        {
            return 0;
        }

        double actual = scrollOffset / extent - 1;
        int rounded = (int)Math.Round(actual);
        return Math.Abs(actual - rounded) < 0.0001 ? Math.Max(rounded, 0) : Math.Max((int)Math.Ceiling(actual), 0);
    }

    public override double GetChildMainAxisExtent(SliverConstraints constraints, int index)
    {
        double maxExtent = EffectiveMaxExtent(constraints);
        if (maxExtent <= 0)
        {
            return 0;
        }

        int firstVisibleIndex = GetMinChildIndexForScrollOffset(constraints, constraints.ScrollOffset);
        double offscreenExtent = constraints.ScrollOffset - firstVisibleIndex * maxExtent;
        double effectiveMinExtent = Math.Max(Remainder(constraints.RemainingPaintExtent, maxExtent), _minExtent);
        if (index == firstVisibleIndex)
        {
            return Math.Max(maxExtent - offscreenExtent, effectiveMinExtent);
        }

        int lastVisibleIndex = GetMaxChildIndexForScrollOffset(constraints, constraints.ScrollOffset + constraints.RemainingPaintExtent);
        if (index == lastVisibleIndex)
        {
            return Math.Clamp(constraints.ScrollOffset + constraints.RemainingPaintExtent - maxExtent * index, effectiveMinExtent, maxExtent);
        }

        return maxExtent;
    }

    public override double GetChildLayoutOffset(SliverConstraints constraints, int index)
    {
        double maxExtent = EffectiveMaxExtent(constraints);
        if (maxExtent <= 0)
        {
            return 0;
        }

        int firstVisibleIndex = GetMinChildIndexForScrollOffset(constraints, constraints.ScrollOffset);
        if (index == firstVisibleIndex)
        {
            double effectiveMinExtent = Math.Max(Remainder(constraints.RemainingPaintExtent, maxExtent), _minExtent);
            return GetChildMainAxisExtent(constraints, index) <= effectiveMinExtent
                ? maxExtent * index - effectiveMinExtent + maxExtent
                : constraints.ScrollOffset;
        }

        return maxExtent * index;
    }

    public override double ComputeMaxScrollOffset(SliverConstraints constraints, int? childCount)
    {
        return _infinite ? double.PositiveInfinity : (childCount ?? 0) * EffectiveMaxExtent(constraints);
    }

    private double EffectiveMaxExtent(SliverConstraints constraints) => Math.Min(_maxExtent, constraints.ViewportMainAxisExtent);

    private static double Remainder(double value, double divisor) => divisor <= 0 ? 0 : value - Math.Floor(value / divisor) * divisor;
}

internal sealed class WeightedCarouselLayout : SliverVariableExtentLayout
{
    private readonly IReadOnlyList<int> _weights;
    private readonly double _shrinkExtent;
    private readonly bool _consumeMaxWeight;
    private readonly bool _infinite;

    public WeightedCarouselLayout(IReadOnlyList<int> weights, double shrinkExtent, bool consumeMaxWeight, bool infinite)
    {
        _weights = weights;
        _shrinkExtent = shrinkExtent;
        _consumeMaxWeight = consumeMaxWeight;
        _infinite = infinite;
    }

    public override int GetMinChildIndexForScrollOffset(SliverConstraints constraints, double scrollOffset) => Math.Max(FirstVisibleItemIndex(constraints), 0);

    public override int GetMaxChildIndexForScrollOffset(SliverConstraints constraints, double scrollOffset)
    {
        double totalExtent = DistanceToLeadingEdge(constraints);
        int index = FirstVisibleItemIndex(constraints) + 1;
        int upperBound = FirstVisibleItemIndex(constraints) + (int)Math.Ceiling(constraints.RemainingPaintExtent / Math.Max(MinChildExtent(constraints), 1));
        while (totalExtent < constraints.RemainingPaintExtent && index <= upperBound)
        {
            totalExtent += GetChildMainAxisExtent(constraints, index);
            if (totalExtent >= constraints.RemainingPaintExtent)
            {
                return index;
            }

            index += 1;
        }

        return Math.Max(index, 0);
    }

    public override double GetChildMainAxisExtent(SliverConstraints constraints, int index)
    {
        if (constraints.ViewportMainAxisExtent <= 0)
        {
            return 0;
        }

        int firstIndex = FirstVisibleItemIndex(constraints);
        if (index == firstIndex)
        {
            return Math.Max(DistanceToLeadingEdge(constraints), EffectiveShrinkExtent(constraints));
        }

        if (index > firstIndex && index - firstIndex + 1 <= _weights.Count)
        {
            int relativeIndex = index - firstIndex;
            double extent = ExtentUnit(constraints) * _weights[relativeIndex];
            double progress = FirstVisibleItemOffscreenExtent(constraints) / FirstChildExtent(constraints);
            double finalIncrease = (_weights[relativeIndex - 1] - _weights[relativeIndex]) / (double)_weights.Max();
            return extent + finalIncrease * progress * MaxChildExtent(constraints);
        }

        if (index > firstIndex)
        {
            double visibleExtent = DistanceToLeadingEdge(constraints);
            for (int item = firstIndex + 1; item < index; item += 1)
            {
                visibleExtent += GetChildMainAxisExtent(constraints, item);
            }

            return Math.Max(constraints.RemainingPaintExtent - visibleExtent, EffectiveShrinkExtent(constraints));
        }

        return Math.Max(MinChildExtent(constraints), EffectiveShrinkExtent(constraints));
    }

    public override double GetChildLayoutOffset(SliverConstraints constraints, int index)
    {
        int firstIndex = FirstVisibleItemIndex(constraints);
        if (index == firstIndex)
        {
            return DistanceToLeadingEdge(constraints) <= EffectiveShrinkExtent(constraints)
                ? constraints.ScrollOffset - EffectiveShrinkExtent(constraints) + DistanceToLeadingEdge(constraints)
                : constraints.ScrollOffset;
        }

        double visibleExtent = DistanceToLeadingEdge(constraints);
        for (int item = firstIndex + 1; item < index; item += 1)
        {
            visibleExtent += GetChildMainAxisExtent(constraints, item);
        }

        return constraints.ScrollOffset + visibleExtent;
    }

    public override double ComputeMaxScrollOffset(SliverConstraints constraints, int? childCount)
    {
        return _infinite ? double.PositiveInfinity : (childCount ?? 0) * MaxChildExtent(constraints);
    }

    private int FirstVisibleItemIndex(SliverConstraints constraints)
    {
        double firstExtent = FirstChildExtent(constraints);
        if (firstExtent <= 0)
        {
            return 0;
        }

        int smallerWeightCount = _consumeMaxWeight ? FirstMaximumWeightIndex() : 0;
        double actual = constraints.ScrollOffset / firstExtent;
        int rounded = (int)Math.Round(actual);
        int item = Math.Abs(actual - rounded) < 0.0001 ? rounded : (int)Math.Floor(actual);
        return _consumeMaxWeight ? item - smallerWeightCount : item;
    }

    private double FirstVisibleItemOffscreenExtent(SliverConstraints constraints)
    {
        double firstExtent = FirstChildExtent(constraints);
        if (firstExtent <= 0)
        {
            return 0;
        }

        double actual = constraints.ScrollOffset / firstExtent;
        int rounded = (int)Math.Round(actual);
        int item = Math.Abs(actual - rounded) < 0.0001 ? rounded : (int)Math.Floor(actual);
        return constraints.ScrollOffset - item * firstExtent;
    }

    private double DistanceToLeadingEdge(SliverConstraints constraints) => FirstChildExtent(constraints) - FirstVisibleItemOffscreenExtent(constraints);

    private double ExtentUnit(SliverConstraints constraints) => constraints.ViewportMainAxisExtent / _weights.Sum();

    private double FirstChildExtent(SliverConstraints constraints) => _weights[0] * ExtentUnit(constraints);

    private double MaxChildExtent(SliverConstraints constraints) => _weights.Max() * ExtentUnit(constraints);

    private double MinChildExtent(SliverConstraints constraints) => _weights.Min() * ExtentUnit(constraints);

    private double EffectiveShrinkExtent(SliverConstraints constraints) => Math.Clamp(_shrinkExtent, 0, MinChildExtent(constraints));

    private int FirstMaximumWeightIndex()
    {
        int maximum = _weights.Max();
        for (int index = 0; index < _weights.Count; index += 1)
        {
            if (_weights[index] == maximum)
            {
                return index;
            }
        }

        return 0;
    }
}
