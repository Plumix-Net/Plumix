using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver_floating_header.dart

namespace Plumix.Widgets;

/// <summary>
/// Specifies how a partially visible <see cref="SliverFloatingHeader"/> animates into a view when a
/// user scroll gesture ends.
/// </summary>
public enum FloatingHeaderSnapMode
{
    /// <summary>
    /// At the end of a user scroll gesture, the <see cref="SliverFloatingHeader"/> will animate into
    /// the viewport in front of the scrolled content: the header overlaps the scrollable's other
    /// slivers.
    /// </summary>
    Overlay,

    /// <summary>
    /// At the end of a user scroll gesture, the <see cref="SliverFloatingHeader"/> will animate into
    /// the viewport and push the scrollable's other slivers out of the way.
    /// </summary>
    Scroll,
}

/// <summary>
/// A sliver that shows its <see cref="Child"/> when the user scrolls forward and hides it when the
/// user scrolls backwards.
/// </summary>
/// <remarks>
/// When the user stops scrolling while the header is partially visible, it animates into or out of
/// view, depending on the scroll direction, with <see cref="AnimationStyle"/>'s curve and duration
/// (300 ms and <see cref="Curves.EaseInOut"/> by default).
/// </remarks>
public sealed class SliverFloatingHeader : StatefulWidget
{
    /// <summary>Create a floating header sliver that animates into view when the user scrolls forward.</summary>
    public SliverFloatingHeader(
        Widget child,
        AnimationStyle? animationStyle = null,
        FloatingHeaderSnapMode? snapMode = null,
        Key? key = null) : base(key)
    {
        AnimationStyle = animationStyle;
        SnapMode = snapMode;
        Child = child;
    }

    /// <summary>
    /// Non-null properties override the default durations (300 ms) and curves
    /// (<see cref="Curves.EaseInOut"/>) used by the animation that shows and hides the header.
    /// </summary>
    public AnimationStyle? AnimationStyle { get; }

    /// <summary>
    /// Specifies how a partially visible <see cref="SliverFloatingHeader"/> animates into a view when
    /// a user scroll gesture ends. The default is <see cref="FloatingHeaderSnapMode.Overlay"/>.
    /// </summary>
    public FloatingHeaderSnapMode? SnapMode { get; }

    /// <summary>The widget contained by this sliver.</summary>
    public Widget Child { get; }

    public override State CreateState() => new SliverFloatingHeaderState();
}

/// <summary>Dart's private <c>_SliverFloatingHeaderState</c>.</summary>
/// <remarks>
/// Dart mixes in <c>SingleTickerProviderStateMixin</c>; every Plumix <see cref="State"/> is already a
/// ticker provider (see DIVERGENCES.md).
/// </remarks>
internal sealed class SliverFloatingHeaderState : State<SliverFloatingHeader>
{
    public ScrollPosition? Position { get; set; }

    public override Widget Build(BuildContext context)
    {
        return new SliverFloatingHeaderRenderObjectWidget(
            vsync: this,
            animationStyle: Widget.AnimationStyle,
            snapMode: Widget.SnapMode,
            child: new SnapTrigger(Widget.Child));
    }
}

/// <summary>Dart's private <c>_SnapTrigger</c>.</summary>
internal sealed class SnapTrigger : StatefulWidget
{
    public SnapTrigger(Widget child)
    {
        Child = child;
    }

    public Widget Child { get; }

    public override State CreateState() => new SnapTriggerState();
}

/// <summary>Dart's private <c>_SnapTriggerState</c>.</summary>
internal sealed class SnapTriggerState : State<SnapTrigger>
{
    private ScrollPosition? _position;

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _position?.IsScrollingNotifier.RemoveListener(IsScrollingListener);

        _position = Scrollable.MaybeOf(Context)?.Position;
        _position?.IsScrollingNotifier.AddListener(IsScrollingListener);
    }

    public override void Dispose()
    {
        _position?.IsScrollingNotifier.RemoveListener(IsScrollingListener);

        base.Dispose();
    }

    // Called when the sliver starts or ends scrolling.
    private void IsScrollingListener()
    {
        DebugAssertions.Assert(_position != null);
        RenderSliverFloatingHeader? renderer = Context.FindAncestorRenderObjectOfType<RenderSliverFloatingHeader>();
        renderer?.IsScrollingUpdate(_position!);
    }

    public override Widget Build(BuildContext context) => Widget.Child;
}

