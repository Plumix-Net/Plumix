using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/magnifier.dart

namespace Plumix.Rendering;

public sealed record MagnifierDecoration
{
    public MagnifierDecoration(
        double opacity = 1.0,
        IReadOnlyList<BoxShadow>? shadows = null,
        ShapeBorder? shape = null)
    {
        if (!double.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }

        Opacity = opacity;
        Shadows = shadows;
        Shape = shape ?? new RoundedRectangleBorder();
    }

    public double Opacity { get; }

    public IReadOnlyList<BoxShadow>? Shadows { get; }

    public ShapeBorder Shape { get; }

    public bool Equals(MagnifierDecoration? other)
    {
        return other is not null
               && Opacity.Equals(other.Opacity)
               && ShadowList.Equals(Shadows, other.Shadows)
               && Shape.Equals(other.Shape);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Opacity, ShadowList.GetHashCode(Shadows), Shape);
    }
}

public sealed class RenderMagnifier : RenderProxyBox
{
    private Size _requestedSize;
    private MagnifierDecoration _decoration;
    private Clip _clipBehavior;
    private Offset _focalPointOffset;
    private double _magnificationScale;

    public RenderMagnifier(
        Size requestedSize,
        MagnifierDecoration decoration,
        Clip clipBehavior,
        Offset focalPointOffset,
        double magnificationScale,
        RenderBox? child = null)
    {
        _requestedSize = requestedSize;
        _decoration = decoration;
        _clipBehavior = clipBehavior;
        _focalPointOffset = focalPointOffset;
        _magnificationScale = magnificationScale;
        Child = child;
    }

    public Size RequestedSize
    {
        get => _requestedSize;
        set
        {
            if (_requestedSize == value)
            {
                return;
            }

            _requestedSize = value;
            MarkNeedsLayout();
        }
    }

    public MagnifierDecoration Decoration
    {
        get => _decoration;
        set
        {
            if (Equals(_decoration, value))
            {
                return;
            }

            _decoration = value;
            MarkNeedsPaint();
        }
    }

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (_clipBehavior == value)
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsPaint();
        }
    }

    public Offset FocalPointOffset
    {
        get => _focalPointOffset;
        set
        {
            if (_focalPointOffset == value)
            {
                return;
            }

            _focalPointOffset = value;
            MarkNeedsPaint();
        }
    }

    public double MagnificationScale
    {
        get => _magnificationScale;
        set
        {
            if (_magnificationScale == value)
            {
                return;
            }

            _magnificationScale = value;
            MarkNeedsPaint();
        }
    }

    public override bool AlwaysNeedsCompositing => true;

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(_requestedSize);
        if (Child == null)
        {
            return;
        }

        Child.Layout(BoxConstraints.Tight(Size));
    }

    public override void Paint(PaintingContext context, Offset offset)
    {
        context.PushMagnifier(
            lensRect: (offset & Size),
            focalPointOffset: _focalPointOffset,
            magnificationScale: _magnificationScale,
            decoration: _decoration,
            clipBehavior: _clipBehavior,
            painter: base.Paint);
    }
}

/// <summary>
/// Dart's <c>_RenderMagnification</c>: pushes a <see cref="BackdropFilterLayer"/> whose matrix filter enlarges
/// what lies behind it around the focal point; <c>RawMagnifier</c> uses it when the backend filters backdrops.
/// </summary>
internal sealed class RenderMagnification : RenderProxyBox
{
    private Offset _focalPointOffset;
    private double _magnificationScale;

    public RenderMagnification(Offset focalPointOffset, double magnificationScale, RenderBox? child = null)
        : base(child)
    {
        _focalPointOffset = focalPointOffset;
        _magnificationScale = magnificationScale;
    }

    public Offset FocalPointOffset
    {
        get => _focalPointOffset;
        set
        {
            if (_focalPointOffset == value)
            {
                return;
            }

            _focalPointOffset = value;
            MarkNeedsPaint();
        }
    }

    public double MagnificationScale
    {
        get => _magnificationScale;
        set
        {
            if (_magnificationScale == value)
            {
                return;
            }

            _magnificationScale = value;
            MarkNeedsPaint();
        }
    }

    public override bool AlwaysNeedsCompositing => true;

    public override void Paint(PaintingContext context, Offset offset)
    {
        Offset thisCenter = Alignment.Center.AlongSize(Size) + offset;
        Matrix4 matrix = Matrix4.Identity();
        matrix.TranslateByDouble(
            (_magnificationScale * ((_focalPointOffset.Dx * -1) - thisCenter.Dx)) + thisCenter.Dx,
            (_magnificationScale * ((_focalPointOffset.Dy * -1) - thisCenter.Dy)) + thisCenter.Dy,
            0,
            1);
        matrix.ScaleByDouble(_magnificationScale, _magnificationScale, _magnificationScale, 1);
        var filter = new ImageFilter.Matrix(matrix.Storage, filterQuality: FilterQuality.High);

        var layer = Layer as BackdropFilterLayer ?? new BackdropFilterLayer();
        layer.ImageFilter = filter;
        Layer = layer;
        context.PushLayer(layer, base.Paint, offset);
    }
}
