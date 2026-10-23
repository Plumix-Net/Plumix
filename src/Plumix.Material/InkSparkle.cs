using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/ink_sparkle.dart

/// <summary>
/// Begin a Material 3 ink sparkle ripple, centered at the tap or click position relative to the
/// <see cref="InkFeature.ReferenceBox"/>.
/// </summary>
/// <remarks>
/// Paints the pinned <c>ink_sparkle.frag</c> through the framework's Skia runtime-effect adapter,
/// including the white sparkles and the turbulence-modulated ring.
/// </remarks>
public class InkSparkle : InteractiveInkFeature
{
    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(617);
    private const double TargetRadiusMultiplier = 2.3;
    private const double RotateRight = Math.PI * 0.0078125;
    private const double RotateLeft = -RotateRight;
    private const double NoiseDensity = 2.1;

    private readonly AnimationController _animationController;

    // The Android 12 version has these values calculated in the GLSL. They are
    // constant for every pixel in the animation, so the Flutter implementation
    // computes these animation values in software in order to simplify the shader
    // implementation and provide better performance on most devices.
    private readonly Animation<Point> _center;
    private readonly Animation<double> _radiusScale;
    private readonly Animation<double> _alpha;
    private readonly Animation<double> _sparkleAlpha;

    private readonly double _turbulenceSeed;
    private readonly Color _color;
    private readonly Point _position;
    private readonly BorderRadius _borderRadius;
    private readonly double _targetRadius;
    private readonly RectCallback? _clipCallback;
    private readonly TextDirection _textDirection;

    private RuntimeFragmentShader? _fragmentShader;
    private readonly double[] _uniforms = new double[28];

    /// <summary>
    /// Begin a sparkly ripple effect, centered at <paramref name="position"/> relative to
    /// <paramref name="referenceBox"/>.
    /// </summary>
    public InkSparkle(
        MaterialInkController controller,
        RenderBox referenceBox,
        Color color,
        Point position,
        TextDirection textDirection,
        bool containedInkWell = true,
        RectCallback? rectCallback = null,
        BorderRadius? borderRadius = null,
        ShapeBorder? customBorder = null,
        double? radius = null,
        Action? onRemoved = null,
        double? turbulenceSeed = null)
        : base(controller, referenceBox, color, customBorder, onRemoved)
    {
        DebugAssertions.Assert(containedInkWell || rectCallback is null);
        _color = color;
        _position = position;
        _borderRadius = borderRadius ?? BorderRadius.Zero;
        _targetRadius = (radius ?? InkRipple.GetTargetRadius(referenceBox, containedInkWell, rectCallback, position))
                        * TargetRadiusMultiplier;
        _clipCallback = InkRipple.GetClipCallback(referenceBox, containedInkWell, rectCallback);
        _textDirection = textDirection;

        // InkSparkle will not be painted until the async compilation completes.
        InkSparkleFactory.InitializeShader();
        controller.AddInkFeature(this);

        // Immediately begin animating the ink.
        _animationController = new AnimationController(duration: AnimationDuration, vsync: controller.Vsync);
        _animationController.AddListener(controller.MarkNeedsPaint);
        _animationController.AddStatusListener(HandleStatusChanged);
        _animationController.Forward();

        _radiusScale = new TweenSequence<double>(
        [
            new TweenSequenceItem<double>(new CurveTween(Curves.FastOutSlowIn), 75),
            new TweenSequenceItem<double>(new ConstantTween<double>(1.0), 25),
        ]).Animate(_animationController);

        // Functionally equivalent to Android 12's SkSL:
        //`return mix(u_touch, u_resolution, saturate(in_radius_scale * 2.0))`
        var centerTween = new Vector2Tween(
            new Point(_position.X, _position.Y),
            new Point(referenceBox.Size.Width / 2, referenceBox.Size.Height / 2));
        Animation<double> centerProgress = new TweenSequence<double>(
        [
            new TweenSequenceItem<double>(new Tween<double>(0.0, 1.0), 50),
            new TweenSequenceItem<double>(new ConstantTween<double>(1.0), 50),
        ]).Animate(_radiusScale);
        _center = centerTween.Animate(centerProgress);

        _alpha = new TweenSequence<double>(
        [
            new TweenSequenceItem<double>(new Tween<double>(0.0, 1.0), 13),
            new TweenSequenceItem<double>(new ConstantTween<double>(1.0), 27),
            new TweenSequenceItem<double>(new Tween<double>(1.0, 0.0), 60),
        ]).Animate(_animationController);

        _sparkleAlpha = new TweenSequence<double>(
        [
            new TweenSequenceItem<double>(new Tween<double>(0.0, 1.0), 13),
            new TweenSequenceItem<double>(new ConstantTween<double>(1.0), 27),
            new TweenSequenceItem<double>(new Tween<double>(1.0, 0.0), 50),
        ]).Animate(_animationController);

        // Creates an element of randomness so that ink emanating from the same
        // pixel have slightly different rings and sparkles.
        if (Constants.KDebugMode)
        {
            // In tests, randomness can cause flakes. So if a seed has not
            // already been specified (i.e. for the purpose of the test), set it to
            // the constant turbulence seed.
            turbulenceSeed ??= InkSparkleFactory.ConstantSeed;
        }

        _turbulenceSeed = turbulenceSeed ?? Random.Shared.NextDouble() * 1000.0;
    }

