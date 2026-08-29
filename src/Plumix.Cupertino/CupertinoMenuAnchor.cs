using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/menu_anchor.dart

public interface CupertinoMenuEntry
{
    bool HasLeading(BuildContext context);

    bool IsDivider { get; }
}

public delegate void CupertinoMenuAnimationStatusChangedCallback(AnimationStatus status);

public sealed class CupertinoMenuAnchor : StatefulWidget
{
    public CupertinoMenuAnchor(
        IReadOnlyList<Widget> menuChildren,
        MenuController? controller = null,
        Action? onOpen = null,
        Action? onClose = null,
        CupertinoMenuAnimationStatusChangedCallback? onAnimationStatusChanged = null,
        BoxConstraints? constraints = null,
        bool constrainCrossAxis = false,
        bool consumeOutsideTaps = false,
        bool enableSwipe = true,
        bool enableLongPressToOpen = false,
        bool useRootOverlay = false,
        EdgeInsetsGeometry? overlayPadding = null,
        RawMenuAnchorChildBuilder? builder = null,
        Widget? child = null,
        FocusNode? childFocusNode = null,
        Key? key = null) : base(key)
    {
        if (enableLongPressToOpen && !enableSwipe)
        {
            throw new ArgumentException(
                "enableLongPressToOpen cannot be true if enableSwipe is false.",
                nameof(enableLongPressToOpen));
        }

        MenuChildren = menuChildren ?? throw new ArgumentNullException(nameof(menuChildren));
        Controller = controller;
        OnOpen = onOpen;
        OnClose = onClose;
        OnAnimationStatusChanged = onAnimationStatusChanged;
        Constraints = constraints;
        ConstrainCrossAxis = constrainCrossAxis;
        ConsumeOutsideTaps = consumeOutsideTaps;
        EnableSwipe = enableSwipe;
        EnableLongPressToOpen = enableLongPressToOpen;
        UseRootOverlay = useRootOverlay;
        OverlayPadding = overlayPadding ?? EdgeInsetsGeometry.All(8.0);
        Builder = builder;
        Child = child;
        ChildFocusNode = childFocusNode;
    }

    public IReadOnlyList<Widget> MenuChildren { get; }

    public MenuController? Controller { get; }

    public Action? OnOpen { get; }

    public Action? OnClose { get; }

    public CupertinoMenuAnimationStatusChangedCallback? OnAnimationStatusChanged { get; }

    public BoxConstraints? Constraints { get; }

    public bool ConstrainCrossAxis { get; }

    public bool ConsumeOutsideTaps { get; }

    public bool EnableSwipe { get; }

    public bool EnableLongPressToOpen { get; }

    public bool UseRootOverlay { get; }

    public EdgeInsetsGeometry OverlayPadding { get; }

    public RawMenuAnchorChildBuilder? Builder { get; }

    public Widget? Child { get; }

    public FocusNode? ChildFocusNode { get; }

    public static bool? MaybeHasLeadingOf(BuildContext context) =>
        context.DependOnInherited<CupertinoMenuAnchorScope>()?.HasLeading;

    public override State CreateState() => new CupertinoMenuAnchorState();
}

public sealed class CupertinoMenuAnchorState : State, WidgetsBindingObserver
{
    private static readonly SpringDescription ForwardSpring = SpringDescription.WithDurationAndBounce(
        TimeSpan.FromMilliseconds(337),
        bounce: 0.2);

    private static readonly SpringDescription ReverseSpring = SpringDescription.WithDurationAndBounce(
        TimeSpan.FromMilliseconds(409));

    private static readonly Tolerance SpringTolerance = new(velocity: 0.1);

    private readonly FocusScopeNode _menuScopeNode = new();
    private readonly ValueNotifier<double> _swipeScale = new(1.0);
    private AnimationController _animationController = null!;
    private Ticker? _swipeTicker;
    private MenuController? _internalMenuController;
    private AnimationStatus _animationStatus = AnimationStatus.Dismissed;
    private double _swipeTargetDistance;
    private double _swipeCurrentDistance;
    private double _swipeVelocity;

    private CupertinoMenuAnchor Current => (CupertinoMenuAnchor)StateWidget;

    internal MenuController MenuController => Current.Controller ?? _internalMenuController!;

    internal FocusScopeNode MenuScopeNode => _menuScopeNode;

    internal AnimationController AnimationController => _animationController;

    internal ValueNotifier<double> SwipeScale => _swipeScale;

    internal bool IsOpenOrOpening =>
        _animationStatus is AnimationStatus.Forward or AnimationStatus.Completed;

    public override void InitState()
    {
        if (Current.Controller is null)
        {
            _internalMenuController = new MenuController();
        }

        _animationController = AnimationController.Unbounded(vsync: this);
        _animationController.AddStatusListener(HandleAnimationStatusChanged);
        WidgetsBinding.Instance.AddObserver(this);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (CupertinoMenuAnchor)oldWidget;
        if (!ReferenceEquals(previous.Controller, Current.Controller))
        {
            _internalMenuController = Current.Controller is null ? new MenuController() : null;
        }
    }

