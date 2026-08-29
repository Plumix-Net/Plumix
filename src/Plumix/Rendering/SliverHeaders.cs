using Avalonia;
using Avalonia.Media;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/sliver_resizing_header.dart
// flutter/packages/flutter/lib/src/widgets/sliver_floating_header.dart

internal sealed class RenderSliverResizingHeader : RenderSliver
{
    private RenderBox? _minExtentPrototype;
    private RenderBox? _maxExtentPrototype;
    private RenderBox? _child;

    public RenderBox? MinExtentPrototype
    {
        get => _minExtentPrototype;
        set => ReplaceChild(ref _minExtentPrototype, value);
    }

    public RenderBox? MaxExtentPrototype
    {
        get => _maxExtentPrototype;
        set => ReplaceChild(ref _maxExtentPrototype, value);
    }

    public RenderBox? Child
    {
        get => _child;
        set => ReplaceChild(ref _child, value);
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not BoxParentData)
        {
            child.parentData = new BoxParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_minExtentPrototype != null)
        {
            visitor(_minExtentPrototype);
        }

        if (_maxExtentPrototype != null)
        {
            visitor(_maxExtentPrototype);
        }

        if (_child != null)
        {
            visitor(_child);
        }
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        return 0.0;
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        BoxConstraints prototypeConstraints = constraints.AsBoxConstraints();
        double minExtent = 0.0;
        if (_minExtentPrototype != null)
        {
            _minExtentPrototype.Layout(prototypeConstraints, parentUsesSize: true);
            minExtent = BoxExtent(_minExtentPrototype, constraints.Axis);
        }

        double maxExtent;
        if (_maxExtentPrototype != null)
        {
            _maxExtentPrototype.Layout(prototypeConstraints, parentUsesSize: true);
            maxExtent = BoxExtent(_maxExtentPrototype, constraints.Axis);
        }
        else if (_child != null)
        {
            Size childSize = _child.GetDryLayout(prototypeConstraints);
            maxExtent = constraints.Axis == Axis.Vertical ? childSize.Height : childSize.Width;
        }
        else
        {
            maxExtent = 0.0;
        }

        double shrinkOffset = Math.Min(constraints.ScrollOffset, maxExtent);
        if (_child != null)
        {
            _child.Layout(
                constraints.AsBoxConstraints(
                    minExtent: minExtent,
                    maxExtent: Math.Max(minExtent, maxExtent - shrinkOffset)),
                parentUsesSize: true);
        }

        double childExtent = _child == null ? 0.0 : BoxExtent(_child, constraints.Axis);
        double remainingPaintExtent = constraints.RemainingPaintExtent;
        double layoutExtent = Math.Min(childExtent, maxExtent - constraints.ScrollOffset);
        Geometry = new SliverGeometry(
            ScrollExtent: maxExtent,
            PaintOrigin: constraints.Overlap,
            PaintExtent: Math.Min(childExtent, remainingPaintExtent),
            LayoutExtent: Math.Clamp(layoutExtent, 0.0, remainingPaintExtent),
            MaxPaintExtent: childExtent,
            MaxScrollObstructionExtent: minExtent,
            CacheExtent: CalculateCacheOffset(constraints, from: 0.0, to: childExtent),
            HasVisualOverflow: true);

        if (_child != null)
        {
            ((BoxParentData)_child.parentData!).offset = PinnedChildOffset(
                constraints,
                childExtent,
                Geometry.PaintExtent);
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);

        // A partially collapsed header must not let its children participate in the scrollable's
        // implicit scrolling, or the viewport would try to scroll them into view.
        double childExtent = _child == null ? 0.0 : BoxExtent(_child, ConstraintsForSliver.Axis);
        if (Geometry.LayoutExtent < childExtent)
        {
            configuration.AddTagForChildren(RenderViewport.ExcludeFromScrolling);
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child == null || !Geometry.Visible)
        {
            return;
        }

