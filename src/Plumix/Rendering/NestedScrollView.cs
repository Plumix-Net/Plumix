using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/nested_scroll_view.dart

namespace Plumix.Rendering;

/// <summary>
/// Passes the overlap a pinned or floating header absorbs from the outer scroll view of a
/// <see cref="Plumix.Widgets.NestedScrollView"/> to the injector that reproduces it inside the body.
/// </summary>
public class SliverOverlapAbsorberHandle : ChangeNotifier
{
    // Incremented by every attached RenderSliverOverlapAbsorber; a handle with more than one writer
    // cannot produce a meaningful extent, which is what Flutter's assert catches.
    internal int Writers;

    private double? _layoutExtent;
    private double? _scrollExtent;

    /// <summary>
    /// The amount of overlap the absorber's sliver applies to the slivers after it, or null until an
    /// absorber has laid out.
    /// </summary>
    public double? LayoutExtent => _layoutExtent;

    /// <summary>The total scroll extent of the gap the absorber removed from the outer view.</summary>
    public double? ScrollExtent => _scrollExtent;

    internal void SetExtents(double? layoutValue, double? scrollValue)
    {
        if (Writers != 1)
        {
            throw new InvalidOperationException(
                "Multiple RenderSliverOverlapAbsorbers have been provided the same "
                + "SliverOverlapAbsorberHandle.");
        }

        _layoutExtent = layoutValue;
        _scrollExtent = scrollValue;
    }

    internal void MarkNeedsLayout() => NotifyListeners();

    public override string ToString()
    {
        string extra = Writers switch
        {
            0 => ", orphan",
            1 => string.Empty,
            _ => $", {Writers} WRITERS ASSIGNED",
        };
        return $"{nameof(SliverOverlapAbsorberHandle)}({LayoutExtent}{extra})";
    }
}

/// <summary>
/// A sliver that wraps another sliver, takes its maximum scroll obstruction extent out of the outer
/// scroll view, and reports it through a <see cref="SliverOverlapAbsorberHandle"/>.
/// </summary>
public class RenderSliverOverlapAbsorber : RenderSliver, IRenderObjectSingleChildContainer
{
    private RenderSliver? _child;
    private SliverOverlapAbsorberHandle _handle;

    public RenderSliverOverlapAbsorber(SliverOverlapAbsorberHandle handle, RenderSliver? sliver = null)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        Child = sliver;
    }

    public SliverOverlapAbsorberHandle Handle
    {
        get => _handle;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_handle, value))
            {
                return;
            }

            if (Attached)
            {
                _handle.Writers -= 1;
                value.Writers += 1;
                value.SetExtents(_handle.LayoutExtent, _handle.ScrollExtent);
            }

            _handle = value;
        }
    }

    public RenderSliver? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;
            if (_child != null)
            {
                AdoptChild(_child);
            }

            MarkNeedsLayout();
        }
    }

    RenderObject? IRenderObjectSingleChildContainer.Child
    {
        get => Child;
        set => Child = (RenderSliver?)value;
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverPhysicalParentData)
        {
            child.parentData = new SliverPhysicalParentData();
        }
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        Handle.Writers += 1;
    }

    protected override void OnDetach()
    {
        Handle.Writers -= 1;
        base.OnDetach();
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        if (Handle.Writers != 1)
        {
            throw new InvalidOperationException(
                "A SliverOverlapAbsorberHandle cannot be passed to multiple "
                + "RenderSliverOverlapAbsorber objects at the same time.");
        }

        if (_child == null)
        {
            Geometry = new SliverGeometry();
            return;
        }

        _child.LayoutWithSliverConstraints(constraints);
        SliverGeometry childLayoutGeometry = _child.Geometry;
        Geometry = childLayoutGeometry with
        {
            ScrollExtent = childLayoutGeometry.ScrollExtent
                           - childLayoutGeometry.MaxScrollObstructionExtent,
            LayoutExtent = Math.Max(
                0.0,
                childLayoutGeometry.PaintExtent - childLayoutGeometry.MaxScrollObstructionExtent),
        };
        Handle.SetExtents(
            childLayoutGeometry.MaxScrollObstructionExtent,
            childLayoutGeometry.MaxScrollObstructionExtent);
    }

    public override double ChildMainAxisPosition(RenderObject child) => 0.0;

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child != null)
        {
            ctx.PaintChild(_child, offset);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return _child != null && _child.HitTest(result, position);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);
}

/// <summary>
/// A sliver that reproduces the overlap a <see cref="RenderSliverOverlapAbsorber"/> removed from the
/// outer scroll view, so a body sliver can start below the header that pins over it.
/// </summary>
public class RenderSliverOverlapInjector : RenderSliver
{
    private SliverOverlapAbsorberHandle _handle;
    private double? _currentLayoutExtent;
    private double? _currentMaxExtent;

    public RenderSliverOverlapInjector(SliverOverlapAbsorberHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    public SliverOverlapAbsorberHandle Handle
    {
        get => _handle;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_handle, value))
            {
                return;
            }

