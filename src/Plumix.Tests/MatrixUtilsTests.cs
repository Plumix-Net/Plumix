using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/painting/matrix_utils.dart

namespace Plumix.Tests;

public sealed class MatrixUtilsTests
{
    [Fact]
    public void GetAsTranslation_MatchesOnlyPureTwoDimensionalTranslations()
    {
        Assert.Equal(new Point(0.0, 0.0), MatrixUtils.GetAsTranslation(Matrix4.Identity()));
        Assert.Null(MatrixUtils.GetAsTranslation(Matrix4.Zero()));
        Assert.Null(MatrixUtils.GetAsTranslation(Matrix4.RotationX(1.0)));
        Assert.Null(MatrixUtils.GetAsTranslation(Matrix4.RotationZ(1.0)));
        Assert.Equal(
            new Point(1.0, 2.0),
            MatrixUtils.GetAsTranslation(Matrix4.TranslationValues(1.0, 2.0, 0.0)));
        Assert.Null(MatrixUtils.GetAsTranslation(Matrix4.TranslationValues(1.0, 2.0, 3.0)));

        Matrix4 rotated = Matrix4.Identity();
        rotated.RotateX(2.0);
        Assert.Null(MatrixUtils.GetAsTranslation(rotated));

        Matrix4 scaled = Matrix4.Identity();
        scaled.ScaleByDouble(2.0, 2.0, 2.0, 1);
        Assert.Null(MatrixUtils.GetAsTranslation(scaled));
    }

    [Fact]
    public void GetAsScale_MatchesOnlySymmetricTwoDimensionalScales()
    {
        Assert.Equal(1.0, MatrixUtils.GetAsScale(Matrix4.Identity()));
        Assert.Equal(2.0, MatrixUtils.GetAsScale(Matrix4.Diagonal3Values(2.0, 2.0, 1.0)));
        Assert.Null(MatrixUtils.GetAsScale(Matrix4.Diagonal3Values(2.0, 3.0, 1.0)));
        Assert.Null(MatrixUtils.GetAsScale(Matrix4.Diagonal3Values(2.0, 2.0, 2.0)));
        Assert.Null(MatrixUtils.GetAsScale(Matrix4.TranslationValues(1.0, 0.0, 0.0)));
    }

    [Fact]
    public void MultiplyInPlace_StoresTheResultInTheSecondArgument()
    {
        Matrix4 a = Matrix4.Identity();
        a.TranslateByDouble(10.0, 0.0, 0.0, 1);
        Matrix4 b = Matrix4.Identity();
        b.TranslateByDouble(20.0, 0.0, 0.0, 1);
        Matrix4 originalA = Matrix4.Copy(a);
        Matrix4 expected = a.Multiplied(b);

        MatrixUtils.MultiplyInPlace(a, b);

        Assert.Equal(originalA, a);
        Assert.Equal(expected, b);
        Assert.Equal(30.0, b.GetTranslation().X);
    }

    [Fact]
    public void MatrixEquals_TreatsNullAsTheIdentity()
    {
        Assert.True(MatrixUtils.MatrixEquals(null, null));
        Assert.True(MatrixUtils.MatrixEquals(null, Matrix4.Identity()));
        Assert.True(MatrixUtils.MatrixEquals(Matrix4.Identity(), null));
        Assert.False(MatrixUtils.MatrixEquals(null, Matrix4.Zero()));
        Assert.True(MatrixUtils.MatrixEquals(Matrix4.RotationZ(1.0), Matrix4.RotationZ(1.0)));
    }

    [Fact]
    public void TransformPoint_DividesThroughByTheResultingW()
    {
        Matrix4 perspective = Matrix4.Identity();
        perspective.SetEntry(3, 0, 0.01);

        // w = 0.01 * 100 + 1 = 2, so the point lands at half its affine position.
        Assert.Equal(new Point(50.0, 10.0), MatrixUtils.TransformPoint(perspective, new Point(100.0, 20.0)));
        Assert.Equal(
            new Point(11.0, 22.0),
            MatrixUtils.TransformPoint(Matrix4.TranslationValues(1.0, 2.0, 0.0), new Point(10.0, 20.0)));
    }

    [Fact]
    public void TransformRect_HandlesIdentityScaleAndRotation()
    {
        var rect = new Rect(10.0, 20.0, 20.0, 20.0);

        Assert.Equal(rect, MatrixUtils.TransformRect(Matrix4.Identity(), rect));

        Rect scaled = MatrixUtils.TransformRect(Matrix4.Diagonal3Values(2.0, 2.0, 2.0), rect);
        Assert.Equal(new Rect(20.0, 40.0, 40.0, 40.0), scaled);

        Rect rotated = MatrixUtils.TransformRect(Matrix4.RotationZ(Math.PI / 2.0), rect);
        Assert.Equal(-40.0, rotated.Left, precision: 5);
        Assert.Equal(10.0, rotated.Top, precision: 5);
        Assert.Equal(-20.0, rotated.Right, precision: 5);
        Assert.Equal(30.0, rotated.Bottom, precision: 5);
    }

