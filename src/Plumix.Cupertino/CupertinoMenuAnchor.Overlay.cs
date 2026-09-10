using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/menu_anchor.dart

/// <summary>Dart parity source: <c>_MenuOverlay</c>.</summary>
internal sealed class CupertinoMenuOverlay : StatefulWidget
{
    public CupertinoMenuOverlay(
        IReadOnlyList<Widget> children,
        FocusScopeNode focusScopeNode,
        bool consumeOutsideTaps,
        bool constrainCrossAxis,
        BoxConstraints? constraints,
        Size overlaySize,
        EdgeInsetsGeometry overlayPadding,
        Rect anchorRect,
        Vector? anchorPosition,
        object tapRegionGroupId,
        Animation<double> visibilityAnimation,
        IValueListenable<double> swipeDistanceListenable,
        Key? key = null) : base(key)
    {
        Children = children;
        FocusScopeNode = focusScopeNode;
        ConsumeOutsideTaps = consumeOutsideTaps;
        ConstrainCrossAxis = constrainCrossAxis;
        Constraints = constraints;
        OverlaySize = overlaySize;
        OverlayPadding = overlayPadding;
        AnchorRect = anchorRect;
        AnchorPosition = anchorPosition;
        TapRegionGroupId = tapRegionGroupId;
        VisibilityAnimation = visibilityAnimation;
        SwipeDistanceListenable = swipeDistanceListenable;
    }

    public IReadOnlyList<Widget> Children { get; }

    public FocusScopeNode FocusScopeNode { get; }

    public bool ConsumeOutsideTaps { get; }

    public bool ConstrainCrossAxis { get; }

    public BoxConstraints? Constraints { get; }

    public Size OverlaySize { get; }

    public EdgeInsetsGeometry OverlayPadding { get; }

    public Rect AnchorRect { get; }

    public Vector? AnchorPosition { get; }

    public object TapRegionGroupId { get; }

    public Animation<double> VisibilityAnimation { get; }

    public IValueListenable<double> SwipeDistanceListenable { get; }

    public override State CreateState() => new CupertinoMenuOverlayState();
}

/// <summary>Dart parity source: <c>_MenuOverlayState</c>.</summary>
internal sealed class CupertinoMenuOverlayState : State, WidgetsBindingObserver
{
    private static readonly Vector KAttachmentOffset = new(0.0, 8.0);

    private static readonly IReadOnlyDictionary<ShortcutActivator, Intent> KMenuTraversalShortcuts =
        new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = new CupertinoMenuFocusUpIntent(),
            [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = new CupertinoMenuFocusDownIntent(),
            [new SingleActivator(LogicalKeyboardKey.Home)] = new CupertinoMenuFocusFirstIntent(),
            [new SingleActivator(LogicalKeyboardKey.End)] = new CupertinoMenuFocusLastIntent(),
        };

    private static readonly IReadOnlyDictionary<Type, FlutterAction> KActions =
        new Dictionary<Type, FlutterAction>
        {
            [typeof(CupertinoMenuFocusDownIntent)] = new CupertinoMenuFocusDownAction(),
            [typeof(CupertinoMenuFocusUpIntent)] = new CupertinoMenuFocusUpAction(),
            [typeof(CupertinoMenuFocusFirstIntent)] = new CupertinoMenuFocusFirstAction(),
            [typeof(CupertinoMenuFocusLastIntent)] = new CupertinoMenuFocusLastAction(),
        };

    private readonly ScrollController _scrollController = new();
    private readonly ProxyAnimation _scaleAnimation = new();
    private readonly ProxyAnimation _fadeAnimation = new();
    private readonly ProxyAnimation _sizeAnimation = new();
    private AnimationController _swipeAnimationController = null!;
    private Alignment _attachmentPointAlignment;
    private Point _attachmentPoint;
    private Alignment _menuAlignment;
    private IReadOnlyList<Widget> _children = [];
    private TextDirection? _textDirection;
    private double _swipeTargetDistance;
    private double _swipeCurrentDistance;
    private double _swipeVelocity;
    private Ticker? _swipeTicker;

    private CupertinoMenuOverlay Current => (CupertinoMenuOverlay)StateWidget;

    internal Animation<double> ScaleAnimation => _scaleAnimation;

    internal Animation<double> FadeAnimation => _fadeAnimation;

    internal Animation<double> SizeAnimation => _sizeAnimation;

    public override void InitState()
    {
        WidgetsBinding.Instance.AddObserver(this);
        _swipeAnimationController = AnimationController.Unbounded(value: 1.0, vsync: this);
        Current.SwipeDistanceListenable.AddListener(HandleSwipeDistanceChanged);
        ResolveChildren();
    }

