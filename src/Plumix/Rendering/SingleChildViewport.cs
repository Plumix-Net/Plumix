using Avalonia;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/single_child_scroll_view.dart (_RenderSingleChildViewport)
public sealed class RenderSingleChildViewport : RenderProxyBox
{
    private AxisDirection _axisDirection;
    private double _offsetPixels;
    private Action<double, double, double>? _onViewportMetricsChanged;

    public RenderSingleChildViewport(
        AxisDirection axisDirection,
        double offsetPixels = 0,
        Action<double, double, double>? onViewportMetricsChanged = null,
        RenderBox? child = null)
    {
        _axisDirection = axisDirection;
        _offsetPixels = offsetPixels;
        _onViewportMetricsChanged = onViewportMetricsChanged;
        Child = child;
    }

    public AxisDirection AxisDirection
    {
        get => _axisDirection;
        set
        {
            if (_axisDirection == value) return;
            _axisDirection = value;
            MarkNeedsLayout();
        }
    }

    public double OffsetPixels
    {
        get => _offsetPixels;
        set
        {
            if (Math.Abs(_offsetPixels - value) < 0.0001) return;
            _offsetPixels = value;
            MarkNeedsLayout();
        }
    }

    public Action<double, double, double>? OnViewportMetricsChanged
    {
        get => _onViewportMetricsChanged;
        set => _onViewportMetricsChanged = value;
    }

    public Axis Axis => ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection);
    public double MaxScrollExtent { get; private set; }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Constrain(new Size());
            MaxScrollExtent = 0;
            OnViewportMetricsChanged?.Invoke(MainExtent(Size), 0, 0);
            return;
        }

        var childConstraints = Axis == Axis.Horizontal
            ? new BoxConstraints(MinHeight: Constraints.MinHeight, MaxHeight: Constraints.MaxHeight)
            : new BoxConstraints(MinWidth: Constraints.MinWidth, MaxWidth: Constraints.MaxWidth);
        Child.Layout(childConstraints, parentUsesSize: true);
        Size = Constraints.Constrain(Child.Size);

        double viewportExtent = MainExtent(Size);
        double childExtent = MainExtent(Child.Size);
        MaxScrollExtent = Math.Max(0, childExtent - viewportExtent);
        // Out-of-range offsets are kept: physics that allow overscroll (iOS bouncing) shift the child
        // instead of being clamped back into range.
        ((BoxParentData)Child.parentData!).offset = ResolvePaintOffset(OffsetPixels);
        OnViewportMetricsChanged?.Invoke(viewportExtent, 0, MaxScrollExtent);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        var child = Child;
        if (child is null || Size.Width <= 0 || Size.Height <= 0) return;
        context.PushClipRect(new Rect(offset, Size), clippedContext =>
        {
            var childOffset = ((BoxParentData)child.parentData!).offset;
            clippedContext.PaintChild(child, offset + childOffset);
        });
    }

    private Point ResolvePaintOffset(double pixels)
    {
        if (Child is null) return default;
        return AxisDirection switch
        {
            AxisDirection.Right => new Point(-pixels, 0),
            AxisDirection.Left => new Point(Size.Width - Child.Size.Width + pixels, 0),
            AxisDirection.Down => new Point(0, -pixels),
            AxisDirection.Up => new Point(0, Size.Height - Child.Size.Height + pixels),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private double MainExtent(Size size) => Axis == Axis.Horizontal ? size.Width : size.Height;
}
