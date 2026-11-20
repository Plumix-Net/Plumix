using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/single_child_scroll_view.dart (_RenderSingleChildViewport)
public sealed class RenderSingleChildViewport : RenderProxyBox, IRenderAbstractViewport
{
    private AxisDirection _axisDirection;
    private ViewportOffset _offset;
    private Clip _clipBehavior;

    public RenderSingleChildViewport(
        AxisDirection axisDirection,
        ViewportOffset offset,
        RenderBox? child = null,
        Clip clipBehavior = Clip.HardEdge)
    {
        ArgumentNullException.ThrowIfNull(offset);
        _axisDirection = axisDirection;
        _offset = offset;
        _clipBehavior = clipBehavior;
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
                _offset.RemoveListener(HasScrolled);
            }

            _offset = value;
            if (Owner != null)
            {
                _offset.AddListener(HasScrolled);
            }

            MarkNeedsLayout();
        }
    }

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (_clipBehavior == value) return;
            _clipBehavior = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public override bool IsRepaintBoundary => true;

    private void HasScrolled()
    {
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>The current scroll offset, in pixels.</summary>
    public double OffsetPixels => _offset.Pixels;

    public Axis Axis => ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection);
    public double MaxScrollExtent { get; private set; }

    protected override void OnAttach()
    {
        base.OnAttach();
        _offset.AddListener(HasScrolled);
    }

    protected override void OnDetach()
    {
        _offset.RemoveListener(HasScrolled);
        base.OnDetach();
    }

    /// <summary>
    /// The semantics clip covers the whole scrollable content, so a child scrolled off screen stays in
    /// the semantics tree and is reported as hidden rather than dropped.
    /// </summary>
    protected override Rect? DescribeSemanticsClip(RenderObject? child)
    {
        var semanticBounds = DartGeometry.RectFromOffsetAndSize(new Offset(0, 0), Size);
        double remainingOffset = MaxScrollExtent - OffsetPixels;
        return AxisDirection switch
        {
            AxisDirection.Up => DartGeometry.RectFromCorners(
                new Offset(semanticBounds.Left, semanticBounds.Top - remainingOffset),
                new Offset(semanticBounds.Right, semanticBounds.Bottom + OffsetPixels)),
            AxisDirection.Right => DartGeometry.RectFromCorners(
                new Offset(semanticBounds.Left - OffsetPixels, semanticBounds.Top),
                new Offset(semanticBounds.Right + remainingOffset, semanticBounds.Bottom)),
            AxisDirection.Down => DartGeometry.RectFromCorners(
                new Offset(semanticBounds.Left, semanticBounds.Top - OffsetPixels),
                new Offset(semanticBounds.Right, semanticBounds.Bottom + remainingOffset)),
            _ => DartGeometry.RectFromCorners(
                new Offset(semanticBounds.Left - remainingOffset, semanticBounds.Top),
                new Offset(semanticBounds.Right + OffsetPixels, semanticBounds.Bottom))
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
        Offset shift = ResolvePaintOffset(targetOffset);
        var targetRect = DartGeometry.RectFromLTWH(bounds.Left + shift.Dx, bounds.Top + shift.Dy, bounds.Width, bounds.Height);
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

    private BoxConstraints GetInnerConstraints(BoxConstraints constraints) => Axis == Axis.Horizontal
        ? new BoxConstraints(MinHeight: constraints.MinHeight, MaxHeight: constraints.MaxHeight)
        : new BoxConstraints(MinWidth: constraints.MinWidth, MaxWidth: constraints.MaxWidth);

    protected override Size ComputeDryLayout(BoxConstraints constraints) => Child is null
        ? constraints.Smallest
        : constraints.Constrain(Child.GetDryLayout(GetInnerConstraints(constraints)));

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline) => null;

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Smallest;
            MaxScrollExtent = 0;
        }
        else
        {
            Child.Layout(GetInnerConstraints(Constraints), parentUsesSize: true);
            Size = Constraints.Constrain(Child.Size);
            MaxScrollExtent = Math.Max(0, MainExtent(Child.Size) - MainExtent(Size));
        }

        if (Offset.HasPixels)
        {
            if (Offset.Pixels > MaxScrollExtent)
            {
                Offset.CorrectBy(MaxScrollExtent - Offset.Pixels);
            }
            else if (Offset.Pixels < 0)
            {
                Offset.CorrectBy(-Offset.Pixels);
            }
        }

        Offset.ApplyViewportDimension(MainExtent(Size));
        Offset.ApplyContentDimensions(0, MaxScrollExtent);
    }

    private readonly LayerHandle<ClipRectLayer> _clipRectLayer = new();

    /// <inheritdoc />
    public override void Dispose()
    {
        _clipRectLayer.Layer = null;
        base.Dispose();
    }

    private bool ShouldClipAtPaintOffset(Offset paintOffset) => ClipBehavior != Clip.None
        && Child is { } child
        && (paintOffset.Dx < 0 || paintOffset.Dy < 0
            || paintOffset.Dx + child.Size.Width > Size.Width
            || paintOffset.Dy + child.Size.Height > Size.Height);

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child) =>
        child != null && ShouldClipAtPaintOffset(PaintOffset) ? DartGeometry.RectFromSize(Size) : null;

    public override void Paint(PaintingContext context, Offset offset)
    {
        if (Child is not { } child) return;
        Offset paintOffset = PaintOffset;
        void PaintContents(PaintingContext ctx, Offset point) => ctx.PaintChild(child, point + paintOffset);
        if (ShouldClipAtPaintOffset(paintOffset))
        {
            _clipRectLayer.Layer = context.PushClipRect(
                NeedsCompositing,
                offset,
                DartGeometry.RectFromSize(Size),
                PaintContents,
                clipBehavior: ClipBehavior,
                oldLayer: _clipRectLayer.Layer);
        }
        else
        {
            _clipRectLayer.Layer = null;
            PaintContents(context, offset);
        }
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Offset>("offset", PaintOffset));
    }

    /// <summary>Dart's <c>_RenderSingleChildViewport._paintOffset</c>: the child is shifted by the
    /// scroll offset at paint time rather than through its parent data. Out-of-range offsets are
    /// kept: physics that allow overscroll (iOS bouncing) shift the child instead of being clamped.
    /// </summary>
    private Offset PaintOffset => ResolvePaintOffset(OffsetPixels);

    /// <inheritdoc />
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        Offset paintOffset = PaintOffset;
        transform.TranslateByDouble(paintOffset.Dx, paintOffset.Dy, 0, 1);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Offset position)
    {
        RenderBox? child = Child;
        if (child is null)
        {
            return false;
        }

        return result.AddWithPaintOffset(
            PaintOffset,
            position,
            (hitResult, transformed) => child.HitTest(hitResult, transformed));
    }

    // Dart does not override computeDistanceToActualBaseline here: as you scroll, the box would
    // shift inside a baseline-aligned parent, which makes no sense.
    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => null;

    private Offset ResolvePaintOffset(double pixels)
    {
        if (Child is null) return default;
        return AxisDirection switch
        {
            AxisDirection.Right => new Offset(-pixels, 0),
            AxisDirection.Left => new Offset(Size.Width - Child.Size.Width + pixels, 0),
            AxisDirection.Down => new Offset(0, -pixels),
            AxisDirection.Up => new Offset(0, Size.Height - Child.Size.Height + pixels),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private double MainExtent(Size size) => Axis == Axis.Horizontal ? size.Width : size.Height;
}