    public void DidChangeAccessibilityFeatures()
    {
        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    public override Widget Build(BuildContext context)
    {
        bool hasLeading = Current.MenuChildren.Any(
            child => child is CupertinoMenuEntry entry && entry.HasLeading(context));
        Widget anchor = new CupertinoMenuAnchorScope(
            hasLeading: hasLeading,
            child: new RawMenuAnchor(
                controller: MenuController,
                overlayBuilder: BuildMenuOverlay,
                onCloseRequested: HandleCloseRequested,
                onOpenRequested: HandleOpenRequested,
                useRootOverlay: Current.UseRootOverlay,
                childFocusNode: Current.ChildFocusNode,
                consumeOutsideTaps: Current.ConsumeOutsideTaps,
                onClose: Current.OnClose,
                onOpen: Current.OnOpen,
                builder: Current.Builder,
                child: Current.Child));

        if (Current.EnableLongPressToOpen && EffectiveSwipeEnabled)
        {
            anchor = new CupertinoMenuSwipeSurface(
                delay: TimeSpan.FromMilliseconds(400),
                onStart: position =>
                {
                    if (!IsOpenOrOpening)
                    {
                        MenuController.Open(position - AnchorTopLeft());
                    }
                },
                child: anchor);
        }

        return new CupertinoMenuSwipeRegion(
            enabled: EffectiveSwipeEnabled,
            onDistanceChanged: HandleSwipeDistanceChanged,
            child: anchor);
    }

    public override void Dispose()
    {
        WidgetsBinding.Instance.RemoveObserver(this);
        _animationController.RemoveStatusListener(HandleAnimationStatusChanged);
        _animationController.Dispose();
        _swipeTicker?.Stop();
        _swipeTicker?.Dispose();
        _swipeTicker = null;
        _menuScopeNode.Dispose();
        _swipeScale.Dispose();
        _internalMenuController = null;
    }

    private bool EffectiveSwipeEnabled =>
        Current.EnableSwipe && _animationStatus != AnimationStatus.Reverse;

    private void HandleOpenRequested(Vector? position, Action showOverlay)
    {
        showOverlay();
        if (_animationStatus is AnimationStatus.Completed or AnimationStatus.Forward)
        {
            return;
        }

        _animationController.AnimateWith(new SpringSimulation(
            ForwardSpring,
            _animationController.Value,
            1.0,
            0.5));

        (FocusScope.MaybeOf(Context) ?? FocusManager.Instance.RootScope).SetFirstFocus(_menuScopeNode);
    }

    private void HandleCloseRequested(Action hideOverlay)
    {
        if (_animationStatus is AnimationStatus.Reverse or AnimationStatus.Dismissed)
        {
            return;
        }

        var spring = new SpringSimulation(
            ReverseSpring,
            _animationController.Value,
            0.0,
            0.0,
            tolerance: SpringTolerance);
        _animationController
            .AnimateBackWith(new ClampedSimulation(spring, xMin: 0.0, xMax: 1.0))
            .WhenComplete(hideOverlay);
    }

    private void HandleAnimationStatusChanged(AnimationStatus status)
    {
        _animationStatus = status;
        if (Mounted)
        {
            SetState(static () => { });
        }

        Current.OnAnimationStatusChanged?.Invoke(status);
    }

    private Widget BuildMenuOverlay(BuildContext context, RawMenuOverlayInfo info)
    {
        bool excluded = !IsOpenOrOpening;
        return new ExcludeSemantics(
            excluding: excluded,
            child: new IgnorePointer(
                ignoring: excluded,
                child: new ExcludeFocus(
                    excluding: excluded,
                    child: new CupertinoMenuOverlay(
                        anchorState: this,
                        overlayInfo: info,
                        menuChildren: Current.MenuChildren,
                        constraints: Current.Constraints,
                        constrainCrossAxis: Current.ConstrainCrossAxis,
                        consumeOutsideTaps: Current.ConsumeOutsideTaps,
                        overlayPadding: Current.OverlayPadding))));
    }

    private void HandleSwipeDistanceChanged(double distance)
    {
        _swipeTargetDistance = Math.Clamp(distance, 0.0, 150.0);
        if (_swipeCurrentDistance == _swipeTargetDistance)
        {
            return;
        }

        _swipeTicker ??= CreateTicker(UpdateSwipeScale);
        if (!_swipeTicker.IsActive)
        {
            _swipeTicker.Start();
        }
    }

    private void UpdateSwipeScale(TimeSpan elapsed)
    {
        const double maxVelocity = 20.0;
        const double minVelocity = 8.0;
        const double maxSwipeDistance = 150.0;
        const double accelerationRate = 0.12;
        const double decelerationDistanceThreshold = 80.0;
        const double remainingDistanceSnapThreshold = 1.0;
        const double terminationDistanceThreshold = 5.0;

        double distance = _swipeTargetDistance - _swipeCurrentDistance;
        double absoluteDistance = Math.Abs(distance);
        double proximityFactor = Math.Min(absoluteDistance / decelerationDistanceThreshold, 1.0);
        _swipeVelocity = Math.Clamp(
            _swipeVelocity + (accelerationRate * proximityFactor),
            minVelocity,
            maxVelocity);
        double distanceReduction = Math.Sign(distance) * _swipeVelocity * proximityFactor;
        _swipeCurrentDistance += distanceReduction;

        if (absoluteDistance < remainingDistanceSnapThreshold)
        {
            _swipeCurrentDistance = _swipeTargetDistance;
            _swipeVelocity = 0.0;
            if (_swipeTargetDistance < terminationDistanceThreshold)
            {
                _swipeTicker!.Stop();
            }
        }

        _swipeScale.Value = 0.8 + (0.2 * (1.0 - (_swipeCurrentDistance / maxSwipeDistance)));
    }

    private Point AnchorTopLeft()
    {
        return Context.FindRenderObject() is RenderBox box ? box.LocalToGlobal(default) : default;
    }
}

internal sealed class CupertinoMenuAnchorScope : InheritedWidget
{
    public CupertinoMenuAnchorScope(bool hasLeading, Widget child) : base()
    {
        HasLeading = hasLeading;
        Child = child;
    }

    public bool HasLeading { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        ((CupertinoMenuAnchorScope)oldWidget).HasLeading != HasLeading;
}

internal sealed class CupertinoMenuOverlay : StatelessWidget
{
    private static readonly IReadOnlyDictionary<ShortcutActivator, Intent> TraversalShortcuts =
        new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = new CupertinoMenuFocusUpIntent(),
            [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = new CupertinoMenuFocusDownIntent(),
            [new SingleActivator(LogicalKeyboardKey.Home)] = new CupertinoMenuFocusFirstIntent(),
            [new SingleActivator(LogicalKeyboardKey.End)] = new CupertinoMenuFocusLastIntent(),
        };

    public CupertinoMenuOverlay(
        CupertinoMenuAnchorState anchorState,
        RawMenuOverlayInfo overlayInfo,
        IReadOnlyList<Widget> menuChildren,
        BoxConstraints? constraints,
        bool constrainCrossAxis,
        bool consumeOutsideTaps,
        EdgeInsetsGeometry overlayPadding,
        Key? key = null) : base(key)
    {
        AnchorState = anchorState;
        OverlayInfo = overlayInfo;
        MenuChildren = menuChildren;
        Constraints = constraints;
        ConstrainCrossAxis = constrainCrossAxis;
        ConsumeOutsideTaps = consumeOutsideTaps;
        OverlayPadding = overlayPadding;
    }

    public CupertinoMenuAnchorState AnchorState { get; }

    public RawMenuOverlayInfo OverlayInfo { get; }

    public IReadOnlyList<Widget> MenuChildren { get; }

    public BoxConstraints? Constraints { get; }

    public bool ConstrainCrossAxis { get; }

    public bool ConsumeOutsideTaps { get; }

    public EdgeInsetsGeometry OverlayPadding { get; }

    public override Widget Build(BuildContext context)
    {
        MediaQueryData? mediaQuery = MediaQuery.MaybeOf(context);
        Size screenSize = mediaQuery?.Size ?? OverlayInfo.OverlaySize;
        double normalizedScale = CupertinoMenuTextMetrics.NormalizeTextScale(
            mediaQuery?.TextScaler ?? TextScaler.NoScaling);
        bool largeText = normalizedScale >= 11.0;
        double width = screenSize.Width < 768.0
            ? largeText ? 370.0 : 250.0
            : largeText ? 343.0 : 262.0;
        BoxConstraints menuConstraints = Constraints ?? BoxConstraints.TightFor(width: width);
        IReadOnlyList<Widget> children = InsertImplicitDividers(context, MenuChildren);

        Widget panel = new Semantics(
            explicitChildNodes: true,
            scopesRoute: true,
            child: new ConstrainedBox(
                menuConstraints,
                new SingleChildScrollView(
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children: children))));