        ctx.PaintChild(_child, offset + ((BoxParentData)_child.parentData!).offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (_child == null || Geometry.HitTestExtent <= 0.0)
        {
            return false;
        }

        Point childOffset = ((BoxParentData)_child.parentData!).offset;
        RenderBox child = _child;
        return result.AddWithPaintOffset(
            childOffset,
            position,
            (hitResult, transformed) => child.HitTest(hitResult, transformed));
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_child != null && Geometry.Visible)
        {
            visitor(_child);
        }
    }

    private void ReplaceChild(ref RenderBox? current, RenderBox? value)
    {
        if (ReferenceEquals(current, value))
        {
            return;
        }

        if (current != null)
        {
            DropChild(current);
        }

        current = value;
        if (current != null)
        {
            AdoptChild(current);
        }

        MarkNeedsLayout();
    }

    private static double BoxExtent(RenderBox box, Axis axis)
    {
        return axis == Axis.Vertical ? box.Size.Height : box.Size.Width;
    }

    private static Point PinnedChildOffset(
        SliverConstraints constraints,
        double childExtent,
        double paintExtent)
    {
        AxisDirection direction = ApplyGrowthDirection(constraints.AxisDirection, constraints.GrowthDirection);
        return direction switch
        {
            AxisDirection.Up => new Point(0.0, paintExtent - childExtent),
            AxisDirection.Left => new Point(paintExtent - childExtent, 0.0),
            _ => default
        };
    }

    private static AxisDirection ApplyGrowthDirection(AxisDirection direction, GrowthDirection growthDirection)
    {
        if (growthDirection == GrowthDirection.Forward)
        {
            return direction;
        }

        return direction switch
        {
            AxisDirection.Up => AxisDirection.Down,
            AxisDirection.Right => AxisDirection.Left,
            AxisDirection.Down => AxisDirection.Up,
            AxisDirection.Left => AxisDirection.Right,
            _ => direction
        };
    }
}

internal sealed class RenderSliverFloatingHeader : RenderSliverSingleBoxAdapter
{
    private AnimationStyle? _animationStyle;
    private FloatingHeaderSnapMode? _snapMode;
    private AnimationController? _snapController;
    private double _snapBegin;
    private double _snapEnd;
    private double? _lastScrollOffset;
    private double _effectiveScrollOffset;

    public RenderSliverFloatingHeader(
        AnimationStyle? animationStyle = null,
        FloatingHeaderSnapMode? snapMode = null,
        RenderBox? child = null)
    {
        _animationStyle = animationStyle;
        _snapMode = snapMode;
        Child = child;
    }

    public AnimationStyle? AnimationStyle
    {
        get => _animationStyle;
        set => _animationStyle = value;
    }

    public FloatingHeaderSnapMode? SnapMode
    {
        get => _snapMode;
        set
        {
            if (_snapMode == value)
            {
                return;
            }

            _snapMode = value;
            MarkNeedsLayout();
        }
    }

    internal double EffectiveScrollOffset => _effectiveScrollOffset;

    internal double ChildExtent
    {
        get
        {
            if (Child == null)
            {
                return 0.0;
            }

            return ConstraintsForSliver.Axis == Axis.Vertical ? Child.Size.Height : Child.Size.Width;
        }
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        return Math.Min(0.0, Geometry.PaintExtent - ChildExtent);
    }

    /// <summary>
    /// How far a reveal request may expand this header, or null to let the enclosing viewport scroll
    /// the request into view instead.
    /// </summary>
    internal PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration { get; set; }

    public override void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        if (ShowOnScreenConfiguration is not { } configuration || Child is null)
        {
            base.ShowOnScreen(descendant, rect, duration, curve);
            return;
        }

        SliverConstraints constraints = ConstraintsForSliver;
        double childExtent = ChildExtent;
        Rect? childBounds = descendant != null
            ? RenderObject.TransformRect(
                descendant.GetTransformTo(Child),
                rect ?? descendant.PaintBounds)
            : rect;

