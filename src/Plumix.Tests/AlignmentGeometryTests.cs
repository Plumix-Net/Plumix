using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

public sealed class AlignmentGeometryTests
{
    [Fact]
    public void AlignmentArithmeticAndPlacementMatchFlutter()
    {
        Alignment value = new(0.5, 0.25);
        Assert.Equal(new Alignment(0.25, 0.125), value / 2.0);
        Assert.Equal(Alignment.Center, value.TruncateDivide(2.0));
        Assert.Equal(value, value % 5.0);
        Assert.Equal(new Alignment(4.0, 7.0), new Alignment(1.0, 2.0) + new Alignment(3.0, 5.0));
        Assert.Equal(new Alignment(-2.0, -3.0), new Alignment(1.0, 2.0) - new Alignment(3.0, 5.0));
        Assert.Equal(new Alignment(1.0, 0.0), new Alignment(-1.0, -2.0) % 2.0);
        Assert.Equal(new Point(75.0, 62.5), value.AlongOffset(new Vector(100.0, 100.0)));
        Assert.Equal(new Point(75.0, 62.5), value.AlongOffset(new Point(100.0, 100.0)));
        Assert.Equal(new Point(75.0, 62.5), value.AlongSize(new Size(100.0, 100.0)));
        Assert.Equal(new Point(85.0, 82.5), value.WithinRect(new Rect(10.0, 20.0, 100.0, 100.0)));
    }

    [Fact]
    public void DirectionalArithmeticAndResolutionMatchFlutter()
    {
        AlignmentDirectional value = new(1.0, 2.0);
        Assert.Equal(new AlignmentDirectional(2.0, 4.0), value * 2.0);
        Assert.Equal(new AlignmentDirectional(0.5, 1.0), value / 2.0);
        Assert.Equal(AlignmentDirectional.CenterEnd, value % 2.0);
        Assert.Equal(AlignmentDirectional.BottomCenter, value.TruncateDivide(2.0));
        Assert.Equal(new AlignmentDirectional(4.0, 7.0), value + new AlignmentDirectional(3.0, 5.0));
        Assert.Equal(new AlignmentDirectional(-2.0, -3.0), value - new AlignmentDirectional(3.0, 5.0));
        Assert.Equal(new Alignment(1.0, 2.0), value.Resolve(TextDirection.Ltr));
        Assert.Equal(new Alignment(-1.0, 2.0), value.Resolve(TextDirection.Rtl));
        Assert.Throws<ArgumentNullException>(() => AlignmentDirectional.Center.Resolve(null));
    }

    [Fact]
    public void MixedAlignmentRetainsPhysicalAndDirectionalComponents()
    {
        AlignmentGeometry mixed = new Alignment(3.0, 5.0).Add(new AlignmentDirectional(1.0, 2.0));
        Assert.Equal(new Alignment(4.0, 7.0), mixed.Resolve(TextDirection.Ltr));
        Assert.Equal(new Alignment(2.0, 7.0), mixed.Resolve(TextDirection.Rtl));
        Assert.True(mixed.RequiresTextDirection);
        Assert.False(mixed.IsDirectional);
        Assert.Throws<ArgumentNullException>(() => mixed.Resolve(null));
        Assert.Equal(mixed, new AlignmentDirectional(1.0, 2.0).Add(new Alignment(3.0, 5.0)));
        Assert.Equal(new Alignment(6.0, 10.0).Add(new AlignmentDirectional(2.0, 4.0)), mixed * 2.0);
        Assert.Equal("Alignment(3.0, 7.0) + AlignmentDirectional.centerEnd", mixed.ToString());

        AlignmentGeometry zeroMixed = AlignmentGeometry.CenterStart + AlignmentGeometry.CenterLeft;
        Assert.True((zeroMixed * 0.0).RequiresTextDirection);
        Assert.Equal(AlignmentGeometry.Center, zeroMixed * 0.0);
    }

