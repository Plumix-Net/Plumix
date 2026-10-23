using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using Path = Plumix.UI.Path;
using Radius = Plumix.Rendering.Radius;

namespace Plumix.Tests;

// Dart parity source: flutter/engine/src/flutter/testing/dart/path_test.dart
// and lib/web_ui/test/ui/{path_test,path_metrics_test}.dart.
public sealed class PathDartParityTests
{
    [Fact]
    public void BoundsCloneShiftAndResetFollowNativePath()
    {
        var path = new Path { FillType = PathFillType.EvenOdd };
        Assert.Equal("Path", path.ToString());
        Assert.Equal(default, path.GetBounds());
        path.AddRect(new Rect(1, 3, 4, 4));
        Assert.Equal(new Rect(1, 3, 4, 4), path.GetBounds());
        var copy = new Path(path);
        path.LineTo(20, 15);
        Assert.Equal(new Rect(1, 3, 19, 12), path.GetBounds());
        Assert.Equal(new Rect(1, 3, 4, 4), copy.GetBounds());
        Path shifted = copy.Shift(new Point(10, 10));
        Assert.Equal(new Rect(11, 13, 4, 4), shifted.GetBounds());
        Path zeroShifted = copy.Shift(default);
        Assert.NotSame(copy, zeroShifted);
        zeroShifted.Reset();
        Assert.Equal(PathFillType.NonZero, zeroShifted.FillType);
        Assert.Equal(new Rect(1, 3, 4, 4), copy.GetBounds());
    }

    [Fact]
    public void BoundsUseControlPointsAndIgnoreOnlyTrailingMove()
    {
        var path = new Path();
        path.MoveTo(0, 0);
        path.QuadraticBezierTo(100, 200, 200, 0);
        Assert.Equal(new Rect(0, 0, 200, 200), path.GetBounds());
        path.MoveTo(1000, 1000);
        Assert.Equal(new Rect(0, 0, 200, 200), path.GetBounds());
        path.LineTo(1001, 1001);
        Assert.Equal(new Rect(0, 0, 1001, 1001), path.GetBounds());
        path.MoveTo(double.PositiveInfinity, 0);
        Assert.Equal(default, path.GetBounds());
        path.Reset();
        path.MoveTo(7, 9);
        Assert.Equal(new Rect(7, 9, 0, 0), path.GetBounds());
        path.Reset();
        path.MoveTo(double.MaxValue, 0);
        Assert.Equal((double)float.MaxValue, path.GetBounds().X);
    }

    [Theory]
    [InlineData(PathOperation.Difference, false, true, false)]
    [InlineData(PathOperation.Intersect, true, false, false)]
    [InlineData(PathOperation.Union, true, true, true)]
    [InlineData(PathOperation.Xor, false, true, true)]
    [InlineData(PathOperation.ReverseDifference, false, false, true)]
    public void BooleanOperationsProduceIndependentExtensibleContours(
        PathOperation operation, bool overlap, bool leftOnly, bool rightOnly)
    {
        var left = new Path();
        left.AddRect(new Rect(0, 0, 40, 40));
        var right = new Path();
        right.AddRect(new Rect(20, 0, 40, 40));
        Path result = Path.Combine(operation, left, right);
        left.Reset();
        right.Reset();
        Assert.Equal(overlap, result.Contains(new Point(30, 20)));
        Assert.Equal(leftOnly, result.Contains(new Point(10, 20)));
        Assert.Equal(rightOnly, result.Contains(new Point(50, 20)));
        result.AddOval(new Rect(80, 0, 20, 20));
        Assert.True(result.Contains(new Point(90, 10)));
        var appended = new Path();
        appended.AddPath(result, new Point(0, 50));
        Assert.True(appended.Contains(new Point(90, 60)));
        Assert.NotEmpty(result.ComputeMetrics());
    }

    [Fact]
    public void BooleanBoundsAreTheSolvedGeometryRatherThanOperandBounds()
    {
        var first = new Path();
        first.AddOval(new Rect(0, 0, 20, 20));
        var second = new Path();
        second.AddOval(new Rect(-5, -5, 20, 20));
        Assert.InRange(Path.Combine(PathOperation.Difference, first, second).GetBounds().Top, 0.87, 0.89);
        Assert.InRange(Path.Combine(PathOperation.ReverseDifference, first, second).GetBounds().Right, 14.10, 14.12);
        Assert.Equal(new Rect(-5, -5, 25, 25), Path.Combine(PathOperation.Union, first, second).GetBounds());
        Assert.Equal(new Rect(0, 0, 15, 15), Path.Combine(PathOperation.Intersect, first, second).GetBounds());
        var rect = new Path();
        rect.AddRect(new Rect(0, 0, 20, 20));
        Assert.Equal(default, Path.Combine(PathOperation.Difference, rect, rect).GetBounds());
        Assert.Empty(Path.Combine(PathOperation.Xor, rect, rect).ComputeMetrics());
    }

