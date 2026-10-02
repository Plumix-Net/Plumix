using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using BoxShadow = Plumix.Rendering.BoxShadow;
using Path = Plumix.UI.Path;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/painting/shape_decoration_test.dart
// Plus the `_ShapeDecorationPainter` contract from painting/shape_decoration.dart.
public sealed class ShapeDecorationDartParityTests
{
    private static readonly Color ColorR = new(0xffff0000);
    private static readonly Color ColorG = new(0xff00ff00);

    private static IReadOnlyList<CanvasCall> PaintDecoration(
        BoxPainter painter,
        Point offset,
        ImageConfiguration configuration)
    {
        var context = new TestRecordingPaintingContext();
        painter.Paint(context, offset, configuration);
        return context.Calls;
    }

    [Fact]
    public void Constructor_AndFromBoxDecoration()
    {
        Assert.Equal(new ShapeDecoration(new Plumix.Rendering.Border()), new ShapeDecoration(new Plumix.Rendering.Border()));
        Assert.Equal(
            new ShapeDecoration(new CircleBorder()),
            ShapeDecoration.FromBoxDecoration(new BoxDecoration(Shape: BoxShape.Circle)));
        Assert.Equal(
            new ShapeDecoration(new RoundedRectangleBorder(borderRadius: BorderRadiusDirectional.Circular(100.0))),
            ShapeDecoration.FromBoxDecoration(
                new BoxDecoration(BorderRadius: BorderRadiusDirectional.Circular(100.0))));
        Assert.Equal(
            new ShapeDecoration(new CircleBorder(new BorderSide(ColorG))),
            ShapeDecoration.FromBoxDecoration(new BoxDecoration(
                Shape: BoxShape.Circle,
                Border: Plumix.Rendering.Border.All(color: ColorG))));
        Assert.Equal(
            new ShapeDecoration(Plumix.Rendering.Border.All(color: ColorR)),
            ShapeDecoration.FromBoxDecoration(new BoxDecoration(Border: Plumix.Rendering.Border.All(color: ColorR))));
        var directional = new BorderDirectional(start: new BorderSide(new Color(0xFF000000)));
        Assert.Equal(
            new ShapeDecoration(directional),
            ShapeDecoration.FromBoxDecoration(new BoxDecoration(Border: directional)));
    }

    [DebugOnlyFact]
    public void Constructor_ColorAndGradient_Asserts()
    {
        var gradient = new LinearGradient([ColorR, ColorG]);
        Assert.Throws<AssertionError>(
            () => new ShapeDecoration(new Plumix.Rendering.Border(), Color: ColorR, Gradient: gradient));
    }

    [Fact]
    public void Lerp_IdenticalAB()
    {
        Assert.Null(ShapeDecoration.Lerp(null, null, 0));
        var shape = new ShapeDecoration(new CircleBorder());
        Assert.Same(shape, ShapeDecoration.Lerp(shape, shape, 0.5));
    }

    [Fact]
    public void Lerp_NullAB()
    {
        Decoration a = new ShapeDecoration(new CircleBorder());
        Decoration b = new ShapeDecoration(new RoundedRectangleBorder());
        Assert.Equal(a, Decoration.Lerp(a, null, 0.0));
        Assert.Equal(b, Decoration.Lerp(null, b, 0.0));
        Assert.Null(Decoration.Lerp(null, null, 0.0));
    }

    [Fact]
    public void Lerp_AndHitTest()
    {
        Decoration a = new ShapeDecoration(new CircleBorder());
        Decoration b = new ShapeDecoration(new RoundedRectangleBorder());
        Decoration c = new ShapeDecoration(new OvalBorder());
        Assert.Equal(a, Decoration.Lerp(a, b, 0.0));
        Assert.Equal(b, Decoration.Lerp(a, b, 1.0));
        Assert.Equal(a, Decoration.Lerp(a, c, 0.0));
        Assert.Equal(c, Decoration.Lerp(a, c, 1.0));
        Assert.Equal(b, Decoration.Lerp(b, c, 0.0));
        Assert.Equal(c, Decoration.Lerp(b, c, 1.0));
        var size = new Size(200.0, 100.0); // at t=0.5, width will be 150 (x=25 to x=175).
        Assert.False(a.HitTest(size, new Point(20.0, 50.0)));
        Assert.False(c.HitTest(size, new Point(50, 5.0)));
        Assert.False(c.HitTest(size, new Point(5, 30.0)));
        Assert.False(Decoration.Lerp(a, b, 0.1)!.HitTest(size, new Point(20.0, 50.0)));
        Assert.False(Decoration.Lerp(a, b, 0.5)!.HitTest(size, new Point(20.0, 50.0)));
        Assert.True(Decoration.Lerp(a, b, 0.9)!.HitTest(size, new Point(20.0, 50.0)));
        Assert.False(Decoration.Lerp(a, c, 0.1)!.HitTest(size, new Point(30.0, 50.0)));
        Assert.True(Decoration.Lerp(a, c, 0.5)!.HitTest(size, new Point(30.0, 50.0)));
        Assert.True(Decoration.Lerp(a, c, 0.9)!.HitTest(size, new Point(30.0, 50.0)));
        Assert.True(Decoration.Lerp(b, c, 0.1)!.HitTest(size, new Point(45.0, 10.0)));
        Assert.True(Decoration.Lerp(b, c, 0.5)!.HitTest(size, new Point(30.0, 10.0)));
        Assert.True(Decoration.Lerp(b, c, 0.9)!.HitTest(size, new Point(10.0, 30.0)));
        Assert.True(b.HitTest(size, new Point(20.0, 50.0)));
    }

