using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/gradient.dart

/// <summary>
/// A transform applied to a gradient's shader, evaluated against the rectangle the gradient covers.
/// </summary>
public abstract record GradientTransform
{
    /// <summary>
    /// Returns the transform for the given bounds, or <see langword="null"/> for the identity transform.
    /// </summary>
    public virtual Matrix? Transform(Rect bounds, TextDirection? textDirection = null) => null;

    /// <summary>
    /// The full shader matrix. Override this to supply perspective; the default preserves existing
    /// affine transforms implemented through <see cref="Transform"/>.
    /// </summary>
    public virtual Matrix4? Transform4(Rect bounds, TextDirection? textDirection = null) =>
        Transform(bounds, textDirection) is { } matrix ? Matrix4.FromAvaloniaMatrix(matrix) : null;
}

/// <summary>A clockwise rotation of a gradient around its center point, in radians.</summary>
public sealed record GradientRotation(double Radians) : GradientTransform
{
    public override Matrix? Transform(Rect bounds, TextDirection? textDirection = null)
    {
        double sinRadians = Math.Sin(Radians);
        double oneMinusCosRadians = 1 - Math.Cos(Radians);
        Point center = bounds.Center;
        double originX = (sinRadians * center.Y) + (oneMinusCosRadians * center.X);
        double originY = (-sinRadians * center.X) + (oneMinusCosRadians * center.Y);

        // Dart composes `Matrix4.identity()..translate(originX, originY)..rotateZ(radians)`, which for a
        // column vector applies the rotation first; Avalonia's row-vector matrices reverse the order.
        return Matrix.CreateRotation(Radians) * Matrix.CreateTranslation(originX, originY);
    }

    public override string ToString() => $"GradientRotation(radians: {DartFormat.Fixed(Radians)})";
}

/// <summary>A 2D gradient, described by colors, optional stops and an optional shader transform.</summary>
public abstract record Gradient
{
    protected Gradient(
        IReadOnlyList<Color> colors,
        IReadOnlyList<double>? stops = null,
        GradientTransform? transform = null)
    {
        ArgumentNullException.ThrowIfNull(colors);
        Colors = colors.ToArray();
        Stops = stops?.ToArray();
        Transform = transform;
    }

    /// <summary>The colors the gradient should obtain at each of the stops.</summary>
    public IReadOnlyList<Color> Colors { get; }

    /// <summary>A list of values from 0.0 to 1.0 that denote fractions along the gradient.</summary>
    public IReadOnlyList<double>? Stops { get; }

    /// <summary>The transform, if any, to apply to the gradient.</summary>
    public GradientTransform? Transform { get; }

    /// <summary>
    /// The stops, defaulting to evenly spaced values when <see cref="Stops"/> was not supplied.
    /// </summary>
    protected internal IReadOnlyList<double> ImpliedStops()
    {
        if (Stops is not null)
        {
            return Stops;
        }

        DebugAssertions.Assert(Colors.Count >= 2);

        double separation = 1.0 / (Colors.Count - 1);
        double[] stops = new double[Colors.Count];
        for (int index = 0; index < stops.Length; index++)
        {
            stops[index] = index * separation;
        }

        return stops;
    }

    /// <summary>Creates a shader for the framework Canvas and ShaderMask, in absolute canvas coordinates.</summary>
    /// <remarks>
    /// The IBrush return type is retained for compatibility. Native shader descriptions are interpreted
    /// by Plumix's drawing pipeline; they are not standalone Avalonia gradient brushes.
    /// </remarks>
    public abstract IBrush CreateShader(Rect rect, TextDirection? textDirection = null);

    /// <summary>Returns a new gradient with its colors' alpha scaled by the given factor.</summary>
    public abstract Gradient Scale(double factor);

    /// <summary>Returns a new gradient whose colors all have the given opacity.</summary>
    public abstract Gradient WithOpacity(double opacity);

    /// <summary>Returns a gradient of this shape whose every color is the given color.</summary>
    public virtual Gradient FromColor(Color color)
    {
        return new LinearGradient(
            colors: Enumerable.Repeat(color, Colors.Count).ToArray(),
            stops: Stops,
            transform: Transform);
    }

    /// <summary>Linearly interpolates from another gradient to this one; null when unsupported.</summary>
    protected virtual Gradient? LerpFrom(Gradient? a, double t)
    {
        return a is null ? Scale(t) : null;
    }

    /// <summary>Linearly interpolates from this gradient to another; null when unsupported.</summary>
    protected virtual Gradient? LerpTo(Gradient? b, double t)
    {
        return b is null ? Scale(1.0 - t) : null;
    }

