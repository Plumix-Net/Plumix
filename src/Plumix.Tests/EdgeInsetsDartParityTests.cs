using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/painting/edge_insets_test.dart
// Ports the tests that cover `EdgeInsets` itself, plus the `EdgeInsetsGeometry` toString and
// cross-type equality cases that `EdgeInsets` takes part in.
public sealed class EdgeInsetsDartParityTests
{
    [Fact]
    public void EdgeInsetsConstructors()
    {
        EdgeInsets ltrbLtr = EdgeInsets.FromLTRB(10, 20, 30, 40).Resolve(TextDirection.Ltr);
        Assert.Equal(10, ltrbLtr.Left);
        Assert.Equal(20, ltrbLtr.Top);
        Assert.Equal(30, ltrbLtr.Right);
        Assert.Equal(40, ltrbLtr.Bottom);
        EdgeInsets ltrbRtl = EdgeInsets.FromLTRB(10, 20, 30, 40).Resolve(TextDirection.Rtl);
        Assert.Equal(10, ltrbRtl.Left);
        Assert.Equal(20, ltrbRtl.Top);
        Assert.Equal(30, ltrbRtl.Right);
        Assert.Equal(40, ltrbRtl.Bottom);

        EdgeInsets all = EdgeInsets.All(10);
        Assert.Equal(EdgeInsets.FromLTRB(10, 10, 10, 10), all.Resolve(TextDirection.Ltr));
        Assert.Equal(EdgeInsets.FromLTRB(10, 10, 10, 10), all.Resolve(TextDirection.Rtl));

        EdgeInsets only = EdgeInsets.Only(left: 10, top: 20, right: 30, bottom: 40);
        Assert.Equal(EdgeInsets.FromLTRB(10, 20, 30, 40), only.Resolve(TextDirection.Ltr));
        Assert.Equal(EdgeInsets.FromLTRB(10, 20, 30, 40), only.Resolve(TextDirection.Rtl));

        EdgeInsets symmetric = EdgeInsets.Symmetric(horizontal: 10, vertical: 20);
        Assert.Equal(EdgeInsets.FromLTRB(10, 20, 10, 20), symmetric.Resolve(TextDirection.Ltr));
        Assert.Equal(EdgeInsets.FromLTRB(10, 20, 10, 20), symmetric.Resolve(TextDirection.Rtl));
    }

    [Fact]
    public void EdgeInsetsControlTest()
    {
        EdgeInsets insets = EdgeInsets.FromLTRB(5.0, 7.0, 11.0, 13.0);

        Assert.DoesNotContain('\n', insets.ToString());
        Assert.Equal(EdgeInsets.FromLTRB(5.0, 7.0, 11.0, 13.0).GetHashCode(), insets.GetHashCode());

        Assert.Equal(new Point(5.0, 7.0), insets.TopLeft);
        Assert.Equal(new Point(-11.0, 7.0), insets.TopRight);
        Assert.Equal(new Point(5.0, -13.0), insets.BottomLeft);
        Assert.Equal(new Point(-11.0, -13.0), insets.BottomRight);

        Assert.Equal(new Size(16.0, 20.0), insets.CollapsedSize);
        Assert.Equal(EdgeInsets.FromLTRB(11.0, 13.0, 5.0, 7.0), insets.Flipped);

        Assert.Equal(16.0, insets.Along(Axis.Horizontal));
        Assert.Equal(20.0, insets.Along(Axis.Vertical));

        Assert.Equal(
            DartGeometry.RectFromLTRB(18.0, 25.0, 135.0, 156.0),
            insets.InflateRect(DartGeometry.RectFromLTRB(23.0, 32.0, 124.0, 143.0)));
        Assert.Equal(
            DartGeometry.RectFromLTRB(28.0, 39.0, 113.0, 130.0),
            insets.DeflateRect(DartGeometry.RectFromLTRB(23.0, 32.0, 124.0, 143.0)));

        Assert.Equal(new Size(116.0, 145.0), insets.InflateSize(new Size(100.0, 125.0)));
        Assert.Equal(new Size(84.0, 105.0), insets.DeflateSize(new Size(100.0, 125.0)));

        Assert.Equal(EdgeInsets.FromLTRB(2.5, 3.5, 5.5, 6.5), insets / 2.0);
        Assert.Equal(EdgeInsets.FromLTRB(2.0, 3.0, 5.0, 6.0), insets.TruncatingDivide(2.0));
        Assert.Equal(EdgeInsets.FromLTRB(0.0, 2.0, 1.0, 3.0), insets % 5.0);
    }

