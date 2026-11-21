using Plumix.Rendering;
using Plumix.Widgets;
using TileMode = Plumix.Rendering.TileMode;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Gradient)
// Engine adapter: engine/src/flutter/lib/ui/painting/gradient.cc and
// engine/src/flutter/display_list/skia/dl_sk_conversions.cc.

/// <summary>
/// Dart's <c>ui.Gradient</c>: a linear, radial, two-point conical or sweep gradient, as data. The
/// backend builds the native gradient from it (<see cref="SkiaShaders"/>).
/// </summary>
internal sealed class GradientShader : Shader
{

    private GradientShader(
        GradientKind kind,
        Offset start,
        Offset end,
        double radius,
        double focalRadius,
        IReadOnlyList<Color> colors,
        IReadOnlyList<double> stops,
        TileMode tileMode,
        Matrix4? matrix,
        double startAngle = 0.0,
        double endAngle = 0.0)
    {
        if (colors.Count != stops.Count)
        {
            throw new ArgumentException("\"colors\" and \"colorStops\" arguments must have equal length.");
        }

        Kind = kind;
        Start = start;
        End = end;
        Radius = radius;
        FocalRadius = focalRadius;
        TileMode = tileMode;
        StartAngle = startAngle;
        EndAngle = endAngle;
        Colors = [.. colors];
        Stops = [.. stops];
        // The engine hands Skia extended-sRGB 32-bit colours and float stops.
        ColorValues = [.. colors.Select(color => color.WithValues(colorSpace: ColorSpace.ExtendedSRGB).ToARGB32())];
        StopValues = [.. stops.Select(value => (float)value)];
        if (matrix is not null)
        {
            double[] values = matrix.Storage;
            DebugAssertions.Assert(values.Length == 16 && values.All(double.IsFinite),
                "'matrix4' must have 16 finite entries.");
            // The 3x3 local matrix, row-major (Skia's SkMatrix order).
            LocalMatrix =
            [
                Path.Narrow(values[0]), Path.Narrow(values[4]), Path.Narrow(values[12]),
                Path.Narrow(values[1]), Path.Narrow(values[5]), Path.Narrow(values[13]),
                Path.Narrow(values[3]), Path.Narrow(values[7]), Path.Narrow(values[15]),
            ];
        }
    }

    internal IReadOnlyList<Color> Colors { get; }
    internal IReadOnlyList<double> Stops { get; }

    /// <summary>The colours as extended-sRGB ARGB32 values.</summary>
    internal uint[] ColorValues { get; }

    internal float[] StopValues { get; }

    /// <summary>The local matrix as nine row-major floats, or null for the identity.</summary>
    internal float[]? LocalMatrix { get; }

    internal GradientKind Kind { get; }
    internal Offset Start { get; }
    internal Offset End { get; }
    internal double Radius { get; }
    internal double FocalRadius { get; }
    internal TileMode TileMode { get; }
    internal double StartAngle { get; }
    internal double EndAngle { get; }

    internal static GradientShader Linear(
        Offset from, Offset to, IReadOnlyList<Color> colors, IReadOnlyList<double> stops,
        TileMode tileMode, Matrix4? matrix)
    {
        CheckPoint(from);
        CheckPoint(to);
        return new GradientShader(GradientKind.Linear, from, to, 0.0, 0.0, colors, stops, tileMode, matrix);
    }

    internal static GradientShader Radial(
        Offset center, double radius, IReadOnlyList<Color> colors, IReadOnlyList<double> stops,
        TileMode tileMode, Matrix4? matrix, Offset? focal, double focalRadius)
    {
        CheckPoint(center);
        bool radial = focal is null || (focal == center && focalRadius == 0.0);
        DebugAssertions.Assert(radial || center != default || focal != default(Offset),
            "center != Offset.zero || focal != Offset.zero");
        return new GradientShader(radial ? GradientKind.Radial : GradientKind.Conical,
            focal ?? center, center, radius, focalRadius, colors, stops, tileMode, matrix);
    }

    internal static GradientShader Sweep(
        Offset center, IReadOnlyList<Color> colors, IReadOnlyList<double> stops,
        TileMode tileMode, double startAngle, double endAngle, Matrix4? matrix)
    {
        CheckPoint(center);
        DebugAssertions.Assert(startAngle < endAngle, "startAngle < endAngle");
        return new GradientShader(GradientKind.Sweep, center, center, 0.0, 0.0,
            colors, stops, tileMode, matrix, startAngle, endAngle);
    }

    private static void CheckPoint(Offset point) => DebugAssertions.Assert(
        !double.IsNaN(point.Dx) && !double.IsNaN(point.Dy), "Offset argument contained a NaN value.");
}

internal enum GradientKind
{
    Linear,
    Radial,
    Conical,
    Sweep,
}