    /// <summary>Linearly interpolates between two gradients, cross-fading unrelated kinds.</summary>
    public static Gradient? Lerp(Gradient? a, Gradient? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        Gradient? result = b?.LerpFrom(a, t);
        if (result is null && a is not null)
        {
            result = a.LerpTo(b, t);
        }

        if (result is not null)
        {
            return result;
        }

        return t < 0.5
            ? a!.Scale(1.0 - (t * 2.0))
            : b!.Scale((t - 0.5) * 2.0);
    }

    /// <summary>Resolves <see cref="Transform"/> against the bounds the gradient is painted into.</summary>
    protected Matrix4? ResolveTransform(Rect bounds, TextDirection? textDirection)
    {
        return Transform?.Transform4(bounds, textDirection);
    }

    /// dart:ui `Gradient._validateColorStops`, which the framework can only trip on unequal lengths.
    private protected IReadOnlyList<double> ValidatedStops()
    {
        IReadOnlyList<double> stops = ImpliedStops();
        if (stops.Count != Colors.Count)
        {
            throw new ArgumentException(
                "\"colors\" and \"colorStops\" arguments must have equal length.",
                nameof(Stops));
        }

        return stops;
    }

    /// dart:ui `Gradient._sample`.
    private protected static Color Sample(IReadOnlyList<Color> colors, IReadOnlyList<double> stops, double t)
    {
        DebugAssertions.Assert(colors.Count > 0 && stops.Count > 0);
        if (t <= stops[0])
        {
            return colors[0];
        }

        if (t >= stops[^1])
        {
            return colors[^1];
        }

        int index = 0;
        for (int candidate = stops.Count - 1; candidate >= 0; candidate--)
        {
            if (stops[candidate] <= t)
            {
                index = candidate;
                break;
            }
        }

        return LerpColor(
            colors[index],
            colors[index + 1],
            (t - stops[index]) / (stops[index + 1] - stops[index]));
    }

    /// dart:ui `Gradient._interpolateColorsAndStops`, which resamples onto the union of both stop lists.
    private protected static (IReadOnlyList<Color> Colors, IReadOnlyList<double> Stops) InterpolateColorsAndStops(
        IReadOnlyList<Color> aColors,
        IReadOnlyList<double> aStops,
        IReadOnlyList<Color> bColors,
        IReadOnlyList<double> bStops,
        double t)
    {
        DebugAssertions.Assert(aColors.Count >= 2 && bColors.Count >= 2);
        DebugAssertions.Assert(aStops.Count == aColors.Count && bStops.Count == bColors.Count);
        var sorted = new SortedSet<double>(aStops);
        sorted.UnionWith(bStops);
        double[] interpolatedStops = [.. sorted];
        var interpolatedColors = new Color[interpolatedStops.Length];
        for (int index = 0; index < interpolatedStops.Length; index++)
        {
            double stop = interpolatedStops[index];
            interpolatedColors[index] = LerpColor(
                Sample(aColors, aStops, stop),
                Sample(bColors, bStops, stop),
                t);
        }

        return (interpolatedColors, interpolatedStops);
    }

    /// dart:ui `Color.lerp` with a null endpoint, which scales the other endpoint's alpha.
    private protected static IReadOnlyList<Color> ScaleColors(IReadOnlyList<Color> colors, double factor) =>
        [.. colors.Select(color => Color.Lerp(null, color, factor))];

    /// dart:ui `Color.withOpacity`, which replaces the alpha rather than scaling it.
    private protected static IReadOnlyList<Color> ColorsWithOpacity(IReadOnlyList<Color> colors, double opacity) =>
        [.. colors.Select(color => color.WithOpacity(opacity))];

    private protected static Color LerpColor(Color a, Color b, double t) => Color.Lerp(a, b, t);

    private protected static double LerpDouble(double a, double b, double t) => a + ((b - a) * t);

    private protected static bool StopsEqual(IReadOnlyList<double>? a, IReadOnlyList<double>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        return a is not null && b is not null && a.SequenceEqual(b);
    }

    private protected static bool ColorsEqual(IReadOnlyList<Color> a, IReadOnlyList<Color> b)
    {
        return ReferenceEquals(a, b) || a.SequenceEqual(b);
    }

    private protected int GradientHashCode()
    {
        var hash = default(HashCode);
        foreach (Color color in Colors)
        {
            hash.Add(color);
        }

        if (Stops is not null)
        {
            foreach (double stop in Stops)
            {
                hash.Add(stop);
            }
        }

        hash.Add(Transform);
        return hash.ToHashCode();
    }

