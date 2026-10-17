using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Canvas = Plumix.UI.Canvas;
using Path = Plumix.UI.Path;

// Dart parity source: flutter/packages/flutter/test/painting/paint_image_test.dart,
// decoration_test.dart (DecorationImage/paintImage cases), decoration_image_lerp_test.dart,
// flutter/packages/flutter/test/widgets/image_rtl_test.dart (DecorationImage cases) and the ImageSizeInfo
// half of painting/debug.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class DecorationImageDartParityTests : IDisposable
{
    private static int s_nextKey;

    public DecorationImageDartParityTests()
    {
        ImageCache.Shared.Clear();
        ImageCache.Shared.ClearLiveImages();
        ImagePainting.DebugFlushLastFrameImageSizeInfo();
    }

    public void Dispose()
    {
        PaintingDebug.DebugOnPaintImage = null;
        PaintingDebug.DebugInvertOversizedImages = false;
        ImageCache.Shared.Clear();
        ImageCache.Shared.ClearLiveImages();
    }

    private static Rect LTRB(double left, double top, double right, double bottom) =>
        new(new Point(left, top), new Point(right, bottom));

    private static List<CanvasCall> Record(Action<Canvas> paint)
    {
        var canvas = new Canvas(new PictureRecorder());
        paint(canvas);
        return canvas.DebugCalls.ToList();
    }

    // flutter_test's test view: a registered view with a device pixel ratio of 3.0.
    private static IDisposable TestView(double devicePixelRatio = 3.0)
    {
        var view = new FlutterView(new Size(2400, 1800), devicePixelRatio, 900_000 + Interlocked.Increment(ref s_nextKey));
        PlatformDispatcher.Instance.AddView(view);
        return new Disposer(() => PlatformDispatcher.Instance.RemoveView(view));
    }

    private static double MaxDevicePixelRatio() =>
        PlatformDispatcher.Instance.Views.Aggregate(0.0, static (value, view) => Math.Max(value, view.DevicePixelRatio));

    // ---- paint_image_test.dart -------------------------------------------------------------------------

    [Fact]
    public void PaintImage_CoverAndAlign()
    {
        var image = new TestImage(300, 300);
        List<CanvasCall> calls = Record(canvas => ImagePainting.PaintImage(
            canvas: canvas,
            rect: new Rect(50.0, 75.0, 200.0, 100.0),
            image: image,
            fit: BoxFit.Cover,
            alignment: Alignment.CenterLeft));

        CanvasCall command = calls.First(call => call.Method == "drawImageRect");
        Assert.Same(image, command.Image);
        Assert.Equal(new Rect(0.0, 75.0, 300.0, 150.0), command.SourceRect);
        Assert.Equal(new Rect(50.0, 75.0, 200.0, 100.0), command.Rect);
    }

    [Fact]
    public void PaintImage_DebugInvertOversizedImages()
    {
        using IDisposable view = TestView();
        Assert.Equal(3.0, MaxDevicePixelRatio());
        PaintingDebug.DebugInvertOversizedImages = true;
        var messages = new List<string>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = details => messages.Add((string)details.Exception);
        List<CanvasCall> calls;
        try
        {
            var rect = new Rect(50.0, 50.0, 100.0, 50.0);
            calls = Record(canvas => ImagePainting.PaintImage(
                canvas: canvas,
                rect: rect,
                image: new TestImage(300, 300),
                debugImageLabel: "TestImage",
                fit: BoxFit.Fill));
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        int saveLayer = calls.FindIndex(call => call.Method == "saveLayer");
        Assert.True(saveLayer >= 0);
        CanvasCall layer = calls[saveLayer];
        Assert.Equal(new Rect(50.0, 50.0, 100.0, 50.0), layer.Rect);
        Assert.Equal(
            new ColorFilter.Matrix(-1, 0, 0, 0, 255, 0, -1, 0, 0, 255, 0, 0, -1, 0, 255, 0, 0, 0, 1, 0),
            layer.Paint!.ColorFilter);
        Assert.Equal(("translate", 0.0, 75.0), (calls[saveLayer + 1].Method, calls[saveLayer + 1].Dx, calls[saveLayer + 1].Dy));
        Assert.Equal(("scale", 1.0, -1.0), (calls[saveLayer + 2].Method, calls[saveLayer + 2].Dx, calls[saveLayer + 2].Dy));
        Assert.Equal(("translate", 0.0, -75.0), (calls[saveLayer + 3].Method, calls[saveLayer + 3].Dx, calls[saveLayer + 3].Dy));

        string message = Assert.Single(messages);
        Assert.Equal(
            "Image TestImage has a display size of 300×150 but a decode size of 300×300, which uses an "
            + "additional 234KB (assuming a device pixel ratio of 3.0).\n\nConsider resizing the asset ahead of "
            + "time, supplying a cacheWidth parameter of 300, a cacheHeight parameter of 150, or using a "
            + "ResizeImage.",
            message);
    }

    [Fact]
    public void PaintImage_DebugInvertOversizedImages_SmallerThanOverheadAllowance()
    {
        using IDisposable view = TestView();
        PaintingDebug.DebugInvertOversizedImages = true;
        var messages = new List<object>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = details => messages.Add(details.Exception);
        try
        {
            Record(canvas => ImagePainting.PaintImage(
                canvas: canvas,
                rect: new Rect(50.0, 50.0, 290.0, 290.0),
                image: new TestImage(300, 300),
                debugImageLabel: "TestImage",
                fit: BoxFit.Fill));
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        Assert.Empty(messages);
    }

    [Fact]
    public void PaintImage_CenterSliceWithScaleNotOne()
    {
        List<CanvasCall> calls = Record(canvas => ImagePainting.PaintImage(
            canvas: canvas,
            rect: LTRB(10, 20, 430, 420),
            image: new TestImage(300, 300),
            scale: 2.0,
            centerSlice: LTRB(50, 40, 250, 260)));

        CanvasCall command = calls.First(call => call.Method == "drawImageNine");
        Assert.Equal(LTRB(100.0, 80.0, 500.0, 520.0), command.SourceRect);
        Assert.Equal(LTRB(20.0, 40.0, 860.0, 840.0), command.Rect);
    }

    [Fact]
    public void PaintImage_ReportsImagePainting()
    {
        using var tester = new FrameworkDartTester();
        using IDisposable view = TestView();
        double ratio = MaxDevicePixelRatio();
        var imageSizeInfos = new List<ImageSizeInfo>();
        PaintingDebug.DebugOnPaintImage = imageSizeInfos.Add;
        var image = new TestImage(300, 300);
        var rect = new Rect(50.0, 75.0, 200.0, 100.0);

        Record(canvas => ImagePainting.PaintImage(canvas: canvas, rect: rect, image: image, debugImageLabel: "test.png"));

        ImageSizeInfo info = Assert.Single(imageSizeInfos);
        Assert.Equal("test.png", info.Source);
        Assert.Equal(new Size(300, 300), info.ImageSize);
        Assert.Equal(new Size(200 * ratio, 100 * ratio), info.DisplaySize);

        // Make sure that we don't report an identical image size info if we redraw in the next frame.
        SchedulerBinding.Instance.ScheduleForcedFrame();
        tester.Pump();

        Record(canvas => ImagePainting.PaintImage(canvas: canvas, rect: rect, image: image, debugImageLabel: "test.png"));
        Assert.Single(imageSizeInfos);
    }

    [Fact]
    public void PaintImage_ReportsImagePainting_ChangePerFrame()
    {
        using var tester = new FrameworkDartTester();
        using IDisposable view = TestView();
        double ratio = MaxDevicePixelRatio();
        var imageSizeInfos = new List<ImageSizeInfo>();
        PaintingDebug.DebugOnPaintImage = imageSizeInfos.Add;
        var image = new TestImage(300, 300);

        Record(canvas => ImagePainting.PaintImage(
            canvas: canvas,
            rect: new Rect(50.0, 75.0, 200.0, 100.0),
            image: image,
            debugImageLabel: "test.png"));
        Assert.Single(imageSizeInfos);

        // Make sure that we don't report an identical image size info if we redraw in the next frame.
        SchedulerBinding.Instance.ScheduleForcedFrame();
        tester.Pump();

        Record(canvas => ImagePainting.PaintImage(
            canvas: canvas,
            rect: new Rect(50.0, 75.0, 200.0, 150.0),
            image: image,
            debugImageLabel: "test.png"));

        Assert.Equal(2, imageSizeInfos.Count);
        ImageSizeInfo info = imageSizeInfos[1];
        Assert.Equal("test.png", info.Source);
        Assert.Equal(new Size(300, 300), info.ImageSize);
        Assert.Equal(new Size(200 * ratio, 150 * ratio), info.DisplaySize);
    }

    [Fact]
    public void PaintImage_ReportsImagePainting_NoDebugLabel()
    {
        using IDisposable view = TestView();
        double ratio = MaxDevicePixelRatio();
        var imageSizeInfos = new List<ImageSizeInfo>();
        PaintingDebug.DebugOnPaintImage = imageSizeInfos.Add;

        Record(canvas => ImagePainting.PaintImage(
            canvas: canvas,
            rect: new Rect(50.0, 75.0, 200.0, 100.0),
            image: new TestImage(300, 200)));

        ImageSizeInfo info = Assert.Single(imageSizeInfos);
        Assert.Equal("<Unknown Image(300×200)>", info.Source);
        Assert.Equal(new Size(300, 200), info.ImageSize);
        Assert.Equal(new Size(200 * ratio, 100 * ratio), info.DisplaySize);
    }

    // ---- decoration_test.dart -------------------------------------------------------------------------

    [Fact]
    public void BoxDecorationImageListenerSync()
    {
        var imageProvider = new SynchronousTestImageProvider();
        bool onChangedCalled = false;
        var boxDecoration = new BoxDecoration(Image: new DecorationImage(imageProvider));
        BoxPainter boxPainter = boxDecoration.CreateBoxPainter(() => onChangedCalled = true);

        boxPainter.Paint(new TestRecordingPaintingContext(), default, new ImageConfiguration(Size: new Size(100, 100)));

        // The onChanged callback should not be invoked during the call to boxPainter.paint
        Assert.False(onChangedCalled);
    }

    [Fact]
    public void BoxDecorationImageListener_DoesNotChangeWhenImageIsClone()
    {
        var image1 = new TestImage(10, 10);
        var image2 = new TestImage(10, 10);
        var completer = new ManualImageStreamCompleter();
        var imageProvider = new ControllableImageProvider(completer);
        bool onChangedCalled = false;
        var boxDecoration = new BoxDecoration(Image: new DecorationImage(imageProvider));
        BoxPainter boxPainter = boxDecoration.CreateBoxPainter(() => onChangedCalled = true);
        var configuration = new ImageConfiguration(Size: new Size(100, 100));

        boxPainter.Paint(new TestRecordingPaintingContext(), default, configuration);
        var info1 = new ImageInfo(image1);
        completer.SetImageForTest(info1.Clone());
        Assert.True(onChangedCalled);

        onChangedCalled = false;
        completer.SetImageForTest(info1.Clone());
        Assert.False(onChangedCalled);

        completer.SetImageForTest(new ImageInfo(image2));
        Assert.True(onChangedCalled);
    }

    [Theory]
    [InlineData("circle")]
    [InlineData("radius")]
    [InlineData("rect")]
    public void BoxDecoration_BackgroundImageClip(string shape)
    {
        var imageProvider = new SynchronousTestImageProvider();
        BoxDecoration decoration = shape switch
        {
            "circle" => new BoxDecoration(Shape: BoxShape.Circle, Image: new DecorationImage(imageProvider)),
            "radius" => new BoxDecoration(
                BorderRadius: Plumix.Rendering.BorderRadius.Circular(16.0),
                Image: new DecorationImage(imageProvider)),
            _ => new BoxDecoration(Image: new DecorationImage(imageProvider)),
        };
        BoxPainter boxPainter = decoration.CreateBoxPainter(() => { });
        var context = new TestRecordingPaintingContext();

        boxPainter.Paint(context, default, new ImageConfiguration(Size: new Size(100, 100)));

        string[] methods = context.Calls
            .Select(call => call.Method)
            .Where(method => method is "clipPath" or "drawImageRect")
            .ToArray();
        Assert.Equal(shape == "rect" ? ["drawImageRect"] : ["clipPath", "drawImageRect"], methods);
    }

    [Fact]
    public void DecorationImage_Test()
    {
        var colorFilter = new ColorFilter.Mode(new Color(0xFF00FF00), BlendMode.Source);
        var backgroundImage = new DecorationImage(
            new SynchronousTestImageProvider(),
            colorFilter: colorFilter,
            fit: BoxFit.Contain,
            alignment: Alignment.BottomLeft,
            centerSlice: new Rect(10.0, 20.0, 30.0, 40.0),
            repeat: ImageRepeat.RepeatY,
            opacity: 0.5,
            filterQuality: FilterQuality.High,
            invertColors: true,
            isAntiAlias: true);
        var boxDecoration = new BoxDecoration(Image: backgroundImage);
        BoxPainter boxPainter = boxDecoration.CreateBoxPainter(() => Assert.Fail("onChanged"));
        var context = new TestRecordingPaintingContext();

        boxPainter.Paint(context, default, new ImageConfiguration(Size: new Size(100.0, 100.0)));

        CanvasCall call = Assert.Single(context.Calls, call => call.Method == "drawImageNine");
        Assert.Equal(LTRB(10.0, 20.0, 40.0, 60.0), call.SourceRect);
        Assert.Equal(LTRB(0.0, 0.0, 100.0, 100.0), call.Rect);
        Paint paint = call.Paint!;
        Assert.Equal(colorFilter, paint.ColorFilter);
        // isSameColorAs(Color(0x7F000000)): Color.fromRGBO(0, 0, 0, 0.5) within a rounding step.
        Assert.InRange(paint.Color.Alpha, 0x7F, 0x80);
        Assert.Equal(0, paint.Color.Red + paint.Color.Green + paint.Color.Blue);
        Assert.Equal(FilterQuality.High, paint.FilterQuality);
        Assert.True(paint.IsAntiAlias);
        Assert.True(paint.InvertColors);
    }

    [Fact]
    public void DecorationImage_ToString()
    {
        var backgroundImage = new DecorationImage(new SynchronousTestImageProvider(), scale: 2.01, opacity: 0.99);

        Assert.Equal(
            "DecorationImage(SynchronousTestImageProvider(), Alignment.center, scale 2.0, opacity 1.0, "
            + "FilterQuality.medium)",
            backgroundImage.ToString());
    }

    [Fact]
    public void DecorationImage_WithNullTextDirectionConfiguration_ShouldThrowError()
    {
        var backgroundImage = new DecorationImage(
            new SynchronousTestImageProvider(),
            colorFilter: new ColorFilter.Mode(new Color(0xFF00FF00), BlendMode.Source),
            fit: BoxFit.Contain,
            centerSlice: new Rect(10.0, 20.0, 30.0, 40.0),
            repeat: ImageRepeat.RepeatY,
            matchTextDirection: true,
            scale: 0.5,
            opacity: 0.5,
            invertColors: true,
            isAntiAlias: true);
        var boxDecoration = new BoxDecoration(Image: backgroundImage);
        BoxPainter boxPainter = boxDecoration.CreateBoxPainter(() => Assert.Fail("onChanged"));

        FlutterError error = Assert.Throws<FlutterError>(() => boxPainter.Paint(
            new TestRecordingPaintingContext(),
            default,
            new ImageConfiguration(Size: new Size(100.0, 100.0))));

        Assert.Equal(4, error.Diagnostics.Count);
        Assert.IsType<DiagnosticsProperty<DecorationImage>>(error.Diagnostics[2]);
        Assert.IsType<DiagnosticsProperty<ImageConfiguration>>(error.Diagnostics[3]);
        Assert.Equal(
            "FlutterError\n"
            + "   DecorationImage.matchTextDirection can only be used when a TextDirection is available.\n"
            + "   When DecorationImagePainter.paint() was called, there was no text direction provided in the "
            + "ImageConfiguration object to match.\n"
            + "   The DecorationImage was:\n"
            + $"     DecorationImage(SynchronousTestImageProvider(), ColorFilter.mode({new Color(0xff00ff00)}, "
            + "BlendMode.src), BoxFit.contain, Alignment.center, centerSlice: Rect.fromLTRB(10.0, 20.0, 40.0, "
            + "60.0), ImageRepeat.repeatY, match text direction, scale 0.5, opacity 0.5, FilterQuality.medium, "
            + "invert colors, use anti-aliasing)\n"
            + "   The ImageConfiguration was:\n"
            + "     ImageConfiguration(size: Size(100.0, 100.0))\n",
            error.ToStringDeep(wrapWidth: 640));
    }

    [Fact]
    public void DecorationImage_ErrorListener()
    {
        object? exception = null;
        var backgroundImage = new DecorationImage(
            new SynchronousErrorTestImageProvider("threw"),
            onError: (error, _) => exception = error.Message);

        backgroundImage.CreatePainter(() => { }).Paint(
            new Canvas(new PictureRecorder()),
            Canvas.LargestRect,
            new Path(),
            ImageConfiguration.Empty);

        Assert.True(EventLoopPump.SpinUntil(() => exception is not null));
        Assert.Equal("threw", exception);
    }

    [Fact]
    public void PaintImage_BoxFitNoneAndScaleDown_ScaleTest()
    {
        foreach (BoxFit fit in new[] { BoxFit.None, BoxFit.ScaleDown })
        {
            for (double scale = 1.0; scale <= 4.0; scale += 1.0)
            {
                var image = new TestImage(100, 100);
                var outputRect = new Rect(30.0, 30.0, 250.0, 250.0);
                List<CanvasCall> calls = Record(canvas => ImagePainting.PaintImage(
                    canvas: canvas,
                    rect: outputRect,
                    image: image,
                    scale: scale,
                    alignment: Alignment.BottomRight,
                    fit: fit));

                CanvasCall call = Assert.Single(calls, call => call.Method == "drawImageRect");
                Assert.Equal(new Rect(0, 0, 100, 100), call.SourceRect);
                double side = 100 / scale;
                AssertCloseToRect(
                    new Rect(new Point(outputRect.Right - side, outputRect.Bottom - side), outputRect.BottomRight),
                    call.Rect!.Value);
            }
        }
    }

    [Fact]
    public void PaintImage_BoxFitScaleDownTest()
    {
        var outputRect = new Rect(30.0, 30.0, 250.0, 20.0);
        List<CanvasCall> calls = Record(canvas => ImagePainting.PaintImage(
            canvas: canvas,
            rect: outputRect,
            image: new TestImage(100, 100),
            scale: 2.0,
            alignment: Alignment.BottomRight,
            fit: BoxFit.ScaleDown));

        CanvasCall call = Assert.Single(calls, call => call.Method == "drawImageRect");
        Assert.Equal(new Rect(0, 0, 100, 100), call.SourceRect);
        Assert.Equal(LTRB(260.0, 30.0, 280.0, 50.0), call.Rect);
    }

    [Fact]
    public void PaintImage_BoxFitScaleAndAlignmentTest()
    {
        BoxFit[] boxFits =
        [
            BoxFit.Contain, BoxFit.Cover, BoxFit.FitWidth, BoxFit.FitWidth, BoxFit.FitHeight, BoxFit.None,
            BoxFit.ScaleDown,
        ];
        foreach (BoxFit boxFit in boxFits)
        {
            var outputRect = new Rect(30.0, 30.0, 250.0, 250.0);
            List<CanvasCall> calls = Record(canvas => ImagePainting.PaintImage(
                canvas: canvas,
                rect: outputRect,
                image: new TestImage(100, 100),
                scale: 3.0,
                fit: boxFit));

            CanvasCall call = Assert.Single(calls, call => call.Method == "drawImageRect");
            Assert.True(
                Math.Abs(outputRect.Center.X - call.Rect!.Value.Center.X) < 1e-9
                && Math.Abs(outputRect.Center.Y - call.Rect!.Value.Center.Y) < 1e-9,
                $"{call.Rect} is not centered on {outputRect}");
        }
    }

    [Fact]
    public void PaintImage_WithRepeatXAndFitHeight()
    {
        List<CanvasCall> calls = Record(canvas => ImagePainting.PaintImage(
            canvas: canvas,
            rect: new Rect(30.0, 30.0, 400.0, 200.0),
            image: new TestImage(100, 100),
            alignment: Alignment.TopLeft,
            repeat: ImageRepeat.RepeatX,
            fit: BoxFit.FitHeight));

        CanvasCall[] draws = calls.Where(call => call.Method == "drawImageRect").ToArray();
        Assert.Equal(2, draws.Length);
        Assert.All(draws, call => Assert.Equal(new Rect(0, 0, 100, 100), call.SourceRect));
        Assert.Equal(
            new HashSet<Rect> { new(30.0, 30.0, 200.0, 200.0), new(230.0, 30.0, 200.0, 200.0) },
            draws.Select(call => call.Rect!.Value).ToHashSet());
    }

    [Fact]
    public void PaintImage_WithRepeatYAndFitWidth()
    {
        List<CanvasCall> calls = Record(canvas => ImagePainting.PaintImage(
            canvas: canvas,
            rect: new Rect(30.0, 30.0, 200.0, 400.0),
            image: new TestImage(100, 100),
            alignment: Alignment.TopLeft,
            repeat: ImageRepeat.RepeatY,
            fit: BoxFit.FitWidth));

        CanvasCall[] draws = calls.Where(call => call.Method == "drawImageRect").ToArray();
        Assert.Equal(2, draws.Length);
        Assert.Equal(
            new HashSet<Rect> { new(30.0, 30.0, 200.0, 200.0), new(30.0, 230.0, 200.0, 200.0) },
            draws.Select(call => call.Rect!.Value).ToHashSet());
    }

    [Fact]
    public void DecorationImage_ScaleTest()
    {
        var backgroundImage = new DecorationImage(
            new SynchronousTestImageProvider(),
            scale: 4,
            alignment: Alignment.TopLeft);
        var boxDecoration = new BoxDecoration(Image: backgroundImage);
        BoxPainter boxPainter = boxDecoration.CreateBoxPainter(() => Assert.Fail("onChanged"));
        var context = new TestRecordingPaintingContext();

        boxPainter.Paint(context, default, new ImageConfiguration(Size: new Size(100.0, 100.0)));

        CanvasCall call = Assert.Single(context.Calls, call => call.Method == "drawImageRect");
        Assert.Equal(LTRB(0, 0, 25, 25), call.Rect);
    }

    // shape_decoration_test.dart 'ShapeDecoration.image RTL test'.
    [Fact]
    public void ShapeDecorationImage_RtlTest()
    {
        var log = new List<int>();
        var decoration = new ShapeDecoration(
            Shape: new CircleBorder(),
            Image: new DecorationImage(
                new SynchronousTestImageProvider(new TestImage(100, 200)),
                alignment: AlignmentDirectional.BottomEnd));
        BoxPainter painter = decoration.CreateBoxPainter(() => log.Add(0));

        Assert.ThrowsAny<Exception>(() => painter.Paint(
            new TestRecordingPaintingContext(),
            default,
            new ImageConfiguration(Size: new Size(100.0, 100.0))));

        var rtl = new TestRecordingPaintingContext();
        painter.Paint(
            rtl,
            new Point(20.0, -40.0),
            new ImageConfiguration(Size: new Size(1000.0, 1000.0), TextDirection: TextDirection.Rtl));
        PaintAssert.Paints(
            rtl.Calls,
            PaintPattern.Paints.DrawImageRect(
                LTRB(0.0, 0.0, 100.0, 200.0),
                LTRB(20.0, 1000.0 - 40.0 - 200.0, 20.0 + 100.0, 1000.0 - 40.0)));

        var ltr = new TestRecordingPaintingContext();
        painter.Paint(
            ltr,
            default,
            new ImageConfiguration(Size: new Size(100.0, 200.0), TextDirection: TextDirection.Ltr));
        // We always use drawImageRect.
        Assert.DoesNotContain(ltr.Calls, call => call.Method == "drawImage");
        Assert.Empty(log);
    }

    // ---- DecorationImage.lerp and the blended painter --------------------------------------------------

    [Fact]
    public void Lerp_FollowsDartsShortcuts()
    {
        var provider = new SynchronousTestImageProvider();
        var a = new DecorationImage(provider);
        var b = new DecorationImage(provider, opacity: 0.5);
        var equalToA = new DecorationImage(provider);

        Assert.Null(DecorationImage.Lerp(null, null, 0.5));
        Assert.Same(a, DecorationImage.Lerp(a, a, 0.5));
        Assert.Same(a, DecorationImage.Lerp(a, b, 0.0));
        Assert.Same(b, DecorationImage.Lerp(a, b, 1.0));

        // There is no == shortcut and no clamping: equal images and extrapolated t still blend.
        DecorationImage blendedEqual = DecorationImage.Lerp(a, equalToA, 0.5)!;
        Assert.Equal("BlendedDecorationImage", blendedEqual.GetType().Name);
        DecorationImage extrapolated = DecorationImage.Lerp(a, b, 2.0)!;
        Assert.Equal(0.5, extrapolated.Opacity);
        Assert.Contains(", 2.0)", extrapolated.ToString());

        // A plain image never equals a blended one, in either direction.
        Assert.NotEqual(a, blendedEqual);
        Assert.NotEqual(blendedEqual, a);
        Assert.Equal(blendedEqual, DecorationImage.Lerp(a, equalToA, 0.5));
    }

    [Fact]
    public void BlendedPainter_PaintsBothImagesInOneLayerWithPlus()
    {
        var a = new DecorationImage(new SynchronousTestImageProvider(), fit: BoxFit.Fill);
        var b = new DecorationImage(new SynchronousTestImageProvider(), fit: BoxFit.Fill);
        DecorationImagePainter painter = DecorationImage.Lerp(a, b, 0.25)!.CreatePainter(() => { });

        List<CanvasCall> calls = Record(canvas => painter.Paint(
            canvas,
            new Rect(0, 0, 20, 20),
            null,
            ImageConfiguration.Empty));

        Assert.Equal(["saveLayer", "drawImageRect", "drawImageRect", "restore"], calls.Select(call => call.Method));
        Assert.Null(calls[0].Rect);
        Assert.Equal(BlendMode.SourceOver, calls[1].Paint!.BlendMode);
        Assert.Equal(Color.FromRGBO(0, 0, 0, 0.75), calls[1].Paint!.Color);
        Assert.Equal(BlendMode.Plus, calls[2].Paint!.BlendMode);
        Assert.Equal(Color.FromRGBO(0, 0, 0, 0.25), calls[2].Paint!.Color);
        painter.Dispose();
    }

    [Fact]
    public void BlendedPainter_ClipsEachImageInsideTheLayer()
    {
        var a = new DecorationImage(new SynchronousTestImageProvider());
        DecorationImagePainter painter = DecorationImage.Lerp(a, null, 0.5)!.CreatePainter(() => { });
        var clip = new Path();
        clip.AddOval(new Rect(0, 0, 20, 20));

        List<CanvasCall> calls = Record(canvas => painter.Paint(
            canvas,
            new Rect(0, 0, 20, 20),
            clip,
            ImageConfiguration.Empty,
            blendMode: BlendMode.Multiply));

        Assert.Equal(
            ["saveLayer", "save", "clipPath", "drawImageRect", "restore", "restore"],
            calls.Select(call => call.Method));
        Assert.Equal(BlendMode.Multiply, calls[3].Paint!.BlendMode);
        Assert.Equal(Color.FromRGBO(0, 0, 0, 0.5), calls[3].Paint!.Color);
    }

    // decoration_image_lerp_test.dart 'ImageDecoration.lerp 1': 1x1 green and red repeated images,
    // cross-faded by an additive blend inside one layer.
    [Theory]
    [InlineData(0.1, 0xFF19E600u)]
    [InlineData(0.2, 0xFF33CC00u)]
    [InlineData(0.5, 0xFF808000u)]
    [InlineData(0.8, 0xFFCD3200u)]
    [InlineData(0.9, 0xFFE61900u)]
    public void Lerp1_CrossFadesAdditively(double t, uint expected)
    {
        var green = new DecorationImage(new BitmapImageProvider(SolidBitmap(0xFF00FF00)), repeat: ImageRepeat.Repeat);
        var red = new DecorationImage(new BitmapImageProvider(SolidBitmap(0xFFFF0000)), repeat: ImageRepeat.Repeat);

        AssertPixelClose(expected, PaintDecorationPixel(DecorationImage.Lerp(green, red, t)!));
    }

    [Fact]
    public void Lerp1_SameImageWithComplementaryWeights_ReconstructsTheImage()
    {
        WriteableBitmap bitmap = SolidBitmap(0xFF00FF00);
        var green = new DecorationImage(new BitmapImageProvider(bitmap), repeat: ImageRepeat.Repeat);
        var green2 = new DecorationImage(new BitmapImageProvider(bitmap), repeat: ImageRepeat.Repeat);

        foreach (double t in new[] { 0.125, 0.25, 0.5, 0.75, 0.875 })
        {
            AssertPixelClose(0xFF00FF00, PaintDecorationPixel(DecorationImage.Lerp(green, green2, t)!));
            DecorationImage nested = DecorationImage.Lerp(
                DecorationImage.Lerp(green, green2, t),
                DecorationImage.Lerp(green, green2, t),
                t)!;
            AssertPixelClose(0xFF00FF00, PaintDecorationPixel(nested));
            DecorationImage complementary = DecorationImage.Lerp(
                DecorationImage.Lerp(green, green2, 1.0 - t),
                DecorationImage.Lerp(green, green2, t),
                t)!;
            AssertPixelClose(0xFF00FF00, PaintDecorationPixel(complementary));
        }
    }

    // ---- image_rtl_test.dart (DecorationImage cases) ---------------------------------------------------

    [Fact]
    public void DecorationImage_RtlWithAlignmentTopEndAndMatch()
    {
        using var tester = new FrameworkDartTester();
        PumpDecorationImage(tester, TextDirection.Rtl, AlignmentDirectional.TopEnd, ImageRepeat.RepeatX, match: true);
        Rect source = LTRB(0.0, 0.0, 16.0, 9.0);

        PaintPattern pattern = PaintPattern.Paints
            .ClipRect(LTRB(0.0, 0.0, 100.0, 50.0))
            .Translate(50.0, 0.0)
            .Scale(-1.0, 1.0)
            .Translate(-50.0, 0.0);
        foreach (Rect destination in Tiles(-12.0))
        {
            pattern = pattern.DrawImageRect(source, destination);
        }

        PaintAssert.Paints(Find.ByType<Container>(), pattern.Restore());
        PaintAssert.DoesNotPaint(Find.ByType<Container>(), PaintPattern.Paints.Scale().Scale());
    }

    [Fact]
    public void DecorationImage_LtrWithAlignmentTopEnd_PointlessMatch()
    {
        using var tester = new FrameworkDartTester();
        PumpDecorationImage(tester, TextDirection.Ltr, AlignmentDirectional.TopEnd, ImageRepeat.RepeatX, match: true);
        AssertTopEndLtrTiles();
    }

    [Fact]
    public void DecorationImage_RtlWithAlignmentTopEnd()
    {
        using var tester = new FrameworkDartTester();
        PumpDecorationImage(tester, TextDirection.Rtl, AlignmentDirectional.TopEnd, ImageRepeat.RepeatX, match: false);
        Rect source = LTRB(0.0, 0.0, 16.0, 9.0);

        PaintPattern pattern = PaintPattern.Paints.ClipRect(LTRB(0.0, 0.0, 100.0, 50.0));
        foreach (Rect destination in Tiles(0.0))
        {
            pattern = pattern.DrawImageRect(source, destination);
        }

        PaintAssert.Paints(Find.ByType<Container>(), pattern.Restore());
        PaintAssert.DoesNotPaint(Find.ByType<Container>(), PaintPattern.Paints.Scale());
    }

    [Fact]
    public void DecorationImage_LtrWithAlignmentTopEnd()
    {
        using var tester = new FrameworkDartTester();
        PumpDecorationImage(tester, TextDirection.Ltr, AlignmentDirectional.TopEnd, ImageRepeat.RepeatX, match: false);
        AssertTopEndLtrTiles();
    }

    [Fact]
    public void DecorationImage_RtlWithAlignmentCenterRightAndMatch()
    {
        using var tester = new FrameworkDartTester();
        PumpDecorationImage(tester, TextDirection.Rtl, Alignment.CenterRight, ImageRepeat.NoRepeat, match: true);

        PaintAssert.Paints(
            Find.ByType<Container>(),
            PaintPattern.Paints
                .Translate(50.0, 0.0)
                .Scale(-1.0, 1.0)
                .Translate(-50.0, 0.0)
                .DrawImageRect(LTRB(0.0, 0.0, 16.0, 9.0), LTRB(0.0, 20.5, 16.0, 29.5))
                .Restore());
        PaintAssert.DoesNotPaint(Find.ByType<Container>(), PaintPattern.Paints.Scale().Scale());
        PaintAssert.DoesNotPaint(Find.ByType<Container>(), PaintPattern.Paints.DrawImageRect().DrawImageRect());
    }

    [Theory]
    [InlineData(TextDirection.Rtl, false)]
    [InlineData(TextDirection.Ltr, true)]
    [InlineData(TextDirection.Ltr, false)]
    public void DecorationImage_CenterRight_WithoutAFlip(TextDirection direction, bool match)
    {
        using var tester = new FrameworkDartTester();
        PumpDecorationImage(tester, direction, Alignment.CenterRight, ImageRepeat.NoRepeat, match);

        PaintAssert.Paints(
            Find.ByType<Container>(),
            PaintPattern.Paints.DrawImageRect(LTRB(0.0, 0.0, 16.0, 9.0), LTRB(84.0, 20.5, 100.0, 29.5)));
        PaintAssert.DoesNotPaint(Find.ByType<Container>(), PaintPattern.Paints.Scale());
        PaintAssert.DoesNotPaint(Find.ByType<Container>(), PaintPattern.Paints.DrawImageRect().DrawImageRect());
    }

    private static void AssertTopEndLtrTiles()
    {
        Rect source = LTRB(0.0, 0.0, 16.0, 9.0);
        PaintPattern pattern = PaintPattern.Paints.ClipRect(LTRB(0.0, 0.0, 100.0, 50.0));
        foreach (Rect destination in Tiles(-12.0))
        {
            pattern = pattern.DrawImageRect(source, destination);
        }

        PaintAssert.Paints(Find.ByType<Container>(), pattern.Restore());
        PaintAssert.DoesNotPaint(Find.ByType<Container>(), PaintPattern.Paints.Scale());
    }

    private static IEnumerable<Rect> Tiles(double firstLeft)
    {
        for (int index = 0; index < 7; index++)
        {
            double left = firstLeft + (index * 16.0);
            yield return LTRB(left, 0.0, left + 16.0, 9.0);
        }
    }

    private static void PumpDecorationImage(
        FrameworkDartTester tester,
        TextDirection direction,
        AlignmentGeometry alignment,
        ImageRepeat repeat,
        bool match)
    {
        tester.PumpWidget(new Directionality(
            textDirection: direction,
            child: new Center(child: new Container(
                width: 100.0,
                height: 50.0,
                decoration: new BoxDecoration(Image: new DecorationImage(
                    new SynchronousTestImageProvider(new TestImage(16, 9)),
                    alignment: alignment,
                    repeat: repeat,
                    matchTextDirection: match))))));
    }

    // ---- painting/debug.dart: ImageSizeInfo and the painting vars ------------------------------------

    [Fact]
    public void ImageSizeInfo_ReportsBytesJsonEqualityAndToString()
    {
        var info = new ImageSizeInfo(source: "a.png", displaySize: new Size(100, 50), imageSize: new Size(300, 300));

        Assert.Equal((int)(100 * 50 * 4 * (4.0 / 3.0)), info.DisplaySizeInBytes);
        Assert.Equal(480000, info.DecodedSizeInBytes);
        Dictionary<string, object?> json = info.ToJson();
        Assert.Equal("a.png", json["source"]);
        Assert.Equal(300.0, ((Dictionary<string, object?>)json["imageSize"]!)["width"]);
        Assert.Equal(info.DecodedSizeInBytes, json["decodedSizeInBytes"]);
        Assert.Equal(
            new ImageSizeInfo(source: "a.png", displaySize: new Size(100, 50), imageSize: new Size(300, 300)),
            info);
        Assert.NotEqual(new ImageSizeInfo(source: "b.png", displaySize: new Size(100, 50), imageSize: new Size(300, 300)), info);
        Assert.Equal("ImageSizeInfo(a.png, imageSize: Size(300.0, 300.0), displaySize: Size(100.0, 50.0))", info.ToString());
    }

    [Fact]
    public void DebugAssertAllPaintingVarsUnset_ThrowsWhenAVarIsSet()
    {
        Assert.True(PaintingDebug.DebugAssertAllPaintingVarsUnset("ok"));
        PaintingDebug.DebugInvertOversizedImages = true;
        Assert.Throws<FlutterError>(() => PaintingDebug.DebugAssertAllPaintingVarsUnset("set"));
        PaintingDebug.DebugInvertOversizedImages = false;
        PaintingDebug.DebugImageOverheadAllowance = 1;
        Assert.Throws<FlutterError>(() => PaintingDebug.DebugAssertAllPaintingVarsUnset("set"));
        PaintingDebug.DebugImageOverheadAllowance = 128 * 1024;
        PaintingDebug.DebugOnPaintImage = _ => { };
        Assert.Throws<FlutterError>(() => PaintingDebug.DebugAssertAllPaintingVarsUnset("set"));
        PaintingDebug.DebugOnPaintImage = null;
        Assert.True(PaintingDebug.DebugAssertAllPaintingVarsUnset("ok"));
    }

    // ---- helpers ---------------------------------------------------------------------------------------

    private static void AssertCloseToRect(Rect expected, Rect actual)
    {
        bool close = Math.Abs(expected.Left - actual.Left) < 1e-9
                     && Math.Abs(expected.Top - actual.Top) < 1e-9
                     && Math.Abs(expected.Right - actual.Right) < 1e-9
                     && Math.Abs(expected.Bottom - actual.Bottom) < 1e-9;
        Assert.True(close, $"expected {expected}, got {actual}");
    }

    private static (byte R, byte G, byte B, byte A) PaintDecorationPixel(DecorationImage image)
    {
        DecorationImagePainter painter = image.CreatePainter(() => { });
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        painter.Paint(canvas, new Rect(0, 0, 20, 20), null, new ImageConfiguration(TextDirection: TextDirection.Ltr));
        Bitmap bitmap = recorder.EndRecording().ToImageSync(20, 20);
        painter.Dispose();
        return RasterBackend.PixelAt(bitmap, 10, 10);
    }

    // The Dart test reads the capture as straight RGBA and forces the alpha to 0xFF, so only the
    // unpremultiplied colour channels are compared.
    private static void AssertPixelClose(uint expected, (byte R, byte G, byte B, byte A) pixel)
    {
        Assert.True(pixel.A > 0, "the pixel is transparent");
        int Straight(byte value) => (int)Math.Round(value * 255.0 / pixel.A);
        Assert.InRange(Straight(pixel.R), (int)((expected >> 16) & 0xFF) - 2, (int)((expected >> 16) & 0xFF) + 2);
        Assert.InRange(Straight(pixel.G), (int)((expected >> 8) & 0xFF) - 2, (int)((expected >> 8) & 0xFF) + 2);
        Assert.InRange(Straight(pixel.B), (int)(expected & 0xFF) - 2, (int)(expected & 0xFF) + 2);
    }

    private static WriteableBitmap SolidBitmap(uint argb)
    {
        var bitmap = new WriteableBitmap(new PixelSize(1, 1), new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);
        using ILockedFramebuffer framebuffer = bitmap.Lock();
        byte[] pixel = [(byte)argb, (byte)(argb >> 8), (byte)(argb >> 16), (byte)(argb >> 24)];
        Marshal.Copy(pixel, 0, framebuffer.Address, 4);
        return bitmap;
    }

    private sealed class Disposer(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }

    private sealed class TestImage(int width, int height) : IImage
    {
        public Size Size { get; } = new(width, height);

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
        }
    }

    // decoration_test.dart's SynchronousTestImageProvider: a 100x100 image (or the given one), completed
    // synchronously.
    private sealed class SynchronousTestImageProvider(IImage? image = null) : ImageProvider<string>
    {
        private readonly string _key = $"synchronous-{Interlocked.Increment(ref s_nextKey)}";
        private readonly IImage _image = image ?? new TestImage(100, 100);

        public override ValueTask<string> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(_key);

        protected override ImageStreamCompleter LoadImage(string key) => new SynchronousCompleter(new ImageInfo(_image));

        public override string ToString() => "SynchronousTestImageProvider()";
    }

    private sealed class BitmapImageProvider(IImage image) : ImageProvider<string>
    {
        private readonly string _key = $"bitmap-{Interlocked.Increment(ref s_nextKey)}";

        public override ValueTask<string> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(_key);

        protected override ImageStreamCompleter LoadImage(string key) => new SynchronousCompleter(new ImageInfo(image));
    }

    private sealed class SynchronousErrorTestImageProvider(string message) : ImageProvider<string>
    {
        private readonly string _key = $"error-{Interlocked.Increment(ref s_nextKey)}";

        public override ValueTask<string> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(_key);

        protected override ImageStreamCompleter LoadImage(string key)
        {
            var completer = new ManualImageStreamCompleter();
            completer.ReportError(new InvalidOperationException(message));
            return completer;
        }
    }

    private sealed class ControllableImageProvider(ManualImageStreamCompleter completer) : ImageProvider<string>
    {
        private readonly string _key = $"controllable-{Interlocked.Increment(ref s_nextKey)}";

        public override ValueTask<string> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(_key);

        protected override ImageStreamCompleter LoadImage(string key) => completer;
    }

    private sealed class SynchronousCompleter : ImageStreamCompleter
    {
        public SynchronousCompleter(ImageInfo image)
        {
            SetImage(image);
        }
    }

    private sealed class ManualImageStreamCompleter : ImageStreamCompleter
    {
        public void SetImageForTest(ImageInfo image) => SetImage(image);
    }
}