    [Fact]
    public void DeflateRect_DoesNotClampANegativeResult()
    {
        // Dart's `Rect.fromLTRB` keeps an inverted rect; Avalonia's `Rect.Deflate` would clamp it to zero.
        Rect deflated = EdgeInsets.All(10).DeflateRect(DartGeometry.RectFromLTRB(0, 0, 4, 4));

        Assert.Equal(DartGeometry.RectFromLTRB(10, 10, -6, -6), deflated);
        Assert.Equal(new Size(-16, -16), EdgeInsets.All(10).DeflateSize(new Size(4, 4)));
    }

    [Fact]
    public void EdgeInsetsLerp()
    {
        EdgeInsets a = EdgeInsets.All(10.0);
        EdgeInsets b = EdgeInsets.All(20.0);
        Assert.Equal(a * 1.25, EdgeInsets.Lerp(a, b, 0.25));
        Assert.Equal(b * 0.625, EdgeInsets.Lerp(a, b, 0.25));
        Assert.Equal(a + EdgeInsets.All(2.5), EdgeInsets.Lerp(a, b, 0.25));
        Assert.Equal(b - EdgeInsets.All(7.5), EdgeInsets.Lerp(a, b, 0.25));

        Assert.Null(EdgeInsets.Lerp(null, null, 0.25));
        Assert.Equal(b * 0.25, EdgeInsets.Lerp(null, b, 0.25));
        Assert.Equal(a * 0.75, EdgeInsets.Lerp(a, null, 0.25));
    }

    [Fact]
    public void EdgeInsetsLerpIdenticalAB()
    {
        Assert.Null(EdgeInsets.Lerp(null, null, 0));
        EdgeInsets insets = EdgeInsets.Zero;
        Assert.Equal(insets, EdgeInsets.Lerp(insets, insets, 0.5));
    }

    [Fact]
    public void EdgeInsetsResolve()
    {
        Assert.Equal(
            EdgeInsets.FromLTRB(10.0, 20.0, 30.0, 40.0),
            new EdgeInsetsDirectional(10.0, 20.0, 30.0, 40.0).Resolve(TextDirection.Ltr));
        Assert.Equal(
            EdgeInsets.FromLTRB(97.0, 98.0, 99.0, 96.0),
            new EdgeInsetsDirectional(99.0, 98.0, 97.0, 96.0).Resolve(TextDirection.Rtl));
        Assert.Equal(EdgeInsets.All(50.0), EdgeInsetsDirectional.All(50.0).Resolve(TextDirection.Ltr));
        Assert.Equal(EdgeInsets.All(50.0), EdgeInsetsDirectional.All(50.0).Resolve(TextDirection.Rtl));
        Assert.Equal(
            EdgeInsets.FromLTRB(963.25, 0.0, 0.0, 0.0),
            EdgeInsetsDirectional.Only(start: 963.25).Resolve(TextDirection.Ltr));
        Assert.Equal(
            EdgeInsets.FromLTRB(0.0, 963.25, 0.0, 0.0),
            EdgeInsetsDirectional.Only(top: 963.25).Resolve(TextDirection.Ltr));
        Assert.Equal(
            EdgeInsets.FromLTRB(0.0, 0.0, 963.25, 0.0),
            EdgeInsetsDirectional.Only(end: 963.25).Resolve(TextDirection.Ltr));
        Assert.Equal(
            EdgeInsets.FromLTRB(0.0, 0.0, 0.0, 963.25),
            EdgeInsetsDirectional.Only(bottom: 963.25).Resolve(TextDirection.Ltr));
        Assert.Equal(
            EdgeInsets.FromLTRB(0.0, 0.0, 963.25, 0.0),
            EdgeInsetsDirectional.Only(start: 963.25).Resolve(TextDirection.Rtl));
        Assert.Equal(
            EdgeInsets.FromLTRB(0.0, 963.25, 0.0, 0.0),
            EdgeInsetsDirectional.Only(top: 963.25).Resolve(TextDirection.Rtl));
        Assert.Equal(
            EdgeInsets.FromLTRB(963.25, 0.0, 0.0, 0.0),
            EdgeInsetsDirectional.Only(end: 963.25).Resolve(TextDirection.Rtl));
        Assert.Equal(
            EdgeInsets.FromLTRB(0.0, 0.0, 0.0, 963.25),
            EdgeInsetsDirectional.Only(bottom: 963.25).Resolve(TextDirection.Rtl));
        Assert.NotEqual(EdgeInsetsDirectional.Only(top: 1.0), EdgeInsetsDirectional.Only(bottom: 1.0));
    }

