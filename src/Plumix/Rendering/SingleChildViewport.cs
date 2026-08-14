using Avalonia;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/single_child_scroll_view.dart (_RenderSingleChildViewport)
public sealed class RenderSingleChildViewport : RenderProxyBox, IRenderAbstractViewport
{
    private AxisDirection _axisDirection;
    private double _offsetPixels;
    private ViewportMetricsChangedCallback? _onViewportMetricsChanged;

    public RenderSingleChildViewport(
        AxisDirection axisDirection,
        double offsetPixels = 0,
        ViewportMetricsChangedCallback? onViewportMetricsChanged = null,
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

    public ViewportMetricsChangedCallback? OnViewportMetricsChanged
    {
        get => _onViewportMetricsChanged;
        set => _onViewportMetricsChanged = value;
    }

    public Axis Axis => ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection);
    public double MaxScrollExtent { get; private set; }

    /// <inheritdoc />
    public ViewportMoveToCallback? OnMoveTo { get; set; }

    /// <inheritdoc />
    public bool AllowImplicitScrolling { get; set; } = true;

    double IRenderAbstractViewport.RevealOffsetPixels => _offsetPixels;

    /// <inheritdoc />
    public RevealedOffset GetOffsetToReveal(
        RenderObject target,
        double alignment,
        Rect? rect = null,
        Axis? axis = null)
    {
        ArgumentNullException.ThrowIfNull(target);

        // A one-dimensional viewport uses its own axis; a mismatched request is not an error.
        rect ??= target.PaintBounds;
        if (Child is not { } content || target is not RenderBox)
        {
            return new RevealedOffset(OffsetPixels, rect.Value);
        }

        Rect bounds = RenderObject.TransformRect(target.GetTransformTo(content), rect.Value);
        Size contentSize = content.Size;
        double mainAxisExtent;
        double leadingScrollOffset;
        double targetMainAxisExtent;
        switch (AxisDirection)
        {
            case AxisDirection.Up:
                mainAxisExtent = Size.Height;
                leadingScrollOffset = contentSize.Height - bounds.Bottom;
                targetMainAxisExtent = bounds.Height;
                break;
            case AxisDirection.Left:
                mainAxisExtent = Size.Width;
                leadingScrollOffset = contentSize.Width - bounds.Right;
                targetMainAxisExtent = bounds.Width;
                break;
            case AxisDirection.Right:
                mainAxisExtent = Size.Width;
                leadingScrollOffset = bounds.Left;
                targetMainAxisExtent = bounds.Width;
                break;
            default:
                mainAxisExtent = Size.Height;
                leadingScrollOffset = bounds.Top;
                targetMainAxisExtent = bounds.Height;
                break;
        }

        double targetOffset = leadingScrollOffset - (mainAxisExtent - targetMainAxisExtent) * alignment;
        Point shift = ResolvePaintOffset(targetOffset);
        var targetRect = new Rect(bounds.X + shift.X, bounds.Y + shift.Y, bounds.Width, bounds.Height);
        return new RevealedOffset(targetOffset, targetRect);
    }

    public override void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        if (!AllowImplicitScrolling)
        {
            base.ShowOnScreen(descendant, rect, duration, curve);
            return;
        }

        Rect? revealed = RenderAbstractViewport.ShowInViewport(this, descendant, rect, duration, curve);
        base.ShowOnScreen(rect: revealed, duration: duration, curve: curve);
    }

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