/// <summary>Dart's private <c>_SliverFloatingHeader</c>.</summary>
internal sealed class SliverFloatingHeaderRenderObjectWidget : SingleChildRenderObjectWidget
{
    public SliverFloatingHeaderRenderObjectWidget(
        ITickerProvider? vsync = null,
        AnimationStyle? animationStyle = null,
        FloatingHeaderSnapMode? snapMode = null,
        Widget? child = null) : base(child)
    {
        Vsync = vsync;
        AnimationStyle = animationStyle;
        SnapMode = snapMode;
    }

    public ITickerProvider? Vsync { get; }

    public AnimationStyle? AnimationStyle { get; }

    public FloatingHeaderSnapMode? SnapMode { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFloatingHeader(
            vsync: Vsync,
            animationStyle: AnimationStyle,
            snapMode: SnapMode);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var header = (RenderSliverFloatingHeader)renderObject;
        header.Vsync = Vsync;
        header.AnimationStyle = AnimationStyle;
        header.SnapMode = SnapMode;
    }
}

/// <summary>Dart's private <c>_RenderSliverFloatingHeader</c>.</summary>
internal sealed class RenderSliverFloatingHeader : RenderSliverSingleBoxAdapter
{
    private ITickerProvider? _vsync;
    private Animation<double>? _snapAnimation;
    private AnimationController? _snapController;
    private double? _lastScrollOffset;

    public RenderSliverFloatingHeader(
        ITickerProvider? vsync = null,
        AnimationStyle? animationStyle = null,
        FloatingHeaderSnapMode? snapMode = null)
    {
        _vsync = vsync;
        AnimationStyle = animationStyle;
        SnapMode = snapMode;
    }

    /// <summary>
    /// The distance from the start of the header to the start of the viewport. When the header is
    /// showing it varies between 0 (completely visible) and <see cref="ChildExtent"/> (not visible
    /// because it's just above the viewport's starting edge). It's used to compute the header's
    /// paint extent, which defines where the header will appear — see <see cref="Paint"/>.
    /// </summary>
    internal double EffectiveScrollOffset { get; private set; }

    public ITickerProvider? Vsync
    {
        get => _vsync;
        set
        {
            if (ReferenceEquals(value, _vsync))
            {
                return;
            }

            _vsync = value;
            if (value == null)
            {
                _snapController?.Dispose();
                _snapController = null;
            }
            else
            {
                _snapController?.Resync(value);
            }
        }
    }

    public AnimationStyle? AnimationStyle { get; set; }

    public FloatingHeaderSnapMode? SnapMode { get; set; }

    /// <summary>
    /// Called each time the position's <see cref="ScrollPosition.IsScrollingNotifier"/> indicates
    /// that user scrolling has stopped or started, i.e. if the sliver "is scrolling".
    /// </summary>
    public void IsScrollingUpdate(ScrollPosition position)
    {
        if (position.IsScrollingNotifier.Value)
        {
            _snapController?.Stop();
            return;
        }

        ScrollDirection direction = position.UserScrollDirection;
        bool headerIsPartiallyVisible = direction switch
        {
            ScrollDirection.Forward when EffectiveScrollOffset <= 0 => false, // completely visible
            ScrollDirection.Reverse when EffectiveScrollOffset >= ChildExtent => false, // not visible
            _ => true,
        };
        if (!headerIsPartiallyVisible)
        {
            return;
        }

        if (_snapController == null)
        {
            _snapController = new AnimationController(vsync: Vsync!);
            _snapController.AddListener(() =>
            {
                if (EffectiveScrollOffset != _snapAnimation!.Value)
                {
                    EffectiveScrollOffset = _snapAnimation.Value;
                    MarkNeedsLayout();
                }
            });
        }

        _snapController.Duration = direction switch
        {
            ScrollDirection.Forward => AnimationStyle?.Duration ?? TimeSpan.FromMilliseconds(300),
            _ => AnimationStyle?.ReverseDuration ?? TimeSpan.FromMilliseconds(300),
        };
        _snapAnimation = _snapController.Drive(
            new DoubleTween(
                begin: EffectiveScrollOffset,
                end: direction switch
                {
                    ScrollDirection.Forward => 0,
                    _ => ChildExtent,
                }).Chain(
                new CurveTween(
                    curve: direction switch
                    {
                        ScrollDirection.Forward => AnimationStyle?.Curve ?? Curves.EaseInOut,
                        _ => AnimationStyle?.ReverseCurve ?? Curves.EaseInOut,
                    })));
        _snapController.Forward(from: 0.0);
    }