    public override void DidChangeDependencies()
    {
        TextDirection newTextDirection = Directionality.Of(Context);
        if (_textDirection != newTextDirection)
        {
            _textDirection = newTextDirection;
            ResolvePosition();
        }

        ResolveMotion();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (CupertinoMenuOverlay)oldWidget;
        if (!ReferenceEquals(previous.SwipeDistanceListenable, Current.SwipeDistanceListenable))
        {
            previous.SwipeDistanceListenable.RemoveListener(HandleSwipeDistanceChanged);
            Current.SwipeDistanceListenable.AddListener(HandleSwipeDistanceChanged);
        }

        if (!ReferenceEquals(previous.VisibilityAnimation, Current.VisibilityAnimation))
        {
            ResolveMotion();
        }

        if (previous.AnchorRect != Current.AnchorRect
            || previous.AnchorPosition != Current.AnchorPosition
            || previous.OverlaySize != Current.OverlaySize)
        {
            ResolvePosition();
        }

        if (!ReferenceEquals(previous.Children, Current.Children))
        {
            ResolveChildren();
        }
    }

    public void DidChangeAccessibilityFeatures() => ResolveMotion();

    public override Widget Build(BuildContext context)
    {
        BoxConstraints constraints;
        if (Current.Constraints is { } explicitConstraints)
        {
            constraints = explicitConstraints;
        }
        else
        {
            bool isLargeTextModeEnabled = CupertinoMenuMetrics.LargeTextModeEnabled(context);
            double screenWidth = MediaQuery.WidthOf(context);
            CupertinoMenuWidth menuWidth = CupertinoMenuWidths.FromScreenWidth(
                screenWidth,
                isLargeTextModeEnabled);
            constraints = BoxConstraints.TightFor(width: menuWidth.Points());
        }

        Widget panel = new AnimatedBuilder(
            animation: _sizeAnimation,
            builder: BuildAlign,
            child: new Semantics(
                explicitChildNodes: true,
                scopesRoute: true,
                child: new ConstrainedBox(
                    constraints,
                    new SingleChildScrollView(
                        clipBehavior: Clip.None,
                        child: new Column(
                            mainAxisSize: MainAxisSize.Min,
                            children: _children)))));
        panel = new CupertinoPopupSurface(child: panel);
        panel = new FadeTransition(
            opacity: _fadeAnimation,
            alwaysIncludeSemantics: true,
            child: panel);
        panel = new CustomPaint(
            painter: new CupertinoMenuShadowPainter(
                CupertinoTheme.MaybeBrightnessOf(context) ?? PlatformBrightness.Light,
                _fadeAnimation),
            child: panel);
        panel = new FocusScope(
            focusScopeNode: Current.FocusScopeNode,
            descendantsAreFocusable: true,
            descendantsAreTraversable: true,
            canRequestFocus: true,
            child: panel);
        panel = new Shortcuts(shortcuts: KMenuTraversalShortcuts, child: panel);
        panel = new Actions(actions: KActions, child: panel);
        panel = new TapRegion(
            groupId: Current.TapRegionGroupId,
            consumeOutsideTaps: Current.ConsumeOutsideTaps,
            onTapOutside: HandleOutsideTap,
            child: panel);
        panel = new CupertinoMenuSwipeSurface(child: panel);

        if (!Current.ConstrainCrossAxis)
        {
            panel = new UnconstrainedBox(
                clipBehavior: Clip.HardEdge,
                alignment: AlignmentDirectional.CenterStart,
                constrainedAxis: Axis.Vertical,
                child: panel);
        }

        MediaQueryData? mediaQuery = MediaQuery.MaybeOf(context);
        IReadOnlyList<Rect> avoidBounds = mediaQuery is null
            ? []
            : DisplayFeatureSubScreen.AvoidBounds(mediaQuery);
        Thickness overlayPadding = Current.OverlayPadding.Resolve(_textDirection);

        Widget layout = new ValueListenableBuilder<double>(
            valueListenable: _sizeAnimation,
            child: panel,
            builder: (_, value, child) => new CustomSingleChildLayout(
                layoutDelegate: new CupertinoMenuLayoutDelegate(
                    anchorRect: Current.AnchorPosition.HasValue
                        ? new Rect(_attachmentPoint, new Size())
                        : Current.AnchorRect,
                    attachmentPoint: _attachmentPoint,
                    avoidBounds: avoidBounds,
                    heightFactor: value,
                    menuAlignment: _menuAlignment,
                    overlayPadding: overlayPadding),
                child: child));
        layout = new ScaleTransition(
            scale: _scaleAnimation,
            alignment: _attachmentPointAlignment,
            child: layout);
        return new ConstrainedBox(BoxConstraints.Loose(Current.OverlaySize), layout);
    }

