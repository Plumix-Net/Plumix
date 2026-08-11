using Avalonia;
using Avalonia.Media;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/gradient.dart
public abstract record Gradient
{
    protected internal abstract IBrush CreateBrush();

    protected internal abstract Gradient Scale(double factor);

    public static Gradient? Lerp(Gradient? a, Gradient? b, double t)
    {
        if (ReferenceEquals(a, b) || Equals(a, b))
        {
            return a;
        }

        if (a is null)
        {
            return b?.Scale(t);
        }
        if (b is null)
        {
            return a.Scale(1.0 - t);
        }

        if (a is LinearGradient from && b is LinearGradient to)
        {
            return LinearGradient.Lerp(from, to, t);
        }

        return t < 0.5 ? a : b;
    }
}

public sealed record LinearGradient : Gradient
{
    public LinearGradient(
        IReadOnlyList<Color> colors,
        Alignment? begin = null,
        Alignment? end = null,
        IReadOnlyList<double>? stops = null)
    {
        ArgumentNullException.ThrowIfNull(colors);
        if (colors.Count < 2)
        {
            throw new ArgumentException("A linear gradient requires at least two colors.", nameof(colors));
        }
        if (stops is not null && stops.Count != colors.Count)
        {
            throw new ArgumentException("Gradient stops must match the color count.", nameof(stops));
        }

        Colors = colors.ToArray();
        Begin = begin ?? Alignment.CenterLeft;
        End = end ?? Alignment.CenterRight;
        Stops = stops?.ToArray();
    }

    public IReadOnlyList<Color> Colors { get; }

    public Alignment Begin { get; }

    public Alignment End { get; }

    public IReadOnlyList<double>? Stops { get; }

    protected internal override IBrush CreateBrush()
    {
        var gradientStops = new GradientStops();
        for (int index = 0; index < Colors.Count; index++)
        {
            double offset = Stops?[index] ?? (double)index / (Colors.Count - 1);
            gradientStops.Add(new GradientStop(Colors[index], offset));
        }

        return new LinearGradientBrush
        {
            StartPoint = ToRelativePoint(Begin),
            EndPoint = ToRelativePoint(End),
            GradientStops = gradientStops,
        };
    }

    protected internal override Gradient Scale(double factor)
    {
        Color[] colors = Colors
            .Select(color => Color.FromArgb(
                (byte)Math.Clamp((int)Math.Round(color.A * factor), byte.MinValue, byte.MaxValue),
                color.R,
                color.G,
                color.B))
            .ToArray();
        return new LinearGradient(colors, Begin, End, Stops);
    }

    internal static LinearGradient Lerp(LinearGradient a, LinearGradient b, double t)
    {
        int colorCount = Math.Max(a.Colors.Count, b.Colors.Count);
        var colors = new Color[colorCount];
        double[] stops = new double[colorCount];
        for (int index = 0; index < colorCount; index++)
        {
            double position = colorCount == 1 ? 0.0 : (double)index / (colorCount - 1);
            colors[index] = LerpColor(Sample(a, position), Sample(b, position), t);
            stops[index] = position;
        }

        return new LinearGradient(
            colors,
            begin: LerpAlignment(a.Begin, b.Begin, t),
            end: LerpAlignment(a.End, b.End, t),
            stops: stops);
    }

    private static Alignment LerpAlignment(Alignment a, Alignment b, double t)
    {
        return new Alignment(
            a.X + ((b.X - a.X) * t),
            a.Y + ((b.Y - a.Y) * t));
    }

    private static RelativePoint ToRelativePoint(Alignment alignment)
    {
        return new RelativePoint((alignment.X + 1.0) / 2.0, (alignment.Y + 1.0) / 2.0, RelativeUnit.Relative);
    }

    private static Color Sample(LinearGradient gradient, double position)
    {
        IReadOnlyList<double> stops = gradient.Stops ?? EvenStops(gradient.Colors.Count);
        if (position <= stops[0])
        {
            return gradient.Colors[0];
        }

        for (int index = 1; index < stops.Count; index++)
        {
            if (position <= stops[index])
            {
                double width = stops[index] - stops[index - 1];
                double t = width == 0.0 ? 0.0 : (position - stops[index - 1]) / width;
                return LerpColor(gradient.Colors[index - 1], gradient.Colors[index], t);
            }
        }

        return gradient.Colors[^1];
    }

    private static IReadOnlyList<double> EvenStops(int count)
    {
        double[] stops = new double[count];
        for (int index = 0; index < count; index++)
        {
            stops[index] = (double)index / (count - 1);
        }
        return stops;
    }

    private static Color LerpColor(Color a, Color b, double t)
    {
        return Color.FromArgb(
            LerpChannel(a.A, b.A, t),
            LerpChannel(a.R, b.R, t),
            LerpChannel(a.G, b.G, t),
            LerpChannel(a.B, b.B, t));
    }

    private static byte LerpChannel(byte a, byte b, double t)
    {
        return (byte)Math.Clamp((int)Math.Round(a + ((b - a) * t)), byte.MinValue, byte.MaxValue);
    }
}
