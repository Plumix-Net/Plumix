using System.Numerics;
using NImpeller;
using Plumix.Rendering;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Impeller;

// C#-only infrastructure: no Dart parity source. Framework value types to the Impeller C API's, for the
// stage 4 prototype (docs/product/avalonia-free-core.md).

/// <summary>Converts framework geometry, colours and paths to Impeller values.</summary>
internal static class ImpellerConversions
{
    internal static ImpellerPoint ToImpeller(this Offset offset) =>
        new() { X = (float)offset.Dx, Y = (float)offset.Dy };

    internal static ImpellerRect ToImpeller(this Rect rect) =>
        new() { X = (float)rect.Left, Y = (float)rect.Top, Width = (float)rect.Width, Height = (float)rect.Height };

    internal static ImpellerColor ToImpeller(this Color color)
    {
        uint argb = color.ToARGB32();
        return new ImpellerColor
        {
            Alpha = ((argb >> 24) & 0xFF) / 255f,
            Red = ((argb >> 16) & 0xFF) / 255f,
            Green = ((argb >> 8) & 0xFF) / 255f,
            Blue = (argb & 0xFF) / 255f,
        };
    }

    internal static ImpellerColor ColorFromArgb(uint argb) => new()
    {
        Alpha = ((argb >> 24) & 0xFF) / 255f,
        Red = ((argb >> 16) & 0xFF) / 255f,
        Green = ((argb >> 8) & 0xFF) / 255f,
        Blue = (argb & 0xFF) / 255f,
    };

    internal static ImpellerRoundingRadii Radii(this RRect rrect) => new()
    {
        Top_left = new ImpellerPoint { X = (float)rrect.TopLeft.X, Y = (float)rrect.TopLeft.Y },
        Top_right = new ImpellerPoint { X = (float)rrect.TopRight.X, Y = (float)rrect.TopRight.Y },
        Bottom_right = new ImpellerPoint { X = (float)rrect.BottomRight.X, Y = (float)rrect.BottomRight.Y },
        Bottom_left = new ImpellerPoint { X = (float)rrect.BottomLeft.X, Y = (float)rrect.BottomLeft.Y },
    };

    // Both Matrix4's storage and Matrix4x4's fields are column-major in memory (translation at 12..14).
    internal static ImpellerMatrix ToImpeller(this Matrix4 matrix)
    {
        double[] m = matrix.Storage;
        return new Matrix4x4(
            (float)m[0], (float)m[1], (float)m[2], (float)m[3],
            (float)m[4], (float)m[5], (float)m[6], (float)m[7],
            (float)m[8], (float)m[9], (float)m[10], (float)m[11],
            (float)m[12], (float)m[13], (float)m[14], (float)m[15]);
    }

    // Flutter's BlendMode order is Skia's, which Impeller's C API keeps.
    internal static ImpellerBlendMode ToImpeller(this BlendMode mode) => (ImpellerBlendMode)(int)mode;

    internal static ImpellerTileMode ToImpeller(this TileMode mode) => mode switch
    {
        TileMode.Repeated => ImpellerTileMode.kImpellerTileModeRepeat,
        TileMode.Mirror => ImpellerTileMode.kImpellerTileModeMirror,
        TileMode.Decal => ImpellerTileMode.kImpellerTileModeDecal,
        _ => ImpellerTileMode.kImpellerTileModeClamp,
    };

    internal static ImpellerTextureSampling ToSampling(this FilterQuality quality) => quality == FilterQuality.None
        ? ImpellerTextureSampling.kImpellerTextureSamplingNearestNeighbor
        : ImpellerTextureSampling.kImpellerTextureSamplingLinear;

    /// <summary>
    /// The path as an Impeller path, built once per version of the path and owned by it. Impeller has no
    /// conic verb, so conics become quads (SkAutoConicToQuads at a quarter-pixel tolerance).
    /// </summary>
    internal static ImpellerPath ToImpeller(this Path path)
    {
        if (path.NativeCache is { } cache && cache.Version == path.Version && cache.Native is ImpellerPath cached)
        {
            return cached;
        }

        using ImpellerPathBuilder builder = ImpellerPathBuilder.New()
            ?? throw new InvalidOperationException("Impeller could not create a path builder.");
        IReadOnlyList<PathVerb> verbs = path.Verbs;
        IReadOnlyList<PathPoint> points = path.Points;
        IReadOnlyList<float> weights = path.ConicWeights;
        int point = 0;
        int weight = 0;
        PathPoint last = default;
        foreach (PathVerb verb in verbs)
        {
            switch (verb)
            {
                case PathVerb.Move:
                    last = points[point++];
                    builder.MoveTo(Point(last));
                    break;
                case PathVerb.Line:
                    last = points[point++];
                    builder.LineTo(Point(last));
                    break;
                case PathVerb.Quad:
                    builder.QuadraticCurveTo(Point(points[point]), Point(points[point + 1]));
                    last = points[point + 1];
                    point += 2;
                    break;
                case PathVerb.Conic:
                    PathPoint[] conic = [last, points[point], points[point + 1]];
                    int count = PathGeometry.ConicToQuads(conic, weights[weight++], 0.25f, out PathPoint[] quads);
                    for (int quad = 0; quad < count; quad++)
                    {
                        builder.QuadraticCurveTo(Point(quads[(2 * quad) + 1]), Point(quads[(2 * quad) + 2]));
                    }

                    last = points[point + 1];
                    point += 2;
                    break;
                case PathVerb.Cubic:
                    builder.CubicCurveTo(Point(points[point]), Point(points[point + 1]), Point(points[point + 2]));
                    last = points[point + 2];
                    point += 3;
                    break;
                case PathVerb.Close:
                    builder.Close();
                    break;
            }
        }

        ImpellerFillType fill = path.FillType == PathFillType.EvenOdd
            ? ImpellerFillType.kImpellerFillTypeOdd
            : ImpellerFillType.kImpellerFillTypeNonZero;
        ImpellerPath native = builder.TakePathNew(fill)
            ?? throw new InvalidOperationException("Impeller could not build a path.");
        path.NativeCache = (path.Version, native);
        return native;
    }

    private static ImpellerPoint Point(PathPoint point) => new() { X = point.X, Y = point.Y };
}