    [Fact]
    public void TransformRect_KeepsVeryLargeFiniteRectsFinite()
    {
        var rect = new Rect(
            new Point(0.0, -1.7976931348623157e+308),
            new Point(800.0, 1.7976931348623157e+308));
        Matrix4 transform = Matrix4.Identity();
        transform.TranslateByDouble(10.0, 0.0, 0.0, 1);

        Rect result = MatrixUtils.TransformRect(transform, rect);

        // The corner-by-corner fallback keeps the horizontal extent exact; the optimized path would
        // multiply the infinite height by the zero skew term and produce NaN.
        Assert.Equal(10.0, result.Left);
        Assert.Equal(810.0, result.Right);
        Assert.Equal(-1.7976931348623157e+308, result.Top);
        Assert.True(double.IsFinite(result.Width));
    }

    [Fact]
    public void TransformRect_WithPerspectiveMatchesTheCornerwiseProjection()
    {
        Matrix4 transform = MatrixUtils.CreateCylindricalProjectionTransform(
            radius: 10.0,
            angle: Math.PI / 8.0,
            perspective: 0.3);

        for (int i = 1; i < 500; i++)
        {
            var rect = new Rect(11.0 * i, 12.0 * i, (15.0 - 11.0) * i, (18.0 - 12.0) * i);
            Rect actual = MatrixUtils.TransformRect(transform, rect);

            double minX = double.PositiveInfinity;
            double minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double maxY = double.NegativeInfinity;
            foreach (Point corner in new[] { rect.TopLeft, rect.TopRight, rect.BottomLeft, rect.BottomRight })
            {
                Vector3 projected = transform.PerspectiveTransform(new Vector3(corner.X, corner.Y, 0.0));
                minX = Math.Min(minX, projected.X);
                minY = Math.Min(minY, projected.Y);
                maxX = Math.Max(maxX, projected.X);
                maxY = Math.Max(maxY, projected.Y);
            }

            Assert.Equal(minX, actual.Left, precision: 5);
            Assert.Equal(minY, actual.Top, precision: 5);
            Assert.Equal(maxX, actual.Right, precision: 5);
            Assert.Equal(maxY, actual.Bottom, precision: 5);
        }
    }

    [Fact]
    public void InverseTransformRect_ShortCircuitsOnTheIdentity()
    {
        var rect = new Rect(10.0, 20.0, 20.0, 20.0);

        Assert.Equal(rect, MatrixUtils.InverseTransformRect(Matrix4.Identity(), rect));
        Assert.Equal(
            new Rect(5.0, 10.0, 10.0, 10.0),
            MatrixUtils.InverseTransformRect(Matrix4.Diagonal3Values(2.0, 2.0, 2.0), rect));
    }

    [Fact]
    public void ForceToPoint_CollapsesEveryPointOntoTheOffset()
    {
        Matrix4 forced = MatrixUtils.ForceToPoint(new Point(20.0, -30.0));

        foreach (Point probe in new[]
                 {
                     new Point(20.0, -30.0),
                     new Point(0.0, 0.0),
                     new Point(1.0, 1.0),
                     new Point(-1.0, -1.0),
                     new Point(-20.0, 30.0),
                     new Point(-1.2344, 1422434.23),
                 })
        {
            Assert.Equal(new Point(20.0, -30.0), MatrixUtils.TransformPoint(forced, probe));
        }
    }

    [Fact]
    public void CreateCylindricalProjectionTransform_MatchesTheDegenerateCases()
    {
        Assert.Equal(
            Matrix4.Identity(),
            MatrixUtils.CreateCylindricalProjectionTransform(radius: 0.0, angle: 0.0, perspective: 0.0));
        Assert.Equal(
            Matrix4.RotationX(Math.PI / 2.0),
            MatrixUtils.CreateCylindricalProjectionTransform(
                radius: 0.0,
                angle: Math.PI / 2.0,
                perspective: 0.0));
        Assert.Equal(
            Matrix4.Identity(),
            MatrixUtils.CreateCylindricalProjectionTransform(radius: 1000000.0, angle: 0.0, perspective: 0.0));
    }

    [Fact]
    public void CreateCylindricalProjectionTransform_MatchesTheSpotCheck()
    {
        double[] storage = MatrixUtils
            .CreateCylindricalProjectionTransform(radius: 100.0, angle: Math.PI / 3.0)
            .Storage;

        Assert.Equal(1.0, storage[0]);
        Assert.Equal(0.0, storage[1]);
        Assert.Equal(0.0, storage[2]);
        Assert.Equal(0.0, storage[3]);
        Assert.Equal(0.0, storage[4]);
        Assert.Equal(0.5, storage[5], precision: 10);
        Assert.Equal(0.8660254037844386, storage[6], precision: 10);
        Assert.Equal(-0.0008660254037844386, storage[7], precision: 10);
        Assert.Equal(0.0, storage[8]);
        Assert.Equal(-0.8660254037844386, storage[9], precision: 10);
        Assert.Equal(0.5, storage[10], precision: 10);
        Assert.Equal(-0.0005, storage[11], precision: 10);
        Assert.Equal(0.0, storage[12]);
        Assert.Equal(-86.60254037844386, storage[13], precision: 10);
        Assert.Equal(-50.0, storage[14], precision: 10);
        Assert.Equal(1.05, storage[15], precision: 10);
    }
}
