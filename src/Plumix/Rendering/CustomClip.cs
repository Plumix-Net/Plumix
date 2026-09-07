using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
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

/// <summary>
/// The scissors marker Flutter's clip render objects paint when
/// <see cref="RenderingDebug.PaintSizeEnabled"/> is set.
/// </summary>
/// <remarks>
/// Flutter keeps <c>_debugPaint</c> and <c>_debugText</c> as instance fields on the private
/// <c>_RenderCustomClip</c> mixin base. Both objects are stateless, so
/// <see cref="RenderCustomClip{T}"/> reaches them through these lazily created statics instead of
/// carrying a copy per render object.
/// </remarks>
internal static class RenderCustomClipDebug
{
    private const double FontSize = 14.0;

    private static Pen? _debugPen;
    private static TextLayout? _debugText;

    /// <remarks>Flutter's <c>_RenderCustomClip._debugPaint</c>.</remarks>
    internal static Pen DebugPen => _debugPen ??= new Pen(
        new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute),
            EndPoint = new RelativePoint(10, 10, RelativeUnit.Absolute),
            SpreadMethod = GradientSpreadMethod.Repeat,
            GradientStops =
            [
                new GradientStop(Color.FromUInt32(0x00000000), 0.25),
                new GradientStop(Color.FromUInt32(0xFFFF00FF), 0.25),
                new GradientStop(Color.FromUInt32(0xFFFF00FF), 0.75),
                new GradientStop(Color.FromUInt32(0x00000000), 0.75),
            ],
        },
        2.0);

    /// <remarks>Flutter's <c>_RenderCustomClip._debugText</c>.</remarks>
    internal static TextLayout DebugText => _debugText ??= new TextLayout(
        text: "\u2702",
        typeface: new Typeface(FontFamily.Default),
        fontSize: FontSize,
        foreground: new SolidColorBrush(Color.FromUInt32(0xFFFF00FF)));

    /// <remarks>Flutter's <c>_RenderCustomClip.debugPaintSize</c> scissors placement.</remarks>
    internal static void PaintScissors(PaintingContext context, Point offset, double clipWidth)
    {
        PaintScissorsAt(context, offset, clipWidth / 8.0);
    }

    /// <summary>Draws the scissors glyph at an explicit horizontal offset from the clip origin.</summary>
    /// <remarks>
    /// Each Flutter clip render object places the glyph differently: <c>RenderClipRect</c> at
    /// <c>width / 8</c>, <c>RenderClipRRect</c>/<c>RenderClipRSuperellipse</c> at the top-left
    /// corner's x radius, <c>RenderClipOval</c> centred, and <c>RenderClipPath</c> at zero.
    /// </remarks>
    internal static void PaintScissorsAt(PaintingContext context, Point offset, double dx)
    {
        context.Canvas.DrawTextLayout(
            DebugText,
            new Point(offset.X + dx, offset.Y - (FontSize * 1.1)));
    }

    /// <summary>The measured width of the scissors glyph, for the centred placements.</summary>
    internal static double DebugTextWidth => DebugText.Width;
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
            InvalidateClip();
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

    /// <summary>Drops the cached clip so the next paint recomputes it.</summary>
    protected void InvalidateClip() => _hasClip = false;

    /// <remarks>Flutter's <c>_RenderCustomClip._markNeedsClip</c>.</remarks>
    protected virtual void MarkNeedsClip()
    {
        InvalidateClip();
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <remarks>Flutter's <c>_RenderCustomClip.debugPaintSize</c>.</remarks>
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Child is null)
        {
            return;
        }

        // Dart's `_RenderCustomClip.debugPaintSize` deliberately does not chain to
        // `RenderBox.debugPaintSize`, so a clip node never draws the standard size rectangle.
        if (ClipBehavior == Clip.None)
        {
            return;
        }

        DebugPaintClip(context, offset);
    }

    /// <summary>Outlines the effective clip; Flutter inlines this in each subclass's paint.</summary>
    protected abstract void DebugPaintClip(PaintingContext context, Point offset);
}

public sealed class RenderClipOval : RenderCustomClip<Rect>
{
    private Rect? _cachedRect;
    private Path? _cachedPath;

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
        ArgumentNullException.ThrowIfNull(context);
        if (Child is null)
        {
            Layer = null;
            return;
        }

        if (ClipBehavior == Clip.None)
        {
            context.PaintChild(Child, offset);
            Layer = null;
            return;
        }

