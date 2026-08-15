using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using TileMode = Plumix.Rendering.TileMode;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/painting/gradient_test.dart
public sealed class GradientTests
{
    private static Color Argb(uint value) => Color.FromUInt32(value);

    [Fact]
    public void LinearGradient_ScaleTest()
    {
        var gradient = new LinearGradient(
            begin: Alignment.BottomRight,
            end: new Alignment(0.7, 1.0),
            colors: [Argb(0x00FFFFFF), Argb(0x11777777), Argb(0x44444444)]);

        LinearGradient? scaled = LinearGradient.Lerp(null, gradient, 0.25);

        Assert.NotNull(scaled);
        Assert.Equal((AlignmentGeometry)Alignment.BottomRight, scaled.Begin);
        Assert.Equal((AlignmentGeometry)new Alignment(0.7, 1.0), scaled.End);
        Assert.Equal([Argb(0x00FFFFFF), Argb(0x04777777), Argb(0x11444444)], scaled.Colors);
    }

    [Fact]
    public void LinearGradient_LerpTest()
    {
        var from = new LinearGradient(
            begin: Alignment.TopLeft,
            end: Alignment.BottomLeft,
            colors: [Argb(0x33333333), Argb(0x66666666)]);
        var to = new LinearGradient(
            begin: Alignment.TopRight,
            end: Alignment.TopLeft,
            colors: [Argb(0x44444444), Argb(0x88888888)]);

        LinearGradient? middle = LinearGradient.Lerp(from, to, 0.5);

        Assert.NotNull(middle);
        Assert.Equal((AlignmentGeometry)Alignment.TopCenter, middle.Begin);
        Assert.Equal((AlignmentGeometry)Alignment.CenterLeft, middle.End);
        Assert.Equal([Argb(0x3B3B3B3B), Argb(0x77777777)], middle.Colors);
        Assert.Equal([0.0, 1.0], middle.Stops);
    }

    [Fact]
    public void LinearGradient_Lerp_IdenticalEndpoints()
    {
        Assert.Null(LinearGradient.Lerp(null, null, 0.0));

        var gradient = new LinearGradient(colors: [Argb(0x33333333), Argb(0x66666666)]);
        Assert.Same(gradient, LinearGradient.Lerp(gradient, gradient, 0.5));
    }

    [Fact]
    public void LinearGradient_LerpTest_WithStops()
    {
        var from = new LinearGradient(
            colors: [Argb(0x33333333), Argb(0x66666666)],
            stops: [0.0, 0.5]);
        var to = new LinearGradient(
            colors: [Argb(0x44444444), Argb(0x88888888)],
            stops: [0.5, 1.0]);

        LinearGradient? middle = LinearGradient.Lerp(from, to, 0.5);

        Assert.NotNull(middle);
        Assert.Equal([Argb(0x3B3B3B3B), Argb(0x55555555), Argb(0x77777777)], middle.Colors);
        Assert.Equal([0.0, 0.5, 1.0], middle.Stops);
    }

    [Fact]
    public void LinearGradient_LerpTest_WithUnequalNumberOfColors()
    {
        var from = new LinearGradient(colors: [Argb(0x22222222), Argb(0x66666666)]);
        var to = new LinearGradient(colors: [Argb(0x44444444), Argb(0x66666666), Argb(0x88888888)]);

        LinearGradient? middle = LinearGradient.Lerp(from, to, 0.5);

        Assert.NotNull(middle);
        Assert.Equal([Argb(0x33333333), Argb(0x55555555), Argb(0x77777777)], middle.Colors);
        Assert.Equal([0.0, 0.5, 1.0], middle.Stops);
    }

