using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using SkiaSharp;
using Xunit;
using Path = Plumix.UI.Path;
using TileMode = Plumix.Rendering.TileMode;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/painting/gradient_test.dart
// Engine contracts: flutter/engine/src/flutter/testing/dart/gradient_test.dart.
public sealed class GradientShaderTests
{
    [Theory]
    [InlineData(TileMode.Clamp)]
    [InlineData(TileMode.Repeated)]
    [InlineData(TileMode.Mirror)]
    [InlineData(TileMode.Decal)]
    public void Linear_TilesOutsideTheShaderRectangleInAbsoluteCoordinates(TileMode mode)
    {
        var gradient = new LinearGradient(colors: [Colors.Red, Colors.Blue], tileMode: mode);
        IBrush shader = gradient.CreateShader(new Rect(20, 10, 40, 40));
        using RenderTargetBitmap image = Render(canvas => canvas.DrawRect(
            new Rect(0, 0, 100, 60), new Paint { Shader = shader }));

        AssertPixel(image, 40, 30, 124, 0, 131, 255);
        switch (mode)
        {
            case TileMode.Clamp:
                AssertPixel(image, 10, 30, 255, 0, 0, 255);
                AssertPixel(image, 70, 30, 0, 0, 255, 255);
                break;
            case TileMode.Decal:
                Assert.Equal(0, RasterBackend.PixelAt(image, 10, 30).A);
                Assert.Equal(0, RasterBackend.PixelAt(image, 70, 30).A);
                break;
            case TileMode.Repeated:
                AssertPixel(image, 10, 30, 61, 0, 194, 255);
                AssertPixel(image, 70, 30, 188, 0, 67, 255);
                break;
            case TileMode.Mirror:
                AssertPixel(image, 10, 30, 194, 0, 61, 255);
                AssertPixel(image, 70, 30, 67, 0, 188, 255);
                break;
        }
    }

    [Theory]
    [InlineData(TileMode.Clamp)]
    [InlineData(TileMode.Repeated)]
    [InlineData(TileMode.Mirror)]
    [InlineData(TileMode.Decal)]
    public void Sweep_UsesTheSectorForTilingBeforeAndAfterItsAngles(TileMode mode)
    {
        var gradient = new SweepGradient(colors: [Colors.Red, Colors.Blue],
            startAngle: Math.PI / 2, endAngle: Math.PI, tileMode: mode);
        using RenderTargetBitmap image = Render(canvas => canvas.DrawRect(new Rect(0, 0, 100, 100),
            new Paint { Shader = gradient.CreateShader(new Rect(0, 0, 100, 100)) }), 100, 100);

        AssertPixel(image, 25, 75, 131, 0, 124, 255);
        // Upper-left lies after the sector, lower-right before it: neither is the seam.
        if (mode == TileMode.Decal)
        {
            Assert.Equal(0, RasterBackend.PixelAt(image, 25, 25).A);
            Assert.Equal(0, RasterBackend.PixelAt(image, 75, 75).A);
        }
        else if (mode == TileMode.Clamp)
        {
            AssertPixel(image, 25, 25, 0, 0, 255, 255);
            AssertPixel(image, 75, 75, 255, 0, 0, 255);
        }
        else
        {
            AssertPixel(image, 25, 25, 128, 0, 128, 255);
            AssertPixel(image, 75, 75, 128, 0, 128, 255);
        }
    }

    [Theory]
    [InlineData(TileMode.Clamp)]
    [InlineData(TileMode.Repeated)]
    [InlineData(TileMode.Mirror)]
    [InlineData(TileMode.Decal)]
    public void FocalRadius_UsesBothCirclesAndShortestSide(TileMode mode)
    {
        var gradient = new RadialGradient(colors: [Colors.Red, Colors.Blue],
            center: Alignment.Center, radius: 0.5, focal: Alignment.Center, focalRadius: 0.125, tileMode: mode);
        // Non-square bounds: center=(60,40), radii=40 and 10.
        using RenderTargetBitmap image = Render(canvas => canvas.DrawRect(new Rect(0, 0, 120, 80),
            new Paint { Shader = gradient.CreateShader(new Rect(0, 0, 120, 80)) }), 120, 80);

        AssertPixel(image, 85, 40, 123, 0, 132, 255);
        if (mode == TileMode.Clamp)
        {
            AssertPixel(image, 62, 40, 255, 0, 0, 255);
            AssertPixel(image, 110, 40, 0, 0, 255, 255);
        }
        else if (mode == TileMode.Decal)
        {
            Assert.Equal(0, RasterBackend.PixelAt(image, 62, 40).A);
            Assert.Equal(0, RasterBackend.PixelAt(image, 110, 40).A);
        }
        else if (mode == TileMode.Repeated)
        {
            AssertPixel(image, 110, 40, 166, 0, 89, 255);
        }
        else
        {
            AssertPixel(image, 110, 40, 89, 0, 166, 255);
        }
    }

