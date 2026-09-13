using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Path = Plumix.UI.Path;
using TextStyle = Plumix.Widgets.TextStyle;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart

/// <summary>An interface for providing custom clips.</summary>
/// <remarks>Flutter's <c>CustomClipper&lt;T&gt;</c>.</remarks>
public abstract class CustomClipper<T> : IListenable
{
    private readonly IListenable? _reclip;

    /// <summary>Creates a custom clipper that updates its clip whenever <paramref name="reclip"/> notifies.</summary>
    protected CustomClipper(IListenable? reclip = null)
    {
        _reclip = reclip;
    }

    /// <summary>Register a closure to be notified when it is time to reclip.</summary>
    public void AddListener(Action listener) => _reclip?.AddListener(listener);

    /// <summary>Remove a previously registered closure.</summary>
    public void RemoveListener(Action listener) => _reclip?.RemoveListener(listener);

    /// <summary>
    /// Returns a description of the clip given that the render object being clipped is of the given size.
    /// </summary>
    public abstract T GetClip(Size size);

    /// <summary>
    /// Returns an approximation of the clip returned by <see cref="GetClip"/>, as an axis-aligned Rect.
    /// </summary>
    public virtual Rect GetApproximateClipRect(Size size) => new(new Point(0, 0), size);

    /// <summary>Whether a new instance of the clipper represents different information than the old one.</summary>
    public abstract bool ShouldReclip(CustomClipper<T> oldClipper);

    /// <inheritdoc />
    public override string ToString() => Foundation.Diagnostics.ObjectRuntimeType(this, "CustomClipper");
}

/// <summary>A <see cref="CustomClipper{T}"/> that clips to the outer path of a <see cref="ShapeBorder"/>.</summary>
public class ShapeBorderClipper : CustomClipper<Path>
{
    /// <summary>Creates a <see cref="ShapeBorder"/> clipper.</summary>
    public ShapeBorderClipper(ShapeBorder shape, TextDirection? textDirection = null)
    {
        Shape = shape;
        TextDirection = textDirection;
    }

    /// <summary>The shape border whose outer path this clipper clips to.</summary>
    public ShapeBorder Shape { get; }

    /// <summary>The text direction to use for getting the outer path for <see cref="Shape"/>.</summary>
    public TextDirection? TextDirection { get; }

    /// <summary>Returns the outer path of <see cref="Shape"/> as the clip.</summary>
    public override Path GetClip(Size size)
    {
        return Shape.GetOuterPath(new Rect(new Point(0, 0), size), TextDirection);
    }

    /// <inheritdoc />
    public override bool ShouldReclip(CustomClipper<Path> oldClipper)
    {
        if (oldClipper.GetType() != typeof(ShapeBorderClipper))
        {
            return true;
        }

        var typedOldClipper = (ShapeBorderClipper)oldClipper;
        return typedOldClipper.Shape != Shape || typedOldClipper.TextDirection != TextDirection;
    }
}

/// <summary>The clipper, cached clip and debug paint shared by the clip render objects.</summary>
/// <remarks>
/// Flutter's private <c>_RenderCustomClip&lt;T&gt;</c>. C# cannot derive a public class from a less
/// accessible one, so the type is public; its Dart-private members are <c>private protected</c>, which
/// keeps them (and subclassing) inside this assembly.
/// </remarks>
public abstract class RenderCustomClip<T> : RenderProxyBox
{
    private CustomClipper<T>? _clipper;
    private Clip _clipBehavior;
    private bool _hasClip;

    private protected RenderCustomClip(
        RenderBox? child = null,
        CustomClipper<T>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias) : base(child)
    {
        _clipper = clipper;
        _clipBehavior = clipBehavior;
    }