        panel = new AnimatedBuilder(
            animation: AnchorState.AnimationController,
            child: panel,
            builder: (_, child) => new Align(
                alignment: Alignment.TopCenter,
                heightFactor: ResolveSizeFactor(),
                widthFactor: 1.0,
                child: child));
        panel = new CupertinoPopupSurface(child: panel);
        panel = new AnimatedBuilder(
            animation: AnchorState.AnimationController,
            child: panel,
            builder: (_, child) => new Opacity(
                opacity: ResolveOpacity(),
                alwaysIncludeSemantics: true,
                child: child));
        panel = new DecoratedBox(
            decoration: new BoxDecoration(
                BoxShadows:
                [
                    new Plumix.Rendering.BoxShadow(
                        color: Color.FromArgb((byte)Math.Round(31.0 * ResolveOpacity()), 0, 0, 10),
                        blurRadius: 50.0 * ResolveOpacity()),
                ]),
            child: panel);
        panel = new FocusScope(
            focusScopeNode: AnchorState.MenuScopeNode,
            skipTraversal: false,
            child: new Actions(
                actions: new Dictionary<Type, FlutterAction>
                {
                    [typeof(DismissIntent)] = new DismissMenuAction(AnchorState.MenuController),
                    [typeof(CupertinoMenuFocusUpIntent)] =
                        new CallbackAction<CupertinoMenuFocusUpIntent>(_ => MoveFocus(up: true)),
                    [typeof(CupertinoMenuFocusDownIntent)] =
                        new CallbackAction<CupertinoMenuFocusDownIntent>(_ => MoveFocus(up: false)),
                    [typeof(CupertinoMenuFocusFirstIntent)] =
                        new CallbackAction<CupertinoMenuFocusFirstIntent>(_ => FocusBoundary(first: true)),
                    [typeof(CupertinoMenuFocusLastIntent)] =
                        new CallbackAction<CupertinoMenuFocusLastIntent>(_ => FocusBoundary(first: false)),
                },
                child: new Shortcuts(
                    shortcuts: TraversalShortcuts,
                    child: panel)));
        panel = new TapRegion(
            groupId: OverlayInfo.TapRegionGroupId,
            consumeOutsideTaps: ConsumeOutsideTaps,
            onTapOutside: _ => AnchorState.MenuController.Close(),
            child: new CupertinoMenuSwipeSurface(child: panel));

        if (!ConstrainCrossAxis)
        {
            panel = new UnconstrainedBox(
                clipBehavior: Clip.HardEdge,
                alignment: AlignmentDirectional.CenterStart,
                constrainedAxis: Axis.Vertical,
                child: panel);
        }

        TextDirection direction = Directionality.Of(context);
        Thickness padding = OverlayPadding.Resolve(direction);
        List<Rect> avoidBounds = mediaQuery is null
            ? []
            : DisplayFeatureSubScreen.AvoidBounds(mediaQuery);
        CupertinoMenuAttachment attachment = CupertinoMenuLayoutDelegate.ResolveAttachment(
            OverlayInfo.AnchorRect,
            OverlayInfo.OverlaySize,
            OverlayInfo.Position);
        Rect effectiveAnchorRect = OverlayInfo.Position.HasValue
            ? new Rect(attachment.AttachmentPoint, new Size())
            : OverlayInfo.AnchorRect;

        Widget layout = new AnimatedBuilder(
            animation: AnchorState.AnimationController,
            child: panel,
            builder: (_, child) => new CustomSingleChildLayout(
                layoutDelegate: new CupertinoMenuLayoutDelegate(
                    anchorRect: effectiveAnchorRect,
                    attachmentPoint: attachment.AttachmentPoint,
                    avoidBounds: avoidBounds,
                    heightFactor: ResolveSizeFactor(),
                    menuAlignment: attachment.MenuAlignment,
                    overlayPadding: padding),
                child: child));
        layout = new AnimatedBuilder(
            animation: AnchorState.SwipeScale,
            child: layout,
            builder: (_, child) => Plumix.Widgets.Transform.Scale(
                scale: ResolveScale(),
                alignment: attachment.TransformAlignment,
                child: child));
        return new ConstrainedBox(BoxConstraints.Loose(OverlayInfo.OverlaySize), layout);
    }

    private double ResolveVisibility()
    {
        if (WidgetsBinding.Instance.AccessibilityFeatures.DisableAnimations)
        {
            return 1.0;
        }

        return Math.Clamp(AnchorState.AnimationController.Value, 0.0, 1.0);
    }

    private double ResolveScale()
    {
        AccessibilityFeatures features = WidgetsBinding.Instance.AccessibilityFeatures;
        if (features.DisableAnimations)
        {
            return 1.0;
        }

        if (features.ReduceMotion)
        {
            return AnchorState.SwipeScale.Value;
        }

        return AnchorState.SwipeScale.Value * (0.8 + (0.2 * ResolveVisibility()));
    }

    private double ResolveSizeFactor() =>
        WidgetsBinding.Instance.AccessibilityFeatures.ReduceMotion ? 1.0 : 0.8 + (0.2 * ResolveVisibility());

    private double ResolveOpacity() => Curves.EaseIn(ResolveVisibility());

    private static IReadOnlyList<Widget> InsertImplicitDividers(
        BuildContext context,
        IReadOnlyList<Widget> children)
    {
        if (children.Count == 0)
        {
            return children;
        }

        double ratio = MediaQuery.MaybeOf(context)?.DevicePixelRatio ?? 1.0;
        var result = new List<Widget>();
        for (int index = 0; index < children.Count; index++)
        {
            Widget child = children[index];
            if (index > 0 && !IsDivider(children[index - 1]) && !IsDivider(child))
            {
                result.Add(new CupertinoMenuImplicitDivider(devicePixelRatio: ratio));
            }

            result.Add(child);
        }

        return result;
    }

    private static bool IsDivider(Widget widget) =>
        widget is CupertinoMenuEntry { IsDivider: true };

    private object? MoveFocus(bool up)
    {
        var policy = new ReadingOrderTraversalPolicy();
        FocusScopeNode scope = AnchorState.MenuScopeNode;
        FocusNode? current = FocusManager.Instance.PrimaryFocus;
        if (current is null)
        {
            return FocusBoundary(first: !up);
        }

        bool preventsWrapping = !OperatingSystem.IsBrowser()
                                && (OperatingSystem.IsIOS() || OperatingSystem.IsMacOS());
        if (!preventsWrapping)
        {
            FocusNode? first = policy.FindFirstFocus(scope, ignoreCurrentFocus: true);
            FocusNode? last = policy.FindLastFocus(scope, ignoreCurrentFocus: true);
            if (up && ReferenceEquals(current, first))
            {
                last?.RequestFocus();
                return null;
            }

            if (!up && ReferenceEquals(current, last))
            {
                first?.RequestFocus();
                return null;
            }
        }

        policy.InDirection(current, up ? TraversalDirection.Up : TraversalDirection.Down);
        return null;
    }

    private object? FocusBoundary(bool first)
    {
        var policy = new ReadingOrderTraversalPolicy();
        FocusNode? target = first
            ? policy.FindFirstFocus(AnchorState.MenuScopeNode, ignoreCurrentFocus: true)
            : policy.FindLastFocus(AnchorState.MenuScopeNode, ignoreCurrentFocus: true);
        target?.RequestFocus();
        return null;
    }
}

internal sealed class CupertinoMenuFocusUpIntent : Intent;

internal sealed class CupertinoMenuFocusDownIntent : Intent;

internal sealed class CupertinoMenuFocusFirstIntent : Intent;

internal sealed class CupertinoMenuFocusLastIntent : Intent;

internal readonly record struct CupertinoMenuAttachment(
    Point AttachmentPoint,
    Alignment MenuAlignment,
    Alignment TransformAlignment);

internal sealed class CupertinoMenuLayoutDelegate : SingleChildLayoutDelegate
{
    public CupertinoMenuLayoutDelegate(
        Rect anchorRect,
        Point attachmentPoint,
        IReadOnlyList<Rect> avoidBounds,
        double heightFactor,
        Alignment menuAlignment,
        Thickness overlayPadding)
    {
        AnchorRect = anchorRect;
        AttachmentPoint = attachmentPoint;
        AvoidBounds = avoidBounds;
        HeightFactor = heightFactor;
        MenuAlignment = menuAlignment;
        OverlayPadding = overlayPadding;
    }

    public Rect AnchorRect { get; }

    public Point AttachmentPoint { get; }