    [Fact]
    public void InvalidBooleanInputReportsDartFailure()
    {
        var invalid = new Path();
        invalid.MoveTo(double.NaN, 0);
        invalid.LineTo(10, 10);
        var other = new Path();
        other.AddRect(new Rect(0, 0, 10, 10));
        Assert.Equal(
            "Path.combine() failed.  This may be due an invalid path; in particular, check for NaN values.",
            Assert.Throws<InvalidOperationException>(() => Path.Combine(PathOperation.Union, invalid, other)).Message);
    }

    [Fact]
    public void TransformAppendAndExtendMatchEngineTests()
    {
        var path = new Path();
        path.AddRect(new Rect(0, 0, 10, 10));
        Matrix4 matrix = Matrix4.Diagonal3Values(2.5, 0.5, 1);
        Assert.Equal(new Rect(0, 0, 25, 5), path.Transform(matrix.Storage).GetBounds());
        Assert.Equal(new Rect(0, 0, 10, 10), path.GetBounds());
        var line = new Path();
        line.MoveTo(0, 0);
        line.LineTo(10, 10);
        path.AddPath(line, new Point(10, 10));
        Assert.Equal(new Rect(0, 0, 20, 20), path.GetBounds());
        path.AddPath(line, new Point(20, 20), matrix.Storage);
        Assert.Equal(new Rect(0, 0, 45, 25), path.GetBounds());
        path.ExtendWithPath(line, default);
        path.ExtendWithPath(line, new Point(45, 25), matrix.Storage);
        Assert.Equal(new Rect(0, 0, 70, 30), path.GetBounds());

        var extended = new Path();
        extended.MoveTo(0, 0);
        extended.LineTo(10, 0);
        line.Reset();
        line.MoveTo(20, 0);
        line.LineTo(30, 0);
        extended.ExtendWithPath(line, default);
        Assert.Equal(30, Assert.Single(extended.ComputeMetrics()).Length);
    }