    /// <summary>If non-null, determines which clip to use on the child.</summary>
    public CustomClipper<T>? Clipper
    {
        get => _clipper;
        set
        {
            if (Equals(_clipper, value))
            {
                return;
            }

            CustomClipper<T>? oldClipper = _clipper;
            _clipper = value;
            Debug.Assert(value is not null || oldClipper is not null);
            if (value is null
                || oldClipper is null
                || value.GetType() != oldClipper.GetType()
                || value.ShouldReclip(oldClipper))
            {
                MarkNeedsClip();
            }

            if (Attached)
            {
                oldClipper?.RemoveListener(MarkNeedsClip);
                value?.AddListener(MarkNeedsClip);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnAttach()
    {
        base.OnAttach();
        _clipper?.AddListener(MarkNeedsClip);
    }

    /// <inheritdoc />
    protected override void OnDetach()
    {
        _clipper?.RemoveListener(MarkNeedsClip);
        base.OnDetach();
    }

    /// <remarks>Flutter's <c>_RenderCustomClip._markNeedsClip</c>.</remarks>
    private protected void MarkNeedsClip()
    {
        _hasClip = false;
        _clip = default!;
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <remarks>Flutter's <c>_RenderCustomClip._defaultClip</c>.</remarks>
    private protected abstract T DefaultClip { get; }

    /// <summary>The cached clip; only meaningful after <see cref="UpdateClip"/>.</summary>
    /// <remarks>Flutter's <c>_RenderCustomClip._clip</c>, whose null state is <see cref="_hasClip"/> here.</remarks>
    private protected T _clip = default!;

    /// <summary>How to clip.</summary>
    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (value != _clipBehavior)
            {
                _clipBehavior = value;
                MarkNeedsPaint();
            }
        }
    }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        Size? oldSize = HasSize ? Size : null;
        base.PerformLayout();
        if (oldSize != Size)
        {
            _hasClip = false;
            _clip = default!;
        }
    }

    /// <remarks>
    /// Flutter's <c>_RenderCustomClip._updateClip</c>: <c>_clip ??= _clipper?.getClip(size) ?? _defaultClip</c>.
    /// </remarks>
    private protected void UpdateClip()
    {
        if (_hasClip)
        {
            return;
        }

        T? clip = _clipper is null ? default : _clipper.GetClip(Size);
        _clip = _clipper is null || clip is null ? DefaultClip : clip;
        _hasClip = true;
    }

    /// <inheritdoc />
    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        switch (ClipBehavior)
        {
            case Clip.None:
                return null;
            case Clip.HardEdge:
            case Clip.AntiAlias:
            case Clip.AntiAliasWithSaveLayer:
            default:
                return _clipper?.GetApproximateClipRect(Size) ?? new Rect(new Point(0, 0), Size);
        }
    }

    /// <remarks>Flutter's <c>_RenderCustomClip._debugPaint</c>.</remarks>
    private protected IPen? _debugPaint;

    /// <remarks>Flutter's <c>_RenderCustomClip._debugText</c>.</remarks>
    private protected TextPainter? _debugText;

    /// <inheritdoc />
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        if (Constants.KDebugMode)
        {
            _debugPaint ??= new Pen(
                new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0.0, 0.0, RelativeUnit.Absolute),
                    EndPoint = new RelativePoint(10.0, 10.0, RelativeUnit.Absolute),
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
            if (_debugText is null)
            {
                _debugText = new TextPainter(
                    text: new TextSpan(
                        text: "✂",
                        style: new TextStyle(Color: Color.FromUInt32(0xFFFF00FF), FontSize: 14.0)),
                    textDirection: TextDirection.Rtl); // doesn't matter, it's one character
                _debugText.Layout();
            }
        }
    }

    /// <summary>The debug scissors glyph's font size, read back from its text style as Dart does.</summary>
    private protected double DebugTextFontSize => _debugText!.Text!.Style!.FontSize!.Value;

    /// <inheritdoc />
    public override void Dispose()
    {
        _debugText?.Dispose();
        _debugText = null;
        base.Dispose();
    }
}

/// <summary>Clips its child using a rectangle.</summary>
public class RenderClipRect : RenderCustomClip<Rect>
{
    /// <summary>Creates a rectangular clip.</summary>
    public RenderClipRect(
        RenderBox? child = null,
        CustomClipper<Rect>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias) : base(child, clipper, clipBehavior)
    {
    }

    private protected override Rect DefaultClip => new(new Point(0, 0), Size);

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null)
        {
            UpdateClip();
            if (!_clip.ContainsHalfOpen(position))
            {
                return false;
            }
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is not null)
        {
            if (ClipBehavior != Clip.None)
            {
                UpdateClip();
                Layer = context.PushClipRect(
                    NeedsCompositing,
                    offset,
                    _clip,
                    base.Paint,
                    clipBehavior: ClipBehavior,
                    oldLayer: Layer as ClipRectLayer);
            }
            else
            {
                context.PaintChild(Child, offset);
                Layer = null;
            }
        }
        else
        {
            Layer = null;
        }
    }

    /// <inheritdoc />
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        if (Constants.KDebugMode)
        {
            if (Child is not null)
            {
                base.DebugPaintSize(context, offset);
                if (ClipBehavior != Clip.None)
                {
                    context.Canvas.DrawRectangle(null, _debugPaint, new Rect(_clip.Position + offset, _clip.Size));
                    _debugText!.Paint(
                        context.Canvas,
                        offset + new Point(_clip.Width / 8.0, -DebugTextFontSize * 1.1));
                }
            }
        }
    }
}