        AxisDirection effectiveDirection = PersistentHeaderReveal.EffectiveAxisDirection(constraints);
        double targetExtent;
        Rect? targetRect;
        switch (effectiveDirection)
        {
            case AxisDirection.Up:
                targetExtent = childExtent - (childBounds?.Top ?? 0.0);
                targetRect = PersistentHeaderReveal.Trim(childBounds, bottom: childExtent);
                break;
            case AxisDirection.Right:
                targetExtent = childBounds?.Right ?? childExtent;
                targetRect = PersistentHeaderReveal.Trim(childBounds, left: 0.0);
                break;
            case AxisDirection.Left:
                targetExtent = childExtent - (childBounds?.Left ?? 0.0);
                targetRect = PersistentHeaderReveal.Trim(childBounds, right: childExtent);
                break;
            default:
                targetExtent = childBounds?.Bottom ?? childExtent;
                targetRect = PersistentHeaderReveal.Trim(childBounds, top: 0.0);
                break;
        }

        // The header only ever grows to satisfy a reveal; it never contracts.
        targetExtent = Math.Clamp(
            Math.Clamp(
                targetExtent,
                configuration.MinShowOnScreenExtent,
                configuration.MaxShowOnScreenExtent),
            Geometry.PaintExtent,
            childExtent);

        if (targetExtent > Geometry.PaintExtent && !IsSnapping)
        {
            StartSnap(childExtent - targetExtent, ResolveSnapDuration(duration), ResolveSnapCurve(curve));
        }