    [Fact]
    public void LinearGradient_LerpTest_WithStopsAndUnequalNumberOfColors()
    {
        var from = new LinearGradient(
            colors: [Argb(0x33333333), Argb(0x66666666)],
            stops: [0.0, 0.5]);
        var to = new LinearGradient(
            colors: [Argb(0x44444444), Argb(0x48484848), Argb(0x88888888)],
            stops: [0.5, 0.7, 1.0]);

        LinearGradient? middle = LinearGradient.Lerp(from, to, 0.5);

        Assert.NotNull(middle);
        Assert.Equal(
            [Argb(0x3B3B3B3B), Argb(0x55555555), Argb(0x57575757), Argb(0x77777777)],
            middle.Colors);
        Assert.Equal([0.0, 0.5, 0.7, 1.0], middle.Stops);
    }

    [Fact]
    public void LinearGradient_LerpTest_WithTransforms()
    {
        var from = new LinearGradient(
            colors: [Argb(0x33333333), Argb(0x66666666)],
            transform: new GradientRotation(Math.PI / 4));
        var to = new LinearGradient(
            colors: [Argb(0x44444444), Argb(0x88888888)],
            transform: new GradientRotation(Math.PI / 2));

        Assert.Equal(from.Transform, LinearGradient.Lerp(from, to, 0.0)!.Transform);
        Assert.Equal(to.Transform, LinearGradient.Lerp(from, to, 0.5)!.Transform);
        Assert.Equal(to.Transform, LinearGradient.Lerp(from, to, 1.0)!.Transform);
    }

