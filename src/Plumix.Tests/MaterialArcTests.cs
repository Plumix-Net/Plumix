using System;
using Avalonia;
using Plumix.Material;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Ports material_ui/test/arc_test.dart.
public sealed class MaterialArcTests
{
    [Fact]
    public void MaterialPointArcTween_ControlTest()
    {
        var a = new MaterialPointArcTween(begin: new Offset(0, 0), end: new Offset(0.0, 10.0));
        var b = new MaterialPointArcTween(begin: new Offset(0, 0), end: new Offset(0.0, 10.0));
        Assert.Equal(a.ToString(), b.ToString());
        Assert.Contains("MaterialPointArcTween(", a.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("\n", a.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MaterialRectArcTween_ControlTest()
    {
        var a = new MaterialRectArcTween(
            begin: DartGeometry.RectFromLTWH(0.0, 0.0, 10.0, 10.0),
            end: DartGeometry.RectFromLTWH(0.0, 10.0, 10.0, 10.0));
        var b = new MaterialRectArcTween(
            begin: DartGeometry.RectFromLTWH(0.0, 0.0, 10.0, 10.0),
            end: DartGeometry.RectFromLTWH(0.0, 10.0, 10.0, 10.0));
        Assert.Equal(a.ToString(), b.ToString());
        Assert.Contains("MaterialRectArcTween(", a.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("\n", a.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void OnAxis_MaterialPointArcTween_LerpsLinearly()
    {
        var tween = new MaterialPointArcTween(begin: new Offset(0, 0), end: new Offset(0.0, 10.0));
        Assert.Equal(new Offset(0.0, 5.0), tween.Transform(0.5));
        Assert.Null(tween.Center);
        Assert.Null(tween.Radius);
        Assert.Null(tween.BeginAngle);

        tween = new MaterialPointArcTween(begin: new Offset(0, 0), end: new Offset(10.0, 0.0));
        Assert.Equal(new Offset(5.0, 0.0), tween.Transform(0.5));
    }

    [Fact]
    public void OnAxis_MaterialRectArcTween_LerpsLinearly()
    {
        var tween = new MaterialRectArcTween(
            begin: DartGeometry.RectFromLTWH(0.0, 0.0, 10.0, 10.0),
            end: DartGeometry.RectFromLTWH(0.0, 10.0, 10.0, 10.0));
        Assert.Equal(DartGeometry.RectFromLTWH(0.0, 5.0, 10.0, 10.0), tween.Transform(0.5));

        tween = new MaterialRectArcTween(
            begin: DartGeometry.RectFromLTWH(0.0, 0.0, 10.0, 10.0),
            end: DartGeometry.RectFromLTWH(10.0, 0.0, 10.0, 10.0));
        Assert.Equal(DartGeometry.RectFromLTWH(5.0, 0.0, 10.0, 10.0), tween.Transform(0.5));
    }

    [Fact]
    public void MaterialPointArcTween_FollowsCircularArc()
    {
        var begin = new Offset(180.0, 110.0);
        var end = new Offset(37.0, 250.0);

        var tween = new MaterialPointArcTween(begin: begin, end: end);
        Assert.Equal(begin, tween.Transform(0.0));
        AssertWithin(new Offset(126.0, 120.0), tween.Transform(0.25), 2.0);
        AssertWithin(new Offset(48.0, 196.0), tween.Transform(0.75), 2.0);
        Assert.Equal(end, tween.Transform(1.0));

        tween = new MaterialPointArcTween(begin: end, end: begin);
        Assert.Equal(end, tween.Transform(0.0));
        AssertWithin(new Offset(91.0, 239.0), tween.Transform(0.25), 2.0);
        AssertWithin(new Offset(168.3, 163.8), tween.Transform(0.75), 2.0);
        Assert.Equal(begin, tween.Transform(1.0));
    }

    [Fact]
    public void MaterialRectArcTween_MovesCornersAlongArcs()
    {
        var begin = DartGeometry.RectFromCorners(new Offset(180.0, 100.0), new Offset(330.0, 200.0));
        var end = DartGeometry.RectFromCorners(new Offset(32.0, 275.0), new Offset(132.0, 425.0));

        var tween = new MaterialRectArcTween(begin: begin, end: end);
        Assert.Equal(begin, tween.Transform(0.0));
        AssertSameRect(DartGeometry.RectFromCorners(new Offset(120.0, 113.0), new Offset(259.0, 237.0)), tween.Transform(0.25)!.Value);
        AssertSameRect(DartGeometry.RectFromCorners(new Offset(42.3, 206.5), new Offset(153.5, 354.7)), tween.Transform(0.75)!.Value);
        Assert.Equal(end, tween.Transform(1.0));

        tween = new MaterialRectArcTween(begin: end, end: begin);
        Assert.Equal(end, tween.Transform(0.0));
        AssertSameRect(DartGeometry.RectFromCorners(new Offset(92.0, 262.0), new Offset(203.0, 388.0)), tween.Transform(0.25)!.Value);
        AssertSameRect(DartGeometry.RectFromCorners(new Offset(169.7, 168.5), new Offset(308.5, 270.3)), tween.Transform(0.75)!.Value);
        Assert.Equal(begin, tween.Transform(1.0));
    }

    [Fact]
    public void MaterialRectArcTween_RecomputesArcsWhenEndpointsChange()
    {
        var tween = new MaterialRectArcTween(
            begin: DartGeometry.RectFromLTWH(0.0, 0.0, 10.0, 10.0),
            end: DartGeometry.RectFromLTWH(0.0, 10.0, 10.0, 10.0));
        Assert.Equal(DartGeometry.RectFromLTWH(0.0, 5.0, 10.0, 10.0), tween.Transform(0.5));

        tween.End = DartGeometry.RectFromLTWH(0.0, 20.0, 10.0, 10.0);
        Assert.Equal(DartGeometry.RectFromLTWH(0.0, 10.0, 10.0, 10.0), tween.Transform(0.5));
    }

    [Fact]
    public void MaterialRectCenterArcTween_MovesCenterAlongArcAndLerpsSize()
    {
        var begin = DartGeometry.RectFromLTWH(0.0, 0.0, 10.0, 10.0);
        var end = DartGeometry.RectFromLTWH(0.0, 100.0, 20.0, 20.0);
        var tween = new MaterialRectCenterArcTween(begin: begin, end: end);

        Assert.Equal(begin, tween.Transform(0.0));
        Assert.Equal(end, tween.Transform(1.0));

        // Centres are on-axis (dx delta is 5 <= 2? no: 5 > 2, dy delta 105 > 2), so the centre arcs.
        Assert.NotNull(tween.CenterArc);

        Rect mid = tween.Transform(0.5)!.Value;
        Assert.Equal(15.0, mid.Width, 6);
        Assert.Equal(15.0, mid.Height, 6);
        Offset center = tween.CenterArc!.Transform(0.5);
        Assert.Equal(center.Dx - (mid.Width / 2.0), mid.Left, 6);
        Assert.Equal(center.Dy - (mid.Height / 2.0), mid.Top, 6);
    }

    [Fact]
    public void MaterialPointArcTween_ReturnsNullGeometryWithoutEndpoints()
    {
        var tween = new MaterialPointArcTween();
        Assert.Null(tween.Center);
        Assert.Null(tween.Radius);
        Assert.Null(tween.BeginAngle);
        Assert.Null(tween.EndAngle);
    }

    [Fact]
    public void MaterialApp_HeroController_UsesMaterialRectArcTween()
    {
        HeroController controller = MaterialApp.CreateMaterialHeroController();

        Assert.NotNull(controller.CreateRectTween);
        Tween<Rect?> tween = controller.CreateRectTween!(
            DartGeometry.RectFromLTWH(0.0, 0.0, 10.0, 10.0),
            DartGeometry.RectFromLTWH(100.0, 200.0, 20.0, 20.0));

        var arcTween = Assert.IsType<MaterialRectArcTween>(tween);
        Assert.Equal(DartGeometry.RectFromLTWH(0.0, 0.0, 10.0, 10.0), arcTween.Transform(0.0));
        Assert.Equal(DartGeometry.RectFromLTWH(100.0, 200.0, 20.0, 20.0), arcTween.Transform(1.0));
        Assert.NotNull(arcTween.BeginArc);
        Assert.NotNull(arcTween.EndArc);
    }

    private static void AssertWithin(Offset expected, Offset actual, double distance)
    {
        double dx = actual.Dx - expected.Dx;
        double dy = actual.Dy - expected.Dy;
        double delta = Math.Sqrt((dx * dx) + (dy * dy));
        Assert.True(delta <= distance, $"Expected {actual} within {distance} of {expected} (was {delta}).");
    }

    private static void AssertSameRect(Rect expected, Rect actual)
    {
        Assert.True(
            Math.Abs(actual.Left - expected.Left) < 2.0
            && Math.Abs(actual.Top - expected.Top) < 2.0
            && Math.Abs(actual.Right - expected.Right) < 2.0
            && Math.Abs(actual.Bottom - expected.Bottom) < 2.0,
            $"Expected {actual} to match {expected} within 2.0 on every edge.");
    }
}
