using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using BoxShadow = Plumix.Rendering.BoxShadow;
using Path = Plumix.UI.Path;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/painting/box_decoration_test.dart
// Dart parity source: flutter/packages/flutter/test/painting/decoration_test.dart (BoxDecoration cases)
// Plus the `_BoxDecorationPainter` contract from painting/box_decoration.dart that those tests rely on.
public sealed class BoxDecorationDartParityTests
{
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
    public void Lerp_IdenticalAB()
    {
        Assert.Null(BoxDecoration.Lerp(null, null, 0));
        var decoration = new BoxDecoration();
        Assert.Same(decoration, BoxDecoration.Lerp(decoration, decoration, 0.5));
    }

    [Fact]
    public void BorderRadiusDirectional_ResolvesForPaintAndHitTest()
    {
        var decoration = new BoxDecoration(
            Color: new Color(0xFF000000),
            BorderRadius: BorderRadiusDirectional.Only(topStart: 100.0));
        BoxPainter painter = decoration.CreateBoxPainter();
        var size = new Size(1000.0, 1000.0);
        var rect = new Rect(size);

        PaintAssert.Paints(
            PaintDecoration(painter, default, new ImageConfiguration(Size: size, TextDirection: TextDirection.Rtl)),
            PaintPattern.Paints.RRect(rrect: RRect.FromRectAndCorners(rect, BorderRadius.Only(topRight: 100.0))));
        Assert.True(decoration.HitTest(size, new Point(10.0, 10.0), TextDirection.Rtl));
        Assert.False(decoration.HitTest(size, new Point(990.0, 10.0), TextDirection.Rtl));

        PaintAssert.Paints(
            PaintDecoration(painter, default, new ImageConfiguration(Size: size, TextDirection: TextDirection.Ltr)),
            PaintPattern.Paints.RRect(rrect: RRect.FromRectAndCorners(rect, BorderRadius.Only(topLeft: 100.0))));
        Assert.False(decoration.HitTest(size, new Point(10.0, 10.0), TextDirection.Ltr));
        Assert.True(decoration.HitTest(size, new Point(990.0, 10.0), TextDirection.Ltr));
    }

    [Fact]
    public void LinearGradientUsingAlignmentDirectional_PaintsRect()
    {
        var decoration = new BoxDecoration(
            Color: new Color(0xFF000000),
            Gradient: new LinearGradient(
                [new Color(0xFF000000), new Color(0xFFFFFFFF)],
                begin: AlignmentDirectional.CenterStart,
                end: AlignmentDirectional.BottomEnd));
        BoxPainter painter = decoration.CreateBoxPainter();
        var size = new Size(1000.0, 1000.0);

        PaintAssert.Paints(
            PaintDecoration(painter, default, new ImageConfiguration(Size: size, TextDirection: TextDirection.Rtl)),
            PaintPattern.Paints.Rect(rect: new Rect(size)));
    }

    [Fact]
    public void GetClipPath_WithBorderRadius()
    {
        var decoration = new BoxDecoration(BorderRadius: BorderRadius.All(Radius.Circular(10)));
        var rect = new Rect(0.0, 0.0, 100.0, 20.0);
        Path clipPath = decoration.GetClipPath(rect, TextDirection.Ltr);

        Assert.True(clipPath.Contains(new Point(30.0, 10.0)));
        Assert.True(clipPath.Contains(new Point(50.0, 10.0)));
        Assert.False(clipPath.Contains(new Point(1.0, 1.0)));
        Assert.False(clipPath.Contains(new Point(99.0, 19.0)));
    }

    [Fact]
    public void GetClipPath_WithShapeCircle()
    {
        var decoration = new BoxDecoration(Shape: BoxShape.Circle);
        var rect = new Rect(0.0, 0.0, 100.0, 20.0);
        Path clipPath = decoration.GetClipPath(rect, TextDirection.Ltr);

        Assert.True(clipPath.Contains(new Point(50.0, 0.0)));
        Assert.True(clipPath.Contains(new Point(40.0, 10.0)));
        Assert.False(clipPath.Contains(new Point(40.0, 0.0)));
        Assert.False(clipPath.Contains(new Point(10.0, 10.0)));
    }

