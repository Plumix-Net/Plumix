using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/engine/src/flutter/testing/dart/geometry_test.dart
public sealed class UiGeometryDartParityTests
{
    [Fact]
    public void OffsetBaseComparisons_AreComponentwiseAndRejectNaN()
    {
        Point zero = default;
        foreach (Point p in new Point[] { new(0, -1), new(-1, 0), new(-1, -1), default })
        {
            Assert.True(DartGeometry.GreaterThanOrEqual(zero, p));
            Assert.True(DartGeometry.LessThanOrEqual(p, zero));
        }
        foreach (Point p in new Point[] { new(0, double.NaN), new(double.NaN, 0), new(10, -10) })
        {
            Assert.False(DartGeometry.GreaterThanOrEqual(zero, p));
            Assert.False(DartGeometry.LessThanOrEqual(zero, p));
        }
        Assert.True(DartGeometry.GreaterThan(zero, new Point(-1, -1)));
        Assert.True(DartGeometry.LessThan(zero, new Point(1, 1)));
        foreach (Point p in new Point[] { new(0, 1), new(1, 0), new(double.NaN, 1) })
        {
            Assert.False(DartGeometry.LessThan(zero, p));
            Assert.False(DartGeometry.GreaterThan(zero, -p));
        }
        Assert.True(DartGeometry.LessThan(new Size(1, 2), new Point(2, 3)));
        Assert.True(DartGeometry.GreaterThanOrEqual(new Point(1, 2), new Size(1, 2)));
        Assert.Equal(new Point(0, 0), zero);
        Assert.NotEqual(new Point(1, 0), zero);
        Assert.NotEqual(new Point(0, 1), zero);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 0.5)]
    [InlineData(0, -1, -0.5)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 1, 0.25)]
    [InlineData(1, -1, -0.25)]
    [InlineData(-1, 0, 1)]
    [InlineData(-1, 1, 0.75)]
    [InlineData(-1, -1, -0.75)]
    public void OffsetDirection_MatchesAllQuadrants(double x, double y, double piFactor) =>
        Assert.Equal(Math.PI * piFactor, new Point(x, y).Direction, 12);

    [Fact]
    public void OffsetFromDirection_UnitAndCustomDistancesMatchFlutter()
    {
        Assert.Equal(default, DartGeometry.OffsetFromDirection(0, 0));
        Assert.Equal(new Point(1, 0), DartGeometry.OffsetFromDirection(0));
        foreach (double angle in new[] { Math.PI / 2, -Math.PI / 2, Math.PI / 4, -Math.PI / 4,
                     Math.PI, 3 * Math.PI / 4, -3 * Math.PI / 4, Math.PI / 6 })
        {
            Point unit = DartGeometry.OffsetFromDirection(angle);
            Assert.Equal(Math.Cos(angle), unit.X, 12);
            Assert.Equal(Math.Sin(angle), unit.Y, 12);
            Point doubled = DartGeometry.OffsetFromDirection(angle, 2);
            Assert.Equal(2 * unit.X, doubled.X, 12);
            Assert.Equal(2 * unit.Y, doubled.Y, 12);
        }
        Assert.Equal(Math.Sqrt(3), DartGeometry.OffsetFromDirection(Math.PI / 6, 2).X, 12);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(-0.0, 0, 0)]
    [InlineData(0, -0.0, 0)]
    [InlineData(-0.0, -0.0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, -1, -0.0)]
    [InlineData(1, 0, double.PositiveInfinity)]
    [InlineData(1, 1, 1)]
    [InlineData(1, -1, -1)]
    [InlineData(-1, 0, double.NegativeInfinity)]
    [InlineData(-1, 1, -1)]
    [InlineData(-1, -1, 1)]
    [InlineData(3, 4, 0.75)]
    public void SizeAspectRatio_HandlesZeroAndSigns(double width, double height, double expected) =>
        Assert.Equal(expected, DartGeometry.SizeAspectRatio(new Size(width, height)));

    [Fact]
    public void SizeFactoriesAnchorsAndHalfOpenContainment()
    {
        Size size = new(5, 7);
        Assert.Equal(5, size.ShortestSide);
        Assert.Equal(7, size.LongestSide);
        Assert.Equal(new Size(7, 5), size.Flipped);
        Assert.Equal(new Size(6, 6), DartGeometry.SizeFromRadius(3));
        Assert.Equal(new Size(3, 3), DartGeometry.SizeSquare(3));
        Assert.Equal(new Size(3, double.PositiveInfinity), DartGeometry.SizeFromWidth(3));
        Assert.Equal(new Size(double.PositiveInfinity, 4), DartGeometry.SizeFromHeight(4));
        Assert.Equal(size, DartGeometry.SizeCopy(size));
        Assert.True(size.Contains(new Point(0, 0)));
        Assert.False(size.Contains(new Point(5, 0)));
        Assert.False(size.Contains(new Point(0, 7)));
        Point origin = new(10, 20);
        Point[] anchors = [size.TopLeft(origin), size.TopCenter(origin), size.TopRight(origin),
            size.CenterLeft(origin), size.Center(origin), size.CenterRight(origin),
            size.BottomLeft(origin), size.BottomCenter(origin), size.BottomRight(origin)];
        Assert.Equal(new Point[] { new(10, 20), new(12.5, 20), new(15, 20), new(10, 23.5), new(12.5, 23.5),
            new(15, 23.5), new(10, 27), new(12.5, 27), new(15, 27) }, anchors);
        Assert.Equal(new Size(6, 9), size.Add(new Point(1, 2)));
        Assert.Equal(new Size(4, 5), size.Subtract(new Point(1, 2)));
        Assert.Equal(new Point(4, 5), size.Difference(new Size(1, 2)));
    }

    [Fact]
    public void GeometryArithmetic_NegativeModuloTruncationAndNonfiniteFlags()
    {
        Point offset = new(-5, 7);
        Assert.Equal(74, offset.DistanceSquared);
        Assert.Equal(Math.Sqrt(74), offset.DartDistance);
        Assert.Equal(new Point(-10, 21), offset.Scale(2, 3));
        Assert.Equal(new Point(-4, 9), offset.Translate(1, 2));
        Assert.Equal(new Rect(-5, 7, 3, 4), offset.WithSize(new Size(3, 4)));
        Assert.Equal(new Point(-2, 3), offset.TruncateDivide(2));
        Assert.Equal(new Point(1, 1), offset.Modulo(-2));
        Assert.Equal(new Size(-2, 3), new Size(-5, 7).TruncateDivide(2));
        Assert.Equal(new Size(1, 1), new Size(-5, 7).Modulo(2));
        Assert.False(new Point(double.NegativeInfinity, 0).IsInfinite);
        Assert.False(new Point(double.NaN, 0).IsFinite);
        Assert.True(DartGeometry.OffsetInfinite.IsInfinite);
        Assert.True(DartGeometry.SizeInfinite.IsInfinite);
        Assert.False(new Size(double.NegativeInfinity, 0).IsFinite);
        Assert.Throws<NotSupportedException>(() => offset.TruncateDivide(0));
        Assert.Equal(new Point((double)long.MaxValue, (double)long.MinValue),
            new Point(1e100, -1e100).TruncateDivide(1));
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(new Point(-4, -0.0).Modulo(2).X));
    }

    [Fact]
    public void RectFactoriesAccessorsAndUnsortedEdges()
    {
        Rect rect = DartGeometry.RectFromLTRB(1, 3, 5, 7);
        Assert.Equal((1.0, 3.0, 5.0, 7.0), (rect.Left, rect.Top, rect.Right, rect.Bottom));
        Assert.Equal("Rect.fromLTRB(1.0, 3.0, 5.0, 7.0)", DartGeometry.RectToString(rect));
        Assert.Equal(new Rect(-1.5, -0.5, 5, 7), DartGeometry.RectFromCenter(new Point(1, 3), 5, 7));
        Assert.Equal(default, DartGeometry.RectFromCenter(default, 0, 0));
        Rect nanX = DartGeometry.RectFromCenter(new Point(double.NaN, 0), 0, 0);
        Assert.True(double.IsNaN(nanX.Left) && double.IsNaN(nanX.Right));
        Assert.Equal(0, nanX.Top);
        Assert.Equal(0, nanX.Bottom);
        Rect nanY = DartGeometry.RectFromCenter(new Point(0, double.NaN), 0, 0);
        Assert.True(double.IsNaN(nanY.Top) && double.IsNaN(nanY.Bottom));
        Assert.Equal(0, nanY.Left);
        Assert.Equal(0, nanY.Right);
        Rect ltwh = DartGeometry.RectFromLTWH(1, 3, 5, 7);
        Assert.Equal((6.0, 10.0, 5.0, 7.0), (ltwh.Right, ltwh.Bottom, ltwh.ShortestSide, ltwh.LongestSide));
        Assert.Equal(new Rect(1, 3, 4, 4), DartGeometry.RectFromPoints(new Point(5, 3), new Point(1, 7)));
        Assert.Equal(new Rect(-2, -2, 4, 4), DartGeometry.RectFromCircle(default, 2));
        Assert.Equal(new Rect(5, 7, -4, -4), DartGeometry.RectFromLTRB(5, 7, 1, 3));
        Assert.True(DartGeometry.RectFromLTRB(5, 7, 1, 3).IsEmpty);
        Assert.False(rect.ContainsHalfOpen(rect.BottomRight));
        Assert.True(rect.ContainsHalfOpen(rect.TopLeft));
        Assert.Equal(2e9, DartGeometry.RectLargest.Width);
    }

    [Fact]
    public void RectIntersectionAndExpansion_PreserveEmptyAndDisjointBounds()
    {
        Rect a = DartGeometry.RectFromLTRB(0, 0, 100, 100);
        Rect b = DartGeometry.RectFromLTRB(50, 50, 200, 200);
        Assert.Equal(new Rect(50, 50, 50, 50), a.IntersectUnclamped(b));
        Assert.Equal(a.IntersectUnclamped(b), b.IntersectUnclamped(a));
        Assert.Equal(new Rect(0, 0, 200, 200), a.ExpandToInclude(b));
        Assert.Equal(a.ExpandToInclude(b), b.ExpandToInclude(a));
        Rect vertical = DartGeometry.RectFromLTRB(50, 0, 50, 200);
        Rect horizontal = DartGeometry.RectFromLTRB(0, 50, 200, 50);
        Assert.Equal(new Rect(0, 0, 200, 200), vertical.ExpandToInclude(horizontal));
        Assert.Equal(new Rect(5, 5, -3, -3), new Rect(0, 0, 2, 2).IntersectUnclamped(new Rect(5, 5, 2, 2)));
        Assert.False(a.Overlaps(new Rect(100, 0, 10, 10)));
        Assert.True(a.Overlaps(b));
        Assert.Equal(new Rect(60, 70, 150, 150), b.Shift(new Point(10, 20)));
        Assert.Equal(new Rect(60, 70, 150, 150), b.Translate(10, 20));
        Assert.Equal(new Rect(2, 2, -2, -2), DartGeometry.RectDeflate(new Rect(0, 0, 2, 2), 2));
    }

    [Fact]
    public void NullableLerp_UsesWeightedSumAndPreservesInfinity()
    {
        Assert.Null(DartGeometry.OffsetLerp(null, null, 0.5));
        Assert.Null(DartGeometry.SizeLerp(null, null, 0.5));
        Assert.Null(DartGeometry.RectLerp(null, null, 0.5));
        Assert.Null(Radius.Lerp(null, null, 0.5));
        Assert.Equal(new Point(2, 3), DartGeometry.OffsetLerp(null, new Point(4, 6), 0.5));
        Assert.Equal(new Point(2, 3), DartGeometry.OffsetLerp(new Point(4, 6), null, 0.5));
        Assert.Equal(new Size(2, 3), DartGeometry.SizeLerp(null, new Size(4, 6), 0.5));
        Assert.Equal(new Size(2, 3), DartGeometry.SizeLerp(new Size(4, 6), null, 0.5));
        Assert.Equal(new Rect(2, 3, 4, 5), DartGeometry.RectLerp(null, new Rect(4, 6, 8, 10), 0.5));
        Assert.Equal(new Rect(2, 3, 4, 5), DartGeometry.RectLerp(new Rect(4, 6, 8, 10), null, 0.5));
        Assert.Equal(Radius.Circular(2), Radius.Lerp(null, Radius.Circular(4), 0.5));
        Assert.Equal(Radius.Circular(2), Radius.Lerp(Radius.Circular(4), null, 0.5));
        Assert.Equal(Radius.Circular(-4), Radius.Lerp(Radius.Zero, Radius.Circular(4), -1));
        Assert.Equal(DartGeometry.OffsetInfinite,
            DartGeometry.OffsetLerp(DartGeometry.OffsetInfinite, new Point(4, 4), 0.5));
        Assert.True(double.IsNaN(DartGeometry.OffsetLerp(DartGeometry.OffsetInfinite,
            DartGeometry.OffsetInfinite, 0)!.Value.X));
    }

    [Fact]
    public void RadiusClampingAndArithmetic_RetainSignedValuesUntilExplicitlyClamped()
    {
        Radius mixed = new(-100, 100);
        Assert.Equal(new Radius(-100, 100), mixed.Clamp());
        Assert.Equal(new Radius(0, 100), mixed.Clamp(minimum: Radius.Zero));
        Assert.Equal(new Radius(-100, 10), mixed.Clamp(maximum: Radius.Circular(10)));
        Assert.Equal(new Radius(0, 10), mixed.Clamp(Radius.Zero, Radius.Circular(10)));
        Assert.Equal(new Radius(10, 0), new Radius(100, -100).Clamp(Radius.Zero, Radius.Circular(10)));
        Assert.Equal(new Radius(5, 20), mixed.ClampValues(5, 6, 10, 20));
        Assert.Equal(new Radius(10, 6), new Radius(100, -100).ClampValues(5, 6, 10, 20));
        Assert.Equal(Radius.Zero, Radius.Circular(-100).ClampValues(minimumX: 0, minimumY: 0));
        Assert.Equal(new Radius(10, 20), Radius.Circular(100).ClampValues(maximumX: 10, maximumY: 20));
        Assert.Equal(new Radius(3, 4), new Radius(double.NaN, double.NaN).ClampValues(maximumX: 3, maximumY: 4));
        Assert.Equal(new Radius(100, -100), -mixed);
        Assert.Equal(new Radius(-200, 200), mixed + mixed);
        Assert.Equal(Radius.Zero, mixed - mixed);
        Assert.Equal(new Radius(-50, 50), mixed / 2);
        Assert.Equal(new Radius(-33, 33), mixed.TruncateDivide(3));
        Assert.Equal(new Radius(2, 1), mixed % 3);
        Assert.Equal("Radius.circular(-1.0)", Radius.Circular(-1).ToString());
        Assert.Equal("Radius.elliptical(-100.0, 100.0)", mixed.ToString());
    }

    [DebugOnlyFact]
    public void RoundRectConstructors_AssertOnEveryNegativeOrNaNCorner()
    {
        foreach (Radius radius in new[] { Radius.Circular(-1), new Radius(0, -1), new Radius(double.NaN, 0) })
        {
            for (int index = 0; index < 4; index++)
            {
                Radius[] corners = [default, default, default, default];
                corners[index] = radius;
                Assert.Throws<AssertionError>(() => new RRect(default, corners[0], corners[1], corners[2], corners[3]));
                Assert.Throws<AssertionError>(() => new RSuperellipse(
                    default, corners[0], corners[1], corners[2], corners[3]));
            }
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10000)]
    public void RRectContainsAndScaleRadii_MatchFlutterAsymmetricProbes(double scale)
    {
        RRect shape = RRect.FromRectAndCorners(new Rect(1, 1, 1, 1),
            topLeft: Radius.Circular(0.5 * scale), topRight: Radius.Circular(0.25 * scale),
            bottomRight: new Radius(0.25 * scale, 0.75 * scale));
        foreach ((Point point, bool inside) in new (Point, bool)[] {
                     (new(1, 1), false), (new(1.1, 1.1), false), (new(1.15, 1.15), true), (new(2, 1), false),
                     (new(1.93, 1.07), false), (new(1.97, 1.7), false), (new(1.7, 1.97), true), (new(1, 1.99), true) })
        {
            Assert.Equal(inside, shape.Contains(point));
        }
        RRect scaled = shape.ScaleRadii();
        Assert.Equal(new Rect(1, 1, 1, 1), scaled.OuterRect);
        Assert.Equal(new Radius(0.5, 0.5), scaled.TopLeft);
        Assert.Equal(new Radius(0.25, 0.25), scaled.TopRight);
        Assert.Equal(new Radius(0.25, 0.75), scaled.BottomRight);
        Assert.Equal(Radius.Zero, scaled.BottomLeft);
        Assert.True(shape.Contains(new Point(double.NaN, 1)));
    }

    [Fact]
    public void RoundedShapes_SharedGeometryConstructorsLerpAndFormatting()
    {
        RRect shape = RRect.FromLTRBAndCorners(10, 20, 30, 40,
            new Radius(1, 2), new Radius(3, 4), new Radius(5, 6), new Radius(7, 8));
        Assert.Equal(new Rect(10, 20, 20, 20), shape.OuterRect);
        Assert.Equal(new Rect(17, 24, 8, 8), shape.MiddleRect);
        Assert.Equal(new Rect(10, 24, 20, 8), shape.WideMiddleRect);
        Assert.Equal(new Rect(17, 20, 8, 20), shape.TallMiddleRect);
        Assert.Equal(10 + 7 * 0.29289321881, shape.SafeInnerRect.Left);
        Assert.Equal(new Point(20, 30), shape.Center);
        Assert.Equal(20, shape.LongestSide);
        Assert.False(shape.IsRect || shape.IsEllipse || shape.IsStadium || shape.IsCircle || shape.HasNaN);
        Assert.True(shape.IsFinite);
        Assert.Equal(new Rect(11, 22, 20, 20), shape.Shift(new Point(1, 2)).Rect);
        Assert.True(RRect.FromRectXY(new Rect(0, 0, 20, 10), 10, 5).IsEllipse);
        Assert.True(RRect.FromRectXY(new Rect(0, 0, 20, 20), 10, 10).IsCircle);
        Assert.True(RRect.FromRectXY(new Rect(0, 0, 30, 10), 5, 5).IsStadium);
        Assert.True(RRect.FromRectXY(new Rect(0, 0, 20, 20), 0, 10).IsRect);
        Assert.Equal(0, RRect.FromRectXY(new Rect(0, 0, 0, 10), 3, 4).ScaleRadii().TopLeft.X);
        RRect inverted = RRect.FromLTRBXY(30, 40, 10, 20, 1, 2);
        Assert.Equal((30.0, 40.0, 10.0, 20.0), (inverted.Left, inverted.Top, inverted.Right, inverted.Bottom));
        Assert.True(inverted.IsEmpty);
        Assert.False(inverted.Contains(new Point(20, 30)));
        RRect infinite = RRect.FromLTRBXY(double.PositiveInfinity, 0, double.PositiveInfinity, 10, 0, 0);
        Assert.Equal(double.PositiveInfinity, infinite.Right);
        Assert.False(infinite.HasNaN);
        Assert.True(infinite.IsEmpty);
        Assert.Null(RRect.Lerp(null, null, 0.5));
        Assert.Null(RSuperellipse.Lerp(null, null, 0.5));
        Assert.Equal(shape.Inflate(2), RRect.Lerp(shape, shape.Inflate(4), 0.5));
        Assert.Equal(new Rect(5, 10, 10, 10), RRect.Lerp(shape, null, 0.5)!.Value.Rect);
        Assert.Equal(RRect.Lerp(shape, null, 0.5), RRect.Lerp(null, shape, 0.5));
        Assert.Equal(Radius.Zero, RRect.Lerp(RRect.Zero, shape, -1)!.Value.TopLeft);
        Assert.Equal("RRect.fromLTRBXY(10.0, 20.0, 30.0, 40.0, 1.0, 2.0)",
            RRect.FromLTRBXY(10, 20, 30, 40, 1, 2).ToString());
        Assert.Equal("RSuperellipse.fromLTRBR(0.0, 0.0, 10.0, 10.0, 2.0)",
            RSuperellipse.FromLTRBR(0, 0, 10, 10, Radius.Circular(2)).ToString());
        var super = RSuperellipse.FromRectAndCorners(shape.Rect,
            shape.TopLeft, shape.TopRight, shape.BottomRight, shape.BottomLeft);
        Assert.Equal(shape.SafeInnerRect, super.SafeInnerRect);
        Assert.Equal(shape.MiddleRect, super.MiddleRect);
        Assert.Equal(shape.WideMiddleRect, super.WideMiddleRect);
        Assert.Equal(shape.TallMiddleRect, super.TallMiddleRect);
        Assert.Equal(shape.IsEllipse, super.IsEllipse);
        Assert.Equal(new Rect(5, 10, 10, 10), RSuperellipse.Lerp(null, super, 0.5)!.Value.Rect);
        Assert.Equal(super.Inflate(2), RSuperellipse.Lerp(super, super.Inflate(4), 0.5));
    }

    [Fact]
    public void InflateAndDeflate_ClampEachRadiusAndPreserveInvertedBounds()
    {
        RRect inflated = RRect.FromLTRBAndCorners(10, 20, 30, 40, 1, 2, 4, 3);
        RRect deflated = inflated;
        RSuperellipse super = RSuperellipse.FromLTRBAndCorners(10, 20, 30, 40, 1, 2, 4, 3);
        for (int step = 1; step <= 4; step++)
        {
            inflated = inflated.Inflate(-1);
            deflated = deflated.Deflate(1);
            super = super.Deflate(1);
            Assert.Equal(inflated, deflated);
            Assert.Equal(Radius.Circular(Math.Max(0, 1 - step)), deflated.TopLeft);
            Assert.Equal(Radius.Circular(Math.Max(0, 2 - step)), deflated.TopRight);
            Assert.Equal(Radius.Circular(Math.Max(0, 4 - step)), deflated.BottomRight);
            Assert.Equal(Radius.Circular(Math.Max(0, 3 - step)), deflated.BottomLeft);
            Assert.Equal(deflated.TopLeft, super.TopLeft);
        }
        Assert.True(deflated.Deflate(20).IsEmpty);
        Assert.Equal(Radius.Zero, deflated.DeflateEdges(new Thickness(100)).TopLeft);
    }

    [Fact]
    public void BorderRadiusRetainsNegativeTweenValuesButClampsWhenCreatingShapes()
    {
        BorderRadius radius = BorderRadius.Circular(-3);
        Assert.Equal(Radius.Circular(-3), radius.TopLeftRadius);
        Assert.Equal(Radius.Zero, radius.ToRRect(new Rect(0, 0, 10, 10)).TopLeft);
        Assert.Equal(Radius.Zero, radius.ToRSuperellipse(new Rect(0, 0, 10, 10)).TopLeft);
        var extrapolated = BorderRadius.Lerp(BorderRadius.Zero, BorderRadius.Circular(4), -1)!.Value;
        Assert.Equal(Radius.Circular(-4), extrapolated.TopLeftRadius);
        Assert.Equal(Radius.Zero, extrapolated.ToRRect(new Rect(0, 0, 10, 10)).TopLeft);
    }

    [Fact]
    public void PathConversionNormalizesInvertedBoundsAndFlatRadiiAtPaintBoundary()
    {
        var inverted = RRect.FromLTRBXY(100, 100, 0, 0, 5, 5);
        Assert.True(inverted.ToPath().Contains(new Point(50, 50)));
        var path = new Plumix.UI.Path();
        path.AddRRect(inverted);
        Assert.True(path.Contains(new Point(50, 50)));
        var flat = RRect.FromLTRBXY(0, 0, 100, 100, 0, 5000);
        Assert.True(flat.ToPath().Contains(new Point(1, 1)));
        Assert.True(RSuperellipse.FromLTRBXY(100, 100, 0, 0, 5, 5).ToPath().Contains(new Point(50, 50)));
    }

    [Fact]
    public void GeometryFormatting_UsesDartRoundingAndLargeNumberNotation()
    {
        Assert.Equal("Radius.circular(1.3)", Radius.Circular(1.25).ToString());
        Assert.Equal("Radius.circular(-1.3)", Radius.Circular(-1.25).ToString());
        Assert.Equal("Radius.circular(1.1)", Radius.Circular(1.15).ToString());
        Assert.Equal("Radius.circular(1e+21)", Radius.Circular(1e21).ToString());
        Assert.Equal("Offset(Infinity, NaN)",
            DartGeometry.OffsetToString(new Point(double.PositiveInfinity, double.NaN)));
        Assert.Equal("Size(5.0, 7.0)", DartGeometry.SizeToString(new Size(5, 7)));
    }

    [Fact]
    public void RSTransform_StoresFloat32AndRotatesAboutAnchor()
    {
        var rounded = new RSTransform(1.23456789012, -2.34567890123, 1e40, double.NaN);
        Assert.Equal((double)(float)1.23456789012, rounded.Scos);
        Assert.Equal((double)(float)-2.34567890123, rounded.Ssin);
        Assert.Equal(double.PositiveInfinity, rounded.Tx);
        Assert.True(double.IsNaN(rounded.Ty));
        var transform = RSTransform.FromComponents(Math.PI / 2, 2, 3, 4, 10, 20);
        Assert.Equal(2, transform.Ssin);
        Assert.Equal(18, transform.Tx);
        Assert.Equal(14, transform.Ty);
    }
}