    public IReadOnlyList<Rect> AvoidBounds { get; }

    public double HeightFactor { get; }

    public Alignment MenuAlignment { get; }

    public Thickness OverlayPadding { get; }

    public override BoxConstraints GetConstraintsForChild(BoxConstraints constraints) =>
        BoxConstraints.Loose(constraints.Biggest).Deflate(OverlayPadding);

    public override Point GetPositionForChild(Size size, Size childSize)
    {
        double inverseHeightFactor = HeightFactor > 0.01 ? 1.0 / HeightFactor : 0.0;
        double finalHeight = Math.Min(childSize.Height * inverseHeightFactor, size.Height);
        var finalSize = new Size(childSize.Width, finalHeight);
        Point desired = AttachmentPoint - MenuAlignment.AlongSize(finalSize);
        Rect screen = ClosestScreen(size);
        Point finalPosition = PositionChild(screen, finalSize, desired, AnchorRect);
        double x = finalPosition.X;
        double y = finalPosition.Y;

        if (y + finalHeight <= AnchorRect.Center.Y)
        {
            return new Point(x, y + finalHeight - childSize.Height);
        }

        double startY = AnchorRect.Bottom;
        return new Point(x, startY + ((y - startY) * HeightFactor));
    }

    public override bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate)
    {
        if (oldDelegate is not CupertinoMenuLayoutDelegate old)
        {
            return true;
        }

        return old.AnchorRect != AnchorRect
               || old.AttachmentPoint != AttachmentPoint
               || old.HeightFactor != HeightFactor
               || old.MenuAlignment != MenuAlignment
               || old.OverlayPadding != OverlayPadding
               || !old.AvoidBounds.SequenceEqual(AvoidBounds);
    }

    internal static CupertinoMenuAttachment ResolveAttachment(
        Rect anchorRect,
        Size overlaySize,
        Vector? position)
    {
        Point midpoint = position.HasValue ? anchorRect.TopLeft + position.Value : anchorRect.Center;
        double xRatio = overlaySize.Width == 0.0 ? 0.5 : midpoint.X / overlaySize.Width;
        double yRatio = overlaySize.Height == 0.0 ? 0.5 : midpoint.Y / overlaySize.Height;
        double dy = yRatio < 0.55 ? 1.0 : -1.0;
        double dx = xRatio < 0.4 ? -1.0 : xRatio > 0.6 ? 1.0 : 0.0;
        var menuAlignment = new Alignment(dx, -dy);
        Point attachment;
        Point transformOrigin;
        if (position.HasValue)
        {
            attachment = anchorRect.TopLeft + position.Value;
            transformOrigin = attachment;
        }
        else
        {
            var offset = new Vector(0.0, 8.0 * dy);
            attachment = new Alignment(dx, dy).WithinRect(anchorRect) + offset;
            transformOrigin = new Alignment(0.0, dy).WithinRect(anchorRect) + offset;
        }

        var transformAlignment = new Alignment(
            overlaySize.Width == 0.0 ? 0.0 : (transformOrigin.X / overlaySize.Width * 2.0) - 1.0,
            overlaySize.Height == 0.0 ? 0.0 : (transformOrigin.Y / overlaySize.Height * 2.0) - 1.0);
        return new CupertinoMenuAttachment(attachment, menuAlignment, transformAlignment);
    }

