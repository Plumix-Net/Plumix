using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Material;

// Dart parity sources:
// - material_ui/lib/src/ink_splash.dart
// - material_ui/lib/src/ink_ripple.dart
// - material_ui/lib/src/ink_sparkle.dart
// - material_ui/lib/src/no_splash.dart
// - material_ui/lib/src/ink_well.dart

public sealed record InkFeatureConfiguration(
    Point Position,
    Color Color,
    TextDirection TextDirection = TextDirection.Ltr,
    bool ContainedInkWell = false,
    BorderRadius? BorderRadius = null,
    ShapeBorder? CustomBorder = null,
    double? Radius = null);

public enum InkFeatureKind
{
    None,
    Splash,
    Ripple,
    Sparkle,
}

public sealed record InkFeatureFrame(
    InkFeatureKind Kind,
    Point Center,
    double Radius,
    double Opacity,
    double SparkleOpacity = 0.0,
    double TurbulenceSeed = 0.0);

public abstract class InteractiveInkFeatureFactory
{
    public abstract InteractiveInkFeature Create(InkFeatureConfiguration configuration);
}

public abstract class InteractiveInkFeature
{
    protected InteractiveInkFeature(InkFeatureConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        if (configuration.Radius.HasValue
            && (!double.IsFinite(configuration.Radius.Value) || configuration.Radius.Value <= 0.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(configuration),
                "The ink feature radius must be finite and greater than zero.");
        }
    }

    public InkFeatureConfiguration Configuration { get; private set; }