    internal double ChildExtent
    {
        get
        {
            if (Child == null)
            {
                return 0.0;
            }

            DebugAssertions.Assert(Child.HasSize);
            return Constraints.Axis switch
            {
                Axis.Vertical => Child.Size.Height,
                _ => Child.Size.Width,
            };
        }
    }

    protected override void OnDetach()
    {
        _snapController?.Dispose();
        _snapController = null; // lazily recreated if we're reattached.
        base.OnDetach();
    }

    /// <summary>
    /// True if the header has been laid at at least once (<c>lastScrollOffset != null</c>) and
    /// either we're scrolling forward (<c>constraints.scrollOffset &lt; lastScrollOffset</c>) or the
    /// header's already partially visible (<c>effectiveScrollOffset &lt; childExtent</c>). Scrolling
    /// forwards (towards the scrollable's start) is the trigger that causes the header to be shown.
    /// </summary>
    private bool FloatingHeaderNeedsToBeUpdated =>
        _lastScrollOffset != null
        && (Constraints.ScrollOffset < _lastScrollOffset.Value || EffectiveScrollOffset < ChildExtent);

    protected override void PerformLayout()
    {
        if (!FloatingHeaderNeedsToBeUpdated)
        {
            EffectiveScrollOffset = Constraints.ScrollOffset;
        }
        else
        {
            double delta = _lastScrollOffset!.Value - Constraints.ScrollOffset; // > 0 when the header is growing
            if (Constraints.UserScrollDirection == ScrollDirection.Forward)
            {
                if (EffectiveScrollOffset > ChildExtent)
                {
                    // The header is now just above the start edge of viewport.
                    EffectiveScrollOffset = ChildExtent;
                }
            }
            else
            {
                // delta > 0 and scrolling forward is a contradiction. Assume that it's noise (set delta to 0).
                delta = Math.Clamp(delta, double.NegativeInfinity, 0);
            }

            EffectiveScrollOffset = Math.Clamp(
                EffectiveScrollOffset - delta,
                0.0,
                Constraints.ScrollOffset);
        }

        Child?.Layout(Constraints.AsBoxConstraints(), parentUsesSize: true);
        double paintExtent = ChildExtent - EffectiveScrollOffset;
        double layoutExtent = (SnapMode ?? FloatingHeaderSnapMode.Overlay) switch
        {
            FloatingHeaderSnapMode.Overlay => ChildExtent - Constraints.ScrollOffset,
            _ => paintExtent,
        };
        Geometry = new SliverGeometry(
            PaintOrigin: Math.Min(Constraints.Overlap, 0.0),
            ScrollExtent: ChildExtent,
            PaintExtent: Math.Clamp(paintExtent, 0.0, Constraints.RemainingPaintExtent),
            LayoutExtent: Math.Clamp(layoutExtent, 0.0, Constraints.RemainingPaintExtent),
            MaxPaintExtent: ChildExtent,
            // Conservatively say we do have overflow to avoid complexity.
            HasVisualOverflow: true);

        _lastScrollOffset = Constraints.ScrollOffset;
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        return Geometry == null ? 0 : Math.Min(0, Geometry.PaintExtent - ChildExtent);
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        DebugAssertions.Assert(ReferenceEquals(child, Child));
        this.ApplyPaintTransformForBoxChild((RenderBox)child, transform);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child != null && Geometry!.Visible)
        {
            offset += ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
                Constraints.AxisDirection,
                Constraints.GrowthDirection) switch
            {
                AxisDirection.Up => new Point(
                    0.0,
                    Geometry.PaintExtent - ChildMainAxisPosition(Child) - ChildExtent),
                AxisDirection.Left => new Point(
                    Geometry.PaintExtent - ChildMainAxisPosition(Child) - ChildExtent,
                    0.0),
                AxisDirection.Right => new Point(ChildMainAxisPosition(Child), 0.0),
                _ => new Point(0.0, ChildMainAxisPosition(Child)),
            };
            context.PaintChild(Child, offset);
        }
    }
}