    [Fact]
    public void DifferentBlendModes_AreNotEqual()
    {
        // Regression test for https://github.com/flutter/flutter/issues/100754.
        var one = new BoxDecoration(Color: new Color(0x00000000), BackgroundBlendMode: BlendMode.Color);
        var two = new BoxDecoration(Color: new Color(0x00000000), BackgroundBlendMode: BlendMode.Difference);
        Assert.False(one == two);
        Assert.NotEqual(one, two);
    }

    [Fact]
    public void Lerp_Shapes()
    {
        // We don't lerp the shape, we just switch from one to the other at t=0.5.
        var rectangle = new BoxDecoration();
        var circle = new BoxDecoration(Shape: BoxShape.Circle);
        Assert.Equal(rectangle, BoxDecoration.Lerp(rectangle, circle, -1.0));
        Assert.Equal(rectangle, BoxDecoration.Lerp(rectangle, circle, 0.0));
        Assert.Equal(rectangle, BoxDecoration.Lerp(rectangle, circle, 0.25));
        Assert.Equal(circle, BoxDecoration.Lerp(rectangle, circle, 0.75));
        Assert.Equal(circle, BoxDecoration.Lerp(rectangle, circle, 1.0));
        Assert.Equal(circle, BoxDecoration.Lerp(rectangle, circle, 2.0));
    }

    [Fact]
    public void Lerp_Gradients()
    {
        var gradient = new LinearGradient([new Color(0x00000000), new Color(0xFFFFFFFF)]);
        var empty = new BoxDecoration();
        var target = new BoxDecoration(Gradient: gradient);
        Assert.Equal(
            new BoxDecoration(Gradient: new LinearGradient([new Color(0x00000000), new Color(0x00FFFFFF)])),
            BoxDecoration.Lerp(empty, target, -1.0));
        Assert.Equal(empty, BoxDecoration.Lerp(empty, target, 0.0));
        Assert.Equal(
            new BoxDecoration(Gradient: new LinearGradient([new Color(0x00000000), new Color(0x33FFFFFF)])),
            BoxDecoration.Lerp(empty, target, 0.2));
        Assert.Equal(
            new BoxDecoration(Gradient: new LinearGradient([new Color(0x00000000), new Color(0x55FFFFFF)])),
            BoxDecoration.Lerp(empty, target, 1.0 / 3.0));
        Assert.Equal(target, BoxDecoration.Lerp(empty, target, 1.0));
        Assert.Equal(target, BoxDecoration.Lerp(empty, target, 2.0));
    }

    [Fact]
    public void BoxShadow_CopyWith()
    {
        Assert.NotEqual(new BoxShadow(), new BoxShadow(color: new Color(0xFF112233)));
        Assert.Equal(
            new BoxShadow(color: new Color(0xFF112233)),
            new BoxShadow().CopyWith(color: new Color(0xFF112233)));
        Assert.NotEqual(new BoxShadow(), new BoxShadow(offset: new Point(1.0, 2.0)));
        Assert.Equal(new BoxShadow(offset: new Point(1.0, 2.0)), new BoxShadow().CopyWith(offset: new Point(1.0, 2.0)));
        Assert.NotEqual(new BoxShadow(), new BoxShadow(blurRadius: 123.0));
        Assert.Equal(new BoxShadow(blurRadius: 123.0), new BoxShadow().CopyWith(blurRadius: 123.0));
        Assert.NotEqual(new BoxShadow(), new BoxShadow(spreadRadius: 123.0));
        Assert.Equal(new BoxShadow(spreadRadius: 123.0), new BoxShadow().CopyWith(spreadRadius: 123.0));
        Assert.NotEqual(new BoxShadow(), new BoxShadow(blurStyle: BlurStyle.Outer));
        Assert.Equal(
            new BoxShadow(blurStyle: BlurStyle.Outer),
            new BoxShadow().CopyWith(blurStyle: BlurStyle.Outer));
    }

    [Theory]
    [InlineData(BoxShape.Circle, false, true)]
    [InlineData(BoxShape.Rectangle, true, true)]
    [InlineData(BoxShape.Rectangle, false, false)]
    public void BackgroundImageClip_PrecedesDrawImageRect(BoxShape shape, bool withRadius, bool expectClip)
    {
        var decoration = new BoxDecoration(
            Shape: shape,
            BorderRadius: withRadius ? BorderRadius.All(Radius.Circular(16.0)) : null,
            Image: new DecorationImage(new SynchronousImageProvider(new FakeImage(new Size(100.0, 100.0)))));
        bool onChangedCalled = false;
        BoxPainter painter = decoration.CreateBoxPainter(() => onChangedCalled = true);
        var configuration = new ImageConfiguration(Size: new Size(100.0, 100.0));

        PaintDecoration(painter, default, configuration);
        IReadOnlyList<CanvasCall> calls = PaintDecoration(painter, default, configuration);

        Assert.False(onChangedCalled); // The synchronous provider has the image before the first paint.
        string[] commands = calls
            .Where(call => call.Method is "clipPath" or "drawImageRect")
            .Select(call => call.Method)
            .ToArray();
        Assert.Equal(expectClip ? ["clipPath", "drawImageRect"] : ["drawImageRect"], commands);
    }

