using Plumix.Rendering;
using Plumix.UI;
using SkiaSharp;
using Xunit;
using Path = Plumix.UI.Path;

// Engine adapter: skia/src/core/SkPath.cpp. Path keeps SkPath's data in managed code; every builder must
// append exactly what the Skia the engine ships appends. Each case runs the same calls on a Path and on
// a native SKPath (the way Path called Skia before it held its own data) and compares the results bit
// for bit, including the oval/rrect flags Skia's drawing fast paths read.

namespace Plumix.Tests;

public sealed class PathSkiaParityTests
{
    private sealed class Pair
    {
        internal Path Managed { get; } = new();

        internal SKPath Native { get; } = new();
    }

    private static float N(double value) => Path.Narrow(value);

    private static SKRect R(Rect rect) => new(N(rect.Left), N(rect.Top), N(rect.Right), N(rect.Bottom));

    private static float Degrees(double radians) => N(radians) * 180.0f / (float)Math.PI;

    private static void AssertSame(Pair pair, string label, float tolerance = 1e-6f, float magnitude = 1.0f)
    {
        Path managed = pair.Managed;
        SKPath native = pair.Native;
        SKPath rebuilt = SkiaPathInterop.ToNative(managed);
        Assert.True(managed.FillType == (native.FillType == SKPathFillType.EvenOdd
            ? PathFillType.EvenOdd : PathFillType.NonZero), $"{label}: fill type");

        List<string> expected = Describe(native);
        List<string> actual = Describe(managed);
        Assert.True(Matches(expected, actual, tolerance, magnitude),
            $"{label}:\nexpected {string.Join(" ", expected)}\nactual   {string.Join(" ", actual)}");
        Assert.True(Matches(expected, Describe(rebuilt), tolerance, magnitude),
            $"{label}: native rebuild differs\nexpected {string.Join(" ", expected)}\n"
            + $"rebuilt  {string.Join(" ", Describe(rebuilt))}");
        Assert.True(native.IsOval == rebuilt.IsOval, $"{label}: IsOval {native.IsOval} vs {rebuilt.IsOval}");
        Assert.True(native.IsRoundRect == rebuilt.IsRoundRect,
            $"{label}: IsRoundRect {native.IsRoundRect} vs {rebuilt.IsRoundRect}");
        Assert.True(native.IsRect == rebuilt.IsRect, $"{label}: IsRect");
        AssertClose(native.GetOvalBounds(), rebuilt.GetOvalBounds(), tolerance, magnitude, $"{label}: oval bounds");
        AssertClose(native.Bounds, rebuilt.Bounds, tolerance, magnitude, $"{label}: bounds");
    }

    private static void AssertClose(SKRect expected, SKRect actual, float tolerance, float magnitude, string label) =>
        Assert.True(
            Matches(
                [$"R{F(expected.Left)},{F(expected.Top)},{F(expected.Right)},{F(expected.Bottom)}"],
                [$"R{F(actual.Left)},{F(actual.Top)},{F(actual.Right)},{F(actual.Bottom)}"],
                tolerance,
                magnitude),
            $"{label}: {expected} vs {actual}");

