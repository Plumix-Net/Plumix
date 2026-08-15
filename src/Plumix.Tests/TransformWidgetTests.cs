using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (Transform);
// flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderTransform)

namespace Plumix.Tests;

public sealed class TransformWidgetTests
{
    [Fact]
    public void Rotate_SnapsQuarterTurnsToExactMatrices()
    {
        Assert.Equal(Matrix4.Identity(), Transform.Rotate(0.0).Matrix);

        double[] quarter = Transform.Rotate(Math.PI / 2.0).Matrix.Storage;
        Assert.Equal(0.0, quarter[0]);
        Assert.Equal(1.0, quarter[1]);
        Assert.Equal(-1.0, quarter[4]);
        Assert.Equal(0.0, quarter[5]);

        double[] half = Transform.Rotate(Math.PI).Matrix.Storage;
        Assert.Equal(-1.0, half[0]);
        Assert.Equal(0.0, half[1]);
        Assert.Equal(0.0, half[4]);
        Assert.Equal(-1.0, half[5]);

        double[] threeQuarter = Transform.Rotate(3.0 * Math.PI / 2.0).Matrix.Storage;
        Assert.Equal(0.0, threeQuarter[0]);
        Assert.Equal(-1.0, threeQuarter[1]);
        Assert.Equal(1.0, threeQuarter[4]);
        Assert.Equal(0.0, threeQuarter[5]);

        // A non-cardinal angle keeps the exact trigonometric values.
        Assert.Equal(Math.Cos(0.3), Transform.Rotate(0.3).Matrix.Storage[0]);
    }

    [Fact]
    public void Rotate_DefaultsToCenterAlignmentAndRejectsNonFiniteAngles()
    {
        Assert.Equal(Alignment.Center, Transform.Rotate(0.5).Alignment);
        Assert.Null(Transform.Rotate(0.5).Origin);
        Assert.True(Transform.Rotate(0.5).TransformHitTests);
        Assert.Throws<ArgumentException>(() => Transform.Rotate(double.NaN));
        Assert.Throws<ArgumentException>(() => Transform.Rotate(double.PositiveInfinity));
    }

    [Fact]
    public void Scale_RequiresExactlyOneScaleForm()
    {
        Assert.Throws<ArgumentException>(() => Transform.Scale());
        Assert.Throws<ArgumentException>(() => Transform.Scale(scale: 2.0, scaleX: 3.0));
        Assert.Throws<ArgumentException>(() => Transform.Scale(scale: 2.0, scaleY: 3.0));

        Assert.Equal(Matrix4.Diagonal3Values(2.0, 2.0, 1.0), Transform.Scale(scale: 2.0).Matrix);
        Assert.Equal(Matrix4.Diagonal3Values(1.5, 1.0, 1.0), Transform.Scale(scaleX: 1.5).Matrix);
        Assert.Equal(Matrix4.Diagonal3Values(1.0, 1.2, 1.0), Transform.Scale(scaleY: 1.2).Matrix);
        Assert.Equal(Matrix4.Diagonal3Values(1.5, 1.2, 1.0), Transform.Scale(scaleX: 1.5, scaleY: 1.2).Matrix);
        Assert.Equal(Alignment.Center, Transform.Scale(scale: 2.0).Alignment);
    }

    [Fact]
    public void Translate_CarriesNoOriginOrAlignment()
    {
        Transform translate = Transform.Translate(new Point(100.0, 50.0));

        Assert.Equal(Matrix4.TranslationValues(100.0, 50.0, 0.0), translate.Matrix);
        Assert.Null(translate.Origin);
        Assert.Null(translate.Alignment);
    }

    [Fact]
    public void Flip_MirrorsAboutTheCenter()
    {
        Assert.Equal(Matrix4.Diagonal3Values(-1.0, 1.0, 1.0), Transform.Flip(flipX: true).Matrix);
        Assert.Equal(Matrix4.Diagonal3Values(1.0, -1.0, 1.0), Transform.Flip(flipY: true).Matrix);
        Assert.Equal(
            Matrix4.Diagonal3Values(-1.0, -1.0, 1.0),
            Transform.Flip(flipX: true, flipY: true).Matrix);
        Assert.Equal(Alignment.Center, Transform.Flip(flipX: true).Alignment);
    }

    [Fact]
    public void EffectiveTransform_ConjugatesByOriginThenAlignment()
    {
        RenderTransform plain = Laid(Matrix4.Diagonal3Values(0.5, 0.5, 1.0));
        Assert.Equal(Matrix4.Diagonal3Values(0.5, 0.5, 1.0), plain.EffectiveTransform);

        RenderTransform withOrigin = Laid(
            Matrix4.Diagonal3Values(0.5, 0.5, 1.0),
            origin: new Point(100.0, 50.0));
        Matrix4 expectedOrigin = Matrix4.TranslationValues(100.0, 50.0, 0.0);
        expectedOrigin.ScaleByDouble(0.5, 0.5, 1.0, 1);
        expectedOrigin.TranslateByDouble(-100.0, -50.0, 0, 1);
        Assert.Equal(expectedOrigin, withOrigin.EffectiveTransform);

        // `Alignment.centerRight` on a 100x100 box resolves to the same anchor as origin (100, 50).
        RenderTransform withAlignment = Laid(
            Matrix4.Diagonal3Values(0.5, 0.5, 1.0),
            alignment: Alignment.CenterRight);
        Assert.Equal(expectedOrigin, withAlignment.EffectiveTransform);

        // Both together: `origin(100, 0)` plus `centerLeft` is again the anchor (100, 50).
        RenderTransform both = Laid(
            Matrix4.Diagonal3Values(0.5, 0.5, 1.0),
            origin: new Point(100.0, 0.0),
            alignment: Alignment.CenterLeft);
        Assert.Equal(expectedOrigin, both.EffectiveTransform);
    }