    [Fact]
    public void Lerp_BetweenGradientAndColor_IsSmoothAndDoesNotThrow()
    {
        // Regression test for https://github.com/flutter/flutter/issues/93953
        var gradient = new LinearGradient([ColorR, ColorG]);
        var colorDecoration = new ShapeDecoration(new CircleBorder(), Color: ColorR);
        var gradientDecoration = new ShapeDecoration(new RoundedRectangleBorder(), Gradient: gradient);

        Assert.Equal(colorDecoration, ShapeDecoration.Lerp(colorDecoration, gradientDecoration, 0.0));
        Assert.Equal(gradientDecoration, ShapeDecoration.Lerp(colorDecoration, gradientDecoration, 1.0));
        foreach (double t in new[] { 0.1, 0.25, 0.49, 0.5, 0.51, 0.75, 0.9 })
        {
            ShapeDecoration forward = ShapeDecoration.Lerp(colorDecoration, gradientDecoration, t)!;
            Assert.Null(forward.Color);
            Assert.IsType<LinearGradient>(forward.Gradient);
            ShapeDecoration reverse = ShapeDecoration.Lerp(gradientDecoration, colorDecoration, t)!;
            Assert.Null(reverse.Color);
            Assert.IsType<LinearGradient>(reverse.Gradient);
        }

        var gradientNearColor = (LinearGradient)ShapeDecoration.Lerp(
            colorDecoration,
            gradientDecoration,
            0.001)!.Gradient!;
        Assert.Equal(2, gradientNearColor.Colors.Count);
        foreach (Color color in gradientNearColor.Colors)
        {
            Assert.InRange(color.R, ColorR.R - 0.05, ColorR.R + 0.05);
            Assert.InRange(color.G, ColorR.G - 0.05, ColorR.G + 0.05);
            Assert.InRange(color.B, ColorR.B - 0.05, ColorR.B + 0.05);
        }
    }

    [Fact]
    public void GetClipPath()
    {
        var decoration = new ShapeDecoration(new CircleBorder());
        Path clipPath = decoration.GetClipPath(new Rect(0.0, 0.0, 100.0, 20.0), TextDirection.Ltr);
        Assert.True(clipPath.Contains(new Point(50.0, 10.0)));
        Assert.False(clipPath.Contains(new Point(1.0, 1.0)));
        Assert.False(clipPath.Contains(new Point(30.0, 10.0)));
        Assert.False(clipPath.Contains(new Point(99.0, 19.0)));
    }

    [Fact]
    public void GetClipPath_ForOval()
    {
        var decoration = new ShapeDecoration(new OvalBorder());
        Path clipPath = decoration.GetClipPath(new Rect(0.0, 0.0, 100.0, 50.0), TextDirection.Ltr);
        Assert.True(clipPath.Contains(new Point(50.0, 10.0)));
        Assert.False(clipPath.Contains(new Point(1.0, 1.0)));
        Assert.False(clipPath.Contains(new Point(15.0, 1.0)));
        Assert.False(clipPath.Contains(new Point(99.0, 19.0)));
    }

    [Fact]
    public void Painter_PreferPaintInteriorShape_PaintsShadowsThroughPaintInterior()
    {
        var shadow = new BoxShadow(
            color: new Color(0x80000000),
            offset: new Point(0.0, 2.0),
            blurRadius: 4.0,
            spreadRadius: 1.0,
            blurStyle: BlurStyle.Inner);
        var decoration = new ShapeDecoration(
            new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(6.0)),
            Color: new Color(0xFF00FF00),
            Shadows: [shadow]);
        IReadOnlyList<CanvasCall> calls = PaintDecoration(
            decoration.CreateBoxPainter(),
            default,
            new ImageConfiguration(Size: new Size(40.0, 20.0)));