    /// <summary>Used to specify this type of ink splash for an <see cref="InkWell"/>, <see cref="InkResponse"/>,
    /// material <see cref="Theme"/>, or <see cref="ButtonStyle"/>.</summary>
    public static InteractiveInkFeatureFactory SplashFactory { get; } = new InkSparkleFactory();

    /// <summary>
    /// Like <see cref="SplashFactory"/> but with a constant turbulence seed, so the sparkle pattern is
    /// deterministic (used by tests and goldens).
    /// </summary>
    public static InteractiveInkFeatureFactory ConstantTurbulenceSeedSplashFactory { get; } =
        InkSparkleFactory.ConstantTurbulenceSeed();

    /// <summary>C#-only test hook: the 28 shader uniforms the last paint computed, in Dart's slot order.</summary>
    internal IReadOnlyList<double> DebugUniforms => _uniforms;

    private void HandleStatusChanged(AnimationStatus status)
    {
        if (status.IsCompleted())
        {
            Dispose();
        }
    }

    public override void Dispose()
    {
        _animationController.Stop();
        _animationController.Dispose();
        _fragmentShader?.Dispose();
        base.Dispose();
    }

    protected override void PaintFeature(Canvas canvas, Matrix4 transform)
    {
        DebugAssertions.Assert(_animationController.IsAnimating);

        // The first paint may precede the program's asynchronous initialization.
        if (InkSparkleFactory.Program is not { } program)
        {
            return;
        }

        _fragmentShader ??= program.FragmentShader();
        canvas.Save();
        TransformCanvas(canvas, transform);
        if (_clipCallback is not null)
        {
            ClipCanvas(
                canvas: canvas,
                clipCallback: _clipCallback,
                textDirection: _textDirection,
                customBorder: CustomBorder,
                borderRadius: _borderRadius);
        }

        UpdateFragmentShader();
        var paint = new Paint { Shader = _fragmentShader };
        if (_clipCallback is not null)
        {
            canvas.DrawRect(_clipCallback(), paint);
        }
        else
        {
            canvas.DrawPaint(paint);
        }

        canvas.Restore();
    }

    private double Width => ReferenceBox.Size.Width;

    private double Height => ReferenceBox.Size.Height;