    [Fact]
    public void Painter_PaintsShadowsBackgroundAndBorderInDartOrder()
    {
        var shadow = new BoxShadow(
            color: new Color(0x80000000),
            offset: new Point(2.0, 4.0),
            blurRadius: 6.0,
            spreadRadius: 1.0,
            blurStyle: BlurStyle.Solid);
        var decoration = new BoxDecoration(
            Color: new Color(0xFF00FF00),
            BorderRadius: BorderRadius.Circular(8.0),
            BoxShadows: [shadow],
            Border: Plumix.Rendering.Border.All(color: new Color(0xFF0000FF), width: 2.0));
        IReadOnlyList<CanvasCall> calls = PaintDecoration(
            decoration.CreateBoxPainter(),
            new Point(10.0, 20.0),
            new ImageConfiguration(Size: new Size(100.0, 50.0)));

        var rect = new Rect(10.0, 20.0, 100.0, 50.0);
        // _paintShadows: the rect shifted by the offset and inflated by the spread, with the shadow's paint.
        CanvasCall shadowCall = calls[0];
        Assert.Equal("drawRRect", shadowCall.Method);
        Assert.Equal(BorderRadius.Circular(8.0).ToRRect(new Rect(11.0, 23.0, 102.0, 52.0)), shadowCall.RRect);
        Assert.Equal(MaskFilter.Blur(BlurStyle.Solid, shadow.BlurSigma), shadowCall.MaskFilter);
        Assert.Equal(new Color(0x80000000), shadowCall.Color);
        // _paintBackgroundColor: an opaque solid Border reduces the rect by half its stroke inset.
        CanvasCall background = calls[1];
        Assert.Equal("drawRRect", background.Method);
        Assert.Equal(BorderRadius.Circular(8.0).ToRRect(new Rect(11.0, 21.0, 98.0, 48.0)), background.RRect);
        Assert.Null(background.MaskFilter);
        Assert.Equal(new Color(0xFF00FF00), background.Color);
    }

    [Fact]
    public void Painter_TranslucentBorder_DoesNotAdjustBackgroundRect()
    {
        var decoration = new BoxDecoration(
            Color: new Color(0xFF00FF00),
            Border: Plumix.Rendering.Border.All(color: new Color(0x800000FF), width: 4.0));
        IReadOnlyList<CanvasCall> calls = PaintDecoration(
            decoration.CreateBoxPainter(),
            default,
            new ImageConfiguration(Size: new Size(100.0, 50.0)));

        PaintAssert.Paints(calls, PaintPattern.Paints.Rect(rect: new Rect(0.0, 0.0, 100.0, 50.0)));
    }

    [Fact]
    public void Painter_BorderDirectional_AdjustsByResolvedSides()
    {
        var decoration = new BoxDecoration(
            Color: new Color(0xFF00FF00),
            Border: new BorderDirectional(start: new BorderSide(new Color(0xFF000000), 4.0)));
        var configuration = new ImageConfiguration(Size: new Size(100.0, 50.0), TextDirection: TextDirection.Rtl);
        IReadOnlyList<CanvasCall> rtl = PaintDecoration(decoration.CreateBoxPainter(), default, configuration);
        PaintAssert.Paints(rtl, PaintPattern.Paints.Rect(rect: new Rect(0.0, 0.0, 98.0, 50.0)));

        IReadOnlyList<CanvasCall> ltr = PaintDecoration(
            decoration.CreateBoxPainter(),
            default,
            configuration with { TextDirection = TextDirection.Ltr });
        PaintAssert.Paints(ltr, PaintPattern.Paints.Rect(rect: new Rect(2.0, 0.0, 98.0, 50.0)));
    }