    private static string F(float value) =>
        float.IsNaN(value) ? "NaN" : value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    // Verbs must match exactly. Numbers may differ in their last bits: Skia's natives contract
    // multiply-adds into FMA where the compiler chooses (arm64), so even Skia differs across platforms.
    private static bool Matches(
        List<string> expected, List<string> actual, float tolerance = 1e-6f, float magnitude = 1.0f)
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }

        // Points are compared relative to the path's size: a coordinate near zero computed from large
        // ones carries their absolute error. Weights stay relative to one.
        foreach (string entry in expected)
        {
            foreach (string value in entry[1..].Split('w')[0].Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                float number = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                if (float.IsFinite(number))
                {
                    magnitude = Math.Max(magnitude, Math.Abs(number));
                }
            }
        }

        for (int index = 0; index < expected.Count; index++)
        {
            if (expected[index][0] != actual[index][0])
            {
                return false;
            }

            string[] a = expected[index][1..].Split([',', 'w'], StringSplitOptions.RemoveEmptyEntries);
            string[] b = actual[index][1..].Split([',', 'w'], StringSplitOptions.RemoveEmptyEntries);
            if (a.Length != b.Length)
            {
                return false;
            }

            int weightIndex = expected[index].Contains('w') ? a.Length - 1 : -1;
            for (int value = 0; value < a.Length; value++)
            {
                float scale = value == weightIndex ? 1.0f : magnitude;
                float x = float.Parse(a[value], System.Globalization.CultureInfo.InvariantCulture);
                float y = float.Parse(b[value], System.Globalization.CultureInfo.InvariantCulture);
                bool close = x == y || (float.IsNaN(x) && float.IsNaN(y))
                    || Math.Abs(x - y) <= tolerance * Math.Max(scale, Math.Max(Math.Abs(x), Math.Abs(y)));
                if (!close)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static List<string> Describe(SKPath path)
    {
        var result = new List<string>();
        using SKPath.RawIterator iterator = path.CreateRawIterator();
        var points = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    result.Add($"M{F(points[0].X)},{F(points[0].Y)}");
                    break;
                case SKPathVerb.Line:
                    result.Add($"L{F(points[1].X)},{F(points[1].Y)}");
                    break;
                case SKPathVerb.Quad:
                    result.Add($"Q{F(points[1].X)},{F(points[1].Y)},{F(points[2].X)},{F(points[2].Y)}");
                    break;
                case SKPathVerb.Conic:
                    result.Add($"K{F(points[1].X)},{F(points[1].Y)},{F(points[2].X)},{F(points[2].Y)}"
                               + $"w{F(iterator.ConicWeight())}");
                    break;
                case SKPathVerb.Cubic:
                    result.Add($"C{F(points[1].X)},{F(points[1].Y)},{F(points[2].X)},{F(points[2].Y)},"
                               + $"{F(points[3].X)},{F(points[3].Y)}");
                    break;
                case SKPathVerb.Close:
                    result.Add("Z");
                    break;
            }
        }

        return result;
    }

    private static List<string> Describe(Path path)
    {
        var result = new List<string>();
        foreach ((PathVerb verb, PathPoint[] p, float weight) in path.Segments())
        {
            result.Add(verb switch
            {
                PathVerb.Move => $"M{F(p[0].X)},{F(p[0].Y)}",
                PathVerb.Line => $"L{F(p[0].X)},{F(p[0].Y)}",
                PathVerb.Quad => $"Q{F(p[0].X)},{F(p[0].Y)},{F(p[1].X)},{F(p[1].Y)}",
                PathVerb.Conic => $"K{F(p[0].X)},{F(p[0].Y)},{F(p[1].X)},{F(p[1].Y)}w{F(weight)}",
                PathVerb.Cubic => $"C{F(p[0].X)},{F(p[0].Y)},{F(p[1].X)},{F(p[1].Y)},{F(p[2].X)},{F(p[2].Y)}",
                _ => "Z",
            });
        }

        return result;
    }

    private static void MoveTo(Pair p, double x, double y)
    {
        p.Managed.MoveTo(x, y);
        p.Native.MoveTo(N(x), N(y));
    }

    private static void LineTo(Pair p, double x, double y)
    {
        p.Managed.LineTo(x, y);
        p.Native.LineTo(N(x), N(y));
    }

    private static void ArcTo(Pair p, Rect oval, double start, double sweep, bool forceMoveTo)
    {
        p.Managed.ArcTo(oval, start, sweep, forceMoveTo);
        p.Native.ArcTo(R(oval), Degrees(start), Degrees(sweep), forceMoveTo);
    }

    private static void AddArc(Pair p, Rect oval, double start, double sweep)
    {
        p.Managed.AddArc(oval, start, sweep);
        p.Native.AddArc(R(oval), Degrees(start), Degrees(sweep));
    }

    private static void AddRRect(Pair p, RRect rrect)
    {
        p.Managed.AddRRect(rrect);
        RRect normalized = rrect.NormalizeForPaint();
        using var round = new SKRoundRect();
        round.SetRectRadii(R(normalized.Rect),
        [
            new SKPoint(N(normalized.TopLeft.X), N(normalized.TopLeft.Y)),
            new SKPoint(N(normalized.TopRight.X), N(normalized.TopRight.Y)),
            new SKPoint(N(normalized.BottomRight.X), N(normalized.BottomRight.Y)),
            new SKPoint(N(normalized.BottomLeft.X), N(normalized.BottomLeft.Y)),
        ]);
        p.Native.AddRoundRect(round);
    }

    private static void SvgArc(Pair p, Offset end, Radius radius, double rotation, bool large, bool clockwise,
        bool relative = false)
    {
        SKPathArcSize size = large ? SKPathArcSize.Large : SKPathArcSize.Small;
        SKPathDirection direction = clockwise ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise;
        if (relative)
        {
            p.Managed.RelativeArcToPoint(end, radius, rotation, large, clockwise);
            p.Native.RArcTo(N(radius.X), N(radius.Y), N(rotation), size, direction, N(end.Dx), N(end.Dy));
        }
        else
        {
            p.Managed.ArcToPoint(end, radius, rotation, large, clockwise);
            p.Native.ArcTo(N(radius.X), N(radius.Y), N(rotation), size, direction, N(end.Dx), N(end.Dy));
        }
    }

    [Fact]
    public void ElementaryVerbs_MatchSkia()
    {
        var p = new Pair();
        p.Managed.LineTo(3, 4);
        p.Native.LineTo(3, 4);
        p.Managed.RelativeLineTo(1.5, -2);
        p.Native.RLineTo(1.5f, -2);
        p.Managed.QuadraticBezierTo(1, 2, 3, 4);
        p.Native.QuadTo(1, 2, 3, 4);
        p.Managed.RelativeQuadraticBezierTo(1, 2, 3, 4);
        p.Native.RQuadTo(1, 2, 3, 4);
        foreach (double weight in new[] { 0.5, 1.0, 2.0, 0.0, -1.0, double.NaN, double.PositiveInfinity })
        {
            p.Managed.ConicTo(5, 6, 7, 8, weight);
            p.Native.ConicTo(5, 6, 7, 8, N(weight));
            p.Managed.RelativeConicTo(1, 1, 2, 0, weight);
            p.Native.RConicTo(1, 1, 2, 0, N(weight));
        }

        p.Managed.CubicTo(1, 2, 3, 4, 5, 6);
        p.Native.CubicTo(1, 2, 3, 4, 5, 6);
        p.Managed.Close();
        p.Native.Close();
        p.Managed.Close();
        p.Native.Close();
        // After a close, a draw injects a move to the last contour's start.
        p.Managed.RelativeCubicTo(1, 2, 3, 4, 5, 6);
        p.Native.RCubicTo(1, 2, 3, 4, 5, 6);
        p.Managed.Close();
        p.Native.Close();
        p.Managed.RelativeMoveTo(10, 10);
        p.Native.RMoveTo(10, 10);
        p.Managed.RelativeMoveTo(1, 1);
        p.Native.RMoveTo(1, 1);
        LineTo(p, 0.1, 1e40);
        MoveTo(p, 7, 7);
        p.Managed.FillType = PathFillType.EvenOdd;
        p.Native.FillType = SKPathFillType.EvenOdd;
        AssertSame(p, "elementary");

        p.Managed.Reset();
        p.Native.Reset();
        LineTo(p, 1, 1);
        AssertSame(p, "after reset");
    }

    public static TheoryData<double, double, double, double> Rects() => new()
    {
        { 0, 0, 100, 50 },
        { 10.25, 3.5, 47.125, 91.75 },
        { 100, 50, 0, 0 },
        { 5, 5, 5, 20 },
        { -30, -20, 30, 20 },
        { 0.1, 0.2, 0.3, 0.4 },
    };

    [Theory]
    [MemberData(nameof(Rects))]
    public void RectsOvalsAndPolygons_MatchSkia(double l, double t, double r, double b)
    {
        var rect = Rect.FromLTRB(l, t, r, b);
        var p = new Pair();
        p.Managed.AddOval(rect);
        p.Native.AddOval(R(rect));
        AssertSame(p, "oval alone");

        p.Managed.AddRect(rect);
        p.Native.AddRect(R(rect));
        p.Managed.AddOval(rect);
        p.Native.AddOval(R(rect));
        p.Managed.AddPolygon([new Offset(l, t), new Offset(r, b), new Offset(l, b)], true);
        p.Native.AddPoly([new SKPoint(N(l), N(t)), new SKPoint(N(r), N(b)), new SKPoint(N(l), N(b))], true);
        p.Managed.AddPolygon([new Offset(1, 2)], false);
        p.Native.AddPoly([new SKPoint(1, 2)], false);
        // Skia's addPoly ignores an empty polygon; SkiaSharp rejects one, so only the managed side runs.
        p.Managed.AddPolygon([], true);
        AssertSame(p, "mixed");

        var moved = new Pair();
        MoveTo(moved, 3, 4);
        moved.Managed.AddOval(rect);
        moved.Native.AddOval(R(rect));
        AssertSame(moved, "oval after a move");
    }

    [Fact]
    public void Arcs_MatchSkia()
    {
        var random = new Random(1234);
        double[] angles =
        [
            0, Math.PI / 2, Math.PI, -Math.PI / 2, 2 * Math.PI, -2 * Math.PI, 2.5 * Math.PI, 4 * Math.PI,
            Math.PI * 359.99 / 180, -Math.PI * 359.5 / 180, 1e-5, -1e-5, 0.3, 1.1, -2.7, 3.0, 6.0,
        ];
        Rect[] ovals = [Rect.FromLTWH(0, 0, 100, 60), Rect.FromLTWH(10, 20, 33.3, 33.3), Rect.FromLTWH(5, 5, 0, 0)];
        foreach (Rect oval in ovals)
        {
            foreach (double start in angles)
            {
                foreach (double sweep in angles)
                {
                    var add = new Pair();
                    AddArc(add, oval, start, sweep);
                    AssertSame(add, $"addArc {oval} {start} {sweep}");

                    var to = new Pair();
                    MoveTo(to, 1, 2);
                    ArcTo(to, oval, start, sweep, forceMoveTo: false);
                    ArcTo(to, oval, start + sweep, sweep / 3, forceMoveTo: false);
                    ArcTo(to, oval, start, sweep, forceMoveTo: true);
                    AssertSame(to, $"arcTo {oval} {start} {sweep}");
                }
            }
        }

        for (int index = 0; index < 500; index++)
        {
            Rect oval = Rect.FromLTWH(random.NextDouble() * 200 - 100, random.NextDouble() * 200 - 100,
                random.NextDouble() * 300, random.NextDouble() * 300);
            double start = random.NextDouble() * 20 - 10;
            double sweep = random.NextDouble() * 20 - 10;
            var p = new Pair();
            AddArc(p, oval, start, sweep);
            ArcTo(p, oval, start, sweep, forceMoveTo: random.Next(2) == 0);
            AssertSame(p, $"random arc {index}");
        }
    }

    [Fact]
    public void RoundedRects_MatchSkia()
    {
        var random = new Random(42);
        Rect rect = Rect.FromLTWH(10, 20, 100, 60);
        Radius[] radii =
        [
            Radius.Zero, Radius.Circular(5), Radius.Circular(30), Radius.Circular(50), Radius.Elliptical(50, 30),
            Radius.Elliptical(80, 10), Radius.Elliptical(10, 0), Radius.Circular(1000),
        ];
        foreach (Radius topLeft in radii)
        {
            foreach (Radius bottomRight in radii)
            {
                var p = new Pair();
                AddRRect(p, RRect.FromRectAndCorners(rect, new BorderRadius(topLeft, topLeft, bottomRight, topLeft)));
                AssertSame(p, $"rrect {topLeft} {bottomRight}");
            }

            var uniform = new Pair();
            AddRRect(uniform, RRect.FromRectAndRadius(rect, topLeft));
            AssertSame(uniform, $"uniform rrect {topLeft}");
        }

        for (int index = 0; index < 500; index++)
        {
            Rect random1 = Rect.FromLTWH(random.NextDouble() * 50, random.NextDouble() * 50,
                random.NextDouble() * 120, random.NextDouble() * 120);
            Radius Next() => Radius.Elliptical(random.NextDouble() * 80, random.NextDouble() * 80);
            var p = new Pair();
            if (random.Next(3) == 0)
            {
                MoveTo(p, 1, 1);
            }

            AddRRect(p, RRect.FromRectAndCorners(random1, new BorderRadius(Next(), Next(), Next(), Next())));
            AssertSame(p, $"random rrect {index}");
        }
    }

    [Fact]
    public void SvgArcs_MatchSkia()
    {
        var random = new Random(7);
        var fixedCases = new List<(Offset Start, Offset End, Radius Radius, double Rotation)>
        {
            (new Offset(0, 0), new Offset(100, 0), Radius.Circular(50), 0),
            (new Offset(0, 0), new Offset(100, 0), Radius.Circular(10), 0),
            (new Offset(10, 10), new Offset(30, 50), Radius.Elliptical(40, 20), 30),
            (new Offset(10, 10), new Offset(10, 10), Radius.Circular(5), 0),
            (new Offset(10, 10), new Offset(20, 20), Radius.Zero, 0),
            (new Offset(8, 0), new Offset(0, 8), Radius.Circular(8), 0),
            (new Offset(0, 0), new Offset(20, 20), Radius.Circular(20), 0),
            (new Offset(0.5, 0.25), new Offset(-30.75, 12.5), Radius.Elliptical(17, 9), -45),
        };
        foreach ((Offset start, Offset end, Radius radius, double rotation) in fixedCases)
        {
            foreach (bool large in new[] { false, true })
            {
                foreach (bool clockwise in new[] { false, true })
                {
                    var p = new Pair();
                    MoveTo(p, start.Dx, start.Dy);
                    SvgArc(p, end, radius, rotation, large, clockwise);
                    SvgArc(p, new Offset(5, -3), radius, rotation, large, clockwise, relative: true);
                    AssertSame(p, $"svg {start}->{end} {radius} {rotation} {large} {clockwise}");
                }
            }
        }

        for (int index = 0; index < 1000; index++)
        {
            var p = new Pair();
            bool integers = random.Next(2) == 0;
            double Next(double range) => integers
                ? Math.Round(random.NextDouble() * range * 2 - range)
                : random.NextDouble() * range * 2 - range;
            var start = new Offset(Next(100), Next(100));
            var end = new Offset(Next(100), Next(100));
            var radius = Radius.Elliptical(Math.Abs(Next(80)), Math.Abs(Next(80)));
            MoveTo(p, start.Dx, start.Dy);
            SvgArc(p, end, radius, integers ? 0 : Next(180), random.Next(2) == 0, random.Next(2) == 0);
            // Radii too small to reach are scaled until the centre sits on the chord, where
            // sqrt(1 / d - 0.25) magnifies a last-bit difference in d into a visible one.
            double chord = (end - start).Distance / 2;
            bool scaled = radius.X < chord + 1 || radius.Y < chord + 1;
            if (scaled)
            {
                // Within 0.1% of the arc's size.
                AssertSame(p, $"random svg {index}", 1e-3f, (float)Math.Max(1.0, chord * 2));
            }
            else
            {
                AssertSame(p, $"random svg {index}");
            }
        }
    }

    [Fact]
    public void AddPathAndTransforms_MatchSkia()
    {
        Path source = new();
        source.MoveTo(1, 2);
        source.LineTo(10, 3);
        source.ConicTo(4, 5, 6, 7, 0.6);
        source.QuadraticBezierTo(1, 1, 2, 2);
        source.CubicTo(1, 2, 3, 4, 5, 6);
        source.Close();
        source.LineTo(9, 9);
        using SKPath native = SkiaPathInterop.CloneNativePath(source);

        double[] affine = Matrix4.Identity().Storage.ToArray();
        affine[0] = 1.5;
        affine[1] = 0.25;
        affine[4] = -0.5;
        affine[5] = 2;
        affine[12] = 7;
        affine[13] = -3;
        double[] perspective = (double[])affine.Clone();
        perspective[3] = 0.001;
        perspective[7] = 0.002;

        foreach (bool extend in new[] { false, true })
        {
            foreach (double[]? matrix in new[] { null, affine, perspective })
            {
                var p = new Pair();
                MoveTo(p, 100, 100);
                LineTo(p, 1, 2);
                if (extend)
                {
                    p.Managed.ExtendWithPath(source, new Offset(3, 4), matrix);
                }
                else
                {
                    p.Managed.AddPath(source, new Offset(3, 4), matrix);
                }

                SKPathAddMode mode = extend ? SKPathAddMode.Extend : SKPathAddMode.Append;
                if (matrix is null)
                {
                    p.Native.AddPath(native, 3, 4, mode);
                }
                else
                {
                    SKMatrix skMatrix = ToSk(matrix);
                    skMatrix.TransX += 3;
                    skMatrix.TransY += 4;
                    p.Native.AddPath(native, in skMatrix, mode);
                }

                AssertSame(p, $"addPath extend={extend} matrix={(matrix is null ? "none" : "set")}");
            }
        }

        var self = new Pair();
        LineTo(self, 5, 5);
        self.Managed.AddPath(self.Managed, new Offset(1, 1));
        self.Native.AddPath(self.Native, 1, 1);
        AssertSame(self, "self");

        var empty = new Pair();
        Path oval = new();
        oval.AddOval(Rect.FromLTWH(0, 0, 10, 20));
        empty.Managed.AddPath(oval);
        using SKPath nativeOval = new();
        nativeOval.AddOval(new SKRect(0, 0, 10, 20));
        empty.Native.AddPath(nativeOval);
        AssertSame(empty, "oval added to an empty path");

        foreach (double[] matrix in new[] { affine, perspective, Scale(2, -3), Rotation90(), Scale(0, 1) })
        {
            foreach (Path shape in Shapes())
            {
                Path transformed = shape.Transform(matrix);
                using var expected = new SKPath();
                SkiaPathInterop.CloneNativePath(shape).Transform(ToSk(matrix), expected);
                var pair = new Pair();
                pair.Managed.AddPath(transformed);
                pair.Native.AddPath(expected);
                AssertSameTransformed(transformed, expected, "transform");
            }
        }

        foreach (Path shape in Shapes())
        {
            Path shifted = shape.Shift(new Offset(2.5, -7.25));
            using SKPath expected = SkiaPathInterop.CloneNativePath(shape);
            expected.Offset(2.5f, -7.25f);
            AssertSameTransformed(shifted, expected, "shift");
        }
    }

    [Fact]
    public void Contains_MatchesSkia()
    {
        var random = new Random(99);
        int checkedPoints = 0;
        foreach (Path path in ContainmentPaths(random))
        {
            SKPath native = SkiaPathInterop.ToNative(path);
            foreach ((float x, float y) in ProbePoints(path, random))
            {
                bool expected = native.Contains(x, y);
                bool actual = path.Contains(new Offset(x, y));
                Assert.True(expected == actual,
                    $"contains({F(x)}, {F(y)}): Skia {expected}, Path {actual}, path {string.Join(" ", Describe(path))}"
                    + $" fill {path.FillType}");
                checkedPoints++;
            }
        }

        Assert.True(checkedPoints > 10000);
    }

    [Fact]
    public void Metrics_MatchSkia()
    {
        var random = new Random(5);
        int checkedQueries = 0;
        foreach (Path path in ContainmentPaths(random).Concat(MeasuredPaths()))
        {
            foreach (bool forceClosed in new[] { false, true })
            {
                SKPath native = SkiaPathInterop.ToNative(path);
                using var measure = new SKPathMeasure(native, forceClosed, 1.0f);
                var metrics = path.ComputeMetrics(forceClosed).ToList();
                var lengths = new List<float>();
                do
                {
                    if (measure.Length > 0.0f)
                    {
                        lengths.Add(measure.Length);
                    }
                }
                while (measure.NextContour());

                string label = $"{string.Join(" ", Describe(path))} forceClosed={forceClosed}";
                Assert.True(lengths.Count == metrics.Count, $"contours {lengths.Count} vs {metrics.Count}: {label}");
                measure.SetPath(native, forceClosed);
                foreach (PathMetric metric in metrics)
                {
                    while (measure.Length <= 0.0f)
                    {
                        measure.NextContour();
                    }

                    Assert.True(measure.Length == (float)metric.Length,
                        $"length {F(measure.Length)} vs {F((float)metric.Length)}: {label}");
                    Assert.Equal(measure.IsClosed, metric.IsClosed);
                    foreach (float distance in Distances(measure.Length, random))
                    {
                        bool found = measure.GetPositionAndTangent(distance, out SKPoint position, out SKPoint tangent);
                        Tangent? actual = metric.GetTangentForOffset(distance);
                        Assert.True(found == actual is not null, $"tangent at {F(distance)}: {label}");
                        if (actual is not null)
                        {
                            Assert.True(
                                Matches(
                                    [$"T{F(position.X)},{F(position.Y)},{F(tangent.X)},{F(tangent.Y)}"],
                                    [$"T{F((float)actual.Position.Dx)},{F((float)actual.Position.Dy)},"
                                     + $"{F((float)actual.Vector.Dx)},{F((float)actual.Vector.Dy)}"],
                                    1e-5f),
                                $"tangent at {F(distance)}: {position} {tangent} vs {actual.Position} {actual.Vector}: "
                                + label);
                        }

                        checkedQueries++;
                    }

                    foreach ((float start, float stop) in Ranges(measure.Length, random))
                    {
                        foreach (bool moveTo in new[] { true, false })
                        {
                            using var expected = new SKPath();
                            measure.GetSegment(start, stop, expected, moveTo);
                            Path extracted = metric.ExtractPath(start, stop, moveTo);
                            Assert.True(Matches(Describe(expected), Describe(extracted), 1e-5f),
                                $"extract {F(start)}..{F(stop)} moveTo={moveTo}: {label}\n"
                                + $"expected {string.Join(" ", Describe(expected))}\n"
                                + $"actual   {string.Join(" ", Describe(extracted))}");
                        }
                    }

                    measure.NextContour();
                }
            }
        }

        Assert.True(checkedQueries > 10000);
    }

    [Fact]
    public void Combine_MatchesSkia()
    {
        var random = new Random(2024);
        var operands = OpOperands(random).ToList();
        int compared = 0;
        var failures = new List<string>();
        for (int index = 0; index < operands.Count; index++)
        {
            for (int other = 0; other < operands.Count; other += 1 + (index % 3))
            {
                foreach (PathOperation operation in Enum.GetValues<PathOperation>())
                {
                    Path one = operands[index];
                    Path two = operands[other];
                    using var expected = new SKPath();
                    bool nativeOk = SkiaPathInterop.ToNative(one).Op(
                        SkiaPathInterop.ToNative(two), (SKPathOp)operation, expected);
                    var actual = new Path();
                    bool managedOk = Plumix.UI.PathOps.SkPathOps.Op(
                        one, two, (Plumix.UI.PathOps.SkPathOp)operation, actual);
                    compared++;
                    string label = $"{operation} #{index} x #{other}";
                    if (nativeOk != managedOk)
                    {
                        failures.Add($"{label}: Skia {nativeOk}, Path {managedOk}");
                        continue;
                    }

                    if (!nativeOk)
                    {
                        continue;
                    }

                    PathFillType expectedFill = expected.FillType == SKPathFillType.EvenOdd
                        ? PathFillType.EvenOdd : PathFillType.NonZero;
                    if (expectedFill != actual.FillType || !Matches(Describe(expected), Describe(actual), 1e-5f))
                    {
                        failures.Add($"{label}:\n  one {string.Join(" ", Describe(one))}\n"
                                     + $"  two {string.Join(" ", Describe(two))}\n"
                                     + $"  expected {expected.FillType} {string.Join(" ", Describe(expected))}\n"
                                     + $"  actual   {actual.FillType} {string.Join(" ", Describe(actual))}");
                    }
                }
            }
        }

        Assert.True(compared > 2000, $"only {compared} ops compared");
        Assert.True(failures.Count == 0,
            $"{failures.Count} of {compared} ops differ:\n" + string.Join("\n", failures.Take(6)));
    }

    private static IEnumerable<Path> OpOperands(Random random)
    {
        var rect = new Path();
        rect.AddRect(Rect.FromLTWH(0, 0, 40, 30));
        yield return rect;
        var rect2 = new Path();
        rect2.AddRect(Rect.FromLTWH(20, 10, 40, 30));
        yield return rect2;
        var oval = new Path();
        oval.AddOval(Rect.FromLTWH(10, 5, 35, 35));
        yield return oval;
        var rrect = new Path();
        rrect.AddRRect(RRect.FromRectXY(Rect.FromLTWH(5, 15, 50, 20), 8, 6));
        yield return rrect;
        var stadium = new Path();
        stadium.AddRRect(RRect.FromRectXY(Rect.FromLTWH(0, 20, 60, 20), 10, 10));
        yield return stadium;
        var notch = new Path();
        notch.AddOval(Rect.FromLTWH(22, 8, 16, 16));
        yield return notch;
        var empty = new Path();
        yield return empty;
        var star = new Path { FillType = PathFillType.EvenOdd };
        star.AddPolygon(
            [new Offset(30, 0), new Offset(40, 40), new Offset(5, 15), new Offset(55, 15), new Offset(20, 40)], true);
        yield return star;
        var ring = new Path { FillType = PathFillType.EvenOdd };
        ring.AddOval(Rect.FromLTWH(0, 0, 50, 50));
        ring.AddOval(Rect.FromLTWH(15, 15, 20, 20));
        yield return ring;
        var coincident = new Path();
        coincident.AddRect(Rect.FromLTWH(40, 0, 20, 30));
        yield return coincident;
        for (int index = 0; index < 22; index++)
        {
            var path = new Path { FillType = random.Next(2) == 0 ? PathFillType.NonZero : PathFillType.EvenOdd };
            double Coordinate() => random.Next(3) == 0 ? random.Next(0, 13) * 5.0 : random.NextDouble() * 60;
            path.MoveTo(Coordinate(), Coordinate());
            int segments = random.Next(2, 6);
            for (int segment = 0; segment < segments; segment++)
            {
                switch (random.Next(4))
                {
                    case 0:
                        path.LineTo(Coordinate(), Coordinate());
                        break;
                    case 1:
                        path.QuadraticBezierTo(Coordinate(), Coordinate(), Coordinate(), Coordinate());
                        break;
                    case 2:
                        path.ConicTo(Coordinate(), Coordinate(), Coordinate(), Coordinate(),
                            0.3 + random.NextDouble() * 2);
                        break;
                    default:
                        path.CubicTo(Coordinate(), Coordinate(), Coordinate(), Coordinate(), Coordinate(),
                            Coordinate());
                        break;
                }
            }

            path.Close();
            yield return path;
        }
    }

    private static IEnumerable<Path> MeasuredPaths()
    {
        var arcs = new Path();
        arcs.AddArc(Rect.FromLTWH(0, 0, 100, 60), 0.3, 4.0);
        arcs.ArcToPoint(new Offset(10, 90), radius: Radius.Circular(40));
        yield return arcs;
        var long_ = new Path();
        long_.MoveTo(0, 0);
        long_.CubicTo(1000, -500, -800, 900, 300, 300);
        long_.LineTo(300, 300);
        long_.QuadraticBezierTo(5000, 0, 0, 1);
        yield return long_;
        var rrect = new Path();
        rrect.AddRRect(RRect.FromRectAndCorners(Rect.FromLTWH(3, 4, 50, 40),
            new BorderRadius(Radius.Circular(9), Radius.Elliptical(20, 5), Radius.Zero, Radius.Circular(15))));
        yield return rrect;
    }

    private static IEnumerable<float> Distances(float length, Random random)
    {
        yield return -1.0f;
        yield return 0.0f;
        yield return length;
        yield return length + 1.0f;
        yield return float.NaN;
        for (int index = 0; index < 20; index++)
        {
            yield return (float)(random.NextDouble() * length);
        }
    }

    private static IEnumerable<(float Start, float Stop)> Ranges(float length, Random random)
    {
        yield return (0.0f, length);
        yield return (-5.0f, length + 5.0f);
        yield return (length / 3, length / 3);
        yield return (length / 2, length / 4);
        for (int index = 0; index < 5; index++)
        {
            float a = (float)(random.NextDouble() * length);
            float b = (float)(random.NextDouble() * length);
            yield return (Math.Min(a, b), Math.Max(a, b));
        }
    }

    private static IEnumerable<Path> ContainmentPaths(Random random)
    {
        foreach (Path shape in Shapes())
        {
            yield return shape;
        }

        var empty = new Path();
        yield return empty;
        var trailingMove = new Path();
        trailingMove.AddRect(Rect.FromLTWH(0, 0, 10, 10));
        trailingMove.MoveTo(50, 50);
        yield return trailingMove;

        // Two overlapping squares wound the same way and the opposite way, under both fill rules.
        foreach (PathFillType fill in new[] { PathFillType.NonZero, PathFillType.EvenOdd })
        {
            var same = new Path { FillType = fill };
            same.AddRect(Rect.FromLTWH(0, 0, 20, 20));
            same.AddRect(Rect.FromLTWH(10, 10, 20, 20));
            yield return same;
            var opposite = new Path { FillType = fill };
            opposite.AddRect(Rect.FromLTWH(0, 0, 20, 20));
            opposite.AddPolygon([new Offset(10, 10), new Offset(10, 30), new Offset(30, 30), new Offset(30, 10)], true);
            yield return opposite;
            // Coincident edges: the shared edge is hit twice.
            var adjacent = new Path { FillType = fill };
            adjacent.AddRect(Rect.FromLTWH(0, 0, 10, 10));
            adjacent.AddRect(Rect.FromLTWH(10, 0, 10, 10));
            yield return adjacent;
            var ring = new Path { FillType = fill };
            ring.AddOval(Rect.FromLTWH(0, 0, 40, 40));
            ring.AddOval(Rect.FromLTWH(10, 10, 20, 20));
            yield return ring;
        }

        for (int index = 0; index < 300; index++)
        {
            var path = new Path { FillType = random.Next(2) == 0 ? PathFillType.NonZero : PathFillType.EvenOdd };
            double Coordinate() => random.Next(4) == 0 ? random.Next(0, 9) * 5.0 : random.NextDouble() * 40;
            path.MoveTo(Coordinate(), Coordinate());
            int segments = random.Next(1, 7);
            for (int segment = 0; segment < segments; segment++)
            {
                switch (random.Next(5))
                {
                    case 0:
                        path.LineTo(Coordinate(), Coordinate());
                        break;
                    case 1:
                        path.QuadraticBezierTo(Coordinate(), Coordinate(), Coordinate(), Coordinate());
                        break;
                    case 2:
                        path.ConicTo(Coordinate(), Coordinate(), Coordinate(), Coordinate(),
                            0.2 + random.NextDouble() * 3);
                        break;
                    case 3:
                        path.CubicTo(Coordinate(), Coordinate(), Coordinate(), Coordinate(), Coordinate(),
                            Coordinate());
                        break;
                    default:
                        path.Close();
                        path.MoveTo(Coordinate(), Coordinate());
                        break;
                }
            }

            if (random.Next(2) == 0)
            {
                path.Close();
            }

            yield return path;
        }
    }

    // Grid points (often exactly on axis-aligned edges), the path's own points, and random points.
    private static IEnumerable<(float X, float Y)> ProbePoints(Path path, Random random)
    {
        for (int x = -1; x <= 9; x++)
        {
            for (int y = -1; y <= 9; y++)
            {
                yield return (x * 5.0f, y * 5.0f);
            }
        }

        foreach (PathPoint point in path.Points)
        {
            yield return (point.X, point.Y);
            yield return (point.X + 0.5f, point.Y);
        }

        for (int index = 0; index < 30; index++)
        {
            yield return ((float)(random.NextDouble() * 50 - 5), (float)(random.NextDouble() * 50 - 5));
        }
    }

    private static void AssertSameTransformed(Path managed, SKPath expected, string label)
    {
        SKPath rebuilt = SkiaPathInterop.ToNative(managed);
        Assert.True(Matches(Describe(expected), Describe(managed)),
            $"{label}:\nexpected {string.Join(" ", Describe(expected))}\n"
            + $"actual   {string.Join(" ", Describe(managed))}");
        Assert.True(Matches(Describe(expected), Describe(rebuilt)), $"{label}: rebuild");
        Assert.Equal(expected.IsOval, rebuilt.IsOval);
        Assert.Equal(expected.IsRoundRect, rebuilt.IsRoundRect);
    }

    private static IEnumerable<Path> Shapes()
    {
        var oval = new Path();
        oval.AddOval(Rect.FromLTWH(1, 2, 30, 40));
        yield return oval;
        var rrect = new Path();
        rrect.AddRRect(RRect.FromRectXY(Rect.FromLTWH(1, 2, 30, 40), 5, 7));
        yield return rrect;
        var mixed = new Path();
        mixed.MoveTo(1, 1);
        mixed.ConicTo(4, 5, 6, 7, 0.6);
        mixed.CubicTo(1, 2, 3, 4, 5, 6);
        mixed.Close();
        yield return mixed;
    }

    private static double[] Scale(double sx, double sy)
    {
        double[] matrix = Matrix4.Identity().Storage.ToArray();
        matrix[0] = sx;
        matrix[5] = sy;
        return matrix;
    }

    private static double[] Rotation90()
    {
        double[] matrix = Matrix4.Identity().Storage.ToArray();
        matrix[0] = 0;
        matrix[1] = 1;
        matrix[4] = -1;
        matrix[5] = 0;
        return matrix;
    }

    private static SKMatrix ToSk(double[] m) => new(
        N(m[0]), N(m[4]), N(m[12]),
        N(m[1]), N(m[5]), N(m[13]),
        N(m[3]), N(m[7]), N(m[15]));
}