    private void UpdateFragmentShader()
    {
        const double turbulenceScale = 1.5;
        double turbulencePhase = _turbulenceSeed + _radiusScale.Value;
        double noisePhase = turbulencePhase;
        double rotation1 = (turbulencePhase * RotateRight) + (1.7 * Math.PI);
        double rotation2 = (turbulencePhase * RotateLeft) + (2.0 * Math.PI);
        double rotation3 = (turbulencePhase * RotateRight) + (2.75 * Math.PI);
        double[] u = _uniforms;
        // uColor
        u[0] = _color.Red / 255.0;
        u[1] = _color.Green / 255.0;
        u[2] = _color.Blue / 255.0;
        u[3] = _color.Alpha / 255.0;
        // Composite 1 (u_alpha, u_sparkle_alpha, u_blur, u_radius_scale)
        u[4] = _alpha.Value;
        u[5] = _sparkleAlpha.Value;
        u[6] = 1.0;
        u[7] = _radiusScale.Value;
        // uCenter
        u[8] = _center.Value.X;
        u[9] = _center.Value.Y;
        // uMaxRadius
        u[10] = _targetRadius;
        // uResolutionScale
        u[11] = 1.0 / Width;
        u[12] = 1.0 / Height;
        // uNoiseScale
        u[13] = NoiseDensity / Width;
        u[14] = NoiseDensity / Height;
        // uNoisePhase
        u[15] = noisePhase / 1000.0;
        // uCircle1
        u[16] = (turbulenceScale * 0.5) + (turbulencePhase * 0.01 * Math.Cos(turbulenceScale * 0.55));
        u[17] = (turbulenceScale * 0.5) + (turbulencePhase * 0.01 * Math.Sin(turbulenceScale * 0.55));
        // uCircle2
        u[18] = (turbulenceScale * 0.2) + (turbulencePhase * -0.0066 * Math.Cos(turbulenceScale * 0.45));
        u[19] = (turbulenceScale * 0.2) + (turbulencePhase * -0.0066 * Math.Sin(turbulenceScale * 0.45));
        // uCircle3
        u[20] = turbulenceScale + (turbulencePhase * -0.0066 * Math.Cos(turbulenceScale * 0.35));
        u[21] = turbulenceScale + (turbulencePhase * -0.0066 * Math.Sin(turbulenceScale * 0.35));
        // uRotation1
        u[22] = Math.Cos(rotation1);
        u[23] = Math.Sin(rotation1);
        // uRotation2
        u[24] = Math.Cos(rotation2);
        u[25] = Math.Sin(rotation2);
        // uRotation3
        u[26] = Math.Cos(rotation3);
        u[27] = Math.Sin(rotation3);
        for (int index = 0; index < u.Length; index++)
        {
            _fragmentShader!.SetFloat(index, u[index]);
        }
    }

    private static void TransformCanvas(Canvas canvas, Matrix4 transform)
    {
        Point? originOffset = MatrixUtils.GetAsTranslation(transform);
        if (originOffset is null)
        {
            canvas.Transform(transform);
        }
        else
        {
            canvas.Translate(originOffset.Value.X, originOffset.Value.Y);
        }
    }

    private static void ClipCanvas(
        Canvas canvas,
        RectCallback clipCallback,
        TextDirection? textDirection = null,
        ShapeBorder? customBorder = null,
        BorderRadius? borderRadius = null)
    {
        BorderRadius radius = borderRadius ?? BorderRadius.Zero;
        Rect rect = clipCallback();
        if (customBorder is not null)
        {
            canvas.ClipPath(customBorder.GetOuterPath(rect, textDirection: textDirection));
        }
        else if (radius != BorderRadius.Zero)
        {
            canvas.ClipRRect(RRect.FromRectAndCorners(rect, radius));
        }
        else
        {
            canvas.ClipRect(rect);
        }
    }
}

// C#-only: Dart's `Tween<Vector2>`, whose default lerp is `begin + (end - begin) * t`.
internal sealed class Vector2Tween(Point begin, Point end) : Tween<Point>(begin, end)
{
    public override Point Lerp(double t) => new(
        Begin.X + ((End.X - Begin.X) * t),
        Begin.Y + ((End.Y - Begin.Y) * t));
}

internal sealed class InkSparkleFactory : InteractiveInkFeatureFactory
{
    public const double ConstantSeed = 1337.0;

    private static bool _initCalled;
    internal static RuntimeFragmentProgram? Program { get; private set; }

    public InkSparkleFactory()
    {
        TurbulenceSeed = null;
    }

    private InkSparkleFactory(double turbulenceSeed)
    {
        TurbulenceSeed = turbulenceSeed;
    }

    public static InkSparkleFactory ConstantTurbulenceSeed() => new(ConstantSeed);

    public static void InitializeShader()
    {
        if (!_initCalled)
        {
            _initCalled = true;
            EventLoop.ScheduleMicrotask(() =>
            {
                using Stream stream = typeof(InkSparkle).Assembly.GetManifestResourceStream(
                    "Plumix.Material.Assets.Shaders.ink_sparkle.sksl")
                    ?? throw new InvalidOperationException("The bundled InkSparkle shader is missing.");
                using var reader = new StreamReader(stream);
                Program = RuntimeFragmentProgram.Compile(reader.ReadToEnd());
            });
        }
    }

    public double? TurbulenceSeed { get; }

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
        return new InkSparkle(
            controller: controller,
            referenceBox: referenceBox,
            position: position,
            color: color,
            textDirection: textDirection,
            containedInkWell: containedInkWell,
            rectCallback: rectCallback,
            borderRadius: borderRadius,
            customBorder: customBorder,
            radius: radius,
            onRemoved: onRemoved,
            turbulenceSeed: TurbulenceSeed);
    }
}