    public override void Dispose()
    {
        _scrollController.Dispose();
        Current.SwipeDistanceListenable.RemoveListener(HandleSwipeDistanceChanged);
        _swipeTicker?.Stop();
        _swipeTicker?.Dispose();
        _swipeAnimationController.Stop();
        _swipeAnimationController.Dispose();
        _scaleAnimation.Parent = null;
        _fadeAnimation.Parent = null;
        _sizeAnimation.Parent = null;
        WidgetsBinding.Instance.RemoveObserver(this);

        base.Dispose();
    }

    /// <summary>Dart parity source: <c>_MenuOverlayState._resolveChildren</c>.</summary>
    private void ResolveChildren()
    {
        if (Current.Children.Count == 0)
        {
            _children = [];
            return;
        }

        var children = new List<Widget>();
        for (int index = 0; index < Current.Children.Count; index++)
        {
            Widget child = Current.Children[index];
            children.Add(child);
            if (index == Current.Children.Count - 1)
            {
                break;
            }

            bool previousIsDivider = child is CupertinoMenuEntry { IsDivider: true };
            bool nextIsDivider = Current.Children[index + 1] is CupertinoMenuEntry { IsDivider: true };
            if (previousIsDivider || nextIsDivider)
            {
                continue;
            }

            children.Add(new CupertinoMenuImplicitDivider());
        }

        _children = children;
    }

    /// <summary>Dart parity source: <c>_MenuOverlayState._resolveMotion</c>.</summary>
    private void ResolveMotion()
    {
        // Plumix has no per-view `platformDispatcher`; the binding-level flags stand in
        // (docs/ai/DIVERGENCES.md).
        AccessibilityFeatures features = WidgetsBinding.Instance.AccessibilityFeatures;
        if (features.DisableAnimations)
        {
            _scaleAnimation.Parent = CupertinoMenuAnimations.AlwaysComplete;
            _fadeAnimation.Parent = CupertinoMenuAnimations.AlwaysComplete;
            _sizeAnimation.Parent = CupertinoMenuAnimations.AlwaysComplete;
            return;
        }

        if (features.ReduceMotion)
        {
            _scaleAnimation.Parent = _swipeAnimationController.View
                .Drive(new DoubleTween(begin: 0.8, end: 1.0));
            _sizeAnimation.Parent = CupertinoMenuAnimations.AlwaysComplete;
            _fadeAnimation.Parent = Current.VisibilityAnimation.Drive(
                new CurveTween(Curves.EaseInCurve).Chain(new CupertinoMenuClampTween(0.0, 1.0)));
            return;
        }

        _scaleAnimation.Parent = new CupertinoMenuAnimationProduct(
            first: Current.VisibilityAnimation,
            next: _swipeAnimationController.View.Drive(new DoubleTween(begin: 0.8, end: 1.0)));
        _sizeAnimation.Parent = Current.VisibilityAnimation
            .Drive(new DoubleTween(begin: 0.8, end: 1.0));
        _fadeAnimation.Parent = Current.VisibilityAnimation.Drive(
            new CurveTween(Curves.EaseInCurve).Chain(new CupertinoMenuClampTween(0.0, 1.0)));
    }

    /// <summary>Dart parity source: <c>_MenuOverlayState._resolvePosition</c>.</summary>
    private void ResolvePosition()
    {
        Point anchorMidpoint = Current.AnchorPosition.HasValue
            ? Current.AnchorRect.TopLeft + Current.AnchorPosition.Value
            : Current.AnchorRect.Center;
        double xMidpointRatio = Current.OverlaySize.Width == 0.0
            ? 0.5
            : anchorMidpoint.X / Current.OverlaySize.Width;
        double yMidpointRatio = Current.OverlaySize.Height == 0.0
            ? 0.5
            : anchorMidpoint.Y / Current.OverlaySize.Height;
        double dy = yMidpointRatio < 0.55 ? 1.0 : -1.0;
        double dx = xMidpointRatio < 0.4 ? -1.0 : xMidpointRatio > 0.6 ? 1.0 : 0.0;
        _menuAlignment = new Alignment(dx, -dy);

        Point transformOrigin;
        if (Current.AnchorPosition.HasValue)
        {
            _attachmentPoint = Current.AnchorRect.TopLeft + Current.AnchorPosition.Value;
            transformOrigin = _attachmentPoint;
        }
        else
        {
            Vector offset = KAttachmentOffset * dy;
            _attachmentPoint = new Alignment(dx, dy).WithinRect(Current.AnchorRect) + offset;
            transformOrigin = new Alignment(0.0, dy).WithinRect(Current.AnchorRect) + offset;
        }

        _attachmentPointAlignment = new Alignment(
            Current.OverlaySize.Width == 0.0
                ? 0.0
                : (transformOrigin.X / Current.OverlaySize.Width * 2.0) - 1.0,
            Current.OverlaySize.Height == 0.0
                ? 0.0
                : (transformOrigin.Y / Current.OverlaySize.Height * 2.0) - 1.0);
    }

