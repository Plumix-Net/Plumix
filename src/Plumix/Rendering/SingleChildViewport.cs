using Avalonia;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/single_child_scroll_view.dart (_RenderSingleChildViewport)
public sealed class RenderSingleChildViewport : RenderProxyBox, IRenderAbstractViewport
{
    private AxisDirection _axisDirection;
    private ViewportOffset _offset;

    public RenderSingleChildViewport(
        AxisDirection axisDirection,
        ViewportOffset offset,
        RenderBox? child = null)
    {
        ArgumentNullException.ThrowIfNull(offset);
        _axisDirection = axisDirection;
        _offset = offset;
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

    /// <inheritdoc />
    public ViewportOffset Offset
    {
        get => _offset;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_offset, value)) return;
            if (Owner != null)
            {
                _offset.RemoveListener(MarkNeedsLayout);
            }

            _offset = value;
            if (Owner != null)
            {
                _offset.AddListener(MarkNeedsLayout);
            }

            MarkNeedsLayout();
        }
    }

    /// <summary>The current scroll offset, in pixels.</summary>
    public double OffsetPixels => _offset.Pixels;

    public Axis Axis => ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection);
    public double MaxScrollExtent { get; private set; }

    protected override void OnAttach()
    {
        base.OnAttach();
        _offset.AddListener(MarkNeedsLayout);
    }

    protected override void OnDetach()
    {
        _offset.RemoveListener(MarkNeedsLayout);
        base.OnDetach();
    }

    /// <summary>
    /// The semantics clip covers the whole scrollable content, so a child scrolled off screen stays in
    /// the semantics tree and is reported as hidden rather than dropped.
    /// </summary>
    protected override Rect? DescribeSemanticsClip(RenderObject? child)
    {
        var semanticBounds = new Rect(new Point(0, 0), Size);
        double remainingOffset = MaxScrollExtent - OffsetPixels;
        return AxisDirection switch
        {
            AxisDirection.Up => new Rect(
                new Point(semanticBounds.Left, semanticBounds.Top - remainingOffset),
                new Point(semanticBounds.Right, semanticBounds.Bottom + OffsetPixels)),
            AxisDirection.Right => new Rect(
                new Point(semanticBounds.Left - OffsetPixels, semanticBounds.Top),
                new Point(semanticBounds.Right + remainingOffset, semanticBounds.Bottom)),
            AxisDirection.Down => new Rect(
                new Point(semanticBounds.Left, semanticBounds.Top - OffsetPixels),
                new Point(semanticBounds.Right, semanticBounds.Bottom + remainingOffset)),
            _ => new Rect(
                new Point(semanticBounds.Left - remainingOffset, semanticBounds.Top),
                new Point(semanticBounds.Right + OffsetPixels, semanticBounds.Bottom))
        };
    }

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
        if (!_offset.AllowImplicitScrolling)
        {
            base.ShowOnScreen(descendant, rect, duration, curve);
            return;
        }

        Rect? revealed = RenderAbstractViewport.ShowInViewport(
            this,
            _offset,
            descendant,
            rect,
            duration,
            curve);
        base.ShowOnScreen(rect: revealed, duration: duration, curve: curve);
    }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Constrain(new Size());
            MaxScrollExtent = 0;
            Offset.ApplyViewportDimension(MainExtent(Size));
            Offset.ApplyContentDimensions(0, 0);
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
        Offset.ApplyViewportDimension(viewportExtent);
        Offset.ApplyContentDimensions(0, MaxScrollExtent);
        // Out-of-range offsets are kept: physics that allow overscroll (iOS bouncing) shift the child
        // instead of being clamped back into range.
        ((BoxParentData)Child.parentData!).offset = ResolvePaintOffset(OffsetPixels);
    }

    private readonly LayerHandle<ClipRectLayer> _clipRectLayer = new();

    /// <inheritdoc />
    public override void Dispose()
    {
        _clipRectLayer.Layer = null;
        base.Dispose();
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        RenderBox? child = Child;
        if (child is null || Size.Width <= 0 || Size.Height <= 0) return;
        _clipRectLayer.Layer = context.PushClipRect(
            NeedsCompositing,
            offset,
            new Rect(new Point(0, 0), Size),
            (clippedContext, clippedOffset) =>
            {
                Point childOffset = ((BoxParentData)child.parentData!).offset;
                clippedContext.PaintChild(child, clippedOffset + childOffset);
            },
            oldLayer: _clipRectLayer.Layer);
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
