using Avalonia;
using Avalonia.Media;
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
    private Point _focalPointOffset;
    private double _magnificationScale;

    public RenderMagnifier(
        Size requestedSize,
        MagnifierDecoration decoration,
        Clip clipBehavior,
        Point focalPointOffset,
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

    public Point FocalPointOffset
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

    protected override bool AlwaysNeedsCompositing => true;

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(_requestedSize);
        if (Child == null)
        {
            return;
        }

        Child.Layout(BoxConstraints.Tight(Size));
        ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        context.PushMagnifier(
            lensRect: new Rect(offset, Size),
            focalPointOffset: _focalPointOffset,
            magnificationScale: _magnificationScale,
            decoration: _decoration,
            clipBehavior: _clipBehavior,
            painter: base.Paint);
    }
}