        base.ShowOnScreen(
            descendant: descendant is null ? this : Child,
            rect: targetRect,
            duration: duration,
            curve: curve);
    }

    internal void IsScrollingUpdate(ScrollPosition position)
    {
        if (position.IsScrollingNotifier.Value)
        {
            _snapController?.Stop();
            return;
        }

        ScrollDirection direction = position.UserScrollDirection;
        bool partiallyVisible = direction switch
        {
            ScrollDirection.Forward when _effectiveScrollOffset <= 0.0 => false,
            ScrollDirection.Reverse when _effectiveScrollOffset >= ChildExtent => false,
            _ => true
        };
        if (!partiallyVisible)
        {
            return;
        }

        double target = direction == ScrollDirection.Forward ? 0.0 : ChildExtent;
        TimeSpan duration = direction == ScrollDirection.Forward
            ? _animationStyle?.Duration ?? TimeSpan.FromMilliseconds(300)
            : _animationStyle?.ReverseDuration ?? TimeSpan.FromMilliseconds(300);
        Curve curve = direction == ScrollDirection.Forward
            ? _animationStyle?.Curve ?? Curves.EaseInOut
            : _animationStyle?.ReverseCurve ?? Curves.EaseInOut;
        StartSnap(target, duration, curve);
    }

    protected override void OnDetach()
    {
        DisposeSnapController();
        base.OnDetach();
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        bool floatingHeaderNeedsUpdate = _lastScrollOffset.HasValue
            && (constraints.ScrollOffset < _lastScrollOffset.Value
                || _effectiveScrollOffset < ChildExtent);

        if (!floatingHeaderNeedsUpdate)
        {
            _effectiveScrollOffset = constraints.ScrollOffset;
        }
        else
        {
            double delta = _lastScrollOffset!.Value - constraints.ScrollOffset;
            if (constraints.UserScrollDirection == ScrollDirection.Forward)
            {
                if (_effectiveScrollOffset > ChildExtent)
                {
                    _effectiveScrollOffset = ChildExtent;
                }
            }
            else
            {
                delta = Math.Clamp(delta, double.NegativeInfinity, 0.0);
            }

            _effectiveScrollOffset = Math.Clamp(
                _effectiveScrollOffset - delta,
                0.0,
                constraints.ScrollOffset);
        }

        Child?.Layout(constraints.AsBoxConstraints(), parentUsesSize: true);
        double childExtent = ChildExtent;
        double paintExtent = childExtent - _effectiveScrollOffset;
        double layoutExtent = (_snapMode ?? FloatingHeaderSnapMode.Overlay) switch
        {
            FloatingHeaderSnapMode.Overlay => childExtent - constraints.ScrollOffset,
            FloatingHeaderSnapMode.Scroll => paintExtent,
            _ => throw new ArgumentOutOfRangeException()
        };
        Geometry = new SliverGeometry(
            PaintOrigin: Math.Min(constraints.Overlap, 0.0),
            ScrollExtent: childExtent,
            PaintExtent: Math.Clamp(paintExtent, 0.0, constraints.RemainingPaintExtent),
            LayoutExtent: Math.Clamp(layoutExtent, 0.0, constraints.RemainingPaintExtent),
            MaxPaintExtent: childExtent,
            HasVisualOverflow: true);

        if (Child != null)
        {
            ((BoxParentData)Child.parentData!).offset = FloatingChildOffset(
                constraints,
                childExtent,
                Geometry.PaintExtent);
        }

        _lastScrollOffset = constraints.ScrollOffset;
    }

    /// <summary>Whether a snap animation is already running towards a larger extent.</summary>
    private bool IsSnapping => _snapController is { IsAnimating: true } && _snapEnd < _snapBegin;

    private TimeSpan ResolveSnapDuration(TimeSpan requested)
    {
        if (requested > TimeSpan.Zero)
        {
            return requested;
        }

        return AnimationStyle?.Duration ?? TimeSpan.Zero;
    }

    private Curve ResolveSnapCurve(Curve? requested)
    {
        return requested ?? AnimationStyle?.Curve ?? Curves.Ease;
    }

    private void StartSnap(double target, TimeSpan duration, Curve curve)
    {
        _snapBegin = _effectiveScrollOffset;
        _snapEnd = target;
        if (duration <= TimeSpan.Zero)
        {
            _effectiveScrollOffset = target;
            MarkNeedsLayout();
            return;
        }

        if (_snapController == null)
        {
            _snapController = new AnimationController(duration: duration);
            _snapController.AddListener(HandleSnapTick);
        }

        _snapController.Stop();
        _snapController.Duration = duration;
        _snapController.Curve = curve;
        _snapController.Forward(from: 0.0);
    }

    private void HandleSnapTick()
    {
        if (_snapController == null)
        {
            return;
        }

        double next = _snapBegin + ((_snapEnd - _snapBegin) * _snapController.Value);
        if (Math.Abs(_effectiveScrollOffset - next) <= 0.000001)
        {
            return;
        }

        _effectiveScrollOffset = next;
        MarkNeedsLayout();
    }

    private void DisposeSnapController()
    {
        if (_snapController == null)
        {
            return;
        }

        _snapController.RemoveListener(HandleSnapTick);
        _snapController.Dispose();
        _snapController = null;
    }

    private static Point FloatingChildOffset(
        SliverConstraints constraints,
        double childExtent,
        double paintExtent)
    {
        AxisDirection direction = constraints.GrowthDirection == GrowthDirection.Forward
            ? constraints.AxisDirection
            : constraints.AxisDirection switch
            {
                AxisDirection.Up => AxisDirection.Down,
                AxisDirection.Right => AxisDirection.Left,
                AxisDirection.Down => AxisDirection.Up,
                AxisDirection.Left => AxisDirection.Right,
                _ => constraints.AxisDirection
            };
        double mainAxisPosition = Math.Min(0.0, paintExtent - childExtent);
        return direction switch
        {
            AxisDirection.Up => new Point(0.0, paintExtent - mainAxisPosition - childExtent),
            AxisDirection.Left => new Point(paintExtent - mainAxisPosition - childExtent, 0.0),
            AxisDirection.Right => new Point(mainAxisPosition, 0.0),
            AxisDirection.Down => new Point(0.0, mainAxisPosition),
            _ => default
        };
    }
}
