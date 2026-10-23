// Dart parity source: material_ui/lib/src/ink_sparkle.dart
// Mirrors material-ui-src/test/ink_sparkle_test.dart

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.UI;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;
using SkiaSharp;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class InkSparkleDartParityTests
{
    [Fact]
    public void GalleryInk_IsVisibleAboveTheOpaquePageSurface()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, logicalSize: new Size(800, 900));
        tester.PumpWidget(new MaterialApp(home: new Scaffold(body: new ColoredBox(
            color: Colors.White,
            child: new InkResponseDemoPage()))));
        Element circle = tester.ElementsOfType<Ink>().Single(element => ((Ink)element.Widget).Width == 112);
        Element well = tester.ElementsOfType<Ink>().Single(element => ((Ink)element.Widget).Width == 150);
        Element stadium = tester.ElementsOfType<Ink>().Single(element => ((Ink)element.Widget).Width == 220);
        using Bitmap before = Assert.IsAssignableFrom<OffsetLayer>(tester.RenderView.Layer)
            .ToImageSync(new Rect(0, 0, 800, 600));
        AssertGalleryColor(before, tester.GetTopLeft(circle) + new Vector(56, 20), 0xEA, 0xDD, 0xFF);
        AssertGalleryColor(before, tester.GetTopLeft(well) + new Vector(20, 20), 0xD7, 0xE3, 0xFF);
        AssertGalleryColor(before, tester.GetTopLeft(stadium) + new Vector(15, 26), 0xFF, 0xD8, 0xE4);

        tester.Tap(Find.Text("Splash factory: InkRipple"));
        tester.PumpAndSettle();
        TestGesture gesture = tester.StartGesture(tester.GetTopLeft(well) + new Vector(20, 20),
            PointerDeviceKind.Touch);
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Contains(InkFeatureProbe.Splashes(tester.RenderView), feature => feature is InkSparkle);
        using Bitmap after = Assert.IsAssignableFrom<OffsetLayer>(tester.RenderView.Layer)
            .ToImageSync(new Rect(0, 0, 800, 600));
        Assert.NotEqual(RasterBackend.ReadPixels(before), RasterBackend.ReadPixels(after));
        gesture.Up();
        tester.PumpAndSettle();
        double originalDilation = Scheduler.TimeDilation;
        tester.Tap(Find.Text("Speed: 1x"));
        tester.Pump();
        Assert.Equal(originalDilation * 10.0, Scheduler.TimeDilation);
        tester.PumpWidget(new SizedBox());
        Assert.Equal(originalDilation, Scheduler.TimeDilation);
        Assert.Null(tester.TakeException());
    }

    private static void AssertGalleryColor(Bitmap image, Point point, int r, int g, int b)
    {
        var pixel = RasterBackend.PixelAt(image, (int)point.X, (int)point.Y);
        Assert.Equal((r, g, b, 255), ((int)pixel.R, (int)pixel.G, (int)pixel.B, (int)pixel.A));
    }

    // Flutter: "InkSparkle in a Button compiles and does not crash"
    [Fact]
    public void InkSparkleInAButtonCompilesAndDoesNotCrash()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialApp(
                home: new Scaffold(
                    body: new Center(
                        child: new ElevatedButton(
                            style: ElevatedButton.StyleFrom(splashFactory: InkSparkle.SplashFactory),
                            child: new Text("Sparkle!"),
                            onPressed: () => { })))));
        Element button = tester.ElementsWithText("Sparkle!").Single();
        Tap(tester, button);
        tester.Pump();
        tester.PumpAndSettle();
        Assert.Null(tester.TakeException());
    }

    // Flutter: "InkSparkle default splashFactory paints with drawRect when bounded"
    [Fact]
    public void InkSparkleDefaultSplashFactoryPaintsWithDrawRectWhenBounded()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialApp(
                home: new Scaffold(
                    body: new Center(
                        child: new InkWell(
                            splashFactory: InkSparkle.SplashFactory,
                            child: new Text("Sparkle!"),
                            onTap: () => { })))));
        Element button = tester.ElementsWithText("Sparkle!").Single();
        Tap(tester, button);
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));

        var material = (RenderInkFeatures)MaterialWidget.Of(button);
        Assert.Equal(1, PaintRecording.Record(material).CountCalls("drawRect"));

        Assert.Single(material.DebugInkFeatures!);

        tester.PumpAndSettle();
        // ink feature is disposed.
        Assert.Empty(material.DebugInkFeatures!);
    }

    // Flutter: "InkSparkle default splashFactory paints with drawPaint when unbounded"
    [Fact]
    public void InkSparkleDefaultSplashFactoryPaintsWithDrawPaintWhenUnbounded()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialApp(
                home: new Scaffold(
                    body: new Center(
                        child: new InkResponse(
                            splashFactory: InkSparkle.SplashFactory,
                            child: new Text("Sparkle!"),
                            onTap: () => { })))));
        Element button = tester.ElementsWithText("Sparkle!").Single();
        Tap(tester, button);
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));

        var material = (RenderInkFeatures)MaterialWidget.Of(button);
        Assert.Equal(1, PaintRecording.Record(material).CountCalls("drawPaint"));
    }

    // The six upstream golden scenarios use real widget animations and compare the ink pixels with
    // Flutter's independently generated CanvasKit shader, without font/platform-specific button pixels.

    // Flutter: "Material2 - InkSparkle renders with sparkles when top left of button is tapped"
    [Fact]
    public void Material2InkSparkleRendersWithSparklesWhenTopLeftOfButtonIsTapped()
    {
        VerifySparkleFrames(false, 0.2);
    }

    // Flutter: "Material3 - InkSparkle renders with sparkles when top left of button is tapped"
    [Fact]
    public void Material3InkSparkleRendersWithSparklesWhenTopLeftOfButtonIsTapped()
    {
        VerifySparkleFrames(true, 0.2);
    }

    // Flutter: "Material2 - InkSparkle renders with sparkles when center of button is tapped"
    [Fact]
    public void Material2InkSparkleRendersWithSparklesWhenCenterOfButtonIsTapped()
    {
        VerifySparkleFrames(false, 0.5);
    }

    // Flutter: "Material3 - InkSparkle renders with sparkles when center of button is tapped"
    [Fact]
    public void Material3InkSparkleRendersWithSparklesWhenCenterOfButtonIsTapped()
    {
        VerifySparkleFrames(true, 0.5);
    }

    // Flutter: "Material2 - InkSparkle renders with sparkles when bottom right of button is tapped"
    [Fact]
    public void Material2InkSparkleRendersWithSparklesWhenBottomRightOfButtonIsTapped()
    {
        VerifySparkleFrames(false, 0.8);
    }

    // Flutter: "Material3 - InkSparkle renders with sparkles when bottom right of button is tapped"
    [Fact]
    public void Material3InkSparkleRendersWithSparklesWhenBottomRightOfButtonIsTapped()
    {
        VerifySparkleFrames(true, 0.8);
    }

    private static void VerifySparkleFrames(bool useMaterial3, double fraction)
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(useMaterial3: useMaterial3),
            home: new Center(child: new MaterialWidget(
                type: MaterialType.Transparency,
                child: new SizedBox(width: 160, height: 80, child: new InkWell(
                    splashFactory: InkSparkle.ConstantTurbulenceSeedSplashFactory,
                    splashColor: new Color(0x80FF0000),
                    onTap: () => { }))))));
        Element well = Assert.Single(tester.ElementsOfType<InkWell>());
        Point topLeft = tester.GetTopLeft(well);
        TestGesture gesture = tester.StartGesture(topLeft + new Vector(160 * fraction, 80 * fraction),
            PointerDeviceKind.Touch);
        tester.Pump();
        var controller = (RenderInkFeatures)MaterialWidget.Of(well);
        var sparkle = Assert.IsType<InkSparkle>(Assert.Single(InkFeatureProbe.Splashes(controller)));
        using Stream fixture = typeof(InkSparkleDartParityTests).Assembly.GetManifestResourceStream(
            "Plumix.Tests.Fixtures.Shaders.ink_sparkle_flutter.sksl")!;
        using var reader = new StreamReader(fixture);
        using SKRuntimeEffect oracle = SKRuntimeEffect.CreateShader(reader.ReadToEnd(), out string errors);
        Assert.True(oracle is not null, errors);
        bool sawWhiteSparkles = false;
        Picture? retained = null;
        byte[]? retainedPixels = null;
        try
        {
            for (int frame = 0; frame <= 5; frame++)
            {
                tester.Pump(TimeSpan.FromMilliseconds(50));
                var recorder = new PictureRecorder();
                sparkle.PaintInternal(new Canvas(recorder));
                Picture picture = recorder.EndRecording();
                using RenderTargetBitmap actual = ReplayInk(picture);
                byte[] pixels = RasterBackend.ReadPixels(actual);
                AssertFlutterShaderPixels(pixels, sparkle.DebugUniforms, oracle!);
                sawWhiteSparkles |= pixels.Where((value, index) => index % 4 == 0).Any(value => value > 0);
                if (frame == 0)
                {
                    // A separately recorded first frame is replayed after animation disposal below.
                    var savedRecorder = new PictureRecorder();
                    sparkle.PaintInternal(new Canvas(savedRecorder));
                    retained = savedRecorder.EndRecording();
                    retainedPixels = pixels;
                }
            }

            Assert.True(sawWhiteSparkles, "White sparkle pixels must be present over the red wave.");
            gesture.Up();
            tester.PumpAndSettle();
            Assert.Empty(controller.DebugInkFeatures!);
            using RenderTargetBitmap replayed = ReplayInk(retained!);
            Assert.Equal(retainedPixels, RasterBackend.ReadPixels(replayed));
        }
        finally
        {
            retained?.Dispose();
        }
    }

    private static RenderTargetBitmap ReplayInk(Picture picture)
    {
        var image = new RenderTargetBitmap(new PixelSize(160, 80));
        using DrawingContext context = image.CreateDrawingContext();
        picture.Playback(context, default);
        return image;
    }

    private static void AssertFlutterShaderPixels(byte[] actual, IReadOnlyList<double> u, SKRuntimeEffect oracle)
    {
        using var uniforms = new SKRuntimeEffectUniforms(oracle)
        {
            ["u_color"] = new float[] { (float)u[0], (float)u[1], (float)u[2], (float)u[3] },
            ["u_alpha"] = (float)u[4],
            ["u_sparkle_color"] = new float[] { 1, 1, 1, 1 },
            ["u_sparkle_alpha"] = (float)u[5],
            ["u_blur"] = (float)u[6],
            ["u_radius_scale"] = (float)u[7],
            ["u_center"] = new float[] { (float)u[8], (float)u[9] },
            ["u_max_radius"] = (float)u[10],
            ["u_resolution_scale"] = new float[] { (float)u[11], (float)u[12] },
            ["u_noise_scale"] = new float[] { (float)u[13], (float)u[14] },
            ["u_noise_phase"] = (float)u[15],
            ["u_circle1"] = new float[] { (float)u[16], (float)u[17] },
            ["u_circle2"] = new float[] { (float)u[18], (float)u[19] },
            ["u_circle3"] = new float[] { (float)u[20], (float)u[21] },
            ["u_rotation1"] = new float[] { (float)u[22], (float)u[23] },
            ["u_rotation2"] = new float[] { (float)u[24], (float)u[25] },
            ["u_rotation3"] = new float[] { (float)u[26], (float)u[27] },
        };
        using SKShader shader = oracle.ToShader(uniforms);
        using var expected = new SKBitmap(new SKImageInfo(160, 80, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(expected);
        using var paint = new SKPaint { Shader = shader, IsDither = true, IsAntialias = true };
        canvas.Clear(SKColors.Transparent);
        canvas.DrawRect(0, 0, 160, 80, paint);
        byte[] pixels = expected.Bytes;
        for (int index = 0; index < pixels.Length; index++)
        {
            Assert.True(Math.Abs(actual[index] - pixels[index]) <= 3,
                $"Shader byte {index}: Plumix {actual[index]}, Flutter {pixels[index]}");
        }
    }

    private static FrameworkDartTester CreateTester() => new(fakeGestureTimers: true);

    // `tester.tap(finder)`: a down and an up at the center, no pump.
    private static void Tap(FrameworkDartTester tester, Element element) =>
        tester.StartGesture(tester.GetCenter(element), PointerDeviceKind.Touch).Up();
}