    private protected string FormatColors()
    {
        return "[" + string.Join(", ", Colors.Select(color => color.ToString())) + "]";
    }

    private protected string FormatStops()
    {
        return Stops is null ? string.Empty : ", stops: [" + string.Join(", ", Stops.Select(DartFormat.Number)) + "]";
    }

    private protected string FormatTransform()
    {
        return Transform is null ? string.Empty : $", transform: {Transform}";
    }
}

/// <summary>A 2D linear gradient, interpolating between <c>Begin</c> and <c>End</c>.</summary>
public sealed record LinearGradient : Gradient
{
    public LinearGradient(
        IReadOnlyList<Color> colors,
        AlignmentGeometry? begin = null,
        AlignmentGeometry? end = null,
        IReadOnlyList<double>? stops = null,
        TileMode tileMode = TileMode.Clamp,
        GradientTransform? transform = null)
        : base(colors, stops, transform)
    {
        Begin = begin ?? Alignment.CenterLeft;
        End = end ?? Alignment.CenterRight;
        TileMode = tileMode;
    }

    /// <summary>The offset at which stop 0.0 of the gradient is placed.</summary>
    public AlignmentGeometry Begin { get; }

    /// <summary>The offset at which stop 1.0 of the gradient is placed.</summary>
    public AlignmentGeometry End { get; }

    /// <summary>How this gradient should tile the plane beyond the region before/after its ends.</summary>
    public TileMode TileMode { get; }

    public override IBrush CreateShader(Rect rect, TextDirection? textDirection = null)
    {
        return GradientShader.Linear(
            Begin.Resolve(textDirection).WithinRect(rect),
            End.Resolve(textDirection).WithinRect(rect),
            Colors,
            ValidatedStops(),
            TileMode,
            ResolveTransform(rect, textDirection));
    }

    public override LinearGradient Scale(double factor)
    {
        return new LinearGradient(ScaleColors(Colors, factor), Begin, End, Stops, TileMode, Transform);
    }

    public override LinearGradient WithOpacity(double opacity)
    {
        return new LinearGradient(ColorsWithOpacity(Colors, opacity), Begin, End, Stops, TileMode, Transform);
    }

    public override LinearGradient FromColor(Color color)
    {
        return new LinearGradient(
            Enumerable.Repeat(color, Colors.Count).ToArray(),
            Begin,
            End,
            Stops,
            TileMode,
            Transform);
    }

    protected override Gradient? LerpFrom(Gradient? a, double t)
    {
        return a is null or LinearGradient ? Lerp((LinearGradient?)a, this, t) : base.LerpFrom(a, t);
    }

    protected override Gradient? LerpTo(Gradient? b, double t)
    {
        return b is null or LinearGradient ? Lerp(this, (LinearGradient?)b, t) : base.LerpTo(b, t);
    }

    public static LinearGradient? Lerp(LinearGradient? a, LinearGradient? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a is null)
        {
            return b!.Scale(t);
        }

        if (b is null)
        {
            return a.Scale(1.0 - t);
        }

        (IReadOnlyList<Color> colors, IReadOnlyList<double> stops) = InterpolateColorsAndStops(
            a.Colors,
            a.ImpliedStops(),
            b.Colors,
            b.ImpliedStops(),
            t);
        return new LinearGradient(
            colors,
            begin: AlignmentGeometry.Lerp(a.Begin, b.Begin, t),
            end: AlignmentGeometry.Lerp(a.End, b.End, t),
            stops: stops,
            tileMode: t < 0.5 ? a.TileMode : b.TileMode,
            transform: t < 0.5 ? a.Transform : b.Transform);
    }

    public bool Equals(LinearGradient? other)
    {
        return other is not null
               && Begin == other.Begin
               && End == other.End
               && TileMode == other.TileMode
               && Equals(Transform, other.Transform)
               && ColorsEqual(Colors, other.Colors)
               && StopsEqual(Stops, other.Stops);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Begin, End, TileMode, GradientHashCode());
    }

    public override string ToString()
    {
        return $"LinearGradient(begin: {Begin}, end: {End}, colors: {FormatColors()}{FormatStops()}"
               + $", tileMode: {DartFormat.Enum(TileMode)}{FormatTransform()})";
    }
}

