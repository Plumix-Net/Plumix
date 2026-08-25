using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart

public abstract class CustomClipper<T> : IListenable
{
    private readonly IListenable? _reclip;

    protected CustomClipper(IListenable? reclip = null)
    {
        _reclip = reclip;
    }

    public void AddListener(Action listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _reclip?.AddListener(listener);
    }

    public void RemoveListener(Action listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _reclip?.RemoveListener(listener);
    }

    public abstract T GetClip(Size size);

    public virtual Rect GetApproximateClipRect(Size size)
    {
        return new Rect(new Point(0, 0), size);
    }

    public abstract bool ShouldReclip(CustomClipper<T> oldClipper);
}

public sealed class ShapeBorderClipper : CustomClipper<Path>
{
    public ShapeBorderClipper(ShapeBorder shape, TextDirection? textDirection = null)
    {
        Shape = shape ?? throw new ArgumentNullException(nameof(shape));
        TextDirection = textDirection;
    }

    public ShapeBorder Shape { get; }

    public TextDirection? TextDirection { get; }

    public override Path GetClip(Size size)
    {
        return Shape.GetOuterPath(new Rect(new Point(0, 0), size), TextDirection);
    }

    public override bool ShouldReclip(CustomClipper<Path> oldClipper)
    {
        return oldClipper is not ShapeBorderClipper oldShapeClipper
               || oldShapeClipper.Shape != Shape
               || oldShapeClipper.TextDirection != TextDirection;
    }
}

/// <summary>Dart's `_DecorationClipper`: clips to a decoration's own outer path.</summary>
internal sealed class DecorationClipper : CustomClipper<Path>
{
    public DecorationClipper(Decoration decoration, TextDirection? textDirection = null)
    {
        Decoration = decoration ?? throw new ArgumentNullException(nameof(decoration));
        TextDirection = textDirection;
    }

    public Decoration Decoration { get; }

    public TextDirection? TextDirection { get; }

    public override Path GetClip(Size size)
    {
        return Decoration.GetClipPath(
            new Rect(new Point(0, 0), size),
            TextDirection ?? Plumix.UI.TextDirection.Ltr);
    }

    public override bool ShouldReclip(CustomClipper<Path> oldClipper)
    {
        return oldClipper is not DecorationClipper oldDecorationClipper
               || oldDecorationClipper.Decoration != Decoration;
    }
}

public abstract class RenderCustomClip<T> : RenderProxyBox
{
    private CustomClipper<T>? _clipper;
    private Clip _clipBehavior;
    private T _clip = default!;
    private bool _hasClip;

    protected RenderCustomClip(
        RenderBox? child = null,
        CustomClipper<T>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias)
    {
        _clipper = clipper;
        _clipBehavior = clipBehavior;
        Child = child;
    }

    public CustomClipper<T>? Clipper
    {
        get => _clipper;
        set
        {
            if (ReferenceEquals(_clipper, value))
            {
                return;
            }

            CustomClipper<T>? oldClipper = _clipper;
            _clipper = value;
            bool shouldReclip = value is null
                                || oldClipper is null
                                || value.GetType() != oldClipper.GetType()
                                || value.ShouldReclip(oldClipper);

            if (Attached)
            {
                oldClipper?.RemoveListener(MarkNeedsClip);
                value?.AddListener(MarkNeedsClip);
            }

            if (shouldReclip)
            {
                MarkNeedsClip();
            }
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
            MarkNeedsSemanticsUpdate();
        }
    }

    protected T EffectiveClip
    {
        get
        {
            if (!_hasClip)
            {
                _clip = _clipper is null ? DefaultClip : _clipper.GetClip(Size);
                ArgumentNullException.ThrowIfNull(_clip);
                _hasClip = true;
            }

            return _clip;
        }
    }

    protected abstract T DefaultClip { get; }

    protected override void OnAttach()
    {
        base.OnAttach();
        _clipper?.AddListener(MarkNeedsClip);
    }

    protected override void OnDetach()
    {
        _clipper?.RemoveListener(MarkNeedsClip);
        base.OnDetach();
    }

    protected override void PerformLayout()
    {
        bool hadSize = HasSize;
        Size oldSize = hadSize ? Size : default;
        base.PerformLayout();
        if (!hadSize || oldSize != Size)
        {
            _hasClip = false;
        }
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        if (_clipBehavior == Clip.None)
        {
            return null;
        }

        return _clipper?.GetApproximateClipRect(Size) ?? new Rect(new Point(0, 0), Size);
    }

    private void MarkNeedsClip()
    {
        _hasClip = false;
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }
}

public sealed class RenderClipOval : RenderCustomClip<Rect>
{
    public RenderClipOval(
        RenderBox? child = null,
        CustomClipper<Rect>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias) : base(child, clipper, clipBehavior)
    {
    }

    protected override Rect DefaultClip => new(new Point(0, 0), Size);

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        Rect clip = EffectiveClip;
        if (clip.Width <= 0 || clip.Height <= 0)
        {
            return false;
        }

        Point center = clip.Center;
        double normalizedX = (position.X - center.X) / clip.Width;
        double normalizedY = (position.Y - center.Y) / clip.Height;
        if ((normalizedX * normalizedX) + (normalizedY * normalizedY) > 0.25)
        {
            return false;
        }

        return base.HitTest(result, position);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is null)
        {
            return;
        }

        if (ClipBehavior == Clip.None)
        {
            base.Paint(context, offset);
            return;
        }

        context.PushClipGeometry(
            new EllipseGeometry(EffectiveClip),
            clippedContext => base.Paint(clippedContext, offset),
            clipBehavior: ClipBehavior,
            geometryOffset: offset);
    }
}

public sealed class RenderClipPath : RenderCustomClip<Path>
{
    public RenderClipPath(
        RenderBox? child = null,
        CustomClipper<Path>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias) : base(child, clipper, clipBehavior)
    {
    }

    protected override Path DefaultClip
    {
        get
        {
            var path = new Path();
            path.AddRect(new Rect(new Point(0, 0), Size));
            return path;
        }
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null && !EffectiveClip.Contains(position))
        {
            return false;
        }

        return base.HitTest(result, position);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is null)
        {
            return;
        }

        if (ClipBehavior == Clip.None)
        {
            base.Paint(context, offset);
            return;
        }

        context.PushClipGeometry(
            EffectiveClip.ToGeometry(),
            clippedContext => base.Paint(clippedContext, offset),
            clipBehavior: ClipBehavior,
            geometryOffset: offset);
    }
}