    [Fact]
    public void EdgeInsetsGeometryResolve_ReturnsEdgeInsets()
    {
        EdgeInsets ltr = EdgeInsetsGeometry.DirectionalOnly(start: 1, top: 2, end: 3, bottom: 4)
            .Resolve(TextDirection.Ltr);
        EdgeInsets rtl = EdgeInsetsGeometry.DirectionalOnly(start: 1, top: 2, end: 3, bottom: 4)
            .Resolve(TextDirection.Rtl);

        Assert.Equal(EdgeInsets.FromLTRB(1, 2, 3, 4), ltr);
        Assert.Equal(EdgeInsets.FromLTRB(3, 2, 1, 4), rtl);
        Assert.Equal(EdgeInsets.FromLTRB(1, 2, 3, 4), ((EdgeInsetsGeometry)ltr).Resolve(null));
        Assert.Equal(ltr, ltr.Resolve(null));
    }

    [Fact]
    public void EdgeInsetsEquality()
    {
        double five = 5.0;
        Assert.Equal(
            (EdgeInsetsGeometry)EdgeInsets.Only(top: five, bottom: 7.0),
            EdgeInsetsGeometry.DirectionalOnly(top: five, bottom: 7.0));
        Assert.Equal(EdgeInsets.Only(top: five, bottom: 7.0), EdgeInsets.Only(top: five, bottom: 7.0));
        Assert.NotEqual(
            (EdgeInsetsGeometry)EdgeInsets.Only(left: 5.0),
            EdgeInsetsGeometry.DirectionalOnly(start: 5.0));
        Assert.NotEqual(
            (EdgeInsetsGeometry)EdgeInsets.Only(right: 5.0),
            EdgeInsetsGeometry.DirectionalOnly(end: 5.0));
        Assert.Equal(EdgeInsets.Only(left: five), EdgeInsets.Only(left: five));
        Assert.Equal(EdgeInsets.Only(right: five), EdgeInsets.Only(right: five));
        Assert.Equal(
            EdgeInsetsGeometry.DirectionalOnly(top: 1.0).Add(EdgeInsets.Only(top: 2.0)),
            EdgeInsets.Only(top: 3.0));
    }

    [Fact]
    public void EdgeInsetsCopyWith()
    {
        EdgeInsets sourceEdgeInsets = EdgeInsets.Only(left: 1.0, top: 2.0, bottom: 3.0, right: 4.0);
        EdgeInsets copy = sourceEdgeInsets.CopyWith(left: 5.0, top: 6.0);
        Assert.Equal(EdgeInsets.Only(left: 5.0, top: 6.0, bottom: 3.0, right: 4.0), copy);
    }

    [Fact]
    public void EdgeInsetsOperators()
    {
        EdgeInsets a = EdgeInsets.FromLTRB(1.0, 2.0, 3.0, 5.0);
        Assert.Equal(EdgeInsets.FromLTRB(2.0, 4.0, 6.0, 10.0), a * 2.0);
        Assert.Equal(EdgeInsets.FromLTRB(0.5, 1.0, 1.5, 2.5), a / 2.0);
        Assert.Equal(EdgeInsets.FromLTRB(1.0, 0.0, 1.0, 1.0), a % 2.0);
        Assert.Equal(EdgeInsets.FromLTRB(0.0, 1.0, 1.0, 2.0), a.TruncatingDivide(2.0));
        Assert.Equal(a * 2.0, a + a);
        Assert.Equal(EdgeInsets.Zero, a - a);
        Assert.Equal((EdgeInsetsGeometry)(a * 2.0), a.Add(a));
        Assert.Equal((EdgeInsetsGeometry)EdgeInsets.Zero, a.Subtract(a));
        Assert.Equal(EdgeInsets.FromLTRB(-1.0, -2.0, -3.0, -5.0), -a);
    }