    internal void UpdateConfiguration(InkFeatureConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public abstract TimeSpan UnconfirmedDuration { get; }

    public abstract TimeSpan ConfirmDuration { get; }

    public abstract TimeSpan CancelDuration { get; }

    public abstract InkFeatureFrame ResolveFrame(Size size, double progress, bool confirmed, bool canceled);

    internal virtual InkFeatureFrame ResolveFrame(Rect bounds, double progress, bool confirmed, bool canceled)
    {
        InkFeatureFrame frame = ResolveFrame(bounds.Size, progress, confirmed, canceled);
        return frame with { Center = frame.Center + (Vector)bounds.Position };
    }

    protected InkFeatureFrame ResolveFrameInBounds(
        Rect bounds,
        double progress,
        bool confirmed,
        bool canceled,
        Func<Size, Point, double, bool, bool, InkFeatureFrame> resolver)
    {
        Point configuredPosition = Configuration.Position;
        Point localPosition = double.IsNaN(configuredPosition.X) || double.IsNaN(configuredPosition.Y)
            ? configuredPosition
            : configuredPosition - bounds.Position;
        Point position = ResolvePosition(bounds.Size, localPosition);
        InkFeatureFrame frame = resolver(bounds.Size, position, progress, confirmed, canceled);
        return frame with { Center = frame.Center + (Vector)bounds.Position };
    }

    protected static Point ResolvePosition(Size size, Point position)
    {
        return double.IsNaN(position.X) || double.IsNaN(position.Y)
            ? new Point(size.Width / 2.0, size.Height / 2.0)
            : position;
    }

    protected double ResolveContainedTargetRadius(Size size, Point position)
    {
        if (Configuration.Radius.HasValue)
        {
            return Configuration.Radius.Value;
        }

        double[] distances =
        [
            Distance(position, new Point(0.0, 0.0)),
            Distance(position, new Point(size.Width, 0.0)),
            Distance(position, new Point(0.0, size.Height)),
            Distance(position, new Point(size.Width, size.Height)),
        ];
        return distances.Max();
    }

    protected double ResolveDiagonalTargetRadius(Size size)
    {
        if (Configuration.Radius.HasValue)
        {
            return Configuration.Radius.Value;
        }

        return Math.Sqrt((size.Width * size.Width) + (size.Height * size.Height)) / 2.0;
    }

    protected static Color ApplyOpacity(Color color, double opacity)
    {
        double normalized = Math.Clamp(opacity, 0.0, 1.0);
        byte alpha = (byte)Math.Clamp((int)Math.Round(color.A * normalized), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}

/// <summary>An interactive ink feature that deliberately paints no splash.</summary>
/// <remarks>Dart parity source: material_ui/lib/src/no_splash.dart.</remarks>
public sealed class NoSplash : InteractiveInkFeature
{
    public NoSplash(InkFeatureConfiguration configuration) : base(configuration)
    {
    }

    public static InteractiveInkFeatureFactory SplashFactory { get; } = new NoSplashFactory();

    public override TimeSpan UnconfirmedDuration => TimeSpan.Zero;

    public override TimeSpan ConfirmDuration => TimeSpan.Zero;

    public override TimeSpan CancelDuration => TimeSpan.Zero;

    public override InkFeatureFrame ResolveFrame(Size size, double progress, bool confirmed, bool canceled)
    {
        Point position = ResolvePosition(size, Configuration.Position);
        return ResolveFrameCore(size, position, progress, confirmed, canceled);
    }

    internal override InkFeatureFrame ResolveFrame(Rect bounds, double progress, bool confirmed, bool canceled)
    {
        return ResolveFrameInBounds(bounds, progress, confirmed, canceled, ResolveFrameCore);
    }

    private static InkFeatureFrame ResolveFrameCore(
        Size size,
        Point position,
        double progress,
        bool confirmed,
        bool canceled)
    {
        return new InkFeatureFrame(InkFeatureKind.None, position, 0.0, 0.0);
    }

    private sealed class NoSplashFactory : InteractiveInkFeatureFactory
    {
        public override InteractiveInkFeature Create(InkFeatureConfiguration configuration)
        {
            return new NoSplash(configuration);
        }
    }
}

public sealed class InkSplash : InteractiveInkFeature
{
    private const double DefaultSplashRadius = 35.0;

    public InkSplash(InkFeatureConfiguration configuration) : base(configuration)
    {
    }

    public static InteractiveInkFeatureFactory SplashFactory { get; } = new InkSplashFactory();

    public override TimeSpan UnconfirmedDuration => TimeSpan.FromSeconds(1.0);

    public override TimeSpan ConfirmDuration => TimeSpan.FromMilliseconds(200.0);

    public override TimeSpan CancelDuration => TimeSpan.FromMilliseconds(200.0);

    public override InkFeatureFrame ResolveFrame(Size size, double progress, bool confirmed, bool canceled)
    {
        Point position = ResolvePosition(size, Configuration.Position);
        return ResolveFrameCore(size, position, progress, confirmed, canceled);
    }

    internal override InkFeatureFrame ResolveFrame(Rect bounds, double progress, bool confirmed, bool canceled)
    {
        return ResolveFrameInBounds(bounds, progress, confirmed, canceled, ResolveFrameCore);
    }

    private InkFeatureFrame ResolveFrameCore(
        Size size,
        Point position,
        double progress,
        bool confirmed,
        bool canceled)
    {
        double t = Math.Clamp(progress, 0.0, 1.0);
        Point center = Configuration.ContainedInkWell
            ? position
            : Lerp(position, new Point(size.Width / 2.0, size.Height / 2.0), t);
        double targetRadius = Configuration.Radius
                              ?? (Configuration.ContainedInkWell
                                  ? ResolveContainedTargetRadius(size, position)
                                  : DefaultSplashRadius);
        double opacity = confirmed || canceled ? 1.0 - t : 1.0;
        return new InkFeatureFrame(InkFeatureKind.Splash, center, targetRadius * t, opacity);
    }

    private sealed class InkSplashFactory : InteractiveInkFeatureFactory
    {
        public override InteractiveInkFeature Create(InkFeatureConfiguration configuration)
        {
            return new InkSplash(configuration);
        }
    }

    private static Point Lerp(Point begin, Point end, double t)
    {
        return new Point(
            begin.X + ((end.X - begin.X) * t),
            begin.Y + ((end.Y - begin.Y) * t));
    }
}

public sealed class InkRipple : InteractiveInkFeature
{
    public InkRipple(InkFeatureConfiguration configuration) : base(configuration)
    {
    }

    public static InteractiveInkFeatureFactory SplashFactory { get; } = new InkRippleFactory();

    public override TimeSpan UnconfirmedDuration => TimeSpan.FromSeconds(1.0);

    public override TimeSpan ConfirmDuration => TimeSpan.FromMilliseconds(375.0);

    public override TimeSpan CancelDuration => TimeSpan.FromMilliseconds(75.0);

    public override InkFeatureFrame ResolveFrame(Size size, double progress, bool confirmed, bool canceled)
    {
        Point position = ResolvePosition(size, Configuration.Position);
        return ResolveFrameCore(size, position, progress, confirmed, canceled);
    }

    internal override InkFeatureFrame ResolveFrame(Rect bounds, double progress, bool confirmed, bool canceled)
    {
        return ResolveFrameInBounds(bounds, progress, confirmed, canceled, ResolveFrameCore);
    }

    private InkFeatureFrame ResolveFrameCore(
        Size size,
        Point position,
        double progress,
        bool confirmed,
        bool canceled)
    {
        double t = Math.Clamp(progress, 0.0, 1.0);
        double eased = Curves.Ease(t);
        Point targetCenter = new(size.Width / 2.0, size.Height / 2.0);
        Point center = new(
            position.X + ((targetCenter.X - position.X) * eased),
            position.Y + ((targetCenter.Y - position.Y) * eased));
        double targetRadius = ResolveDiagonalTargetRadius(size);
        double radius = Lerp(targetRadius * 0.30, targetRadius + 5.0, eased);
        double opacity = canceled
            ? 1.0 - t
            : confirmed
                ? ResolveConfirmedOpacity(t)
                : Math.Clamp(t / 0.075, 0.0, 1.0);
        return new InkFeatureFrame(InkFeatureKind.Ripple, center, radius, opacity);
    }

    private static double ResolveConfirmedOpacity(double t)
    {
        const double fadeInEnd = 75.0 / 375.0;
        const double fadeOutStart = 225.0 / 375.0;
        if (t < fadeInEnd)
        {
            return t / fadeInEnd;
        }

        if (t <= fadeOutStart)
        {
            return 1.0;
        }

        return 1.0 - ((t - fadeOutStart) / (1.0 - fadeOutStart));
    }

    private static double Lerp(double begin, double end, double t)
    {
        return begin + ((end - begin) * t);
    }

    private sealed class InkRippleFactory : InteractiveInkFeatureFactory
    {
        public override InteractiveInkFeature Create(InkFeatureConfiguration configuration)
        {
            return new InkRipple(configuration);
        }
    }
}

public sealed class InkSparkle : InteractiveInkFeature
{
    private const double TargetRadiusMultiplier = 2.3;
    private readonly double _turbulenceSeed;

    public InkSparkle(InkFeatureConfiguration configuration, double? turbulenceSeed = null) : base(configuration)
    {
        _turbulenceSeed = turbulenceSeed ?? Random.Shared.NextDouble() * 1000.0;
    }

    public static InteractiveInkFeatureFactory SplashFactory { get; } = new InkSparkleFactory(null);

    public static InteractiveInkFeatureFactory ConstantTurbulenceSeedSplashFactory { get; } =
        new InkSparkleFactory(1337.0);

    public double TurbulenceSeed => _turbulenceSeed;

    public override TimeSpan UnconfirmedDuration => TimeSpan.FromMilliseconds(617.0);

    public override TimeSpan ConfirmDuration => TimeSpan.FromMilliseconds(617.0);

    public override TimeSpan CancelDuration => TimeSpan.FromMilliseconds(617.0);

    public override InkFeatureFrame ResolveFrame(Size size, double progress, bool confirmed, bool canceled)
    {
        Point position = ResolvePosition(size, Configuration.Position);
        return ResolveFrameCore(size, position, progress, confirmed, canceled);
    }

    internal override InkFeatureFrame ResolveFrame(Rect bounds, double progress, bool confirmed, bool canceled)
    {
        return ResolveFrameInBounds(bounds, progress, confirmed, canceled, ResolveFrameCore);
    }

    private InkFeatureFrame ResolveFrameCore(
        Size size,
        Point position,
        double progress,
        bool confirmed,
        bool canceled)
    {
        double t = Math.Clamp(progress, 0.0, 1.0);
        double radiusProgress = t < 0.75 ? Curves.FastOutSlowIn(t / 0.75) : 1.0;
        double centerProgress = Math.Clamp(radiusProgress * 2.0, 0.0, 1.0);
        Point targetCenter = new(size.Width / 2.0, size.Height / 2.0);
        Point center = new(
            position.X + ((targetCenter.X - position.X) * centerProgress),
            position.Y + ((targetCenter.Y - position.Y) * centerProgress));
        double targetRadius = ResolveDiagonalTargetRadius(size) * TargetRadiusMultiplier;
        double opacity = ResolveSequence(t, 0.13, 0.40, 1.0);
        double sparkleOpacity = ResolveSequence(t, 13.0 / 90.0, 40.0 / 90.0, 1.0);
        return new InkFeatureFrame(
            InkFeatureKind.Sparkle,
            center,
            targetRadius * radiusProgress,
            opacity,
            sparkleOpacity,
            _turbulenceSeed);
    }

    private static double ResolveSequence(double t, double fadeInEnd, double holdEnd, double fadeOutEnd)
    {
        if (t < fadeInEnd)
        {
            return t / fadeInEnd;
        }

        if (t <= holdEnd)
        {
            return 1.0;
        }

        if (t >= fadeOutEnd)
        {
            return 0.0;
        }

        return 1.0 - ((t - holdEnd) / (fadeOutEnd - holdEnd));
    }

    private sealed class InkSparkleFactory : InteractiveInkFeatureFactory
    {
        private readonly double? _turbulenceSeed;

        public InkSparkleFactory(double? turbulenceSeed)
        {
            _turbulenceSeed = turbulenceSeed;
        }

        public override InteractiveInkFeature Create(InkFeatureConfiguration configuration)
        {
            return new InkSparkle(configuration, _turbulenceSeed);
        }
    }
}