    [Fact]
    public void ApplyPaintTransform_PostMultipliesTheEffectiveTransform()
    {
        RenderTransform transform = Laid(
            Matrix4.Diagonal3Values(0.5, 0.5, 1.0),
            alignment: Alignment.CenterRight);
        Matrix4 accumulated = Matrix4.Identity();

        transform.ApplyPaintTransform(transform.Child!, accumulated);

        Assert.Equal(transform.EffectiveTransform, accumulated);
        Assert.Equal(new Point(50.0, 25.0), MatrixUtils.TransformPoint(accumulated, new Point(0.0, 0.0)));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0.0)]
    public void PaintsChild_IsFalseForSingularAndNonFiniteTransforms(double scaleX)
    {
        Matrix4 matrix = Matrix4.Identity();
        matrix.Storage[0] = scaleX;
        RenderTransform transform = Laid(matrix);

        Assert.False(transform.PaintsChild(transform.Child!));
    }

    [Fact]
    public void PaintsChild_IsTrueForAnInvertibleTransform()
    {
        RenderTransform transform = Laid(Matrix4.Diagonal3Values(0.01, 0.01, 1.0));

        Assert.True(transform.PaintsChild(transform.Child!));
    }

    [Fact]
    public void HitTest_MapsThroughTheInverseAndRespectsTransformHitTests()
    {
        RenderTransform transform = Laid(
            Matrix4.Diagonal3Values(0.5, 0.5, 1.0),
            alignment: Alignment.CenterRight);

        // The child occupies global x in [50, 100], y in [25, 75] once scaled about the right edge.
        Assert.True(transform.HitTest(new BoxHitTestResult(), new Point(90.0, 50.0)));
        Assert.False(transform.HitTest(new BoxHitTestResult(), new Point(10.0, 10.0)));

        transform.TransformHitTests = false;
        Assert.True(transform.HitTest(new BoxHitTestResult(), new Point(10.0, 10.0)));
    }

    [Fact]
    public void HitTest_ReturnsFalseForANonInvertibleTransform()
    {
        RenderTransform transform = Laid(Matrix4.Diagonal3Values(1.0, 0.0, 1.0));

        Assert.False(transform.HitTest(new BoxHitTestResult(), new Point(50.0, 50.0)));
    }

    [Fact]
    public void HitTest_WorksThroughAPerspectiveTransform()
    {
        Matrix4 matrix = Matrix4.Identity();
        matrix.SetEntry(3, 2, 0.001);
        matrix.RotateY(0.2);
        RenderTransform transform = Laid(matrix, alignment: Alignment.Center);

        // The z row is removed before inverting, so the centre still maps onto itself.
        Assert.True(transform.HitTest(new BoxHitTestResult(), new Point(50.0, 50.0)));
    }

    [Fact]
    public void Mutators_PostMultiplyTheStoredTransform()
    {
        RenderTransform transform = Laid(Matrix4.Identity());

        transform.Translate(10.0, 20.0);
        Assert.Equal(new Point(10.0, 20.0), MatrixUtils.GetAsTranslation(transform.Transform));

        transform.Scale(2.0);
        Matrix4 expected = Matrix4.TranslationValues(10.0, 20.0, 0.0);
        expected.ScaleByDouble(2.0, 2.0, 2.0, 1);
        Assert.Equal(expected, transform.Transform);

        transform.SetIdentity();
        Assert.True(transform.Transform.IsIdentity());

        transform.RotateZ(0.4);
        Assert.Equal(Matrix4.RotationZ(0.4), transform.Transform);
    }

    [Fact]
    public void Transform_CopiesTheAssignedMatrixSoLaterMutationsDoNotLeak()
    {
        Matrix4 source = Matrix4.Identity();
        RenderTransform transform = Laid(source);

        source.TranslateByDouble(100.0, 0.0, 0.0, 1);

        Assert.True(transform.Transform.IsIdentity());
    }

    private static RenderTransform Laid(Matrix4 matrix, Point? origin = null, Alignment? alignment = null)
    {
        var child = new HitTestBox();
        var transform = new RenderTransform(
            matrix,
            alignment,
            child,
            filterQuality: null,
            origin: origin);
        transform.Layout(BoxConstraints.Tight(new Size(100, 100)));
        return transform;
    }

    private sealed class HitTestBox : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(new Size(100, 100));
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }
}