/// <summary>A 2D radial gradient, optionally focused away from its center.</summary>
public sealed record RadialGradient : Gradient
{
    public RadialGradient(
        IReadOnlyList<Color> colors,
        AlignmentGeometry? center = null,
        double radius = 0.5,
        IReadOnlyList<double>? stops = null,
        TileMode tileMode = TileMode.Clamp,
        AlignmentGeometry? focal = null,
        double focalRadius = 0.0,
        GradientTransform? transform = null)
        : base(colors, stops, transform)
    {
        Center = center ?? Alignment.Center;
        Radius = radius;
        TileMode = tileMode;
        Focal = focal;
        FocalRadius = focalRadius;
    }

    /// <summary>The center of the gradient, as a fraction of the paint box.</summary>
    public AlignmentGeometry Center { get; }

    /// <summary>The radius of the gradient, as a fraction of the shortest side of the paint box.</summary>
    public double Radius { get; }

    /// <summary>How this gradient should tile the plane beyond its outer ring.</summary>
    public TileMode TileMode { get; }

    /// <summary>The focal point of the gradient, if it is not the center.</summary>
    public AlignmentGeometry? Focal { get; }

    /// <summary>The radius of the focal point, as a fraction of the shortest side of the paint box.</summary>
    public double FocalRadius { get; }

    public override IBrush CreateShader(Rect rect, TextDirection? textDirection = null)
    {
        double shortestSide = Math.Min(Math.Abs(rect.Width), Math.Abs(rect.Height));
        return GradientShader.Radial(
            Center.Resolve(textDirection).WithinRect(rect),
            Radius * shortestSide,
            Colors,
            ValidatedStops(),
            TileMode,
            ResolveTransform(rect, textDirection),
            Focal?.Resolve(textDirection).WithinRect(rect),
            FocalRadius * shortestSide);
    }

    public override RadialGradient Scale(double factor)
    {
        return new RadialGradient(
            ScaleColors(Colors, factor),
            Center,
            Radius,
            Stops,
            TileMode,
            Focal,
            FocalRadius,
            Transform);
    }

    public override RadialGradient WithOpacity(double opacity)
    {
        return new RadialGradient(
            ColorsWithOpacity(Colors, opacity),
            Center,
            Radius,
            Stops,
            TileMode,
            Focal,
            FocalRadius,
            Transform);
    }

    public override RadialGradient FromColor(Color color)
    {
        return new RadialGradient(
            Enumerable.Repeat(color, Colors.Count).ToArray(),
            Center,
            Radius,
            Stops,
            TileMode,
            Focal,
            FocalRadius,
            Transform);
    }

    protected override Gradient? LerpFrom(Gradient? a, double t)
    {
        return a is null or RadialGradient ? Lerp((RadialGradient?)a, this, t) : base.LerpFrom(a, t);
    }

    protected override Gradient? LerpTo(Gradient? b, double t)
    {
        return b is null or RadialGradient ? Lerp(this, (RadialGradient?)b, t) : base.LerpTo(b, t);
    }

    public static RadialGradient? Lerp(RadialGradient? a, RadialGradient? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a is null)
        {
            return b!.Scale(t);
        }

        if (b is null)
        {
            return a.Scale(1.0 - t);
        }

        (IReadOnlyList<Color> colors, IReadOnlyList<double> stops) = InterpolateColorsAndStops(
            a.Colors,
            a.ImpliedStops(),
            b.Colors,
            b.ImpliedStops(),
            t);
        return new RadialGradient(
            colors,
            center: AlignmentGeometry.Lerp(a.Center, b.Center, t),
            radius: Math.Max(0.0, LerpDouble(a.Radius, b.Radius, t)),
            stops: stops,
            tileMode: t < 0.5 ? a.TileMode : b.TileMode,
            focal: AlignmentGeometry.Lerp(a.Focal, b.Focal, t),
            focalRadius: Math.Max(0.0, LerpDouble(a.FocalRadius, b.FocalRadius, t)),
            transform: t < 0.5 ? a.Transform : b.Transform);
    }

    public bool Equals(RadialGradient? other)
    {
        return other is not null
               && Center == other.Center
               && Radius.Equals(other.Radius)
               && TileMode == other.TileMode
               && Nullable.Equals(Focal, other.Focal)
               && FocalRadius.Equals(other.FocalRadius)
               && Equals(Transform, other.Transform)
               && ColorsEqual(Colors, other.Colors)
               && StopsEqual(Stops, other.Stops);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Center, Radius, TileMode, Focal, FocalRadius, GradientHashCode());
    }

    public override string ToString()
    {
        string focal = Focal is null ? string.Empty : $", focal: {Focal}";
        return $"RadialGradient(center: {Center}, radius: {DartFormat.Fixed(Radius)}, "
               + $"colors: {FormatColors()}{FormatStops()}, tileMode: {DartFormat.Enum(TileMode)}{focal}"
               + $", focalRadius: {DartFormat.Fixed(FocalRadius)}{FormatTransform()})";
    }
}