    private Rect ClosestScreen(Size size)
    {
        List<Rect> screens = DisplayFeatureSubScreen.SubScreensInBounds(new Rect(default, size), AvoidBounds);
        Point anchor = AnchorRect.Center;
        Rect closest = screens.Count == 0 ? new Rect(default, size) : screens[0];
        double closestDistance = DistanceToRect(anchor, closest);
        foreach (Rect screen in screens.Skip(1))
        {
            double distance = DistanceToRect(anchor, screen);
            if (distance < closestDistance)
            {
                closest = screen;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private Point PositionChild(Rect screen, Size childSize, Point position, Rect anchor)
    {
        double x = position.X;
        double y = position.Y;
        bool OverLeft(double value) => value < screen.Left + OverlayPadding.Left;
        bool OverRight(double value) =>
            value > screen.Right - childSize.Width - OverlayPadding.Right;
        bool OverTop(double value) => value < screen.Top + OverlayPadding.Top;
        bool OverBottom(double value) =>
            value > screen.Bottom - childSize.Height - OverlayPadding.Bottom;

        bool hasHorizontalAnchorOverlap = childSize.Width >= screen.Width;
        if (hasHorizontalAnchorOverlap)
        {
            x = screen.Left + OverlayPadding.Left;
        }
        else if (OverLeft(x))
        {
            double flipped = (anchor.Center.X * 2.0) - position.X - childSize.Width;
            hasHorizontalAnchorOverlap = OverRight(flipped);
            x = hasHorizontalAnchorOverlap || OverLeft(flipped)
                ? screen.Left + OverlayPadding.Left
                : flipped;
        }
        else if (OverRight(x))
        {
            double flipped = (anchor.Center.X * 2.0) - position.X - childSize.Width;
            hasHorizontalAnchorOverlap = OverLeft(flipped);
            x = hasHorizontalAnchorOverlap || OverRight(flipped)
                ? screen.Right - childSize.Width - OverlayPadding.Right
                : flipped;
        }

        if (childSize.Height >= screen.Height)
        {
            return new Point(x, screen.Top + OverlayPadding.Top);
        }

        if (hasHorizontalAnchorOverlap && anchor.Width > 0.0 && anchor.Height > 0.0)
        {
            double below = anchor.Bottom - y;
            double above = y + childSize.Height - anchor.Top;
            if (below > 0.0 && above > 0.0)
            {
                y = below > above ? anchor.Top - childSize.Height : anchor.Bottom;
            }
        }

        if (OverTop(y))
        {
            double flipped = (anchor.Center.Y * 2.0) - position.Y - childSize.Height;
            y = OverTop(flipped) || OverBottom(flipped)
                ? screen.Top + OverlayPadding.Top
                : flipped;
        }
        else if (OverBottom(y))
        {
            double flipped = (anchor.Center.Y * 2.0) - position.Y - childSize.Height;
            y = OverTop(flipped) || OverBottom(flipped)
                ? screen.Bottom - childSize.Height - OverlayPadding.Bottom
                : flipped;
        }

        return new Point(x, y);
    }

    private static double DistanceToRect(Point point, Rect rect)
    {
        double dx = Math.Max(Math.Max(rect.Left - point.X, 0.0), point.X - rect.Right);
        double dy = Math.Max(Math.Max(rect.Top - point.Y, 0.0), point.Y - rect.Bottom);
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}

public sealed class CupertinoMenuDivider : StatelessWidget, CupertinoMenuEntry
{
    public static CupertinoDynamicColor KDefaultColor { get; } = CupertinoDynamicColor.WithBrightness(
        Avalonia.Media.Color.FromArgb(20, 0, 0, 0),
        Avalonia.Media.Color.FromArgb(41, 0, 0, 0));

    public CupertinoMenuDivider(CupertinoDynamicColor? color = null, Key? key = null) : base(key)
    {
        Color = color ?? KDefaultColor;
    }

    public CupertinoDynamicColor Color { get; }

    public bool IsDivider => true;

    public bool HasLeading(BuildContext context) => false;

    public override Widget Build(BuildContext context) => new ColoredBox(
        Color.ResolveFrom(context),
        child: new SizedBox(height: 8.0, width: double.PositiveInfinity));
}

internal sealed class CupertinoMenuImplicitDivider : StatelessWidget
{
    public CupertinoMenuImplicitDivider(double devicePixelRatio)
    {
        DevicePixelRatio = devicePixelRatio;
    }

    public double DevicePixelRatio { get; }

    public override Widget Build(BuildContext context)
    {
        bool dark = CupertinoTheme.MaybeBrightnessOf(context) == PlatformBrightness.Dark;
        Color color = dark ? Color.FromArgb(64, 255, 255, 255) : Color.FromArgb(64, 0, 0, 0);
        return new ColoredBox(
            color,
            child: new SizedBox(height: 1.0 / Math.Max(DevicePixelRatio, double.Epsilon)));
    }
}

public sealed class CupertinoMenuItem : StatefulWidget, CupertinoMenuEntry
{
    public static WidgetStateProperty<BoxDecoration> KDefaultDecoration { get; } =
        WidgetStateProperty<BoxDecoration>.ResolveWith(states => ResolveDefaultDecoration(states, dark: false));

    private static WidgetStateProperty<MouseCursor> DefaultCursor { get; } =
        WidgetStateProperty<MouseCursor>.ResolveWith(states =>
            !states.Contains(WidgetState.Disabled) && OperatingSystem.IsBrowser()
                ? SystemMouseCursors.Click
                : Plumix.Widgets.MouseCursor.Defer);

    public CupertinoMenuItem(
        Widget child,
        Widget? subtitle = null,
        Widget? leading = null,
        double? leadingWidth = null,
        AlignmentGeometry? leadingMidpointAlignment = null,
        Widget? trailing = null,
        double? trailingWidth = null,
        AlignmentGeometry? trailingMidpointAlignment = null,
        EdgeInsetsGeometry? padding = null,
        BoxConstraints? constraints = null,
        bool autofocus = false,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        Action<bool>? onHover = null,
        Action? onPressed = null,
        WidgetStateProperty<BoxDecoration>? decoration = null,
        WidgetStateProperty<MouseCursor>? mouseCursor = null,
        HitTestBehavior behavior = HitTestBehavior.Opaque,
        bool requestCloseOnActivate = true,
        bool requestFocusOnHover = true,
        bool isDestructiveAction = false,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Subtitle = subtitle;
        Leading = leading;
        LeadingWidth = leadingWidth;
        LeadingMidpointAlignment = leadingMidpointAlignment;
        Trailing = trailing;
        TrailingWidth = trailingWidth;
        TrailingMidpointAlignment = trailingMidpointAlignment;
        Padding = padding;
        Constraints = constraints;
        Autofocus = autofocus;
        FocusNode = focusNode;
        OnFocusChange = onFocusChange;
        OnHover = onHover;
        OnPressed = onPressed;
        Decoration = decoration;
        MouseCursor = mouseCursor;
        Behavior = behavior;
        RequestCloseOnActivate = requestCloseOnActivate;
        RequestFocusOnHover = requestFocusOnHover;
        IsDestructiveAction = isDestructiveAction;
    }

    public Widget Child { get; }

    public Widget? Subtitle { get; }

    public Widget? Leading { get; }

    public double? LeadingWidth { get; }

    public AlignmentGeometry? LeadingMidpointAlignment { get; }

    public Widget? Trailing { get; }

    public double? TrailingWidth { get; }

    public AlignmentGeometry? TrailingMidpointAlignment { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public BoxConstraints? Constraints { get; }

    public bool Autofocus { get; }

    public FocusNode? FocusNode { get; }

    public Action<bool>? OnFocusChange { get; }

    public Action<bool>? OnHover { get; }

    public Action? OnPressed { get; }

    public WidgetStateProperty<BoxDecoration>? Decoration { get; }

    public WidgetStateProperty<MouseCursor>? MouseCursor { get; }

    public HitTestBehavior Behavior { get; }

    public bool RequestCloseOnActivate { get; }

    public bool RequestFocusOnHover { get; }

    public bool IsDestructiveAction { get; }

    public bool IsDivider => false;

    public bool HasLeading(BuildContext context) => Leading is not null;

    public override State CreateState() => new CupertinoMenuItemState();

    internal static BoxDecoration ResolveDefaultDecoration(IReadOnlySet<WidgetState> states, bool dark)
    {
        byte alpha = states.Contains(WidgetState.Dragged) || states.Contains(WidgetState.Pressed)
            ? (byte)26
            : states.Contains(WidgetState.Focused)
                ? (byte)19
                : states.Contains(WidgetState.Hovered)
                    ? (byte)13
                    : (byte)0;
        Color color = dark ? Color.FromArgb(alpha, 255, 255, 255) : Color.FromArgb(alpha, 50, 50, 50);
        return new BoxDecoration(Color: color);
    }

    internal static WidgetStateProperty<MouseCursor> ResolveMouseCursorProperty(
        WidgetStateProperty<MouseCursor>? property) => property ?? DefaultCursor;
}

public sealed class CupertinoMenuItemState : State, CupertinoMenuSwipeTarget
{
    private readonly HashSet<WidgetState> _states = [];
    private FocusNode? _internalFocusNode;
    private bool _isHovered;

    private CupertinoMenuItem Current => (CupertinoMenuItem)StateWidget;

    internal IReadOnlySet<WidgetState> States => _states;

    private bool Enabled => Current.OnPressed is not null;

    public override void InitState()
    {
        if (Current.FocusNode is null)
        {
            _internalFocusNode = new FocusNode();
        }

        if (!Enabled)
        {
            _states.Add(WidgetState.Disabled);
        }
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (CupertinoMenuItem)oldWidget;
        if (!ReferenceEquals(previous.FocusNode, Current.FocusNode))
        {
            _internalFocusNode?.Dispose();
            _internalFocusNode = Current.FocusNode is null ? new FocusNode() : null;
        }

        if (Enabled)
        {
            _states.Remove(WidgetState.Disabled);
        }
        else
        {
            _states.Clear();
            _states.Add(WidgetState.Disabled);
            _isHovered = false;
        }
    }

    public override Widget Build(BuildContext context)
    {
        TextScaler scaler = MediaQuery.MaybeTextScalerOf(context) ?? TextScaler.NoScaling;
        double normalizedScale = CupertinoMenuTextMetrics.NormalizeTextScale(scaler);
        bool largeText = normalizedScale >= 11.0;
        TextStyle bodyStyle = CupertinoMenuTextMetrics.ResolveBody(normalizedScale);
        TextStyle subheadStyle = CupertinoMenuTextMetrics.ResolveSubhead(normalizedScale);
        Color contentColor = ResolveContentColor(context);
        Widget label = BuildLabel(context, scaler, normalizedScale, largeText, bodyStyle, subheadStyle);
        label = IconTheme.Merge(new IconThemeData(Color: contentColor), label);
        label = DefaultTextStyle.Merge(
            child: label,
            style: bodyStyle.CopyWith(color: contentColor),
            maxLines: largeText ? 100 : 2,
            overflow: TextOverflow.Ellipsis,
            softWrap: true);

        BoxDecoration decoration = ResolveDecoration(context);
        MouseCursor cursor = CupertinoMenuItem
            .ResolveMouseCursorProperty(Current.MouseCursor)
            .Resolve(_states);
        Widget interaction = new RawGestureDetector(
            behavior: Current.Behavior,
            gestures: Enabled ? BuildItemGestures(context) : RawGestureDetector.NoGestures,
            child: label);
        interaction = new DecoratedBox(decoration, interaction);
        interaction = new MouseRegion(
            cursor: cursor,
            opaque: false,
            onHover: Enabled ? _ => HandleHover() : null,
            onExit: Enabled ? _ => HandleExit() : null,
            child: interaction);
        interaction = new MetaData(
            metaData: this,
            behavior: HitTestBehavior.Opaque,
            child: interaction);
        interaction = new Focus(
            child: interaction,
            includeSemantics: true,
            autofocus: Enabled && Current.Autofocus,
            focusNode: Current.FocusNode ?? _internalFocusNode,
            canRequestFocus: Enabled,
            skipTraversal: !Enabled,
            onFocusChange: HandleFocusChange);
        interaction = new Actions(
            actions: Enabled
                ? new Dictionary<Type, FlutterAction>
                {
                    [typeof(ActivateIntent)] = new CallbackAction<ActivateIntent>(_ => HandleActivate()),
                    [typeof(ButtonActivateIntent)] =
                        new CallbackAction<ButtonActivateIntent>(_ => HandleActivate()),
                }
                : new Dictionary<Type, FlutterAction>(),
            child: interaction);
        interaction = new Semantics(
            enabled: Enabled,
            onDismiss: Enabled ? HandleDismiss : null,
            child: interaction);
        interaction = new MergeSemantics(interaction);
        return MediaQuery.WithClampedTextScaling(
            context,
            interaction,
            minScaleFactor: 1.0 - (3.0 / 17.0),
            maxScaleFactor: 1.0 + (36.0 / 17.0));
    }

    public override void Dispose()
    {
        _internalFocusNode?.Dispose();
        _internalFocusNode = null;
    }

    public bool SwipeEnter()
    {
        if (!Enabled)
        {
            return false;
        }

        if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
        {
            HapticFeedback.SelectionClick();
        }

        UpdateState(WidgetState.Dragged, true);
        return true;
    }

    public void SwipeExit() => UpdateState(WidgetState.Dragged, false);

    public void SwipeComplete()
    {
        if (Enabled)
        {
            HandleActivate();
        }
    }

    /// <summary>
    /// The single tap recognizer a menu item registers, rebuilt whenever the ambient
    /// <see cref="DeviceGestureSettings"/> change. Dart's `_gestures` cache.
    /// </summary>
    private IReadOnlyDictionary<Type, IGestureRecognizerFactory> BuildItemGestures(BuildContext context)
    {
        DeviceGestureSettings? gestureSettings = MediaQuery.MaybeGestureSettingsOf(context);
        return new Dictionary<Type, IGestureRecognizerFactory>
        {
            [typeof(TapGestureRecognizer)] = new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                () => new TapGestureRecognizer { DebugOwner = this },
                instance =>
                {
                    instance.OnTapDown = _ => UpdateState(WidgetState.Pressed, true);
                    instance.OnTapUp = _ => HandleTapUp();
                    instance.OnTapCancel = () => UpdateState(WidgetState.Pressed, false);
                    instance.GestureSettings = gestureSettings;
                }),
        };
    }

    private Widget BuildLabel(
        BuildContext context,
        TextScaler scaler,
        double normalizedScale,
        bool largeText,
        TextStyle bodyStyle,
        TextStyle subheadStyle)
    {
        double ratio = MediaQuery.MaybeOf(context)?.DevicePixelRatio ?? 1.0;
        double lineHeight = bodyStyle.FontSize!.Value * bodyStyle.Height!.Value;
        bool reserveLeading = Current.Leading is not null
                              || (CupertinoMenuAnchor.MaybeHasLeadingOf(context) ?? false);
        double leadingWidth = Current.LeadingWidth ?? (reserveLeading
            ? CupertinoMenuTextMetrics.RoundToPixel((-0.311 * normalizedScale) + 10.0 + lineHeight, ratio)
            : 16.0);
        double trailingWidth = Current.TrailingWidth ?? (Current.Trailing is not null && !largeText
            ? CupertinoMenuTextMetrics.RoundToPixel((0.1 * normalizedScale) + 22.0 + lineHeight, ratio)
            : 16.0);
        double minimumHeight = CupertinoMenuTextMetrics.RoundToPixel(lineHeight * 14.0 / 11.0, ratio)
                               + CupertinoMenuTextMetrics.RoundToPixel(lineHeight * 71.0 / 100.0, ratio);
        double verticalPadding = Math.Max(0.0, minimumHeight - lineHeight) / 2.0;
        EdgeInsetsGeometry padding = Current.Padding ?? EdgeInsetsGeometry.Symmetric(vertical: verticalPadding);
        BoxConstraints constraints = Current.Constraints ?? new BoxConstraints(MinHeight: minimumHeight);

        var stackChildren = new List<Widget>();
        if (Current.Leading is not null)
        {
            double ratioX = (0.000118 * normalizedScale) + (73.0 / 125.0);
            AlignmentGeometry alignment = Current.LeadingMidpointAlignment
                                          ?? new AlignmentDirectional((ratioX * 2.0) - 1.0, 0.0);
            Widget leading = IconTheme.Merge(
                new IconThemeData(Size: 15.0, Weight: 600.0, ApplyTextScaling: true),
                DefaultTextStyle.Merge(
                    child: Current.Leading,
                    style: new TextStyle(FontSize: 15.0, FontWeight: FontWeight.DemiBold)));
            stackChildren.Add(new PositionedDirectional(
                start: 0.0,
                top: 0.0,
                bottom: 0.0,
                width: leadingWidth,
                child: new CupertinoAlignMidpoint(alignment: alignment, child: leading)));
        }

        Widget center = Current.Child;
        if (Current.Subtitle is not null)
        {
            Color subtitleColor = CupertinoMenuTextMetrics.SubtitleColor(context);
            center = new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                mainAxisAlignment: MainAxisAlignment.Center,
                children:
                [
                    Current.Child,
                    new SizedBox(height: 1.0),
                    DefaultTextStyle.Merge(
                        child: Current.Subtitle,
                        style: subheadStyle.CopyWith(color: subtitleColor),
                        maxLines: largeText ? 100 : 2,
                        overflow: TextOverflow.Ellipsis,
                        softWrap: true),
                ]);
        }
        else
        {
            center = new Align(alignment: AlignmentDirectional.CenterStart, child: center);
        }

        stackChildren.Add(new Padding(
            EdgeInsetsGeometry.DirectionalOnly(start: leadingWidth, end: trailingWidth).Resolve(
                Directionality.Of(context)),
            center));

        if (Current.Trailing is not null && !largeText)
        {
            double offset = (trailingWidth / 2.0) + 6.0;
            double ratioX = (trailingWidth - offset) / trailingWidth;
            AlignmentGeometry alignment = Current.TrailingMidpointAlignment
                                          ?? new AlignmentDirectional((ratioX * 2.0) - 1.0, 0.0);
            Widget trailing = IconTheme.Merge(
                new IconThemeData(Size: 21.0, ApplyTextScaling: true),
                DefaultTextStyle.Merge(
                    child: Current.Trailing,
                    style: new TextStyle(FontSize: 21.0)));
            stackChildren.Add(new PositionedDirectional(
                end: 0.0,
                top: 0.0,
                bottom: 0.0,
                width: trailingWidth,
                child: new CupertinoAlignMidpoint(alignment: alignment, child: trailing)));
        }

        return new ConstrainedBox(
            constraints,
            new Padding(
                padding.Resolve(Directionality.Of(context)),
                new Stack(children: stackChildren)));
    }

    private Color ResolveContentColor(BuildContext context)
    {
        CupertinoDynamicColor color = !Enabled
            ? CupertinoColors.SystemGrey
            : Current.IsDestructiveAction
                ? CupertinoColors.SystemRed
                : CupertinoMenuTextMetrics.DefaultTextColor;
        return color.ResolveFrom(context);
    }

    private BoxDecoration ResolveDecoration(BuildContext context)
    {
        WidgetStateProperty<BoxDecoration> property = Current.Decoration ?? CupertinoMenuItem.KDefaultDecoration;
        if (ReferenceEquals(property, CupertinoMenuItem.KDefaultDecoration))
        {
            bool dark = CupertinoTheme.MaybeBrightnessOf(context) == PlatformBrightness.Dark;
            return CupertinoMenuItem.ResolveDefaultDecoration(_states, dark);
        }

        return property.Resolve(_states);
    }

    private void HandleHover()
    {
        if (_isHovered)
        {
            return;
        }

        _isHovered = true;
        UpdateState(WidgetState.Hovered, true);
        Current.OnHover?.Invoke(true);
        if (Current.RequestFocusOnHover)
        {
            (Current.FocusNode ?? _internalFocusNode)?.RequestFocus();
        }
    }

    private void HandleExit()
    {
        if (!_isHovered)
        {
            return;
        }

        _isHovered = false;
        UpdateState(WidgetState.Hovered, false);
        UpdateState(WidgetState.Focused, false);
        Current.OnHover?.Invoke(false);
    }

    private void HandleFocusChange(bool focused)
    {
        UpdateState(WidgetState.Focused, focused);
        Current.OnFocusChange?.Invoke(focused);
    }

    private void HandleTapUp()
    {
        UpdateState(WidgetState.Pressed, false);
        HandleActivate();
    }

    private object? HandleActivate()
    {
        UpdateState(WidgetState.Dragged, false);
        UpdateState(WidgetState.Pressed, false);
        if (Current.RequestCloseOnActivate)
        {
            MenuController.MaybeOf(Context)?.Close();
        }

        Current.OnPressed?.Invoke();
        return null;
    }

    private void HandleDismiss() => MenuController.MaybeOf(Context)?.Close();

    private void UpdateState(WidgetState state, bool value)
    {
        bool changed = value ? _states.Add(state) : _states.Remove(state);
        if (changed && Mounted)
        {
            SetState(static () => { });
        }
    }
}

internal static class CupertinoMenuTextMetrics
{
    private static readonly double[] Breakpoints = [-3, -2, -1, 0, 2, 4, 6, 11, 16, 23, 30, 36];

    private static readonly TextStyle[] BodyStyles =
    [
        Style(14, 19.0 / 14.0, -0.15, display: false),
        Style(15, 20.0 / 15.0, -0.23, display: false),
        Style(16, 21.0 / 16.0, -0.31, display: false),
        Style(17, 22.0 / 17.0, -0.43, display: false),
        Style(19, 24.0 / 19.0, -0.44, display: false),
        Style(21, 26.0 / 21.0, -0.36, display: false),
        Style(23, 29.0 / 23.0, -0.10, display: true),
        Style(28, 34.0 / 28.0, 0.38, display: true),
        Style(33, 40.0 / 33.0, 0.40, display: true),
        Style(40, 48.0 / 40.0, 0.37, display: true),
        Style(47, 56.0 / 47.0, 0.37, display: true),
        Style(53, 62.0 / 53.0, 0.31, display: true),
    ];

    private static readonly TextStyle[] SubheadStyles =
    [
        Style(12, 16.0 / 12.0, 0.0, display: false),
        Style(13, 18.0 / 13.0, -0.08, display: false),
        Style(14, 19.0 / 14.0, -0.15, display: false),
        Style(15, 20.0 / 15.0, -0.23, display: false),
        Style(17, 22.0 / 17.0, -0.43, display: false),
        Style(19, 24.0 / 19.0, -0.45, display: false),
        Style(21, 28.0 / 21.0, -0.36, display: false),
        Style(25, 31.0 / 25.0, 0.15, display: true),
        Style(30, 37.0 / 30.0, 0.40, display: true),
        Style(36, 43.0 / 36.0, 0.37, display: true),
        Style(42, 50.0 / 42.0, 0.37, display: true),
        Style(49, 58.0 / 49.0, 0.33, display: true),
    ];

    public static CupertinoDynamicColor DefaultTextColor { get; } = CupertinoDynamicColor.WithBrightness(
        Color.FromArgb(245, 0, 0, 0),
        Color.FromArgb(245, 255, 255, 255));

    private static CupertinoDynamicColor DefaultSubtitleTextColor { get; } =
        CupertinoDynamicColor.WithBrightness(
            Color.FromArgb(140, 0, 0, 0),
            Color.FromArgb(102, 255, 255, 255));

    public static double NormalizeTextScale(TextScaler scaler) => scaler.Scale(17.0) - 17.0;

    public static TextStyle ResolveBody(double normalizedScale) => Resolve(BodyStyles, normalizedScale);

    public static TextStyle ResolveSubhead(double normalizedScale) => Resolve(SubheadStyles, normalizedScale);

    public static Color SubtitleColor(BuildContext context) => DefaultSubtitleTextColor.ResolveFrom(context);

    public static double RoundToPixel(double value, double devicePixelRatio) =>
        Math.Floor((value * devicePixelRatio) + 0.5) / devicePixelRatio;

    private static TextStyle Resolve(IReadOnlyList<TextStyle> styles, double value)
    {
        if (value <= Breakpoints[0])
        {
            return styles[0];
        }

        for (int index = 1; index < Breakpoints.Length; index++)
        {
            if (value <= Breakpoints[index])
            {
                double t = (value - Breakpoints[index - 1]) / (Breakpoints[index] - Breakpoints[index - 1]);
                return TextStyle.Lerp(styles[index - 1], styles[index], t);
            }
        }

        return styles[^1];
    }

    private static TextStyle Style(double size, double height, double spacing, bool display) => new(
        FontFamily: new FontFamily(display ? "CupertinoSystemDisplay" : "CupertinoSystemText"),
        FontSize: size,
        Height: height,
        LetterSpacing: spacing);
}

internal sealed class CupertinoAlignMidpoint : SingleChildRenderObjectWidget
{
    public CupertinoAlignMidpoint(
        AlignmentGeometry alignment,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Alignment = alignment;
    }

    public AlignmentGeometry Alignment { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderCupertinoAlignMidpoint(Alignment.Resolve(Directionality.Of(context)));

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderCupertinoAlignMidpoint)renderObject).Alignment = Alignment.Resolve(Directionality.Of(context));
    }
}

internal sealed class RenderCupertinoAlignMidpoint : RenderProxyBox
{
    private Alignment _alignment;