    [Fact]
    public void LerpCommutesWithResolutionAcrossKindsAndNulls()
    {
        AlignmentGeometry?[] values =
        [
            Alignment.TopLeft,
            AlignmentDirectional.TopEnd,
            new Alignment(0.25, 0.875),
            new Alignment(0.0625, 0.5625).Add(new AlignmentDirectional(0.1875, 0.6875)),
            null,
        ];

        foreach (TextDirection direction in Enum.GetValues<TextDirection>())
        {
            foreach (AlignmentGeometry? a in values)
            {
                foreach (AlignmentGeometry? b in values)
                {
                    Alignment resolvedA = a?.Resolve(direction) ?? Alignment.Center;
                    Alignment resolvedB = b?.Resolve(direction) ?? Alignment.Center;
                    foreach (double t in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
                    {
                        Alignment actual = AlignmentGeometry.Lerp(a, b, t)?.Resolve(direction) ?? Alignment.Center;
                        Alignment expected = Alignment.Lerp(resolvedA, resolvedB, t)!.Value;
                        Assert.Equal(expected.X, actual.X, 10);
                        Assert.Equal(expected.Y, actual.Y, 10);
                    }
                }
            }
        }

        Assert.Null(Alignment.Lerp(null, null, 0.25));
        Assert.Equal(new Alignment(0.0, -0.25), Alignment.Lerp(null, Alignment.TopCenter, 0.25));
        Assert.Equal(new Alignment(-0.75, -0.75), Alignment.Lerp(Alignment.TopLeft, null, 0.25));
        Assert.Null(AlignmentDirectional.Lerp(null, null, 0.25));
        Assert.Equal(new AlignmentDirectional(0.0, -0.25),
            AlignmentDirectional.Lerp(null, AlignmentDirectional.TopCenter, 0.25));
    }

    [Fact]
    public void StaticMembersAndValueEqualityMatchFlutter()
    {
        Assert.Equal((AlignmentGeometry)Alignment.TopLeft, AlignmentGeometry.TopLeft);
        Assert.Equal((AlignmentGeometry)Alignment.TopCenter, AlignmentGeometry.TopCenter);
        Assert.Equal((AlignmentGeometry)Alignment.TopRight, AlignmentGeometry.TopRight);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.TopStart, AlignmentGeometry.TopStart);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.TopEnd, AlignmentGeometry.TopEnd);
        Assert.Equal((AlignmentGeometry)Alignment.CenterLeft, AlignmentGeometry.CenterLeft);
        Assert.Equal((AlignmentGeometry)Alignment.Center, AlignmentGeometry.Center);
        Assert.Equal((AlignmentGeometry)Alignment.CenterRight, AlignmentGeometry.CenterRight);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.CenterStart, AlignmentGeometry.CenterStart);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.CenterEnd, AlignmentGeometry.CenterEnd);
        Assert.Equal((AlignmentGeometry)Alignment.BottomLeft, AlignmentGeometry.BottomLeft);
        Assert.Equal((AlignmentGeometry)Alignment.BottomCenter, AlignmentGeometry.BottomCenter);
        Assert.Equal((AlignmentGeometry)Alignment.BottomRight, AlignmentGeometry.BottomRight);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.BottomStart, AlignmentGeometry.BottomStart);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.BottomEnd, AlignmentGeometry.BottomEnd);
        Assert.Equal((AlignmentGeometry)new Alignment(4.0, 5.0), AlignmentGeometry.Xy(4.0, 5.0));
        Assert.Equal((AlignmentGeometry)new AlignmentDirectional(4.0, 5.0),
            AlignmentGeometry.Directional(4.0, 5.0));
        Assert.Equal((AlignmentGeometry)Alignment.Center, (AlignmentGeometry)AlignmentDirectional.Center);
        Assert.Equal(AlignmentGeometry.Center.GetHashCode(),
            ((AlignmentGeometry)AlignmentDirectional.Center).GetHashCode());
        Assert.Equal("Alignment.bottomLeft + AlignmentDirectional.centerEnd",
            Alignment.BottomLeft.Add(AlignmentDirectional.CenterEnd).ToString());
        Assert.Equal("AlignmentDirectional.center", ((AlignmentGeometry)AlignmentDirectional.Center).ToString());
    }

    [Fact]
    public void TextAlignVerticalMatchesFlutterDefaultsAndDiagnostics()
    {
        Assert.Equal(-1.0, TextAlignVertical.Top.Y);
        Assert.Equal(0.0, TextAlignVertical.Center.Y);
        Assert.Equal(1.0, TextAlignVertical.Bottom.Y);
        Assert.Equal("TextAlignVertical(y: 0.25)", new TextAlignVertical(0.25).ToString());
        if (Plumix.Foundation.Constants.KDebugMode)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TextAlignVertical(2.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TextAlignVertical(double.NaN));
        }
        else
        {
            Assert.Equal(2.0, new TextAlignVertical(2.0).Y);
        }
    }
}