/// <summary>Clips its child using a rounded rectangle.</summary>
public class RenderClipRRect : RenderCustomClip<RRect>
{
    private BorderRadiusGeometry _borderRadius;
    private TextDirection? _textDirection;

    /// <summary>Creates a rounded-rectangular clip.</summary>
    /// <remarks>
    /// Dart's <c>borderRadius</c> defaults to <c>BorderRadius.zero</c>; <see cref="BorderRadiusGeometry"/>'s
    /// <c>default</c> is that same zero radius.
    /// </remarks>
    public RenderClipRRect(
        RenderBox? child = null,
        BorderRadiusGeometry borderRadius = default,
        CustomClipper<RRect>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias,
        TextDirection? textDirection = null) : base(child, clipper, clipBehavior)
    {
        _borderRadius = borderRadius;
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

    /// <summary>The text direction with which to resolve <see cref="BorderRadius"/>.</summary>
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

    private protected override RRect DefaultClip =>
        _borderRadius.Resolve(TextDirection).ToRRect(new Rect(new Point(0, 0), Size));

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null)
        {
            UpdateClip();
            if (!_clip.Contains(position))
            {
                return false;
            }
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is not null)
        {
            if (ClipBehavior != Clip.None)
            {
                UpdateClip();
                Layer = context.PushClipRRect(
                    NeedsCompositing,
                    offset,
                    _clip.Rect,
                    _clip,
                    base.Paint,
                    clipBehavior: ClipBehavior,
                    oldLayer: Layer as ClipRRectLayer);
            }
            else
            {
                context.PaintChild(Child, offset);
                Layer = null;
            }
        }
        else
        {
            Layer = null;
        }
    }

    /// <inheritdoc />
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        if (Constants.KDebugMode)
        {
            if (Child is not null)
            {
                base.DebugPaintSize(context, offset);
                if (ClipBehavior != Clip.None)
                {
                    context.Canvas.DrawRRect(_clip.Shift(offset), brush: null, pen: _debugPaint);
                    _debugText!.Paint(
                        context.Canvas,
                        offset + new Point(_clip.TopLeft.X, -DebugTextFontSize * 1.1));
                }
            }
        }
    }
}

/// <summary>Clips its child using a rounded superellipse.</summary>
/// <remarks>Hit tests are performed based on the bounding box of the RSuperellipse.</remarks>
public class RenderClipRSuperellipse : RenderCustomClip<RSuperellipse>
{
    private BorderRadiusGeometry _borderRadius;
    private TextDirection? _textDirection;

    /// <summary>Creates a rounded-superellipse clip.</summary>
    public RenderClipRSuperellipse(
        RenderBox? child = null,
        BorderRadiusGeometry borderRadius = default,
        CustomClipper<RSuperellipse>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias,
        TextDirection? textDirection = null) : base(child, clipper, clipBehavior)
    {
        _borderRadius = borderRadius;
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

    /// <summary>The text direction with which to resolve <see cref="BorderRadius"/>.</summary>
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

    private protected override RSuperellipse DefaultClip =>
        _borderRadius.Resolve(TextDirection).ToRSuperellipse(new Rect(new Point(0, 0), Size));

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null)
        {
            UpdateClip();
            if (!_clip.OuterRect.ContainsHalfOpen(position))
            {
                return false;
            }
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is not null)
        {
            if (ClipBehavior != Clip.None)
            {
                UpdateClip();
                Layer = context.PushClipRSuperellipse(
                    NeedsCompositing,
                    offset,
                    _clip.OuterRect,
                    _clip,
                    base.Paint,
                    clipBehavior: ClipBehavior,
                    oldLayer: Layer as ClipRSuperellipseLayer);
            }
            else
            {
                context.PaintChild(Child, offset);
                Layer = null;
            }
        }
        else
        {
            Layer = null;
        }
    }

    /// <inheritdoc />
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        if (Constants.KDebugMode)
        {
            if (Child is not null)
            {
                base.DebugPaintSize(context, offset);
                if (ClipBehavior != Clip.None)
                {
                    context.Canvas.DrawRSuperellipse(_clip.Shift(offset), brush: null, pen: _debugPaint);
                    _debugText!.Paint(
                        context.Canvas,
                        offset + new Point(_clip.TopLeft.X, -DebugTextFontSize * 1.1));
                }
            }
        }
    }
}