    [Fact]
    public void EdgeInsetsGeometryToString()
    {
        Assert.Equal("EdgeInsets.zero", EdgeInsets.Zero.ToString());
        Assert.Equal(
            "EdgeInsets.all(1.0)",
            EdgeInsets.Only(top: 1.01, left: 1.01, right: 1.01, bottom: 1.01).ToString());
        Assert.Equal("EdgeInsets(5.0, 7.0, 11.0, 13.0)", EdgeInsets.FromLTRB(5, 7, 11, 13).ToString());
        Assert.Equal(
            "EdgeInsetsDirectional(1.0, 1.0, 1.0, 1.0)",
            EdgeInsetsGeometry.DirectionalOnly(start: 1.01, end: 1.01, top: 1.01, bottom: 1.01).ToString());
        Assert.Equal(
            "EdgeInsetsDirectional(4.0, 3.0, 0.0, 0.0)",
            EdgeInsetsGeometry.DirectionalOnly(start: 4.0).Add(EdgeInsets.Only(top: 3.0)).ToString());
        Assert.Equal(
            "EdgeInsets(0.0, 4.0, 3.0, 0.0)",
            EdgeInsetsGeometry.DirectionalOnly(top: 4.0).Add(EdgeInsets.Only(right: 3.0)).ToString());
        Assert.Equal(
            "EdgeInsets(3.0, 0.0, 0.0, 0.0) + EdgeInsetsDirectional(4.0, 0.0, 0.0, 0.0)",
            EdgeInsetsGeometry.DirectionalOnly(start: 4.0).Add(EdgeInsets.Only(left: 3.0)).ToString());
    }

    [Fact]
    public void EdgeInsetsClamp_ClampsEachEdge()
    {
        EdgeInsets value = EdgeInsets.FromLTRB(-1, 5, 20, 3);

        EdgeInsetsGeometry clamped = value.Clamp(EdgeInsets.Zero, EdgeInsets.All(10));

        Assert.Equal((EdgeInsetsGeometry)EdgeInsets.FromLTRB(0, 5, 10, 3), clamped);
        Assert.True(EdgeInsets.All(1).IsNonNegative);
        Assert.False(EdgeInsets.Only(bottom: -1).IsNonNegative);
    }

    [Fact]
    public void FromViewPadding_DividesByTheDevicePixelRatio()
    {
        EdgeInsets insets = EdgeInsets.FromViewPadding(new ViewPadding(4, 8, 12, 16), 2.0);

        Assert.Equal(EdgeInsets.FromLTRB(2, 4, 6, 8), insets);
        Assert.Equal(
            "ViewPadding(left: 4.0, top: 8.0, right: 12.0, bottom: 16.0)",
            new ViewPadding(4, 8, 12, 16).ToString());
    }

    [Fact]
    public void Thickness_ConvertsImplicitlyAndBack()
    {
        EdgeInsets insets = new Thickness(1, 2, 3, 4);

        Assert.Equal(EdgeInsets.FromLTRB(1, 2, 3, 4), insets);
        Assert.Equal(new Thickness(1, 2, 3, 4), insets.ToThickness());
    }

    [Fact]
    public void InflateRRect_IncreasesTheSizeWhilePreservingRadiusProportions()
    {
        RRect original = RRect.FromLTRBAndCorners(
            10.0,
            20.0,
            30.0,
            60.0,
            topLeft: Radius.Circular(5.0),
            topRight: Radius.Circular(6.0),
            bottomRight: Radius.Circular(7.0),
            bottomLeft: Radius.Circular(8.0));

        EdgeInsets insets = EdgeInsets.FromLTRB(2.0, 3.0, 4.0, 5.0);
        RRect inflated = insets.InflateRRect(original);

        Assert.Equal(original.Left - insets.Left, inflated.Left);
        Assert.Equal(original.Top - insets.Top, inflated.Top);
        Assert.Equal(original.Right + insets.Right, inflated.Right);
        Assert.Equal(original.Bottom + insets.Bottom, inflated.Bottom);

        Assert.Equal(original.TopLeft.X + insets.Left, inflated.TopLeft.X);
        Assert.Equal(original.TopLeft.Y + insets.Top, inflated.TopLeft.Y);
        Assert.Equal(original.TopRight.X + insets.Right, inflated.TopRight.X);
        Assert.Equal(original.TopRight.Y + insets.Top, inflated.TopRight.Y);
        Assert.Equal(original.BottomRight.X + insets.Right, inflated.BottomRight.X);
        Assert.Equal(original.BottomRight.Y + insets.Bottom, inflated.BottomRight.Y);
        Assert.Equal(original.BottomLeft.X + insets.Left, inflated.BottomLeft.X);
        Assert.Equal(original.BottomLeft.Y + insets.Bottom, inflated.BottomLeft.Y);
    }