    public RenderCupertinoAlignMidpoint(Alignment alignment)
    {
        _alignment = alignment;
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        Child.Layout(Constraints.Loosen(), parentUsesSize: true);
        Size = Constraints.Constrain(Child.Size);
        Point midpoint = _alignment.AlongSize(Size);
        double dx = Math.Clamp(midpoint.X - (Child.Size.Width / 2.0), 0.0, Size.Width - Child.Size.Width);
        double dy = Math.Clamp(midpoint.Y - (Child.Size.Height / 2.0), 0.0, Size.Height - Child.Size.Height);
        ((BoxParentData)Child.parentData!).offset = new Point(dx, dy);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        Size childSize = Child?.GetDryLayout(constraints.Loosen()) ?? default;
        return constraints.Constrain(childSize);
    }
}

internal interface CupertinoMenuSwipeTarget
{
    bool SwipeEnter();

    void SwipeExit();

    void SwipeComplete();
}

internal sealed class CupertinoMenuSwipeRegion : StatefulWidget
{
    public CupertinoMenuSwipeRegion(
        bool enabled,
        Action<double> onDistanceChanged,
        Widget child,
        Key? key = null) : base(key)
    {
        Enabled = enabled;
        OnDistanceChanged = onDistanceChanged;
        Child = child;
    }