            if (Attached)
            {
                _handle.RemoveListener(MarkNeedsLayout);
            }

            _handle = value;
            if (Attached)
            {
                _handle.AddListener(MarkNeedsLayout);
                if (Handle.LayoutExtent != _currentLayoutExtent
                    || Handle.ScrollExtent != _currentMaxExtent)
                {
                    MarkNeedsLayout();
                }
            }
        }
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        Handle.AddListener(MarkNeedsLayout);
        if (Handle.LayoutExtent != _currentLayoutExtent || Handle.ScrollExtent != _currentMaxExtent)
        {
            MarkNeedsLayout();
        }
    }

    protected override void OnDetach()
    {
        Handle.RemoveListener(MarkNeedsLayout);
        base.OnDetach();
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        _currentLayoutExtent = Handle.LayoutExtent;
        // Flutter reads layoutExtent for both; the absorber always reports the same value for each.
        _currentMaxExtent = Handle.LayoutExtent;
        if (_currentLayoutExtent == null || _currentMaxExtent == null)
        {
            throw new InvalidOperationException(
                "SliverOverlapInjector has found no absorbed extent to inject.\n "
                + "The SliverOverlapAbsorber must be an earlier descendant of a common "
                + "ancestor Viewport, so that it will always be laid out before the "
                + "SliverOverlapInjector during a particular frame.\n "
                + "The SliverOverlapAbsorber is typically contained in the list of slivers "
                + "provided by NestedScrollView.headerSliverBuilder.\n");
        }

        double clampedPaintExtent = Math.Min(
            _currentLayoutExtent.Value,
            constraints.RemainingPaintExtent);
        double clampedLayoutExtent = Math.Min(
            _currentLayoutExtent.Value - constraints.ScrollOffset,
            constraints.RemainingPaintExtent);
        Geometry = new SliverGeometry(
            ScrollExtent: _currentLayoutExtent.Value,
            PaintExtent: Math.Max(0.0, clampedPaintExtent),
            LayoutExtent: Math.Max(0.0, clampedLayoutExtent),
            MaxPaintExtent: _currentMaxExtent.Value);
    }

    /// <summary>An injector has no content of its own to paint.</summary>
    public override void Paint(PaintingContext ctx, Point offset)
    {
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderSliverOverlapInjector.debugPaint</c>: a zig-zag over the gap.</remarks>
    protected override void DebugPaint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        base.DebugPaint(context, offset);
        if (!RenderingDebug.PaintSizeEnabled || !HasSliverConstraints)
        {
            return;
        }

        var pen = new Pen(new SolidColorBrush(Color.FromUInt32(0xFFCC9933)), 3.0);
        SliverConstraints constraints = ConstraintsForSliver;
        Point start;
        Point end;
        Point delta;
        if (constraints.Axis == Axis.Vertical)
        {
            double x = offset.X + (constraints.CrossAxisExtent / 2.0);
            start = new Point(x, offset.Y);
            end = new Point(x, offset.Y + Geometry.PaintExtent);
            delta = new Point(constraints.CrossAxisExtent / 5.0, 0.0);
        }
        else
        {
            double y = offset.Y + (constraints.CrossAxisExtent / 2.0);
            start = new Point(offset.X, y);
            end = new Point(offset.Y + Geometry.PaintExtent, y);
            delta = new Point(0.0, constraints.CrossAxisExtent / 5.0);
        }

        for (int index = -2; index <= 2; index += 1)
        {
            var shift = new Point(delta.X * index, delta.Y * index);
            PaintUtilities.PaintZigZag(context, pen, start - shift, end - shift, 10, 10.0);
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<SliverOverlapAbsorberHandle>("handle", Handle));
    }
}

/// <summary>
/// The viewport a <see cref="Plumix.Widgets.NestedScrollView"/>'s outer scroll view uses, which tells
/// its overlap handle whenever it needs to lay out again.
/// </summary>
public class RenderNestedScrollViewViewport : RenderViewport
{
    private SliverOverlapAbsorberHandle _handle;

    public RenderNestedScrollViewViewport(
        ViewportOffset offset,
        SliverOverlapAbsorberHandle handle,
        AxisDirection? crossAxisDirection = null,
        AxisDirection axisDirection = AxisDirection.Down,
        double anchor = 0.0,
        IReadOnlyList<RenderSliver>? children = null,
        RenderSliver? center = null,
        Clip clipBehavior = Clip.HardEdge)
        : base(
            offset,
            crossAxisDirection,
            axisDirection,
            anchor,
            children,
            center,
            clipBehavior: clipBehavior)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    public SliverOverlapAbsorberHandle Handle
    {
        get => _handle;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_handle, value))
            {
                return;
            }

            _handle = value;
            Handle.MarkNeedsLayout();
        }
    }

    public override void MarkNeedsLayout()
    {
        // The injectors listen to the handle, so telling it first is what makes them dirty in time
        // to be laid out after the absorber during this frame.
        Handle.MarkNeedsLayout();
        base.MarkNeedsLayout();
    }
}