    private Widget BuildAlign(BuildContext context, Widget? child) => new Align(
        alignment: Alignment.TopCenter,
        heightFactor: _sizeAnimation.Value,
        widthFactor: 1.0,
        child: child);

    private void HandleOutsideTap(PointerDownEvent evt) => MenuController.MaybeOf(Context)!.Close();

    /// <summary>Dart parity source: <c>_MenuOverlayState._handleSwipeDistanceChanged</c>.</summary>
    private void HandleSwipeDistanceChanged()
    {
        _swipeTargetDistance = Math.Clamp(Current.SwipeDistanceListenable.Value, 0.0, 150.0);
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

    /// <summary>Dart parity source: <c>_MenuOverlayState._updateSwipeScale</c>.</summary>
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
        _swipeVelocity += accelerationRate * proximityFactor;
        _swipeVelocity = Math.Clamp(_swipeVelocity, minVelocity, maxVelocity);
        double finalVelocity = _swipeVelocity * proximityFactor;
        double distanceReduction = Math.Sign(distance) * finalVelocity;
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

        _swipeAnimationController.SetValue(1.0 - (_swipeCurrentDistance / maxSwipeDistance));
    }
}

/// <summary>
/// Dart's <c>kAlwaysCompleteAnimation</c>, the constant animation the accessibility branches of
/// <c>_resolveMotion</c> substitute for the real ones.
/// </summary>
internal static class CupertinoMenuAnimations
{
    internal static Animation<double> AlwaysComplete { get; } =
        new ConstantAnimation<double>(1.0, AnimationStatus.Completed);
}

/// <summary>Dart parity source: <c>_ShadowPainter</c>.</summary>
internal sealed class CupertinoMenuShadowPainter : CustomPainter
{
    private static readonly Radius KRadius = Radius.Circular(13.0);

    private const double KShadowOpacity = 0.12;

    public CupertinoMenuShadowPainter(PlatformBrightness brightness, Animation<double> repaint)
        : base(repaint)
    {
        Brightness = brightness;
        RepaintAnimation = repaint;
    }

    public PlatformBrightness Brightness { get; }

    public Animation<double> RepaintAnimation { get; }

    public double ShadowAnimation => Math.Clamp(RepaintAnimation.Value, 0.0, 1.0);

    public override void Paint(PaintingContext context, Size size)
    {
        double shadowAnimation = ShadowAnimation;
        var rect = new Rect(default, size);
        RSuperellipse roundedRect = RSuperellipse.FromRectAndRadius(rect, KRadius);
        double blurSigma = shadowAnimation * 50.0;
        Color shadowColor = Color.FromArgb(
            (byte)Math.Round(shadowAnimation * shadowAnimation * KShadowOpacity * 255.0),
            0,
            0,
            10);

        var maskPath = new Plumix.UI.Path { FillType = PathFillType.EvenOdd };
        maskPath.AddRect(rect.Inflate(200.0));
        maskPath.AddRRect(RRect.FromRectAndRadius(rect, KRadius));

        Canvas canvas = context.Canvas;
        canvas.Save();
        canvas.ClipPath(maskPath);
        canvas.DrawRSuperellipseBlur(roundedRect.Inflate(50.0), shadowColor, blurSigma);
        canvas.Restore();
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) =>
        oldDelegate is not CupertinoMenuShadowPainter old
        || old.Brightness != Brightness
        || !ReferenceEquals(old.RepaintAnimation, RepaintAnimation);
}

/// <summary>Dart parity source: <c>_MenuLayoutDelegate</c>.</summary>
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

    private Rect ClosestScreen(Size size)
    {
        List<Rect> screens = DisplayFeatureSubScreen.SubScreensInBounds(new Rect(default, size), AvoidBounds);
        Point anchor = AnchorRect.Center;
        Rect closest = screens.Count == 0 ? new Rect(default, size) : screens[0];
        double closestDistance = CupertinoMenuMetrics.ComputeSquaredDistanceToRect(anchor, closest);
        foreach (Rect screen in screens.Skip(1))
        {
            double distance = CupertinoMenuMetrics.ComputeSquaredDistanceToRect(anchor, screen);
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
}
