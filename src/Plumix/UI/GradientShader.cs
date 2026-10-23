using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Plumix.Rendering;
using Plumix.Widgets;
using SkiaSharp;
using TileMode = Plumix.Rendering.TileMode;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Gradient)
// Engine adapter: engine/src/flutter/lib/ui/painting/gradient.cc and
// engine/src/flutter/display_list/skia/dl_sk_conversions.cc.

/// <summary>
/// A shader description consumed by the framework canvas and shader-mask layers. It implements the
/// published IBrush shader contract; native resources belong to a draw, never to this description.
/// </summary>
internal sealed class GradientShader : ImmutableGradientBrush, INativeShader
{
    // Avalonia 12 prevents implementing IBrush directly. Its immutable base supplies that transport
    // contract; ShaderDrawing consumes this descriptor before a draw reaches Avalonia's brush switch.
    private readonly SKColor[] _colors;
    private readonly float[] _stops;
    private readonly SKMatrix _matrix;

    private GradientShader(
        GradientKind kind,
        Point start,
        Point end,
        double radius,
        double focalRadius,
        IReadOnlyList<Color> colors,
        IReadOnlyList<double> stops,
        TileMode tileMode,
        Matrix4? matrix,
        double startAngle = 0.0,
        double endAngle = 0.0)
        : base([.. colors.Select((color, index) => new ImmutableGradientStop(
            index < stops.Count ? stops[index] : 0.0, color))],
            1.0, null, null, GradientSpreadMethod.Pad)
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
        _colors = [.. colors.Select(color => new SKColor(color.WithValues(
            colorSpace: ColorSpace.ExtendedSRGB).ToARGB32()))];
        _stops = [.. stops.Select(value => (float)value)];
        if (matrix is null)
        {
            _matrix = SKMatrix.Identity;
        }
        else
        {
            double[] values = matrix.Storage;
            DebugAssertions.Assert(values.Length == 16 && values.All(double.IsFinite),
                "'matrix4' must have 16 finite entries.");
            _matrix = new SKMatrix(
                Path.Narrow(values[0]), Path.Narrow(values[4]), Path.Narrow(values[12]),
                Path.Narrow(values[1]), Path.Narrow(values[5]), Path.Narrow(values[13]),
                Path.Narrow(values[3]), Path.Narrow(values[7]), Path.Narrow(values[15]));
        }
    }

    internal GradientKind Kind { get; }
    internal Point Start { get; }
    internal Point End { get; }
    internal double Radius { get; }
    internal double FocalRadius { get; }
    internal TileMode TileMode { get; }
    internal double StartAngle { get; }
    internal double EndAngle { get; }
    internal SKMatrix LocalMatrix => _matrix;

    internal static GradientShader Linear(
        Point from, Point to, IReadOnlyList<Color> colors, IReadOnlyList<double> stops,
        TileMode tileMode, Matrix4? matrix)
    {
        CheckPoint(from);
        CheckPoint(to);
        return new GradientShader(GradientKind.Linear, from, to, 0.0, 0.0, colors, stops, tileMode, matrix);
    }

    internal static GradientShader Radial(
        Point center, double radius, IReadOnlyList<Color> colors, IReadOnlyList<double> stops,
        TileMode tileMode, Matrix4? matrix, Point? focal, double focalRadius)
    {
        CheckPoint(center);
        bool radial = focal is null || (focal == center && focalRadius == 0.0);
        DebugAssertions.Assert(radial || center != default || focal != default(Point),
            "center != Offset.zero || focal != Offset.zero");
        return new GradientShader(radial ? GradientKind.Radial : GradientKind.Conical,
            focal ?? center, center, radius, focalRadius, colors, stops, tileMode, matrix);
    }

    internal static GradientShader Sweep(
        Point center, IReadOnlyList<Color> colors, IReadOnlyList<double> stops,
        TileMode tileMode, double startAngle, double endAngle, Matrix4? matrix)
    {
        CheckPoint(center);
        DebugAssertions.Assert(startAngle < endAngle, "startAngle < endAngle");
        return new GradientShader(GradientKind.Sweep, center, center, 0.0, 0.0,
            colors, stops, tileMode, matrix, startAngle, endAngle);
    }

    public IBrush Snapshot() => this;

    public SKShader? CreateNativeShader()
    {
        SKShaderTileMode tileMode = TileMode switch
        {
            TileMode.Repeated => SKShaderTileMode.Repeat,
            TileMode.Mirror => SKShaderTileMode.Mirror,
            TileMode.Decal => SKShaderTileMode.Decal,
            _ => SKShaderTileMode.Clamp,
        };
        SKPoint start = new(Path.Narrow(Start.X), Path.Narrow(Start.Y));
        SKPoint end = new(Path.Narrow(End.X), Path.Narrow(End.Y));
        // Skia owns stop sanitization and degenerate geometry, just as in Flutter's Skia backend.
        return Kind switch
        {
            GradientKind.Linear => SKShader.CreateLinearGradient(
                start, end, _colors, _stops, tileMode, _matrix),
            GradientKind.Radial => SKShader.CreateRadialGradient(
                end, Path.Narrow(Radius), _colors, _stops, tileMode, _matrix),
            GradientKind.Conical => SKShader.CreateTwoPointConicalGradient(
                start, Path.Narrow(FocalRadius), end, Path.Narrow(Radius), _colors, _stops, tileMode, _matrix),
            _ => SKShader.CreateSweepGradient(end, _colors, _stops, tileMode,
                Degrees(StartAngle), Degrees(EndAngle), _matrix),
        };
    }

    private static float Degrees(double radians) => Path.Narrow(radians) * 180.0f / (float)Math.PI;

    private static void CheckPoint(Point point) => DebugAssertions.Assert(
        !double.IsNaN(point.X) && !double.IsNaN(point.Y), "Offset argument contained a NaN value.");
}

internal enum GradientKind
{
    Linear,
    Radial,
    Conical,
    Sweep,
}