    [Fact]
    public void NullFocal_IgnoresFocalRadius()
    {
        var withRadius = new RadialGradient(colors: [Colors.Red, Colors.Blue], focalRadius: 0.2);
        var ordinary = new RadialGradient(colors: [Colors.Red, Colors.Blue]);
        using RenderTargetBitmap first = RenderGradient(withRadius);
        using RenderTargetBitmap second = RenderGradient(ordinary);
        Assert.Equal(RasterBackend.ReadPixels(first), RasterBackend.ReadPixels(second));
    }

    [Fact]
    public void DisplacedFocal_WithFiniteRadiusMatchesTheNativeConicalDomain()
    {
        // A shrinking cone whose focal circle is outside its terminal circle has invalid-domain pixels.
        var gradient = new RadialGradient(colors: [Colors.Red, Colors.Blue],
            focal: new Alignment(-0.6, -0.2), focalRadius: 0.4, radius: 0.2, tileMode: TileMode.Mirror);
        using RenderTargetBitmap actual = RenderGradient(gradient);
        using SKShader expected = SKShader.CreateTwoPointConicalGradient(
            new SKPoint(20, 40), 40, new SKPoint(50, 50), 20,
            [SKColors.Red, SKColors.Blue], [0.0f, 1.0f], SKShaderTileMode.Mirror);
        AssertNativePixels(actual, expected);
    }

    [Theory]
    [InlineData(Math.PI / 4)]
    [InlineData(Math.PI / 2)]
    public void Rotation_UsesAbsoluteBoundsForEveryGradientKind(double angle)
    {
        var bounds = new Rect(10, 20, 60, 40);
        var rotation = new GradientRotation(angle);
        SKMatrix matrix = SKMatrix.CreateRotation((float)angle, 40, 40);
        foreach (Gradient gradient in new Gradient[]
        {
            new LinearGradient(colors: [Colors.Red, Colors.Blue], transform: rotation),
            new RadialGradient(colors: [Colors.Red, Colors.Blue], radius: 0.6,
                center: Alignment.TopLeft, focal: Alignment.Center, focalRadius: 0.1, transform: rotation),
            new SweepGradient(colors: [Colors.Red, Colors.Blue], startAngle: 0.5, endAngle: 5.5,
                tileMode: TileMode.Mirror, transform: rotation),
        })
        {
            using RenderTargetBitmap actual = Render(canvas => canvas.DrawRect(new Rect(0, 0, 100, 100),
                new Paint { Shader = gradient.CreateShader(bounds) }), 100, 100);
            using SKShader expected = gradient switch
            {
                LinearGradient => SKShader.CreateLinearGradient(new SKPoint(10, 40), new SKPoint(70, 40),
                    [SKColors.Red, SKColors.Blue], [0.0f, 1.0f], SKShaderTileMode.Clamp, matrix),
                RadialGradient => SKShader.CreateTwoPointConicalGradient(new SKPoint(40, 40), 4,
                    new SKPoint(10, 20), 24, [SKColors.Red, SKColors.Blue], [0.0f, 1.0f],
                    SKShaderTileMode.Clamp, matrix),
                _ => SKShader.CreateSweepGradient(new SKPoint(40, 40), [SKColors.Red, SKColors.Blue],
                    [0.0f, 1.0f], SKShaderTileMode.Mirror,
                    0.5f * 180 / (float)Math.PI, 5.5f * 180 / (float)Math.PI, matrix),
            };
            AssertNativePixels(actual, expected);
        }
    }

    [Fact]
    public void PerspectiveMatrix_IsAppliedAndCapturedAtShaderCreation()
    {
        Matrix4 matrix = Matrix4.Identity();
        matrix.Storage[3] = 0.01;
        var gradient = new LinearGradient(colors: [Colors.Red, Colors.Blue],
            transform: new PerspectiveTransform(matrix));
        IBrush shader = gradient.CreateShader(new Rect(0, 0, 100, 100));
        matrix.Storage[3] = 0.5;
        using RenderTargetBitmap actual = Render(canvas => canvas.DrawRect(new Rect(0, 0, 100, 100),
            new Paint { Shader = shader }), 100, 100);
        using SKShader expected = SKShader.CreateLinearGradient(new SKPoint(0, 50), new SKPoint(100, 50),
            [SKColors.Red, SKColors.Blue], [0.0f, 1.0f], SKShaderTileMode.Clamp,
            new SKMatrix(1, 0, 0, 0, 1, 0, 0.01f, 0, 1));
        AssertNativePixels(actual, expected);
    }