    [Fact]
    public void LinearGradient_ToStringAndEquality()
    {
        var gradient = new LinearGradient(
            begin: Alignment.TopLeft,
            end: Alignment.BottomLeft,
            colors: [Argb(0x33333333), Argb(0x66666666)],
            transform: new GradientRotation(1.6));

        Assert.Equal(
            "LinearGradient(begin: Alignment.topLeft, end: Alignment.bottomLeft, "
            + "colors: [Color(0x33333333), Color(0x66666666)], tileMode: TileMode.clamp, "
            + "transform: GradientRotation(radians: 1.6))",
            gradient.ToString());

        var same = new LinearGradient(
            begin: Alignment.TopLeft,
            end: Alignment.BottomLeft,
            colors: [Argb(0x33333333), Argb(0x66666666)],
            transform: new GradientRotation(1.6));
        var different = new LinearGradient(
            begin: Alignment.TopLeft,
            end: Alignment.BottomLeft,
            colors: [Argb(0x33333333), Argb(0x66666666)],
            transform: new GradientRotation(Math.PI / 2));

        Assert.Equal(gradient, same);
        Assert.Equal(gradient.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(gradient, different);
    }

    [Fact]
    public void LinearGradient_WithAlignmentDirectional_RequiresTextDirection()
    {
        var rect = new Rect(1.0, 2.0, 3.0, 4.0);
        var directional = new LinearGradient(
            begin: AlignmentDirectional.TopStart,
            colors: [Colors.Red, Colors.Blue]);

        Assert.Throws<ArgumentNullException>(() => directional.CreateShader(rect));
        Assert.NotNull(directional.CreateShader(rect, TextDirection.Rtl));
        Assert.NotNull(directional.CreateShader(rect, TextDirection.Ltr));

        var physical = new LinearGradient(begin: Alignment.TopLeft, colors: [Colors.Red, Colors.Blue]);
        Assert.NotNull(physical.CreateShader(rect));
    }

    [Fact]
    public void LinearGradient_WithOpacity_ReplacesAlphaAndPreservesTransform()
    {
        var gradient = new LinearGradient(
            colors: [Argb(0xFFFFFFFF), Argb(0xAF777777), Argb(0x44444444)],
            transform: new GradientRotation(1));

        LinearGradient faded = gradient.WithOpacity(0.5);

        Assert.Equal([Argb(0x80FFFFFF), Argb(0x80777777), Argb(0x80444444)], faded.Colors);
        Assert.Equal(new GradientRotation(1), faded.Transform);
    }

    [Fact]
    public void LinearGradient_FromColor_ReplacesColorsAndPreservesGeometry()
    {
        var gradient = new LinearGradient(
            begin: Alignment.BottomRight,
            end: Alignment.TopCenter,
            colors: [Colors.Red, Colors.Blue, Colors.Green],
            stops: [0.0, 0.3, 1.0],
            tileMode: TileMode.Mirror,
            transform: new GradientRotation(1));

        LinearGradient replaced = gradient.FromColor(Argb(0xFF00FF00));

        Assert.Equal([Argb(0xFF00FF00), Argb(0xFF00FF00), Argb(0xFF00FF00)], replaced.Colors);
        Assert.Equal((AlignmentGeometry)Alignment.BottomRight, replaced.Begin);
        Assert.Equal((AlignmentGeometry)Alignment.TopCenter, replaced.End);
        Assert.Equal([0.0, 0.3, 1.0], replaced.Stops);
        Assert.Equal(TileMode.Mirror, replaced.TileMode);
        Assert.Equal(new GradientRotation(1), replaced.Transform);
    }

    [Fact]
    public void LinearGradient_Scale_PreservesTransform()
    {
        var gradient = new LinearGradient(
            colors: [Argb(0xFFFF0000), Argb(0xFF00FF00), Argb(0xFF0000FF)],
            stops: [0.0, 0.5, 1.0],
            tileMode: TileMode.Decal,
            transform: new GradientRotation(Math.PI / 4));

        LinearGradient scaled = gradient.Scale(0.5);

        Assert.Equal([Argb(0x80FF0000), Argb(0x8000FF00), Argb(0x800000FF)], scaled.Colors);
        Assert.Equal([0.0, 0.5, 1.0], scaled.Stops);
        Assert.Equal(TileMode.Decal, scaled.TileMode);
        Assert.Equal(new GradientRotation(Math.PI / 4), scaled.Transform);
    }

    [Fact]
    public void RadialGradient_LerpTest()
    {
        var from = new RadialGradient(
            center: Alignment.TopLeft,
            radius: 20.0,
            colors: [Argb(0x33333333), Argb(0x66666666)]);
        var to = new RadialGradient(
            center: Alignment.TopRight,
            radius: 10.0,
            colors: [Argb(0x44444444), Argb(0x88888888)]);

        RadialGradient? middle = RadialGradient.Lerp(from, to, 0.5);

        Assert.NotNull(middle);
        Assert.Equal((AlignmentGeometry)Alignment.TopCenter, middle.Center);
        Assert.Equal(15.0, middle.Radius);
        Assert.Equal([Argb(0x3B3B3B3B), Argb(0x77777777)], middle.Colors);
        Assert.Equal([0.0, 1.0], middle.Stops);
        Assert.Null(middle.Focal);
    }

    [Fact]
    public void RadialGradient_LerpTest_WithFocal()
    {
        var from = new RadialGradient(
            center: Alignment.TopLeft,
            focal: Alignment.CenterLeft,
            radius: 20.0,
            focalRadius: 10.0,
            colors: [Argb(0x33333333), Argb(0x66666666)]);
        var to = new RadialGradient(
            center: Alignment.TopRight,
            focal: Alignment.CenterRight,
            radius: 10.0,
            focalRadius: 5.0,
            colors: [Argb(0x44444444), Argb(0x88888888)]);

        RadialGradient? middle = RadialGradient.Lerp(from, to, 0.5);

        Assert.NotNull(middle);
        Assert.Equal((AlignmentGeometry)Alignment.TopCenter, middle.Center);
        Assert.Equal((AlignmentGeometry)Alignment.Center, middle.Focal);
        Assert.Equal(15.0, middle.Radius);
        Assert.Equal(7.5, middle.FocalRadius);

        var withoutFocal = new RadialGradient(
            center: Alignment.TopRight,
            radius: 10.0,
            colors: [Argb(0x44444444), Argb(0x88888888)]);
        RadialGradient? mixed = RadialGradient.Lerp(from, withoutFocal, 0.5);

        Assert.NotNull(mixed);
        Assert.Equal((AlignmentGeometry)new Alignment(-0.5, 0.0), mixed.Focal);
        Assert.Equal(5.0, mixed.FocalRadius);
        Assert.Equal(15.0, mixed.Radius);
    }

    [Fact]
    public void RadialGradient_Lerp_IdenticalEndpointsAndStops()
    {
        Assert.Null(RadialGradient.Lerp(null, null, 0.0));

        var gradient = new RadialGradient(colors: [Argb(0x33333333), Argb(0x66666666)]);
        Assert.Same(gradient, RadialGradient.Lerp(gradient, gradient, 0.5));

        var from = new RadialGradient(colors: [Argb(0x33333333), Argb(0x66666666)], stops: [0.0, 0.5]);
        var to = new RadialGradient(colors: [Argb(0x44444444), Argb(0x88888888)], stops: [0.5, 1.0]);
        RadialGradient? middle = RadialGradient.Lerp(from, to, 0.5);

        Assert.NotNull(middle);
        Assert.Equal([Argb(0x3B3B3B3B), Argb(0x55555555), Argb(0x77777777)], middle.Colors);
        Assert.Equal([0.0, 0.5, 1.0], middle.Stops);
    }

    [Fact]
    public void RadialGradient_FromColor_PreservesGeometry()
    {
        var gradient = new RadialGradient(
            center: Alignment.TopLeft,
            radius: 0.25,
            colors: [Colors.Red, Colors.Blue],
            focal: Alignment.CenterRight,
            focalRadius: 0.1,
            tileMode: TileMode.Mirror,
            transform: new GradientRotation(1));

        RadialGradient replaced = gradient.FromColor(Argb(0xFF00FF00));

        Assert.Equal([Argb(0xFF00FF00), Argb(0xFF00FF00)], replaced.Colors);
        Assert.Equal((AlignmentGeometry)Alignment.TopLeft, replaced.Center);
        Assert.Equal(0.25, replaced.Radius);
        Assert.Equal((AlignmentGeometry)Alignment.CenterRight, replaced.Focal);
        Assert.Equal(0.1, replaced.FocalRadius);
        Assert.Equal(TileMode.Mirror, replaced.TileMode);
        Assert.Equal(new GradientRotation(1), replaced.Transform);
    }

    [Fact]
    public void RadialGradient_WithAlignmentDirectional_RequiresTextDirection()
    {
        var rect = new Rect(1.0, 2.0, 3.0, 4.0);
        var directional = new RadialGradient(
            center: AlignmentDirectional.TopStart,
            colors: [Colors.Red, Colors.Blue]);

        Assert.Throws<ArgumentNullException>(() => directional.CreateShader(rect));
        Assert.NotNull(directional.CreateShader(rect, TextDirection.Rtl));
    }

    [Fact]
    public void SweepGradient_LerpTest()
    {
        var from = new SweepGradient(
            center: Alignment.TopLeft,
            endAngle: Math.PI / 2,
            colors: [Argb(0x33333333), Argb(0x66666666)]);
        var to = new SweepGradient(
            center: Alignment.TopRight,
            startAngle: Math.PI / 2,
            endAngle: Math.PI,
            colors: [Argb(0x44444444), Argb(0x88888888)]);

        SweepGradient? middle = SweepGradient.Lerp(from, to, 0.5);

        Assert.NotNull(middle);
        Assert.Equal((AlignmentGeometry)Alignment.TopCenter, middle.Center);
        Assert.Equal(Math.PI / 4, middle.StartAngle);
        Assert.Equal(Math.PI * 3 / 4, middle.EndAngle);
        Assert.Equal([Argb(0x3B3B3B3B), Argb(0x77777777)], middle.Colors);
        Assert.Equal([0.0, 1.0], middle.Stops);
    }

    [Fact]
    public void SweepGradient_ScaleAndFromColor()
    {
        var gradient = new SweepGradient(
            center: Alignment.TopLeft,
            startAngle: 0.1,
            endAngle: 2.0,
            colors: [Argb(0xFF333333), Argb(0xFF666666)],
            tileMode: TileMode.Repeated,
            transform: new GradientRotation(1));

        SweepGradient scaled = gradient.Scale(0.5);
        Assert.Equal([Argb(0x80333333), Argb(0x80666666)], scaled.Colors);
        Assert.Equal(0.1, scaled.StartAngle);
        Assert.Equal(2.0, scaled.EndAngle);

        SweepGradient replaced = gradient.FromColor(Argb(0xFF00FF00));
        Assert.Equal([Argb(0xFF00FF00), Argb(0xFF00FF00)], replaced.Colors);
        Assert.Equal((AlignmentGeometry)Alignment.TopLeft, replaced.Center);
        Assert.Equal(TileMode.Repeated, replaced.TileMode);
        Assert.Equal(new GradientRotation(1), replaced.Transform);
    }

    [Fact]
    public void SweepGradient_Lerp_IdenticalEndpoints()
    {
        Assert.Null(SweepGradient.Lerp(null, null, 0.0));

        var gradient = new SweepGradient(colors: [Argb(0x33333333), Argb(0x66666666)]);
        Assert.Same(gradient, SweepGradient.Lerp(gradient, gradient, 0.5));
        Assert.Equal(Math.PI * 2, gradient.EndAngle);
    }

    [Fact]
    public void Gradient_Lerp_SameKindInterpolatesSymmetrically()
    {
        var from = new RadialGradient(
            center: Alignment.TopLeft,
            radius: 20.0,
            colors: [Argb(0x33333333), Argb(0x66666666)]);
        var to = new RadialGradient(
            center: Alignment.TopRight,
            radius: 10.0,
            colors: [Argb(0x44444444), Argb(0x88888888)]);

        var start = Assert.IsType<RadialGradient>(Gradient.Lerp(from, to, 0.0));
        Assert.Equal(from.Colors, start.Colors);
        Assert.Equal(from.Center, start.Center);
        Assert.Equal(from.Radius, start.Radius);

        var end = Assert.IsType<RadialGradient>(Gradient.Lerp(from, to, 1.0));
        Assert.Equal(to.Colors, end.Colors);
        Assert.Equal(to.Center, end.Center);
        Assert.Equal(to.Radius, end.Radius);

        var expected = new RadialGradient(
            center: Alignment.TopCenter,
            radius: 15.0,
            colors: [Argb(0x3B3B3B3B), Argb(0x77777777)],
            stops: [0.0, 1.0]);
        Assert.Equal(expected, Gradient.Lerp(from, to, 0.5));
        Assert.Equal(expected, Gradient.Lerp(to, from, 0.5));
    }

    [Fact]
    public void Gradient_Lerp_DifferentKindsCrossFade()
    {
        var linear = new LinearGradient(colors: [Argb(0x33333333), Argb(0x66666666)]);
        var radial = new RadialGradient(colors: [Argb(0x44444444), Argb(0x88888888)]);

        Assert.Equal(linear, Gradient.Lerp(linear, radial, 0.0));
        Assert.Equal(radial, Gradient.Lerp(linear, radial, 1.0));
        Assert.Equal(radial.Scale(0.0), Gradient.Lerp(linear, radial, 0.5));
    }

    [Fact]
    public void Gradient_CreateShader_HandlesMissingStopsAndReportsMismatchedStops()
    {
        var rect = new Rect(1.0, 2.0, 3.0, 4.0);
        var linear = new LinearGradient(colors: [Colors.Red, Colors.Green, Colors.Blue]);
        var radial = new RadialGradient(colors: [Colors.Red, Colors.Green, Colors.Blue]);

        Assert.NotNull(linear.CreateShader(rect));
        Assert.NotNull(radial.CreateShader(rect));

        var badLinear = new LinearGradient(
            colors: [Colors.Red, Colors.Green, Colors.Blue],
            stops: [0.0, 1.0]);
        var badRadial = new RadialGradient(
            colors: [Colors.Red, Colors.Green, Colors.Blue],
            stops: [0.0, 1.0]);

        Assert.Throws<ArgumentException>(() => badLinear.CreateShader(rect));
        Assert.Throws<ArgumentException>(() => badRadial.CreateShader(rect));
    }

    [Fact]
    public void Gradient_ImpliedStops_RequireTwoColors()
    {
        var single = new LinearGradient(colors: [Colors.Red]);
        Assert.Throws<ArgumentException>(() => single.CreateShader(new Rect(0, 0, 10, 10)));
    }

    [Fact]
    public void LinearGradient_CreateShader_MapsEndpointsAndTileMode()
    {
        var gradient = new LinearGradient(
            begin: Alignment.TopLeft,
            end: Alignment.BottomRight,
            colors: [Colors.Red, Colors.Blue],
            tileMode: TileMode.Repeated);

        var brush = Assert.IsType<LinearGradientBrush>(gradient.CreateShader(new Rect(0, 0, 100, 50)));

        Assert.Equal(new RelativePoint(0.0, 0.0, RelativeUnit.Relative), brush.StartPoint);
        Assert.Equal(new RelativePoint(1.0, 1.0, RelativeUnit.Relative), brush.EndPoint);
        Assert.Equal(GradientSpreadMethod.Repeat, brush.SpreadMethod);
        Assert.Equal(2, brush.GradientStops.Count);
        Assert.Equal(0.0, brush.GradientStops[0].Offset);
        Assert.Equal(1.0, brush.GradientStops[1].Offset);
    }

    [Fact]
    public void RadialGradient_CreateShader_UsesShortestSideRadiusAndFocalOrigin()
    {
        var gradient = new RadialGradient(
            center: Alignment.Center,
            radius: 0.5,
            colors: [Colors.Red, Colors.Blue],
            focal: Alignment.TopLeft);

        var brush = Assert.IsType<RadialGradientBrush>(gradient.CreateShader(new Rect(0, 0, 100, 40)));

        Assert.Equal(new RelativePoint(0.5, 0.5, RelativeUnit.Relative), brush.Center);
        Assert.Equal(new RelativePoint(0.0, 0.0, RelativeUnit.Relative), brush.GradientOrigin);
        Assert.Equal(new RelativeScalar(20.0, RelativeUnit.Absolute), brush.RadiusX);
        Assert.Equal(new RelativeScalar(20.0, RelativeUnit.Absolute), brush.RadiusY);
    }

    [Fact]
    public void SweepGradient_CreateShader_FoldsSectorIntoStopsAndAngle()
    {
        var gradient = new SweepGradient(
            colors: [Colors.Red, Colors.Blue],
            startAngle: Math.PI / 2,
            endAngle: Math.PI);

        var brush = Assert.IsType<ConicGradientBrush>(gradient.CreateShader(new Rect(0, 0, 60, 60)));

        Assert.Equal(180.0, brush.Angle, 6);
        Assert.Equal(new RelativePoint(0.5, 0.5, RelativeUnit.Relative), brush.Center);
        Assert.Equal(0.0, brush.GradientStops[0].Offset, 6);
        Assert.Equal(0.25, brush.GradientStops[1].Offset, 6);
    }

    [Fact]
    public void Gradient_CreateShader_RebasesTransformOntoTheRectangle()
    {
        var gradient = new LinearGradient(
            colors: [Colors.Red, Colors.Blue],
            transform: new GradientRotation(Math.PI / 2));

        var brush = Assert.IsType<LinearGradientBrush>(gradient.CreateShader(new Rect(10, 20, 40, 40)));
        var transform = Assert.IsType<MatrixTransform>(brush.Transform);

        // A quarter turn about the rectangle's own center maps its top-left corner onto the top-right.
        Point rotated = new Point(0, 0) * transform.Value;
        Assert.Equal(40.0, rotated.X, 6);
        Assert.Equal(0.0, rotated.Y, 6);
    }

    [Fact]
    public void BoxDecoration_Lerp_Gradients()
    {
        var from = new BoxDecoration();
        var to = new BoxDecoration(
            Gradient: new LinearGradient(colors: [Argb(0x00000000), Argb(0xFFFFFFFF)]));

        Assert.Equal(
            [Argb(0x00000000), Argb(0x00FFFFFF)],
            RequireLinear(BoxDecoration.Lerp(from, to, -1.0)).Colors);
        Assert.Null(((BoxDecoration)BoxDecoration.Lerp(from, to, 0.0)!).Gradient);
        Assert.Equal(
            [Argb(0x00000000), Argb(0x33FFFFFF)],
            RequireLinear(BoxDecoration.Lerp(from, to, 0.2)).Colors);
        Assert.Equal(
            [Argb(0x00000000), Argb(0x55FFFFFF)],
            RequireLinear(BoxDecoration.Lerp(from, to, 1.0 / 3.0)).Colors);
        Assert.Equal(
            [Argb(0x00000000), Argb(0xFFFFFFFF)],
            RequireLinear(BoxDecoration.Lerp(from, to, 1.0)).Colors);
        Assert.Equal(
            [Argb(0x00000000), Argb(0xFFFFFFFF)],
            RequireLinear(BoxDecoration.Lerp(from, to, 2.0)).Colors);
    }

    [Fact]
    public void ShapeDecoration_Lerp_BetweenGradientAndColorIsSmooth()
    {
        var colored = new ShapeDecoration(Shape: new CircleBorder(), Color: Colors.Red);
        var gradientDecoration = new ShapeDecoration(
            Shape: new CircleBorder(),
            Gradient: new LinearGradient(colors: [Colors.Blue, Colors.Green]));

        foreach (double t in new[] { 0.1, 0.25, 0.49, 0.5, 0.51, 0.75, 0.9 })
        {
            var forward = (ShapeDecoration)ShapeDecoration.Lerp(colored, gradientDecoration, t)!;
            Assert.Null(forward.Color);
            Assert.IsType<LinearGradient>(forward.Gradient);

            var backward = (ShapeDecoration)ShapeDecoration.Lerp(gradientDecoration, colored, t)!;
            Assert.Null(backward.Color);
            Assert.IsType<LinearGradient>(backward.Gradient);
        }

        var almostRed = (ShapeDecoration)ShapeDecoration.Lerp(colored, gradientDecoration, 0.001)!;
        LinearGradient gradient = Assert.IsType<LinearGradient>(almostRed.Gradient);
        foreach (Color color in gradient.Colors)
        {
            Assert.InRange(color.R, 240, 255);
            Assert.InRange(color.G, 0, 15);
            Assert.InRange(color.B, 0, 15);
        }
    }

    [Fact]
    public void ShapeDecoration_Lerp_UsesEndpointsWhenBothAreColors()
    {
        var from = new ShapeDecoration(Shape: new CircleBorder(), Color: Argb(0xFF000000));
        var to = new ShapeDecoration(Shape: new CircleBorder(), Color: Argb(0xFFFFFFFF));

        var middle = (ShapeDecoration)ShapeDecoration.Lerp(from, to, 0.5)!;

        Assert.Null(middle.Gradient);
        Assert.Equal(Argb(0xFF7F7F7F), middle.Color);
    }

    private static LinearGradient RequireLinear(Decoration? decoration)
    {
        var box = Assert.IsType<BoxDecoration>(decoration);
        return Assert.IsType<LinearGradient>(box.Gradient);
    }
}