    [Fact]
    public void DeflateRRect_DecreasesTheSizeWhilePreservingRadiusProportions()
    {
        RRect original = RRect.FromLTRBAndCorners(
            10.0,
            20.0,
            30.0,
            60.0,
            topLeft: Radius.Circular(8.0),
            topRight: Radius.Circular(9.0),
            bottomRight: Radius.Circular(10.0),
            bottomLeft: Radius.Circular(11.0));

        EdgeInsets insets = EdgeInsets.FromLTRB(2.0, 3.0, 4.0, 5.0);
        RRect deflated = insets.DeflateRRect(original);

        Assert.Equal(original.Left + insets.Left, deflated.Left);
        Assert.Equal(original.Top + insets.Top, deflated.Top);
        Assert.Equal(original.Right - insets.Right, deflated.Right);
        Assert.Equal(original.Bottom - insets.Bottom, deflated.Bottom);

        Assert.Equal(original.TopLeft.X - insets.Left, deflated.TopLeft.X);
        Assert.Equal(original.TopLeft.Y - insets.Top, deflated.TopLeft.Y);
        Assert.Equal(original.TopRight.X - insets.Right, deflated.TopRight.X);
        Assert.Equal(original.TopRight.Y - insets.Top, deflated.TopRight.Y);
        Assert.Equal(original.BottomRight.X - insets.Right, deflated.BottomRight.X);
        Assert.Equal(original.BottomRight.Y - insets.Bottom, deflated.BottomRight.Y);
        Assert.Equal(original.BottomLeft.X - insets.Left, deflated.BottomLeft.X);
        Assert.Equal(original.BottomLeft.Y - insets.Bottom, deflated.BottomLeft.Y);
    }

    [Fact]
    public void InflateAndDeflateRRect_WithZeroInsetsReturnTheSameRRect()
    {
        RRect original = RRect.FromLTRBAndCorners(10.0, 20.0, 30.0, 60.0, topLeft: Radius.Circular(5.0));

        Assert.Equal(original, EdgeInsets.Zero.InflateRRect(original));
        Assert.Equal(original, EdgeInsets.Zero.DeflateRRect(original));
    }

    [Fact]
    public void DeflateRRect_ClampsRadiusToZeroWhenInsetsAreLargerThanRadius()
    {
        RRect original = RRect.FromLTRBAndCorners(
            10.0,
            20.0,
            30.0,
            60.0,
            topLeft: Radius.Circular(3.0),
            topRight: Radius.Circular(3.0),
            bottomRight: Radius.Circular(3.0),
            bottomLeft: Radius.Circular(3.0));

        RRect deflated = EdgeInsets.All(5.0).DeflateRRect(original);

        Assert.Equal(Radius.Zero, deflated.TopLeft);
        Assert.Equal(Radius.Zero, deflated.TopRight);
        Assert.Equal(Radius.Zero, deflated.BottomRight);
        Assert.Equal(Radius.Zero, deflated.BottomLeft);
    }

    [Fact]
    public void InflateRRectAndDeflateRRect_AreInverseOperations()
    {
        RRect original = RRect.FromLTRBAndCorners(
            10.0,
            20.0,
            30.0,
            60.0,
            topLeft: Radius.Circular(5.0),
            topRight: Radius.Circular(6.0),
            bottomRight: Radius.Circular(7.0),
            bottomLeft: Radius.Circular(8.0));
        EdgeInsets insets = EdgeInsets.FromLTRB(2.0, 3.0, 4.0, 5.0);

        RRect inflatedThenDeflated = insets.DeflateRRect(insets.InflateRRect(original));
        RRect deflatedThenInflated = insets.InflateRRect(insets.DeflateRRect(original));

        Assert.Equal(original.Left, inflatedThenDeflated.Left, 3);
        Assert.Equal(original.Top, inflatedThenDeflated.Top, 3);
        Assert.Equal(original.Right, inflatedThenDeflated.Right, 3);
        Assert.Equal(original.Bottom, inflatedThenDeflated.Bottom, 3);
        Assert.Equal(original.Left, deflatedThenInflated.Left, 3);
        Assert.Equal(original.Top, deflatedThenInflated.Top, 3);
        Assert.Equal(original.Right, deflatedThenInflated.Right, 3);
        Assert.Equal(original.Bottom, deflatedThenInflated.Bottom, 3);
    }
}