        Rect clip = EffectiveClip;
        Layer = context.PushClipPath(
            NeedsCompositing,
            offset,
            clip,
            GetClipPath(clip),
            base.Paint,
            ClipBehavior,
            Layer as ClipPathLayer);
    }

    /// <remarks>Flutter's <c>RenderClipOval._getClipPath</c>.</remarks>
    private Path GetClipPath(Rect rect)
    {
        if (_cachedRect != rect)
        {
            _cachedRect = rect;
            _cachedPath = new Path();
            _cachedPath.AddOval(rect);
        }

        return _cachedPath!;
    }

    /// <inheritdoc />
    protected override void DebugPaintClip(PaintingContext context, Point offset)
    {
        Rect clip = EffectiveClip;
        context.Canvas.DrawOval(
            new Rect(clip.Position + offset, clip.Size),
            brush: null,
            pen: RenderCustomClipDebug.DebugPen);
        RenderCustomClipDebug.PaintScissorsAt(
            context,
            offset,
            (clip.Width - RenderCustomClipDebug.DebugTextWidth) / 2.0);
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
        ArgumentNullException.ThrowIfNull(context);
        if (Child is null)
        {
            Layer = null;
            return;
        }

        if (ClipBehavior == Clip.None)
        {
            context.PaintChild(Child, offset);
            Layer = null;
            return;
        }

        Layer = context.PushClipPath(
            NeedsCompositing,
            offset,
            new Rect(new Point(0, 0), Size),
            EffectiveClip,
            base.Paint,
            ClipBehavior,
            Layer as ClipPathLayer);
    }

    /// <inheritdoc />
    protected override void DebugPaintClip(PaintingContext context, Point offset)
    {
        Path clip = EffectiveClip;
        context.Canvas.DrawGeometry(null, RenderCustomClipDebug.DebugPen, clip.ToGeometry(), geometryOffset: offset);
        RenderCustomClipDebug.PaintScissorsAt(context, offset, 0.0);
    }
}

/// <summary>
/// Clips its child to an iOS-style rounded superellipse.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderClipRSuperellipse</c>. Hit testing is performed against the bounding box of
/// the superellipse, not its contour, exactly as Dart documents.
/// </remarks>
public sealed class RenderClipRSuperellipse : RenderCustomClip<RSuperellipse>
{
    private BorderRadiusGeometry _borderRadius;
    private TextDirection? _textDirection;

    public RenderClipRSuperellipse(
        RenderBox? child = null,
        BorderRadiusGeometry? borderRadius = null,
        CustomClipper<RSuperellipse>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias,
        TextDirection? textDirection = null) : base(child, clipper, clipBehavior)
    {
        _borderRadius = borderRadius ?? Rendering.BorderRadius.Zero;
        _textDirection = textDirection;
    }

    /// <summary>The border radius of the rounded corners.</summary>
    public BorderRadiusGeometry BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            MarkNeedsClip();
        }
    }

    /// <summary>The text direction with which to resolve a directional <see cref="BorderRadius"/>.</summary>
    public TextDirection? TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value)
            {
                return;
            }

            _textDirection = value;
            MarkNeedsClip();
        }
    }

    /// <inheritdoc />
    protected override RSuperellipse DefaultClip => _borderRadius
        .Resolve(_textDirection ?? Plumix.UI.TextDirection.Ltr)
        .ToRSuperellipse(new Rect(new Point(0, 0), Size));

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null && !EffectiveClip.OuterRect.Contains(position))
        {
            return false;
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Child is null)
        {
            Layer = null;
            return;
        }

        if (ClipBehavior == Clip.None)
        {
            context.PaintChild(Child, offset);
            Layer = null;
            return;
        }

        RSuperellipse clip = EffectiveClip;
        Layer = context.PushClipRSuperellipse(
            NeedsCompositing,
            offset,
            clip.OuterRect,
            clip,
            base.Paint,
            ClipBehavior,
            Layer as ClipRSuperellipseLayer);
    }

    /// <inheritdoc />
    protected override void DebugPaintClip(PaintingContext context, Point offset)
    {
        RSuperellipse clip = EffectiveClip;
        context.Canvas.DrawRSuperellipse(clip.Shift(offset), brush: null, pen: RenderCustomClipDebug.DebugPen);
        RenderCustomClipDebug.PaintScissorsAt(context, offset, clip.TopLeft.X);
    }
}