    [Fact]
    public void ShaderMask_AndBlurredPaintUseTheSameDecalShader()
    {
        IBrush shader = new LinearGradient(colors: [Colors.Red, Colors.Blue], tileMode: TileMode.Decal)
            .CreateShader(new Rect(20, 0, 40, 60));
        using var masked = new RenderTargetBitmap(new PixelSize(100, 60));
        using (DrawingContext context = masked.CreateDrawingContext())
        using (FilterLayerRasterizer.DrawShaderMasked(context,
            target => target.DrawRectangle(Brushes.White, null, new Rect(0, 0, 100, 60)),
            shader, BlendMode.SourceIn, new Rect(0, 0, 100, 60)))
        {
        }

        AssertPixel(masked, 40, 30, 124, 0, 131, 255);
        Assert.Equal(0, RasterBackend.PixelAt(masked, 10, 30).A);
        using RenderTargetBitmap blurred = Render(canvas => canvas.DrawRect(new Rect(0, 0, 100, 60), new Paint
        {
            Shader = shader,
            MaskFilter = MaskFilter.Blur(BlurStyle.Normal, 2.0),
        }));
        AssertPixel(blurred, 40, 30, 124, 0, 131, 255);
        Assert.Equal(0, RasterBackend.PixelAt(blurred, 10, 30).A);
    }

    [Fact]
    public void PathStroke_PreservesRecordingClipOpacityAndDevicePixelHairlines()
    {
        IBrush shader = new LinearGradient(colors: [Colors.Red, Colors.Blue])
            .CreateShader(new Rect(0, 0, 100, 60));
        var paint = new Paint { Shader = shader };
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        canvas.ClipRect(new Rect(0, 0, 100, 40));
        canvas.Scale(2, 2);
        canvas.DrawLine(new Point(20, 5), new Point(20, 30), paint);
        paint.Shader = null;
        Picture picture = recorder.EndRecording();
        using var image = new RenderTargetBitmap(new PixelSize(100, 60));
        using (DrawingContext context = image.CreateDrawingContext())
        using (context.PushOpacity(0.5))
        {
            picture.Playback(context, default);
        }

        double coverage = 0.0;
        for (int x = 0; x < 100; x++)
        {
            coverage += RasterBackend.PixelAt(image, x, 20).A / 255.0;
        }

        Assert.InRange(coverage, 0.49, 0.51);
        Assert.Equal(0, RasterBackend.PixelAt(image, 40, 45).A);
        Assert.True(RasterBackend.PixelAt(image, 40, 20).R > RasterBackend.PixelAt(image, 40, 20).B);
    }

