using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/ink_ripple.dart

internal sealed class InkRippleFactory : InteractiveInkFeatureFactory
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
        return new InkRipple(
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
/// origin starts at the input touch point and whose radius expands from 60% of the final radius, while
/// the splash origin animates to the center of its <see cref="InkFeature.ReferenceBox"/>.
/// </summary>
public class InkRipple : InteractiveInkFeature
{
    private static readonly TimeSpan UnconfirmedRippleDuration = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan FadeInDuration = TimeSpan.FromMilliseconds(75);
    private static readonly TimeSpan RadiusDuration = TimeSpan.FromMilliseconds(225);
    private static readonly TimeSpan FadeOutDuration = TimeSpan.FromMilliseconds(375);
    private static readonly TimeSpan CancelDuration = TimeSpan.FromMilliseconds(75);

    // The fade out begins 225ms after the _fadeOutController starts. See confirm().
    private const double FadeOutIntervalStart = 225.0 / 375.0;

    private static readonly Animatable<double> EaseCurveTween = new CurveTween(Curves.Ease);
    private static readonly Animatable<double> FadeOutIntervalTween =
        new CurveTween(new Interval(FadeOutIntervalStart, 1.0));

    private readonly Point _position;
    private readonly BorderRadius _borderRadius;
    private readonly double _targetRadius;
    private readonly RectCallback? _clipCallback;
    private readonly TextDirection _textDirection;

    private readonly Animation<double> _radius;
    private readonly AnimationController _radiusController;
    private readonly Animation<int> _fadeIn;
    private readonly AnimationController _fadeInController;
    private readonly Animation<int> _fadeOut;
    private readonly AnimationController _fadeOutController;

    /// <summary>
    /// Begin a ripple, centered at <paramref name="position"/> relative to <paramref name="referenceBox"/>.
    /// </summary>
    public InkRipple(
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
        : base(controller, referenceBox, color, customBorder, onRemoved)
    {
        _position = position;
        _borderRadius = borderRadius ?? BorderRadius.Zero;
        _targetRadius = radius ?? GetTargetRadius(referenceBox, containedInkWell, rectCallback, position);
        _clipCallback = GetClipCallback(referenceBox, containedInkWell, rectCallback);
        _textDirection = textDirection;

        // Immediately begin fading-in the initial splash.
        _fadeInController = new AnimationController(duration: FadeInDuration, vsync: controller.Vsync);
        _fadeInController.AddListener(controller.MarkNeedsPaint);
        _fadeInController.Forward();
        _fadeIn = _fadeInController.Drive(new IntTween(0, color.Alpha));

        // Controls the splash radius and its center. Starts upon confirm.
        _radiusController = new AnimationController(duration: UnconfirmedRippleDuration, vsync: controller.Vsync);
        _radiusController.AddListener(controller.MarkNeedsPaint);
        _radiusController.Forward();
        // Initial splash diameter is 60% of the target diameter, final
        // diameter is 10dps larger than the target diameter.
        _radius = _radiusController.Drive(
            new Tween<double>(_targetRadius * 0.30, _targetRadius + 5.0).Chain(EaseCurveTween));

        // Controls the splash radius and its center. Starts upon confirm however its
        // Interval delays changes until the radius expansion has completed.
        _fadeOutController = new AnimationController(duration: FadeOutDuration, vsync: controller.Vsync);
        _fadeOutController.AddListener(controller.MarkNeedsPaint);
        _fadeOutController.AddStatusListener(HandleAlphaStatusChanged);
        _fadeOut = _fadeOutController.Drive(new IntTween(color.Alpha, 0).Chain(FadeOutIntervalTween));

        controller.AddInkFeature(this);
    }

    /// <summary>Used to specify this type of ink splash for an <see cref="InkWell"/>, <see cref="InkResponse"/>,
    /// material <see cref="Theme"/>, or <see cref="ButtonStyle"/>.</summary>
    public static InteractiveInkFeatureFactory SplashFactory { get; } = new InkRippleFactory();

    public override void Confirm()
    {
        _radiusController.Duration = RadiusDuration;
        _radiusController.Forward();
        // This confirm may have been preceded by a cancel.
        _fadeInController.Forward();
        _fadeOutController.AnimateTo(1.0, duration: FadeOutDuration);
    }

    public override void Cancel()
    {
        _fadeInController.Stop();
        // Watch out: setting _fadeOutController's value to 1.0 will
        // trigger a call to _handleAlphaStatusChanged() which will
        // dispose _fadeOutController.
        double fadeOutValue = 1.0 - _fadeInController.Value;
        _fadeOutController.SetValue(fadeOutValue);
        if (fadeOutValue < 1.0)
        {
            _fadeOutController.AnimateTo(1.0, duration: CancelDuration);
        }
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
        _fadeInController.Dispose();
        _fadeOutController.Dispose();
        base.Dispose();
    }

    protected override void PaintFeature(Canvas canvas, Matrix4 transform)
    {
        int alpha = _fadeInController.IsAnimating ? _fadeIn.Value : _fadeOut.Value;
        var paint = new Paint { Color = Color.WithAlpha(alpha) };
        Rect? rect = _clipCallback?.Invoke();
        // Splash moves to the center of the reference box.
        Point center = InkFeatureGeometry.LerpOffset(
            _position,
            rect is { } clip ? clip.Center : InkFeatureGeometry.SizeCenter(ReferenceBox.Size),
            Curves.Ease.Transform(_radiusController.Value))!.Value;
        PaintInkCircle(
            canvas: canvas,
            transform: transform,
            paint: paint,
            center: center,
            textDirection: _textDirection,
            radius: _radius.Value,
            customBorder: CustomBorder,
            borderRadius: _borderRadius,
            clipCallback: _clipCallback);
    }

    internal static RectCallback? GetClipCallback(
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

    internal static double GetTargetRadius(
        RenderBox referenceBox,
        bool containedInkWell,
        RectCallback? rectCallback,
        Point position)
    {
        Size size = rectCallback is not null ? rectCallback().Size : referenceBox.Size;
        double d1 = InkFeatureGeometry.Distance(new Point(size.Width, size.Height), new Point(0.0, 0.0));
        double d2 = InkFeatureGeometry.Distance(new Point(size.Width, 0.0), new Point(0.0, size.Height));
        return Math.Max(d1, d2) / 2.0;
    }
}

// C#-only: the `Offset`/`Size` helpers Dart's ink features call inline (`Offset.lerp`,
// `Size.center`, `Offset.distance`).
internal static class InkFeatureGeometry
{
    // Dart's `Offset.lerp`, including its null cases.
    public static Point? LerpOffset(Point? a, Point? b, double t)
    {
        if (b is null)
        {
            return a is null ? null : a.Value * (1.0 - t);
        }

        if (a is null)
        {
            return b.Value * t;
        }

        return new Point(
            a.Value.X + ((b.Value.X - a.Value.X) * t),
            a.Value.Y + ((b.Value.Y - a.Value.Y) * t));
    }

    public static Point SizeCenter(Size size) => new(size.Width / 2.0, size.Height / 2.0);

    public static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
