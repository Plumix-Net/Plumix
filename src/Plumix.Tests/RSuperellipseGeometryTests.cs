using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/engine/src/flutter/testing/dart/geometry_test.dart (RSuperellipse.contains)
public sealed class RSuperellipseGeometryTests
{
    public static IEnumerable<object[]> BoundaryProbes()
    {
        (double X, double Y)[][] points =
        [
            [(0, 49.995), (44.245, 49.95), (45.72, 49.87), (48.53, 48.53),
                (49.87, 45.72), (49.95, 44.245), (49.995, 0)],
            [(0, 49.995), (44.245, 49.911), (45.72, 49.75), (48.51, 47.07),
                (49.87, 41.44), (49.95, 38.49), (49.995, 0)],
            [(24.99, 99.99), (29.99, 99.64), (34.99, 98.06), (39.99, 94.73),
                (44.13, 89.99), (48.46, 79.99), (49.70, 69.99), (49.97, 59.99), (49.99, 49.99)],
        ];
        for (int kind = 0; kind < points.Length; kind++)
        {
            double cx = kind == 2 ? 25 : 0;
            double cy = kind == 2 ? 50 : 0;
            foreach ((double x, double y) in points[kind])
            {
                foreach (int sx in new[] { -1, 1 })
                {
                    foreach (int sy in new[] { -1, 1 })
                    {
                        yield return [kind, cx + (x - cx) * sx, cy + (y - cy) * sy, 0.02 * sx, 0.02 * sy];
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(BoundaryProbes))]
    public void Contains_UniformCircularEllipticalAndUnequalBounds(
        int kind, double x, double y, double dx, double dy)
    {
        RSuperellipse shape = kind switch
        {
            0 => RSuperellipse.FromLTRBXY(-50, -50, 50, 50, 5, 5),
            1 => RSuperellipse.FromLTRBXY(-50, -50, 50, 50, 5, 10),
            _ => RSuperellipse.FromLTRBXY(0, 0, 50, 100, 23, 30),
        };
        CheckPoint(shape, new Point(x, y), new Point(dx, dy));
    }

    [Fact]
    public void Contains_NoCornersHasHalfOpenBoundsAndTinyCornersExcludeVertices()
    {
        RSuperellipse square = RSuperellipse.FromLTRBXY(-50, -50, 50, 50, 0, 0);
        foreach (Point p in new Point[] { new(-50, -50), new(-50, 49.99), new(49.99, -50), new(49.99, 49.99) })
        {
            Assert.True(square.Contains(p));
        }
        foreach (Point p in new Point[] { new(-50.01, -50), new(-50, -50.01), new(-50.01, 50), new(-50, 50.01),
                     new(50.01, -50), new(50, -50.01), new(50.01, 50), new(50, 50.01), new(50, 0), new(0, 50) })
        {
            Assert.False(square.Contains(p));
        }
        var tiny = RSuperellipse.FromLTRBXY(-50, -50, 50, 50, 0.01, 0.01);
        foreach (Point p in new Point[] { new(-50, -50), new(-50, 50), new(50, -50), new(50, 50) })
        {
            Assert.False(tiny.Contains(p));
        }
    }

    [Fact]
    public void Contains_DiagonalAlmondUsesIndependentCornerSplits()
    {
        RSuperellipse shape = RSuperellipse.FromLTRBAndCorners(-50, -50, 50, 50, 1, 99, 1, 99);
        Assert.True(shape.Contains(default));
        foreach (Point p in new Point[] { new(-49.999, -49.999), new(-49.999, 49.999),
                     new(49.999, -49.999), new(49.999, 49.999) })
        {
            Assert.False(shape.Contains(p));
        }
        CheckPoint(shape, new Point(-49.70, -49.70), new Point(-0.02, -0.02));
        CheckPoint(shape, new Point(49.70, 49.70), new Point(0.02, 0.02));
        foreach (Point p in new Point[] { new(-40, -49.59), new(-20, -45.64), new(0, -37.01),
                     new(20, -21.96), new(21.05, -20.92), new(40, 5.68) })
        {
            CheckPoint(shape, p, new Point(0.02, -0.02));
            CheckPoint(shape, -p, new Point(-0.02, 0.02));
        }
    }

    [Fact]
    public void Contains_RejectsOutsideSharpCornerAndNonfinitePoint()
    {
        var shape = RSuperellipse.FromLTRBAndCorners(196, 0, 294, 28, topRight: 3, bottomRight: 3);
        Assert.False(shape.Contains(new Point(147, 14)));
        Assert.False(shape.Contains(new Point(double.NaN, 14)));
        Assert.False(shape.Contains(new Point(double.PositiveInfinity, 14)));
        Assert.False(RSuperellipse.Zero.Contains(default));
    }

    [Fact]
    public void Contains_NormalizesBoundsAndNarrowsPointLikeNativeEngine()
    {
        var shape = RSuperellipse.FromLTRBXY(50, 50, -50, -50, 0, 0);
        Assert.True(shape.IsEmpty);
        Assert.True(shape.Contains(default));
        Assert.False(shape.Contains(new Point(50, 0)));
        Assert.False(shape.Contains(new Point(50 - 1e-8, 0)));
        Assert.True(shape.Contains(new Point(-50 - 1e-8, 0)));
        var flat = RSuperellipse.FromLTRBXY(-50, -50, 50, 50, 0, 5);
        Assert.True(flat.Contains(new Point(-50, -50)));
        Assert.False(flat.Contains(new Point(50, 0)));
    }

    private static void CheckPoint(RSuperellipse shape, Point inside, Point outward)
    {
        Assert.True(shape.Contains(inside), $"{shape} must contain {inside}");
        Assert.False(shape.Contains(inside + outward), $"{shape} must exclude {inside + outward}");
    }
}