    [Fact]
    public void Painter_Circle_DrawsCircleInscribedInShortestSide()
    {
        var decoration = new BoxDecoration(
            Color: new Color(0xFF123456),
            Shape: BoxShape.Circle,
            BoxShadows: [new BoxShadow(color: new Color(0x40000000), offset: new Point(0.0, 3.0), blurRadius: 4.0)]);
        IReadOnlyList<CanvasCall> calls = PaintDecoration(
            decoration.CreateBoxPainter(),
            new Point(10.0, 10.0),
            new ImageConfiguration(Size: new Size(100.0, 40.0)));

        PaintAssert.Paints(
            calls,
            PaintPattern.Paints
                .Circle(x: 60.0, y: 33.0, radius: 20.0, color: new Color(0x40000000))
                .Circle(x: 60.0, y: 30.0, radius: 20.0, color: new Color(0xFF123456)));
        Assert.NotNull(calls[0].MaskFilter);
        Assert.Null(calls[1].MaskFilter);
        Assert.DoesNotContain(calls, call => call.Method is "drawRRect" or "drawRect");
    }

    [Fact]
    public void Painter_ZeroBorderRadius_DrawsRect()
    {
        var decoration = new BoxDecoration(Color: new Color(0xFF123456), BorderRadius: BorderRadius.Zero);
        IReadOnlyList<CanvasCall> calls = PaintDecoration(
            decoration.CreateBoxPainter(),
            default,
            new ImageConfiguration(Size: new Size(10.0, 10.0)));
        PaintAssert.Paints(calls, PaintPattern.Paints.Rect(rect: new Rect(0.0, 0.0, 10.0, 10.0)));
    }

    [Fact]
    public void Painter_BackgroundBlendMode_ReachesThePaint()
    {
        var decoration = new BoxDecoration(Color: new Color(0xFF123456), BackgroundBlendMode: BlendMode.Multiply);
        IReadOnlyList<CanvasCall> calls = PaintDecoration(
            decoration.CreateBoxPainter(),
            default,
            new ImageConfiguration(Size: new Size(10.0, 10.0)));
        CanvasCall background = Assert.Single(calls);
        Assert.Equal(BlendMode.Multiply, background.BlendMode);

        IReadOnlyList<CanvasCall> plain = PaintDecoration(
            new BoxDecoration(Color: new Color(0xFF123456)).CreateBoxPainter(),
            default,
            new ImageConfiguration(Size: new Size(10.0, 10.0)));
        Assert.Equal(BlendMode.SourceOver, Assert.Single(plain).BlendMode);
    }

    [Fact]
    public void Painter_GradientShaderIsCachedPerRect()
    {
        var gradient = new LinearGradient([new Color(0xFF000000), new Color(0xFFFFFFFF)]);
        BoxPainter painter = new BoxDecoration(Gradient: gradient).CreateBoxPainter();
        var configuration = new ImageConfiguration(Size: new Size(10.0, 10.0));

        IBrush? first = Assert.Single(PaintDecoration(painter, default, configuration)).Brush;
        IBrush? same = Assert.Single(PaintDecoration(painter, default, configuration)).Brush;
        IBrush? moved = Assert.Single(PaintDecoration(painter, new Point(5.0, 0.0), configuration)).Brush;

        Assert.NotNull(first);
        Assert.Same(first, same);
        Assert.NotSame(first, moved);
    }

    [Fact]
    public void Painter_NoColorOrGradient_PaintsNoBackground()
    {
        var decoration = new BoxDecoration(BorderRadius: BorderRadius.Circular(4.0));
        Assert.Empty(PaintDecoration(
            decoration.CreateBoxPainter(),
            default,
            new ImageConfiguration(Size: new Size(10.0, 10.0))));
    }

    [DebugOnlyFact]
    public void Painter_DisabledOuterShadow_IsClippedToItsBounds()
    {
        bool previous = RenderingDebug.DisableShadows;
        RenderingDebug.DisableShadows = true;
        try
        {
            var decoration = new BoxDecoration(
                BoxShadows: [new BoxShadow(offset: new Point(1.0, 1.0), blurRadius: 4.0, blurStyle: BlurStyle.Outer)]);
            IReadOnlyList<CanvasCall> calls = PaintDecoration(
                decoration.CreateBoxPainter(),
                default,
                new ImageConfiguration(Size: new Size(10.0, 10.0)));

            PaintAssert.Paints(
                calls,
                PaintPattern.Paints
                    .Save()
                    .ClipRect(rect: new Rect(1.0, 1.0, 10.0, 10.0))
                    .Rect(rect: new Rect(1.0, 1.0, 10.0, 10.0), hasMaskFilter: false)
                    .Restore());
        }
        finally
        {
            RenderingDebug.DisableShadows = previous;
        }
    }

