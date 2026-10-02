using Avalonia;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/ink_highlight.dart

/// <summary>
/// A visual emphasis on a part of a <see cref="Material"/> receiving user interaction: a shape that
/// fades in when <see cref="Activate"/> is called and fades out (then disposes itself) after
/// <see cref="Deactivate"/>.
/// </summary>
public class InkHighlight : InteractiveInkFeature
{
    private static readonly TimeSpan DefaultHighlightFadeDuration = TimeSpan.FromMilliseconds(200);

    private readonly BoxShape _shape;
    private readonly double? _radius;
    private readonly BorderRadius _borderRadius;
    private readonly RectCallback? _rectCallback;
    private readonly TextDirection _textDirection;

    private readonly Animation<int> _alpha;
    private readonly AnimationController _alphaController;

    private bool _active = true;

    /// <summary>Begin a highlight animation, then add it to <paramref name="controller"/>.</summary>
    public InkHighlight(
        MaterialInkController controller,
        RenderBox referenceBox,
        Color color,
        TextDirection textDirection,
        BoxShape shape = BoxShape.Rectangle,
        double? radius = null,
        BorderRadius? borderRadius = null,
        ShapeBorder? customBorder = null,
        RectCallback? rectCallback = null,
        Action? onRemoved = null,
        TimeSpan? fadeDuration = null)
        : base(controller, referenceBox, color, customBorder, onRemoved)
    {
        _textDirection = textDirection;
        _shape = shape;
        _radius = radius;
        _borderRadius = borderRadius ?? BorderRadius.Zero;
        _rectCallback = rectCallback;

        _alphaController = new AnimationController(
            duration: fadeDuration ?? DefaultHighlightFadeDuration,
            vsync: controller.Vsync);
        _alphaController.AddListener(controller.MarkNeedsPaint);
        _alphaController.AddStatusListener(HandleAlphaStatusChanged);
        _alphaController.Forward();
        _alpha = _alphaController.Drive(new IntTween(0, color.Alpha));

        controller.AddInkFeature(this);
    }

    /// <summary>Whether this part of the material is being visually emphasized.</summary>
    public bool Active => _active;

    /// <summary>Start visually emphasizing this part of the material.</summary>
    public void Activate()
    {
        _active = true;
        _alphaController.Forward();
    }

    /// <summary>Stop visually emphasizing this part of the material.</summary>
    public void Deactivate()
    {
        _active = false;
        _alphaController.Reverse();
    }

    private void HandleAlphaStatusChanged(AnimationStatus status)
    {
        if (status.IsDismissed() && !_active)
        {
            Dispose();
        }
    }

    public override void Dispose()
    {
        _alphaController.Dispose();
        base.Dispose();
    }

    private void PaintHighlight(Canvas canvas, Rect rect, Paint paint)
    {
        canvas.Save();
        if (CustomBorder is not null)
        {
            canvas.ClipPath(CustomBorder.GetOuterPath(rect, textDirection: _textDirection));
        }

        switch (_shape)
        {
            case BoxShape.Circle:
                canvas.DrawCircle(rect.Center, _radius ?? Material.DefaultSplashRadius, paint);
                break;
            case BoxShape.Rectangle:
                if (_borderRadius != BorderRadius.Zero)
                {
                    RRect clipRRect = RRect.FromRectAndCorners(rect, _borderRadius);
                    canvas.DrawRRect(clipRRect, paint);
                }
                else
                {
                    canvas.DrawRect(rect, paint);
                }

                break;
        }

        canvas.Restore();
    }

    protected override void PaintFeature(Canvas canvas, Matrix4 transform)
    {
        var paint = new Paint { Color = Color.WithAlpha(_alpha.Value) };
        Avalonia.Point? originOffset = MatrixUtils.GetAsTranslation(transform);
        Rect rect = _rectCallback is not null ? _rectCallback() : new Rect(ReferenceBox.Size);
        if (originOffset is null)
        {
            canvas.Save();
            canvas.Transform(transform);
            PaintHighlight(canvas, rect, paint);
            canvas.Restore();
        }
        else
        {
            PaintHighlight(canvas, rect.Translate((Avalonia.Vector)originOffset.Value), paint);
        }
    }
}
