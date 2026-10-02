using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/ink_splash.dart

internal sealed class InkSplashFactory : InteractiveInkFeatureFactory
{
    public override InteractiveInkFeature Create(
        MaterialInkController controller,
        RenderBox referenceBox,
        Point position,
        Color color,
        TextDirection textDirection,
        bool containedInkWell = false,
        RectCallback? rectCallback = null,
        BorderRadius? borderRadius = null,
        ShapeBorder? customBorder = null,
        double? radius = null,
        Action? onRemoved = null)
    {
        return new InkSplash(
            controller: controller,
            referenceBox: referenceBox,
            position: position,
            color: color,
            containedInkWell: containedInkWell,
            rectCallback: rectCallback,
            borderRadius: borderRadius,
            customBorder: customBorder,
            radius: radius,
            onRemoved: onRemoved,
            textDirection: textDirection);
    }
}

/// <summary>
/// A visual reaction on a piece of <see cref="Material"/> to user input: a circular ink feature whose
/// origin starts at the input touch point and whose radius expands from zero.
/// </summary>
public class InkSplash : InteractiveInkFeature
{
    private static readonly TimeSpan UnconfirmedSplashDuration = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SplashFadeDuration = TimeSpan.FromMilliseconds(200);
    private const double SplashInitialSize = 0.0; // logical pixels
    private const double SplashConfirmedVelocity = 1.0; // logical pixels per millisecond

    private readonly Point? _position;
    private readonly BorderRadius _borderRadius;
    private readonly double _targetRadius;
    private readonly RectCallback? _clipCallback;
    private readonly bool _repositionToReferenceBox;
    private readonly TextDirection _textDirection;

    private readonly Animation<double> _radius;
    private readonly AnimationController _radiusController;
    private readonly Animation<int> _alpha;
    private AnimationController? _alphaController;

    /// <summary>
    /// Begin a splash, centered at <paramref name="position"/> relative to <paramref name="referenceBox"/>.
    /// </summary>
    public InkSplash(
        MaterialInkController controller,
        RenderBox referenceBox,
        TextDirection textDirection,
        Color color,
        Point? position = null,
        bool containedInkWell = false,
        RectCallback? rectCallback = null,
        BorderRadius? borderRadius = null,
        ShapeBorder? customBorder = null,
        double? radius = null,
        Action? onRemoved = null)
        : base(controller, referenceBox, color, customBorder, onRemoved)
    {
        _position = position;
        _borderRadius = borderRadius ?? BorderRadius.Zero;
        _targetRadius = radius ?? GetTargetRadius(referenceBox, containedInkWell, rectCallback, position!.Value);
        _clipCallback = GetClipCallback(referenceBox, containedInkWell, rectCallback);
        _repositionToReferenceBox = !containedInkWell;
        _textDirection = textDirection;

        _radiusController = new AnimationController(duration: UnconfirmedSplashDuration, vsync: controller.Vsync);
        _radiusController.AddListener(controller.MarkNeedsPaint);
        _radiusController.Forward();
        _radius = _radiusController.Drive(new Tween<double>(SplashInitialSize, _targetRadius));
        _alphaController = new AnimationController(duration: SplashFadeDuration, vsync: controller.Vsync);
        _alphaController.AddListener(controller.MarkNeedsPaint);
        _alphaController.AddStatusListener(HandleAlphaStatusChanged);
        _alpha = _alphaController.Drive(new IntTween(color.Alpha, 0));

        controller.AddInkFeature(this);
    }

    /// <summary>Used to specify this type of ink splash for an <see cref="InkWell"/>, <see cref="InkResponse"/>,
    /// material <see cref="Theme"/>, or <see cref="ButtonStyle"/>.</summary>
    public static InteractiveInkFeatureFactory SplashFactory { get; } = new InkSplashFactory();

    public override void Confirm()
    {
        int duration = (int)Math.Floor(_targetRadius / SplashConfirmedVelocity);
        _radiusController.Duration = TimeSpan.FromMilliseconds(duration);
        _radiusController.Forward();
        _alphaController!.Forward();
    }

    public override void Cancel()
    {
        _alphaController?.Forward();
    }

    private void HandleAlphaStatusChanged(AnimationStatus status)
    {
        if (status.IsCompleted())
        {
            Dispose();
        }
    }

    public override void Dispose()
    {
        _radiusController.Dispose();
        _alphaController!.Dispose();
        _alphaController = null;
        base.Dispose();
    }

    protected override void PaintFeature(Canvas canvas, Matrix4 transform)
    {
        var paint = new Paint { Color = Color.WithAlpha(_alpha.Value) };
        Point? center = _position;
        if (_repositionToReferenceBox)
        {
            center = InkFeatureGeometry.LerpOffset(
                center,
                InkFeatureGeometry.SizeCenter(ReferenceBox.Size),
                _radiusController.Value);
        }

        PaintInkCircle(
            canvas: canvas,
            transform: transform,
            paint: paint,
            center: center!.Value,
            textDirection: _textDirection,
            radius: _radius.Value,
            customBorder: CustomBorder,
            borderRadius: _borderRadius,
            clipCallback: _clipCallback);
    }

    private static RectCallback? GetClipCallback(
        RenderBox referenceBox,
        bool containedInkWell,
        RectCallback? rectCallback)
    {
        if (rectCallback is not null)
        {
            DebugAssertions.Assert(containedInkWell);
            return rectCallback;
        }

        if (containedInkWell)
        {
            return () => new Rect(referenceBox.Size);
        }

        return null;
    }

    private static double GetTargetRadius(
        RenderBox referenceBox,
        bool containedInkWell,
        RectCallback? rectCallback,
        Point position)
    {
        if (containedInkWell)
        {
            Size size = rectCallback is not null ? rectCallback().Size : referenceBox.Size;
            return GetSplashRadiusForPositionInSize(size, position);
        }

        return Material.DefaultSplashRadius;
    }

    private static double GetSplashRadiusForPositionInSize(Size bounds, Point position)
    {
        double d1 = InkFeatureGeometry.Distance(position, new Point(0.0, 0.0));
        double d2 = InkFeatureGeometry.Distance(position, new Point(bounds.Width, 0.0));
        double d3 = InkFeatureGeometry.Distance(position, new Point(0.0, bounds.Height));
        double d4 = InkFeatureGeometry.Distance(position, new Point(bounds.Width, bounds.Height));
        return Math.Ceiling(Math.Max(Math.Max(d1, d2), Math.Max(d3, d4)));
    }
}