        Assert.Equal("drawRRect", calls[0].Method);
        Assert.Equal(BorderRadius.Circular(6.0).ToRRect(new Rect(-1.0, 1.0, 42.0, 22.0)), calls[0].RRect);
        Assert.Equal(MaskFilter.Blur(BlurStyle.Inner, shadow.BlurSigma), calls[0].MaskFilter);
        Assert.Equal("drawRRect", calls[1].Method);
        Assert.Equal(BorderRadius.Circular(6.0).ToRRect(new Rect(0.0, 0.0, 40.0, 20.0)), calls[1].RRect);
        Assert.Null(calls[1].MaskFilter);
    }

    [Fact]
    public void Painter_PathShape_PaintsShadowsAndInteriorAsPaths()
    {
        // Shapes without paintInterior (a star) cast shadows through their outer path, which Plumix
        // used to skip.
        var decoration = new ShapeDecoration(
            new StarBorder(),
            Color: new Color(0xFF0000FF),
            Shadows: [new BoxShadow(color: new Color(0x40000000), offset: new Point(3.0, 3.0), blurRadius: 2.0)]);
        IReadOnlyList<CanvasCall> calls = PaintDecoration(
            decoration.CreateBoxPainter(),
            new Point(10.0, 10.0),
            new ImageConfiguration(Size: new Size(50.0, 50.0)));

        CanvasCall[] paths = calls.Where(call => call.Method == "drawPath").ToArray();
        Assert.True(paths.Length >= 2);
        Assert.NotNull(paths[0].MaskFilter);
        Assert.Equal(new Color(0x40000000), paths[0].Color);
        Assert.Null(paths[1].MaskFilter);
        Assert.Equal(new Color(0xFF0000FF), paths[1].Color);
        Rect shadowBounds = paths[0].Path!.GetBounds();
        Rect interiorBounds = paths[1].Path!.GetBounds();
        Assert.Equal(interiorBounds.X + 3.0, shadowBounds.X, 6);
        Assert.Equal(interiorBounds.Y + 3.0, shadowBounds.Y, 6);
    }

    [Fact]
    public void Painter_OpaqueOutlinedBorderWithColor_DeflatesTheInterior()
    {
        var decoration = new ShapeDecoration(
            new RoundedRectangleBorder(new BorderSide(new Color(0xFF000000), 4.0)),
            Color: new Color(0xFF00FF00));
        IReadOnlyList<CanvasCall> calls = PaintDecoration(
            decoration.CreateBoxPainter(),
            default,
            new ImageConfiguration(Size: new Size(40.0, 20.0)));
        PaintAssert.Paints(calls, PaintPattern.Paints.Rect(rect: new Rect(2.0, 2.0, 36.0, 16.0)));

        var gradientDecoration = new ShapeDecoration(
            new RoundedRectangleBorder(new BorderSide(new Color(0xFF000000), 4.0)),
            Gradient: new LinearGradient([ColorR, ColorG]));
        IReadOnlyList<CanvasCall> gradientCalls = PaintDecoration(
            gradientDecoration.CreateBoxPainter(),
            default,
            new ImageConfiguration(Size: new Size(40.0, 20.0)));
        PaintAssert.Paints(gradientCalls, PaintPattern.Paints.Rect(rect: new Rect(0.0, 0.0, 40.0, 20.0)));
    }

    [Fact]
    public void Painter_ShadowsWithoutFill_PaintNoInterior()
    {
        var decoration = new ShapeDecoration(
            new CircleBorder(),
            Shadows: [new BoxShadow(color: new Color(0x40000000), blurRadius: 2.0)]);
        IReadOnlyList<CanvasCall> calls = PaintDecoration(
            decoration.CreateBoxPainter(),
            default,
            new ImageConfiguration(Size: new Size(20.0, 20.0)));
        CanvasCall shadow = Assert.Single(calls, call => call.Method == "drawCircle");
        Assert.NotNull(shadow.MaskFilter);
    }

    [DebugOnlyFact]
    public void Painter_DisabledOuterShadow_IsClippedOutOfTheShape()
    {
        bool previous = RenderingDebug.DisableShadows;
        RenderingDebug.DisableShadows = true;
        try
        {
            var decoration = new ShapeDecoration(
                new CircleBorder(),
                Shadows: [new BoxShadow(blurRadius: 4.0, blurStyle: BlurStyle.Outer)]);
            IReadOnlyList<CanvasCall> calls = PaintDecoration(
                decoration.CreateBoxPainter(),
                default,
                new ImageConfiguration(Size: new Size(20.0, 20.0)));

            PaintAssert.Paints(
                calls,
                PaintPattern.Paints.Save().ClipPath().Circle(x: 10.0, y: 10.0, radius: 10.0).Restore());
            CanvasCall clip = calls.Single(call => call.Method == "clipPath");
            Assert.False(clip.Path!.Contains(new Point(10.0, 10.0)));
            Assert.True(clip.Path.Contains(new Point(-5.0, -5.0)));
        }
        finally
        {
            RenderingDebug.DisableShadows = previous;
        }
    }
}