    public bool Enabled { get; }

    public Action<double> OnDistanceChanged { get; }

    public Widget Child { get; }

    public override State CreateState() => new CupertinoMenuSwipeRegionState();
}

internal sealed class CupertinoMenuSwipeRegionState : State
{
    private readonly List<CupertinoMenuSwipeSurfaceState> _surfaces = [];
    private readonly List<CupertinoMenuSwipeTarget> _targets = [];
    private bool _isSwiping;

    private CupertinoMenuSwipeRegion Current => (CupertinoMenuSwipeRegion)StateWidget;

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (CupertinoMenuSwipeRegion)oldWidget;
        if (previous.Enabled && !Current.Enabled)
        {
            EndSwipe(complete: false);
        }
    }

    public override Widget Build(BuildContext context) => new CupertinoMenuSwipeRegionScope(this, Current.Child);

    internal void Attach(CupertinoMenuSwipeSurfaceState surface)
    {
        if (!_surfaces.Contains(surface))
        {
            _surfaces.Add(surface);
        }
    }

    internal void Detach(CupertinoMenuSwipeSurfaceState surface) => _surfaces.Remove(surface);

    internal Drag? BeginSwipe(Point position)
    {
        if (!Current.Enabled || _isSwiping)
        {
            return null;
        }

        _isSwiping = true;
        return new CupertinoMenuSwipeDrag(this, position);
    }