    [Fact]
    public void Decoration_UsesTheNativeShaderThroughTheWidgetRenderPipeline()
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(100, 100));
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new DecoratedBox(
            decoration: new BoxDecoration(Gradient: new RadialGradient(
                colors: [Colors.Red, Colors.Blue], radius: 0.4,
                focal: Alignment.Center, focalRadius: 0.1, tileMode: TileMode.Decal)))));
        using Bitmap image = Assert.IsAssignableFrom<OffsetLayer>(tester.RenderView.Layer)
            .ToImageSync(new Rect(0, 0, 100, 100));
        AssertPixel(image, 75, 50, 123, 0, 132, 255);
        Assert.Equal(0, RasterBackend.PixelAt(image, 51, 51).A);
        Assert.Equal(0, RasterBackend.PixelAt(image, 99, 99).A);
    }

    [Fact]
    public void Sweep_MultiTurnAndNegativeAnglesAreNotIndependentlyNormalized()
    {
        foreach ((double start, double end) in new[] { (-Math.PI, Math.PI), (Math.PI, Math.PI * 3) })
        {
            var gradient = new SweepGradient(colors: [Colors.Red, Colors.Blue],
                startAngle: start, endAngle: end, tileMode: TileMode.Decal);
            using RenderTargetBitmap actual = RenderGradient(gradient);
            using SKShader expected = SKShader.CreateSweepGradient(new SKPoint(50, 50),
                [SKColors.Red, SKColors.Blue], [0.0f, 1.0f], SKShaderTileMode.Decal,
                (float)start * 180 / (float)Math.PI, (float)end * 180 / (float)Math.PI);
            AssertNativePixels(actual, expected);
        }
    }

    [Fact]
    public void Stops_AreSanitizedByTheBackendAndPreserveHardTransitions()
    {
        var gradient = new LinearGradient(colors: [Colors.Red, Colors.Green, Colors.Blue, Colors.White],
            stops: [-0.5, 0.4, 0.2, 1.4]);
        using RenderTargetBitmap actual = RenderGradient(gradient);
        using SKShader expected = SKShader.CreateLinearGradient(new SKPoint(0, 50), new SKPoint(100, 50),
            [SKColors.Red, SKColors.Green, SKColors.Blue, SKColors.White], [0.0f, 0.4f, 0.4f, 1.0f],
            SKShaderTileMode.Clamp);
        AssertNativePixels(actual, expected);
    }

    [Fact]
    public void Radial_EngineAcceptsZeroRadiusAndTheOrdinaryZeroFocal()
    {
        Assert.NotNull(GradientShader.Radial(default, 0.0, [Colors.Red, Colors.Blue], [0.0, 1.0],
            TileMode.Mirror, null, null, 1.0));
        Assert.NotNull(GradientShader.Radial(default, 0.0, [Colors.Red, Colors.Blue], [0.0, 1.0],
            TileMode.Mirror, null, default(Point), 0.0));
        Assert.NotNull(GradientShader.Radial(default, 10.0, [Colors.Red, Colors.Blue], [0.0, 1.0],
            TileMode.Mirror, null, new Point(1, 1), 0.0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PaintAlpha_ModulatesTheShaderWithAndWithoutRasterEffects(bool blur)
    {
        using RenderTargetBitmap image = Render(canvas => canvas.DrawRect(new Rect(0, 0, 100, 60), new Paint
        {
            Shader = new LinearGradient(colors: [Colors.Red, Colors.Blue])
                .CreateShader(new Rect(0, 0, 100, 60)),
            Color = Color.From(alpha: 0.5, red: 0.0, green: 0.0, blue: 0.0),
            MaskFilter = blur ? MaskFilter.Blur(BlurStyle.Normal, 1.0) : null,
        }));
        Assert.InRange(RasterBackend.PixelAt(image, 50, 30).A, 127, 128);
        Assert.InRange(RasterBackend.PixelAt(image, 50, 30).R, 62, 65);
        Assert.InRange(RasterBackend.PixelAt(image, 50, 30).B, 63, 66);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ExplicitStops_AllowEmptyAndSingleColorGradients(int colorCount)
    {
        Color[] colors = colorCount == 0 ? [] : [Colors.Red];
        double[] stops = colorCount == 0 ? [] : [0.5];
        foreach (Gradient gradient in new Gradient[]
        {
            new LinearGradient(colors, stops: stops),
            new RadialGradient(colors, stops: stops),
            new SweepGradient(colors, stops: stops),
        })
        {
            using RenderTargetBitmap image = RenderGradient(gradient);
            AssertPixel(image, 70, 30, colorCount == 0 ? 0 : 255, 0, 0, colorCount == 0 ? 0 : 255);
        }
    }

    [Fact]
    public void DrawPaint_UsesTheCurrentClipAndRetainsPaintAlpha()
    {
        using RenderTargetBitmap image = Render(canvas =>
        {
            canvas.ClipRect(new Rect(10, 10, 60, 40));
            canvas.DrawPaint(new Paint
            {
                Shader = new LinearGradient(colors: [Colors.Red, Colors.Blue], tileMode: TileMode.Decal)
                    .CreateShader(new Rect(20, 0, 40, 60)),
                Color = Color.From(alpha: 0.5, red: 0.0, green: 0.0, blue: 0.0),
            });
        });
        Assert.Equal(128, RasterBackend.PixelAt(image, 40, 30).A);
        Assert.Equal(0, RasterBackend.PixelAt(image, 40, 5).A);
        Assert.Equal(0, RasterBackend.PixelAt(image, 15, 30).A);
    }

    [Fact]
    public void Gallery_ExercisesFocalRadiusSectorTilingAndRotation()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new GradientsDemoPage()));
        tester.Tap(Find.Text("Focal: Point"));
        tester.Pump();
        tester.Tap(Find.Text("Sweep: 3/4"));
        tester.Pump();
        foreach (string tileLabel in new[] { "Tile: Clamp", "Tile: Repeated", "Tile: Mirror" })
        {
            tester.Tap(Find.Text(tileLabel));
            tester.Pump();
        }

        tester.Tap(Find.Text("Rotate +"));
        tester.Pump();
        Gradient[] gradients = [.. tester.WidgetList<DecoratedBox>(Find.ByType<DecoratedBox>())
            .Select(widget => (widget.Decoration as BoxDecoration)?.Gradient).OfType<Gradient>()];
        var radial = Assert.Single(gradients.OfType<RadialGradient>());
        Assert.Equal(0.12, radial.FocalRadius);
        Assert.Equal(TileMode.Decal, radial.TileMode);
        Assert.Equal(new GradientRotation(Math.PI / 8), radial.Transform);
        var sweep = Assert.Single(gradients.OfType<SweepGradient>());
        Assert.Equal(Math.PI / 2, sweep.StartAngle);
        Assert.Equal(Math.PI, sweep.EndAngle);
        Assert.Equal(TileMode.Decal, sweep.TileMode);
        using Bitmap image = Assert.IsAssignableFrom<OffsetLayer>(tester.RenderView.Layer)
            .ToImageSync(new Rect(0, 0, 800, 600));
        Assert.Contains(RasterBackend.ReadPixels(image), value => value != 0);
        Assert.Null(tester.TakeException());
    }

    [DebugOnlyFact]
    public void ShaderCreation_RejectsInvalidSweepNaNCoordinatesAndNonfiniteMatrices()
    {
        Assert.Throws<AssertionError>(() => new SweepGradient(colors: [Colors.Red, Colors.Blue],
            startAngle: 1, endAngle: 1).CreateShader(new Rect(0, 0, 100, 100)));
        Assert.Throws<AssertionError>(() => GradientShader.Linear(new Point(double.NaN, 0), default,
            [Colors.Red, Colors.Blue], [0.0, 1.0], TileMode.Clamp, null));
        Assert.Throws<AssertionError>(() => GradientShader.Radial(default, 10,
            [Colors.Red, Colors.Blue], [0.0, 1.0], TileMode.Clamp, null, default(Point), 1.0));
        Matrix4 matrix = Matrix4.Identity();
        matrix.Storage[10] = double.PositiveInfinity;
        Assert.Throws<AssertionError>(() => new LinearGradient(colors: [Colors.Red, Colors.Blue],
            transform: new PerspectiveTransform(matrix)).CreateShader(new Rect(0, 0, 100, 100)));
    }

    private static RenderTargetBitmap RenderGradient(Gradient gradient) => Render(canvas => canvas.DrawRect(
        new Rect(0, 0, 100, 100), new Paint { Shader = gradient.CreateShader(new Rect(0, 0, 100, 100)) }),
        100, 100);

    private static RenderTargetBitmap Render(Action<Canvas> draw, int width = 100, int height = 60)
    {
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        draw(canvas);
        Picture picture = recorder.EndRecording();
        var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
        using DrawingContext context = bitmap.CreateDrawingContext();
        picture.Playback(context, default);
        return bitmap;
    }

    private static void AssertPixel(Bitmap bitmap, int x, int y, int r, int g, int b, int a)
    {
        var pixel = RasterBackend.PixelAt(bitmap, x, y);
        Assert.InRange((int)pixel.R, Math.Max(0, r - 3), Math.Min(255, r + 3));
        Assert.InRange((int)pixel.G, Math.Max(0, g - 3), Math.Min(255, g + 3));
        Assert.InRange((int)pixel.B, Math.Max(0, b - 3), Math.Min(255, b + 3));
        Assert.Equal(a, pixel.A);
    }

    private static void AssertNativePixels(Bitmap actual, SKShader shader)
    {
        using var expected = new SKBitmap(new SKImageInfo(actual.PixelSize.Width, actual.PixelSize.Height,
            SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(expected);
        using var paint = new SKPaint { Shader = shader, IsDither = true, IsAntialias = true };
        canvas.Clear(SKColors.Transparent);
        canvas.DrawRect(0, 0, expected.Width, expected.Height, paint);
        byte[] pixels = RasterBackend.ReadPixels(actual);
        byte[] oracle = expected.Bytes;
        Assert.Equal(pixels.Length, oracle.Length);
        for (int index = 0; index < pixels.Length; index++)
        {
            Assert.True(Math.Abs(pixels[index] - oracle[index]) <= 2,
                $"Byte {index}: actual {pixels[index]}, native {oracle[index]}");
        }
    }

    private sealed record PerspectiveTransform(Matrix4 Matrix) : GradientTransform
    {
        public override Matrix? Transform(Rect bounds, TextDirection? textDirection = null) => null;
        public override Matrix4? Transform4(Rect bounds, TextDirection? textDirection = null) => Matrix;
    }
}