    [Fact]
    public void PerspectiveUsesTheFullMatrixAndPreservesCurves()
    {
        var path = new Path();
        path.AddRect(new Rect(0, 0, 10, 10));
        Matrix4 matrix = Matrix4.Identity();
        matrix.Storage[3] = 0.1;
        Path transformed = path.Transform(matrix.Storage);
        Assert.Equal(new Rect(0, 0, 5, 10), transformed.GetBounds());
        Assert.True(transformed.Contains(new Point(2, 2)));
        Assert.False(transformed.Contains(new Point(8, 2)));
        path.Reset();
        path.MoveTo(0, 0);
        path.QuadraticBezierTo(10, 20, 20, 0);
        Path scaled = path.Transform(Matrix.CreateScale(2, 3));
        Assert.Equal(new Rect(0, 0, 40, 60), scaled.GetBounds());
        Assert.True(scaled.Contains(new Point(20, 29.99)));
        Assert.False(scaled.Contains(new Point(20, 30.01)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RelativeCurvesResumeFromTheClosedContourStart(int command)
    {
        var path = new Path();
        path.MoveTo(10, 20);
        path.LineTo(80, 90);
        path.Close();
        path.Close();
        switch (command)
        {
            case 0: path.RelativeLineTo(2, 3); break;
            case 1: path.RelativeQuadraticBezierTo(1, 1, 2, 3); break;
            case 2: path.RelativeCubicTo(1, 1, 2, 2, 2, 3); break;
            case 3: path.RelativeConicTo(1, 1, 2, 3, 0.5); break;
        }

        PathMetric metric = path.ComputeMetrics().Last();
        Assert.Equal(new Point(12, 23), metric.GetTangentForOffset(metric.Length)!.Position);
    }

    [Fact]
    public void RelativeMovesAndLargePolygonsMatchWebTests()
    {
        var path = new Path();
        path.MoveTo(100, 100);
        path.RelativeLineTo(-50, -50);
        path.RelativeLineTo(100, 0);
        path.RelativeLineTo(-50, 50);
        Assert.Equal(new Rect(50, 50, 100, 50), path.GetBounds());
        path.Close();
        path.RelativeMoveTo(5, 5);
        path.RelativeLineTo(1, 1);
        Assert.Equal(new Point(106, 106), path.ComputeMetrics().Last().GetTangentForOffset(100)!.Position);
        path.Reset();
        path.AddPolygon(Enumerable.Range(0, 10001).Select(index => new Point(index, index)).ToArray(), false);
        Assert.Equal(new Rect(0, 0, 10000, 10000), path.GetBounds());
    }

    [Fact]
    public void MetricsIteratorSnapshotClosureExtractionAndTangentsMatchEngine()
    {
        var path = new Path();
        path.LineTo(10, 0);
        PathMetrics metrics = path.ComputeMetrics();
        PathMetricIterator iterator = metrics.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => iterator.Current);
        Assert.True(iterator.MoveNext());
        PathMetric metric = iterator.Current;
        Assert.Equal(10, metric.Length);
        Assert.False(metric.IsClosed);
        Assert.Equal(0, metric.ContourIndex);
        Assert.Equal(new Rect(1, 0, 8, 0), metric.ExtractPath(1, 9).GetBounds());
        Assert.Equal(new Point(1, 0), metric.GetTangentForOffset(1)!.Position);
        Assert.Equal(0, metric.GetTangentForOffset(1)!.Angle);
        Assert.Equal(new Point(0, 0), metric.GetTangentForOffset(-100)!.Position);
        Assert.Equal(new Point(10, 0), metric.GetTangentForOffset(100)!.Position);
        Assert.Null(metric.GetTangentForOffset(double.NaN));
        Assert.Equal(default, metric.ExtractPath(9, 1).GetBounds());
        Assert.Equal(new Rect(5, 0, 0, 0), metric.ExtractPath(5, 5).GetBounds());
        Assert.Equal(new Rect(0, 0, 10, 0), metric.ExtractPath(-100, 100).GetBounds());
        Assert.False(iterator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => iterator.Current);
        Assert.Empty(metrics);
        Assert.Same(iterator, metrics.GetEnumerator());
        path.LineTo(10, 10);
        path.LineTo(0, 10);
        path.Close();
        Assert.Equal(10, metric.Length);
        Assert.Equal(40, Assert.Single(path.ComputeMetrics()).Length);
        var line = new Path();
        line.LineTo(10, 0);
        PathMetric closed = Assert.Single(line.ComputeMetrics(forceClosed: true));
        Assert.Equal(20, closed.Length);
        Assert.True(closed.IsClosed);
        Assert.Equal(new Rect(1, 0, 8, 0), closed.ExtractPath(1, 9).GetBounds());
    }

    [Fact]
    public void PreviousMetricsSurviveAdvancementAndEarlyEnumerationDisposal()
    {
        var path = new Path();
        path.LineTo(0, 10);
        path.Close();
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        PathMetrics metrics = path.ComputeMetrics();
        PathMetric first = metrics.First();
        PathMetric second = Assert.Single(metrics);
        Assert.Equal(20, first.Length);
        Assert.True(first.IsClosed);
        Assert.Equal(new Point(0, 1), first.GetTangentForOffset(5)!.Vector);
        Assert.Equal(6, Assert.Single(first.ExtractPath(4, 10).ComputeMetrics()).Length);
        Assert.Equal(new Point(1, 0), second.GetTangentForOffset(5)!.Vector);
        Assert.Equal(2, Assert.Single(second.ExtractPath(4, 6).ComputeMetrics()).Length);
        Assert.Equal("PathMetric(length: 10.0, isClosed: false, contourIndex: 1)", second.ToString());
        path.Reset();
        path.MoveTo(4, 5);
        path.LineTo(4, 5);
        Assert.Empty(path.ComputeMetrics());
    }

    [Fact]
    public void TangentAnglesPreserveThePinnedSign()
    {
        var path = new Path();
        path.LineTo(0, 10);
        Assert.Equal(-Math.PI / 2, Assert.Single(path.ComputeMetrics()).GetTangentForOffset(5)!.Angle, 5);
        path.Reset();
        path.LineTo(10, 10);
        Assert.Equal(-Math.PI / 4, Assert.Single(path.ComputeMetrics()).GetTangentForOffset(5)!.Angle, 5);
        Tangent tangent = Tangent.FromAngle(new Point(2, 3), Math.PI / 2);
        Assert.Equal(new Point(2, 3), tangent.Position);
        Assert.Equal(-Math.PI / 2, tangent.Angle, 5);
    }

    [Theory]
    [InlineData(0, 159.473)]
    [InlineData(1, 146.567)]
    [InlineData(2, 600)]
    [InlineData(3, 590.408)]
    public void NativeCurveAndRoundedRectangleLengthsMatchWebTests(int kind, double length)
    {
        var path = new Path();
        switch (kind)
        {
            case 0:
                path.MoveTo(20, 100);
                path.QuadraticBezierTo(80, 10, 140, 110);
                break;
            case 1:
                path.MoveTo(20, 100);
                path.CubicTo(80, 10, 120, 90, 140, 40);
                break;
            case 2: path.AddRRect(RRect.FromRectXY(new Rect(20, 30, 200, 100), 0, 0)); break;
            case 3: path.AddRRect(RRect.FromRectXY(new Rect(20, 30, 200, 100), 8, 4)); break;
        }

        Assert.InRange(Assert.Single(path.ComputeMetrics()).Length, length - 0.1, length + 0.1);
    }

    [Theory]
    [InlineData(90, 100, false, 156.827)]
    [InlineData(180, 100, false, 313.654)]
    [InlineData(270, 100, true, 470.482)]
    [InlineData(270, 50, true, 362.733)]
    public void EndpointArcsMatchWebMetrics(double degrees, double radiusY, bool large, double length)
    {
        var path = new Path();
        path.MoveTo(250, 100);
        double angle = degrees * Math.PI / 180;
        path.ArcToPoint(new Point(150 + 100 * Math.Cos(angle), 100 + radiusY * Math.Sin(angle)),
            new Radius(100, radiusY), largeArc: large);
        Assert.InRange(Assert.Single(path.ComputeMetrics()).Length, length - 0.1, length + 0.1);
        Path shifted = path.Shift(new Point(3, 5));
        var relative = new Path();
        relative.MoveTo(253, 105);
        relative.RelativeArcToPoint(new Point(100 * (Math.Cos(angle) - 1), radiusY * Math.Sin(angle)),
            new Radius(100, radiusY), largeArc: large);
        Assert.Equal(shifted.GetBounds(), relative.GetBounds());
    }

    [Fact]
    public void ExactCurveHitTestingAndPaintKeepTheCurveBetweenOldPolylineVertices()
    {
        var path = new Path();
        path.MoveTo(0, 0);
        path.QuadraticBezierTo(100, 200, 200, 0);
        path.Close();
        // At x=106.25, the true curve is y=99.609375; old 16-segment flattening gave y=99.21875.
        Assert.True(path.Contains(new Point(106.25, 99.5)));
        Assert.False(path.Contains(new Point(106.25, 99.7)));
        Assert.True(path.ToGeometry().FillContains(new Point(106.25, 99.5)));
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        canvas.Scale(4, 4);
        canvas.DrawPath(path, new Paint { Color = new Color(0xFFFF0000) });
        var picture = recorder.EndRecording();
        using var image = picture.ToImageSync(800, 420);
        Assert.True(RasterBackend.PixelAt(image, 425, 397).A > 0);
        Assert.Equal(0, RasterBackend.PixelAt(image, 425, 403).A);
    }

    [DebugOnlyFact]
    public void PathValidatesOffsetsRadiiAndMatrices()
    {
        var path = new Path();
        Assert.Throws<AssertionError>(() => path.Contains(new Point(double.NaN, 0)));
        Assert.Throws<AssertionError>(() => path.Shift(new Point(0, double.NaN)));
        Assert.Throws<AssertionError>(() => path.ArcToPoint(default, new Radius(double.NaN, 1)));
        Assert.Throws<AssertionError>(() => path.Transform(new double[15]));
        double[] matrix = Matrix4.Identity().Storage;
        matrix[2] = double.PositiveInfinity;
        Assert.Throws<AssertionError>(() => path.AddPath(new Path(), default, matrix));
    }

    [Fact]
    public void FillRulesAndContourOrientationAreSharedByPaintAndContains()
    {
        var path = new Path();
        path.AddRect(new Rect(0, 0, 100, 100));
        path.AddRect(new Rect(20, 20, 60, 60));
        Assert.True(path.Contains(new Point(50, 50)));
        path.FillType = PathFillType.EvenOdd;
        Assert.False(path.Contains(new Point(50, 50)));
        Assert.False(path.ToGeometry().FillContains(new Point(50, 50)));
        Assert.True(path.Contains(new Point(10, 10)));
        Assert.True(path.ToGeometry().FillContains(new Point(10, 10)));
        path.Reset();
        path.AddRect(new Rect(0, 0, 100, 100));
        path.AddPolygon([new Point(20, 20), new Point(20, 80), new Point(80, 80), new Point(80, 20)], true);
        Assert.False(path.Contains(new Point(50, 50)));
    }

    [Fact]
    public void ArcForceMoveAndAppendVersusExtendPreserveContourBoundaries()
    {
        var arc = new Path();
        arc.MoveTo(-10, 0);
        arc.ArcTo(new Rect(0, 0, 20, 20), 0, Math.PI / 2, forceMoveTo: false);
        Assert.Single(arc.ComputeMetrics());
        var forced = new Path();
        forced.MoveTo(-20, 0);
        forced.LineTo(-10, 0);
        forced.ArcTo(new Rect(0, 0, 20, 20), 0, Math.PI / 2, forceMoveTo: true);
        Assert.Equal(2, forced.ComputeMetrics().Count());
        var added = new Path();
        added.AddArc(new Rect(0, 0, 20, 20), 0, Math.PI * 2);
        Assert.InRange(Assert.Single(added.ComputeMetrics()).Length, 62, 63);
        var line = new Path();
        line.LineTo(10, 0);
        var appended = new Path(line);
        appended.AddPath(line, new Point(20, 0));
        Assert.Equal(2, appended.ComputeMetrics().Count());
        var extended = new Path(line);
        extended.ExtendWithPath(line, new Point(20, 0));
        Assert.Equal(30, Assert.Single(extended.ComputeMetrics()).Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(0.5)]
    [InlineData(double.PositiveInfinity)]
    public void ConicDegeneracyAndRelativeCoordinatesFollowNativeRules(double weight)
    {
        var path = new Path();
        path.MoveTo(10, 20);
        path.ConicTo(20, 0, 30, 20, weight);
        var relative = new Path();
        relative.MoveTo(10, 20);
        relative.RelativeConicTo(10, -20, 20, 0, weight);
        Assert.Equal(path.GetBounds(), relative.GetBounds());
        PathMetric metric = Assert.Single(path.ComputeMetrics());
        Assert.Equal(new Point(30, 20), metric.GetTangentForOffset(metric.Length)!.Position);
        if (weight <= 0)
        {
            Assert.Equal(20, metric.Length);
        }

        Assert.Equal(metric.Length, Assert.Single(relative.ComputeMetrics()).Length);
    }

    [Fact]
    public void MetricsSkipDegenerateContoursAndRememberEarlierIndices()
    {
        var path = new Path();
        path.MoveTo(1, 1);
        path.LineTo(1, 1);
        path.MoveTo(0, 0);
        path.LineTo(70, 0);
        path.MoveTo(3, 4);
        path.LineTo(3, 4);
        path.AddRect(new Rect(0, 0, 40, 20));
        PathMetric[] metrics = path.ComputeMetrics().ToArray();
        Assert.Equal(2, metrics.Length);
        Assert.Equal("PathMetric(length: 70.0, isClosed: false, contourIndex: 0)", metrics[0].ToString());
        Assert.Equal("PathMetric(length: 120.0, isClosed: true, contourIndex: 1)", metrics[1].ToString());
        Assert.Equal(new Point(40, 20), metrics[1].GetTangentForOffset(60)!.Position);
        Assert.Equal(new Point(5, 0), metrics[0].GetTangentForOffset(5)!.Position);
        Assert.Equal(new Point(40, 20), metrics[1].GetTangentForOffset(60)!.Position);
        Assert.Empty(metrics[0].ExtractPath(5, 5).ComputeMetrics());
        Assert.Equal(default, metrics[0].ExtractPath(5, 5, startWithMoveTo: false).GetBounds());
    }

    [Fact]
    public void NativeSuperellipsePathMatchesTheSlimDiagonalEngineProbe()
    {
        var shape = RSuperellipse.FromLTRBAndCorners(-50, -50, 50, 50,
            topLeft: Radius.Circular(1), topRight: Radius.Circular(99),
            bottomRight: Radius.Circular(1), bottomLeft: Radius.Circular(99));
        var path = new Path();
        path.AddRSuperellipse(shape);
        Assert.True(path.Contains(default));
        Assert.False(path.Contains(new Point(49.999, 49.999)));
        Assert.False(path.Contains(new Point(-49.999, -49.999)));
        Assert.True(path.Contains(new Point(-49.70, -49.70)));
        Assert.False(path.Contains(new Point(-49.72, -49.72)));
        Point[] interior =
        [
            new(-40, -49.58), new(-20, -45.64), new(0, -37.01),
            new(20, -21.96), new(21.05, -20.92), new(40, 5.68),
        ];
        foreach (Point point in interior)
        {
            Assert.True(path.Contains(point));
            Assert.False(path.Contains(new Point(point.X + 0.02, point.Y - 0.02)));
            Assert.True(path.Contains(new Point(-point.X, -point.Y)));
            Assert.False(path.Contains(new Point(-point.X - 0.02, -point.Y + 0.02)));
        }
    }
}