    internal void UpdateSwipe(Point position)
    {
        double distance = _surfaces
            .Select(surface => surface.DistanceTo(position))
            .DefaultIfEmpty(0.0)
            .Min();
        Current.OnDistanceChanged(Math.Clamp(distance, 0.0, 150.0));
        UpdateTargets(position);
    }

    internal void EndSwipe(bool complete)
    {
        foreach (CupertinoMenuSwipeTarget target in _targets.ToArray())
        {
            target.SwipeExit();
            if (complete)
            {
                target.SwipeComplete();
            }
        }

        _targets.Clear();
        _isSwiping = false;
        Current.OnDistanceChanged(0.0);
    }

    private void UpdateTargets(Point position)
    {
        List<CupertinoMenuSwipeTarget> next = HitTestTargets(position);
        foreach (CupertinoMenuSwipeTarget target in _targets.Except(next).ToArray())
        {
            target.SwipeExit();
        }

        foreach (CupertinoMenuSwipeTarget target in next.Except(_targets).Reverse())
        {
            target.SwipeEnter();
        }

        _targets.Clear();
        _targets.AddRange(next);
    }

    private List<CupertinoMenuSwipeTarget> HitTestTargets(Point position)
    {
        var targets = new List<CupertinoMenuSwipeTarget>();
        RenderObject? renderObject = Context.FindRenderObject();
        while (renderObject?.Parent is not null)
        {
            renderObject = renderObject.Parent;
        }

        if (renderObject is not RenderView root)
        {
            return targets;
        }

        var result = new BoxHitTestResult();
        root.HitTest(result, position);
        foreach (HitTestEntry entry in result.Path)
        {
            if (entry.Target is RenderMetaData { MetaData: CupertinoMenuSwipeTarget target })
            {
                targets.Add(target);
            }
        }

        return targets;
    }
}

internal sealed class CupertinoMenuSwipeRegionScope : InheritedWidget
{
    public CupertinoMenuSwipeRegionScope(CupertinoMenuSwipeRegionState state, Widget child) : base()
    {
        State = state;
        Child = child;
    }

    public CupertinoMenuSwipeRegionState State { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !ReferenceEquals(((CupertinoMenuSwipeRegionScope)oldWidget).State, State);
}

internal sealed class CupertinoMenuSwipeSurface : StatefulWidget
{
    public CupertinoMenuSwipeSurface(
        Widget child,
        TimeSpan? delay = null,
        Action<Point>? onStart = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Delay = delay;
        OnStart = onStart;
    }

    public Widget Child { get; }

    public TimeSpan? Delay { get; }

    public Action<Point>? OnStart { get; }

    public override State CreateState() => new CupertinoMenuSwipeSurfaceState();
}

internal sealed class CupertinoMenuSwipeSurfaceState : State
{
    private CupertinoMenuSwipeRegionState? _region;

    private CupertinoMenuSwipeSurface Current => (CupertinoMenuSwipeSurface)StateWidget;

    public override void DidChangeDependencies()
    {
        CupertinoMenuSwipeRegionState? next =
            Context.DependOnInherited<CupertinoMenuSwipeRegionScope>()?.State;
        if (ReferenceEquals(next, _region))
        {
            return;
        }

        _region?.Detach(this);
        _region = next;
        _region?.Attach(this);
    }

    public override Widget Build(BuildContext context)
    {
        IGestureRecognizerFactory factory = Current.Delay.HasValue
            ? new GestureRecognizerFactoryWithHandlers<DelayedMultiDragGestureRecognizer>(
                () => new DelayedMultiDragGestureRecognizer(Current.Delay),
                recognizer => recognizer.OnStart = HandleStart)
            : new GestureRecognizerFactoryWithHandlers<ImmediateMultiDragGestureRecognizer>(
                () => new ImmediateMultiDragGestureRecognizer(),
                recognizer => recognizer.OnStart = HandleStart);
        Type type = Current.Delay.HasValue
            ? typeof(DelayedMultiDragGestureRecognizer)
            : typeof(ImmediateMultiDragGestureRecognizer);
        return new RawGestureDetector(
            gestures: new Dictionary<Type, IGestureRecognizerFactory> { [type] = factory },
            behavior: HitTestBehavior.Opaque,
            child: Current.Child);
    }

    public override void Dispose()
    {
        _region?.Detach(this);
        _region = null;
    }

    internal double DistanceTo(Point position)
    {
        if (Context.FindRenderObject() is not RenderBox box || !box.HasSize)
        {
            return double.PositiveInfinity;
        }

        Point topLeft = box.LocalToGlobal(default);
        var rect = new Rect(topLeft, box.Size);
        double dx = Math.Max(Math.Max(rect.Left - position.X, 0.0), position.X - rect.Right);
        double dy = Math.Max(Math.Max(rect.Top - position.Y, 0.0), position.Y - rect.Bottom);
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private Drag? HandleStart(Point position)
    {
        Current.OnStart?.Invoke(position);
        return _region?.BeginSwipe(position);
    }
}

internal sealed class CupertinoMenuSwipeDrag : Drag
{
    private readonly CupertinoMenuSwipeRegionState _region;

    public CupertinoMenuSwipeDrag(CupertinoMenuSwipeRegionState region, Point initialPosition)
    {
        _region = region;
        _region.UpdateSwipe(initialPosition);
    }

    public override void Update(DragUpdateDetails details) => _region.UpdateSwipe(details.GlobalPosition);

    public override void End(DragEndDetails details) => _region.EndSwipe(complete: true);

    public override void Cancel() => _region.EndSwipe(complete: false);
}
