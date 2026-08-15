using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Port of the `vector_math` 2.4.2 `Matrix4` surface Flutter builds on (src/Plumix/UI/Matrix4.cs).

namespace Plumix.Tests;

public sealed class Matrix4Tests
{
    [Fact]
    public void Storage_IsColumnMajorWithTheTranslationInTheLastColumn()
    {
        Matrix4 translation = Matrix4.TranslationValues(3.0, 4.0, 5.0);

        Assert.Equal(3.0, translation.Storage[12]);
        Assert.Equal(4.0, translation.Storage[13]);
        Assert.Equal(5.0, translation.Storage[14]);
        Assert.Equal(3.0, translation.Entry(0, 3));
        Assert.Equal(4.0, translation.Entry(1, 3));
        Assert.Equal(5.0, translation.Entry(2, 3));
        Assert.Equal(new Vector3(3.0, 4.0, 5.0).X, translation.GetTranslation().X);
    }

    [Fact]
    public void Identity_And_Zero_MatchTheirPredicates()
    {
        Assert.True(Matrix4.Identity().IsIdentity());
        Assert.False(Matrix4.Identity().IsZero());
        Assert.True(Matrix4.Zero().IsZero());
        Assert.False(Matrix4.Zero().IsIdentity());
        Assert.Equal(Matrix4.Identity(), Matrix4.Identity());
        Assert.NotEqual(Matrix4.Identity(), Matrix4.Zero());
    }

    [Fact]
    public void Multiply_AppliesTheArgumentFirst()
    {
        // Column vectors: `a.Multiply(b)` maps a point through `b` and then through `a`.
        Matrix4 composed = Matrix4.TranslationValues(10.0, 0.0, 0.0);
        composed.Multiply(Matrix4.Diagonal3Values(2.0, 2.0, 1.0));

        Assert.Equal(new Point(30.0, 20.0), MatrixUtils.TransformPoint(composed, new Point(10.0, 10.0)));
    }

    [Fact]
    public void TranslateByDouble_And_ScaleByDouble_PostMultiply()
    {
        Matrix4 transform = Matrix4.Identity();
        transform.TranslateByDouble(2.0, -2.0, 0.0, 1);
        Assert.Equal(new Point(2.0, -2.0), MatrixUtils.GetAsTranslation(transform));

        transform.TranslateByDouble(4.0, 8.0, 0.0, 1);
        Assert.Equal(new Point(6.0, 6.0), MatrixUtils.GetAsTranslation(transform));

        Matrix4 scaled = Matrix4.TranslationValues(10.0, 0.0, 0.0);
        scaled.ScaleByDouble(3.0, 3.0, 1.0, 1);
        Assert.Equal(new Point(40.0, 30.0), MatrixUtils.TransformPoint(scaled, new Point(10.0, 10.0)));
    }

    [Fact]
    public void Invert_ReturnsTheDeterminantAndLeavesSingularMatricesAlone()
    {
        Matrix4 scale = Matrix4.Diagonal3Values(2.0, 4.0, 1.0);
        Matrix4 inverted = Matrix4.Copy(scale);
        double determinant = inverted.Invert();

        Assert.Equal(8.0, determinant, precision: 10);
        Assert.Equal(new Point(5.0, 5.0), MatrixUtils.TransformPoint(inverted, new Point(10.0, 20.0)));

        Matrix4 singular = MatrixUtils.ForceToPoint(new Point(20.0, -30.0));
        Matrix4 copy = Matrix4.Copy(singular);
        Assert.Equal(0.0, copy.Invert());
        Assert.Equal(singular, copy);
        Assert.Null(Matrix4.TryInvert(singular));
    }

    [Fact]
    public void RotateZ_MatchesTheRotationFactory()
    {
        Matrix4 rotated = Matrix4.Identity();
        rotated.RotateZ(0.7);

        Assert.Equal(Matrix4.RotationZ(0.7), rotated);
    }

    [Fact]
    public void RemovePerspectiveTransform_ResetsTheZRowAndColumn()
    {
        Matrix4 perspective = MatrixUtils.CreateCylindricalProjectionTransform(radius: 100.0, angle: 0.5);
        Matrix4 flattened = Plumix.Gestures.PointerEventUtils.RemovePerspectiveTransform(perspective);

        Assert.Equal(0.0, flattened.Storage[2]);
        Assert.Equal(0.0, flattened.Storage[6]);
        Assert.Equal(1.0, flattened.Storage[10]);
        Assert.Equal(0.0, flattened.Storage[14]);
        Assert.Equal(0.0, flattened.Storage[8]);
        Assert.Equal(0.0, flattened.Storage[9]);
        Assert.Equal(0.0, flattened.Storage[11]);
    }

    [Fact]
    public void ToAvaloniaMatrix_KeepsThePerspectiveRow()
    {
        Matrix4 perspective = Matrix4.Identity();
        perspective.SetEntry(3, 2, 0.001);
        perspective.RotateY(0.4);
        Matrix hostMatrix = perspective.ToAvaloniaMatrix();

        Assert.Equal(perspective.Storage[0], hostMatrix.M11);
        Assert.Equal(perspective.Storage[1], hostMatrix.M12);
        Assert.Equal(perspective.Storage[3], hostMatrix.M13);
        Assert.Equal(perspective.Storage[4], hostMatrix.M21);
        Assert.Equal(perspective.Storage[7], hostMatrix.M23);
        Assert.Equal(perspective.Storage[15], hostMatrix.M33);

        var probe = new Point(37.0, -19.0);
        Point expected = MatrixUtils.TransformPoint(perspective, probe);
        Point actual = hostMatrix.Transform(probe);
        Assert.Equal(expected.X, actual.X, precision: 6);
        Assert.Equal(expected.Y, actual.Y, precision: 6);
    }
}