/// <summary>A 2D sweep gradient, sweeping from <c>StartAngle</c> to <c>EndAngle</c> around a center.</summary>
public sealed record SweepGradient : Gradient
{
    public SweepGradient(
        IReadOnlyList<Color> colors,
        AlignmentGeometry? center = null,
        double startAngle = 0.0,
        double? endAngle = null,
        IReadOnlyList<double>? stops = null,
        TileMode tileMode = TileMode.Clamp,
        GradientTransform? transform = null)
        : base(colors, stops, transform)
    {
        Center = center ?? Alignment.Center;
        StartAngle = startAngle;
        EndAngle = endAngle ?? (Math.PI * 2);
        TileMode = tileMode;
    }

    /// <summary>The center of the gradient, as a fraction of the paint box.</summary>
    public AlignmentGeometry Center { get; }

    /// <summary>The angle in radians at which stop 0.0 of the gradient is placed.</summary>
    public double StartAngle { get; }

    /// <summary>The angle in radians at which stop 1.0 of the gradient is placed.</summary>
    public double EndAngle { get; }

    /// <summary>How this gradient should tile the plane outside its sweep.</summary>
    public TileMode TileMode { get; }

    public override IBrush CreateShader(Rect rect, TextDirection? textDirection = null)
    {
        return GradientShader.Sweep(
            Center.Resolve(textDirection).WithinRect(rect),
            Colors,
            ValidatedStops(),
            TileMode,
            StartAngle,
            EndAngle,
            ResolveTransform(rect, textDirection));
    }

    public override SweepGradient Scale(double factor)
    {
        return new SweepGradient(
            ScaleColors(Colors, factor),
            Center,
            StartAngle,
            EndAngle,
            Stops,
            TileMode,
            Transform);
    }

    public override SweepGradient WithOpacity(double opacity)
    {
        return new SweepGradient(
            ColorsWithOpacity(Colors, opacity),
            Center,
            StartAngle,
            EndAngle,
            Stops,
            TileMode,
            Transform);
    }

    public override SweepGradient FromColor(Color color)
    {
        return new SweepGradient(
            Enumerable.Repeat(color, Colors.Count).ToArray(),
            Center,
            StartAngle,
            EndAngle,
            Stops,
            TileMode,
            Transform);
    }

    protected override Gradient? LerpFrom(Gradient? a, double t)
    {
        return a is null or SweepGradient ? Lerp((SweepGradient?)a, this, t) : base.LerpFrom(a, t);
    }

    protected override Gradient? LerpTo(Gradient? b, double t)
    {
        return b is null or SweepGradient ? Lerp(this, (SweepGradient?)b, t) : base.LerpTo(b, t);
    }

    public static SweepGradient? Lerp(SweepGradient? a, SweepGradient? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a is null)
        {
            return b!.Scale(t);
        }

        if (b is null)
        {
            return a.Scale(1.0 - t);
        }

        (IReadOnlyList<Color> colors, IReadOnlyList<double> stops) = InterpolateColorsAndStops(
            a.Colors,
            a.ImpliedStops(),
            b.Colors,
            b.ImpliedStops(),
            t);
        return new SweepGradient(
            colors,
            center: AlignmentGeometry.Lerp(a.Center, b.Center, t),
            startAngle: Math.Max(0.0, LerpDouble(a.StartAngle, b.StartAngle, t)),
            endAngle: Math.Max(0.0, LerpDouble(a.EndAngle, b.EndAngle, t)),
            stops: stops,
            tileMode: t < 0.5 ? a.TileMode : b.TileMode,
            transform: t < 0.5 ? a.Transform : b.Transform);
    }

    public bool Equals(SweepGradient? other)
    {
        return other is not null
               && Center == other.Center
               && StartAngle.Equals(other.StartAngle)
               && EndAngle.Equals(other.EndAngle)
               && TileMode == other.TileMode
               && Equals(Transform, other.Transform)
               && ColorsEqual(Colors, other.Colors)
               && StopsEqual(Stops, other.Stops);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Center, StartAngle, EndAngle, TileMode, GradientHashCode());
    }

    public override string ToString()
    {
        return $"SweepGradient(center: {Center}, startAngle: {DartFormat.Fixed(StartAngle)}, "
               + $"endAngle: {DartFormat.Fixed(EndAngle)}, colors: {FormatColors()}{FormatStops()}"
               + $", tileMode: {DartFormat.Enum(TileMode)}{FormatTransform()})";
    }
}