/// <summary>Clips its child using an oval.</summary>
public class RenderClipOval : RenderCustomClip<Rect>
{
    private Rect? _cachedRect;
    private Path _cachedPath = null!;

    /// <summary>Creates an oval-shaped clip.</summary>
    public RenderClipOval(
        RenderBox? child = null,
        CustomClipper<Rect>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias) : base(child, clipper, clipBehavior)
    {
    }

    private Path GetClipPath(Rect rect)
    {
        if (rect != _cachedRect)
        {
            _cachedRect = rect;
            _cachedPath = new Path();
            _cachedPath.AddOval(rect);
        }

        return _cachedPath;
    }

    private protected override Rect DefaultClip => new(new Point(0, 0), Size);

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        UpdateClip();
        Point center = _clip.Center;
        // convert the position to an offset from the center of the unit circle
        var offset = new Point(
            (position.X - center.X) / _clip.Width,
            (position.Y - center.Y) / _clip.Height);
        // check if the point is outside the unit circle
        if ((offset.X * offset.X) + (offset.Y * offset.Y) > 0.25)
        {
            // x^2 + y^2 > r^2
            return false;
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is not null)
        {
            if (ClipBehavior != Clip.None)
            {
                UpdateClip();
                Layer = context.PushClipPath(
                    NeedsCompositing,
                    offset,
                    _clip,
                    GetClipPath(_clip),
                    base.Paint,
                    clipBehavior: ClipBehavior,
                    oldLayer: Layer as ClipPathLayer);
            }
            else
            {
                context.PaintChild(Child, offset);
                Layer = null;
            }
        }
        else
        {
            Layer = null;
        }
    }

    /// <inheritdoc />
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        if (Constants.KDebugMode)
        {
            if (Child is not null)
            {
                base.DebugPaintSize(context, offset);
                if (ClipBehavior != Clip.None)
                {
                    context.Canvas.DrawPath(GetClipPath(_clip).Shift(offset), brush: null, pen: _debugPaint);
                    _debugText!.Paint(
                        context.Canvas,
                        offset + new Point((_clip.Width - _debugText.Width) / 2.0, -DebugTextFontSize * 1.1));
                }
            }
        }
    }
}

/// <summary>Clips its child using a path.</summary>
public class RenderClipPath : RenderCustomClip<Path>
{
    /// <summary>Creates a path clip.</summary>
    public RenderClipPath(
        RenderBox? child = null,
        CustomClipper<Path>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias) : base(child, clipper, clipBehavior)
    {
    }

    private protected override Path DefaultClip
    {
        get
        {
            var path = new Path();
            path.AddRect(new Rect(new Point(0, 0), Size));
            return path;
        }
    }

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Clipper is not null)
        {
            UpdateClip();
            if (!_clip.Contains(position))
            {
                return false;
            }
        }

        return base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is not null)
        {
            if (ClipBehavior != Clip.None)
            {
                UpdateClip();
                Layer = context.PushClipPath(
                    NeedsCompositing,
                    offset,
                    new Rect(new Point(0, 0), Size),
                    _clip,
                    base.Paint,
                    clipBehavior: ClipBehavior,
                    oldLayer: Layer as ClipPathLayer);
            }
            else
            {
                context.PaintChild(Child, offset);
                Layer = null;
            }
        }
        else
        {
            Layer = null;
        }
    }

    /// <inheritdoc />
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        if (Constants.KDebugMode)
        {
            if (Child is not null)
            {
                base.DebugPaintSize(context, offset);
                if (ClipBehavior != Clip.None)
                {
                    context.Canvas.DrawPath(_clip.Shift(offset), brush: null, pen: _debugPaint);
                    _debugText!.Paint(context.Canvas, offset);
                }
            }
        }
    }
}

/// <summary>Dart's `_DecorationClipper` (widgets/container.dart): clips to a decoration's own outer path.</summary>
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
