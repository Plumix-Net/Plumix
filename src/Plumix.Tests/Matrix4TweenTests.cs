using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/implicit_animations.dart (Matrix4Tween)

namespace Plumix.Tests;

public sealed class Matrix4TweenTests
{
    [Fact]
    public void Lerp_InterpolatesTranslationAndScale()
    {
        Matrix4 begin = Matrix4.Identity();
        Matrix4 end = Matrix4.Copy(begin);
        end.TranslateByDouble(6.0, -8.0, 0.0, 1);
        end.ScaleByDouble(0.5, 1.0, 5.0, 1);
        var tween = new Matrix4Tween(begin, end);

        Assert.Equal(begin, tween.Lerp(begin, end, 0.0));
        Assert.Equal(end, tween.Lerp(begin, end, 1.0));

        Matrix4 expectedHalf = Matrix4.Identity();
        expectedHalf.TranslateByDouble(3.0, -4.0, 0.0, 1);
        expectedHalf.ScaleByDouble(0.75, 1.0, 3.0, 1);
        Assert.Equal(expectedHalf, tween.Lerp(begin, end, 0.5));
    }

    [Fact]
    public void Lerp_InterpolatesRotation()
    {
        Matrix4 begin = Matrix4.Identity();
        Matrix4 end = Matrix4.Copy(begin);
        end.RotateZ(1.0);
        var tween = new Matrix4Tween(begin, end);

        Matrix4 half = tween.Lerp(begin, end, 0.5);
        Matrix4 expected = Matrix4.Identity();
        expected.RotateZ(0.5);
        for (int index = 0; index < 16; index++)
        {
            Assert.Equal(expected.Storage[index], half.Storage[index], precision: 8);
        }
    }

    [Fact]
    public void Lerp_ControlTest()
    {
        Matrix4 begin = Matrix4.TranslationValues(10.0, 20.0, 30.0);
        Matrix4 end = Matrix4.TranslationValues(14.0, 24.0, 34.0);
        var tween = new Matrix4Tween(begin, end);

        Matrix4 quarter = tween.Lerp(begin, end, 0.25);
        Assert.Equal(11.0, quarter.GetTranslation().X, precision: 10);
        Assert.Equal(21.0, quarter.GetTranslation().Y, precision: 10);
        Assert.Equal(31.0, quarter.GetTranslation().Z, precision: 10);
    }
}