    [DebugOnlyFact]
    public void CircleWithBorderRadius_FailsDebugAssertIsValid()
    {
        var decoration = new BoxDecoration(Shape: BoxShape.Circle, BorderRadius: BorderRadius.Circular(4.0));
        AssertionError error = Assert.Throws<AssertionError>(() => decoration.DebugAssertIsValid());
        Assert.Contains("A circle cannot have a border radius", error.Message);
        Assert.True(new BoxDecoration(Shape: BoxShape.Circle).DebugAssertIsValid());
        Assert.Throws<AssertionError>(() => new Plumix.Widgets.Container(decoration: decoration));
    }

    [DebugOnlyFact]
    public void BackgroundBlendModeWithoutColorOrGradient_Asserts()
    {
        AssertionError error = Assert.Throws<AssertionError>(
            () => new BoxDecoration(BackgroundBlendMode: BlendMode.Multiply));
        Assert.Contains("backgroundBlendMode applies to BoxDecoration's background color", error.Message);
    }

    [Fact]
    public void ScaleAndLerp_DropBackgroundBlendMode_AsInDart()
    {
        var a = new BoxDecoration(Color: new Color(0xFF000000), BackgroundBlendMode: BlendMode.Multiply);
        var b = new BoxDecoration(Color: new Color(0xFFFFFFFF), BackgroundBlendMode: BlendMode.Multiply);
        Assert.Null(a.Scale(0.5).BackgroundBlendMode);
        Assert.Null(BoxDecoration.Lerp(a, b, 0.5)!.BackgroundBlendMode);
        Assert.Equal(Color.Lerp(null, new Color(0xFF000000), 0.5), a.Scale(0.5).Color);
    }

    [Fact]
    public void CopyWith_ReplacesOnlyTheGivenFields()
    {
        var original = new BoxDecoration(
            Color: new Color(0xFF000000),
            BorderRadius: BorderRadius.Circular(2.0),
            Shape: BoxShape.Rectangle);
        BoxDecoration copy = original.CopyWith(
            backgroundBlendMode: BlendMode.Screen,
            boxShadow: [new BoxShadow()]);

        Assert.Equal(original.Color, copy.Color);
        Assert.Equal(original.BorderRadius, copy.BorderRadius);
        Assert.Equal(BlendMode.Screen, copy.BackgroundBlendMode);
        Assert.Single(copy.BoxShadows!);
        Assert.Equal(original, original.CopyWith());
    }

    [Fact]
    public void IsComplex_IsTrueForAnyNonNullShadowList()
    {
        Assert.True(new BoxDecoration(BoxShadows: []).IsComplex);
        Assert.False(new BoxDecoration().IsComplex);
        Assert.True(new ShapeDecoration(new CircleBorder(), Shadows: []).IsComplex);
    }

    [Fact]
    public void Padding_IsTheBorderDimensions()
    {
        Assert.Equal(EdgeInsetsGeometry.Zero, new BoxDecoration().Padding);
        var border = Plumix.Rendering.Border.All(width: 3.0);
        Assert.Equal(border.Dimensions, new BoxDecoration(Border: border).Padding);
    }

    [Fact]
    public void Painter_ToString_DescribesTheDecoration()
    {
        var decoration = new BoxDecoration(Color: new Color(0xFF000000));
        Assert.StartsWith("BoxPainter for ", decoration.CreateBoxPainter().ToString());
    }

    private sealed class SynchronousImageProvider(IImage image) : ImageProvider<string>
    {
        public override ValueTask<string> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult("image");

        protected override ImageStreamCompleter LoadImage(string key) => new SynchronousImageCompleter(image, key);
    }

    private sealed class SynchronousImageCompleter : ImageStreamCompleter
    {
        public SynchronousImageCompleter(IImage image, string debugLabel)
        {
            DebugLabel = debugLabel;
            SetImage(new ImageInfo(image, debugLabel: debugLabel));
        }
    }

    private sealed class FakeImage(Size size) : IImage
    {
        public Size Size { get; } = size;

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
        }
    }
}
